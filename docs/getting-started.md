# Getting Started

## 1. Reference the Library

**Project reference (before NuGet publishing):**

Add to your `.csproj`:
```xml
<ItemGroup>
  <ProjectReference Include="..\src\TerraFluent.Chart.Reporting\TerraFluent.Chart.Reporting.csproj" />
</ItemGroup>
```

**After NuGet publishing:**
```bash
dotnet add package TerraFluent.Chart.Reporting
```

---

## 2. Your First Chart

All charts start with `ChartBuilder.Create()`. Every method returns `this`, so you chain them freely:

```csharp
using TerraFluent.Chart.Reporting.Builder;

string svg = ChartBuilder.Create()
    .Title("Monthly Website Visitors")
    .Subtitle("Jan – Jun 2025")
    .Size(700, 400)
    .XAxis("Month", "Jan", "Feb", "Mar", "Apr", "May", "Jun")
    .YAxis("Visitors", min: 0)
    .AsAnimated()
    .Series(s => s
        .AddLine("Visitors", new double[] { 12400, 14800, 16200, 18500, 21000, 19800 }))
    .RenderToSvg();
```

**Output** — self-contained SVG (excerpt):
```xml
<svg xmlns="http://www.w3.org/2000/svg" width="700" height="400" viewBox="0 0 700 400">
  <rect width="700" height="400" fill="#ffffff"/>
  <text x="350" y="28" text-anchor="middle" font-size="16" font-weight="bold" fill="#333333">
    Monthly Website Visitors
  </text>
  <text x="350" y="46" text-anchor="middle" font-size="11" fill="#666666">Jan – Jun 2025</text>
  <!-- grid lines, axis labels, SMIL-animated line path … -->
  <path d="M … L … L …" fill="none" stroke="#7CB5EC" stroke-width="2">
    <animate attributeName="stroke-dashoffset" from="…" to="0" dur="0.8s" fill="freeze"/>
  </path>
</svg>
```

---

## 3. Save to a File

```csharp
// Save as SVG
ChartBuilder.Create()
    .Title("Sales Report")
    .Size(800, 500)
    .Series(s => s.AddColumn("Q1", new double[] { 320, 410, 390, 480 }))
    .RenderToFile("output/sales.svg");

// Save as HTML fragment (SVG wrapped in <figure>)
ChartBuilder.Create()
    .Title("Sales Report")
    .Size(800, 500)
    .Series(s => s.AddColumn("Q1", new double[] { 320, 410, 390, 480 }))
    .RenderToHtmlFile("output/sales.html", caption: "FY 2025 Sales");
```

---

## 4. ASP.NET Core — Return as HTTP Response

```csharp
// Minimal API
app.MapGet("/charts/revenue", () =>
{
    string svg = ChartBuilder.Create()
        .Title("Revenue")
        .Size(700, 400)
        .XAxis("Q", "Q1", "Q2", "Q3", "Q4")
        .YAxis("$k", min: 0)
        .AsStatic()                          // no JS — safe for any client
        .Series(s => s.AddColumn("Revenue", new double[] { 310, 390, 420, 510 }))
        .RenderToSvg();

    return Results.Content(svg, "image/svg+xml");
});

// Controller
[HttpGet("chart")]
public IActionResult GetChart()
{
    string svg = ChartBuilder.Create()
        .Title("Revenue")
        .Size(700, 400)
        .AsStatic()
        .Series(s => s.AddColumn("Revenue", new double[] { 310, 390, 420, 510 }))
        .RenderToSvg();

    return Content(svg, "image/svg+xml");
}
```

---

## 5. Blazor — Render Inline SVG

```razor
@using TerraFluent.Chart.Reporting.Builder

<div class="chart-container">
    @((MarkupString)ChartSvg)
</div>

@code {
    private string ChartSvg = string.Empty;

    protected override void OnInitialized()
    {
        ChartSvg = ChartBuilder.Create()
            .Title("Active Users")
            .Size(700, 380)
            .XAxis("Day", "Mon", "Tue", "Wed", "Thu", "Fri")
            .YAxis("Users", min: 0)
            .AsAnimated()                    // SMIL animations work in Blazor WASM
            .Series(s => s
                .AddArea("DAU", new double[] { 4200, 5100, 4800, 6300, 7200 }))
            .RenderToSvg();
    }
}
```

> **Render Mode Tip:** Use `.AsAnimated()` in Blazor. Use `.AsStatic()` for PDF / email. Use `.AsInteractive()` only for plain HTML browser pages.

---

## 6. Dependency Injection

Register via the `IChartBuilder` interface for testable, decoupled code:

```csharp
// Program.cs — register
builder.Services.AddScoped<IChartBuilder>(_ => ChartBuilder.Create());

// Service — inject and use
public class ReportService
{
    private readonly IChartBuilder _charts;

    public ReportService(IChartBuilder charts) => _charts = charts;

    public string BuildRevenueSvg(double[] data)
    {
        return _charts
            .Title("Revenue")
            .Size(700, 400)
            .Series(s => s.AddColumn("Revenue", data))
            .RenderToSvg();
    }
}
```

---

## 7. Core Pattern — How the Builder Works

```
ChartBuilder.Create()           ← creates a fresh builder
  .Title(…)                     ← chart-level settings
  .Size(…)
  .Theme(…)
  .XAxis(…) / .YAxis(…)
  .AsAnimated()                 ← render mode
  .Series(s => s                ← open the series scope
      .AddLine(…)               ← add first series
      .AddColumn(…))            ← add second series (chain inside lambda)
  .RenderToSvg()                ← terminate and get the SVG string
```

All methods are null-safe and throw `ArgumentException` / `ArgumentNullException` on invalid input. Every `double?[]` parameter supports `null` gaps (the line or column is skipped at that index).

---

## 8. Choosing a Render Mode

| Mode | Method | CSS hover? | JS? | SMIL? | Use for |
|---|---|---|---|---|---|
| **Static** | `.AsStatic()` | No | No | No | PDF, email, server-side image |
| **Animated** | `.AsAnimated()` | Yes | No | Yes | Blazor, browser embedding |
| **Interactive** | `.AsInteractive()` | Yes | Yes | Yes | Browser-only dashboards |

See [Advanced Features](advanced.md#render-modes) for full details.
