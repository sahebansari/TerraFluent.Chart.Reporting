using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace TerraFluent.Chart.Reporting.Api.Tests;

/// <summary>
/// Integration tests for the analytics-as-a-service endpoints, exercised through the real HTTP
/// pipeline via <see cref="WebApplicationFactory{TEntryPoint}"/>.
/// </summary>
public sealed class AnalyticsEndpointsTests : IClassFixture<WebApplicationFactory<Program>>
{
    private const string SampleCsv =
        "Month,Region,Revenue,Cost\n" +
        "2024-01,NA,12000,7000\n" +
        "2024-02,NA,13500,7600\n" +
        "2024-03,EU,15000,8100\n" +
        "2024-04,EU,42000,8300\n" +
        "2024-05,APAC,16500,8800\n" +
        "2024-06,APAC,17200,9100\n";

    private const string SampleJson =
        "[{\"Month\":\"2024-01\",\"Region\":\"NA\",\"Revenue\":12000}," +
        "{\"Month\":\"2024-02\",\"Region\":\"NA\",\"Revenue\":13500}," +
        "{\"Month\":\"2024-03\",\"Region\":\"EU\",\"Revenue\":15000}]";

    private static readonly JsonSerializerOptions JsonOpts = new(JsonSerializerDefaults.Web);

    private readonly WebApplicationFactory<Program> _factory;

    public AnalyticsEndpointsTests(WebApplicationFactory<Program> factory) => _factory = factory;

    // ── analyze (structured body) ──────────────────────────────────────────────

    [Fact]
    public async Task Analyze_WithCsv_ReturnsSummaryInsightsAndRecommendations()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/analytics/analyze",
            new { data = SampleCsv, datasetName = "Sales" });

        response.EnsureSuccessStatusCode();
        var doc = await ReadJsonAsync(response);
        var root = doc.RootElement;

        Assert.Equal("Sales", root.GetProperty("summary").GetProperty("datasetName").GetString());
        Assert.Equal(6, root.GetProperty("summary").GetProperty("rowCount").GetInt32());
        Assert.True(root.GetProperty("insights").GetArrayLength() > 0);
        Assert.True(root.GetProperty("recommendations").GetArrayLength() > 0);
        Assert.True(root.GetProperty("columns").GetArrayLength() == 4);
    }

    [Fact]
    public async Task Analyze_WithFormatAsEnumName_IsAccepted()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/analytics/analyze",
            new { data = SampleCsv, format = "csv" });

        response.EnsureSuccessStatusCode();
        var doc = await ReadJsonAsync(response);
        Assert.Equal(6, doc.RootElement.GetProperty("summary").GetProperty("rowCount").GetInt32());
    }

    [Fact]
    public async Task Analyze_WithIncludeSvg_EmbedsRenderedSvg()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/analytics/analyze?includeSvg=true",
            new { data = SampleCsv });

        response.EnsureSuccessStatusCode();
        var doc = await ReadJsonAsync(response);
        var firstRec = doc.RootElement.GetProperty("recommendations")[0];
        string svg = firstRec.GetProperty("svg").GetString()!;
        Assert.Contains("<svg", svg, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Analyze_WithoutSvg_LeavesSvgNull()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/analytics/analyze",
            new { data = SampleCsv });

        response.EnsureSuccessStatusCode();
        var doc = await ReadJsonAsync(response);
        var firstRec = doc.RootElement.GetProperty("recommendations")[0];
        Assert.Equal(JsonValueKind.Null, firstRec.GetProperty("svg").ValueKind);
    }

    [Fact]
    public async Task Analyze_WithAutoDetectedJson_Works()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/analytics/analyze",
            new { data = SampleJson });

        response.EnsureSuccessStatusCode();
        var doc = await ReadJsonAsync(response);
        Assert.Equal(3, doc.RootElement.GetProperty("summary").GetProperty("rowCount").GetInt32());
    }

    [Fact]
    public async Task Analyze_WithEmptyData_ReturnsBadRequest()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/analytics/analyze",
            new { data = "" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Analyze_WithMalformedJson_ReturnsBadRequest()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/analytics/analyze",
            new { data = "[{ this is not valid json", format = "json" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // ── analyze (raw body) ─────────────────────────────────────────────────────

    [Fact]
    public async Task AnalyzeCsvRaw_WithTextCsvBody_Works()
    {
        var client = _factory.CreateClient();
        var content = new StringContent(SampleCsv, Encoding.UTF8, "text/csv");

        var response = await client.PostAsync("/api/analytics/analyze/csv?datasetName=Raw", content);

        response.EnsureSuccessStatusCode();
        var doc = await ReadJsonAsync(response);
        Assert.Equal("Raw", doc.RootElement.GetProperty("summary").GetProperty("datasetName").GetString());
    }

    [Fact]
    public async Task AnalyzeJsonRaw_WithJsonBody_Works()
    {
        var client = _factory.CreateClient();
        var content = new StringContent(SampleJson, Encoding.UTF8, "application/json");

        var response = await client.PostAsync("/api/analytics/analyze/json", content);

        response.EnsureSuccessStatusCode();
        var doc = await ReadJsonAsync(response);
        Assert.Equal(3, doc.RootElement.GetProperty("summary").GetProperty("rowCount").GetInt32());
    }

    // ── focused slices ─────────────────────────────────────────────────────────

    [Fact]
    public async Task Insights_ReturnsSummaryAndInsights()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/analytics/insights",
            new { data = SampleCsv });

        response.EnsureSuccessStatusCode();
        var doc = await ReadJsonAsync(response);
        Assert.True(doc.RootElement.GetProperty("insights").GetArrayLength() > 0);
        Assert.False(string.IsNullOrEmpty(
            doc.RootElement.GetProperty("summary").GetProperty("headline").GetString()));
    }

    [Fact]
    public async Task Validate_ReturnsDataQualityReport()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/analytics/validate",
            new { data = SampleCsv });

        response.EnsureSuccessStatusCode();
        var doc = await ReadJsonAsync(response);
        Assert.True(doc.RootElement.TryGetProperty("isClean", out _));
        Assert.True(doc.RootElement.TryGetProperty("issues", out _));
    }

    // ── dashboard ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task Dashboard_ReturnsChartsWithSvg()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/analytics/dashboard",
            new { data = SampleCsv });

        response.EnsureSuccessStatusCode();
        var doc = await ReadJsonAsync(response);
        var root = doc.RootElement;

        Assert.True(root.GetProperty("kpis").GetArrayLength() > 0);
        int chartSections =
            root.GetProperty("trendCharts").GetArrayLength() +
            root.GetProperty("comparisonCharts").GetArrayLength() +
            root.GetProperty("distributionCharts").GetArrayLength();
        Assert.True(chartSections > 0);

        // Every rendered chart must carry inline SVG.
        foreach (var section in new[] { "trendCharts", "comparisonCharts", "distributionCharts" })
            foreach (var chart in root.GetProperty(section).EnumerateArray())
                Assert.Contains("<svg", chart.GetProperty("svg").GetString()!, StringComparison.Ordinal);
    }

    [Fact]
    public async Task DashboardHtml_ReturnsSelfContainedHtmlPage()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/analytics/dashboard/html",
            new { data = SampleCsv });

        response.EnsureSuccessStatusCode();
        Assert.Equal("text/html", response.Content.Headers.ContentType?.MediaType);

        string html = await response.Content.ReadAsStringAsync();
        Assert.Contains("<!DOCTYPE html>", html, StringComparison.Ordinal);
        Assert.Contains("<svg", html, StringComparison.Ordinal);
    }

    // ── helpers ────────────────────────────────────────────────────────────────

    private static async Task<JsonDocument> ReadJsonAsync(HttpResponseMessage response)
        => JsonDocument.Parse(await response.Content.ReadAsStringAsync());
}
