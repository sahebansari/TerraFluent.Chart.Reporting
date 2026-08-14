using TerraFluent.AutoAnalytics.Enums;
using ChartType = TerraFluent.Chart.Reporting.Enums.ChartType;

namespace TerraFluent.AutoAnalytics.Agent.Skills;

/// <summary>
/// Wraps the relationship engine's correlation findings: surfaces measures that move together.
/// Terminal skill — it does not raise further follow-ups.
/// </summary>
public sealed class CorrelationSkill : AnalyticSkillBase
{
    public override string Name => "Correlation";
    public override string Description => "Detects measures that move together (positive or negative correlation).";

    public override bool CanHandle(AnalyticGoal goal, AgentContext context) =>
        (goal.Kind is GoalKind.Explore or GoalKind.Correlation)
        && SelectInsights(context, InsightKind.Correlation, goal.TargetColumns).Count > 0;

    public override SkillResult Execute(AgentContext ctx)
    {
        var insights = SelectInsights(ctx, InsightKind.Correlation, ctx.Goal.TargetColumns);
        if (insights.Count == 0)
            return SkillResult.Empty("No notable correlations for the requested measures.");

        var charts = SelectCharts(ctx, MeasuresOf(insights), ChartType.Scatter);
        string rationale = $"Found {insights.Count} notable correlation(s) between measures.";
        return new SkillResult(rationale, insights, charts);
    }
}
