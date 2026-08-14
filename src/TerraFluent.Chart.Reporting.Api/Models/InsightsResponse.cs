namespace TerraFluent.Chart.Reporting.Api.Models;

/// <summary>The executive summary plus ranked insights for a dataset.</summary>
public sealed record InsightsResponse
{
    public required SummaryDto Summary { get; init; }
    public required IReadOnlyList<InsightDto> Insights { get; init; }
}
