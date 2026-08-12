using System.Collections.Generic;

namespace TerraFluent.AutoAnalytics.Data;

/// <summary>
/// A single column of a <see cref="Dataset"/>, stored in columnar form for cache-friendly profiling.
/// Values are the raw parsed cell objects (string, double, DateTime, bool, or <see langword="null"/>).
/// </summary>
public sealed class DataColumn
{
    /// <summary>Zero-based position of this column within the dataset.</summary>
    public int Index { get; }

    /// <summary>The column header/name.</summary>
    public string Name { get; }

    /// <summary>Raw cell values, one per row, aligned with <see cref="Dataset.RowCount"/>.</summary>
    public IReadOnlyList<object?> Values { get; }

    public DataColumn(int index, string name, IReadOnlyList<object?> values)
    {
        Index  = index;
        Name   = name ?? string.Empty;
        Values = values ?? new List<object?>();
    }
}
