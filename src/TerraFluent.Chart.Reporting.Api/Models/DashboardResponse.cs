using TerraFluent.AutoAnalytics.Dashboard;
using TerraFluent.AutoAnalytics.Engine;
using TerraFluent.AutoAnalytics.Recommendation;

namespace TerraFluent.Chart.Reporting.Api.Models;

/// <summary>An auto-generated dashboard: KPIs, chart sections (with rendered SVG), anomalies and insights.</summary>
public sealed record DashboardResponse
{
    public required string Title { get; init; }
    public required SummaryDto Summary { get; init; }
    public required IReadOnlyList<KpiDto> Kpis { get; init; }
    public required IReadOnlyList<DashboardChartDto> TrendCharts { get; init; }
    public required IReadOnlyList<DashboardChartDto> ComparisonCharts { get; init; }
    public required IReadOnlyList<DashboardChartDto> DistributionCharts { get; init; }
    public required IReadOnlyList<InsightDto> Anomalies { get; init; }
    public required IReadOnlyList<InsightDto> KeyInsights { get; init; }

    /// <summary>Maps a dashboard definition + analysis to the API response, rendering each chart to SVG.</summary>
    public static DashboardResponse From(DashboardDefinition d, AnalyticsResult result, ChartStyle style = default) => new()
    {
        Title = d.Title,
        Summary = SummaryDto.From(result.Summary),
        Kpis = d.Kpis.Select(k => new KpiDto
        {
            Label = k.Label,
            DisplayValue = k.DisplayValue,
            RawValue = k.RawValue,
            Caption = k.Caption
        }).ToList(),
        TrendCharts = d.TrendCharts.Select(c => DashboardChartDto.From(c, style)).ToList(),
        ComparisonCharts = d.ComparisonCharts.Select(c => DashboardChartDto.From(c, style)).ToList(),
        DistributionCharts = d.DistributionCharts.Select(c => DashboardChartDto.From(c, style)).ToList(),
        Anomalies = d.Anomalies.Select(InsightDto.From).ToList(),
        KeyInsights = d.KeyInsights.Select(InsightDto.From).ToList()
    };
}

/// <summary>A headline KPI card.</summary>
public sealed record KpiDto
{
    public required string Label { get; init; }
    public required string DisplayValue { get; init; }
    public double RawValue { get; init; }
    public string? Caption { get; init; }
}

/// <summary>A recommended chart rendered to SVG for embedding.</summary>
public sealed record DashboardChartDto
{
    public required string ChartType { get; init; }
    public int SuitabilityScore { get; init; }
    public required string Title { get; init; }
    public required string Reason { get; init; }
    public required string Svg { get; init; }

    public static DashboardChartDto From(RecommendedChart rec, ChartStyle style = default) => new()
    {
        ChartType = rec.ChartName,
        SuitabilityScore = rec.SuitabilityScore,
        Title = rec.Spec.Title,
        Reason = rec.Reason,
        Svg = style.Apply(ChartConfigBuilder.ToChartBuilder(rec.Spec).Size(560, 340)).RenderToSvg()
    };
}
