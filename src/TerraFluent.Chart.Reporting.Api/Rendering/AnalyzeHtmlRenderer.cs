using System.Globalization;
using System.Text;
using TerraFluent.AutoAnalytics;
using TerraFluent.AutoAnalytics.Engine;
using TerraFluent.AutoAnalytics.Recommendation;

namespace TerraFluent.Chart.Reporting.Api.Rendering;

/// <summary>
/// Renders a full <see cref="AnalyticsResult"/> (summary, ranked insights, chart recommendations
/// and column profiles) into a self-contained HTML page — the export counterpart of the web
/// "Analyze" view, mirroring the dashboard export.
/// </summary>
internal static class AnalyzeHtmlRenderer
{
    public static string Render(AnalyticsResult result)
    {
        var s = result.Summary;
        var v = result.Validation;
        var sb = new StringBuilder();

        sb.AppendLine("<!DOCTYPE html>");
        sb.AppendLine("<html lang=\"en\"><head><meta charset=\"utf-8\">");
        sb.AppendLine("<meta name=\"viewport\" content=\"width=device-width, initial-scale=1\">");
        sb.AppendLine($"<title>{Enc(s.DatasetName)} — Analysis</title>");
        sb.AppendLine(Styles());
        sb.AppendLine("</head><body>");

        string vBadge = v.HasErrors ? "<span class=\"badge err\">Errors</span>"
            : v.IsClean ? "<span class=\"badge ok\">Clean</span>"
            : "<span class=\"badge warn\">Warnings</span>";

        sb.AppendLine("<header class=\"hero\">");
        sb.AppendLine($"  <h1>{Enc(s.DatasetName)} — Analysis</h1>");
        sb.AppendLine($"  <p class=\"headline\">{Enc(s.Headline)}</p>");
        sb.AppendLine("  <div class=\"meta\">");
        sb.AppendLine($"    <span>{s.RowCount} rows</span><span>{s.ColumnCount} columns</span>");
        sb.AppendLine($"    <span>{s.InsightCount} insights</span><span>{s.RecommendationCount} recommendations</span>");
        sb.AppendLine($"    <span class=\"quality quality-{s.DataQuality.ToLowerInvariant()}\">Data quality: {Enc(s.DataQuality)}</span>");
        sb.AppendLine("  </div>");
        sb.AppendLine("</header><main>");

        // Summary + key findings.
        sb.AppendLine("<section><div class=\"summary-head\">");
        sb.AppendLine($"  <h2>{Enc(s.Headline)}</h2>{vBadge}");
        sb.AppendLine("</div>");
        if (s.KeyFindings.Count > 0)
        {
            sb.AppendLine("<h3>Key findings</h3><ul class=\"findings\">");
            foreach (var f in s.KeyFindings)
                sb.AppendLine($"  <li>{Enc(f)}</li>");
            sb.AppendLine("</ul>");
        }
        sb.AppendLine("</section>");

        // Insights.
        sb.AppendLine("<section><h2>Insights</h2><ul class=\"panel insights\">");
        if (result.Insights.Count == 0)
            sb.AppendLine("  <li><div><strong>No notable insights</strong><span>Nothing stood out in this dataset.</span></div></li>");
        foreach (var insight in result.Insights)
        {
            bool isAnomaly = insight.Kind == TerraFluent.AutoAnalytics.Enums.InsightKind.Anomaly;
            string scoreTitle = isAnomaly
                ? "Severity score (0\u2013100): how far this anomaly stands out from the norm"
                : "Importance score (0\u2013100): how noteworthy this insight is";
            sb.AppendLine("  <li>");
            sb.AppendLine($"    <span class=\"score tip{(isAnomaly ? " anomaly" : string.Empty)}\" data-tip=\"{scoreTitle}\">{insight.ImportanceScore}</span>");
            sb.AppendLine($"    <div><strong>{Enc(insight.Title)}</strong><span>{Enc(insight.Description)}</span></div>");
            sb.AppendLine("  </li>");
        }
        sb.AppendLine("</ul></section>");

        // Chart recommendations.
        if (result.Recommendations.Count > 0)
        {
            sb.AppendLine("<section><h2>Chart recommendations</h2><div class=\"chart-grid\">");
            foreach (var rec in result.Recommendations)
            {
                string svg = ChartConfigBuilder.ToChartBuilder(rec.Spec).Size(560, 340).RenderToSvg();
                sb.AppendLine("  <figure class=\"chart-card\">");
                sb.AppendLine($"    <div class=\"chart-badge\">{Enc(rec.ChartName)} · suitability {rec.SuitabilityScore}</div>");
                sb.AppendLine($"    {svg}");
                sb.AppendLine($"    <figcaption><strong>{Enc(rec.Spec.Title)}</strong><br>{Enc(rec.Reason)}</figcaption>");
                sb.AppendLine("  </figure>");
            }
            sb.AppendLine("</div></section>");
        }

        // Column profiles.
        sb.AppendLine("<section><h2>Column profiles</h2><div class=\"table-wrap\"><table class=\"data\">");
        sb.AppendLine("<thead><tr><th>Column</th><th>Type</th><th>Role</th><th class=\"num\">Distinct</th>" +
                      "<th class=\"num\">Complete</th><th class=\"num\">Min</th><th class=\"num\">Max</th>" +
                      "<th class=\"num\">Mean</th><th class=\"num\">Sum</th></tr></thead><tbody>");
        foreach (var c in result.Profile.Columns)
        {
            var p = c.Profile;
            sb.AppendLine("  <tr>" +
                $"<td><strong>{Enc(p.Name)}</strong></td>" +
                $"<td><span class=\"tag muted\">{Enc(p.Type.ToString())}</span></td>" +
                $"<td><span class=\"tag info\">{Enc(p.Role.ToString())}</span></td>" +
                $"<td class=\"num\">{p.DistinctCount}</td>" +
                $"<td class=\"num\">{(p.Completeness * 100).ToString("0", CultureInfo.InvariantCulture)}%</td>" +
                $"<td class=\"num\">{Num(c.Numeric?.Min)}</td>" +
                $"<td class=\"num\">{Num(c.Numeric?.Max)}</td>" +
                $"<td class=\"num\">{Num(c.Numeric?.Mean)}</td>" +
                $"<td class=\"num\">{Num(c.Numeric?.Sum)}</td></tr>");
        }
        sb.AppendLine("</tbody></table></div></section>");

        sb.AppendLine("</main>");
        sb.AppendLine("<footer>Generated by TerraFluent.AutoAnalytics — deterministic, no AI.</footer>");
        sb.AppendLine("</body></html>");
        return sb.ToString();
    }

    private static string Num(double? value) =>
        value is double d ? Enc(DisplayText.FormatNumber(d)) : "—";

    private static string Enc(string value) => value
        .Replace("&", "&amp;")
        .Replace("<", "&lt;")
        .Replace(">", "&gt;")
        .Replace("\"", "&quot;");

    private static string Styles() => """
        <style>
          * { box-sizing: border-box; }
          body { margin:0; font-family:'Segoe UI',system-ui,sans-serif; background:#f1f5f9; color:#1e293b; }
          .hero { background:linear-gradient(135deg,#4f46e5,#7c3aed); color:#fff; padding:32px 40px; }
          .hero h1 { margin:0 0 6px; font-size:28px; }
          .hero .headline { margin:0 0 14px; font-size:16px; opacity:.92; }
          .meta { display:flex; flex-wrap:wrap; gap:10px; }
          .meta span { background:rgba(255,255,255,.15); padding:4px 12px; border-radius:999px; font-size:13px; }
          .quality-clean { background:rgba(34,197,94,.35) !important; }
          .quality-warnings { background:rgba(234,179,8,.4) !important; }
          .quality-errors { background:rgba(239,68,68,.45) !important; }
          main { padding:28px 40px; max-width:1280px; margin:0 auto; }
          section { margin-bottom:34px; background:#fff; border-radius:14px; padding:22px 24px; box-shadow:0 1px 3px rgba(0,0,0,.08); }
          section h2 { font-size:18px; margin:0 0 16px; color:#334155; }
          section h3 { font-size:15px; margin:18px 0 8px; color:#475569; }
          .summary-head { display:flex; align-items:center; justify-content:space-between; gap:12px; }
          .summary-head h2 { margin:0; }
          .findings { margin:0; padding-left:20px; color:#475569; }
          .findings li { margin:4px 0; }
          .badge { font-size:12px; font-weight:700; padding:4px 12px; border-radius:999px; }
          .badge.ok { background:#dcfce7; color:#166534; }
          .badge.warn { background:#fef9c3; color:#854d0e; }
          .badge.err { background:#fee2e2; color:#991b1b; }
          .chart-grid { display:grid; grid-template-columns:repeat(auto-fit,minmax(420px,1fr)); gap:20px; }
          .chart-card { margin:0; background:#f8fafc; border-radius:14px; padding:16px; }
          .chart-card svg { width:100%; height:auto; }
          .chart-badge { display:inline-block; background:#eef2ff; color:#4f46e5; font-size:12px; font-weight:600; padding:3px 10px; border-radius:999px; margin-bottom:10px; }
          .chart-card figcaption { margin-top:10px; font-size:13px; color:#64748b; }
          .panel { list-style:none; margin:0; padding:0; display:flex; flex-direction:column; gap:10px; }
          .panel li { background:#f8fafc; border-radius:12px; padding:14px 16px; display:flex; gap:12px; align-items:flex-start; }
          .panel strong { display:block; }
          .panel span { color:#64748b; font-size:14px; }
          .insights .score { background:#5b21b6; color:#fff; font-weight:700; border-radius:8px; padding:4px 10px; font-size:13px; min-width:34px; text-align:center; }
          .insights .score.anomaly { background:#ef4444; }
          .tip { position:relative; }
          .tip::after { content:attr(data-tip); position:absolute; left:0; bottom:calc(100% + 9px); width:max-content; max-width:240px; white-space:normal; text-align:left; background:#1e293b; color:#fff; font-size:12px; font-weight:500; line-height:1.4; padding:8px 10px; border-radius:8px; box-shadow:0 8px 24px rgba(15,23,42,.22); opacity:0; visibility:hidden; transform:translateY(4px); transition:opacity .15s ease, transform .15s ease; pointer-events:none; z-index:60; }
          .tip::before { content:""; position:absolute; left:16px; bottom:calc(100% + 3px); border:6px solid transparent; border-top-color:#1e293b; opacity:0; visibility:hidden; transition:opacity .15s ease; pointer-events:none; z-index:60; }
          .tip:hover::after { opacity:1; visibility:visible; transform:translateY(0); }
          .tip:hover::before { opacity:1; visibility:visible; }
          .table-wrap { overflow-x:auto; }
          table.data { width:100%; border-collapse:collapse; font-size:14px; }
          table.data th, table.data td { padding:8px 12px; text-align:left; border-bottom:1px solid #e2e8f0; }
          table.data th.num, table.data td.num { text-align:right; }
          .tag { display:inline-block; font-size:12px; font-weight:600; padding:2px 9px; border-radius:999px; }
          .tag.muted { background:#f1f5f9; color:#475569; }
          .tag.info { background:#e0f2fe; color:#075985; }
          footer { text-align:center; padding:20px; color:#94a3b8; font-size:13px; }
        </style>
        """;
}
