using System.Collections.Generic;
using System.Linq;
using TerraFluent.AutoAnalytics.Enums;
using ChartType = TerraFluent.Chart.Reporting.Enums.ChartType;

namespace TerraFluent.AutoAnalytics.Agent.Skills;

/// <summary>
/// Wraps the anomaly engine's findings: surfaces outliers/spikes in measures. Each measure with
/// anomalies raises a follow-up <see cref="GoalKind.Dominance"/> goal — a lightweight root-cause
/// step that asks which category the anomalous measure concentrates in.
/// </summary>
public sealed class AnomalySkill : AnalyticSkillBase
{
    public override string Name => "Anomaly";
    public override string Description => "Detects outliers and spikes in measures and drills into their likely source.";

    public override bool CanHandle(AnalyticGoal goal, AgentContext context) =>
        (goal.Kind is GoalKind.Explore or GoalKind.Anomaly)
        && SelectInsights(context, InsightKind.Anomaly, goal.TargetColumns).Count > 0;

    public override SkillResult Execute(AgentContext ctx)
    {
        var insights = SelectInsights(ctx, InsightKind.Anomaly, ctx.Goal.TargetColumns);
        if (insights.Count == 0)
            return SkillResult.Empty("No anomalies for the requested measures.");

        var measures = MeasuresOf(insights);
        var charts = SelectCharts(ctx, measures, ChartType.Line, ChartType.Column);

        var followUps = new List<AnalyticGoal>();
        bool hasDate = ctx.Profile.DateColumns.Any();
        foreach (var measure in measures)
        {
            if (ctx.Findings.Groups.Any(g => g.Measure == measure))
            {
                followUps.Add(AnalyticGoal.Dominance(measure)
                    .AsFollowUp($"{DisplayText.Humanize(measure)} contains anomalies \u2014 checking which segment concentrates it."));

                if (hasDate)
                    followUps.Add(AnalyticGoal.RootCause(measure)
                        .AsFollowUp($"{DisplayText.Humanize(measure)} contains anomalies \u2014 explaining what drove the change."));
            }
        }

        string rationale = $"Flagged anomalies in {measures.Count} measure(s); " +
                           "raised root-cause drill-downs.";
        return new SkillResult(rationale, insights, charts, followUps);
    }
}
