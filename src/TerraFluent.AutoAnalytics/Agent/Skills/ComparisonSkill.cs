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
/// Compares a measure across calendar periods (month/quarter/year over period, plus year-over-year).
/// Reached as a follow-up from the trend skill or via an explicit <see cref="GoalKind.Compare"/>
/// question ("how does revenue compare to last month?"). Produces new math.
/// </summary>
public sealed class ComparisonSkill : AnalyticSkillBase
{
    private const int MaxMeasures = 3;

    private readonly PeriodComparisonEngine _engine = new();

    public override string Name => "Comparison";
    public override string Description => "Compares a measure across calendar periods (MoM/QoQ/YoY).";

    public override bool CanHandle(AnalyticGoal goal, AgentContext context) =>
        goal.Kind is GoalKind.Compare && context.Profile.DateColumns.Any();

    public override SkillResult Execute(AgentContext ctx)
    {
        var measures = ctx.Profile.Measures.Select(m => m.Name);
        if (ctx.Goal.TargetColumns.Count > 0)
            measures = measures.Where(m => ctx.Goal.TargetColumns.Any(t => string.Equals(t, m, StringComparison.OrdinalIgnoreCase)));

        var insights = new List<Insight>();
        var charts = new List<RecommendedChart>();

        foreach (var measure in measures.Take(MaxMeasures))
        {
            var comparison = _engine.Compare(ctx.Profile, measure);
            if (comparison is null || comparison.IsEmpty) continue;

            insights.Add(BuildInsight(comparison));
            charts.Add(BuildChart(comparison));
        }

        if (insights.Count == 0)
            return SkillResult.Empty("Not enough dated periods to compare the requested measures.");

        string rationale = $"Compared {insights.Count} measure(s) across calendar periods.";
        return new SkillResult(rationale, insights, charts);
    }

    private static Insight BuildInsight(PeriodComparisonResult c)
    {
        string unit = PeriodWord(c.Granularity);
        var last = c.Periods[^1];
        var prev = c.Periods[^2];
        string dir = c.LatestChangePct >= 0 ? "up" : "down";
        string pct = Math.Abs(c.LatestChangePct).ToString("P0", CultureInfo.InvariantCulture);

        string yoy = c.YearOverYearPct is double y
            ? $" Year-over-year it is {(y >= 0 ? "up" : "down")} {Math.Abs(y).ToString("P0", CultureInfo.InvariantCulture)}."
            : string.Empty;

        int score = (int)Math.Min(80, 42 + Math.Abs(c.LatestChangePct) * 80);
        string measureDisp = DisplayText.Humanize(c.Measure);

        return new Insight
        {
            Kind = InsightKind.Observation,
            Title = $"{measureDisp} is {dir} {pct} {unit}-over-{unit}",
            Description = $"{measureDisp} moved from {FormatNumber(prev.Value)} ({prev.Label}) to " +
                          $"{FormatNumber(last.Value)} ({last.Label}), {dir} {pct} versus the previous {unit}.{yoy}",
            ImportanceScore = score,
            RelatedColumns = new[] { c.Measure },
            Evidence = new Dictionary<string, string>
            {
                ["granularity"] = c.Granularity.ToString(),
                ["latestChangePct"] = c.LatestChangePct.ToString("F3", CultureInfo.InvariantCulture),
                ["latestChangeAbs"] = c.LatestChangeAbs.ToString("F2", CultureInfo.InvariantCulture),
                ["yearOverYearPct"] = c.YearOverYearPct?.ToString("F3", CultureInfo.InvariantCulture) ?? "n/a",
                ["periods"] = c.Periods.Count.ToString(CultureInfo.InvariantCulture)
            }
        };
    }

    private static RecommendedChart BuildChart(PeriodComparisonResult c)
    {
        string measureDisp = DisplayText.Humanize(c.Measure);
        var spec = new ChartSpec
        {
            Type = ChartType.Column,
            Title = $"{measureDisp} by {PeriodWord(c.Granularity)}",
            XAxisTitle = "Period",
            YAxisTitle = measureDisp,
            Categories = c.Periods.Select(p => p.Label).ToList(),
            Series = new[]
            {
                new SeriesSpec { Name = measureDisp, Values = c.Periods.Select(p => (double?)p.Value).ToList() }
            }
        };

        return new RecommendedChart
        {
            ChartType = ChartType.Column,
            SuitabilityScore = 76,
            Reason = $"{measureDisp} totalled by {PeriodWord(c.Granularity)} for period-over-period comparison.",
            Spec = spec
        };
    }

    private static string PeriodWord(DateGranularity g) => g switch
    {
        DateGranularity.Yearly    => "year",
        DateGranularity.Quarterly => "quarter",
        _                         => "month"
    };
}
