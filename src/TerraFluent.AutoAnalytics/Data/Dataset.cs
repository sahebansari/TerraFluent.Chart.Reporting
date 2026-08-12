using System;
using System.Collections.Generic;
using System.Linq;

namespace TerraFluent.AutoAnalytics.Data;

/// <summary>
/// Unified, source-agnostic in-memory representation of a tabular dataset. All input adapters
/// (CSV, Excel, DataTable, IEnumerable, JSON) normalise into this columnar structure before analysis.
/// </summary>
public sealed class Dataset
{
    private readonly List<DataColumn> _columns;

    /// <summary>Optional friendly name for the dataset (used in narratives and dashboard titles).</summary>
    public string Name { get; }

    /// <summary>The columns in original order.</summary>
    public IReadOnlyList<DataColumn> Columns => _columns;

    /// <summary>Number of data rows (excludes the header).</summary>
    public int RowCount { get; }

    /// <summary>Number of columns.</summary>
    public int ColumnCount => _columns.Count;

    public Dataset(string name, IEnumerable<DataColumn> columns)
    {
        Name     = string.IsNullOrWhiteSpace(name) ? "Dataset" : name;
        _columns = columns?.ToList() ?? new List<DataColumn>();
        RowCount = _columns.Count == 0 ? 0 : _columns.Max(c => c.Values.Count);
    }

    /// <summary>Finds a column by name (case-insensitive), or <see langword="null"/> if absent.</summary>
    public DataColumn? GetColumn(string name) =>
        _columns.FirstOrDefault(c => string.Equals(c.Name, name, StringComparison.OrdinalIgnoreCase));

    /// <summary>
    /// Builds a columnar dataset from a sequence of rows where each row is a value array aligned
    /// with <paramref name="columnNames"/>.
    /// </summary>
    public static Dataset FromRows(string name, IReadOnlyList<string> columnNames, IEnumerable<object?[]> rows)
    {
        if (columnNames is null) throw new ArgumentNullException(nameof(columnNames));
        var materialised = rows?.ToList() ?? new List<object?[]>();
        var columns = new List<DataColumn>(columnNames.Count);

        for (int c = 0; c < columnNames.Count; c++)
        {
            var values = new List<object?>(materialised.Count);
            foreach (var row in materialised)
                values.Add(c < row.Length ? row[c] : null);
            columns.Add(new DataColumn(c, columnNames[c], values));
        }

        return new Dataset(name, columns);
    }
}
