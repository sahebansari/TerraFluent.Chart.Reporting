using System.Linq;
using TerraFluent.AutoAnalytics.Analytics;
using TerraFluent.AutoAnalytics.Data.Sources;
using TerraFluent.AutoAnalytics.Enums;
using TerraFluent.AutoAnalytics.Profiling;
using TerraFluent.AutoAnalytics.Schema;
using Xunit;

namespace TerraFluent.AutoAnalytics.Tests;

public class AnalyticsTests
{
    private static DatasetProfile Profile(string csv)
    {
        var dataset = new CsvDataSource(csv).Load();
        var schema = new SchemaDiscoveryEngine().Discover(dataset);
        return new DataProfilingEngine().Profile(dataset, schema);
    }

    [Fact]
    public void GroupAnalysis_SumsMeasureByDimension()
    {
        var profile = Profile("Region,Sales\nEast,100\nWest,50\nEast,150\nWest,50\n");
        var groups = new RelationshipEngine().GroupAnalyses(profile);
        var g = Assert.Single(groups);
        Assert.Equal("Region", g.Dimension);
        Assert.Equal(350, g.Total, 3);
        Assert.Equal("East", g.Top!.Key);
        Assert.Equal(250, g.Top!.Value, 3);
    }

    [Fact]
    public void Correlation_DetectsStrongPositive()
    {
        var profile = Profile("X,Y\n1,2\n2,4\n3,6\n4,8\n5,10\n");
        var corr = new RelationshipEngine().Correlations(profile);
        var c = Assert.Single(corr);
        Assert.True(c.Pearson > 0.99);
        Assert.Equal(CorrelationStrength.VeryStrong, c.Strength);
    }

    [Fact]
    public void Trend_DetectsRising()
    {
        var profile = Profile("Month,Revenue\n2024-01-01,100\n2024-02-01,120\n2024-03-01,145\n2024-04-01,170\n");
        var trends = new TrendEngine().DetectTrends(profile);
        var t = Assert.Single(trends);
        Assert.Equal(TrendKind.Rising, t.Kind);
        Assert.True(t.GrowthRate > 0.5);
    }

    [Fact]
    public void Anomaly_DetectsOutlier()
    {
        var profile = Profile("Id,Value\n1,10\n2,11\n3,10\n4,12\n5,11\n6,9\n7,10\n8,500\n");
        var anomalies = new AnomalyEngine().Detect(profile);
        var a = Assert.Single(anomalies);
        Assert.Contains(a.Anomalies, p => p.Value == 500);
    }
}
