namespace TerraFluent.AutoAnalytics.Enums;

/// <summary>
/// Business-semantic role inferred from a column's name and profile. Drives smarter chart
/// recommendations (e.g. a <see cref="RevenueMetric"/> plotted against a <see cref="DateDimension"/>
/// strongly implies a time-series line chart).
/// </summary>
public enum SemanticRole
{
    /// <summary>No specific business role was inferred.</summary>
    Unknown,
    /// <summary>A revenue/sales/income measure to be summed or trended.</summary>
    RevenueMetric,
    /// <summary>A cost/expense measure.</summary>
    CostMetric,
    /// <summary>A profit/margin measure.</summary>
    ProfitMetric,
    /// <summary>A generic additive quantity measure (units, count, volume).</summary>
    QuantityMetric,
    /// <summary>A time dimension used for trends (Date, Year, Month, Quarter).</summary>
    DateDimension,
    /// <summary>A geographic dimension (Country, State, City, Region).</summary>
    GeographyDimension,
    /// <summary>A generic grouping dimension (Product, Customer, Category, Channel).</summary>
    CategoryDimension,
    /// <summary>A unique record identifier.</summary>
    IdentifierRole
}
