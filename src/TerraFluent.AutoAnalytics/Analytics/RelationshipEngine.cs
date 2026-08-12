using System;
using System.Collections.Generic;
using System.Linq;
using TerraFluent.AutoAnalytics.Enums;
using TerraFluent.AutoAnalytics.Profiling;
using TerraFluent.AutoAnalytics.Statistics;

namespace TerraFluent.AutoAnalytics.Analytics;

/// <summary>
/// Phase 4 — discovers relationships: pairwise correlations between measures and group-by
/// aggregations of measures across dimensions.
/// </summary>
public sealed class RelationshipEngine
{
    private readonly int _maxGroupCombinations;

    public RelationshipEngine(int maxGroupCombinations = 12)
        => _maxGroupCombinations = maxGroupCombinations;

    /// <summary>Computes Pearson/Spearman correlations for every measure pair with aligned data.</summary>
    public IReadOnlyList<CorrelationResult> Correlations(DatasetProfile profile)
    {
        var measures = profile.Measures.Where(m => m.NumericValues.Count >= 3).ToList();
        var results = new List<CorrelationResult>();

        for (int i = 0; i < measures.Count; i++)
        for (int j = i + 1; j < measures.Count; j++)
        {
            var x = measures[i].NumericValues;
            var y = measures[j].NumericValues;
            int n = Math.Min(x.Count, y.Count);
            if (n < 3) continue;

            var xs = x.Take(n).ToList();
            var ys = y.Take(n).ToList();
            double pearson = Correlation.Pearson(xs, ys);
            double spearman = Correlation.Spearman(xs, ys);

            results.Add(new CorrelationResult
            {
                ColumnX = measures[i].Name,
                ColumnY = measures[j].Name,
                Pearson = pearson,
                Spearman = spearman,
                Strength = Classify(pearson),
                SampleSize = n
            });
        }

        return results.OrderByDescending(r => Math.Abs(r.Pearson)).ToList();
    }

    /// <summary>Aggregates each measure by each low-cardinality dimension (summed).</summary>
    public IReadOnlyList<GroupAnalysisResult> GroupAnalyses(DatasetProfile profile)
    {
        var results = new List<GroupAnalysisResult>();
        var dimensions = profile.Categories
            .Where(c => c.Categorical is { DistinctCount: >= 2 and <= 50 })
            .ToList();
        var measures = profile.Measures.Where(m => m.NumericValues.Count > 0).ToList();

        foreach (var dim in dimensions)
        foreach (var measure in measures)
        {
            var result = Aggregate(dim, measure);
            if (result is not null) results.Add(result);
            if (results.Count >= _maxGroupCombinations) return Order(results);
        }
        return Order(results);
    }

    private static GroupAnalysisResult? Aggregate(ColumnStatistics dimension, ColumnStatistics measure)
    {
        var labels = dimension.Labels;
        var values = measure.NumericValues;
        int n = Math.Min(labels.Count, values.Count);
        if (n == 0) return null;

        var sums = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
        for (int i = 0; i < n; i++)
            sums[labels[i]] = sums.TryGetValue(labels[i], out var s) ? s + values[i] : values[i];

        double total = sums.Values.Sum();
        var buckets = sums.OrderByDescending(kv => kv.Value)
            .Select(kv => new GroupBucket
            {
                Key = kv.Key,
                Value = kv.Value,
                Share = total == 0 ? 0 : kv.Value / total
            })
            .ToList();

        return new GroupAnalysisResult
        {
            Dimension = dimension.Name,
            Measure = measure.Name,
            Aggregation = "sum",
            Total = total,
            Buckets = buckets
        };
    }

    private static IReadOnlyList<GroupAnalysisResult> Order(List<GroupAnalysisResult> results) =>
        results.OrderByDescending(r => r.Top?.Share ?? 0).ToList();

    private static CorrelationStrength Classify(double r)
    {
        double a = Math.Abs(r);
        return a switch
        {
            >= 0.9 => CorrelationStrength.VeryStrong,
            >= 0.7 => CorrelationStrength.Strong,
            >= 0.4 => CorrelationStrength.Moderate,
            >= 0.2 => CorrelationStrength.Weak,
            _      => CorrelationStrength.None
        };
    }
}
