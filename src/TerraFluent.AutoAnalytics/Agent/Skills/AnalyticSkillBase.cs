using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using TerraFluent.AutoAnalytics.Enums;
using TerraFluent.AutoAnalytics.Insights;
using TerraFluent.AutoAnalytics.Profiling;
using TerraFluent.AutoAnalytics.Recommendation;
using ChartType = TerraFluent.Chart.Reporting.Enums.ChartType;

namespace TerraFluent.AutoAnalytics.Agent.Skills;

/// <summary>Shared selection helpers for skills that pick insights/charts from the shared context.</summary>
public abstract class AnalyticSkillBase : IAnalyticSkill
{
    public abstract string Name { get; }
    public abstract string Description { get; }
    public abstract bool CanHandle(AnalyticGoal goal, AgentContext context);
    public abstract SkillResult Execute(AgentContext context);

    /// <summary>Selects insights of a kind, optionally restricted to the goal's target columns.</summary>
    protected static IReadOnlyList<Insight> SelectInsights(
        AgentContext ctx, InsightKind kind, IReadOnlyList<string> cols) =>
        ctx.Insights
            .Where(i => i.Kind == kind)
            .Where(i => cols.Count == 0 || i.RelatedColumns.Any(rc => Contains(cols, rc)))
            .ToList();

    /// <summary>Selects recommended charts of the given type family that reference the measures.</summary>
    protected static IReadOnlyList<RecommendedChart> SelectCharts(
        AgentContext ctx, IReadOnlyCollection<string> measures, params ChartType[] types)
    {
        var typeSet = new HashSet<ChartType>(types);
        return ctx.Recommendations
            .Where(r => typeSet.Contains(r.ChartType))
            .Where(r => measures.Count == 0 || measures.Any(m => ChartReferences(r, m)))
            .ToList();
    }

    /// <summary>The distinct measure columns referenced by a set of insights.</summary>
    protected static IReadOnlyList<string> MeasuresOf(IEnumerable<Insight> insights) =>
        insights.SelectMany(i => i.RelatedColumns).Distinct(StringComparer.OrdinalIgnoreCase).ToList();

    /// <summary>Compact, human-readable number formatting (k/M/B, rounded, no scientific notation).</summary>
    protected static string FormatNumber(double value)
    {
        double abs = Math.Abs(value);
        return abs switch
        {
            >= 1_000_000_000 => (value / 1_000_000_000).ToString("0.##", CultureInfo.InvariantCulture) + "B",
            >= 1_000_000     => (value / 1_000_000).ToString("0.##", CultureInfo.InvariantCulture) + "M",
            >= 1_000         => (value / 1_000).ToString("0.##", CultureInfo.InvariantCulture) + "k",
            _                => DisplayText.FormatNumber(value)
        };
    }

    /// <summary>Returns a measure's values (and matching labels) ordered by the primary date column.</summary>
    protected static (IReadOnlyList<string> Labels, IReadOnlyList<double> Values) OrderedSeries(
        DatasetProfile profile, ColumnStatistics measure)
    {
        var date = profile.DateColumns.FirstOrDefault();

        // Pair each value with its row's date (complete-case) and order by date, so a missing date
        // or value never plots a value against the wrong period.
        var ordered = RowAlignment.DateValues(measure, date);
        var values = ordered.Select(p => p.Value).ToList();

        if (date is null || date.Dates.Count == 0)
        {
            var idxLabels = Enumerable.Range(1, values.Count).Select(i => i.ToString(CultureInfo.InvariantCulture)).ToList();
            return (idxLabels, values);
        }

        var labels = ordered.Select(p => p.Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)).ToList();
        return (labels, values);
    }

    private static bool ChartReferences(RecommendedChart r, string measure) =>
        r.Spec.Title.IndexOf(measure, StringComparison.OrdinalIgnoreCase) >= 0
        || r.Spec.Series.Any(s => string.Equals(s.Name, measure, StringComparison.OrdinalIgnoreCase));

    private static bool Contains(IReadOnlyList<string> cols, string value) =>
        cols.Any(c => string.Equals(c, value, StringComparison.OrdinalIgnoreCase));
}
