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
[Route("api/v1/charts")]
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
        return Content(SvgWithFont(BuildFromJson(body)), "image/svg+xml");
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
        return Content(DataUriWithFont(BuildFromJson(body)), "text/plain");
    }

    // ── Shareable link render ─────────────────────────────────────────────────

    /// <summary>
    /// Renders a chart from Base64-encoded <c>ChartOptions</c> JSON supplied on the query string
    /// and returns the SVG markup.
    /// </summary>
    /// <remarks>
    /// Designed for shareable links: the whole chart definition travels inside the URL, so opening
    /// the link in a browser shows the rendered chart with no request body. Both standard and
    /// URL-safe Base64 encodings of the JSON are accepted.
    /// <para>Example: <c>GET /api/charts/shared?options=eyJ0aXRsZSI6...</c></para>
    /// </remarks>
    /// <param name="options">Base64-encoded (standard or URL-safe) ChartOptions JSON.</param>
    [HttpGet("shared")]
    [Produces("image/svg+xml")]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public IActionResult RenderShared([FromQuery] string? options)
    {
        if (string.IsNullOrWhiteSpace(options))
            return BadRequest(new ProblemDetails
            {
                Title = "Bad Request",
                Detail = "The 'options' query parameter is required (Base64-encoded ChartOptions JSON)."
            });

        string json;
        try { json = DecodeBase64Json(options!); }
        catch (FormatException)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Bad Request",
                Detail = "The 'options' query parameter is not valid Base64."
            });
        }

        using var doc = JsonDocument.Parse(json);
        // Embed the theme font so the shared SVG renders correctly when loaded via <img> (isolated
        // from the host page's @font-face).
        var svg = Rendering.EmbeddedFontCss.InjectInto(BuildFromJson(doc.RootElement).RenderToSvg());
        return Content(svg, "image/svg+xml");
    }

    // ── Raster-intent render endpoints ───────────────────────────────────────

    /// <summary>
    /// Returns a self-contained HTML page that rasterizes the chart to PNG and immediately
    /// triggers a browser download — no third-party library required.
    /// <para>Open the response URL in a browser (or redirect to it) to receive a real
    /// <c>.png</c> file. The rasterization uses the same Canvas-based logic as the
    /// interactive export menu embedded in the chart library.</para>
    /// </summary>
    [HttpPost("png")]
    [Produces("text/html")]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public IActionResult RenderPng([FromBody] JsonElement body)
    {
        var builder = BuildFromJsonStatic(body);
        var fname   = BuildRasterFilename(builder.GetOptions().Title?.Text, "png");
        return Content(BuildRasterHtml(builder.RenderToSvg(), "image/png", fname), "text/html");
    }

    /// <summary>
    /// Returns a self-contained HTML page that rasterizes the chart to JPEG and immediately
    /// triggers a browser download — no third-party library required.
    /// <para>The page fills the canvas with white before drawing the SVG, exactly mirroring
    /// the <c>canvas.fillStyle='#fff'</c> step in the interactive export menu, so JPEG
    /// artefacts from transparent areas are eliminated.</para>
    /// </summary>
    [HttpPost("jpg")]
    [Produces("text/html")]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public IActionResult RenderJpeg([FromBody] JsonElement body)
    {
        var builder = BuildFromJsonStatic(body);
        var fname   = BuildRasterFilename(builder.GetOptions().Title?.Text, "jpg");
        return Content(BuildRasterHtml(builder.RenderToSvg(), "image/jpeg", fname), "text/html");
    }

    // ── Content-negotiated smart render ──────────────────────────────────────

    /// <summary>
    /// Renders a chart in the format requested via the <c>Accept</c> header (or <c>?format=</c>).
    /// Supported values: <c>image/svg+xml</c> → SVG, <c>text/html</c> → HTML fragment,
    /// <c>text/plain</c> or <c>application/json</c> → data URI,
    /// <c>image/png</c> or <c>png</c> → SVG prepared for PNG rasterization,
    /// <c>image/jpeg</c>, <c>image/jpg</c>, or <c>jpeg</c>/<c>jpg</c> → SVG prepared for JPEG rasterization.
    /// </summary>
    [HttpPost("render")]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public IActionResult Render([FromBody] JsonElement body,
        [FromQuery] string? format = null)
    {
        string accept = format
            ?? Request.Headers.Accept.FirstOrDefault()
            ?? "image/svg+xml";

        if (accept.Contains("text/html", StringComparison.OrdinalIgnoreCase))
            return Content(BuildFromJson(body).RenderToHtml(), "text/html");

        if (accept.Contains("text/plain", StringComparison.OrdinalIgnoreCase)
            || accept.Contains("datauri",  StringComparison.OrdinalIgnoreCase)
            || string.Equals(format, "datauri", StringComparison.OrdinalIgnoreCase))
            return Content(DataUriWithFont(BuildFromJson(body)), "text/plain");

        if (accept.Contains("image/png", StringComparison.OrdinalIgnoreCase)
            || string.Equals(format, "png", StringComparison.OrdinalIgnoreCase))
            return RenderPng(body);

        if (accept.Contains("image/jpeg", StringComparison.OrdinalIgnoreCase)
            || accept.Contains("image/jpg",  StringComparison.OrdinalIgnoreCase)
            || string.Equals(format, "jpeg", StringComparison.OrdinalIgnoreCase)
            || string.Equals(format, "jpg",  StringComparison.OrdinalIgnoreCase))
            return RenderJpeg(body);

        return Content(SvgWithFont(BuildFromJson(body)), "image/svg+xml");
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
                string fmt = items[i].Format?.ToLowerInvariant() ?? "svg";
                var    b   = fmt is "png" or "jpg" or "jpeg"
                    ? BuildFromJsonStatic(items[i].Options)
                    : BuildFromJson(items[i].Options);
                string output = fmt switch
                {
                    "html"    => b.RenderToHtml(),
                    "datauri" => DataUriWithFont(b),
                    "png"     => BuildRasterHtml(b.RenderToSvg(), "image/png",  BuildRasterFilename(b.GetOptions().Title?.Text, "png")),
                    "jpg" or "jpeg" => BuildRasterHtml(b.RenderToSvg(), "image/jpeg", BuildRasterFilename(b.GetOptions().Title?.Text, "jpg")),
                    _         => SvgWithFont(b)
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
        "Vivid", "Default", "Dark", "Pastel", "Monochrome", "Ocean", "Sunset", "Forest",
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

        var builder = ChartBuilder.FromJson(body.GetRawText());
        ApplyThemeName(builder, body);
        return builder;
    }

    // Decodes a Base64 string (standard or URL-safe, padding optional) to its UTF-8 text.
    private static string DecodeBase64Json(string encoded)
    {
        string s = encoded.Trim().Replace('-', '+').Replace('_', '/');
        switch (s.Length % 4)
        {
            case 2: s += "=="; break;
            case 3: s += "="; break;
        }
        return System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(s));
    }

    // Applies a built-in theme when the body carries a top-level "themeName" string.
    // Vivid is used as the fallback so charts render with the vivid palette by default.
    // An optional top-level "fontScale" number multiplies all chart text.
    private static void ApplyThemeName(ChartBuilder builder, JsonElement body)
    {
        string? name = null;
        if (body.ValueKind == JsonValueKind.Object
            && body.TryGetProperty("themeName", out var tn)
            && tn.ValueKind == JsonValueKind.String)
            name = tn.GetString();

        var theme = ResolveTheme(name) ?? ChartTheme.Vivid;

        // Clone before scaling so the shared static preset instance is never mutated.
        if (body.ValueKind == JsonValueKind.Object
            && body.TryGetProperty("fontScale", out var fs)
            && fs.ValueKind == JsonValueKind.Number
            && fs.TryGetDouble(out var scale)
            && scale > 0)
        {
            theme = theme.Clone();
            theme.FontScale = scale;
        }

        builder.Theme(theme);
    }

    private static ChartTheme? ResolveTheme(string? name) => name?.Trim().ToLowerInvariant() switch
    {
        "vivid"        => ChartTheme.Vivid,
        "default"      => ChartTheme.Default,
        "dark"         => ChartTheme.Dark,
        "pastel"       => ChartTheme.Pastel,
        "monochrome"   => ChartTheme.Monochrome,
        "ocean"        => ChartTheme.Ocean,
        "sunset"       => ChartTheme.Sunset,
        "forest"       => ChartTheme.Forest,
        "neon"         => ChartTheme.Neon,
        "minimal"      => ChartTheme.Minimal,
        "warm"         => ChartTheme.Warm,
        "arctic"       => ChartTheme.Arctic,
        "business"     => ChartTheme.Business,
        "material"     => ChartTheme.Material,
        "trafficlight" => ChartTheme.TrafficLight,
        "accessible"   => ChartTheme.Accessible,
        "highcontrast" => ChartTheme.HighContrast,
        _              => null,
    };

    // Forces Static render mode so raster pipelines receive clean SVG (no JS, no hover rules).
    private static ChartBuilder BuildFromJsonStatic(JsonElement body)
    {
        var builder = BuildFromJson(body);
        builder.GetOptions().RenderMode = SvgMode.Static;
        return builder;
    }

    // Renders SVG with the chart's theme font embedded as base64 @font-face so the standalone file
    // is self-contained (page @font-face does not apply to a downloaded/isolated SVG).
    private static string SvgWithFont(ChartBuilder builder) =>
        Rendering.EmbeddedFontCss.InjectInto(builder.RenderToSvg());

    // Data URI over the font-embedded SVG (for <img src> / CSS background use where the SVG is isolated).
    private static string DataUriWithFont(ChartBuilder builder)
    {
        string svg = SvgWithFont(builder);
        return "data:image/svg+xml;base64," +
            System.Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(svg));
    }

    // Returns a self-contained HTML page that rasterizes svgContent via browser Canvas and
    // auto-triggers a download — the same Canvas pipeline as the library's exportRaster() JS.
    private static string BuildRasterHtml(string svgContent, string mime, string filename)
    {
        bool isJpeg = mime == "image/jpeg";
        // Inline the theme font so the rasterized SVG (loaded as an isolated <img>) embeds it.
        svgContent = Rendering.EmbeddedFontCss.InjectInto(svgContent);
        // Escape for safe embedding inside a JS single-quoted string literal.
        string jsFilename = filename.Replace("\\", "\\\\").Replace("'", "\\'");
        string bgStep     = isJpeg
            ? "ctx.fillStyle='#fff'; ctx.fillRect(0,0,w,h);" // white fill before draw — same as library JS
            : "// PNG keeps alpha channel — no fill needed";

        var sb = new System.Text.StringBuilder();
        sb.AppendLine("<!DOCTYPE html>");
        sb.AppendLine("<html lang=\"en\">");
        sb.AppendLine("<head>");
        sb.AppendLine("  <meta charset=\"utf-8\">");
        sb.AppendLine("  <title>Chart export</title>");
        sb.AppendLine("  <style>body{margin:0;display:flex;flex-direction:column;align-items:center;");
        sb.AppendLine("    justify-content:center;min-height:100vh;background:#f5f5f5;font-family:sans-serif;color:#444}</style>");
        sb.AppendLine("</head>");
        sb.AppendLine("<body>");
        sb.AppendLine("  <div id=\"svg-host\" style=\"display:none\">");
        sb.AppendLine(svgContent);
        sb.AppendLine("  </div>");
        sb.AppendLine("  <p id=\"msg\">Preparing download&hellip;</p>");
        sb.AppendLine("  <script>");
        sb.AppendLine("  (function () {");
        sb.AppendLine("    var svg = document.querySelector('#svg-host svg');");
        sb.AppendLine("    var vb  = svg.viewBox && svg.viewBox.baseVal ? svg.viewBox.baseVal : null;");
        sb.AppendLine("    var w   = (vb && vb.width)  ? vb.width  : (parseInt(svg.getAttribute('width')  || '800', 10));");
        sb.AppendLine("    var h   = (vb && vb.height) ? vb.height : (parseInt(svg.getAttribute('height') || '600', 10));");
        sb.AppendLine("    w = Math.max(1, Math.round(w));");
        sb.AppendLine("    h = Math.max(1, Math.round(h));");
        sb.AppendLine("    var xml  = new XMLSerializer().serializeToString(svg);");
        sb.AppendLine("    var blob = new Blob([xml], { type: 'image/svg+xml' });");
        sb.AppendLine("    var url  = URL.createObjectURL(blob);");
        sb.AppendLine("    var img  = new Image(); img.width = w; img.height = h;");
        sb.AppendLine("    img.onload = function () {");
        sb.AppendLine("      var canvas = document.createElement('canvas');");
        sb.AppendLine("      canvas.width = w; canvas.height = h;");
        sb.AppendLine("      var ctx = canvas.getContext('2d');");
        sb.AppendLine($"      {bgStep}");
        sb.AppendLine("      ctx.drawImage(img, 0, 0, w, h);");
        sb.AppendLine("      URL.revokeObjectURL(url);");
        sb.AppendLine($"      var data = canvas.toDataURL('{mime}', 0.95);");
        sb.AppendLine("      var a = document.createElement('a');");
        sb.AppendLine($"      a.href = data; a.download = '{jsFilename}';");
        sb.AppendLine("      document.body.appendChild(a); a.click(); document.body.removeChild(a);");
        sb.AppendLine("      document.getElementById('msg').textContent = 'Download started. You may close this tab.';");
        sb.AppendLine("    };");
        sb.AppendLine("    img.onerror = function () {");
        sb.AppendLine("      document.getElementById('msg').textContent = 'Rasterization failed \u2014 browser could not load the SVG.';");
        sb.AppendLine("    };");
        sb.AppendLine("    img.src = url;");
        sb.AppendLine("  })();");
        sb.AppendLine("  </script>");
        sb.AppendLine("</body>");
        sb.Append("</html>");
        return sb.ToString();
    }

    // Produces a safe ASCII filename (letters/digits/hyphens only, max 50 chars).
    private static string BuildRasterFilename(string? title, string ext)
    {
        if (string.IsNullOrWhiteSpace(title)) return $"chart.{ext}";
        var sb = new System.Text.StringBuilder();
        foreach (char c in title)
            if (char.IsLetterOrDigit(c) || c == '-' || c == '_') sb.Append(c);
            else if (c == ' ' && sb.Length > 0 && sb[sb.Length - 1] != '-') sb.Append('-');
        string name = sb.ToString().Trim('-');
        if (name.Length > 50) name = name.Substring(0, 50);
        return $"{(name.Length > 0 ? name : "chart")}.{ext}";
    }

    private static string RenderModeDescription(SvgMode mode) => mode switch
    {
        SvgMode.Static      => "No CSS hover, no JavaScript. Safe for PDF and email.",
        SvgMode.Animated    => "SMIL/CSS animations, no JavaScript. Suitable for Blazor and browsers.",
        SvgMode.Interactive => "CSS hover effects and embedded JavaScript. Browser-only.",
        _                   => string.Empty
    };
}
