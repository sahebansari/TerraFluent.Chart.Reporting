using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using TerraFluent.AutoAnalytics.Enums;
using TerraFluent.AutoAnalytics.Insights;
using TerraFluent.AutoAnalytics.Profiling;
using TerraFluent.AutoAnalytics.Recommendation;
using TerraFluent.AutoAnalytics.Schema;
using ChartType = TerraFluent.Chart.Reporting.Enums.ChartType;

namespace TerraFluent.AutoAnalytics.Agent.Skills;

/// <summary>
/// Wraps the relationship engine's group-by findings: explains which category concentrates a
/// measure (share/breakdown). Reached as a drill-down from the trend/anomaly skills, or directly
/// via a <see cref="GoalKind.Dominance"/> goal. Terminal skill.
/// <para>When no pre-ranked dominance insight matches the request (e.g. the combination was
/// filtered out by scoring or the group-combination cap), the skill ranks the requested measure
/// by the requested dimension on demand, so a "which X has the highest Y" question is always
/// answered.</para>
/// </summary>
public sealed class DominanceSkill : AnalyticSkillBase
{
    public override string Name => "Dominance";
    public override string Description => "Explains which category concentrates a measure (share and breakdown).";

    public override bool CanHandle(AnalyticGoal goal, AgentContext context) =>
        goal.Kind is GoalKind.Dominance
        && (SelectInsights(context, InsightKind.Dominance, goal.TargetColumns).Count > 0
            || CanRank(goal, context));

    public override SkillResult Execute(AgentContext ctx)
    {
        // When the question explicitly named both a measure and a dimension, answer that exact
        // pairing directly — otherwise a generic "matches any target column" selection can surface
        // a different measure (e.g. asking about salary but reporting experience).
        var namedMeasure = NamedMeasure(ctx.Goal, ctx.Profile);
        var namedDimension = NamedDimension(ctx.Goal, ctx.Profile);
        if (namedMeasure is not null && namedDimension is not null)
        {
            var direct = RankPair(namedMeasure, namedDimension);
            if (direct is not null) return direct;
        }

        // If a specific measure was named, keep only that measure's pre-ranked insights.
        var insights = SelectInsights(ctx, InsightKind.Dominance, ctx.Goal.TargetColumns);
        if (namedMeasure is not null)
            insights = insights
                .Where(i => i.RelatedColumns.Any(rc => string.Equals(rc, namedMeasure.Name, StringComparison.OrdinalIgnoreCase)))
                .ToList();

        if (insights.Count > 0)
        {
            var charts = SelectCharts(ctx, MeasuresOf(insights),
                ChartType.Bar, ChartType.Column, ChartType.Pie);

            string measures = ctx.Goal.TargetColumns.Count > 0
                ? string.Join(", ", ctx.Goal.TargetColumns)
                : "the measures";
            string rationale = $"Broke down {measures} by category to locate concentration.";
            return new SkillResult(rationale, insights, charts);
        }

        // Fallback: rank the requested (or most relevant) measure by the requested dimension directly.
        return RankOnDemand(ctx) ?? SkillResult.Empty("No concentration pattern for the requested measures.");
    }

    private static bool CanRank(AnalyticGoal goal, AgentContext ctx) =>
        ResolveMeasure(goal, ctx.Profile) is not null && ResolveDimension(goal, ctx.Profile) is not null;

    private static SkillResult? RankOnDemand(AgentContext ctx)
    {
        var measure = ResolveMeasure(ctx.Goal, ctx.Profile);
        var dimension = ResolveDimension(ctx.Goal, ctx.Profile);
        return measure is null || dimension is null ? null : RankPair(measure, dimension);
    }

    private static SkillResult? RankPair(ColumnStatistics measure, ColumnStatistics dimension)
    {
        var labels = dimension.Labels;
        var values = measure.NumericValues;
        int n = Math.Min(labels.Count, values.Count);
        if (n == 0) return null;

        var groups = new Dictionary<string, (double Sum, int Count)>(StringComparer.OrdinalIgnoreCase);
        for (int i = 0; i < n; i++)
        {
            groups.TryGetValue(labels[i], out var acc);
            groups[labels[i]] = (acc.Sum + values[i], acc.Count + 1);
        }
        if (groups.Count < 2) return null;

        // Additive measures rank by total; per-row attributes (age, tenure) rank by average.
        bool additive = MeasureSemantics.IsAdditive(measure.Profile, measure.Numeric?.Min, measure.Numeric?.Max);
        string agg = additive ? "total" : "average";

        var ranked = groups
            .Select(kv => (Key: kv.Key, Value: additive ? kv.Value.Sum : kv.Value.Sum / kv.Value.Count))
            .OrderByDescending(g => g.Value)
            .ToList();

        var top = ranked[0];
        double grand = ranked.Sum(g => g.Value);
        string detail = additive && grand > 0
            ? $"{FormatNumber(top.Value)} ({top.Value / grand:P0} of the total)"
            : FormatNumber(top.Value);
        string measureDisp = DisplayText.Humanize(measure.Name);
        string dimDisp     = DisplayText.Humanize(dimension.Name);

        var insight = new Insight
        {
            Kind = InsightKind.Dominance,
            Title = $"{top.Key} has the highest {agg} {measureDisp}",
            Description = $"Across {ranked.Count} '{dimDisp}' groups, {top.Key} has the highest " +
                          $"{agg} {measureDisp}: {detail}.",
            ImportanceScore = 62,
            RelatedColumns = new[] { dimension.Name, measure.Name },
            Evidence = new Dictionary<string, string>
            {
                ["dimension"] = dimension.Name,
                ["measure"] = measure.Name,
                ["aggregation"] = agg,
                ["topCategory"] = top.Key,
                ["topValue"] = top.Value.ToString("F2", CultureInfo.InvariantCulture),
                ["groups"] = ranked.Count.ToString(CultureInfo.InvariantCulture)
            }
        };

        var top12 = ranked.Take(12).ToList();
        string aggTitle = char.ToUpperInvariant(agg[0]) + agg[1..];
        var chart = new RecommendedChart
        {
            ChartType = ChartType.Bar,
            SuitabilityScore = 80,
            Reason = $"Ranks '{dimDisp}' by {agg} {measureDisp} so the leader is obvious.",
            Spec = new ChartSpec
            {
                Type = ChartType.Bar,
                Title = $"{aggTitle} {measureDisp} by {dimDisp}",
                XAxisTitle = dimDisp,
                YAxisTitle = $"{aggTitle} {measureDisp}",
                Categories = top12.Select(g => g.Key).ToList(),
                Series = new[] { new SeriesSpec { Name = measureDisp, Values = top12.Select(g => (double?)g.Value).ToList() } }
            }
        };

        string rationale = $"Ranked {agg} {measureDisp} by {dimDisp} to identify the leader.";
        return new SkillResult(rationale, new[] { insight }, new[] { chart });
    }

    // Picks the measure the question named, else the most business-relevant measure.
    private static ColumnStatistics? ResolveMeasure(AnalyticGoal goal, DatasetProfile profile) =>
        NamedMeasure(goal, profile) ?? profile.Measures
            .Where(m => m.NumericValues.Count > 0)
            .OrderByDescending(m => RolePriority(m.Profile.Role))
            .FirstOrDefault();

    // Picks the dimension the question named (allowing an explicitly-requested text column), else
    // the lowest-cardinality categorical dimension.
    private static ColumnStatistics? ResolveDimension(AnalyticGoal goal, DatasetProfile profile) =>
        NamedDimension(goal, profile) ?? profile.Categories
            .Where(c => c.Categorical is { DistinctCount: >= 2 and <= 50 })
            .OrderBy(c => c.Categorical!.DistinctCount)
            .FirstOrDefault();

    // A measure explicitly named in the question, or null.
    private static ColumnStatistics? NamedMeasure(AnalyticGoal goal, DatasetProfile profile)
    {
        foreach (var col in goal.TargetColumns)
        {
            var stat = profile.ByName(col);
            if (stat is not null && stat.Profile.IsMeasure && stat.NumericValues.Count > 0)
                return stat;
        }
        return null;
    }

    // A groupable dimension explicitly named in the question, or null.
    private static ColumnStatistics? NamedDimension(AnalyticGoal goal, DatasetProfile profile)
    {
        foreach (var col in goal.TargetColumns)
        {
            var stat = profile.ByName(col);
            if (stat is not null && IsGroupable(stat, allowText: true))
                return stat;
        }
        return null;
    }

    private static bool IsGroupable(ColumnStatistics stat, bool allowText)
    {
        int distinct = stat.Categorical?.DistinctCount ?? 0;
        if (distinct is < 2 or > 50) return false;
        return stat.Profile.Type is ColumnType.Category or ColumnType.Boolean
            || (allowText && stat.Profile.Type == ColumnType.Text);
    }

    private static int RolePriority(SemanticRole role) => role switch
    {
        SemanticRole.RevenueMetric => 5,
        SemanticRole.ProfitMetric => 4,
        SemanticRole.CostMetric => 3,
        SemanticRole.QuantityMetric => 2,
        _ => 1
    };
}
