using System.Text;

namespace TerraFluent.Chart.Reporting.Samples;

/// <summary>
/// Builds the HTML output pages for the sample app: the <c>index.html</c> chart showcase.
/// Split from <see cref="Program"/> to keep orchestration lean.
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
}

