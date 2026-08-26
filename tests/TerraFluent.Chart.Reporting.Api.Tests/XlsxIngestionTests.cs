using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace TerraFluent.Chart.Reporting.Api.Tests;

/// <summary>
/// Integration tests for the <c>.xlsx</c> ingestion routes: raw-bytes conversion via
/// <c>convert/xlsx</c> and inline base64 workbooks via <c>"format": "xlsx"</c>.
/// </summary>
public sealed class XlsxIngestionTests : IClassFixture<WebApplicationFactory<Program>>
{
    private const string XlsxMediaType =
        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

    private readonly WebApplicationFactory<Program> _factory;

    public XlsxIngestionTests(WebApplicationFactory<Program> factory) => _factory = factory;

    // ── Fixture ───────────────────────────────────────────────────────────────

    private static readonly string[] Header = { "Month", "Region", "Revenue" };
    private static readonly object?[][] Rows =
    {
        new object?[] { "2024-01", "NA", 12000d },
        new object?[] { "2024-02", "NA", 13500d },
        new object?[] { "2024-03", "EU", 15000d },
        new object?[] { "2024-04", "EU", 16400d },
        new object?[] { "2024-05", "APAC", 17100d },
        new object?[] { "2024-06", "APAC", 18300d },
    };

    /// <summary>Synthesises a minimal single-sheet workbook with a shared-string table.</summary>
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

        var sheet = new StringBuilder()
            .Append("<?xml version=\"1.0\" encoding=\"UTF-8\"?>")
            .Append("<worksheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\"><sheetData>");

        void AppendRow(int rowNumber, IReadOnlyList<object?> cells)
        {
            sheet.Append("<row r=\"").Append(rowNumber).Append("\">");
            for (int c = 0; c < cells.Count; c++)
            {
                object? value = cells[c];
                if (value is null) continue;
                string reference = $"{(char)('A' + c)}{rowNumber}";
                if (value is double d)
                    sheet.Append("<c r=\"").Append(reference).Append("\"><v>")
                         .Append(d.ToString(System.Globalization.CultureInfo.InvariantCulture)).Append("</v></c>");
                else
                    sheet.Append("<c r=\"").Append(reference).Append("\" t=\"s\"><v>")
                         .Append(SharedIndex(value.ToString()!)).Append("</v></c>");
            }
            sheet.Append("</row>");
        }

        AppendRow(1, header.Cast<object?>().ToList());
        for (int r = 0; r < rows.Count; r++) AppendRow(r + 2, rows[r]);
        sheet.Append("</sheetData></worksheet>");

        var sst = new StringBuilder()
            .Append("<?xml version=\"1.0\" encoding=\"UTF-8\"?>")
            .Append("<sst xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\">");
        foreach (var s in shared)
            sst.Append("<si><t>").Append(System.Security.SecurityElement.Escape(s)).Append("</t></si>");
        sst.Append("</sst>");

        using var ms = new MemoryStream();
        using (var zip = new ZipArchive(ms, ZipArchiveMode.Create, leaveOpen: true))
        {
            void Write(string path, string content)
            {
                using var writer = new StreamWriter(zip.CreateEntry(path).Open(), new UTF8Encoding(false));
                writer.Write(content);
            }
            Write("xl/worksheets/sheet1.xml", sheet.ToString());
            Write("xl/sharedStrings.xml", sst.ToString());
        }
        return ms.ToArray();
    }

    private static byte[] SalesWorkbook() => BuildWorkbook(Header, Rows);

    private static ByteArrayContent Binary(byte[] bytes, string mediaType = XlsxMediaType)
    {
        var content = new ByteArrayContent(bytes);
        content.Headers.ContentType = new MediaTypeHeaderValue(mediaType);
        return content;
    }

    // ── convert/xlsx ──────────────────────────────────────────────────────────

    [Fact]
    public async Task ConvertXlsx_ReturnsCsvWithCountsAndName()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsync(
            "/api/analytics/convert/xlsx?datasetName=Q1%20Sales", Binary(SalesWorkbook()));

        response.EnsureSuccessStatusCode();
        var root = JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;

        Assert.Equal("Q1 Sales", root.GetProperty("datasetName").GetString());
        Assert.Equal("Csv", root.GetProperty("format").GetString());
        Assert.Equal(6, root.GetProperty("rowCount").GetInt32());
        Assert.Equal(3, root.GetProperty("columnCount").GetInt32());

        string csv = root.GetProperty("data").GetString()!;
        Assert.StartsWith("Month,Region,Revenue", csv);
        Assert.Contains("2024-01,NA,12000", csv);
    }

    [Fact]
    public async Task ConvertXlsx_OutputFeedsStraightIntoAnalyze()
    {
        var client = _factory.CreateClient();

        var converted = await client.PostAsync("/api/analytics/convert/xlsx", Binary(SalesWorkbook()));
        converted.EnsureSuccessStatusCode();
        string csv = JsonDocument.Parse(await converted.Content.ReadAsStringAsync())
            .RootElement.GetProperty("data").GetString()!;

        var analyzed = await client.PostAsJsonAsync("/api/analytics/analyze",
            new { data = csv, datasetName = "From Workbook" });

        analyzed.EnsureSuccessStatusCode();
        var root = JsonDocument.Parse(await analyzed.Content.ReadAsStringAsync()).RootElement;
        Assert.Equal(6, root.GetProperty("summary").GetProperty("rowCount").GetInt32());
        Assert.True(root.GetProperty("insights").GetArrayLength() > 0);
    }

    [Fact]
    public async Task ConvertXlsx_RejectsEmptyBody()
    {
        var client = _factory.CreateClient();
        var response = await client.PostAsync("/api/analytics/convert/xlsx", Binary(Array.Empty<byte>()));
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task ConvertXlsx_RejectsNonWorkbookBytes()
    {
        var client = _factory.CreateClient();
        var response = await client.PostAsync("/api/analytics/convert/xlsx",
            Binary(Encoding.UTF8.GetBytes("Month,Region\n2024-01,NA\n")));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // ── inline base64 workbooks ───────────────────────────────────────────────

    [Fact]
    public async Task Analyze_WithExplicitXlsxFormat_ReadsBase64Workbook()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/analytics/analyze", new
        {
            data = Convert.ToBase64String(SalesWorkbook()),
            format = "xlsx",
            datasetName = "Inline Workbook"
        });

        response.EnsureSuccessStatusCode();
        var root = JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;

        Assert.Equal("Inline Workbook", root.GetProperty("summary").GetProperty("datasetName").GetString());
        Assert.Equal(6, root.GetProperty("summary").GetProperty("rowCount").GetInt32());
        Assert.Equal(3, root.GetProperty("columns").GetArrayLength());
    }

    [Fact]
    public async Task Analyze_AutoDetectsBase64WorkbookFromZipSignature()
    {
        var client = _factory.CreateClient();

        // No explicit format: the base64 "UEsDB" prefix (ZIP "PK\x03\x04") must identify it as xlsx.
        var response = await client.PostAsJsonAsync("/api/analytics/analyze",
            new { data = Convert.ToBase64String(SalesWorkbook()) });

        response.EnsureSuccessStatusCode();
        var root = JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;
        Assert.Equal(6, root.GetProperty("summary").GetProperty("rowCount").GetInt32());
    }

    [Fact]
    public async Task Analyze_WithMalformedBase64_ReturnsBadRequest()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/analytics/analyze",
            new { data = "not base64 at all!!", format = "xlsx" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Ask_AcceptsAWorkbookAndAnswersAQuestion()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/analytics/ask", new
        {
            data = Convert.ToBase64String(SalesWorkbook()),
            format = "xlsx",
            question = "what is the trend in revenue?"
        });

        response.EnsureSuccessStatusCode();
        var root = JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;
        Assert.False(string.IsNullOrWhiteSpace(root.GetProperty("headline").GetString()));
    }
}
