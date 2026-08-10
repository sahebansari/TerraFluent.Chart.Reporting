# Advanced Features

Covers render modes, all output methods, axis configuration, plot annotations, stacking, the Fork pattern, and Dependency Injection.

---

## Render Modes

TerraFluent generates different SVG variants depending on the target environment.

### Static

Safe for PDF, email, and server-side image generation. No JavaScript, no CSS hover rules, no SMIL animation.

```csharp
string svg = ChartBuilder.Create()
    .Title("Annual Report — Revenue Summary")
    .Size(700, 400)
    .AsStatic()                          // no hover, no JS, no animation
    .XAxis("Quarter", "Q1","Q2","Q3","Q4")
    .YAxis("Revenue ($k)", min: 0)
    .Series(s => s
        .AddColumn("Revenue", new double[] { 320, 410, 390, 480 },
            cfg => cfg.Color(ChartColor.Charcoal))
        .AddLine("Target",   new double[] { 300, 380, 420, 450 },
            cfg => cfg.Color(ChartColor.ChartRose).Dashed()))
    .RenderToSvg();
```

**Output:** A pure SVG with no `<script>` blocks, no CSS `:hover` rules, and no `<animate>` elements. Every charting tool that supports SVG 1.1 can render it — including iTextSharp, PuppeteerSharp, and most email clients.

> **Note:** `AsStatic()` automatically sets `Animation.Enabled = false`. Call `.DisableAnimation()` explicitly if you use `AsAnimated()` but want to suppress animation.

---

### Animated

SMIL/CSS animations only — no JavaScript. Recommended for Blazor, embedded SVG in HTML, and server-rendered pages.

```csharp
string svg = ChartBuilder.Create()
    .Title("Monthly Sales")
    .Size(700, 420)
    .AsAnimated()
    .Animate(800)                        // optional: custom duration (ms)
    .Animation(a => a.EaseInOut())       // optional: custom easing
    .XAxis("Month", "Jan","Feb","Mar","Apr","May","Jun")
    .YAxis("Units", min: 0)
    .Series(s => s
        .AddColumn("Sales", new double[] { 420, 380, 510, 490, 560, 600 }))
    .RenderToSvg();
```

**Output:** Each column contains an `<animate>` element that grows the bar from zero height to its target height over 800 ms with an ease-in-out curve. Line and area charts animate with a stroke-dashoffset sweep.

**Easing options:**

```csharp
.Animation(a => a.EaseOut())      // default — fast then slow
.Animation(a => a.EaseIn())       // slow then fast
.Animation(a => a.EaseInOut())    // slow at both ends
.Animation(a => a.Linear())       // constant speed
.Animation(a => a.Bounce())       // bounces at the end
.Animation(a => a.Elastic())      // elastic snap
```

---

### Interactive

Adds CSS hover highlight effects and embedded JavaScript for click interactions. Browser-only — do not use in PDFs or emails.

```csharp
string svg = ChartBuilder.Create()
    .Title("Interactive Sales Dashboard")
    .Size(750, 440)
    .AsInteractive()
    .XAxis("Month", "Jan","Feb","Mar","Apr","May","Jun")
    .YAxis("USD (thousands)", min: 0)
    .Series(s => s
        .AddLine("Online",   new double[] { 180, 210, 190, 260, 310, 280 },
            cfg => cfg.Color(ChartColor.ChartBlue))
        .AddLine("In-Store", new double[] { 140, 160, 175, 200, 195, 220 },
            cfg => cfg.Color(ChartColor.ChartOrange)))
    .RenderToSvg();
```

**Output:** The SVG includes `<style>` blocks with `:hover` transitions and a `<script>` section that highlights the hovered data point and shows a tooltip pop-up. The tooltip is rendered with CSS positioning inside the SVG's foreignObject.

---

### Mode Decision Guide

```
┌─ Does it go into a PDF or email?
│  └─ Yes  →  AsStatic()
│  └─ No
│     ├─ Is it in a Blazor component / embedded SVG tag?
│     │  └─ Yes  →  AsAnimated()
│     └─ Is it a standalone HTML page / dashboard?
│        └─ Yes  →  AsInteractive() or AsAnimated()
```

---

## Output Methods

### String (most common)

```csharp
string svg = builder.RenderToSvg();
```

Returns the complete SVG markup as a `string`. Embed it directly in HTML, Blazor `MarkupString`, or a response body.

### HTML Fragment

```csharp
// String
string html = builder.RenderToHtml(caption: "FY 2025 Sales", cssClass: "chart-card");

// File
builder.RenderToHtmlFile("output/chart.html", caption: "FY 2025 Sales");
```

Wraps the SVG in `<figure>` + optional `<figcaption>`. The outer `<figure>` has `display:inline-block; width:100%` so the SVG is responsive within its container.

**Output structure:**

```html
<figure class="chart-card" style="margin:0;padding:0;display:inline-block;width:100%;">
  <div style="width:100%;overflow:hidden;">
    <svg …> … </svg>
  </div>
  <figcaption style="text-align:center;font-size:0.85em;color:#666;margin-top:4px;">
    FY 2025 Sales
  </figcaption>
</figure>
```

### File

```csharp
builder.RenderToFile("output/chart.svg");
builder.RenderToHtmlFile("output/chart.html", caption: "My Chart");
```

The parent directory must exist before calling either method. A `DirectoryNotFoundException` is thrown otherwise.

### Stream

```csharp
// Synchronous
using var fs = File.OpenWrite("output/chart.svg");
builder.RenderToStream(fs);

// Asynchronous (.NET 6+)
await using var fs = File.OpenWrite("output/chart.svg");
await builder.RenderToStreamAsync(fs, cancellationToken);
```

### Byte Array

```csharp
byte[] bytes = builder.RenderToBytes();
// Useful for: Response.Body.WriteAsync, Azure Blob upload, etc.
```

### Async File Methods (.NET 6+)

```csharp
await builder.RenderToFileAsync("output/chart.svg", cancellationToken);
await builder.RenderToHtmlFileAsync("output/chart.html", "Caption", cancellationToken: ct);
```

---

## Axes — Advanced Configuration

### Categorical X-Axis

```csharp
// Shorthand (title + categories in one call)
.XAxis("Month", "Jan","Feb","Mar","Apr","May","Jun")

// Lambda (full control)
.XAxis(x =>
{
    x.Title          = "Month";
    x.LabelRotation  = -45;          // diagonal labels for long names
    x.GridLineVisible = true;
    x.Categories.AddRange(new[] { "Jan","Feb","Mar","Apr","May","Jun" });
})
```

**Output with rotation:** Category labels are rotated 45° counter-clockwise, preventing overlap on dense charts.

### Numeric X-Axis (no categories)

Use `XAxis.Min`, `XAxis.Max`, and `XAxis.TickInterval` for charts with a numeric X dimension:

```csharp
.XAxis(x =>
{
    x.Title          = "Time (s)";
    x.Min            = 0;
    x.Max            = 30;
    x.TickInterval   = 5;            // ticks at 0, 5, 10, 15, 20, 25, 30
    x.GridLineVisible = true;
})
.YAxis("Amplitude", min: -1.5, max: 1.5)
.Series(s => s
    .AddLine("Signal",
        new double[] { 0.0, 0.87, 1.0, 0.5, -0.5, -1.0, -0.87, 0.0,
                       0.87, 1.0, 0.5, -0.5, -1.0, -0.87, 0.0, 0.87 }))
```

**Output:** A numeric axis from 0 to 30, with tick marks every 5 units. The signal values are plotted at their Y positions; the X positions are evenly spaced.

### Y-Axis Format

```csharp
.YAxisFormat("${value}k")        // → $320k
.YAxisFormat("{value}%")         // → 42%
.YAxisFormat("{value:N0}")       // → 12,400
```

### Secondary Y-Axis

Bind a second, independent Y-axis on the right side:

```csharp
string svg = ChartBuilder.Create()
    .Title("Revenue & Profit Margin")
    .Size(720, 420)
    .XAxis("Quarter", "Q1 2024","Q2 2024","Q3 2024","Q4 2024")
    .YAxis(y => { y.Title = "Revenue ($k)"; y.Min = 0; })
    .YAxis2(y => { y.Title = "Margin (%)"; y.Min = 0; y.Max = 50; })
    .AsAnimated()
    .Series(s => s
        .AddColumn("Revenue", new double[] { 310, 390, 420, 510 },
            cfg => cfg.Color(ChartColor.ChartBlue))
        .AddLine("Margin %",  new double[] { 22, 28, 25, 31 },
            cfg => cfg.Color(ChartColor.ChartRose).LineWidth(3).OnSecondaryAxis()))
    .RenderToSvg();
```

**Output:** Columns scaled against the left axis (0–510+). The margin line floats independently scaled against the right axis (0–50 %). Both axis labels are visible.

---

## Plot Bands & Reference Lines

### Plot Band (shaded region)

```csharp
string svg = ChartBuilder.Create()
    .Title("Server Response Time")
    .Size(720, 420)
    .XAxis("Hour", "00","02","04","06","08","10","12","14","16","18","20","22")
    .YAxis(y =>
    {
        y.Title = "ms";
        y.Min   = 0;
        // Green band: response time is "Good"
        y.PlotBands.Add(new PlotBand { From = 0,   To = 200, Color = "rgba(144,237,125,0.15)", Label = "Good" });
        // Orange band: "Acceptable"
        y.PlotBands.Add(new PlotBand { From = 200, To = 500, Color = "rgba(247,163,92,0.15)",  Label = "Acceptable" });
    })
    .AsAnimated()
    .Series(s => s
        .AddLine("p50", new double[] { 85, 78, 72, 90, 145, 220, 310, 280, 195, 160, 130, 105 },
            cfg => cfg.Color(ChartColor.ChartBlue))
        .AddLine("p99", new double[] { 210, 185, 170, 240, 420, 610, 780, 720, 490, 380, 290, 230 },
            cfg => cfg.Color(ChartColor.ChartRed).Dashed()))
    .RenderToSvg();
```

**Output:** Two semi-transparent coloured bands fill the plot area at `0–200 ms` (green) and `200–500 ms` (orange). Band labels "Good" and "Acceptable" appear at the right edge of each band. The p99 line visibly crosses out of the orange band into the danger zone during peak hours.

### Reference Line (PlotLine)

```csharp
// Shorthand on ChartBuilder
.PlotLine(value: 500, color: ChartColor.ChartRed, width: 2, label: "SLA limit", dashStyle: "Dash")

// Via lambda on YAxis
.YAxis(y => y.PlotLines.Add(new PlotLine
{
    Value     = 500,
    Color     = ChartColor.ChartRed,
    Width     = 2,
    DashStyle = "Dash",
    Label     = "SLA limit"
}))
```

**Output:** A red dashed horizontal line drawn at y = 500. The label "SLA limit" floats to the right of the line.

---

## Stacking

### Normal (cumulative)

```csharp
string svg = ChartBuilder.Create()
    .Title("Monthly Traffic — Stacked")
    .Size(720, 420)
    .StackNormal()
    .XAxis("Month", "Jan","Feb","Mar","Apr","May","Jun")
    .YAxis("Sessions", min: 0)
    .AsAnimated()
    .Series(s => s
        .AddArea("Organic",  new double[] { 1200, 1350, 1500, 1700, 1900, 2100 })
        .AddArea("Direct",   new double[] {  800,  850,  900,  950, 1000, 1100 })
        .AddArea("Referral", new double[] {  400,  420,  440,  480,  520,  560 }))
    .RenderToSvg();
```

**Output:** Three areas stack on top of each other. The top edge of the Referral band shows the combined total for all channels at each month. Users can visually compare both individual channels and the total.

### Percent (100 % normalised)

```csharp
string svg = ChartBuilder.Create()
    .Title("Revenue Mix — 100 % Stacked")
    .Size(700, 420)
    .StackPercent()
    .XAxis("Quarter", "Q1","Q2","Q3","Q4")
    .YAxis(y => { y.Title = "%"; y.Min = 0; y.Max = 100; y.LabelFormat = "{value}%"; })
    .Series(s => s
        .AddColumn("Product A", new double[] { 180, 220, 240, 280 },
            cfg => cfg.DataLabel.Show().Format("{value}%"))
        .AddColumn("Product B", new double[] { 140, 160, 175, 200 },
            cfg => cfg.DataLabel.Show().Format("{value}%"))
        .AddColumn("Product C", new double[] {  90, 110, 130, 150 },
            cfg => cfg.DataLabel.Show().Format("{value}%")))
    .RenderToSvg();
```

**Output:** Each column fills 100 % of the chart height. Segments show each product's proportional share. Data labels inside each segment show the actual percentage values.

---

## Fork — Chart Variants

`Fork()` creates an independent deep copy of the builder state. Use it to produce multiple variations (e.g. Static for PDF + Animated for web) without re-writing the configuration.

```csharp
// Build the shared base once
ChartBuilder baseChart = ChartBuilder.Create()
    .Size(820, 380)
    .Title("Team Performance Scores")
    .XAxis("Month", "Jan","Feb","Mar","Apr","May","Jun")
    .YAxis("Score", min: 0)
    .Colors("#4a90d9", "#e8734a")
    .Series(s => s
        .AddLine("Team A", new double[] { 70, 78, 82, 88, 85, 92 })
        .AddLine("Team B", new double[] { 60, 65, 74, 71, 80, 85 }));

// Fork 1: static SVG for PDF export
string pdfSvg = baseChart
    .Fork()
    .AsStatic()
    .Title("Team Performance Scores — Q1/Q2 Report")    // different title on the fork
    .RenderToSvg();

// Fork 2: animated SVG for the web dashboard
string webSvg = baseChart
    .Fork()
    .AsAnimated()
    .Animate(600)
    .RenderToSvg();

// The original baseChart is unchanged — you can fork again as many times as needed.
```

**Key Fork rules:**
- Changes on a fork do not affect the original builder or other forks.
- All series data, colours, axis configuration, titles, and theme are deep-copied.
- The renderer instance is shared (it is stateless, so this is safe).

---

## Dependency Injection

Register `IChartBuilder` in the DI container to keep services testable:

```csharp
// Program.cs / Startup.cs
builder.Services.AddScoped<IChartBuilder>(_ => ChartBuilder.Create());

// Or with a shared theme:
builder.Services.AddScoped<IChartBuilder>(_ =>
    ChartBuilder.Create().Theme(ChartTheme.Dark));
```

**Service usage:**

```csharp
public class DashboardService
{
    private readonly IChartBuilder _charts;

    public DashboardService(IChartBuilder charts) => _charts = charts;

    public string BuildRevenueSvg(IReadOnlyList<double> values, string[] months)
    {
        return _charts
            .Fork()                  // always fork so the injected instance stays clean
            .Title("Monthly Revenue")
            .Size(700, 400)
            .XAxis("Month", months)
            .YAxis("Revenue ($k)", min: 0)
            .AsAnimated()
            .Series(s => s.AddColumn("Revenue", values))
            .RenderToSvg();
    }
}
```

> **Best practice:** Call `.Fork()` at the beginning of each method that uses the injected `IChartBuilder`. This guarantees that previous calls haven't left stale series or axis configuration on the shared instance.

**Unit test mock:**

```csharp
// xUnit + NSubstitute
public class DashboardServiceTests
{
    [Fact]
    public void BuildRevenueSvg_ReturnsSvgContainingTitle()
    {
        var mockBuilder = Substitute.For<IChartBuilder>();
        mockBuilder.Fork().Returns(ChartBuilder.Create());    // return real builder on Fork
        // … assert on the real ChartBuilder's output
    }
}
```

---

## ChartOptions — Raw Access

For advanced scenarios you can read or mutate the underlying options directly:

```csharp
ChartBuilder builder = ChartBuilder.Create()
    .Title("Revenue")
    .Size(700, 400);

// Read current options (mutable reference):
ChartOptions opts = builder.GetOptions();
opts.Title.Text = "Modified Title";      // affects the builder's state

// Take an independent snapshot (deep copy):
ChartOptions snapshot = builder.Build();
snapshot.Title.Text = "Snapshot Title"; // does NOT affect the builder
```

This is useful when you need to pass chart configuration to a custom `ISvgRenderer` implementation or serialise the options for caching.

---

## Multi-Target Compatibility

The library targets `netstandard2.0`, `netstandard2.1`, `net6.0`, `net8.0`, and `net10.0`.

| Feature | netstandard2.0 | netstandard2.1 | net6+ |
|---|---|---|---|
| All chart types | ✅ | ✅ | ✅ |
| All builder methods | ✅ | ✅ | ✅ |
| `RenderToStreamAsync` | ❌ | ❌ | ✅ |
| `RenderToFileAsync` | ❌ | ❌ | ✅ |
| `RenderToHtmlFileAsync` | ❌ | ❌ | ✅ |
| `double?[]` and `double[]` overloads | ✅ | ✅ | ✅ |

Async methods are guarded by `#if NET6_0_OR_GREATER` and are absent in lower-targeting builds.

**`double?[]` nullable gap support:**

A `null` in a data array is treated as a missing value — the chart skips that data point:

```csharp
// The line breaks between Jan and Apr; Feb and Mar are not drawn.
.AddLine("Sales", new double?[] { 120, null, null, 178, 210, 195 })
```

**Output:** Two separate line segments — one covering Jan, then a gap, then Apr–Jun. Useful for representing missing data without introducing zero distortion.

---

## Custom SVG Renderer

Implement `ISvgRenderer` to replace the rendering engine entirely (for testing, custom styling engines, etc.):

```csharp
public class StubRenderer : ISvgRenderer
{
    public string Render(ChartOptions options)
        => $"<svg><!-- {options.Title.Text} --></svg>";
}

// Use it:
var chart = ChartBuilder.Create(new StubRenderer())
    .Title("My Chart")
    .RenderToSvg();
// → "<svg><!-- My Chart --></svg>"
```

This pattern is especially useful in unit tests where you want to verify that options are configured correctly without running the full renderer.
