using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using TerraFluent.AutoAnalytics.Comparison;
using TerraFluent.AutoAnalytics.Engine;
using TerraFluent.AutoAnalytics.Enums;
using TerraFluent.AutoAnalytics.Recommendation;
using Xunit;
using ChartType = TerraFluent.Chart.Reporting.Enums.ChartType;

namespace TerraFluent.AutoAnalytics.Tests;

/// <summary>
/// Covers the two-dataset diff: column alignment, the statistic each measure is compared on,
/// category mix shifts, and the narrative built from them.
/// </summary>
public class DatasetComparisonTests
{
    // ── Fixtures ──────────────────────────────────────────────────────────────

    /// <summary>Monthly revenue split across regions, parameterised so a "after" file can differ.</summary>
    private static string SalesCsv(
        double euRevenue, double naRevenue, int months = 6,
        double growth = 0, string[]? extraRegions = null)
    {
        var sb = new StringBuilder("Month,Region,Revenue,Satisfaction\n");
        var start = new DateTime(2024, 1, 1);
        for (int m = 0; m < months; m++)
        {
            string month = start.AddMonths(m).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            Append(sb, month, "EU", euRevenue + m * growth, 4.1);
            Append(sb, month, "NA", naRevenue + m * growth, 4.4);
            foreach (var extra in extraRegions ?? Array.Empty<string>())
                Append(sb, month, extra, 3000, 4.0);
        }
        return sb.ToString();

        static void Append(StringBuilder sb, string month, string region, double revenue, double satisfaction) =>
            sb.Append(month).Append(',').Append(region).Append(',')
              .Append(revenue.ToString(CultureInfo.InvariantCulture)).Append(',')
              .Append(satisfaction.ToString(CultureInfo.InvariantCulture)).Append('\n');
    }

    private static AnalyticsResult Analyze(string csv, string name) =>
        AnalyticsEngine.AnalyzeCsv(csv, new AnalyticsOptions { DatasetName = name });

    private static DatasetComparison Compare(string baselineCsv, string currentCsv) =>
        new DatasetComparisonEngine().Compare(Analyze(baselineCsv, "Q1"), Analyze(currentCsv, "Q2"));

    /// <summary>Weekly traffic split by channel, over an inclusive week range (mirrors the Web Traffic sample).</summary>
    private static string WeeklyCsv(int startWeek, int endWeek)
    {
        var sb = new StringBuilder("Week,Channel,Sessions,Conversions\n");
        for (int w = startWeek; w <= endWeek; w++)
        {
            string wk = string.Format(CultureInfo.InvariantCulture, "2024-W{0:00}", w);
            sb.Append(wk).Append(",Organic,").Append(4000 + w * 200).Append(',').Append(180 + w * 10).Append('\n');
            sb.Append(wk).Append(",Paid,").Append(3000 + w * 150).Append(',').Append(200 + w * 12).Append('\n');
        }
        return sb.ToString();
    }

    // ── Measures ──────────────────────────────────────────────────────────────

    [Fact]
    public void Compare_ReportsMeasureMovementBetweenTheTwoDatasets()
    {
        var comparison = Compare(SalesCsv(10_000, 20_000), SalesCsv(12_000, 24_000));

        Assert.True(comparison.IsComparable);
        var revenue = comparison.MeasureDeltas.Single(d => d.Measure == "Revenue");

        Assert.Equal(180_000, revenue.BaselineValue);   // (10k + 20k) x 6 months
        Assert.Equal(216_000, revenue.CurrentValue);
        Assert.Equal(36_000, revenue.Delta);
        Assert.Equal(0.2, revenue.DeltaPct!.Value, 3);
    }

    [Fact]
    public void Compare_UsesTotalsForAdditiveMeasuresAndAveragesForAttributes()
    {
        var comparison = Compare(SalesCsv(10_000, 20_000), SalesCsv(12_000, 24_000));

        var revenue = comparison.MeasureDeltas.Single(d => d.Measure == "Revenue");
        var satisfaction = comparison.MeasureDeltas.Single(d => d.Measure == "Satisfaction");

        Assert.True(revenue.IsAdditive);
        Assert.Equal("total", revenue.Statistic);

        // Summing a satisfaction score would be meaningless — it must be averaged.
        Assert.False(satisfaction.IsAdditive);
        Assert.Equal("average", satisfaction.Statistic);
        Assert.InRange(satisfaction.BaselineValue, 4.2, 4.3);
    }

    [Fact]
    public void Compare_OrdersMeasuresByRelativeMovement()
    {
        // Revenue moves 20%; Satisfaction does not move at all.
        var comparison = Compare(SalesCsv(10_000, 20_000), SalesCsv(12_000, 24_000));
        Assert.Equal("Revenue", comparison.MeasureDeltas[0].Measure);
    }

    [Fact]
    public void Compare_FlagsADirectionReversal()
    {
        var rising = SalesCsv(10_000, 20_000, months: 12, growth: 800);
        var falling = SalesCsv(20_000, 30_000, months: 12, growth: -800);

        var comparison = new DatasetComparisonEngine().Compare(Analyze(rising, "Up"), Analyze(falling, "Down"));
        var revenue = comparison.MeasureDeltas.Single(d => d.Measure == "Revenue");

        Assert.Equal(TrendKind.Rising, revenue.BaselineTrend);
        Assert.Equal(TrendKind.Declining, revenue.CurrentTrend);
        Assert.True(revenue.TrendReversed);
    }

    [Fact]
    public void Compare_DoesNotTreatStableAsAReversal()
    {
        var comparison = Compare(SalesCsv(10_000, 20_000), SalesCsv(10_000, 20_000));
        Assert.All(comparison.MeasureDeltas, d => Assert.False(d.TrendReversed));
    }

    // ── Category mix ──────────────────────────────────────────────────────────

    [Fact]
    public void Compare_DetectsAShiftInCategoryShare()
    {
        // EU goes from a third of revenue to half of it.
        var comparison = Compare(SalesCsv(10_000, 20_000), SalesCsv(20_000, 20_000));

        var eu = comparison.CategoryShifts
            .First(s => s.Category == "EU" && s.Measure == "Revenue");

        Assert.Equal(1.0 / 3, eu.BaselineShare, 2);
        Assert.Equal(0.5, eu.CurrentShare, 2);
        Assert.True(eu.ShareDelta > 0.15);
        Assert.False(eu.IsNew);
        Assert.False(eu.IsGone);
    }

    [Fact]
    public void Compare_ReportsCategoriesThatAppearedOrVanished()
    {
        var before = SalesCsv(10_000, 20_000);
        var after = SalesCsv(10_000, 20_000, extraRegions: new[] { "APAC" });

        var comparison = new DatasetComparisonEngine().Compare(Analyze(before, "Q1"), Analyze(after, "Q2"));

        var apac = comparison.CategoryShifts.First(s => s.Category == "APAC" && s.Measure == "Revenue");
        Assert.True(apac.IsNew);
        Assert.Equal(0, apac.BaselineShare);
        Assert.True(apac.CurrentShare > 0);
    }

    [Fact]
    public void Compare_IgnoresMixNoise()
    {
        // Identical datasets produce no share movement at all.
        var comparison = Compare(SalesCsv(10_000, 20_000), SalesCsv(10_000, 20_000));
        Assert.Empty(comparison.CategoryShifts);
    }

    [Fact]
    public void Compare_ExcludesDimensionsWithDisjointCategoriesFromMixShifts()
    {
        // Splitting a weekly series into two non-overlapping halves enumerates different weeks on
        // each side. "Week" is therefore not a shared basis for a mix comparison — every week would
        // read as new or gone, which is noise, not a real shift. The shared "Channel" mix is stable.
        var comparison = new DatasetComparisonEngine().Compare(
            Analyze(WeeklyCsv(1, 3), "Weeks 1-3"), Analyze(WeeklyCsv(4, 6), "Weeks 4-6"));

        Assert.True(comparison.IsComparable);
        Assert.DoesNotContain(comparison.CategoryShifts,
            s => string.Equals(s.Dimension, "Week", StringComparison.OrdinalIgnoreCase));
    }

    // ── Schema ────────────────────────────────────────────────────────────────

    [Fact]
    public void Compare_ReportsAddedAndRemovedColumns()
    {
        const string before = "Month,Region,Revenue\n2024-01,EU,100\n2024-02,EU,110\n2024-03,EU,120\n";
        const string after = "Month,Region,Revenue,Units\n2024-01,EU,100,5\n2024-02,EU,110,6\n2024-03,EU,120,7\n";

        var comparison = new DatasetComparisonEngine().Compare(Analyze(before, "Q1"), Analyze(after, "Q2"));

        var added = comparison.SchemaChanges.Single(c => c.Kind == SchemaChangeKind.Added);
        Assert.Equal("Units", added.Column);

        // And the reverse direction reports it as removed rather than silently dropping it.
        var reversed = new DatasetComparisonEngine().Compare(Analyze(after, "Q2"), Analyze(before, "Q1"));
        Assert.Equal("Units", reversed.SchemaChanges.Single(c => c.Kind == SchemaChangeKind.Removed).Column);
    }

    [Fact]
    public void Compare_ReportsAColumnThatChangedType()
    {
        // Grade is numeric on one side and categorical on the other. Repeated values keep it out of
        // the identifier heuristic, so the difference really is a type change.
        const string numeric = "Region,Grade,Revenue\nEU,10,10\nNA,20,20\nEU,10,30\nNA,20,40\n";
        const string text = "Region,Grade,Revenue\nEU,low,10\nNA,high,20\nEU,low,30\nNA,high,40\n";

        var comparison = new DatasetComparisonEngine().Compare(Analyze(numeric, "Before"), Analyze(text, "After"));

        var change = comparison.SchemaChanges.Single(c => c.Kind == SchemaChangeKind.TypeChanged);
        Assert.Equal("Grade", change.Column);
        Assert.NotEqual(change.BaselineType, change.CurrentType);
    }

    [Fact]
    public void Compare_MatchesColumnsCaseInsensitively()
    {
        const string before = "Month,Region,Revenue\n2024-01,EU,100\n2024-02,EU,110\n2024-03,EU,120\n";
        const string after = "MONTH,region,REVENUE\n2024-01,EU,120\n2024-02,EU,130\n2024-03,EU,140\n";

        var comparison = new DatasetComparisonEngine().Compare(Analyze(before, "Q1"), Analyze(after, "Q2"));

        Assert.True(comparison.IsComparable);
        Assert.DoesNotContain(comparison.SchemaChanges,
            c => c.Kind is SchemaChangeKind.Added or SchemaChangeKind.Removed);
    }

    // ── Incompatible pairs ────────────────────────────────────────────────────

    [Fact]
    public void Compare_SaysSoWhenTheDatasetsShareNoMeasure()
    {
        const string sales = "Month,Region,Revenue\n2024-01,EU,100\n2024-02,EU,110\n2024-03,EU,120\n";
        const string weather = "Day,City,Temperature\n2024-01-01,Oslo,-2\n2024-01-02,Oslo,-1\n2024-01-03,Oslo,0\n";

        var comparison = new DatasetComparisonEngine().Compare(Analyze(sales, "Sales"), Analyze(weather, "Weather"));

        Assert.False(comparison.IsComparable);
        Assert.Empty(comparison.MeasureDeltas);
        Assert.Empty(comparison.CategoryShifts);
        Assert.Contains("share no measure", comparison.CompatibilityNote, StringComparison.OrdinalIgnoreCase);
        Assert.NotEmpty(comparison.SchemaChanges);      // the structural diff is still useful
    }

    [Fact]
    public void Compare_RejectsNullInputs()
    {
        var engine = new DatasetComparisonEngine();
        var ok = Analyze(SalesCsv(1000, 2000), "Q1");

        Assert.Throws<ArgumentNullException>(() => engine.Compare(null!, ok));
        Assert.Throws<ArgumentNullException>(() => engine.Compare(ok, null!));
    }

    [Fact]
    public void Compare_IsDeterministic()
    {
        var first = Compare(SalesCsv(10_000, 20_000), SalesCsv(12_000, 24_000));
        var second = Compare(SalesCsv(10_000, 20_000), SalesCsv(12_000, 24_000));

        Assert.Equal(first.Headline, second.Headline);
        Assert.Equal(
            first.MeasureDeltas.Select(d => (d.Measure, d.Delta)),
            second.MeasureDeltas.Select(d => (d.Measure, d.Delta)));
    }

    // ── Narrative ─────────────────────────────────────────────────────────────

    [Fact]
    public void Headline_NamesTheLargestMove()
    {
        var comparison = Compare(SalesCsv(10_000, 20_000), SalesCsv(12_000, 24_000));

        Assert.Contains("Revenue", comparison.Headline);
        Assert.Contains("up", comparison.Headline, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("20", comparison.Headline);
    }

    [Fact]
    public void Insights_ExplainWhichStatisticWasCompared()
    {
        var comparison = Compare(SalesCsv(10_000, 20_000), SalesCsv(12_000, 24_000));
        var insights = ComparisonNarrator.Insights(comparison);

        var revenue = insights.First(i => i.Title.Contains("Revenue", StringComparison.OrdinalIgnoreCase));
        Assert.Equal(InsightKind.Comparison, revenue.Kind);
        Assert.Contains("summable", revenue.Description, StringComparison.OrdinalIgnoreCase);
        Assert.Equal("total", revenue.Evidence["statistic"]);
    }

    [Fact]
    public void Insights_WarnLoudlyAboutARetypedColumn()
    {
        // Grade is numeric on one side and categorical on the other. Repeated values keep it out of
        // the identifier heuristic, so the difference really is a type change.
        const string numeric = "Region,Grade,Revenue\nEU,10,10\nNA,20,20\nEU,10,30\nNA,20,40\n";
        const string text = "Region,Grade,Revenue\nEU,low,10\nNA,high,20\nEU,low,30\nNA,high,40\n";

        var comparison = new DatasetComparisonEngine().Compare(Analyze(numeric, "Before"), Analyze(text, "After"));
        var insight = ComparisonNarrator.Insights(comparison).First(i => i.Title.Contains("changed type"));

        // A retyped column invalidates the numbers, so it must outrank ordinary movement.
        Assert.True(insight.ImportanceScore >= 85);
        Assert.Contains("not safely comparable", insight.Description);
    }

    [Fact]
    public void Insights_CallOutARowCountChangeThatCouldExplainTotals()
    {
        var comparison = new DatasetComparisonEngine().Compare(
            Analyze(SalesCsv(10_000, 20_000, months: 6), "Short"),
            Analyze(SalesCsv(10_000, 20_000, months: 12), "Long"));

        var insight = ComparisonNarrator.Insights(comparison)
            .First(i => i.Title.Contains("more rows", StringComparison.OrdinalIgnoreCase));

        Assert.Contains("averages", insight.Description, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Insights_AreEmptyForIdenticalDatasets()
    {
        var comparison = Compare(SalesCsv(10_000, 20_000), SalesCsv(10_000, 20_000));
        Assert.Empty(ComparisonNarrator.Insights(comparison));
    }

    // ── Charts ────────────────────────────────────────────────────────────────

    [Fact]
    public void Charts_PairEachMeasureAcrossBothDatasets()
    {
        var comparison = Compare(SalesCsv(10_000, 20_000), SalesCsv(12_000, 24_000));
        var chart = ComparisonNarrator.Charts(comparison).First(c => c.ChartType == ChartType.Column);

        Assert.Equal(2, chart.Spec.Series.Count);
        Assert.Equal("Q1", chart.Spec.Series[0].Name);
        Assert.Equal("Q2", chart.Spec.Series[1].Name);
        Assert.Equal(chart.Spec.Categories.Count, chart.Spec.Series[0].Values.Count);
        // The axis label states which statistic each bar represents.
        Assert.Contains(chart.Spec.Categories, c => c.Contains("total"));
    }

    [Fact]
    public void Charts_UseADumbbellForCategoryMovement()
    {
        var comparison = Compare(SalesCsv(10_000, 20_000), SalesCsv(20_000, 20_000));
        var dumbbell = ComparisonNarrator.Charts(comparison).First(c => c.ChartType == ChartType.Dumbbell);

        var series = Assert.Single(dumbbell.Spec.Series);
        Assert.Equal(dumbbell.Spec.Categories.Count, series.RangeValues.Count);
        Assert.All(series.RangeValues, r => Assert.True(r.Low <= r.High));
    }

    [Fact]
    public void Charts_RenderToSvg()
    {
        var comparison = Compare(SalesCsv(10_000, 20_000), SalesCsv(20_000, 24_000));

        foreach (var chart in ComparisonNarrator.Charts(comparison))
        {
            string svg = ChartConfigBuilder.ToSvg(chart.Spec);
            Assert.StartsWith("<svg", svg);
            Assert.DoesNotContain("NaN", svg);
        }
    }

    [Fact]
    public void Charts_AreEmptyWhenTheDatasetsAreNotComparable()
    {
        const string sales = "Month,Region,Revenue\n2024-01,EU,100\n2024-02,EU,110\n2024-03,EU,120\n";
        const string weather = "Day,City,Temperature\n2024-01-01,Oslo,-2\n2024-01-02,Oslo,-1\n2024-01-03,Oslo,0\n";

        var comparison = new DatasetComparisonEngine().Compare(Analyze(sales, "Sales"), Analyze(weather, "Weather"));
        Assert.Empty(ComparisonNarrator.Charts(comparison));
    }
}
