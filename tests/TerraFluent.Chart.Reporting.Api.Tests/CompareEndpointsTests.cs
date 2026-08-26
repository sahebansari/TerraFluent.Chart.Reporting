using System;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace TerraFluent.Chart.Reporting.Api.Tests;

/// <summary>Integration tests for the two-dataset comparison endpoints.</summary>
public sealed class CompareEndpointsTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public CompareEndpointsTests(WebApplicationFactory<Program> factory) => _factory = factory;

    private static string SalesCsv(double eu, double na, int months = 6)
    {
        var sb = new StringBuilder("Month,Region,Revenue\n");
        var start = new DateTime(2024, 1, 1);
        for (int m = 0; m < months; m++)
        {
            string month = start.AddMonths(m).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            sb.Append(month).Append(",EU,").Append(eu.ToString(CultureInfo.InvariantCulture)).Append('\n');
            sb.Append(month).Append(",NA,").Append(na.ToString(CultureInfo.InvariantCulture)).Append('\n');
        }
        return sb.ToString();
    }

    private static object Request(string baselineCsv, string currentCsv) => new
    {
        baseline = new { data = baselineCsv, datasetName = "Q1" },
        current = new { data = currentCsv, datasetName = "Q2" }
    };

    [Fact]
    public async Task Compare_ReturnsHeadlineMeasureDeltasAndInsights()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/analytics/compare",
            Request(SalesCsv(10_000, 20_000), SalesCsv(12_000, 24_000)));

        response.EnsureSuccessStatusCode();
        var root = JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;

        Assert.Equal("Q1", root.GetProperty("baselineName").GetString());
        Assert.Equal("Q2", root.GetProperty("currentName").GetString());
        Assert.True(root.GetProperty("isComparable").GetBoolean());
        Assert.Contains("Revenue", root.GetProperty("headline").GetString()!);

        var deltas = root.GetProperty("measureDeltas");
        Assert.True(deltas.GetArrayLength() > 0);
        var revenue = deltas.EnumerateArray().First(d => d.GetProperty("measure").GetString() == "Revenue");
        Assert.Equal("total", revenue.GetProperty("statistic").GetString());
        Assert.Equal(180_000, revenue.GetProperty("baselineValue").GetDouble());
        Assert.Equal(216_000, revenue.GetProperty("currentValue").GetDouble());

        Assert.True(root.GetProperty("insights").GetArrayLength() > 0);
    }

    [Fact]
    public async Task Compare_EmbedsChartSvgWhenRequested()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/analytics/compare?includeSvg=true",
            Request(SalesCsv(10_000, 20_000), SalesCsv(20_000, 20_000)));

        response.EnsureSuccessStatusCode();
        var root = JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;

        var charts = root.GetProperty("charts");
        Assert.True(charts.GetArrayLength() > 0);
        foreach (var chart in charts.EnumerateArray())
            Assert.StartsWith("<svg", chart.GetProperty("svg").GetString());
    }

    [Fact]
    public async Task Compare_ReportsCategoryMixShifts()
    {
        var client = _factory.CreateClient();

        // EU moves from a third of revenue to half.
        var response = await client.PostAsJsonAsync("/api/analytics/compare",
            Request(SalesCsv(10_000, 20_000), SalesCsv(20_000, 20_000)));

        response.EnsureSuccessStatusCode();
        var root = JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;

        var shifts = root.GetProperty("categoryShifts").EnumerateArray().ToList();
        Assert.NotEmpty(shifts);
        var eu = shifts.First(s => s.GetProperty("category").GetString() == "EU");
        Assert.True(eu.GetProperty("shareDelta").GetDouble() > 0.1);
    }

    [Fact]
    public async Task Compare_SaysWhenTheDatasetsCannotBeCompared()
    {
        var client = _factory.CreateClient();

        const string sales = "Month,Region,Revenue\n2024-01,EU,100\n2024-02,EU,110\n2024-03,EU,120\n";
        const string weather = "Day,City,Temperature\n2024-01-01,Oslo,-2\n2024-01-02,Oslo,-1\n2024-01-03,Oslo,0\n";

        var response = await client.PostAsJsonAsync("/api/analytics/compare", Request(sales, weather));

        response.EnsureSuccessStatusCode();
        var root = JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;

        Assert.False(root.GetProperty("isComparable").GetBoolean());
        Assert.Equal(0, root.GetProperty("measureDeltas").GetArrayLength());
        Assert.Contains("share no measure",
            root.GetProperty("compatibilityNote").GetString()!, StringComparison.OrdinalIgnoreCase);
        // The structural diff still tells the caller why.
        Assert.True(root.GetProperty("schemaChanges").GetArrayLength() > 0);
    }

    [Fact]
    public async Task Compare_AcceptsMixedFormatsOnEachSide()
    {
        var client = _factory.CreateClient();

        const string json = "[{\"Month\":\"2024-01\",\"Region\":\"EU\",\"Revenue\":10000}," +
                            "{\"Month\":\"2024-02\",\"Region\":\"EU\",\"Revenue\":11000}," +
                            "{\"Month\":\"2024-03\",\"Region\":\"EU\",\"Revenue\":12000}]";

        var response = await client.PostAsJsonAsync("/api/analytics/compare", new
        {
            baseline = new { data = SalesCsv(10_000, 20_000, months: 3), datasetName = "CSV side" },
            current = new { data = json, datasetName = "JSON side" }
        });

        response.EnsureSuccessStatusCode();
        var root = JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;
        Assert.True(root.GetProperty("isComparable").GetBoolean());
    }

    [Fact]
    public async Task Compare_RejectsAMissingSide()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/analytics/compare", new
        {
            baseline = new { data = SalesCsv(10_000, 20_000) },
            current = new { data = "" }
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CompareHtml_ReturnsASelfContainedPage()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/analytics/compare/html",
            Request(SalesCsv(10_000, 20_000), SalesCsv(12_000, 24_000)));

        response.EnsureSuccessStatusCode();
        Assert.Equal("text/html", response.Content.Headers.ContentType?.MediaType);

        string html = await response.Content.ReadAsStringAsync();
        Assert.StartsWith("<!DOCTYPE html>", html);
        Assert.Contains("Q1 vs Q2", html);
        Assert.Contains("<svg", html);
        // No external requests: fonts are embedded, so the export works offline.
        Assert.DoesNotContain("http://fonts.", html);
        Assert.DoesNotContain("https://fonts.", html);
    }

    [Fact]
    public async Task Compare_IsDeterministicAcrossRequests()
    {
        var client = _factory.CreateClient();
        var request = Request(SalesCsv(10_000, 20_000), SalesCsv(12_000, 24_000));

        var first = await client.PostAsJsonAsync("/api/analytics/compare", request);
        var second = await client.PostAsJsonAsync("/api/analytics/compare", request);

        Assert.Equal(await first.Content.ReadAsStringAsync(), await second.Content.ReadAsStringAsync());
    }
}
