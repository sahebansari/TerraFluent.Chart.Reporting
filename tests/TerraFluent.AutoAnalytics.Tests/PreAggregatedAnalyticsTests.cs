using System.Globalization;
using System.Linq;
using System.Text;
using TerraFluent.AutoAnalytics.Dashboard;
using TerraFluent.AutoAnalytics.Engine;
using Xunit;
using ChartType = TerraFluent.Chart.Reporting.Enums.ChartType;

namespace TerraFluent.AutoAnalytics.Tests;

/// <summary>
/// Covers <see cref="AnalyticsOptions.PreAggregated"/> — the mode for already-summarized data
/// (one row per group, no raw granularity). It should break every measure down by each dimension
/// regardless of the group-combination cap, and skip the time-series engines.
/// </summary>
public class PreAggregatedAnalyticsTests
{
    // One row per business unit, six measures, no date column — the shape a summary export produces.
    private static string BusinessUnitSummaryCsv()
    {
        var sb = new StringBuilder("Unit,Accounts,Users,Cases,Assets,Products,Notes\n");
        for (int i = 1; i <= 6; i++)
            sb.Append("U").Append(i.ToString(CultureInfo.InvariantCulture)).Append(',')
              .Append(10 * i).Append(',')
              .Append(20 * i).Append(',')
              .Append(30 * i).Append(',')
              .Append(40 * i).Append(',')
              .Append(50 * i).Append(',')
              .Append(60 * i).Append('\n');
        return sb.ToString();
    }

    // A short monthly series (one row per period) that the forecast engine can fit.
    private static string MonthlySummaryCsv()
    {
        var sb = new StringBuilder("Month,Revenue\n");
        for (int m = 0; m < 8; m++)
            sb.Append("2023-").Append((m + 1).ToString("00", CultureInfo.InvariantCulture)).Append("-01,")
              .Append((100 + m * 25).ToString(CultureInfo.InvariantCulture)).Append('\n');
        return sb.ToString();
    }

    [Fact]
    public void PreAggregated_BreaksDownEveryMeasure_EvenUnderALowCap()
    {
        string csv = BusinessUnitSummaryCsv();

        var capped = AnalyticsEngine.AnalyzeCsv(csv, new AnalyticsOptions { MaxGroupCombinations = 2 });
        var full   = AnalyticsEngine.AnalyzeCsv(csv, new AnalyticsOptions { MaxGroupCombinations = 2, PreAggregated = true });

        int cappedMeasures = capped.Findings.Groups.Select(g => g.Measure).Distinct().Count();
        int fullMeasures   = full.Findings.Groups.Select(g => g.Measure).Distinct().Count();

        // The cap limits the default run; PreAggregated widens the budget to cover all six measures.
        Assert.Equal(2, cappedMeasures);
        Assert.Equal(6, fullMeasures);
    }

    [Fact]
    public void PreAggregated_SkipsTimeSeriesEngines()
    {
        string csv = MonthlySummaryCsv();

        var normal = AnalyticsEngine.AnalyzeCsv(csv, new AnalyticsOptions());
        var pre    = AnalyticsEngine.AnalyzeCsv(csv, new AnalyticsOptions { PreAggregated = true });

        // The default pipeline projects the series; PreAggregated treats each row as final.
        Assert.NotEmpty(normal.Findings.Forecasts);
        Assert.Empty(pre.Findings.Forecasts);
        Assert.Empty(pre.Findings.PeriodComparisons);
        Assert.Empty(pre.Findings.MovingAverages);
        Assert.Empty(pre.Findings.CumulativeSeries);
    }

    // One entity per row with a single extreme outlier — the shape that produced degenerate,
    // skew-dominated correlation lines and single-spike histograms.
    private static string SkewedSummaryCsv()
    {
        var sb = new StringBuilder("Unit,Accounts,Users,Cases\n");
        for (int i = 1; i <= 9; i++)
            sb.Append("U").Append(i.ToString(CultureInfo.InvariantCulture)).Append(',')
              .Append(10 * i).Append(',').Append(12 * i).Append(',').Append(5 * i).Append('\n');
        sb.Append("Mega,100000,120000,50000\n");
        return sb.ToString();
    }

    [Fact]
    public void SkewedSummary_ProducesNoDegenerateRelationshipLines()
    {
        var recs = AnalyticsEngine.AnalyzeCsv(SkewedSummaryCsv()).Recommendations;

        // A surviving line chart must span several bands; the skew-collapsed 2-point lines are gone.
        Assert.All(
            recs.Where(r => r.ChartType is ChartType.Line or ChartType.Spline),
            r => Assert.True(r.Spec.Categories.Count >= 3));

        // The one-bin-swallows-everything histograms are dropped, not charted.
        Assert.DoesNotContain(recs, r =>
            r.ChartType == ChartType.Column &&
            r.Spec.Title.StartsWith("Distribution", System.StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void SkewedSummary_Dashboard_HasNoTimelessTrendSection()
    {
        var result = AnalyticsEngine.AnalyzeCsv(SkewedSummaryCsv(), new AnalyticsOptions { PreAggregated = true });
        var dashboard = DashboardBuilder.Generate(result);

        // No date column means no genuine trends; the ranked comparison bars carry the story instead.
        Assert.Empty(dashboard.TrendCharts);
        Assert.NotEmpty(dashboard.ComparisonCharts);
    }

    // 25 distinct entity labels — above the plain category threshold, so default classification reads
    // the column as free text rather than a grouping dimension.
    private static string WideEntitySummaryCsv()
    {
        var sb = new StringBuilder("Business Unit,Accounts,Users,Cases\n");
        for (int i = 1; i <= 25; i++)
            sb.Append("Unit ").Append(i.ToString(CultureInfo.InvariantCulture)).Append(',')
              .Append(10 * i).Append(',').Append(7 * i).Append(',').Append(3 * i).Append('\n');
        return sb.ToString();
    }

    [Fact]
    public void PreAggregated_PromotesHighCardinalityLabelToDimension_AndRanksByEntity()
    {
        string csv = WideEntitySummaryCsv();
        var off = AnalyticsEngine.AnalyzeCsv(csv, new AnalyticsOptions { MaxRecommendations = 40 });
        var on = AnalyticsEngine.AnalyzeCsv(csv, new AnalyticsOptions { MaxRecommendations = 40, PreAggregated = true });

        // Read as free text by default, the label yields no per-entity breakdown.
        Assert.DoesNotContain(off.Recommendations, r => r.Spec.Title.Contains("by Business Unit"));
        // PreAggregated promotes it to a grouping dimension, so each measure is ranked by entity.
        Assert.Contains(on.Recommendations, r =>
            r.ChartType == ChartType.Bar && r.Spec.Title.Contains("by Business Unit"));
    }
}
