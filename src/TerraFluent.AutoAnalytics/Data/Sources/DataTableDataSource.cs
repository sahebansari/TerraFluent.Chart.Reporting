using System;
using System.Collections.Generic;
using System.Data;

namespace TerraFluent.AutoAnalytics.Data.Sources;

/// <summary>Adapter that wraps an ADO.NET <see cref="System.Data.DataTable"/>.</summary>
public sealed class DataTableDataSource : IDataSource
{
    private readonly DataTable _table;
    private readonly string _name;

    public DataTableDataSource(DataTable table, string? name = null)
    {
        _table = table ?? throw new ArgumentNullException(nameof(table));
        _name  = name ?? (string.IsNullOrWhiteSpace(table.TableName) ? "DataTable" : table.TableName);
    }

    public Dataset Load()
    {
        var columnNames = new List<string>(_table.Columns.Count);
        foreach (System.Data.DataColumn col in _table.Columns)
            columnNames.Add(col.ColumnName);

        var rows = new List<object?[]>(_table.Rows.Count);
        foreach (DataRow dr in _table.Rows)
        {
            var row = new object?[columnNames.Count];
            for (int c = 0; c < columnNames.Count; c++)
            {
                var v = dr[c];
                row[c] = v is null || v == DBNull.Value ? null : v;
            }
            rows.Add(row);
        }

        return Dataset.FromRows(_name, columnNames, rows);
    }
}
