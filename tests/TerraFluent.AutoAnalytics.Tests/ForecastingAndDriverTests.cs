using System.Collections.Generic;
using System.Linq;
using TerraFluent.AutoAnalytics.Analytics;
using TerraFluent.AutoAnalytics.Engine;
using TerraFluent.AutoAnalytics.Statistics;
using Xunit;

namespace TerraFluent.AutoAnalytics.Tests;

public class ForecastingAndDriverTests
{
    private const string SalesCsv =
        "Month,Region,Revenue\n" +
        "2024-01,East,10000\n" +
        "2024-02,West,7000\n" +
        "2024-03,East,12000\n" +
        "2024-04,West,7500\n" +
        "2024-05,East,14000\n" +
        "2024-06,West,8000\n" +
        "2024-07,East,16000\n" +
        "2024-08,West,8500\n" +
        "2024-09,East,45000\n";

    [Fact]
    public void Holt_ProjectsRisingSeriesUpward()
    {
        var series = new List<double> { 100, 110, 120, 130, 140, 150 };
        var f = Forecasting.Holt(series, horizon: 3);

        Assert.False(f.IsEmpty);
        Assert.Equal(3, f.Points.Count);
        Assert.True(f.ProjectedChange > 0);
        Assert.True(f.Points[^1].Value > series[^1]);
    }

    [Fact]
    public void Holt_ConfidenceBandBracketsTheValue()
    {
        var series = new List<double> { 10, 14, 9, 16, 12, 18, 13, 20 };
        var f = Forecasting.Holt(series, horizon: 2);

        Assert.All(f.Points, p =>
        {
            Assert.True(p.Lower <= p.Value);
            Assert.True(p.Upper >= p.Value);
        });
    }

    [Fact]
    public void Holt_ShortSeries_ReturnsEmpty()
    {
        var f = Forecasting.Holt(new List<double> { 1, 2, 3 }, horizon: 3);
        Assert.True(f.IsEmpty);
    }

    [Fact]
    public void Driver_AttributesRevenueRiseToEast()
    {
        var profile = AnalyticsEngine.AnalyzeCsv(SalesCsv).Profile;
        var driver = new DriverEngine().Explain(profile, "Revenue");

        Assert.NotNull(driver);
        Assert.True(driver!.Increased);
        Assert.Equal("East", driver.TopDriver!.Category);
        // East should account for the large majority of the net change.
        Assert.True(driver.TopDriver.ShareOfChange > 0.7);
    }

    [Fact]
    public void Driver_ContributionsSumToTotalChange()
    {
        var profile = AnalyticsEngine.AnalyzeCsv(SalesCsv).Profile;
        var driver = new DriverEngine().Explain(profile, "Revenue")!;

        double sumOfDeltas = driver.Contributions.Sum(c => c.Delta);
        Assert.Equal(driver.TotalChange, sumOfDeltas, 3);
    }
}
