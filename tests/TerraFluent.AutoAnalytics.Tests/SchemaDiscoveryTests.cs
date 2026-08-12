using System.Linq;
using TerraFluent.AutoAnalytics.Data.Sources;
using TerraFluent.AutoAnalytics.Enums;
using TerraFluent.AutoAnalytics.Schema;
using Xunit;

namespace TerraFluent.AutoAnalytics.Tests;

public class SchemaDiscoveryTests
{
    private const string Csv =
        "OrderId,OrderDate,Country,Revenue,Profit%,Active\n" +
        "1001,2024-01-15,USA,$1200,12%,true\n" +
        "1002,2024-02-15,Canada,$980,9%,false\n" +
        "1003,2024-03-15,USA,$1450,15%,true\n" +
        "1004,2024-04-15,Mexico,$760,7%,false\n";

    private static IReadOnlyList<ColumnProfile> Discover()
    {
        var dataset = new CsvDataSource(Csv).Load();
        return new SchemaDiscoveryEngine().Discover(dataset);
    }

    [Fact]
    public void InfersDate_Currency_Percentage_Boolean_Category()
    {
        var profiles = Discover().ToDictionary(p => p.Name);
        Assert.Equal(ColumnType.Date, profiles["OrderDate"].Type);
        Assert.Equal(ColumnType.Currency, profiles["Revenue"].Type);
        Assert.Equal(ColumnType.Percentage, profiles["Profit%"].Type);
        Assert.Equal(ColumnType.Boolean, profiles["Active"].Type);
        Assert.Equal(ColumnType.Category, profiles["Country"].Type);
    }

    [Fact]
    public void InfersIdentifier_ForUniqueIdColumn()
        => Assert.Equal(ColumnType.Identifier, Discover().First(p => p.Name == "OrderId").Type);

    [Fact]
    public void InfersSemanticRoles()
    {
        var profiles = Discover().ToDictionary(p => p.Name);
        Assert.Equal(SemanticRole.DateDimension, profiles["OrderDate"].Role);
        Assert.Equal(SemanticRole.GeographyDimension, profiles["Country"].Role);
        Assert.Equal(SemanticRole.RevenueMetric, profiles["Revenue"].Role);
        Assert.Equal(SemanticRole.ProfitMetric, profiles["Profit%"].Role);
    }
}
