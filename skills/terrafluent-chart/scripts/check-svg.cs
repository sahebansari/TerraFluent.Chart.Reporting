// Validates SVG files produced by TerraFluent.Chart.Reporting.
// Requires the .NET 10 SDK (file-based app, BCL only):
//
//   dotnet run --file skills/terrafluent-chart/scripts/check-svg.cs -- <file.svg|file.html> [more files…] [--mode static|animated|interactive]
//
// Checks: well-formed XML, <svg> root in the SVG namespace, viewBox/size present,
// at least a few drawn marks, and render-mode safety (Static: no <script>, no :hover,
// no SMIL animation; Animated: no <script>). HTML files are scanned for every inline <svg>.
// Exit code 0 = all checks passed, 1 = at least one failure, 2 = usage error.

using System.Text.RegularExpressions;
using System.Xml.Linq;

var files = new List<string>();
string? mode = null;
for (int i = 0; i < args.Length; i++)
{
    if (args[i] == "--mode" && i + 1 < args.Length) mode = args[++i].ToLowerInvariant();
    else files.Add(args[i]);
}

if (files.Count == 0 || (mode is not null && mode is not ("static" or "animated" or "interactive")))
{
    Console.Error.WriteLine("usage: check-svg.cs <file.svg|file.html> [...] [--mode static|animated|interactive]");
    return 2;
}

XNamespace svgNs = "http://www.w3.org/2000/svg";
string[] markElements = { "path", "rect", "circle", "ellipse", "polygon", "polyline", "line" };
int failures = 0;

foreach (var file in files)
{
    if (!File.Exists(file)) { Fail(file, "file not found"); continue; }
    string text = File.ReadAllText(file);

    // An .svg file is one document; an .html file may contain several inline <svg> elements.
    var fragments = file.EndsWith(".svg", StringComparison.OrdinalIgnoreCase)
        ? new List<string> { text }
        : Regex.Matches(text, @"<svg\b.*?</svg>", RegexOptions.Singleline | RegexOptions.IgnoreCase)
               .Select(m => m.Value).ToList();

    if (fragments.Count == 0) { Fail(file, "no <svg> element found"); continue; }

    for (int n = 0; n < fragments.Count; n++)
    {
        string label = fragments.Count > 1 ? $"{file} [svg #{n + 1}]" : file;
        var problems = new List<string>();
        XDocument doc;
        try { doc = XDocument.Parse(fragments[n]); }
        catch (Exception ex) { Fail(label, "not well-formed XML: " + ex.Message); continue; }

        var root = doc.Root!;
        if (root.Name != svgNs + "svg") problems.Add($"root element is <{root.Name}>, expected <svg> in {svgNs}");
        if (root.Attribute("viewBox") is null) problems.Add("missing viewBox attribute");

        int marks = doc.Descendants().Count(e => e.Name.Namespace == svgNs && markElements.Contains(e.Name.LocalName));
        if (marks < 3) problems.Add($"only {marks} drawn shape(s); the chart is probably empty (check series data)");

        bool hasScript  = doc.Descendants(svgNs + "script").Any();
        bool hasHover   = doc.Descendants(svgNs + "style").Any(s => s.Value.Contains(":hover"));
        bool hasSmil    = doc.Descendants().Any(e => e.Name.LocalName is "animate" or "animateTransform" or "animateMotion" or "set");

        if (mode == "static")
        {
            if (hasScript) problems.Add("static output contains <script> (call .AsStatic())");
            if (hasHover)  problems.Add("static output contains CSS :hover rules (call .AsStatic())");
            if (hasSmil)   problems.Add("static output contains SMIL animation (call .AsStatic())");
        }
        else if (mode == "animated" && hasScript)
        {
            problems.Add("animated output contains <script> (call .AsAnimated(), not .AsInteractive())");
        }

        string title = doc.Descendants(svgNs + "title").FirstOrDefault()?.Value.Trim() ?? "(no <title>)";
        string size = $"{root.Attribute("width")?.Value ?? "?"}x{root.Attribute("height")?.Value ?? "?"}";
        string features = $"script={Yes(hasScript)} hover={Yes(hasHover)} smil={Yes(hasSmil)}";

        if (problems.Count == 0)
            Console.WriteLine($"PASS {label}: \"{title}\" {size}, {marks} shapes, {features}");
        else
            foreach (var p in problems) Fail(label, p);
    }
}

return failures == 0 ? 0 : 1;

void Fail(string file, string message)
{
    failures++;
    Console.WriteLine($"FAIL {file}: {message}");
}

static string Yes(bool b) => b ? "yes" : "no";
