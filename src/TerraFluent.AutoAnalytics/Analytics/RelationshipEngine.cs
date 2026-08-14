using System;
using System.Collections.Generic;
using System.Linq;
using TerraFluent.AutoAnalytics.Enums;
using TerraFluent.AutoAnalytics.Profiling;
using TerraFluent.AutoAnalytics.Schema;
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
            // Pair the two measures by ORIGINAL ROW, keeping only rows where both are present.
            // (Each column's NumericValues has missing cells compacted out independently, so
            // aligning by list index would correlate mismatched rows once any value is missing.)
            var (xs, ys) = RowAlignment.NumericPairs(measures[i], measures[j]);
            int n = xs.Count;
            if (n < 3) continue;

            double pearson = Correlation.Pearson(xs, ys);
            double spearman = Correlation.Spearman(xs, ys);

            results.Add(new CorrelationResult
            {
                ColumnX = measures[i].Name,
                ColumnY = measures[j].Name,
                Pearson = pearson,
                Spearman = spearman,
                Strength = Classify(pearson, spearman),
                SampleSize = n,
                PValue = Correlation.PValue(pearson, n)
            });
        }

        return results.OrderByDescending(r => Math.Abs(r.Pearson)).ToList();
    }

    /// <summary>
    /// Aggregates each measure by each low-cardinality dimension. Additive measures (revenue, cost,
    /// quantity) are summed; non-additive per-row attributes (age, tenure, ratings) are averaged.
    /// </summary>
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
        // Pair dimension label with measure value by ORIGINAL ROW (complete-case), so a missing
        // label or value never shifts the remaining rows into the wrong group.
        var pairs = RowAlignment.LabelValues(dimension, measure);
        if (pairs.Count == 0) return null;

        // Additive measures (revenue, cost, quantity) are summed; non-additive per-row attributes
        // (age, tenure, ratings) are averaged.
        bool additive = MeasureSemantics.IsAdditive(measure.Profile, measure.Numeric?.Min, measure.Numeric?.Max);

        // Accumulate a running sum and count per group so we can emit a sum or an average.
        var groups = new Dictionary<string, (double Sum, int Count)>(StringComparer.OrdinalIgnoreCase);
        foreach (var (label, value) in pairs)
        {
            groups.TryGetValue(label, out var acc);
            groups[label] = (acc.Sum + value, acc.Count + 1);
        }

        var aggregated = groups.ToDictionary(
            kv => kv.Key,
            kv => additive ? kv.Value.Sum : kv.Value.Sum / kv.Value.Count,
            StringComparer.OrdinalIgnoreCase);

        double total = aggregated.Values.Sum();
        var buckets = aggregated.OrderByDescending(kv => kv.Value)
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
            Aggregation = additive ? "sum" : "average",
            IsAdditive = additive,
            Total = total,
            Buckets = buckets
        };
    }

    private static IReadOnlyList<GroupAnalysisResult> Order(List<GroupAnalysisResult> results) =>
        results.OrderByDescending(r => r.Top?.Share ?? 0).ToList();

    private static CorrelationStrength Classify(double pearson, double spearman)
    {
        // Use the stronger of the linear (Pearson) and monotonic (Spearman) coefficients so a
        // strong but curved/monotonic relationship isn't dismissed by a weaker linear r.
        double a = Math.Max(Math.Abs(pearson), Math.Abs(spearman));
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
