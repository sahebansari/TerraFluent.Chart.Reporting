using TerraFluent.AutoAnalytics.Agent;
using TerraFluent.AutoAnalytics.Recommendation;

namespace TerraFluent.Chart.Reporting.Api.Models;

/// <summary>The agent's answer to a question: its reasoning steps, ranked insights and charts.</summary>
public sealed record AskResponse
{
    public required string Goal { get; init; }
    public required string Headline { get; init; }
    public required string Narrative { get; init; }
    public required IReadOnlyList<InvestigationStepDto> Steps { get; init; }
    public required IReadOnlyList<InsightDto> Insights { get; init; }
    public required IReadOnlyList<RecommendationDto> Charts { get; init; }

    /// <summary>Maps an <see cref="InvestigationTrace"/> to the API response DTO.</summary>
    public static AskResponse From(InvestigationTrace trace, bool includeSvg = false, ChartStyle style = default) => new()
    {
        Goal = trace.Goal,
        Headline = trace.Headline,
        Narrative = trace.Narrative,
        Steps = trace.Steps.Select(InvestigationStepDto.From).ToList(),
        Insights = trace.Insights.Select(InsightDto.From).ToList(),
        Charts = trace.Charts.Select(c => RecommendationDto.From(c, includeSvg, style)).ToList()
    };
}

/// <summary>One step in the agent's reasoning path.</summary>
public sealed record InvestigationStepDto
{
    public required string Skill { get; init; }
    public required string Goal { get; init; }
    public string? Trigger { get; init; }
    public required string Rationale { get; init; }
    public required IReadOnlyList<InsightDto> Insights { get; init; }

    public static InvestigationStepDto From(InvestigationStep s) => new()
    {
        Skill = s.SkillName,
        Goal = s.Goal,
        Trigger = s.Trigger,
        Rationale = s.Rationale,
        Insights = s.Insights.Select(InsightDto.From).ToList()
    };
}
