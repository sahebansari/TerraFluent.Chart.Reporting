using System;
using System.Collections.Generic;
using System.Linq;
using TerraFluent.AutoAnalytics.Profiling;
using TerraFluent.AutoAnalytics.Statistics;

namespace TerraFluent.AutoAnalytics.Analytics;

/// <summary>
/// Phase 4g — explains individual anomalies by testing whether any dimension accounts for them.
/// </summary>
/// <remarks>
/// This is a different question from <see cref="DriverEngine"/>, which attributes a measure's
/// <em>change over time</em> to categories by splitting history into halves. Here the subject is a
/// single outlying observation: for each dimension the engine asks how the value compares to its own
/// category's norm rather than the dataset's. Deterministic; medians are used throughout so a second
/// outlier in the same category cannot drag the baseline toward the point being explained.
/// </remarks>
public sealed class AnomalyExplanationEngine
{
    /// <summary>Minimum comparison rows a category needs before its median is trusted.</summary>
    private const int MinCategoryRows = 3;

    /// <summary>Widest a category may be and still be a useful explanation.</summary>
    private const int MaxDistinct = 50;

    /// <summary>Share of the deviation a category must absorb for the anomaly to count as explained.</summary>
    private const double ExplainedThreshold = 0.5;

    private readonly int _maxPerMeasure;

    public AnomalyExplanationEngine(int maxPerMeasure = 3)
        => _maxPerMeasure = maxPerMeasure < 1 ? 1 : maxPerMeasure;

    /// <summary>
    /// Explains the most extreme anomalies in each measure. Anomalies whose row cannot be traced to
    /// any usable dimension are skipped rather than reported without evidence.
    /// </summary>
    public IReadOnlyList<AnomalyExplanation> Explain(
        DatasetProfile profile, IReadOnlyList<AnomalyResult> anomalies)
    {
        if (profile is null) throw new ArgumentNullException(nameof(profile));
        if (anomalies is null) return Array.Empty<AnomalyExplanation>();

        var dimensions = profile.Categories
            .Where(c => c.Categorical is { DistinctCount: >= 2 and <= MaxDistinct })
            .ToList();
        if (dimensions.Count == 0) return Array.Empty<AnomalyExplanation>();

        var explanations = new List<AnomalyExplanation>();

        foreach (var result in anomalies)
        {
            var measure = profile.ByName(result.Measure);
            if (measure?.Numeric is null || measure.NumericByRow.Count == 0) continue;

            double overallMedian = measure.Numeric.Median;

            foreach (var point in result.Anomalies.Take(_maxPerMeasure))
            {
                var explanation = ExplainPoint(measure, dimensions, point, overallMedian);
                if (explanation is not null) explanations.Add(explanation);
            }
        }

        return explanations
            .OrderByDescending(e => e.Magnitude)
            .ToList();
    }

    private AnomalyExplanation? ExplainPoint(
        ColumnStatistics measure, IReadOnlyList<ColumnStatistics> dimensions,
        AnomalyPoint point, double overallMedian)
    {
        int row = point.RowIndex;
        if (row < 0 || row >= measure.NumericByRow.Count) return null;

        // Distance from the dataset norm — the deviation an explanation has to absorb. A value
        // sitting exactly on the median is not an anomaly worth attributing.
        double excess = Math.Abs(point.Value - overallMedian);
        if (excess < 1e-9) return null;

        var attributions = new List<AnomalyAttribution>();

        foreach (var dimension in dimensions)
        {
            if (row >= dimension.LabelByRow.Count) continue;
            string? category = dimension.LabelByRow[row];
            if (string.IsNullOrWhiteSpace(category)) continue;

            var peers = PeerValues(measure, dimension, category!, excludeRow: row);
            if (peers.Count < MinCategoryRows) continue;

            peers.Sort();
            double categoryMedian = DescriptiveStatistics.Percentile(peers, 50);

            double residual = Math.Abs(point.Value - categoryMedian);
            double explained = Clamp01(1 - residual / excess);

            attributions.Add(new AnomalyAttribution
            {
                Dimension = dimension.Name,
                Category = category!,
                CategoryMedian = categoryMedian,
                CategoryCount = peers.Count,
                ResidualRatio = Math.Abs(categoryMedian) > 1e-9 ? point.Value / categoryMedian : double.NaN,
                ExplainedFraction = explained
            });
        }

        if (attributions.Count == 0) return null;

        attributions = attributions
            .OrderByDescending(a => a.ExplainedFraction)
            .ThenBy(a => a.Dimension, StringComparer.OrdinalIgnoreCase)
            .ToList();

        return new AnomalyExplanation
        {
            Measure = measure.Name,
            WhenLabel = point.Label ?? $"row {point.Index + 1}",
            RowIndex = row,
            Value = point.Value,
            OverallMedian = overallMedian,
            OverallRatio = Math.Abs(overallMedian) > 1e-9 ? point.Value / overallMedian : double.NaN,
            Magnitude = point.Magnitude,
            Attributions = attributions,
            IsExplained = attributions[0].ExplainedFraction >= ExplainedThreshold
        };
    }

    /// <summary>
    /// The measure's values for other rows sharing the anomaly's category. The anomalous row is
    /// excluded so a sparse category cannot explain its own outlier.
    /// </summary>
    private static List<double> PeerValues(
        ColumnStatistics measure, ColumnStatistics dimension, string category, int excludeRow)
    {
        var labels = dimension.LabelByRow;
        var values = measure.NumericByRow;
        int n = Math.Min(labels.Count, values.Count);

        var peers = new List<double>();
        for (int i = 0; i < n; i++)
        {
            if (i == excludeRow) continue;
            if (!values[i].HasValue) continue;
            if (!string.Equals(labels[i], category, StringComparison.OrdinalIgnoreCase)) continue;
            peers.Add(values[i]!.Value);
        }
        return peers;
    }

    private static double Clamp01(double x) =>
        double.IsNaN(x) ? 0 : x < 0 ? 0 : x > 1 ? 1 : x;
}
