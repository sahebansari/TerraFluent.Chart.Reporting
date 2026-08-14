using System.Collections.Generic;
using TerraFluent.AutoAnalytics.Insights;

namespace TerraFluent.AutoAnalytics.Agent;

/// <summary>One executed step in an <see cref="InvestigationTrace"/> — which skill ran, for which
/// goal, why, and what it surfaced. Together the steps form the agent's explainable reasoning path.</summary>
public sealed class InvestigationStep
{
    public required string SkillName { get; init; }

    /// <summary>The goal this step pursued.</summary>
    public required string Goal { get; init; }

    /// <summary>Why the agent ran this step (the goal's trigger, when it was a follow-up).</summary>
    public string? Trigger { get; init; }

    /// <summary>The skill's plain-English explanation of what it examined and found.</summary>
    public required string Rationale { get; init; }

    /// <summary>Insights surfaced by this step.</summary>
    public IReadOnlyList<Insight> Insights { get; init; } = new List<Insight>();
}
