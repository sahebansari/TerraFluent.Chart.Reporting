using System.Globalization;
using System.Text;
using TerraFluent.AutoAnalytics;
using TerraFluent.AutoAnalytics.Comparison;
using TerraFluent.AutoAnalytics.Insights;
using TerraFluent.AutoAnalytics.Recommendation;
using TerraFluent.Chart.Reporting.Api.Models;

namespace TerraFluent.Chart.Reporting.Api.Rendering;

/// <summary>
/// Renders a <see cref="DatasetComparison"/> into a self-contained HTML page — the export
/// counterpart of the web "Compare" view, mirroring the analysis and dashboard exports.
/// </summary>
internal static class CompareHtmlRenderer
{
    public static string Render(
        DatasetComparison c, IReadOnlyList<Insight> insights,
        IReadOnlyList<RecommendedChart> charts, ChartStyle style = default)
    {
        var sb = new StringBuilder();
        string title = $"{c.BaselineName} vs {c.CurrentName}";

        sb.AppendLine("<!DOCTYPE html>");
        sb.AppendLine("<html lang=\"en\"><head><meta charset=\"utf-8\">");
        sb.AppendLine("<meta name=\"viewport\" content=\"width=device-width, initial-scale=1\">");
        sb.AppendLine($"<title>{Enc(title)} — Comparison</title>");
        sb.AppendLine(EmbeddedFontCss.StyleBlock);
        sb.AppendLine(Styles());
        sb.AppendLine("</head><body>");

        // Header.
        sb.AppendLine("<header class=\"hero\">");
        sb.AppendLine($"  <h1>{Enc(title)}</h1>");
        sb.AppendLine($"  <p class=\"headline\">{Enc(c.Headline)}</p>");
        sb.AppendLine("  <div class=\"meta\">");
        sb.AppendLine($"    <span>{c.BaselineRowCount} → {c.CurrentRowCount} rows</span>");
        sb.AppendLine($"    <span>{c.SharedMeasures.Count} shared measure(s)</span>");
        sb.AppendLine($"    <span>{c.SchemaChanges.Count} structural change(s)</span>");
        sb.AppendLine(c.IsComparable
            ? "    <span class=\"quality quality-clean\">Comparable</span>"
            : "    <span class=\"quality quality-errors\">Not comparable</span>");
        sb.AppendLine("  </div>");
        sb.AppendLine("</header><main>");

        // Compatibility note — the caveat has to come before the numbers, not after them.
        sb.AppendLine("<section><div class=\"summary-head\"><h2>How these line up</h2>");
        sb.AppendLine(c.IsComparable
            ? "<span class=\"badge ok\">Comparable</span>"
            : "<span class=\"badge err\">Not comparable</span>");
        sb.AppendLine($"</div><p class=\"note\">{Enc(c.CompatibilityNote)}</p></section>");

        // Insights.
        sb.AppendLine("<section><h2>What changed</h2><ul class=\"panel insights\">");
        if (insights.Count == 0)
            sb.AppendLine("  <li><div><strong>No material differences</strong><span>The two datasets look equivalent.</span></div></li>");
        foreach (var insight in insights)
        {
            sb.AppendLine("  <li>");
            sb.AppendLine($"    <span class=\"score\">{insight.ImportanceScore}</span>");
            sb.AppendLine($"    <div><strong>{Enc(insight.Title)}</strong><span>{Enc(insight.Description)}</span></div>");
            sb.AppendLine("  </li>");
        }
        sb.AppendLine("</ul></section>");

        // Measure movement.
        if (c.MeasureDeltas.Count > 0)
        {
            sb.AppendLine("<section><h2>Measures</h2><div class=\"table-wrap\"><table class=\"data\">");
            sb.AppendLine($"<thead><tr><th>Measure</th><th>Compared on</th>" +
                          $"<th class=\"num\">{Enc(c.BaselineName)}</th><th class=\"num\">{Enc(c.CurrentName)}</th>" +
                          "<th class=\"num\">Change</th><th class=\"num\">Change %</th><th>Trend</th></tr></thead><tbody>");
            foreach (var d in c.MeasureDeltas)
            {
                string pct = d.DeltaPct is double p
                    ? Signed(p * 100, "0.#") + "%"
                    : "—";
                string trend = d.TrendReversed
                    ? $"<span class=\"tag warnflag\">{Enc(d.BaselineTrend.ToString())} → {Enc(d.CurrentTrend.ToString())}</span>"
                    : $"<span class=\"tag muted\">{Enc(d.CurrentTrend.ToString())}</span>";

                sb.AppendLine("  <tr>" +
                    $"<td><strong>{Enc(DisplayText.Humanize(d.Measure))}</strong></td>" +
                    $"<td><span class=\"tag info\">{Enc(d.Statistic)}</span></td>" +
                    $"<td class=\"num\">{Enc(DisplayText.FormatNumber(d.BaselineValue))}</td>" +
                    $"<td class=\"num\">{Enc(DisplayText.FormatNumber(d.CurrentValue))}</td>" +
                    $"<td class=\"num {Direction(d.Delta)}\">{Enc(Signed(d.Delta, "0.##"))}</td>" +
                    $"<td class=\"num {Direction(d.Delta)}\">{Enc(pct)}</td>" +
                    $"<td>{trend}</td></tr>");
            }
            sb.AppendLine("</tbody></table></div></section>");
        }

        // Category mix.
        if (c.CategoryShifts.Count > 0)
        {
            sb.AppendLine("<section><h2>Category mix</h2><div class=\"table-wrap\"><table class=\"data\">");
            sb.AppendLine("<thead><tr><th>Dimension</th><th>Category</th><th>Measure</th>" +
                          "<th class=\"num\">Share before</th><th class=\"num\">Share after</th>" +
                          "<th class=\"num\">Shift</th><th>Status</th></tr></thead><tbody>");
            foreach (var s in c.CategoryShifts)
            {
                string status = s.IsNew ? "<span class=\"tag ok\">new</span>"
                    : s.IsGone ? "<span class=\"tag err\">gone</span>"
                    : "<span class=\"tag muted\">present</span>";

                sb.AppendLine("  <tr>" +
                    $"<td>{Enc(DisplayText.Humanize(s.Dimension))}</td>" +
                    $"<td><strong>{Enc(s.Category)}</strong></td>" +
                    $"<td>{Enc(DisplayText.Humanize(s.Measure))}</td>" +
                    $"<td class=\"num\">{Pct(s.BaselineShare)}</td>" +
                    $"<td class=\"num\">{Pct(s.CurrentShare)}</td>" +
                    $"<td class=\"num {Direction(s.ShareDelta)}\">{Enc(DatasetComparisonEnginePoints(s.ShareDelta))}</td>" +
                    $"<td>{status}</td></tr>");
            }
            sb.AppendLine("</tbody></table></div></section>");
        }

        // Structural differences.
        if (c.SchemaChanges.Count > 0)
        {
            sb.AppendLine("<section><h2>Structural differences</h2><ul class=\"panel\">");
            foreach (var change in c.SchemaChanges)
                sb.AppendLine($"  <li><span class=\"tag {KindClass(change.Kind)}\">{Enc(change.Kind.ToString())}</span>" +
                              $"<div><strong>{Enc(DisplayText.Humanize(change.Column))}</strong>" +
                              $"<span>{Enc(change.Description)}</span></div></li>");
            sb.AppendLine("</ul></section>");
        }

        // Charts.
        if (charts.Count > 0)
        {
            sb.AppendLine("<section><h2>Charts</h2><div class=\"chart-grid\">");
            foreach (var chart in charts)
            {
                string svg = style.Apply(ChartConfigBuilder.ToChartBuilder(chart.Spec).Size(560, 340)).RenderToSvg();
                sb.AppendLine("  <figure class=\"chart-card\">");
                sb.AppendLine($"    <div class=\"chart-badge\">{Enc(chart.ChartName)} · suitability {chart.SuitabilityScore}</div>");
                sb.AppendLine($"    {svg}");
                sb.AppendLine($"    <figcaption><strong>{Enc(chart.Spec.Title)}</strong><br>{Enc(chart.Reason)}</figcaption>");
                sb.AppendLine("  </figure>");
            }
            sb.AppendLine("</div></section>");
        }

        sb.AppendLine("</main>");
        sb.AppendLine("<footer>Generated by TerraFluent.AutoAnalytics — deterministic, no AI.</footer>");
        sb.AppendLine("</body></html>");
        return sb.ToString();
    }

    private static string DatasetComparisonEnginePoints(double shareDelta) =>
        (shareDelta >= 0 ? "+" : "-") +
        (System.Math.Abs(shareDelta) * 100).ToString("0.#", CultureInfo.InvariantCulture) + " pts";

    private static string Signed(double value, string format) =>
        (value >= 0 ? "+" : "-") + System.Math.Abs(value).ToString(format, CultureInfo.InvariantCulture);

    private static string Pct(double fraction) =>
        (fraction * 100).ToString("0.#", CultureInfo.InvariantCulture) + "%";

    // Colour cues only; the sign is always spelled out too, so this never carries meaning alone.
    private static string Direction(double value) =>
        System.Math.Abs(value) < 1e-9 ? string.Empty : value > 0 ? "up" : "down";

    private static string KindClass(SchemaChangeKind kind) => kind switch
    {
        SchemaChangeKind.Added => "ok",
        SchemaChangeKind.Removed => "err",
        SchemaChangeKind.TypeChanged => "warnflag",
        _ => "muted"
    };

    private static string Enc(string value) => value
        .Replace("&", "&amp;")
        .Replace("<", "&lt;")
        .Replace(">", "&gt;")
        .Replace("\"", "&quot;");

    private static string Styles() => """
        <style>
          * { box-sizing: border-box; }
          body { margin:0; font-family:'Inter','Segoe UI',system-ui,sans-serif; background:#f1f5f9; color:#1e293b; }
          .hero { background:linear-gradient(135deg,#0f766e,#0ea5e9); color:#fff; padding:32px 40px; }
          .hero h1 { margin:0 0 6px; font-size:28px; }
          .hero .headline { margin:0 0 14px; font-size:16px; opacity:.92; }
          .meta { display:flex; flex-wrap:wrap; gap:10px; }
          .meta span { background:rgba(255,255,255,.15); padding:4px 12px; border-radius:999px; font-size:13px; }
          .quality-clean { background:rgba(34,197,94,.35) !important; }
          .quality-errors { background:rgba(239,68,68,.45) !important; }
          main { padding:28px 40px; max-width:1280px; margin:0 auto; }
          section { margin-bottom:34px; background:#fff; border-radius:14px; padding:22px 24px; box-shadow:0 1px 3px rgba(0,0,0,.08); }
          section h2 { font-size:18px; margin:0 0 16px; color:#334155; }
          .summary-head { display:flex; align-items:center; justify-content:space-between; gap:12px; }
          .summary-head h2 { margin:0; }
          .note { margin:12px 0 0; color:#475569; font-size:15px; }
          .badge { font-size:12px; font-weight:700; padding:4px 12px; border-radius:999px; }
          .badge.ok { background:#dcfce7; color:#166534; }
          .badge.err { background:#fee2e2; color:#991b1b; }
          .chart-grid { display:grid; grid-template-columns:repeat(auto-fit,minmax(420px,1fr)); gap:20px; }
          .chart-card { margin:0; background:#f8fafc; border-radius:14px; padding:16px; }
          .chart-card svg { width:100%; height:auto; }
          .chart-badge { display:inline-block; background:#ecfeff; color:#0e7490; font-size:12px; font-weight:600; padding:3px 10px; border-radius:999px; margin-bottom:10px; }
          .chart-card figcaption { margin-top:10px; font-size:13px; color:#64748b; }
          .panel { list-style:none; margin:0; padding:0; display:flex; flex-direction:column; gap:10px; }
          .panel li { background:#f8fafc; border-radius:12px; padding:14px 16px; display:flex; gap:12px; align-items:flex-start; }
          .panel strong { display:block; }
          .panel span { color:#64748b; font-size:14px; }
          .insights .score { background:#0f766e; color:#fff; font-weight:700; border-radius:8px; padding:4px 10px; font-size:13px; min-width:34px; text-align:center; }
          .table-wrap { overflow-x:auto; }
          table.data { width:100%; border-collapse:collapse; font-size:14px; }
          table.data th, table.data td { padding:8px 12px; text-align:left; border-bottom:1px solid #e2e8f0; }
          table.data th.num, table.data td.num { text-align:right; }
          td.up { color:#15803d; font-weight:600; }
          td.down { color:#b91c1c; font-weight:600; }
          .tag { display:inline-block; font-size:12px; font-weight:600; padding:2px 9px; border-radius:999px; }
          .tag.muted { background:#f1f5f9; color:#475569; }
          .tag.info { background:#e0f2fe; color:#075985; }
          .tag.ok { background:#dcfce7; color:#166534; }
          .tag.err { background:#fee2e2; color:#991b1b; }
          .tag.warnflag { background:#fef9c3; color:#854d0e; }
          footer { text-align:center; padding:20px; color:#94a3b8; font-size:13px; }
        </style>
        """;
}
