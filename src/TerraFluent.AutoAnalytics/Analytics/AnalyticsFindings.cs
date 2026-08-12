using System.Collections.Generic;

namespace TerraFluent.AutoAnalytics.Analytics;

/// <summary>Bundles the outputs of the relationship, trend and anomaly engines for one dataset.</summary>
public sealed class AnalyticsFindings
{
    public IReadOnlyList<CorrelationResult> Correlations { get; init; } = new List<CorrelationResult>();
    public IReadOnlyList<GroupAnalysisResult> Groups { get; init; } = new List<GroupAnalysisResult>();
    public IReadOnlyList<TrendResult> Trends { get; init; } = new List<TrendResult>();
    public IReadOnlyList<AnomalyResult> Anomalies { get; init; } = new List<AnomalyResult>();
}
