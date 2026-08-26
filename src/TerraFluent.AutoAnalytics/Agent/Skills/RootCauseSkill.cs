using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using TerraFluent.AutoAnalytics.Analytics;
using TerraFluent.AutoAnalytics.Enums;
using TerraFluent.AutoAnalytics.Insights;
using TerraFluent.AutoAnalytics.Recommendation;
using ChartType = TerraFluent.Chart.Reporting.Enums.ChartType;

namespace TerraFluent.AutoAnalytics.Agent.Skills;

/// <summary>
/// Explains <em>why</em> a measure changed by attributing its period-over-period movement to the
/// categories of a dimension (driver analysis). Reached as a follow-up from the trend/anomaly
/// skills or via an explicit <see cref="GoalKind.RootCause"/> question.
/// </summary>
public sealed class RootCauseSkill : AnalyticSkillBase
{
    private const int MaxMeasures = 3;
    private const int MaxBars = 8;

    private readonly DriverEngine _driver = new();

    public override string Name => "RootCause";
    public override string Description => "Attributes a measure's change to the categories that drove it.";

    public override bool CanHandle(AnalyticGoal goal, AgentContext context) =>
        goal.Kind is GoalKind.RootCause
        && context.Profile.Measures.Any()
        && context.Profile.Categories.Any(c => c.Categorical is { DistinctCount: >= 2 and <= 50 });

    public override SkillResult Execute(AgentContext ctx)
    {
        var measures = NarrowToTargets(ctx.Profile.Measures.Select(m => m.Name), ctx.Goal);

        var insights = new List<Insight>();
        var charts = new List<RecommendedChart>();

        foreach (var measure in measures.Take(MaxMeasures))
        {
            var driver = _driver.Explain(ctx.Profile, measure);
            if (driver?.TopDriver is null || Math.Abs(driver.TotalChange) < 1e-9) continue;

            insights.Add(BuildInsight(driver));
            charts.Add(BuildChart(driver));
        }

        if (insights.Count == 0)
            return SkillResult.Empty("Could not attribute a meaningful change for the requested measures.");

        string rationale = $"Decomposed the change in {insights.Count} measure(s) across dimension categories.";
        return new SkillResult(rationale, insights, charts);
    }

    private static Insight BuildInsight(DriverResult d)
    {
        var top = d.TopDriver!;
        string verb = d.Increased ? "increase" : "decrease";
        string sharePct = Math.Abs(top.ShareOfChange).ToString("P0", CultureInfo.InvariantCulture);
        int score = (int)Math.Min(88, 45 + Math.Abs(top.ShareOfChange) * 45);
        string measureDisp = DisplayText.Humanize(d.Measure);
        string dimDisp     = DisplayText.Humanize(d.Dimension);

        return new Insight
        {
            Kind = InsightKind.Observation,
            Title = $"{top.Category} drove the {measureDisp} {verb}",
            Description = $"The {measureDisp} {verb} of {FormatNumber(Math.Abs(d.TotalChange))} was driven mainly by " +
                          $"{top.Category} ({dimDisp}), contributing {sharePct} of the net change " +
                          $"({(top.Delta >= 0 ? "+" : "-")}{FormatNumber(Math.Abs(top.Delta))}).",
            ImportanceScore = score,
            RelatedColumns = new[] { d.Measure, d.Dimension },
            Evidence = new Dictionary<string, string>
            {
                ["dimension"] = d.Dimension,
                ["topCategory"] = top.Category,
                ["topDelta"] = top.Delta.ToString("F2", CultureInfo.InvariantCulture),
                ["shareOfChange"] = top.ShareOfChange.ToString("F3", CultureInfo.InvariantCulture),
                ["totalChange"] = d.TotalChange.ToString("F2", CultureInfo.InvariantCulture)
            }
        };
    }

    private static RecommendedChart BuildChart(DriverResult d)
    {
        var top = d.Contributions.Take(MaxBars).ToList();
        string measureDisp = DisplayText.Humanize(d.Measure);
        string dimDisp     = DisplayText.Humanize(d.Dimension);
        var spec = new ChartSpec
        {
            Type = ChartType.Column,
            Title = $"{measureDisp} change by {dimDisp}",
            XAxisTitle = dimDisp,
            YAxisTitle = $"{measureDisp} change",
            Categories = top.Select(c => c.Category).ToList(),
            Series = new[]
            {
                new SeriesSpec { Name = $"{measureDisp} change", Values = top.Select(c => (double?)c.Delta).ToList() }
            }
        };

        return new RecommendedChart
        {
            ChartType = ChartType.Column,
            SuitabilityScore = 78,
            Reason = $"Contribution of each {dimDisp} to the change in {measureDisp}.",
            Spec = spec
        };
    }
}
