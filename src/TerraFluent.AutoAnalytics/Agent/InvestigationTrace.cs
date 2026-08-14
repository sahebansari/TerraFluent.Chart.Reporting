using System.Collections.Generic;
using TerraFluent.AutoAnalytics.Insights;
using TerraFluent.AutoAnalytics.Recommendation;

namespace TerraFluent.AutoAnalytics.Agent;

/// <summary>
/// The complete result of an <see cref="AnalyticAgent"/> investigation: the ordered reasoning
/// <see cref="Steps"/>, the de-duplicated ranked <see cref="Insights"/>, the supporting
/// <see cref="Charts"/>, and a headline/narrative summary.
/// </summary>
public sealed class InvestigationTrace
{
    /// <summary>The root goal that started the investigation.</summary>
    public required string Goal { get; init; }

    /// <summary>The ordered steps the agent executed (its reasoning path).</summary>
    public IReadOnlyList<InvestigationStep> Steps { get; init; } = new List<InvestigationStep>();

    /// <summary>All insights surfaced, de-duplicated and ranked by importance.</summary>
    public IReadOnlyList<Insight> Insights { get; init; } = new List<Insight>();

    /// <summary>Supporting charts, de-duplicated by title.</summary>
    public IReadOnlyList<RecommendedChart> Charts { get; init; } = new List<RecommendedChart>();

    /// <summary>The single most important finding, or a "nothing found" message.</summary>
    public required string Headline { get; init; }

    /// <summary>A one-line summary of the investigation (steps run, insights found).</summary>
    public required string Narrative { get; init; }
}
