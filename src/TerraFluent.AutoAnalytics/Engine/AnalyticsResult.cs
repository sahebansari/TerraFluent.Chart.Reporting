using System.Collections.Generic;
using TerraFluent.AutoAnalytics.Analytics;
using TerraFluent.AutoAnalytics.Insights;
using TerraFluent.AutoAnalytics.Profiling;
using TerraFluent.AutoAnalytics.Recommendation;
using TerraFluent.AutoAnalytics.Validation;

namespace TerraFluent.AutoAnalytics.Engine;

/// <summary>
/// The complete, immutable output of <see cref="AnalyticsEngine"/>: schema/profile, validation,
/// raw analytics findings, ranked insights, chart recommendations and an executive summary.
/// </summary>
public sealed class AnalyticsResult
{
    /// <summary>Per-column statistical profile of the dataset.</summary>
    public DatasetProfile Profile { get; init; } = new();

    /// <summary>Data-quality findings.</summary>
    public ValidationReport Validation { get; init; } = new(new List<ValidationIssue>());

    /// <summary>Raw correlation/group/trend/anomaly findings.</summary>
    public AnalyticsFindings Findings { get; init; } = new();

    /// <summary>Ranked, natural-language insights.</summary>
    public IReadOnlyList<Insight> Insights { get; init; } = new List<Insight>();

    /// <summary>Ranked chart recommendations with suitability scores and explanations.</summary>
    public IReadOnlyList<RecommendedChart> Recommendations { get; init; } = new List<RecommendedChart>();

    /// <summary>Executive summary.</summary>
    public AnalyticsSummary Summary { get; init; } = new();
}
