namespace TerraFluent.AutoAnalytics.Enums;

/// <summary>
/// The inferred semantic/storage type of a dataset column, produced by the schema discovery engine.
/// </summary>
public enum ColumnType
{
    /// <summary>Plain numeric quantity (integers or reals) with no currency/percentage semantics.</summary>
    Numeric,
    /// <summary>Monetary amount (detected via symbols such as $, €, £ or currency-like column names).</summary>
    Currency,
    /// <summary>Ratio expressed out of 100 (detected via % symbol or names ending in "%", "rate", "margin").</summary>
    Percentage,
    /// <summary>Calendar date or timestamp.</summary>
    Date,
    /// <summary>Low-cardinality discrete label used for grouping (e.g. Country, Product).</summary>
    Category,
    /// <summary>Two-state value (true/false, yes/no, 0/1).</summary>
    Boolean,
    /// <summary>Free-form high-cardinality text with no grouping value.</summary>
    Text,
    /// <summary>Unique row key (Id, GUID, sequential number) — excluded from most analysis.</summary>
    Identifier
}
