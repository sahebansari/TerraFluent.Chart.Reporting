using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using TerraFluent.AutoAnalytics.Engine;
using TerraFluent.AutoAnalytics.Enums;
using TerraFluent.AutoAnalytics.Profiling;
using TerraFluent.AutoAnalytics.Recommendation;
using TerraFluent.AutoAnalytics.Schema;
using ChartType = TerraFluent.Chart.Reporting.Enums.ChartType;

namespace TerraFluent.AutoAnalytics.Dashboard;

/// <summary>
/// Assembles a coherent <see cref="DashboardDefinition"/> from an <see cref="AnalyticsResult"/>:
/// executive KPIs from measure totals, and chart sections drawn from the ranked recommendations.
/// </summary>
public static class DashboardBuilder
{
    /// <summary>Builds a dashboard from a completed analysis.</summary>
    public static DashboardDefinition Generate(AnalyticsResult result, int maxKpis = 4)
    {
        if (result is null) throw new ArgumentNullException(nameof(result));

        var kpis = BuildKpis(result.Profile, maxKpis);
        var recs = result.Recommendations;

        var trend = recs.Where(r => r.ChartType is ChartType.Line or ChartType.Spline or ChartType.Area).ToList();
        var comparison = recs.Where(r => r.ChartType is ChartType.Bar or ChartType.Column or ChartType.Pie).ToList();
        var distribution = recs.Where(r => r.ChartType == ChartType.Column && r.Spec.Title.StartsWith("Distribution", StringComparison.OrdinalIgnoreCase)).ToList();
        // Keep distribution charts out of the comparison bucket.
        comparison = comparison.Except(distribution).ToList();

        var anomalies = result.Insights.Where(i => i.Kind == InsightKind.Anomaly).ToList();

        return new DashboardDefinition
        {
            Title = $"{result.Summary.DatasetName} — Smart Dashboard",
            Kpis = kpis,
            TrendCharts = trend,
            ComparisonCharts = comparison,
            DistributionCharts = distribution,
            Anomalies = anomalies,
            KeyInsights = result.Insights.Take(6).ToList()
        };
    }

    private static List<KpiCard> BuildKpis(DatasetProfile profile, int maxKpis)
    {
        var cards = new List<KpiCard>();
        // Prefer revenue/profit/cost measures, then any measure, ranked by magnitude.
        var ranked = profile.Measures
            .Where(m => m.Numeric is not null)
            .OrderByDescending(m => RolePriority(m.Profile.Role))
            .ThenByDescending(m => Math.Abs(m.Numeric!.Sum))
            .Take(maxKpis);

        foreach (var m in ranked)
        {
            var n = m.Numeric!;
            bool isCurrency = m.Profile.Type == ColumnType.Currency || m.Profile.Role is SemanticRole.RevenueMetric or SemanticRole.CostMetric or SemanticRole.ProfitMetric;
            bool isPercent = m.Profile.Type == ColumnType.Percentage;

            // Per-entity attributes (age, tenure, ratings, …) are not additive — summing them is
            // meaningless, so surface their average instead of a total.
            bool useAverage = isPercent || !MeasureSemantics.IsAdditive(m.Profile, n.Min, n.Max);

            double headline = useAverage ? n.Mean : n.Sum;
            string mDisp = DisplayText.Humanize(m.Name);
            cards.Add(new KpiCard
            {
                Label = mDisp,
                RawValue = headline,
                DisplayValue = Format(headline, isCurrency, isPercent),
                Caption = $"{(useAverage ? "average" : "total")} {mDisp}"
            });
        }
        return cards;
    }

    private static int RolePriority(SemanticRole role) => role switch
    {
        SemanticRole.RevenueMetric => 5,
        SemanticRole.ProfitMetric => 4,
        SemanticRole.CostMetric => 3,
        SemanticRole.QuantityMetric => 2,
        _ => 1
    };

    private static string Format(double value, bool currency, bool percent)
    {
        if (percent) return value.ToString("0.#", CultureInfo.InvariantCulture) + "%";
        string abbreviated = Abbreviate(value);
        return currency ? "$" + abbreviated : abbreviated;
    }

    private static string Abbreviate(double value)
    {
        double abs = Math.Abs(value);
        return abs switch
        {
            >= 1_000_000_000 => (value / 1_000_000_000).ToString("0.##", CultureInfo.InvariantCulture) + "B",
            >= 1_000_000 => (value / 1_000_000).ToString("0.##", CultureInfo.InvariantCulture) + "M",
            >= 1_000 => (value / 1_000).ToString("0.##", CultureInfo.InvariantCulture) + "k",
            _ => DisplayText.FormatNumber(value)
        };
    }
}
