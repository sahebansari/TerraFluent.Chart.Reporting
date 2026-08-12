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
    public void DashboardBuilder_AssemblesKpisAndSections()
    {
        var result = AnalyticsEngine.AnalyzeCsv(SalesCsv);
        var dashboard = DashboardBuilder.Generate(result);

        Assert.NotEmpty(dashboard.Kpis);
        Assert.NotEmpty(dashboard.TrendCharts);
        Assert.NotEmpty(dashboard.KeyInsights);
        Assert.Contains(dashboard.Kpis, k => k.Label == "Revenue");
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
