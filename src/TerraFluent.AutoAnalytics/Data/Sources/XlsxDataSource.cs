using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Xml;

namespace TerraFluent.AutoAnalytics.Data.Sources;

/// <summary>
/// Minimal, dependency-free reader for the first worksheet of an <c>.xlsx</c> workbook, built on
/// <see cref="System.IO.Compression.ZipArchive"/> and <see cref="System.Xml.XmlReader"/> (both BCL).
/// Supports shared strings, inline strings, numbers and booleans. The first row is the header.
/// </summary>
/// <remarks>
/// Excel stores dates as styled serial numbers; without parsing cell styles they surface as numbers.
/// Provide dates as ISO text if reliable date typing is required, or plug in a richer adapter.
/// </remarks>
public sealed class XlsxDataSource : IDataSource
{
    private readonly Func<Stream> _streamFactory;
    private readonly string _name;

    public XlsxDataSource(byte[] xlsxBytes, string? name = null)
    {
        if (xlsxBytes is null) throw new ArgumentNullException(nameof(xlsxBytes));
        _streamFactory = () => new MemoryStream(xlsxBytes, writable: false);
        _name = name ?? "Workbook";
    }

    public static XlsxDataSource FromFile(string path)
    {
        if (string.IsNullOrWhiteSpace(path)) throw new ArgumentException("Path is required.", nameof(path));
        return new XlsxDataSource(File.ReadAllBytes(path), Path.GetFileNameWithoutExtension(path));
    }

    public Dataset Load()
    {
        using var stream = _streamFactory();
        using var archive = new ZipArchive(stream, ZipArchiveMode.Read);

        var sharedStrings = ReadSharedStrings(archive);
        var sheetEntry = archive.GetEntry("xl/worksheets/sheet1.xml")
                         ?? archive.Entries.FirstOrDefault(e => e.FullName.StartsWith("xl/worksheets/", StringComparison.OrdinalIgnoreCase));
        if (sheetEntry is null)
            return new Dataset(_name, Array.Empty<DataColumn>());

        var grid = ReadSheet(sheetEntry, sharedStrings);
        if (grid.Count == 0)
            return new Dataset(_name, Array.Empty<DataColumn>());

        var header = grid[0];
        int width = grid.Max(r => r.Length);
        var columnNames = new List<string>(width);
        for (int c = 0; c < width; c++)
        {
            string? h = c < header.Length ? header[c]?.ToString() : null;
            columnNames.Add(string.IsNullOrWhiteSpace(h) ? $"Column{c + 1}" : h!.Trim());
        }

        var rows = new List<object?[]>(grid.Count - 1);
        for (int r = 1; r < grid.Count; r++)
        {
            var src = grid[r];
            var row = new object?[width];
            for (int c = 0; c < width; c++)
                row[c] = c < src.Length ? src[c] : null;
            rows.Add(row);
        }

        return Dataset.FromRows(_name, columnNames, rows);
    }

    private static List<string> ReadSharedStrings(ZipArchive archive)
    {
        var result = new List<string>();
        var entry = archive.GetEntry("xl/sharedStrings.xml");
        if (entry is null) return result;

        using var s = entry.Open();
        using var reader = XmlReader.Create(s, new XmlReaderSettings { IgnoreWhitespace = false });
        var current = new System.Text.StringBuilder();
        bool inSi = false;
        while (reader.Read())
        {
            if (reader.NodeType == XmlNodeType.Element && reader.LocalName == "si")
            {
                current.Clear();
                inSi = true;
            }
            else if (reader.NodeType == XmlNodeType.Text && inSi)
            {
                current.Append(reader.Value);
            }
            else if (reader.NodeType == XmlNodeType.EndElement && reader.LocalName == "si")
            {
                result.Add(current.ToString());
                inSi = false;
            }
        }
        return result;
    }

    private static List<object?[]> ReadSheet(ZipArchiveEntry sheetEntry, List<string> sharedStrings)
    {
        var rows = new List<object?[]>();
        using var s = sheetEntry.Open();
        using var reader = XmlReader.Create(s, new XmlReaderSettings { IgnoreWhitespace = true });

        var rowCells = new List<(int col, object? value)>();
        string cellType = string.Empty;
        string cellRef = string.Empty;
        var text = new System.Text.StringBuilder();
        bool inValue = false;

        while (reader.Read())
        {
            if (reader.NodeType == XmlNodeType.Element)
            {
                switch (reader.LocalName)
                {
                    case "row":
                        rowCells.Clear();
                        break;
                    case "c":
                        cellRef  = reader.GetAttribute("r") ?? string.Empty;
                        cellType = reader.GetAttribute("t") ?? string.Empty;
                        break;
                    case "v":
                    case "t":
                        text.Clear();
                        inValue = true;
                        break;
                }
            }
            else if (reader.NodeType == XmlNodeType.Text && inValue)
            {
                text.Append(reader.Value);
            }
            else if (reader.NodeType == XmlNodeType.EndElement)
            {
                switch (reader.LocalName)
                {
                    case "v":
                    case "t":
                        inValue = false;
                        break;
                    case "c":
                        rowCells.Add((ColumnIndex(cellRef), DecodeCell(cellType, text.ToString(), sharedStrings)));
                        break;
                    case "row":
                        int width = rowCells.Count == 0 ? 0 : rowCells.Max(rc => rc.col) + 1;
                        var arr = new object?[width];
                        foreach (var (col, value) in rowCells)
                            if (col >= 0 && col < width) arr[col] = value;
                        rows.Add(arr);
                        break;
                }
            }
        }
        return rows;
    }

    private static object? DecodeCell(string type, string raw, List<string> sharedStrings)
    {
        if (string.IsNullOrEmpty(raw)) return null;
        switch (type)
        {
            case "s":
                return int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var idx)
                       && idx >= 0 && idx < sharedStrings.Count
                    ? sharedStrings[idx] : raw;
            case "str":
            case "inlineStr":
                return raw;
            case "b":
                return raw == "1";
            default:
                return double.TryParse(raw, NumberStyles.Any, CultureInfo.InvariantCulture, out var d) ? d : raw;
        }
    }

    // Converts an A1-style reference to a zero-based column index (e.g. "C5" -> 2).
    private static int ColumnIndex(string cellRef)
    {
        int index = 0;
        bool any = false;
        foreach (char ch in cellRef)
        {
            if (ch >= 'A' && ch <= 'Z') { index = index * 26 + (ch - 'A' + 1); any = true; }
            else if (ch >= 'a' && ch <= 'z') { index = index * 26 + (ch - 'a' + 1); any = true; }
            else break;
        }
        return any ? index - 1 : 0;
    }
}
