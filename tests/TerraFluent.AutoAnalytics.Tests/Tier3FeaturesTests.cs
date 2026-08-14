using System.Collections.Generic;
using System.Linq;
using TerraFluent.AutoAnalytics.Analytics;
using TerraFluent.AutoAnalytics.Data;
using TerraFluent.AutoAnalytics.Data.Sources;
using TerraFluent.AutoAnalytics.Engine;
using TerraFluent.AutoAnalytics.Enums;
using TerraFluent.AutoAnalytics.Statistics;
using Xunit;

namespace TerraFluent.AutoAnalytics.Tests;

public class Tier3FeaturesTests
{
    private const string Csv =
        "Region,Product,Revenue,Cost\n" +
        "East,Alpha,12000,7000\n" +
        "West,Beta,7000,4200\n" +
        "East,Beta,15000,8000\n" +
        "West,Alpha,8000,4600\n" +
        "East,Alpha,4000,2500\n" +
        "West,Beta,9000,5000\n" +
        "East,Beta,20000,9500\n" +
        "West,Alpha,6000,3600\n";

    private static Dataset LoadDataset() => new CsvDataSource(Csv).Load();

    // ── Slice / filter ──────────────────────────────────────────────────────────

    [Fact]
    public void Slice_SingleStringCondition_FiltersRows()
    {
        var filtered = SliceExpression.Parse("Region = East").Apply(LoadDataset());
        Assert.Equal(4, filtered.RowCount);
    }

    [Fact]
    public void Slice_AndConditions_CombineWithLogicalAnd()
    {
        var filtered = SliceExpression.Parse("Region = East and Revenue > 10000").Apply(LoadDataset());
        Assert.Equal(3, filtered.RowCount);
    }

    [Fact]
    public void Slice_NotEqualAndContains_Work()
    {
        Assert.Equal(4, SliceExpression.Parse("Region != East").Apply(LoadDataset()).RowCount);
        Assert.Equal(4, SliceExpression.Parse("Product ~ Alpha").Apply(LoadDataset()).RowCount);
    }

    [Fact]
    public void Slice_EmptyOrUnknownColumn_ReturnsAllRows()
    {
        Assert.Equal(8, SliceExpression.Parse(null).Apply(LoadDataset()).RowCount);
        Assert.Equal(8, SliceExpression.Parse("Nonexistent = 5").Apply(LoadDataset()).RowCount);
    }

    // ── Aggregation ─────────────────────────────────────────────────────────────

    [Fact]
    public void Aggregate_AverageByRegion_ComputesGroupMeans()
    {
        var profile = AnalyticsEngine.AnalyzeCsv(Csv).Profile;
        var result = new AggregationEngine().GroupBy(profile, "Revenue", "Region", null, AggregationKind.Average);

        Assert.NotNull(result);
        var east = result!.Buckets.First(b => b.Key == "East");
        Assert.Equal(12750, east.Value, 3);
        Assert.Equal(4, east.Count);
    }

    [Fact]
    public void Aggregate_MedianByRegion_ComputesGroupMedians()
    {
        var profile = AnalyticsEngine.AnalyzeCsv(Csv).Profile;
        var result = new AggregationEngine().GroupBy(profile, "Revenue", "Region", null, AggregationKind.Median)!;

        Assert.Equal(13500, result.Buckets.First(b => b.Key == "East").Value, 3);
    }

    [Fact]
    public void Aggregate_TwoDimensions_ProducesPivot()
    {
        var profile = AnalyticsEngine.AnalyzeCsv(Csv).Profile;
        var result = new AggregationEngine().GroupBy(profile, "Revenue", "Region", "Product", AggregationKind.Sum)!;

        Assert.True(result.IsPivot);
        Assert.Equal(4, result.Cells.Count);
        Assert.Contains("East", result.RowKeys);
        Assert.Contains("Beta", result.ColumnKeys);

        var eastBeta = result.Cells.First(c => c.Row == "East" && c.Column == "Beta");
        Assert.Equal(35000, eastBeta.Value, 3);
    }

    [Fact]
    public void Aggregate_UnknownColumn_ReturnsNull()
    {
        var profile = AnalyticsEngine.AnalyzeCsv(Csv).Profile;
        Assert.Null(new AggregationEngine().GroupBy(profile, "Nope", "Region"));
    }

    // ── Segmentation / k-means ──────────────────────────────────────────────────

    [Fact]
    public void KMeans_IsDeterministic()
    {
        var points = new List<double[]>
        {
            new[] { 1.0, 1.0 }, new[] { 1.2, 0.9 }, new[] { 8.0, 8.1 },
            new[] { 8.2, 7.9 }, new[] { 1.1, 1.3 }, new[] { 7.9, 8.2 }
        };
        var a = KMeans.Cluster(points, 2);
        var b = KMeans.Cluster(points, 2);

        Assert.Equal(a.Assignments, b.Assignments);
        Assert.Equal(2, a.K);
    }

    [Fact]
    public void KMeans_SeparatesTwoObviousClusters()
    {
        var points = new List<double[]>
        {
            new[] { 0.0 }, new[] { 0.5 }, new[] { 1.0 },
            new[] { 50.0 }, new[] { 50.5 }, new[] { 51.0 }
        };
        var result = KMeans.Cluster(points, 2);

        // The first three and last three points must land in different clusters.
        Assert.Equal(result.Assignments[0], result.Assignments[2]);
        Assert.Equal(result.Assignments[3], result.Assignments[5]);
        Assert.NotEqual(result.Assignments[0], result.Assignments[5]);
    }

    [Fact]
    public void Segmentation_ProducesLabelledSegments()
    {
        var profile = AnalyticsEngine.AnalyzeCsv(Csv).Profile;
        var result = new SegmentationEngine().Segment(profile);

        Assert.NotNull(result);
        Assert.True(result!.Segments.Count >= 2);
        Assert.Equal(8, result.RowsClustered);
        Assert.All(result.Segments, s => Assert.False(string.IsNullOrWhiteSpace(s.Label)));
    }
}
