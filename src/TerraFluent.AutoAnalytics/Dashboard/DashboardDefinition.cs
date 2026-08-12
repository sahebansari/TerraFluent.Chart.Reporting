using System.Collections.Generic;
using TerraFluent.AutoAnalytics.Insights;
using TerraFluent.AutoAnalytics.Recommendation;

namespace TerraFluent.AutoAnalytics.Dashboard;

/// <summary>A single headline metric (KPI card) for the executive strip of a dashboard.</summary>
public sealed class KpiCard
{
    public string Label { get; init; } = string.Empty;
    /// <summary>Formatted display value (e.g. "$1.2M", "34%").</summary>
    public string DisplayValue { get; init; } = string.Empty;
    /// <summary>Raw numeric value behind the card.</summary>
    public double RawValue { get; init; }
    /// <summary>Optional caption (e.g. "sum of Revenue").</summary>
    public string? Caption { get; init; }
}

/// <summary>
/// A fully-assembled auto-dashboard: executive KPIs, trend/comparison/distribution chart sections,
/// an anomalies panel and a key-insights narrative panel.
/// </summary>
public sealed class DashboardDefinition
{
    public string Title { get; init; } = "Dashboard";

    /// <summary>Top-line metrics.</summary>
    public IReadOnlyList<KpiCard> Kpis { get; init; } = new List<KpiCard>();

    /// <summary>Time-series charts.</summary>
    public IReadOnlyList<RecommendedChart> TrendCharts { get; init; } = new List<RecommendedChart>();

    /// <summary>Category comparison / share charts.</summary>
    public IReadOnlyList<RecommendedChart> ComparisonCharts { get; init; } = new List<RecommendedChart>();

    /// <summary>Distribution charts.</summary>
    public IReadOnlyList<RecommendedChart> DistributionCharts { get; init; } = new List<RecommendedChart>();

    /// <summary>Detected anomalies, surfaced as insights.</summary>
    public IReadOnlyList<Insight> Anomalies { get; init; } = new List<Insight>();

    /// <summary>The most important narrative insights.</summary>
    public IReadOnlyList<Insight> KeyInsights { get; init; } = new List<Insight>();
}
