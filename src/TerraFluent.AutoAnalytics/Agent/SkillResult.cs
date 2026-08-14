using System.Collections.Generic;
using System.Linq;
using TerraFluent.AutoAnalytics.Insights;
using TerraFluent.AutoAnalytics.Recommendation;

namespace TerraFluent.AutoAnalytics.Agent;

/// <summary>
/// The output of one <see cref="IAnalyticSkill"/> execution: the insights and charts it surfaced,
/// a one-line rationale for the trace, and any follow-up goals the agent should pursue next.
/// </summary>
public sealed class SkillResult
{
    public IReadOnlyList<Insight> Insights { get; init; } = new List<Insight>();
    public IReadOnlyList<RecommendedChart> Charts { get; init; } = new List<RecommendedChart>();

    /// <summary>Plain-English explanation of what the skill examined and found.</summary>
    public string Rationale { get; init; } = string.Empty;

    /// <summary>Additional goals the agent should investigate as a consequence of this step.</summary>
    public IReadOnlyList<AnalyticGoal> FollowUps { get; init; } = new List<AnalyticGoal>();

    /// <summary>True when the skill produced no insights (it is skipped in the trace).</summary>
    public bool IsEmpty => Insights.Count == 0;

    public static SkillResult Empty(string rationale) => new() { Rationale = rationale };

    public SkillResult(string rationale,
        IReadOnlyList<Insight>? insights = null,
        IReadOnlyList<RecommendedChart>? charts = null,
        IReadOnlyList<AnalyticGoal>? followUps = null)
    {
        Rationale = rationale;
        Insights = insights ?? new List<Insight>();
        Charts = charts ?? new List<RecommendedChart>();
        FollowUps = followUps ?? new List<AnalyticGoal>();
    }

    public SkillResult() { }
}
