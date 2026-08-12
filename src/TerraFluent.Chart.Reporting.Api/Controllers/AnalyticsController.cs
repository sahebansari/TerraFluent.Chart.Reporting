using Microsoft.AspNetCore.Mvc;
using TerraFluent.AutoAnalytics.Dashboard;
using TerraFluent.AutoAnalytics.Data;
using TerraFluent.AutoAnalytics.Data.Sources;
using TerraFluent.AutoAnalytics.Engine;
using TerraFluent.Chart.Reporting.Api.Models;
using TerraFluent.Chart.Reporting.Api.Rendering;

namespace TerraFluent.Chart.Reporting.Api.Controllers;

/// <summary>
/// Deterministic auto-analytics as a service. POST raw CSV or JSON data and receive schema
/// profiling, data-quality validation, ranked insights, anomalies, chart recommendations and a
/// fully-assembled auto dashboard — no AI, fully reproducible.
/// </summary>
[ApiController]
[Route("api/analytics")]
[Produces("application/json")]
public sealed class AnalyticsController : ControllerBase
{
    private readonly AnalyticsEngine _engine;

    public AnalyticsController(AnalyticsEngine engine) => _engine = engine;

    // ── Full analysis ─────────────────────────────────────────────────────────

    /// <summary>
    /// Analyses raw tabular data and returns profiling, validation, insights and chart recommendations.
    /// </summary>
    /// <remarks>
    /// Supply the data inline in the request body:
    /// ```json
    /// {
    ///   "format": "csv",
    ///   "datasetName": "Q1 Sales",
    ///   "data": "Month,Region,Revenue\n2024-01,NA,12000\n2024-02,NA,13500"
    /// }
    /// ```
    /// Set <c>?includeSvg=true</c> to embed a rendered SVG for each recommended chart.
    /// </remarks>
    [HttpPost("analyze")]
    [ProducesResponseType(typeof(AnalyticsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public IActionResult Analyze([FromBody] AnalyzeRequest request, [FromQuery] bool includeSvg = false)
    {
        var result = RunAnalysis(request);
        return Ok(AnalyticsResponse.From(result, includeSvg));
    }

    /// <summary>
    /// Analyses a raw CSV payload posted directly as the request body (<c>text/csv</c> or <c>text/plain</c>).
    /// Tuning options are supplied via query-string parameters.
    /// </summary>
    [HttpPost("analyze/csv")]
    [Consumes("text/csv", "text/plain")]
    [ProducesResponseType(typeof(AnalyticsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> AnalyzeCsv(
        [FromQuery] string? datasetName = null,
        [FromQuery] bool includeSvg = false)
    {
        string data = await ReadBodyAsync();
        var result = RunAnalysis(new AnalyzeRequest { Data = data, Format = AnalyzeFormat.Csv, DatasetName = datasetName });
        return Ok(AnalyticsResponse.From(result, includeSvg));
    }

    /// <summary>
    /// Analyses a raw JSON array posted directly as the request body.
    /// Tuning options are supplied via query-string parameters.
    /// </summary>
    [HttpPost("analyze/json")]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(AnalyticsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> AnalyzeJson(
        [FromQuery] string? datasetName = null,
        [FromQuery] bool includeSvg = false)
    {
        string data = await ReadBodyAsync();
        var result = RunAnalysis(new AnalyzeRequest { Data = data, Format = AnalyzeFormat.Json, DatasetName = datasetName });
        return Ok(AnalyticsResponse.From(result, includeSvg));
    }

    // ── Focused slices ────────────────────────────────────────────────────────

    /// <summary>Returns only the ranked insights and executive summary for the supplied data.</summary>
    [HttpPost("insights")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public IActionResult Insights([FromBody] AnalyzeRequest request)
    {
        var result = RunAnalysis(request);
        return Ok(new
        {
            summary = SummaryDto.From(result.Summary),
            insights = result.Insights.Select(InsightDto.From)
        });
    }

    /// <summary>Returns only the data-quality validation report for the supplied data.</summary>
    [HttpPost("validate")]
    [ProducesResponseType(typeof(ValidationDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public IActionResult Validate([FromBody] AnalyzeRequest request)
    {
        var result = RunAnalysis(request);
        return Ok(ValidationDto.From(result.Validation));
    }

    // ── Dashboard ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Builds an auto dashboard (KPIs, trend/comparison/distribution charts, anomalies, insights)
    /// and returns it as structured JSON with each chart pre-rendered to SVG.
    /// </summary>
    [HttpPost("dashboard")]
    [ProducesResponseType(typeof(DashboardResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public IActionResult Dashboard([FromBody] AnalyzeRequest request, [FromQuery] int maxKpis = 4)
    {
        var result = RunAnalysis(request);
        var dashboard = DashboardBuilder.Generate(result, maxKpis);
        return Ok(DashboardResponse.From(dashboard, result));
    }

    /// <summary>
    /// Builds an auto dashboard and returns a complete, self-contained HTML page ready to display
    /// in a browser or embed in a report.
    /// </summary>
    [HttpPost("dashboard/html")]
    [Produces("text/html")]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public IActionResult DashboardHtml([FromBody] AnalyzeRequest request, [FromQuery] int maxKpis = 4)
    {
        var result = RunAnalysis(request);
        var dashboard = DashboardBuilder.Generate(result, maxKpis);
        return Content(DashboardHtmlRenderer.Render(dashboard, result), "text/html");
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private AnalyticsResult RunAnalysis(AnalyzeRequest request)
    {
        if (request is null || string.IsNullOrWhiteSpace(request.Data))
            throw new ArgumentException("Request body must include non-empty 'data' to analyse.");

        var options = new AnalyticsOptions { DatasetName = request.DatasetName };
        if (request.MaxInsights is int mi) options.MaxInsights = mi;
        if (request.MaxRecommendations is int mr) options.MaxRecommendations = mr;
        if (request.MaxGroupCombinations is int mg) options.MaxGroupCombinations = mg;
        if (request.ZScoreThreshold is double z) options.ZScoreThreshold = z;
        options.ThrowOnValidationError = request.ThrowOnValidationError;

        IDataSource source = ResolveFormat(request) switch
        {
            AnalyzeFormat.Json => new JsonDataSource(request.Data),
            _                  => new CsvDataSource(request.Data)
        };

        return _engine.Run(source.Load(), options);
    }

    // Auto-detects CSV vs JSON when the caller did not specify a concrete format.
    private static AnalyzeFormat ResolveFormat(AnalyzeRequest request)
    {
        if (request.Format != AnalyzeFormat.Auto) return request.Format;
        string trimmed = request.Data.TrimStart();
        return trimmed.StartsWith('[') || trimmed.StartsWith('{')
            ? AnalyzeFormat.Json
            : AnalyzeFormat.Csv;
    }

    private async Task<string> ReadBodyAsync()
    {
        using var reader = new StreamReader(Request.Body);
        return await reader.ReadToEndAsync();
    }
}
