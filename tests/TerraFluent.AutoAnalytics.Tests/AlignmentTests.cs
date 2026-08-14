using System.Linq;
using TerraFluent.AutoAnalytics.Analytics;
using TerraFluent.AutoAnalytics.Data.Sources;
using TerraFluent.AutoAnalytics.Enums;
using TerraFluent.AutoAnalytics.Profiling;
using TerraFluent.AutoAnalytics.Schema;
using Xunit;

namespace TerraFluent.AutoAnalytics.Tests;

/// <summary>
/// Guards the row-alignment contract for cross-column analytics. Each column stores its non-missing
/// values compacted, so aligning two columns by list index silently pairs the wrong rows once any
/// cell is missing. These tests use datasets with missing cells and assert the row-aligned result.
/// </summary>
public class AlignmentTests
{
    private static DatasetProfile Profile(string csv)
    {
        var dataset = new CsvDataSource(csv).Load();
        var schema = new SchemaDiscoveryEngine().Discover(dataset);
        return new DataProfilingEngine().Profile(dataset, schema);
    }

    [Fact]
    public void GroupAggregation_WithMissingMeasureCell_AttributesToCorrectRow()
    {
        // West's Sales is missing on row 2. Index alignment would shift East's 200 onto West.
        var profile = Profile("Region,Sales\nEast,100\nWest,\nEast,50\nWest,200\n");
        var result = new AggregationEngine().GroupBy(profile, "Sales", "Region", null, AggregationKind.Sum)!;

        Assert.Equal(150, result.Buckets.First(b => b.Key == "East").Value, 3);
        Assert.Equal(200, result.Buckets.First(b => b.Key == "West").Value, 3);
    }

    [Fact]
    public void Correlation_WithMissingCell_ExcludesThatRowNotShiftsIt()
    {
        // A is missing on the row whose B is a 100 outlier. Row alignment drops only that row,
        // leaving a perfect positive relationship; index alignment would pull the outlier in.
        var profile = Profile("A,B\n1,1\n2,2\n3,3\n,100\n5,5\n6,6\n");
        var corr = new RelationshipEngine().Correlations(profile);
        var c = Assert.Single(corr);

        Assert.Equal(1.0, c.Pearson, 6);
        Assert.Equal(5, c.SampleSize); // 5 complete-case rows (the missing-A row is dropped)
        Assert.Equal(CorrelationStrength.VeryStrong, c.Strength);
    }

    [Fact]
    public void PeriodComparison_WithMissingValue_BucketsRemainingRowsCorrectly()
    {
        // February's revenue is missing; its row must not bucket January's or March's value.
        var profile = Profile(
            "Month,Revenue\n2024-01-01,100\n2024-02-01,\n2024-03-01,300\n2024-04-01,400\n");
        var result = new PeriodComparisonEngine().Compare(profile, "Revenue")!;

        Assert.Equal(100, result.Periods.First(p => p.Label == "2024-01").Value, 3);
        Assert.Equal(300, result.Periods.First(p => p.Label == "2024-03").Value, 3);
        Assert.DoesNotContain(result.Periods, p => p.Label == "2024-02");
    }
}
