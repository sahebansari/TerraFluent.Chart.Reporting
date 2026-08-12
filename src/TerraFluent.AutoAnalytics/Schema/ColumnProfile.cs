using TerraFluent.AutoAnalytics.Enums;

namespace TerraFluent.AutoAnalytics.Schema;

/// <summary>
/// The inferred schema of a single column: its data type, business role, cardinality and
/// how confident the discovery engine is. Immutable once produced.
/// </summary>
public sealed class ColumnProfile
{
    /// <summary>Zero-based column position.</summary>
    public int Index { get; init; }

    /// <summary>Column header/name.</summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>Inferred storage/semantic type.</summary>
    public ColumnType Type { get; init; }

    /// <summary>Inferred business role (drives chart recommendation).</summary>
    public SemanticRole Role { get; init; }

    /// <summary>Number of distinct non-missing values.</summary>
    public int DistinctCount { get; init; }

    /// <summary>Number of missing (null/blank/NaN) cells.</summary>
    public int MissingCount { get; init; }

    /// <summary>Total cells considered (= dataset row count).</summary>
    public int TotalCount { get; init; }

    /// <summary>Fraction 0..1 of cells that are populated.</summary>
    public double Completeness => TotalCount == 0 ? 0 : (double)(TotalCount - MissingCount) / TotalCount;

    /// <summary>Fraction 0..1 of populated cells that are distinct (1 ≈ identifier).</summary>
    public double Uniqueness
    {
        get
        {
            int populated = TotalCount - MissingCount;
            return populated == 0 ? 0 : (double)DistinctCount / populated;
        }
    }

    /// <summary>Confidence 0..1 that <see cref="Type"/> is correct (share of cells matching the type).</summary>
    public double TypeConfidence { get; init; }

    /// <summary>True when this column is a numeric measure suitable as a chart value axis.</summary>
    public bool IsMeasure => Type is ColumnType.Numeric or ColumnType.Currency or ColumnType.Percentage;

    /// <summary>True when this column is a dimension suitable for grouping/x-axis.</summary>
    public bool IsDimension => Type is ColumnType.Category or ColumnType.Date or ColumnType.Boolean;
}
