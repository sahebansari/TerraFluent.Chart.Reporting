using System.Linq;
using TerraFluent.AutoAnalytics.Dashboard;
using TerraFluent.AutoAnalytics.Engine;
using TerraFluent.AutoAnalytics.Enums;
using TerraFluent.AutoAnalytics.Recommendation;
using Xunit;
using ChartType = TerraFluent.Chart.Reporting.Enums.ChartType;

namespace TerraFluent.AutoAnalytics.Tests;

public class EngineEndToEndTests
{
    private const string SalesCsv =
        "OrderDate,Region,Product,Revenue,Profit\n" +
        "2024-01-01,East,Alpha,1000,200\n" +
        "2024-02-01,West,Beta,800,150\n" +
        "2024-03-01,East,Alpha,1200,260\n" +
        "2024-04-01,West,Beta,900,170\n" +
        "2024-05-01,East,Alpha,1500,330\n" +
        "2024-06-01,West,Gamma,700,120\n";

    [Fact]
    public void Analyze_ProducesProfileInsightsAndRecommendations()
    {
        var result = AnalyticsEngine.AnalyzeCsv(SalesCsv);

        Assert.True(result.Profile.ColumnCount == 5);
        Assert.True(result.Profile.RowCount == 6);
        Assert.NotEmpty(result.Insights);
        Assert.NotEmpty(result.Recommendations);
        Assert.False(string.IsNullOrWhiteSpace(result.Summary.Headline));
    }

    [Fact]
    public void Recommendations_IncludeTimeSeriesLine_AndAreExplained()
    {
        var result = AnalyticsEngine.AnalyzeCsv(SalesCsv);
        Assert.Contains(result.Recommendations, r => r.ChartType == ChartType.Line);
        Assert.All(result.Recommendations, r =>
        {
            Assert.False(string.IsNullOrWhiteSpace(r.Reason));
            Assert.InRange(r.SuitabilityScore, 0, 100);
        });
    }

    [Fact]
    public void Recommendations_AreSortedByScoreDescending()
    {
        var recs = AnalyticsEngine.AnalyzeCsv(SalesCsv).Recommendations;
        for (int i = 1; i < recs.Count; i++)
            Assert.True(recs[i - 1].SuitabilityScore >= recs[i].SuitabilityScore);
    }

    [Fact]
    public void ChartConfigBuilder_RendersSvgFromRecommendation()
    {
        var result = AnalyticsEngine.AnalyzeCsv(SalesCsv);
        var rec = result.Recommendations.First();
        string svg = ChartConfigBuilder.ToSvg(rec.Spec);
        Assert.StartsWith("<svg", svg.TrimStart());
        Assert.Contains("</svg>", svg);
    }

    [Fact]
    public void ChartConfigBuilder_EnlargesChartFonts()
    {
        var result = AnalyticsEngine.AnalyzeCsv(SalesCsv);
        string svg = ChartConfigBuilder.ToSvg(result.Recommendations.First().Spec);
        // FontScale 1.2 scales axis labels 11→13px and the title 16→19px (vs the library default).
        Assert.Contains(".axis-label    { font: 13px", svg);
        Assert.Contains(".chart-title   { font: bold 19px", svg);
    }

    [Fact]
    public void RangeTicks_ManyBins_AreRotatedDiagonally()
    {
        var spec = new ChartSpec
        {
            Type = ChartType.Line,
            Title = "Binned",
            Categories = new[] { "0\u201310", "10\u201320", "20\u201330", "30\u201340", "40\u201350", "50\u201360", "60\u201370", "70\u201380" },
            Series = new[] { new SeriesSpec { Name = "Avg", Values = new double?[] { 1, 2, 3, 4, 5, 6, 7, 8 } } }
        };
        string svg = ChartConfigBuilder.ToSvg(spec);
        Assert.Contains("rotate(-45", svg);
    }

    [Fact]
    public void RangeTicks_FewBins_AreStillDiagonal()
    {
        var spec = new ChartSpec
        {
            Type = ChartType.Line,
            Title = "Binned",
            Categories = new[] { "0\u201310", "10\u201320", "20\u201330", "30\u201340" },
            Series = new[] { new SeriesSpec { Name = "Avg", Values = new double?[] { 1, 2, 3, 4 } } }
        };
        string svg = ChartConfigBuilder.ToSvg(spec);
        // Range-style ticks are always angled regardless of count (diagonal for ≤12 bins).
        Assert.Contains("rotate(-45", svg);
        Assert.DoesNotContain("rotate(-90", svg);
    }

    [Fact]
    public void DashboardBuilder_AssemblesKpisAndSections()
    {
        var result = AnalyticsEngine.AnalyzeCsv(SalesCsv);
        var dashboard = DashboardBuilder.Generate(result);

        Assert.NotEmpty(dashboard.Kpis);
        Assert.NotEmpty(dashboard.TrendCharts);
        Assert.NotEmpty(dashboard.KeyInsights);
        Assert.Contains(dashboard.Kpis, k => k.Label == "Revenue");
    }

    // Additivity is domain-agnostic: counts and money are summed; per-row attributes (price,
    // rating) and ratio-like [0,1] columns (efficiency) are averaged — for any dataset, not just
    // the employee example.
    [Fact]
    public void DashboardBuilder_SumsCountsAndMoney_ButAveragesAttributesAndRatios()
    {
        const string retailCsv =
            "Store,Product,UnitsSold,Price,Rating,Efficiency,Revenue\n" +
            "North,Widget,120,9.99,4.5,0.82,1199\n" +
            "South,Widget,80,9.99,4.1,0.75,799\n" +
            "North,Gadget,60,19.99,3.9,0.66,1199\n" +
            "South,Gadget,45,19.99,4.7,0.90,900\n" +
            "North,Gizmo,200,4.99,4.2,0.71,998\n" +
            "South,Gizmo,150,4.99,4.8,0.88,749\n";

        var result = AnalyticsEngine.AnalyzeCsv(retailCsv);
        var dashboard = DashboardBuilder.Generate(result, maxKpis: 8);

        string Caption(string label) =>
            dashboard.Kpis.Single(k => k.Label == label).Caption;

        Assert.StartsWith("total", Caption("Revenue"));      // money → sum
        Assert.StartsWith("total", Caption("Units Sold"));   // count → sum (label humanised from UnitsSold)
        Assert.StartsWith("average", Caption("Price"));      // per-unit attribute → average
        Assert.StartsWith("average", Caption("Rating"));     // rating attribute → average
        Assert.StartsWith("average", Caption("Efficiency")); // [0,1] ratio (unknown name) → average
    }

    [Fact]
    public void Insights_HaveScoresAndEvidence()
    {
        var result = AnalyticsEngine.AnalyzeCsv(SalesCsv);
        Assert.All(result.Insights, i =>
        {
            Assert.InRange(i.ImportanceScore, 0, 100);
            Assert.False(string.IsNullOrWhiteSpace(i.Description));
        });
    }

    [Fact]
    public void Analyze_Enumerable_Works()
    {
        var rows = new[]
        {
            new { Region = "East", Revenue = 100.0 },
            new { Region = "West", Revenue = 250.0 },
            new { Region = "East", Revenue = 300.0 },
        };
        var result = AnalyticsEngine.Analyze(rows);
        Assert.Equal(3, result.Profile.RowCount);
        Assert.Equal(2, result.Profile.ColumnCount);
    }
}
