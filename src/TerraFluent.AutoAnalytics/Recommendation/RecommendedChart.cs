using ChartType = TerraFluent.Chart.Reporting.Enums.ChartType;

namespace TerraFluent.AutoAnalytics.Recommendation;

/// <summary>
/// A single chart recommendation with a deterministic suitability score (0..100) and a plain-English
/// explanation of why it was chosen. The <see cref="Spec"/> carries the data needed to render it.
/// </summary>
public sealed class RecommendedChart
{
    /// <summary>The recommended chart type from the rendering library.</summary>
    public ChartType ChartType { get; init; }

    /// <summary>Human-readable chart type name (for JSON/UI).</summary>
    public string ChartName => ChartType.ToString();

    /// <summary>Suitability score 0..100 (higher = better fit). Recommendations are sorted descending.</summary>
    public int SuitabilityScore { get; init; }

    /// <summary>Plain-English justification for the recommendation.</summary>
    public string Reason { get; init; } = string.Empty;

    /// <summary>The concrete data/spec used to build and render the chart.</summary>
    public ChartSpec Spec { get; init; } = new();
}
