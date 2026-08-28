using Microsoft.AspNetCore.Mvc;
using TerraFluent.AutoAnalytics.Agent;
using TerraFluent.AutoAnalytics.Analytics;
using TerraFluent.AutoAnalytics.Comparison;
using TerraFluent.AutoAnalytics.Connectors;
using TerraFluent.AutoAnalytics.Data.Connections;
using TerraFluent.AutoAnalytics.Dashboard;
using TerraFluent.AutoAnalytics.Data;
using TerraFluent.AutoAnalytics.Data.Sources;
using TerraFluent.AutoAnalytics.Engine;
using TerraFluent.AutoAnalytics.Enums;
using TerraFluent.AutoAnalytics.Insights;
using TerraFluent.AutoAnalytics.Recommendation;
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
    private readonly ConnectionResolver _connections;

    public AnalyticsController(
        AnalyticsEngine engine, AnalyticAgent agent, SessionStore sessions, ConnectionResolver connections)
    {
        _engine = engine;
        _agent = agent;
        _sessions = sessions;
        _connections = connections;
    }

    // ── Connections ───────────────────────────────────────────────────────────

    /// <summary>
    /// Lists the live data connections an operator has configured on this server.
    /// </summary>
    /// <remarks>
    /// Only the name, kind, description and row cap are returned — never the target URL, connection
    /// string or any credential, which stay in server configuration. Use a returned <c>name</c> as
    /// <c>connectionName</c> on any analytics endpoint to pull that source instead of sending data
    /// inline. Returns an empty list when no connections are configured.
    /// </remarks>
    [HttpGet("connections")]
    [ProducesResponseType(typeof(IReadOnlyList<ConnectionDescriptorDto>), StatusCodes.Status200OK)]
    public IActionResult Connections() =>
        Ok(_connections.List().Select(ConnectionDescriptorDto.From).ToList());

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
    public async Task<IActionResult> Analyze([FromBody] AnalyzeRequest request, [FromQuery] bool includeSvg = false,
        [FromQuery] string? theme = null, [FromQuery] string? renderMode = null,
        [FromQuery] bool? exportMenu = null, [FromQuery] bool? gridLines = null,
        CancellationToken ct = default)
    {
        var result = await RunAnalysisAsync(request, ct);
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
    public async Task<IActionResult> AnalyzeHtml([FromBody] AnalyzeRequest request,
        [FromQuery] string? theme = null, [FromQuery] string? renderMode = null,
        [FromQuery] bool? exportMenu = null, [FromQuery] bool? gridLines = null,
        CancellationToken ct = default)
    {
        var result = await RunAnalysisAsync(request, ct);
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
        [FromQuery] bool preAggregated = false,
        [FromQuery] bool throwOnValidationError = false,
        CancellationToken ct = default)
    {
        string data = await ReadBodyAsync(ct);
        var result = await RunAnalysisAsync(new AnalyzeRequest
        {
            Data = data, Format = AnalyzeFormat.Csv, DatasetName = datasetName, Filter = filter,
            MaxInsights = maxInsights, MaxRecommendations = maxRecommendations,
            MaxGroupCombinations = maxGroupCombinations, ZScoreThreshold = zScoreThreshold,
            PreAggregated = preAggregated,
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
        [FromQuery] bool preAggregated = false,
        [FromQuery] bool throwOnValidationError = false,
        CancellationToken ct = default)
    {
        string data = await ReadBodyAsync(ct);
        var result = await RunAnalysisAsync(new AnalyzeRequest
        {
            Data = data, Format = AnalyzeFormat.Json, DatasetName = datasetName, Filter = filter,
            MaxInsights = maxInsights, MaxRecommendations = maxRecommendations,
            MaxGroupCombinations = maxGroupCombinations, ZScoreThreshold = zScoreThreshold,
            PreAggregated = preAggregated,
            ThrowOnValidationError = throwOnValidationError
        }, ct);
        return Ok(AnalyticsResponse.From(result, includeSvg));
    }

    // ── Conversion ────────────────────────────────────────────────────────────

    /// <summary>
    /// Normalises an uploaded <c>.xlsx</c> workbook into CSV text. POST the raw file bytes as the
    /// request body; the first worksheet is read with its first row as the header.
    /// </summary>
    /// <remarks>
    /// The returned <c>data</c> can be fed straight into any other analytics endpoint. Use this when
    /// you hold the file bytes; to send a workbook inline instead, base64-encode it and post it with
    /// <c>"format": "xlsx"</c>.
    /// Excel stores dates as styled serial numbers, so date columns may arrive as plain numbers —
    /// format them as text in the workbook if reliable date typing matters.
    /// </remarks>
    [HttpPost("convert/xlsx")]
    [Consumes("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "application/octet-stream")]
    [ProducesResponseType(typeof(ConversionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ConvertXlsx([FromQuery] string? datasetName = null, CancellationToken ct = default)
    {
        byte[] bytes = await ReadBodyBytesAsync(ct);
        if (bytes.Length == 0)
            throw new ArgumentException("Request body must contain the .xlsx file bytes.");

        Dataset dataset;
        try
        {
            dataset = new XlsxDataSource(bytes, datasetName).Load();
        }
        catch (Exception ex) when (ex is InvalidDataException or System.Xml.XmlException)
        {
            throw new ArgumentException("The uploaded file is not a readable .xlsx workbook.", ex);
        }

        if (dataset.ColumnCount == 0)
            throw new ArgumentException("The workbook's first worksheet has no header row to read.");

        return Ok(new ConversionResponse
        {
            DatasetName = dataset.Name,
            Format      = AnalyzeFormat.Csv,
            Data        = CsvSerializer.ToCsv(dataset),
            RowCount    = dataset.RowCount,
            ColumnCount = dataset.ColumnCount
        });
    }

    // ── Focused slices ────────────────────────────────────────────────────────

    /// <summary>Returns only the ranked insights and executive summary for the supplied data.</summary>
    [HttpPost("insights")]
    [ProducesResponseType(typeof(InsightsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Insights([FromBody] AnalyzeRequest request, CancellationToken ct = default)
    {
        var result = await RunAnalysisAsync(request, ct);
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
    public async Task<IActionResult> Validate([FromBody] AnalyzeRequest request, CancellationToken ct = default)
    {
        var result = await RunAnalysisAsync(request, ct);
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
    public async Task<IActionResult> Ask([FromBody] AnalyzeRequest request, [FromQuery] bool includeSvg = false,
        [FromQuery] string? theme = null, [FromQuery] string? renderMode = null,
        [FromQuery] bool? exportMenu = null, [FromQuery] bool? gridLines = null,
        CancellationToken ct = default)
    {
        var result = await RunAnalysisAsync(request, ct);
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
    public async Task<IActionResult> CreateSession([FromBody] AnalyzeRequest request, CancellationToken ct = default)
    {
        var result = await RunAnalysisAsync(request, ct);
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
    public async Task<IActionResult> Aggregate(
        [FromBody] AnalyzeRequest request,
        [FromQuery] string measure,
        [FromQuery] string dimension,
        [FromQuery] string? secondDimension = null,
        [FromQuery] AggregationKind aggregation = AggregationKind.Sum,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(measure) || string.IsNullOrWhiteSpace(dimension))
            throw new ArgumentException("Both 'measure' and 'dimension' query parameters are required.");

        var result = await RunAnalysisAsync(request, ct);
        var aggregation2 = new AggregationEngine()
            .GroupBy(result.Profile, measure, dimension, secondDimension, aggregation)
            ?? throw new ArgumentException(
                "Could not aggregate: check that the measure and dimension column names exist in the data.");

        return Ok(AggregationResponse.From(aggregation2));
    }

    // ── Comparison ────────────────────────────────────────────────────────────

    /// <summary>
    /// Diffs two datasets: structural changes, how each shared measure moved, and how the category
    /// mix shifted — with ranked insights and supporting charts.
    /// </summary>
    /// <remarks>
    /// Supply both sides inline; each is analysed independently and neither is retained:
    /// ```json
    /// {
    ///   "baseline": { "datasetName": "Q1", "data": "Month,Region,Revenue\n..." },
    ///   "current":  { "datasetName": "Q2", "data": "Month,Region,Revenue\n..." }
    /// }
    /// ```
    /// Additive measures are compared on totals and per-row attributes on averages. Columns present
    /// on only one side are reported rather than silently dropped. Set <c>?includeSvg=true</c> to
    /// embed a rendered SVG for each supporting chart.
    /// </remarks>
    [HttpPost("compare")]
    [ProducesResponseType(typeof(CompareResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Compare([FromBody] CompareRequest request, [FromQuery] bool includeSvg = false,
        [FromQuery] string? theme = null, [FromQuery] string? renderMode = null,
        [FromQuery] bool? exportMenu = null, [FromQuery] bool? gridLines = null,
        CancellationToken ct = default)
    {
        var (comparison, insights, charts) = await RunComparisonAsync(request, ct);
        return Ok(CompareResponse.From(comparison, insights, charts, includeSvg,
            ChartStyle.FromQuery(theme, renderMode, exportMenu, gridLines)));
    }

    /// <summary>
    /// Diffs two datasets and returns a complete, self-contained HTML page ready to display in a
    /// browser or embed in a report.
    /// </summary>
    [HttpPost("compare/html")]
    [Produces("text/html")]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> CompareHtml([FromBody] CompareRequest request,
        [FromQuery] string? theme = null, [FromQuery] string? renderMode = null,
        [FromQuery] bool? exportMenu = null, [FromQuery] bool? gridLines = null,
        CancellationToken ct = default)
    {
        var (comparison, insights, charts) = await RunComparisonAsync(request, ct);
        return Content(
            CompareHtmlRenderer.Render(comparison, insights, charts,
                ChartStyle.FromQuery(theme, renderMode, exportMenu, gridLines)),
            "text/html");
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
    public async Task<IActionResult> Dashboard([FromBody] AnalyzeRequest request, [FromQuery] int maxKpis = 4,
        [FromQuery] string? theme = null, [FromQuery] string? renderMode = null,
        [FromQuery] bool? exportMenu = null, [FromQuery] bool? gridLines = null,
        CancellationToken ct = default)
    {
        var result = await RunAnalysisAsync(request, ct);
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
    public async Task<IActionResult> DashboardHtml([FromBody] AnalyzeRequest request, [FromQuery] int maxKpis = 4,
        [FromQuery] string? theme = null, [FromQuery] string? renderMode = null,
        [FromQuery] bool? exportMenu = null, [FromQuery] bool? gridLines = null,
        CancellationToken ct = default)
    {
        var result = await RunAnalysisAsync(request, ct);
        var dashboard = DashboardBuilder.Generate(result, maxKpis);
        return Content(DashboardHtmlRenderer.Render(dashboard, result, ChartStyle.FromQuery(theme, renderMode, exportMenu, gridLines)), "text/html");
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    // Analyses both sides independently, then diffs them. Neither dataset outlives the request.
    private async Task<(DatasetComparison Comparison, IReadOnlyList<Insight> Insights, IReadOnlyList<RecommendedChart> Charts)>
        RunComparisonAsync(CompareRequest request, CancellationToken ct)
    {
        if (request is null)
            throw new ArgumentException("Request body must include a 'baseline' and a 'current' dataset.");

        var baseline = await RunAnalysisAsync(request.Baseline, ct);
        var current = await RunAnalysisAsync(request.Current, ct);

        var comparison = new DatasetComparisonEngine().Compare(baseline, current);
        return (comparison, ComparisonNarrator.Insights(comparison), ComparisonNarrator.Charts(comparison));
    }

    private async Task<AnalyticsResult> RunAnalysisAsync(AnalyzeRequest request, CancellationToken ct = default)
    {
        bool usesConnection = !string.IsNullOrWhiteSpace(request?.ConnectionName);
        if (request is null || (!usesConnection && string.IsNullOrWhiteSpace(request.Data)))
            throw new ArgumentException(
                "Request body must include non-empty 'data', or a 'connectionName' to pull it from.");

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
        options.PreAggregated = request.PreAggregated ?? false;
        options.ThrowOnValidationError = request.ThrowOnValidationError;

        // A named connection pulls the rows from a server-configured source; otherwise they came
        // inline with the request. Either way the dataset lives only for this call.
        Dataset loaded;
        if (usesConnection)
        {
            options.DatasetName ??= request.ConnectionName;
            var live = _connections.Resolve(request.ConnectionName!);
            loaded = await live.LoadAsync(ct).ConfigureAwait(false);
        }
        else
        {
            IDataSource source = ResolveFormat(request) switch
            {
                AnalyzeFormat.Json => new JsonDataSource(request.Data),
                AnalyzeFormat.Xlsx => new XlsxDataSource(DecodeXlsx(request.Data), request.DatasetName),
                _                  => new CsvDataSource(request.Data)
            };
            loaded = source.Load();
        }

        // Optional row filter applied before analysis (AND-combined conditions).
        var dataset = SliceExpression.Parse(request.Filter).Apply(loaded);
        ct.ThrowIfCancellationRequested();
        return _engine.Run(dataset, options);
    }

    // Guards a request-tuning constraint, surfacing a 400 via the exception middleware.
    private static void Ensure(bool condition, string message)
    {
        if (!condition) throw new ArgumentException(message);
    }

    // Auto-detects CSV, JSON or base64 XLSX when the caller did not specify a concrete format.
    private static AnalyzeFormat ResolveFormat(AnalyzeRequest request)
    {
        if (request.Format != AnalyzeFormat.Auto) return request.Format;
        string trimmed = request.Data.TrimStart();
        if (trimmed.StartsWith('[') || trimmed.StartsWith('{')) return AnalyzeFormat.Json;
        // Every .xlsx is a ZIP, whose "PK\x03\x04" signature base64-encodes to the "UEsDB" prefix.
        if (trimmed.StartsWith("UEsDB", StringComparison.Ordinal)) return AnalyzeFormat.Xlsx;
        return AnalyzeFormat.Csv;
    }

    // Decodes a base64 .xlsx payload, surfacing a 400 rather than a 500 on malformed input.
    private static byte[] DecodeXlsx(string data)
    {
        try
        {
            return Convert.FromBase64String(data.Trim());
        }
        catch (FormatException)
        {
            throw new ArgumentException(
                "Format 'xlsx' expects 'data' to be a base64-encoded .xlsx workbook. " +
                "To upload the raw bytes instead, POST them to /api/analytics/convert/xlsx.");
        }
    }

    private async Task<string> ReadBodyAsync(CancellationToken ct = default)
    {
        using var reader = new StreamReader(Request.Body);
        return await reader.ReadToEndAsync(ct);
    }

    private async Task<byte[]> ReadBodyBytesAsync(CancellationToken ct = default)
    {
        using var buffer = new MemoryStream();
        await Request.Body.CopyToAsync(buffer, ct);
        return buffer.ToArray();
    }
}
