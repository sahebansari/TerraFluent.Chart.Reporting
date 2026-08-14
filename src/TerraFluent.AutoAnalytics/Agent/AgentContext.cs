using System.Collections.Generic;
using TerraFluent.AutoAnalytics.Analytics;
using TerraFluent.AutoAnalytics.Engine;
using TerraFluent.AutoAnalytics.Insights;
using TerraFluent.AutoAnalytics.Profiling;
using TerraFluent.AutoAnalytics.Recommendation;

namespace TerraFluent.AutoAnalytics.Agent;

/// <summary>
/// Shared, read-only investigation state handed to every <see cref="IAnalyticSkill"/>: the profiled
/// dataset, the raw analytics findings, and the full pool of insights/recommendations produced by
/// the one-shot <see cref="AnalyticsEngine"/> pass. Skills select from this pool for the current
/// <see cref="Goal"/>; the agent never recomputes the underlying statistics.
/// </summary>
public sealed class AgentContext
{
    public required DatasetProfile Profile { get; init; }
    public required AnalyticsFindings Findings { get; init; }
    public required IReadOnlyList<Insight> Insights { get; init; }
    public required IReadOnlyList<RecommendedChart> Recommendations { get; init; }
    public required AnalyticsSummary Summary { get; init; }

    /// <summary>The goal currently being pursued. Set by the agent before each skill executes.</summary>
    public AnalyticGoal Goal { get; internal set; } = AnalyticGoal.Explore();

    /// <summary>Builds a context from a completed analysis result.</summary>
    public static AgentContext FromResult(AnalyticsResult result) => new()
    {
        Profile = result.Profile,
        Findings = result.Findings,
        Insights = result.Insights,
        Recommendations = result.Recommendations,
        Summary = result.Summary
    };
}
