using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using TerraFluent.Chart.Reporting.Builder;
using TerraFluent.Chart.Reporting.Api.Models;
using TerraFluent.Chart.Reporting.Enums;
using TerraFluent.Chart.Reporting.Models;

namespace TerraFluent.Chart.Reporting.Api.Controllers;

/// <summary>
/// Renders TerraFluent SVG charts from a ChartOptions JSON body.
/// </summary>
[ApiController]
[Route("api/charts")]
[Produces("application/json")]
public sealed class ChartsController : ControllerBase
{
    // ── Single-format render endpoints ────────────────────────────────────────

    /// <summary>
    /// Renders a chart and returns the SVG markup.
    /// </summary>
    /// <remarks>
    /// The request body must be a JSON object that matches the <c>ChartOptions</c> schema.
    /// The response body is a self-contained SVG document (<c>image/svg+xml</c>).
    ///
    /// **Minimal example:**
    /// ```json
    /// {
    ///   "title": { "text": "Monthly Sales" },
    ///   "xAxis": { "categories": ["Jan","Feb","Mar"] },
    ///   "series": [{ "name": "Revenue", "type": 0, "data": [120, 200, 150] }]
    /// }
    /// ```
    /// </remarks>
    [HttpPost("svg")]
    [Produces("image/svg+xml")]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public IActionResult RenderSvg([FromBody] JsonElement body)
    {
        var svg = BuildFromJson(body).RenderToSvg();
        return Content(svg, "image/svg+xml");
    }

    /// <summary>
    /// Renders a chart and returns an HTML fragment suitable for direct embedding in a web page.
    /// The SVG is wrapped in a <c>&lt;figure&gt;</c> element with responsive styling.
    /// </summary>
    [HttpPost("html")]
    [Produces("text/html")]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public IActionResult RenderHtml([FromBody] JsonElement body,
        [FromQuery] string? caption = null,
        [FromQuery] string? cssClass = null)
    {
        var html = BuildFromJson(body).RenderToHtml(caption, cssClass);
        return Content(html, "text/html");
    }

    /// <summary>
    /// Renders a chart and returns a Base64-encoded <c>data:image/svg+xml;base64,…</c> URI.
    /// Use the result directly as an <c>&lt;img src="…"&gt;</c> or CSS <c>background-image</c>.
    /// </summary>
    [HttpPost("datauri")]
    [Produces("text/plain")]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public IActionResult RenderDataUri([FromBody] JsonElement body)
    {
        var uri = BuildFromJson(body).RenderToDataUri();
        return Content(uri, "text/plain");
    }

    // ── Content-negotiated smart render ──────────────────────────────────────

    /// <summary>
    /// Renders a chart in the format requested via the <c>Accept</c> header (or <c>?format=</c>).
    /// Supported values: <c>image/svg+xml</c> → SVG, <c>text/html</c> → HTML fragment,
    /// <c>text/plain</c> or <c>application/json</c> → data URI.
    /// </summary>
    [HttpPost("render")]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public IActionResult Render([FromBody] JsonElement body,
        [FromQuery] string? format = null)
    {
        var builder  = BuildFromJson(body);
        string accept = format
            ?? Request.Headers.Accept.FirstOrDefault()
            ?? "image/svg+xml";

        if (accept.Contains("text/html", StringComparison.OrdinalIgnoreCase))
            return Content(builder.RenderToHtml(), "text/html");

        if (accept.Contains("text/plain", StringComparison.OrdinalIgnoreCase)
            || accept.Contains("datauri",  StringComparison.OrdinalIgnoreCase)
            || string.Equals(format, "datauri", StringComparison.OrdinalIgnoreCase))
            return Content(builder.RenderToDataUri(), "text/plain");

        return Content(builder.RenderToSvg(), "image/svg+xml");
    }

    // ── Batch render ──────────────────────────────────────────────────────────

    /// <summary>
    /// Renders multiple charts in a single request.
    /// Each item in the array specifies a <c>format</c> (<c>svg</c>, <c>html</c>, or <c>datauri</c>)
    /// and a <c>options</c> object. Results are returned in the same order.
    /// Individual item failures are captured in the result rather than aborting the whole batch.
    /// </summary>
    /// <remarks>Maximum 50 items per batch.</remarks>
    [HttpPost("batch")]
    [ProducesResponseType(typeof(BatchRenderResult[]), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public IActionResult RenderBatch([FromBody] BatchRenderItem[] items)
    {
        const int maxBatch = 50;
        if (items is null || items.Length == 0)
            return BadRequest(new ProblemDetails { Title = "Bad Request", Detail = "Batch must contain at least one item." });
        if (items.Length > maxBatch)
            return BadRequest(new ProblemDetails { Title = "Bad Request", Detail = $"Batch size {items.Length} exceeds the maximum of {maxBatch}." });

        var results = new BatchRenderResult[items.Length];
        for (int i = 0; i < items.Length; i++)
        {
            try
            {
                var builder = BuildFromJson(items[i].Options);
                string output = items[i].Format.ToLowerInvariant() switch
                {
                    "html"    => builder.RenderToHtml(),
                    "datauri" => builder.RenderToDataUri(),
                    _         => builder.RenderToSvg()
                };
                results[i] = new BatchRenderResult { Index = i, Success = true, Output = output };
            }
            catch (Exception ex)
            {
                results[i] = new BatchRenderResult { Index = i, Success = false, Error = ex.Message };
            }
        }

        return Ok(results);
    }

    // ── Data-quality analysis (without rendering) ─────────────────────────────

    /// <summary>
    /// Analyses a chart's data quality without rendering it.
    /// Returns a structured report of errors, warnings, and informational findings.
    /// Useful for validating configuration before calling a render endpoint.
    /// </summary>
    [HttpPost("analyze")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public IActionResult Analyze([FromBody] JsonElement body)
    {
        var report = BuildFromJson(body).AnalyzeDataQuality();
        return Ok(new
        {
            isClean   = report.IsClean,
            hasErrors = report.HasErrors,
            findings  = report.Warnings.Select(w => new
            {
                severity = w.Severity.ToString(),
                message  = w.Message
            })
        });
    }

    // ── Catalogue endpoints ───────────────────────────────────────────────────

    /// <summary>Returns the names of all built-in chart themes.</summary>
    [HttpGet("catalogue/themes")]
    [ProducesResponseType(typeof(string[]), StatusCodes.Status200OK)]
    public IActionResult GetThemes() => Ok(new[]
    {
        "Default", "Dark", "Pastel", "Monochrome", "Ocean", "Sunset", "Forest",
        "Neon", "Minimal", "Warm", "Arctic", "Business", "Material",
        "TrafficLight", "Accessible", "HighContrast"
    });

    /// <summary>Returns the names of all supported chart types with their numeric enum values.</summary>
    [HttpGet("catalogue/chart-types")]
    [ProducesResponseType(typeof(object[]), StatusCodes.Status200OK)]
    public IActionResult GetChartTypes() =>
        Ok(Enum.GetValues<ChartType>()
            .Select(t => new { name = t.ToString(), value = (int)t }));

    /// <summary>Returns the supported render modes.</summary>
    [HttpGet("catalogue/render-modes")]
    [ProducesResponseType(typeof(object[]), StatusCodes.Status200OK)]
    public IActionResult GetRenderModes() =>
        Ok(Enum.GetValues<SvgMode>()
            .Select(m => new { name = m.ToString(), value = (int)m, description = RenderModeDescription(m) }));

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static ChartBuilder BuildFromJson(JsonElement body)
    {
        if (body.ValueKind == JsonValueKind.Null || body.ValueKind == JsonValueKind.Undefined)
            throw new ArgumentException("Request body must be a valid JSON object representing ChartOptions.");

        return ChartBuilder.FromJson(body.GetRawText());
    }

    private static string RenderModeDescription(SvgMode mode) => mode switch
    {
        SvgMode.Static      => "No CSS hover, no JavaScript. Safe for PDF and email.",
        SvgMode.Animated    => "SMIL/CSS animations, no JavaScript. Suitable for Blazor and browsers.",
        SvgMode.Interactive => "CSS hover effects and embedded JavaScript. Browser-only.",
        _                   => string.Empty
    };
}
