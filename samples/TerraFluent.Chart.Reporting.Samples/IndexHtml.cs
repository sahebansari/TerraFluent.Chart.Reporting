using System.Text;
using TerraFluent.AutoAnalytics.Dashboard;
using TerraFluent.AutoAnalytics.Engine;
using TerraFluent.AutoAnalytics.Recommendation;

namespace TerraFluent.Chart.Reporting.Samples;

/// <summary>
/// Builds the HTML output pages for the sample app: the <c>index.html</c> chart showcase and the
/// AutoAnalytics <c>dashboard.html</c>. Split from <see cref="Program"/> to keep orchestration lean.
/// </summary>
internal static partial class Program
{
    private static void GenerateIndexHtml(IEnumerable<ChartEntry> charts)
    {
        var sb = new StringBuilder();
        sb.AppendLine("<!DOCTYPE html>");
        sb.AppendLine("<html lang=\"en\">");
        sb.AppendLine("<head>");
        sb.AppendLine("  <meta charset=\"UTF-8\" />");
        sb.AppendLine("  <meta name=\"viewport\" content=\"width=device-width, initial-scale=1.0\" />");
        sb.AppendLine("  <title>TerraFluent.Chart.Reporting \u2014 Chart Showcase</title>");
        sb.AppendLine("  <style>");
        sb.AppendLine("    *, *::before, *::after { box-sizing: border-box; margin: 0; padding: 0; }");
        sb.AppendLine("    body { font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', sans-serif; background: #f0f2f5; color: #333; }");
        sb.AppendLine("    header { background: #1a1f36; color: #fff; padding: 2rem 2.5rem; }");
        sb.AppendLine("    header h1 { font-size: 1.6rem; font-weight: 700; }");
        sb.AppendLine("    header p  { margin-top: 0.4rem; color: #a0aec0; font-size: 0.9rem; }");
        sb.AppendLine("    main { max-width: 1600px; margin: 2rem auto; padding: 0 1.5rem 4rem; }");
        sb.AppendLine("    .grid { display: grid; grid-template-columns: repeat(auto-fill, minmax(720px, 1fr)); gap: 1.75rem; }");
        sb.AppendLine("    .card { background: #fff; border-radius: 12px; box-shadow: 0 2px 12px rgba(0,0,0,.08); overflow: hidden; }");
        sb.AppendLine("    .card-header { padding: 1.1rem 1.4rem 0.6rem; border-bottom: 1px solid #edf2f7; }");
        sb.AppendLine("    .card-header h2 { font-size: 1rem; font-weight: 600; color: #1a202c; }");
        sb.AppendLine("    .card-header p  { margin-top: 0.25rem; font-size: 0.8rem; color: #718096; }");
        sb.AppendLine("    .card-body { padding: 1rem 1rem 0.5rem; display: flex; justify-content: center; }");
        sb.AppendLine("    .card-body svg { max-width: 100%; height: auto; }");
        sb.AppendLine("    .card-footer { padding: 0.5rem 1.4rem 0.9rem; }");
        sb.AppendLine("    .card-footer a { font-size: 0.78rem; color: #4a90d9; text-decoration: none; }");
        sb.AppendLine("    .card-footer a:hover { text-decoration: underline; }");
        sb.AppendLine("    footer { text-align: center; padding: 2rem; color: #a0aec0; font-size: 0.8rem; }");
        sb.AppendLine("  </style>");
        sb.AppendLine("  <script>");
        sb.AppendLine("    function displayPointData(pt) {");
        sb.AppendLine("      alert(");
        sb.AppendLine("        'Index:    ' + pt.index    + '\\n' +");
        sb.AppendLine("        'Value:    ' + pt.value    + '\\n' +");
        sb.AppendLine("        'Name:     ' + pt.name     + '\\n' +");
        sb.AppendLine("        'Color:    ' + pt.color    + '\\n' +");
        sb.AppendLine("        'Category: ' + pt.category");
        sb.AppendLine("      );");
        sb.AppendLine("    }");
        sb.AppendLine("  </script>");
        sb.AppendLine("</head>");
        sb.AppendLine("<body>");
        sb.AppendLine("  <header>");
        sb.AppendLine("    <h1>TerraFluent.Chart.Reporting</h1>");
        sb.AppendLine("    <p>Server-side SVG chart generation for .NET \u2014 zero JavaScript dependency.</p>");
        sb.AppendLine("  </header>");
        sb.AppendLine("  <main>");
        sb.AppendLine("    <div class=\"grid\">");

        int index = 1;
        foreach (var chart in charts)
        {
            if (string.IsNullOrEmpty(chart.Svg)) continue;
            sb.AppendLine("      <div class=\"card\">");
            sb.AppendLine("        <div class=\"card-header\">");
            sb.AppendLine($"          <h2>{index:D2} &mdash; {HtmlEncode(chart.Title)}</h2>");
            sb.AppendLine($"          <p>{HtmlEncode(chart.Description)}</p>");
            sb.AppendLine("        </div>");
            sb.AppendLine("        <div class=\"card-body\">");
            string inlineSvg = chart.Svg.TrimStart();
            if (inlineSvg.StartsWith("<?xml", StringComparison.OrdinalIgnoreCase))
            {
                int end = inlineSvg.IndexOf("?>", StringComparison.Ordinal);
                if (end >= 0) inlineSvg = inlineSvg[(end + 2)..].TrimStart();
            }
            sb.AppendLine("          " + inlineSvg.Replace("\n", "\n          ").TrimEnd());
            sb.AppendLine("        </div>");
            sb.AppendLine("        <div class=\"card-footer\">");
            sb.AppendLine($"          <a href=\"{HtmlEncode(chart.FileName)}.svg\" target=\"_blank\">Open as standalone SVG &#8599;</a>");
            sb.AppendLine("        </div>");
            sb.AppendLine("      </div>");
            index++;
        }

        sb.AppendLine("    </div>");
        sb.AppendLine("  </main>");
        sb.AppendLine($"  <footer>Generated by TerraFluent.Chart.Reporting &mdash; {DateTime.UtcNow:yyyy-MM-dd HH:mm} UTC</footer>");
        sb.AppendLine("</body>");
        sb.AppendLine("</html>");

        File.WriteAllText(Path.Combine(OutputDir, "index.html"), sb.ToString(), Encoding.UTF8);
    }

    private static string HtmlEncode(string text) =>
        text.Replace("&", "&amp;")
            .Replace("<", "&lt;")
            .Replace(">", "&gt;")
            .Replace("\"", "&quot;");

    // ── AutoAnalytics dashboard.html ─────────────────────────────────────────────────────────────

    /// <summary>Renders the AutoAnalytics dashboard (KPIs, chart sections, anomalies, insights) to HTML.</summary>
    internal static string BuildDashboardHtml(DashboardDefinition dashboard, AnalyticsResult result)
    {
        var sb = new StringBuilder();
        sb.AppendLine("<!DOCTYPE html>");
        sb.AppendLine("<html lang=\"en\"><head><meta charset=\"utf-8\">");
        sb.AppendLine("<meta name=\"viewport\" content=\"width=device-width, initial-scale=1\">");
        sb.AppendLine($"<title>{HtmlEncode(dashboard.Title)}</title>");
        sb.AppendLine(DashboardStyles());
        sb.AppendLine("</head><body>");

        // Header + summary strip.
        var s = result.Summary;
        sb.AppendLine("<header class=\"hero\">");
        sb.AppendLine($"  <h1>{HtmlEncode(dashboard.Title)}</h1>");
        sb.AppendLine($"  <p class=\"headline\">{HtmlEncode(s.Headline)}</p>");
        sb.AppendLine("  <div class=\"meta\">");
        sb.AppendLine($"    <span>{s.RowCount} rows</span><span>{s.ColumnCount} columns</span>");
        sb.AppendLine($"    <span>{s.MeasureCount} measures</span><span>{s.DimensionCount} dimensions</span>");
        sb.AppendLine($"    <span class=\"quality quality-{s.DataQuality.ToLowerInvariant()}\">Data quality: {HtmlEncode(s.DataQuality)}</span>");
        sb.AppendLine("  </div>");
        sb.AppendLine("</header>");
        sb.AppendLine("<main>");

        // KPI cards.
        if (dashboard.Kpis.Count > 0)
        {
            sb.AppendLine("<section><h2>Executive KPIs</h2><div class=\"kpi-grid\">");
            foreach (var kpi in dashboard.Kpis)
            {
                sb.AppendLine("  <div class=\"kpi\">");
                sb.AppendLine($"    <div class=\"kpi-value\">{HtmlEncode(kpi.DisplayValue)}</div>");
                sb.AppendLine($"    <div class=\"kpi-label\">{HtmlEncode(kpi.Label)}</div>");
                if (!string.IsNullOrEmpty(kpi.Caption))
                    sb.AppendLine($"    <div class=\"kpi-caption\">{HtmlEncode(kpi.Caption!)}</div>");
                sb.AppendLine("  </div>");
            }
            sb.AppendLine("</div></section>");
        }

        RenderDashboardChartSection(sb, "Trends over time", dashboard.TrendCharts);
        RenderDashboardChartSection(sb, "Category comparisons", dashboard.ComparisonCharts);
        RenderDashboardChartSection(sb, "Distributions", dashboard.DistributionCharts);

        // Anomalies panel.
        if (dashboard.Anomalies.Count > 0)
        {
            sb.AppendLine("<section><h2>Anomalies detected</h2><ul class=\"panel anomalies\">");
            foreach (var a in dashboard.Anomalies)
                sb.AppendLine($"  <li><strong>{HtmlEncode(a.Title)}</strong><span>{HtmlEncode(a.Description)}</span></li>");
            sb.AppendLine("</ul></section>");
        }

        // Key insights panel.
        sb.AppendLine("<section><h2>Key insights</h2><ul class=\"panel insights\">");
        foreach (var insight in dashboard.KeyInsights)
        {
            sb.AppendLine("  <li>");
            sb.AppendLine($"    <span class=\"score\">{insight.ImportanceScore}</span>");
            sb.AppendLine($"    <div><strong>{HtmlEncode(insight.Title)}</strong><span>{HtmlEncode(insight.Description)}</span></div>");
            sb.AppendLine("  </li>");
        }
        sb.AppendLine("</ul></section>");

        sb.AppendLine("</main>");
        sb.AppendLine("<footer>Generated by TerraFluent.AutoAnalytics — deterministic, no AI.</footer>");
        sb.AppendLine("</body></html>");
        return sb.ToString();
    }

    private static void RenderDashboardChartSection(StringBuilder sb, string title, IReadOnlyList<RecommendedChart> charts)
    {
        if (charts.Count == 0) return;
        sb.AppendLine($"<section><h2>{HtmlEncode(title)}</h2><div class=\"chart-grid\">");
        foreach (var chart in charts)
        {
            string svg = ChartConfigBuilder.ToChartBuilder(chart.Spec)
                .AsAnimated()
                .Size(560, 340)
                .RenderToSvg();

            sb.AppendLine("  <figure class=\"chart-card\">");
            sb.AppendLine($"    <div class=\"chart-badge\">{HtmlEncode(chart.ChartName)} · score {chart.SuitabilityScore}</div>");
            sb.AppendLine($"    {svg}");
            sb.AppendLine($"    <figcaption>{HtmlEncode(chart.Reason)}</figcaption>");
            sb.AppendLine("  </figure>");
        }
        sb.AppendLine("</div></section>");
    }

    private static string DashboardStyles() => """
        <style>
          :root { --bg:#0f172a; --card:#ffffff; --muted:#64748b; --accent:#6366f1; }
          * { box-sizing: border-box; }
          body { margin:0; font-family: 'Segoe UI', system-ui, sans-serif; background:#f1f5f9; color:#1e293b; }
          .hero { background: linear-gradient(135deg,#4f46e5,#7c3aed); color:#fff; padding:32px 40px; }
          .hero h1 { margin:0 0 6px; font-size:28px; }
          .headline { margin:0 0 14px; font-size:16px; opacity:.92; }
          .meta { display:flex; flex-wrap:wrap; gap:10px; font-size:13px; }
          .meta span { background:rgba(255,255,255,.16); padding:4px 12px; border-radius:999px; }
          .quality-clean { background:rgba(34,197,94,.35)!important; }
          .quality-warnings { background:rgba(234,179,8,.4)!important; }
          .quality-errors { background:rgba(239,68,68,.45)!important; }
          main { padding:28px 40px 48px; max-width:1200px; margin:0 auto; }
          section { margin-bottom:34px; }
          h2 { font-size:18px; margin:0 0 16px; color:#334155; border-left:4px solid var(--accent); padding-left:10px; }
          .kpi-grid { display:grid; grid-template-columns:repeat(auto-fit,minmax(180px,1fr)); gap:16px; }
          .kpi { background:var(--card); border-radius:14px; padding:18px 20px; box-shadow:0 1px 3px rgba(0,0,0,.08); }
          .kpi-value { font-size:30px; font-weight:700; color:#4f46e5; }
          .kpi-label { font-size:14px; font-weight:600; margin-top:4px; }
          .kpi-caption { font-size:12px; color:var(--muted); margin-top:2px; }
          .chart-grid { display:grid; grid-template-columns:repeat(auto-fit,minmax(380px,1fr)); gap:20px; }
          .chart-card { margin:0; background:var(--card); border-radius:14px; padding:16px; box-shadow:0 1px 3px rgba(0,0,0,.08); }
          .chart-card svg { width:100%; height:auto; }
          .chart-badge { display:inline-block; font-size:11px; font-weight:600; color:#4f46e5; background:#eef2ff; padding:3px 10px; border-radius:999px; margin-bottom:8px; }
          figcaption { font-size:12px; color:var(--muted); margin-top:10px; line-height:1.4; }
          .panel { list-style:none; margin:0; padding:0; display:grid; gap:10px; }
          .panel li { background:var(--card); border-radius:12px; padding:14px 16px; box-shadow:0 1px 3px rgba(0,0,0,.06); display:flex; gap:14px; align-items:flex-start; }
          .panel strong { display:block; font-size:14px; }
          .panel span { font-size:13px; color:var(--muted); }
          .anomalies li { border-left:4px solid #ef4444; }
          .insights .score { flex:0 0 auto; width:38px; height:38px; border-radius:10px; background:var(--accent); color:#fff; font-weight:700; display:flex; align-items:center; justify-content:center; }
          footer { text-align:center; color:var(--muted); font-size:12px; padding:24px; }
        </style>
        """;
}

