using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using TerraFluent.AutoAnalytics.Engine;
using TerraFluent.AutoAnalytics.Enums;
using TerraFluent.AutoAnalytics.Recommendation;
using Xunit;
using ChartType = TerraFluent.Chart.Reporting.Enums.ChartType;

namespace TerraFluent.AutoAnalytics.Tests;

/// <summary>
/// Covers the forward-looking analytics that the core pipeline now runs itself — forecasts,
/// period-over-period comparisons and segmentation — plus the chart forms they unlock. Previously
/// these were reachable only through the agent, so <c>/analyze</c> and the dashboard never saw them.
/// </summary>
public class PipelineForwardAnalyticsTests
{
    // Two years of monthly revenue with a steady upward trend, split across two regions and
    // three products so composition/segmentation have something to work with.
    private static string MonthlySalesCsv()
    {
        var sb = new StringBuilder("Month,Region,Product,Revenue,Cost\n");
        string[] regions = { "EU", "NA" };
        string[] products = { "Widget", "Gadget", "Gizmo" };
        var start = new DateTime(2023, 1, 1);

        for (int m = 0; m < 24; m++)
        {
            var month = start.AddMonths(m);
            for (int r = 0; r < regions.Length; r++)
            {
                for (int p = 0; p < products.Length; p++)
                {
                    double revenue = 1000 + m * 60 + r * 400 + p * 150;
                    double cost = revenue * 0.6;
                    sb.Append(month.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)).Append(',')
                      .Append(regions[r]).Append(',')
                      .Append(products[p]).Append(',')
                      .Append(revenue.ToString(CultureInfo.InvariantCulture)).Append(',')
                      .Append(cost.ToString(CultureInfo.InvariantCulture)).Append('\n');
                }
            }
        }
        return sb.ToString();
    }

    private static AnalyticsResult Analyze(AnalyticsOptions? options = null) =>
        AnalyticsEngine.AnalyzeCsv(MonthlySalesCsv(), options ?? new AnalyticsOptions { MaxRecommendations = 60 });

    // ── Findings ──────────────────────────────────────────────────────────────

    [Fact]
    public void Pipeline_ProducesForecastsForItsMeasures()
    {
        var findings = Analyze().Findings;

        Assert.NotEmpty(findings.Forecasts);
        var revenue = findings.Forecasts.Single(f => f.Measure == "Revenue");

        Assert.Equal(24, revenue.HistoryValues.Count);           // collapsed to one point per month
        Assert.Equal(DateGranularity.Monthly, revenue.Granularity);
        Assert.Equal(ForecastEngineHorizon, revenue.Forecast.Points.Count);
        Assert.All(revenue.Forecast.Points, p => Assert.True(p.Lower <= p.Value && p.Value <= p.Upper));
        Assert.True(revenue.Forecast.ProjectedChange > 0);       // the series rises throughout
    }

    private const int ForecastEngineHorizon = 3;

    [Fact]
    public void Pipeline_HonoursTheConfiguredForecastHorizon()
    {
        var findings = Analyze(new AnalyticsOptions { ForecastHorizon = 6 }).Findings;
        Assert.All(findings.Forecasts, f => Assert.Equal(6, f.Forecast.Points.Count));
    }

    [Fact]
    public void Pipeline_ProducesPeriodComparisons()
    {
        var findings = Analyze().Findings;

        var revenue = findings.PeriodComparisons.Single(c => c.Measure == "Revenue");
        Assert.Equal(24, revenue.Periods.Count);
        Assert.Equal(DateGranularity.Monthly, revenue.Granularity);
        Assert.NotNull(revenue.YearOverYearPct);                 // two full years of history
    }

    [Fact]
    public void Pipeline_ProducesSegmentation()
    {
        var findings = Analyze().Findings;

        Assert.NotNull(findings.Segmentation);
        Assert.True(findings.Segmentation!.Segments.Count >= 2);
        Assert.True(findings.Segmentation.RowsClustered > 0);
        Assert.Equal(1.0, findings.Segmentation.Segments.Sum(s => s.Share), 3);
    }

    [Fact]
    public void Pipeline_CanDisableEachForwardAnalysis()
    {
        var findings = Analyze(new AnalyticsOptions
        {
            EnableForecasting = false,
            EnablePeriodComparison = false,
            EnableSegmentation = false
        }).Findings;

        Assert.Empty(findings.Forecasts);
        Assert.Empty(findings.PeriodComparisons);
        Assert.Null(findings.Segmentation);
    }

    [Fact]
    public void Pipeline_IsDeterministicAcrossRuns()
    {
        var first = Analyze().Findings;
        var second = Analyze().Findings;

        Assert.Equal(
            first.Forecasts.Select(f => f.Forecast.Points[^1].Value),
            second.Forecasts.Select(f => f.Forecast.Points[^1].Value));
        Assert.Equal(
            first.Segmentation!.Segments.Select(s => s.Size),
            second.Segmentation!.Segments.Select(s => s.Size));
    }

    // ── Insights ──────────────────────────────────────────────────────────────

    [Fact]
    public void Insights_IncludeForecastPeriodChangeAndSegmentation()
    {
        var insights = Analyze(new AnalyticsOptions { MaxInsights = 100 }).Insights;
        var kinds = insights.Select(i => i.Kind).ToHashSet();

        Assert.Contains(InsightKind.Forecast, kinds);
        Assert.Contains(InsightKind.PeriodChange, kinds);
        Assert.Contains(InsightKind.Segmentation, kinds);
    }

    [Fact]
    public void ForecastInsight_CarriesItsBandAndMethodAsEvidence()
    {
        var forecast = Analyze(new AnalyticsOptions { MaxInsights = 100 }).Insights
            .First(i => i.Kind == InsightKind.Forecast);

        Assert.InRange(forecast.ImportanceScore, 0, 100);
        Assert.True(forecast.Evidence.ContainsKey("lower"));
        Assert.True(forecast.Evidence.ContainsKey("upper"));
        Assert.True(forecast.Evidence.ContainsKey("method"));
        Assert.False(string.IsNullOrWhiteSpace(forecast.Description));
    }

    [Fact]
    public void PeriodChangeInsight_ReportsYearOverYearWhenAvailable()
    {
        var change = Analyze(new AnalyticsOptions { MaxInsights = 100 }).Insights
            .First(i => i.Kind == InsightKind.PeriodChange);

        Assert.True(change.Evidence.ContainsKey("yearOverYearPct"));
        Assert.NotEqual("n/a", change.Evidence["yearOverYearPct"]);
        Assert.Contains("Year-over-year", change.Description, StringComparison.OrdinalIgnoreCase);
    }

    // ── Chart recommendations ─────────────────────────────────────────────────

    [Fact]
    public void Recommendations_IncludeTheNewlyUnlockedChartTypes()
    {
        var types = Analyze().Recommendations.Select(r => r.ChartType).ToHashSet();

        Assert.Contains(ChartType.Waterfall, types);   // period-over-period bridge
        Assert.Contains(ChartType.Heatmap, types);     // dimension × dimension composition
        Assert.Contains(ChartType.BoxPlot, types);     // spread of a measure within each category
    }

    [Fact]
    public void ForecastChart_PlotsHistoryProjectionAndBandOnOneAxis()
    {
        var chart = Analyze().Recommendations
            .First(r => r.Spec.Title.Contains("forecast", StringComparison.OrdinalIgnoreCase));

        var band = chart.Spec.Series.Single(s => s.TypeOverride == ChartType.AreaRange);
        var actual = chart.Spec.Series.First(s => s.TypeOverride is null);

        // Every series spans the same category axis: history followed by the horizon.
        Assert.Equal(chart.Spec.Categories.Count, band.RangeValues.Count);
        Assert.Equal(chart.Spec.Categories.Count, actual.Values.Count);

        // The band is zero-width over history and opens out across the projection.
        Assert.Equal(band.RangeValues[0].Low, band.RangeValues[0].High);
        Assert.True(band.RangeValues[^1].High > band.RangeValues[^1].Low);
    }

    // A year of trendless, heavily-noisy daily readings — the shape that made the forecast chart
    // unreadable, because 365 plotted history points crush the horizon into a sliver of the width.
    private static string NoisyDailyCsv()
    {
        var sb = new StringBuilder("Day,Load\n");
        var start = new DateTime(2024, 1, 1);
        uint seed = 12345;
        for (int d = 0; d < 365; d++)
        {
            seed = unchecked(seed * 1664525 + 1013904223);
            double load = 400 + (seed >> 16 & 0x7FFF) / 32767.0 * 400;   // 400..800, no trend
            sb.Append(start.AddDays(d).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)).Append(',')
              .Append(load.ToString("F1", CultureInfo.InvariantCulture)).Append('\n');
        }
        return sb.ToString();
    }

    private static RecommendedChart NoisyDailyForecastChart() =>
        AnalyticsEngine.AnalyzeCsv(NoisyDailyCsv(), new AnalyticsOptions { MaxRecommendations = 60 })
            .Recommendations.First(r => r.Spec.Title.Contains("forecast", StringComparison.OrdinalIgnoreCase));

    [Fact]
    public void ForecastChart_WindowsALongHistorySoTheHorizonStaysLegible()
    {
        var chart = NoisyDailyForecastChart();

        // The plotted axis is a short tail plus the horizon, not all 365 fitted periods.
        Assert.InRange(chart.Spec.Categories.Count, 4, 32);
        Assert.All(chart.Spec.Series, s =>
            Assert.Equal(chart.Spec.Categories.Count,
                s.TypeOverride == ChartType.AreaRange ? s.RangeValues.Count : s.Values.Count));

        // Windowing is a plotting concern only: the fit still saw the whole history, and the tail
        // ends on the real last period rather than wherever the window happened to start.
        Assert.Contains("365", chart.Reason);
        Assert.Equal("2024-12-30", chart.Spec.Categories[^4]);
    }

    [Fact]
    public void ForecastChart_LabelsItsHorizonWithSignedOffsets()
    {
        var chart = NoisyDailyForecastChart();
        Assert.Equal(new[] { "+1", "+2", "+3" }, chart.Spec.Categories.TakeLast(3));

        // The sign has to survive category formatting — rounding "+1" to "1" strips the only thing
        // that distinguishes a horizon step from a calendar label.
        Assert.Contains(">+1</text>", ChartConfigBuilder.ToSvg(chart.Spec));
    }

    [Fact]
    public void ForecastChart_IsDemotedWhenItsBandSwampsTheHistoryItWasFittedOn()
    {
        // Scoring on history length alone ranks a long, noisy, trendless series highest — exactly
        // the series that forecasts worst. A band as wide as the data itself carries no signal.
        var noisy = NoisyDailyForecastChart();
        var trended = Analyze().Recommendations
            .First(r => r.Spec.Title.Contains("forecast", StringComparison.OrdinalIgnoreCase));

        Assert.True(noisy.SuitabilityScore < trended.SuitabilityScore,
            $"noisy {noisy.SuitabilityScore} should rank below trended {trended.SuitabilityScore}");
        Assert.Contains("indicative only", noisy.Reason);
        Assert.DoesNotContain("indicative only", trended.Reason);
    }

    [Fact]
    public void ChartConfig_ThinsDenseCategoryLabelsInsteadOfOverlappingThem()
    {
        var categories = Enumerable.Range(0, 200)
            .Select(i => new DateTime(2024, 1, 1).AddDays(i).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture))
            .ToList();
        var spec = new ChartSpec
        {
            Type = ChartType.Line,
            Title = "Dense axis",
            Categories = categories,
            Series = new[]
            {
                new SeriesSpec { Name = "Load", Values = categories.Select((_, i) => (double?)i).ToList() }
            }
        };

        int rendered = System.Text.RegularExpressions.Regex
            .Matches(ChartConfigBuilder.ToSvg(spec), "<text[^>]*>2024-").Count;

        Assert.InRange(rendered, 1, categories.Count / 2);
    }

    [Fact]
    public void BoxPlotChart_CarriesAnOrderedFiveNumberSummaryPerCategory()
    {
        var chart = Analyze().Recommendations.First(r => r.ChartType == ChartType.BoxPlot);
        var series = Assert.Single(chart.Spec.Series);

        Assert.Equal(chart.Spec.Categories.Count, series.BoxValues.Count);
        Assert.All(series.BoxValues, b =>
        {
            Assert.True(b.Low <= b.Q1);
            Assert.True(b.Q1 <= b.Median);
            Assert.True(b.Median <= b.Q3);
            Assert.True(b.Q3 <= b.High);
        });
    }

    [Fact]
    public void HeatmapChart_AddressesEveryCellWithinItsGrid()
    {
        var chart = Analyze().Recommendations.First(r => r.ChartType == ChartType.Heatmap);
        var series = Assert.Single(chart.Spec.Series);

        Assert.NotEmpty(series.HeatCells);
        Assert.NotEmpty(series.RowLabels);
        Assert.All(series.HeatCells, c =>
        {
            Assert.InRange(c.Column, 0, chart.Spec.Categories.Count - 1);
            Assert.InRange(c.Row, 0, series.RowLabels.Count - 1);
        });
    }

    // ── Rendering ─────────────────────────────────────────────────────────────

    [Fact]
    public void EveryRecommendedChart_RendersToSvg()
    {
        foreach (var rec in Analyze().Recommendations)
        {
            string svg = ChartConfigBuilder.ToSvg(rec.Spec);
            Assert.StartsWith("<svg", svg);
            Assert.DoesNotContain("NaN", svg);
        }
    }

    [Fact]
    public void NewChartTypes_RenderAsThemselvesRatherThanFallingBackToColumns()
    {
        var recommendations = Analyze().Recommendations;

        foreach (var type in new[] { ChartType.Waterfall, ChartType.Heatmap, ChartType.BoxPlot })
        {
            var spec = recommendations.First(r => r.ChartType == type).Spec;
            var builder = ChartConfigBuilder.ToChartBuilder(spec);
            string svg = builder.RenderToSvg();

            Assert.StartsWith("<svg", svg);
            Assert.DoesNotContain("NaN", svg);
        }
    }
}
