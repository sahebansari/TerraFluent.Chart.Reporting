using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace TerraFluent.AutoAnalytics.Data.Sources;

/// <summary>
/// Streaming, RFC-4180-aware CSV adapter. Handles quoted fields, embedded commas/newlines,
/// and escaped double-quotes. The first non-empty row is treated as the header.
/// </summary>
public sealed class CsvDataSource : IDataSource
{
    private readonly Func<TextReader> _readerFactory;
    private readonly char _delimiter;
    private readonly string _name;

    /// <summary>Creates a CSV source over raw CSV text.</summary>
    public CsvDataSource(string csvText, char delimiter = ',', string? name = null)
    {
        if (csvText is null) throw new ArgumentNullException(nameof(csvText));
        _readerFactory = () => new StringReader(csvText);
        _delimiter     = delimiter;
        _name          = name ?? "CSV";
    }

    /// <summary>Creates a CSV source that reads from a file path.</summary>
    public static CsvDataSource FromFile(string path, char delimiter = ',')
    {
        if (string.IsNullOrWhiteSpace(path)) throw new ArgumentException("Path is required.", nameof(path));
        return new CsvDataSource(File.ReadAllText(path, Encoding.UTF8), delimiter, Path.GetFileNameWithoutExtension(path));
    }

    public Dataset Load()
    {
        using var reader = _readerFactory();
        var records = ParseAll(reader, _delimiter);
        if (records.Count == 0)
            return new Dataset(_name, System.Array.Empty<DataColumn>());

        var header = records[0];
        var columnNames = new List<string>(header.Count);
        for (int i = 0; i < header.Count; i++)
            columnNames.Add(string.IsNullOrWhiteSpace(header[i]) ? $"Column{i + 1}" : header[i].Trim());

        var rows = new List<object?[]>(records.Count - 1);
        for (int r = 1; r < records.Count; r++)
        {
            var fields = records[r];
            var row = new object?[columnNames.Count];
            for (int c = 0; c < columnNames.Count; c++)
            {
                string? cell = c < fields.Count ? fields[c] : null;
                row[c] = NormaliseCell(cell);
            }
            rows.Add(row);
        }

        return Dataset.FromRows(_name, columnNames, rows);
    }

    // Empty/whitespace becomes null so downstream null-handling is uniform.
    private static object? NormaliseCell(string? cell)
    {
        if (string.IsNullOrWhiteSpace(cell)) return null;
        return cell.Trim();
    }

    private static List<List<string>> ParseAll(TextReader reader, char delimiter)
    {
        var records = new List<List<string>>();
        var field   = new StringBuilder();
        var current = new List<string>();
        bool inQuotes = false;
        int ci;

        void EndField()
        {
            current.Add(field.ToString());
            field.Clear();
        }
        void EndRecord()
        {
            EndField();
            // Skip fully blank lines.
            if (current.Count > 1 || (current.Count == 1 && current[0].Length > 0))
                records.Add(current);
            current = new List<string>();
        }

        while ((ci = reader.Read()) != -1)
        {
            char ch = (char)ci;
            if (inQuotes)
            {
                if (ch == '"')
                {
                    if (reader.Peek() == '"') { reader.Read(); field.Append('"'); }
                    else inQuotes = false;
                }
                else field.Append(ch);
            }
            else
            {
                if (ch == '"') inQuotes = true;
                else if (ch == delimiter) EndField();
                else if (ch == '\r') { /* swallow, handle at \n */ }
                else if (ch == '\n') EndRecord();
                else field.Append(ch);
            }
        }

        // Flush trailing record with no terminating newline.
        if (field.Length > 0 || current.Count > 0) EndRecord();
        return records;
    }
}
