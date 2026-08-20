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

    [Fact]
    public void FlagsInvalidFormat_AsAtLeastWarning()
    {
        // 10 numeric ages + 1 non-numeric ("pr") keeps the column Numeric (>=90%) but one bad value.
        var report = Validate("Id,Age\n1,22\n2,23\n3,24\n4,25\n5,26\n6,pr\n7,28\n8,29\n9,30\n10,31\n11,32\n");
        Assert.Contains(report.Issues, i => i.Code == "INVALID_FORMAT" && i.Column == "Age");
        Assert.False(report.IsClean); // a type mismatch must not be treated as clean
    }
}
