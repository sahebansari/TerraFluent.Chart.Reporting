using System.Linq;
using TerraFluent.AutoAnalytics.Data.Sources;
using TerraFluent.AutoAnalytics.Schema;
using TerraFluent.AutoAnalytics.Validation;
using Xunit;

namespace TerraFluent.AutoAnalytics.Tests;

public class ValidationTests
{
    private static ValidationReport Validate(string csv)
    {
        var dataset = new CsvDataSource(csv).Load();
        var schema = new SchemaDiscoveryEngine().Discover(dataset);
        return new DataValidationEngine().Validate(dataset, schema);
    }

    [Fact]
    public void FlagsMissingValues()
    {
        var report = Validate("Id,Amount\n1,10\n2,\n3,30\n");
        Assert.Contains(report.Issues, i => i.Code == "MISSING_VALUES" && i.Column == "Amount");
    }

    [Fact]
    public void FlagsDuplicateRows()
    {
        var report = Validate("Id,City\n1,NY\n1,NY\n2,LA\n");
        Assert.Contains(report.Issues, i => i.Code == "DUPLICATE_ROWS");
    }

    [Fact]
    public void FlagsDuplicateKeys()
    {
        var report = Validate("CustomerId,City\n7,NY\n7,LA\n8,SF\n");
        Assert.Contains(report.Issues, i => i.Code == "DUPLICATE_KEYS" && i.Column == "CustomerId");
    }

    [Fact]
    public void FlagsPercentageOutOfRange()
    {
        var report = Validate("Id,Rate\n1,45%\n2,140%\n3,20%\n");
        Assert.Contains(report.Issues, i => i.Code == "PERCENT_OUT_OF_RANGE");
    }

    [Fact]
    public void CleanData_ProducesNoWarnings()
    {
        var report = Validate("Id,City\n1,NY\n2,LA\n3,SF\n");
        Assert.True(report.IsClean);
    }
}
