using TerraFluent.AutoAnalytics;
using Xunit;

namespace TerraFluent.AutoAnalytics.Tests;

public class DisplayTextTests
{
    [Theory]
    [InlineData("years_of_experience", "Years of Experience")]
    [InlineData("employee_id", "Employee ID")]
    [InlineData("units_sold", "Units Sold")]
    [InlineData("salaryUSD", "Salary USD")]
    [InlineData("yearsOfExperience", "Years of Experience")]
    [InlineData("Revenue", "Revenue")]
    [InlineData("age", "Age")]
    [InlineData("first-name", "First Name")]
    [InlineData("order date", "Order Date")]
    public void Humanize_ProducesTitleCasedDisplayText(string raw, string expected)
    {
        Assert.Equal(expected, DisplayText.Humanize(raw));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Humanize_PassesThroughBlankInput(string? raw)
    {
        Assert.Equal(raw ?? string.Empty, DisplayText.Humanize(raw));
    }

    [Theory]
    [InlineData(40.7333, 40.7)]   // 10–100 → 1 decimal
    [InlineData(405.72, 406.0)]   // ≥100 → whole number
    [InlineData(4.7333, 4.73)]    // 1–10 → 2 decimals
    [InlineData(0.8267, 0.827)]   // <1 → 3 decimals
    [InlineData(12345.0, 12345.0)]
    public void Round_UsesMagnitudeAwarePrecision(double value, double expected)
    {
        Assert.Equal(expected, DisplayText.Round(value), 3);
    }

    [Theory]
    [InlineData(40.7333, "40.7")]
    [InlineData(405.72, "406")]
    [InlineData(1234.56, "1,235")]
    [InlineData(4.7333, "4.73")]
    public void FormatNumber_RoundsAndGroups(double value, string expected)
    {
        Assert.Equal(expected, DisplayText.FormatNumber(value));
    }
}
