using Microsoft.AspNetCore.Mvc;
using TerraFluent.AutoAnalytics.Agent;
using TerraFluent.AutoAnalytics.Analytics;
using TerraFluent.AutoAnalytics.Dashboard;
using TerraFluent.AutoAnalytics.Data;
using TerraFluent.AutoAnalytics.Data.Sources;
using TerraFluent.AutoAnalytics.Engine;
using TerraFluent.AutoAnalytics.Enums;
using TerraFluent.Chart.Reporting.Api.Models;
using TerraFluent.Chart.Reporting.Api.Rendering;
using TerraFluent.Chart.Reporting.Api.Services;

namespace TerraFluent.Chart.Reporting.Api.Controllers;

/// <summary>
/// Deterministic auto-analytics as a service. POST raw CSV or JSON data and receive schema
/// profiling, data-quality validation, ranked insights, anomalies, chart recommendations and a
/// fully-assembled smart dashboard — no AI, fully reproducible.
/// </summary>
[ApiController]
[Route("api/analytics")]
[Route("api/v1/analytics")]
[Produces("application/json")]
public sealed class AnalyticsController : ControllerBase
{
    private readonly AnalyticsEngine _engine;
    private readonly AnalyticAgent _agent;
    private readonly SessionStore _sessions;

    public AnalyticsController(AnalyticsEngine engine, AnalyticAgent agent, SessionStore sessions)
    {
        _engine = engine;
        _agent = agent;
        _sessions = sessions;
    }

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
    public IActionResult Analyze([FromBody] AnalyzeRequest request, [FromQuery] bool includeSvg = false,
        [FromQuery] string? theme = null, [FromQuery] string? renderMode = null,
        [FromQuery] bool? exportMenu = null, [FromQuery] bool? gridLines = null,
        CancellationToken ct = default)
    {
        var result = RunAnalysis(request, ct);
        return Ok(AnalyticsResponse.From(result, includeSvg, ChartStyle.FromQuery(theme, renderMode, exportMenu, gridLines)));
    }

    /// <summary>
    /// Runs the full analysis and returns a complete, self-contained HTML page (summary, insights,
    /// chart recommendations and column profiles) ready to display in a browser or embed in a report.
    /// </summary>
    [HttpPost("analyze/html")]
    [Produces("text/html")]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public IActionResult AnalyzeHtml([FromBody] AnalyzeRequest request,
        [FromQuery] string? theme = null, [FromQuery] string? renderMode = null,
        [FromQuery] bool? exportMenu = null, [FromQuery] bool? gridLines = null,
        CancellationToken ct = default)
    {
        var result = RunAnalysis(request, ct);
        return Content(AnalyzeHtmlRenderer.Render(result, ChartStyle.FromQuery(theme, renderMode, exportMenu, gridLines)), "text/html");
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
        [FromQuery] string? filter = null,
        [FromQuery] bool includeSvg = false,
        [FromQuery] int? maxInsights = null,
        [FromQuery] int? maxRecommendations = null,
        [FromQuery] int? maxGroupCombinations = null,
        [FromQuery] double? zScoreThreshold = null,
        [FromQuery] bool throwOnValidationError = false,
        CancellationToken ct = default)
    {
        string data = await ReadBodyAsync(ct);
        var result = RunAnalysis(new AnalyzeRequest
        {
            Data = data, Format = AnalyzeFormat.Csv, DatasetName = datasetName, Filter = filter,
            MaxInsights = maxInsights, MaxRecommendations = maxRecommendations,
            MaxGroupCombinations = maxGroupCombinations, ZScoreThreshold = zScoreThreshold,
            ThrowOnValidationError = throwOnValidationError
        }, ct);
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
        [FromQuery] string? filter = null,
        [FromQuery] bool includeSvg = false,
        [FromQuery] int? maxInsights = null,
        [FromQuery] int? maxRecommendations = null,
        [FromQuery] int? maxGroupCombinations = null,
        [FromQuery] double? zScoreThreshold = null,
        [FromQuery] bool throwOnValidationError = false,
        CancellationToken ct = default)
    {
        string data = await ReadBodyAsync(ct);
        var result = RunAnalysis(new AnalyzeRequest
        {
            Data = data, Format = AnalyzeFormat.Json, DatasetName = datasetName, Filter = filter,
            MaxInsights = maxInsights, MaxRecommendations = maxRecommendations,
            MaxGroupCombinations = maxGroupCombinations, ZScoreThreshold = zScoreThreshold,
            ThrowOnValidationError = throwOnValidationError
        }, ct);
        return Ok(AnalyticsResponse.From(result, includeSvg));
    }

    // ── Focused slices ────────────────────────────────────────────────────────

    /// <summary>Returns only the ranked insights and executive summary for the supplied data.</summary>
    [HttpPost("insights")]
    [ProducesResponseType(typeof(InsightsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public IActionResult Insights([FromBody] AnalyzeRequest request, CancellationToken ct = default)
    {
        var result = RunAnalysis(request, ct);
        return Ok(new InsightsResponse
        {
            Summary = SummaryDto.From(result.Summary),
            Insights = result.Insights.Select(InsightDto.From).ToList()
        });
    }

    /// <summary>Returns only the data-quality validation report for the supplied data.</summary>
    [HttpPost("validate")]
    [ProducesResponseType(typeof(ValidationDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public IActionResult Validate([FromBody] AnalyzeRequest request, CancellationToken ct = default)
    {
        var result = RunAnalysis(request, ct);
        return Ok(ValidationDto.From(result.Validation));
    }

    // ── Agent ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// Asks the deterministic analytic agent a question about the data. The agent plans and runs the
    /// relevant analysis skills (trend, anomaly, correlation, dominance, forecast, root-cause),
    /// following the evidence, and returns an explainable reasoning trace with ranked insights and
    /// supporting charts.
    /// </summary>
    /// <remarks>
    /// Provide the raw <c>data</c> plus an optional <c>question</c>. Omit the question for an
    /// open-ended investigation. Examples: "why did revenue change?", "forecast revenue",
    /// "which region leads?", "any anomalies in profit?". Set <c>?includeSvg=true</c> to embed a
    /// rendered SVG for each supporting chart.
    /// </remarks>
    [HttpPost("ask")]
    [ProducesResponseType(typeof(AskResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public IActionResult Ask([FromBody] AnalyzeRequest request, [FromQuery] bool includeSvg = false,
        [FromQuery] string? theme = null, [FromQuery] string? renderMode = null,
        [FromQuery] bool? exportMenu = null, [FromQuery] bool? gridLines = null,
        CancellationToken ct = default)
    {
        var result = RunAnalysis(request, ct);
        var trace = _agent.Investigate(result, request.Question);
        return Ok(AskResponse.From(trace, includeSvg, ChartStyle.FromQuery(theme, renderMode, exportMenu, gridLines)));
    }

    // ── Sessions (multi-turn agent) ───────────────────────────────────────────

    /// <summary>
    /// Creates a multi-turn analytic session over the supplied data and returns its id and summary.
    /// Post follow-up questions to <c>/api/analytics/sessions/{id}/ask</c>; the session remembers
    /// prior turns, avoids repeating work, and resolves pronouns ("break that down") to earlier focus.
    /// </summary>
    [HttpPost("sessions")]
    [ProducesResponseType(typeof(SessionCreatedResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public IActionResult CreateSession([FromBody] AnalyzeRequest request, CancellationToken ct = default)
    {
        var result = RunAnalysis(request, ct);
        var session = _agent.StartSession(result);
        string id = _sessions.Add(session);
        return Ok(new SessionCreatedResponse { SessionId = id, Summary = SummaryDto.From(result.Summary) });
    }

    /// <summary>
    /// Asks a follow-up question within an existing session, building on everything asked earlier.
    /// </summary>
    [HttpPost("sessions/{id}/ask")]
    [ProducesResponseType(typeof(AskResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public IActionResult AskSession(string id, [FromBody] SessionAskRequest request, [FromQuery] bool includeSvg = false,
        [FromQuery] string? theme = null, [FromQuery] string? renderMode = null,
        [FromQuery] bool? exportMenu = null, [FromQuery] bool? gridLines = null)
    {
        var session = _sessions.Get(id)
            ?? throw new KeyNotFoundException($"No active session with id '{id}'.");

        var trace = session.Ask(request?.Question);
        return Ok(AskResponse.From(trace, includeSvg, ChartStyle.FromQuery(theme, renderMode, exportMenu, gridLines)));
    }

    // ── Aggregation ───────────────────────────────────────────────────────────

    /// <summary>
    /// Groups a measure by one or two dimensions with a chosen aggregation function
    /// (sum, average, count, min, max, median). With a second dimension the result is a pivot.
    /// </summary>
    /// <remarks>
    /// Supply the raw <c>data</c> in the body and the grouping via query parameters, e.g.
    /// <c>?measure=Revenue&amp;dimension=Region&amp;secondDimension=Product&amp;aggregation=Average</c>.
    /// A row <c>filter</c> in the body is applied first.
    /// </remarks>
    [HttpPost("aggregate")]
    [ProducesResponseType(typeof(AggregationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public IActionResult Aggregate(
        [FromBody] AnalyzeRequest request,
        [FromQuery] string measure,
        [FromQuery] string dimension,
        [FromQuery] string? secondDimension = null,
        [FromQuery] AggregationKind aggregation = AggregationKind.Sum,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(measure) || string.IsNullOrWhiteSpace(dimension))
            throw new ArgumentException("Both 'measure' and 'dimension' query parameters are required.");

        var result = RunAnalysis(request, ct);
        var aggregation2 = new AggregationEngine()
            .GroupBy(result.Profile, measure, dimension, secondDimension, aggregation)
            ?? throw new ArgumentException(
                "Could not aggregate: check that the measure and dimension column names exist in the data.");

        return Ok(AggregationResponse.From(aggregation2));
    }

    // ── Dashboard ─────────────────────────────────────────────────────────────
    /// <summary>
    /// Builds a smart dashboard (KPIs, trend/comparison/distribution charts, anomalies, insights)
    /// and returns it as structured JSON with each chart pre-rendered to SVG.
    /// </summary>
    [HttpPost("dashboard")]
    [ProducesResponseType(typeof(DashboardResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public IActionResult Dashboard([FromBody] AnalyzeRequest request, [FromQuery] int maxKpis = 4,
        [FromQuery] string? theme = null, [FromQuery] string? renderMode = null,
        [FromQuery] bool? exportMenu = null, [FromQuery] bool? gridLines = null,
        CancellationToken ct = default)
    {
        var result = RunAnalysis(request, ct);
        var dashboard = DashboardBuilder.Generate(result, maxKpis);
        return Ok(DashboardResponse.From(dashboard, result, ChartStyle.FromQuery(theme, renderMode, exportMenu, gridLines)));
    }

    /// <summary>
    /// Builds a smart dashboard and returns a complete, self-contained HTML page ready to display
    /// in a browser or embed in a report.
    /// </summary>
    [HttpPost("dashboard/html")]
    [Produces("text/html")]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public IActionResult DashboardHtml([FromBody] AnalyzeRequest request, [FromQuery] int maxKpis = 4,
        [FromQuery] string? theme = null, [FromQuery] string? renderMode = null,
        [FromQuery] bool? exportMenu = null, [FromQuery] bool? gridLines = null,
        CancellationToken ct = default)
    {
        var result = RunAnalysis(request, ct);
        var dashboard = DashboardBuilder.Generate(result, maxKpis);
        return Content(DashboardHtmlRenderer.Render(dashboard, result, ChartStyle.FromQuery(theme, renderMode, exportMenu, gridLines)), "text/html");
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private AnalyticsResult RunAnalysis(AnalyzeRequest request, CancellationToken ct = default)
    {
        if (request is null || string.IsNullOrWhiteSpace(request.Data))
            throw new ArgumentException("Request body must include non-empty 'data' to analyse.");

        ct.ThrowIfCancellationRequested();

        var options = new AnalyticsOptions { DatasetName = request.DatasetName };
        if (request.MaxInsights is int mi)
        {
            Ensure(mi is >= 1 and <= 500, "maxInsights must be between 1 and 500.");
            options.MaxInsights = mi;
        }
        if (request.MaxRecommendations is int mr)
        {
            Ensure(mr is >= 1 and <= 200, "maxRecommendations must be between 1 and 200.");
            options.MaxRecommendations = mr;
        }
        if (request.MaxGroupCombinations is int mg)
        {
            Ensure(mg is >= 1 and <= 200, "maxGroupCombinations must be between 1 and 200.");
            options.MaxGroupCombinations = mg;
        }
        if (request.ZScoreThreshold is double z)
        {
            Ensure(z is >= 0.1 and <= 10.0, "zScoreThreshold must be between 0.1 and 10.");
            options.ZScoreThreshold = z;
        }
        options.ThrowOnValidationError = request.ThrowOnValidationError;

        IDataSource source = ResolveFormat(request) switch
        {
            AnalyzeFormat.Json => new JsonDataSource(request.Data),
            _                  => new CsvDataSource(request.Data)
        };

        // Optional row filter applied before analysis (AND-combined conditions).
        var dataset = SliceExpression.Parse(request.Filter).Apply(source.Load());
        ct.ThrowIfCancellationRequested();
        return _engine.Run(dataset, options);
    }

    // Guards a request-tuning constraint, surfacing a 400 via the exception middleware.
    private static void Ensure(bool condition, string message)
    {
        if (!condition) throw new ArgumentException(message);
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

    private async Task<string> ReadBodyAsync(CancellationToken ct = default)
    {
        using var reader = new StreamReader(Request.Body);
        return await reader.ReadToEndAsync(ct);
    }
}
