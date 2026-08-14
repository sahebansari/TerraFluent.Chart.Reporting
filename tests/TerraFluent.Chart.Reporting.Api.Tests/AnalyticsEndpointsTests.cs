using System.Net;
using System.Net.Http.Json;
using System.Linq;
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

    // ── agent (ask) ────────────────────────────────────────────────────────────

    [Fact]
    public async Task Ask_OpenEnded_ReturnsTraceWithStepsAndInsights()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/analytics/ask",
            new { data = SampleCsv });

        response.EnsureSuccessStatusCode();
        var doc = await ReadJsonAsync(response);
        var root = doc.RootElement;

        Assert.False(string.IsNullOrEmpty(root.GetProperty("headline").GetString()));
        Assert.True(root.GetProperty("steps").GetArrayLength() > 0);
        Assert.True(root.GetProperty("insights").GetArrayLength() > 0);
    }

    [Fact]
    public async Task Ask_ForecastQuestion_ProducesForecastStep()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/analytics/ask?includeSvg=true",
            new { data = SampleCsv, question = "forecast Revenue" });

        response.EnsureSuccessStatusCode();
        var doc = await ReadJsonAsync(response);
        var steps = doc.RootElement.GetProperty("steps");

        bool hasForecast = steps.EnumerateArray().Any(s => s.GetProperty("skill").GetString() == "Forecast");
        Assert.True(hasForecast);

        // includeSvg should embed rendered SVG on the supporting charts.
        var charts = doc.RootElement.GetProperty("charts");
        if (charts.GetArrayLength() > 0)
            Assert.Contains("<svg", charts[0].GetProperty("svg").GetString()!, StringComparison.Ordinal);
    }

    // ── aggregate & filter ───────────────────────────────────────────────────────

    [Fact]
    public async Task Aggregate_ByRegion_ReturnsBuckets()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/api/analytics/aggregate?measure=Revenue&dimension=Region&aggregation=Sum",
            new { data = SampleCsv });

        response.EnsureSuccessStatusCode();
        var doc = await ReadJsonAsync(response);
        var root = doc.RootElement;

        Assert.False(root.GetProperty("isPivot").GetBoolean());
        Assert.True(root.GetProperty("buckets").GetArrayLength() > 0);
        Assert.Equal("Sum", root.GetProperty("aggregation").GetString());
    }

    [Fact]
    public async Task Aggregate_MissingMeasure_ReturnsBadRequest()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/api/analytics/aggregate?dimension=Region",
            new { data = SampleCsv });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Analyze_WithFilter_RestrictsRows()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/analytics/analyze",
            new { data = SampleCsv, filter = "Region = NA" });

        response.EnsureSuccessStatusCode();
        var doc = await ReadJsonAsync(response);
        // SampleCsv has 2 NA rows out of 6.
        Assert.Equal(2, doc.RootElement.GetProperty("summary").GetProperty("rowCount").GetInt32());
    }

    // ── sessions (multi-turn) ────────────────────────────────────────────────────

    [Fact]
    public async Task Session_CreateThenAsk_BuildsOnPriorTurns()
    {
        var client = _factory.CreateClient();

        var createResponse = await client.PostAsJsonAsync("/api/analytics/sessions",
            new { data = SampleCsv });
        createResponse.EnsureSuccessStatusCode();
        var created = await ReadJsonAsync(createResponse);
        string sessionId = created.RootElement.GetProperty("sessionId").GetString()!;
        Assert.False(string.IsNullOrEmpty(sessionId));

        // First turn: open-ended explore.
        var firstResponse = await client.PostAsJsonAsync($"/api/analytics/sessions/{sessionId}/ask",
            new { question = (string?)null });
        firstResponse.EnsureSuccessStatusCode();
        var first = await ReadJsonAsync(firstResponse);
        Assert.True(first.RootElement.GetProperty("insights").GetArrayLength() > 0);

        // Second identical turn: memory suppresses already-reported insights.
        var secondResponse = await client.PostAsJsonAsync($"/api/analytics/sessions/{sessionId}/ask",
            new { question = (string?)null });
        secondResponse.EnsureSuccessStatusCode();
        var second = await ReadJsonAsync(secondResponse);
        Assert.Equal(0, second.RootElement.GetProperty("insights").GetArrayLength());
    }

    [Fact]
    public async Task Session_UnknownId_ReturnsNotFound()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/analytics/sessions/does-not-exist/ask",
            new { question = "explore" });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    // ── hardening: validation, headers, versioning, aggregate pivot ──────────────

    [Fact]
    public async Task Analyze_WithOutOfRangeMaxInsights_ReturnsBadRequest()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/analytics/analyze",
            new { data = SampleCsv, maxInsights = 0 });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Responses_IncludeSecurityHeaders()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/analytics/analyze",
            new { data = SampleCsv });

        response.EnsureSuccessStatusCode();
        Assert.True(response.Headers.Contains("X-Content-Type-Options"));
        Assert.Equal("nosniff", response.Headers.GetValues("X-Content-Type-Options").First());
        Assert.True(response.Headers.Contains("X-Frame-Options"));
    }

    [Fact]
    public async Task V1RouteAlias_Works()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/v1/analytics/analyze",
            new { data = SampleCsv });

        response.EnsureSuccessStatusCode();
        var doc = await ReadJsonAsync(response);
        Assert.Equal(6, doc.RootElement.GetProperty("summary").GetProperty("rowCount").GetInt32());
    }

    [Fact]
    public async Task Aggregate_TwoDimensions_ReturnsPivot()
    {
        var client = _factory.CreateClient();
        const string pivotCsv =
            "Region,Product,Revenue\n" +
            "East,Alpha,12000\nWest,Beta,7000\nEast,Beta,15000\nWest,Alpha,8000\n";

        var response = await client.PostAsJsonAsync(
            "/api/analytics/aggregate?measure=Revenue&dimension=Region&secondDimension=Product&aggregation=Sum",
            new { data = pivotCsv });

        response.EnsureSuccessStatusCode();
        var doc = await ReadJsonAsync(response);
        var root = doc.RootElement;

        Assert.True(root.GetProperty("isPivot").GetBoolean());
        Assert.True(root.GetProperty("cells").GetArrayLength() > 0);
    }

    [Fact]
    public async Task Ask_CompareQuestion_ProducesComparisonStep()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/analytics/ask",
            new { data = SampleCsv, question = "compare Revenue to last month" });

        response.EnsureSuccessStatusCode();
        var doc = await ReadJsonAsync(response);
        bool hasComparison = doc.RootElement.GetProperty("steps").EnumerateArray()
            .Any(s => s.GetProperty("skill").GetString() == "Comparison");
        Assert.True(hasComparison);
    }

    [Fact]
    public async Task Session_AskWithQuestion_BuildsFocusedTrace()
    {
        var client = _factory.CreateClient();

        var create = await client.PostAsJsonAsync("/api/analytics/sessions", new { data = SampleCsv });
        create.EnsureSuccessStatusCode();
        string id = (await ReadJsonAsync(create)).RootElement.GetProperty("sessionId").GetString()!;

        var ask = await client.PostAsJsonAsync($"/api/analytics/sessions/{id}/ask",
            new { question = "forecast Revenue" });
        ask.EnsureSuccessStatusCode();
        var doc = await ReadJsonAsync(ask);
        bool hasForecast = doc.RootElement.GetProperty("steps").EnumerateArray()
            .Any(s => s.GetProperty("skill").GetString() == "Forecast");
        Assert.True(hasForecast);
    }

    [Fact]
    public async Task Health_LiveAndReady_RespondOk()
    {
        var client = _factory.CreateClient();

        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/health/live")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/health/ready")).StatusCode);
    }

    [Fact]
    public async Task AnalyzeCsvRaw_WithFilterAndTuning_IsHonoured()
    {
        var client = _factory.CreateClient();
        var content = new StringContent(SampleCsv, Encoding.UTF8, "text/csv");

        var response = await client.PostAsync(
            "/api/analytics/analyze/csv?filter=Region%20%3D%20NA&maxInsights=5", content);

        response.EnsureSuccessStatusCode();
        var doc = await ReadJsonAsync(response);
        // Filter applied (2 NA rows) and analysis still succeeds.
        Assert.Equal(2, doc.RootElement.GetProperty("summary").GetProperty("rowCount").GetInt32());
    }

    [Fact]
    public async Task AnalyzeCsvRaw_WithOutOfRangeTuning_ReturnsBadRequest()
    {
        var client = _factory.CreateClient();
        var content = new StringContent(SampleCsv, Encoding.UTF8, "text/csv");

        var response = await client.PostAsync("/api/analytics/analyze/csv?maxInsights=0", content);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // ── helpers ────────────────────────────────────────────────────────────────

    private static async Task<JsonDocument> ReadJsonAsync(HttpResponseMessage response)
        => JsonDocument.Parse(await response.Content.ReadAsStringAsync());
}
