using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using TerraFluent.AutoAnalytics.Enums;
using TerraFluent.AutoAnalytics.Insights;
using TerraFluent.AutoAnalytics.Recommendation;
using TerraFluent.AutoAnalytics.Statistics;
using ChartType = TerraFluent.Chart.Reporting.Enums.ChartType;

namespace TerraFluent.AutoAnalytics.Agent.Skills;

/// <summary>
/// Projects a measure's future trajectory using Holt's linear method. Reached as a follow-up from
/// the trend skill or via an explicit <see cref="GoalKind.Forecast"/> question. Produces new math
/// (a forecast), unlike the selection-only skills.
/// </summary>
public sealed class ForecastSkill : AnalyticSkillBase
{
    private const int Horizon = 3;
    private const int MaxMeasures = 3;

    public override string Name => "Forecast";
    public override string Description => "Projects a measure forward with confidence bands (Holt's linear method).";

    public override bool CanHandle(AnalyticGoal goal, AgentContext context) =>
        goal.Kind is GoalKind.Forecast
        && context.Profile.Measures.Any(m => m.NumericValues.Count >= 4);

    public override SkillResult Execute(AgentContext ctx)
    {
        var measures = ctx.Profile.Measures.Where(m => m.NumericValues.Count >= 4);
        if (ctx.Goal.TargetColumns.Count > 0)
            measures = measures.Where(m => ctx.Goal.TargetColumns.Any(t => string.Equals(t, m.Name, StringComparison.OrdinalIgnoreCase)));

        var insights = new List<Insight>();
        var charts = new List<RecommendedChart>();

        // Calendar-driven seasonal hint (null → auto-detect via autocorrelation).
        int? seasonHint = SeasonHint(ctx.Profile);

        foreach (var measure in measures.Take(MaxMeasures))
        {
            var (labels, values) = OrderedSeries(ctx.Profile, measure);
            var forecast = Forecasting.Forecast(values, Horizon, seasonHint);
            if (forecast.IsEmpty) continue;

            insights.Add(BuildInsight(measure.Name, forecast));
            charts.Add(BuildChart(measure.Name, labels, values, forecast));
        }

        if (insights.Count == 0)
            return SkillResult.Empty("Not enough ordered data points to forecast the requested measures.");

        string rationale = $"Projected {insights.Count} measure(s) forward {Horizon} period(s) with Holt's method.";
        return new SkillResult(rationale, insights, charts);
    }

    // Maps the primary date column's cadence to a seasonal period; null when unknown (auto-detect).
    private static int? SeasonHint(Profiling.DatasetProfile profile)
    {
        var granularity = profile.DateColumns.FirstOrDefault()?.Date?.Granularity;
        return granularity switch
        {
            DateGranularity.Monthly   => 12,
            DateGranularity.Quarterly => 4,
            DateGranularity.Weekly    => 7,
            DateGranularity.Daily     => 7,
            _                         => (int?)null
        };
    }

    private static Insight BuildInsight(string measure, ForecastResult f)
    {
        double finalValue = f.Points[^1].Value;
        string direction = f.ProjectedChange >= 0 ? "rise" : "fall";
        string pct = Math.Abs(f.ProjectedChange).ToString("P0", CultureInfo.InvariantCulture);
        int score = (int)Math.Min(85, 45 + Math.Abs(f.ProjectedChange) * 80);
        string measureDisp = DisplayText.Humanize(measure);

        return new Insight
        {
            Kind = InsightKind.Observation,
            Title = $"{measureDisp} is projected to {direction} {pct}",
            Description = $"Projecting {measureDisp} forward {f.Points.Count} period(s) gives about " +
                          $"{FormatNumber(finalValue)} by the final period " +
                          $"({(f.ProjectedChange >= 0 ? "+" : "-")}{pct} vs the latest {FormatNumber(f.LastActual)}), " +
                          $"within a range of {FormatNumber(f.Points[^1].Lower)}–{FormatNumber(f.Points[^1].Upper)}.",
            ImportanceScore = score,
            RelatedColumns = new[] { measure },
            Evidence = new Dictionary<string, string>
            {
                ["projectedChange"] = f.ProjectedChange.ToString("F3", CultureInfo.InvariantCulture),
                ["finalValue"] = finalValue.ToString("F2", CultureInfo.InvariantCulture),
                ["lower"] = f.Points[^1].Lower.ToString("F2", CultureInfo.InvariantCulture),
                ["upper"] = f.Points[^1].Upper.ToString("F2", CultureInfo.InvariantCulture),
                ["horizon"] = f.Points.Count.ToString(CultureInfo.InvariantCulture)
            }
        };
    }

    private static RecommendedChart BuildChart(
        string measure, IReadOnlyList<string> labels, IReadOnlyList<double> values, ForecastResult f)
    {
        int h = f.Points.Count;
        int history = values.Count;

        var categories = new List<string>(labels);
        for (int i = 1; i <= h; i++) categories.Add("+" + i.ToString(CultureInfo.InvariantCulture));

        // Actual: history values then gaps; Forecast: gaps then an anchor at the last actual + projections.
        var actual = values.Select(v => (double?)v).Concat(Enumerable.Repeat((double?)null, h)).ToList();
        var projected = new List<double?>(new double?[history - 1]);
        projected.Add(f.LastActual);
        projected.AddRange(f.Points.Select(p => (double?)p.Value));

        string measureDisp = DisplayText.Humanize(measure);
        var spec = new ChartSpec
        {
            Type = ChartType.Line,
            Title = $"{measureDisp} forecast",
            XAxisTitle = "Period",
            YAxisTitle = measureDisp,
            Categories = categories,
            Series = new[]
            {
                new SeriesSpec { Name = measureDisp, Values = actual },
                new SeriesSpec { Name = $"{measureDisp} (forecast)", Values = projected }
            }
        };

        return new RecommendedChart
        {
            ChartType = ChartType.Line,
            SuitabilityScore = 80,
            Reason = $"Forecast of {measureDisp} over the next {h} period(s) with Holt's linear method.",
            Spec = spec
        };
    }
}
