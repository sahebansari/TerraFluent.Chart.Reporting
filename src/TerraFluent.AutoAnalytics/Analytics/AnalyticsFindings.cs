using System.Collections.Generic;

namespace TerraFluent.AutoAnalytics.Analytics;

/// <summary>Bundles the outputs of the relationship, trend and anomaly engines for one dataset.</summary>
public sealed class AnalyticsFindings
{
    public IReadOnlyList<CorrelationResult> Correlations { get; init; } = new List<CorrelationResult>();
    public IReadOnlyList<GroupAnalysisResult> Groups { get; init; } = new List<GroupAnalysisResult>();
    public IReadOnlyList<TrendResult> Trends { get; init; } = new List<TrendResult>();
    public IReadOnlyList<AnomalyResult> Anomalies { get; init; } = new List<AnomalyResult>();
    /// <summary>Per-measure simple moving averages (when a date column exists with ≥8 periods).</summary>
    public IReadOnlyList<MovingAverageResult> MovingAverages { get; init; } = new List<MovingAverageResult>();
    /// <summary>Running cumulative totals for additive measures (when a date column exists).</summary>
    public IReadOnlyList<CumulativeSeriesResult> CumulativeSeries { get; init; } = new List<CumulativeSeriesResult>();
    /// <summary>Cross-dimensional breakdowns (date×dim or dim×dim) for stacked-chart recommendations.</summary>
    public IReadOnlyList<CompositionResult> Compositions { get; init; } = new List<CompositionResult>();
}
