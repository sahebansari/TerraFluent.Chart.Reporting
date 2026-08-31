using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using TerraFluent.AutoAnalytics.Data;
using TerraFluent.AutoAnalytics.Data.Sources;
using TerraFluent.AutoAnalytics.Engine;
using TerraFluent.AutoAnalytics.Enums;
using Xunit;

namespace TerraFluent.AutoAnalytics.Tests;

/// <summary>
/// Covers the <c>.xlsx</c> ingestion path and the CSV serialiser that normalises it back to text.
/// Workbooks are synthesised in-test so the fixtures stay readable and no binary blobs are committed.
/// </summary>
public class XlsxAndCsvSerializerTests
{
    // ── Fixture builder ───────────────────────────────────────────────────────

    /// <summary>
    /// Writes a minimal but valid single-sheet workbook. Each cell is either a shared string
    /// (non-numeric) or an inline number, mirroring what Excel emits for a simple table.
    /// </summary>
    private static byte[] BuildWorkbook(IReadOnlyList<string> header, IReadOnlyList<object?[]> rows)
    {
        var shared = new List<string>();
        int SharedIndex(string s)
        {
            int i = shared.IndexOf(s);
            if (i >= 0) return i;
            shared.Add(s);
            return shared.Count - 1;
        }

        var sheet = new StringBuilder();
        sheet.Append("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
        sheet.Append("<worksheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\"><sheetData>");

        void AppendRow(int rowNumber, IReadOnlyList<object?> cells)
        {
            sheet.Append("<row r=\"").Append(rowNumber).Append("\">");
            for (int c = 0; c < cells.Count; c++)
            {
                string reference = $"{(char)('A' + c)}{rowNumber}";
                object? value = cells[c];
                if (value is null) continue;
                if (value is double or int)
                {
                    sheet.Append("<c r=\"").Append(reference).Append("\"><v>")
                         .Append(Convert.ToDouble(value).ToString(System.Globalization.CultureInfo.InvariantCulture))
                         .Append("</v></c>");
                }
                else
                {
                    sheet.Append("<c r=\"").Append(reference).Append("\" t=\"s\"><v>")
                         .Append(SharedIndex(value.ToString()!))
                         .Append("</v></c>");
                }
            }
            sheet.Append("</row>");
        }

        AppendRow(1, header.Cast<object?>().ToList());
        for (int r = 0; r < rows.Count; r++) AppendRow(r + 2, rows[r]);
        sheet.Append("</sheetData></worksheet>");

        var sharedXml = new StringBuilder();
        sharedXml.Append("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
        sharedXml.Append("<sst xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\" count=\"")
                 .Append(shared.Count).Append("\" uniqueCount=\"").Append(shared.Count).Append("\">");
        foreach (var s in shared)
            sharedXml.Append("<si><t>").Append(System.Security.SecurityElement.Escape(s)).Append("</t></si>");
        sharedXml.Append("</sst>");

        using var ms = new MemoryStream();
        using (var zip = new ZipArchive(ms, ZipArchiveMode.Create, leaveOpen: true))
        {
            void Write(string path, string content)
            {
                using var writer = new StreamWriter(zip.CreateEntry(path).Open(), new UTF8Encoding(false));
                writer.Write(content);
            }
            Write("xl/worksheets/sheet1.xml", sheet.ToString());
            Write("xl/sharedStrings.xml", sharedXml.ToString());
        }
        return ms.ToArray();
    }

    private static readonly string[] SalesHeader = { "Month", "Region", "Revenue", "Units" };
    private static readonly object?[][] SalesRows =
    {
        new object?[] { "2024-01", "EU", 12000d, 120d },
        new object?[] { "2024-02", "EU", 13500d, 131d },
        new object?[] { "2024-03", "EU", 14100d, 140d },
        new object?[] { "2024-01", "NA", 18000d, 175d },
        new object?[] { "2024-02", "NA", 19200d, 186d },
        new object?[] { "2024-03", "NA", 21000d, 203d },
    };

    private static byte[] SalesWorkbook() => BuildWorkbook(SalesHeader, SalesRows);

    // ── XlsxDataSource ────────────────────────────────────────────────────────

    [Fact]
    public void XlsxDataSource_ReadsHeaderRowsAndTypedCells()
    {
        var dataset = new XlsxDataSource(SalesWorkbook(), "Sales").Load();

        Assert.Equal("Sales", dataset.Name);
        Assert.Equal(4, dataset.ColumnCount);
        Assert.Equal(6, dataset.RowCount);
        Assert.Equal(SalesHeader, dataset.Columns.Select(c => c.Name));

        // Shared strings decode to text; numeric cells decode to doubles.
        Assert.Equal("2024-01", dataset.GetColumn("Month")!.Values[0]);
        Assert.Equal(12000d, dataset.GetColumn("Revenue")!.Values[0]);
        Assert.Equal(21000d, dataset.GetColumn("Revenue")!.Values[5]);
    }

    [Fact]
    public void XlsxDataSource_DefaultsMissingNameAndHandlesEmptyWorkbook()
    {
        var named = new XlsxDataSource(SalesWorkbook()).Load();
        Assert.Equal("Workbook", named.Name);

        // A zip with no worksheet yields an empty dataset rather than throwing.
        using var ms = new MemoryStream();
        using (var zip = new ZipArchive(ms, ZipArchiveMode.Create, leaveOpen: true))
            zip.CreateEntry("docProps/app.xml");
        var empty = new XlsxDataSource(ms.ToArray()).Load();
        Assert.Equal(0, empty.ColumnCount);
    }

    [Fact]
    public void XlsxDataSource_FillsGapsFromSparseRows()
    {
        // A row that omits its middle cell must still align to the header, leaving a null hole.
        byte[] bytes = BuildWorkbook(
            new[] { "A", "B", "C" },
            new[] { new object?[] { "x", null, 3d } });

        var dataset = new XlsxDataSource(bytes).Load();
        Assert.Equal(3, dataset.ColumnCount);
        Assert.Null(dataset.GetColumn("B")!.Values[0]);
        Assert.Equal(3d, dataset.GetColumn("C")!.Values[0]);
    }

    [Fact]
    public void XlsxDataSource_RejectsNullBytes() =>
        Assert.Throws<ArgumentNullException>(() => new XlsxDataSource(null!));

    // ── CsvSerializer ─────────────────────────────────────────────────────────

    [Fact]
    public void CsvSerializer_RoundTripsThroughCsvDataSource()
    {
        var original = new XlsxDataSource(SalesWorkbook(), "Sales").Load();
        string csv = CsvSerializer.ToCsv(original);

        Assert.StartsWith("Month,Region,Revenue,Units", csv);

        var reparsed = new CsvDataSource(csv).Load();
        Assert.Equal(original.ColumnCount, reparsed.ColumnCount);
        Assert.Equal(original.RowCount, reparsed.RowCount);
        Assert.Equal(
            original.Columns.Select(c => c.Name),
            reparsed.Columns.Select(c => c.Name));
    }

    [Fact]
    public void CsvSerializer_QuotesOnlyFieldsThatNeedIt()
    {
        var dataset = Dataset.FromRows("Q", new[] { "Plain", "Comma", "Quote", "Newline" }, new[]
        {
            new object?[] { "ok", "a,b", "say \"hi\"", "line1\nline2" }
        });

        string csv = CsvSerializer.ToCsv(dataset);
        string[] lines = csv.Split('\n');

        Assert.Equal("Plain,Comma,Quote,Newline", lines[0]);
        Assert.Contains("ok,\"a,b\",\"say \"\"hi\"\"\",\"line1\nline2\"", csv);
    }

    [Fact]
    public void CsvSerializer_FormatsValuesInvariantlyAndDatesAsIso()
    {
        var dataset = Dataset.FromRows("Types", new[] { "Num", "Flag", "When", "Stamp", "Nothing" }, new[]
        {
            new object?[]
            {
                1234.5d, true,
                new DateTime(2024, 3, 7),
                new DateTime(2024, 3, 7, 14, 30, 0),
                null
            }
        });

        string row = CsvSerializer.ToCsv(dataset).Split('\n')[1];
        Assert.Equal("1234.5,true,2024-03-07,2024-03-07 14:30:00,", row);
    }

    [Fact]
    public void CsvSerializer_ReturnsEmptyForColumnlessDatasetAndRejectsNull()
    {
        Assert.Equal(string.Empty, CsvSerializer.ToCsv(new Dataset("empty", Array.Empty<DataColumn>())));
        Assert.Throws<ArgumentNullException>(() => CsvSerializer.ToCsv(null!));
    }

    // ── End-to-end ────────────────────────────────────────────────────────────

    [Fact]
    public void Engine_AnalysesAWorkbookAsWellAsTheEquivalentCsv()
    {
        var fromXlsx = AnalyticsEngine.Analyze(new XlsxDataSource(SalesWorkbook(), "Sales"));
        var fromCsv = AnalyticsEngine.AnalyzeCsv(
            CsvSerializer.ToCsv(new XlsxDataSource(SalesWorkbook(), "Sales").Load()),
            new AnalyticsOptions { DatasetName = "Sales" });

        Assert.Equal(6, fromXlsx.Summary.RowCount);
        Assert.Equal(fromCsv.Summary.RowCount, fromXlsx.Summary.RowCount);
        Assert.Equal(fromCsv.Summary.MeasureCount, fromXlsx.Summary.MeasureCount);
        Assert.NotEmpty(fromXlsx.Insights);

        // The Month column must survive as a date dimension through both routes.
        Assert.Equal(ColumnType.Date, fromXlsx.Profile.Columns.Single(c => c.Name == "Month").Profile.Type);
        Assert.Equal(ColumnType.Date, fromCsv.Profile.Columns.Single(c => c.Name == "Month").Profile.Type);
    }
}
