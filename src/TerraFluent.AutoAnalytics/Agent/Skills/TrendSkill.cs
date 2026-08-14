using System.Collections.Generic;
using System.Linq;
using TerraFluent.AutoAnalytics.Enums;
using ChartType = TerraFluent.Chart.Reporting.Enums.ChartType;

namespace TerraFluent.AutoAnalytics.Agent.Skills;

/// <summary>
/// Wraps the trend engine's findings: surfaces directional/volatile/seasonal movement of measures
/// over time. When a measure shows a strong directional trend and a grouping dimension exists, it
/// raises a follow-up <see cref="GoalKind.Dominance"/> goal to explain which segment drives it.
/// </summary>
public sealed class TrendSkill : AnalyticSkillBase
{
    public override string Name => "Trend";
    public override string Description => "Detects growth, decline, volatility and seasonality of measures over time.";

    public override bool CanHandle(AnalyticGoal goal, AgentContext context) =>
        (goal.Kind is GoalKind.Explore or GoalKind.Trend)
        && SelectInsights(context, InsightKind.Trend, goal.TargetColumns).Count > 0;

    public override SkillResult Execute(AgentContext ctx)
    {
        var insights = SelectInsights(ctx, InsightKind.Trend, ctx.Goal.TargetColumns);
        if (insights.Count == 0)
            return SkillResult.Empty("No notable trends for the requested measures.");

        var measures = MeasuresOf(insights);
        var charts = SelectCharts(ctx, measures, ChartType.Line, ChartType.Spline, ChartType.Area);

        // Drill into strong directional trends: project them forward and explain what drives them.
        var followUps = new List<AnalyticGoal>();
        bool hasDate = ctx.Profile.DateColumns.Any();
        bool hasDimension = ctx.Findings.Groups.Any();
        foreach (var t in ctx.Findings.Trends.Where(t => t.Kind is TrendKind.Rising or TrendKind.Declining))
        {
            if (!measures.Contains(t.Measure)) continue;

            followUps.Add(AnalyticGoal.Forecast(t.Measure)
                .AsFollowUp($"{DisplayText.Humanize(t.Measure)} shows a strong trend \u2014 projecting it forward."));

            if (hasDate)
                followUps.Add(AnalyticGoal.Compare(t.Measure)
                    .AsFollowUp($"{DisplayText.Humanize(t.Measure)} shows a strong trend \u2014 comparing recent periods."));

            if (hasDate && hasDimension)
                followUps.Add(AnalyticGoal.RootCause(t.Measure)
                    .AsFollowUp($"{DisplayText.Humanize(t.Measure)} shows a strong trend \u2014 explaining which segment drives it."));
        }

        string rationale = $"Examined {measures.Count} measure(s) for trends; " +
                           $"found {insights.Count} notable movement(s).";
        return new SkillResult(rationale, insights, charts, followUps);
    }
}
