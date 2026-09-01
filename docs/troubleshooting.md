# Troubleshooting & FAQ

Common questions, error messages, and rendering issues — with fixes.

---

## Rendering & display

### My chart shows no animation or hover effects
Animation and CSS hover are only emitted in **Animated** or **Interactive** render modes. If you called `.AsStatic()` (or nothing, in a context where static was applied), you get a plain SVG.

```csharp
.AsAnimated()   // SMIL animation + CSS hover, no JS
```

### The export menu / tooltips / legend toggle don't work
Those features rely on embedded JavaScript, which is only produced in **Interactive** mode:

```csharp
.AsInteractive()
    .ShowExportMenu()   // now the menu is functional
```

Interactive output is browser-only. For PDF or email, use `.AsStatic()`.

### The chart renders but is blank / tiny inside a PDF or email
PDF and email clients strip `<script>` and often ignore SMIL animation, so a chart that reveals itself via animation can appear empty. Use **Static** mode, which draws everything immediately:

```csharp
.AsStatic()
```

### My responsive chart exports as a 100 px thumbnail
A responsive chart uses `width="100%"`. When you rasterise it (PNG/PDF), give it an explicit size instead:

```csharp
.Size(800, 450)   // fixed pixels for raster/PDF export
```

### Fonts look different in an exported/shared image than on my page
An SVG loaded as an image (`<img src=...>`) is isolated and cannot see your page's `@font-face` rules — it falls back to a system font. For inline SVG (injected into the DOM) the page fonts apply. Set an explicit, widely-available `FontFamily` on the theme if you need consistent typography in isolated images.

### X-axis labels overlap or get cut off
Use `LabelLayout` to rotate, wrap, skip, or shrink dense labels:

```csharp
.LabelLayout(l => l.AutoRotate(45).AutoSkip())
```

Long horizontal **Bar** category labels widen the left gutter automatically; very long labels can still be wrapped with `.LabelLayout(l => l.Wrap(14))`.

---

## Data & series

### Some points are missing from my line/area
`null` entries in a `double?[]` are treated as gaps. Control the behaviour with `NullGap`:

```csharp
.AddLine("Sales", data, cfg => cfg.NullGap(GapPolicy.Connect))  // bridge the gap
// GapPolicy.Break (default) = leave a gap; GapPolicy.Zero = plot at 0
```

### My waterfall's first bar is invisible or wrong
Mark which bars are absolute totals with the `totals` array. The first bar is an absolute start bar (`0 → value`); intermediate bars are deltas; mark running totals with `true`:

```csharp
.AddWaterfall("P&L",
    new double?[] { 500, 800, -320, 980 },
    totals: new[] { true, false, false, true });
```

If you omit `totals`, the first bar is treated as an absolute start and the last as a total.

### Stacking has no effect
`StackNormal()` / `StackPercent()` apply to stackable types only: **Column**, **Bar**, **Area**, and **Line**. Stacking is ignored for Pie, Scatter, Gauge, etc.

### Scatter/bubble points look evenly spaced regardless of their X value
Scatter is **category-indexed** — points are placed by their position in the series, not by a numeric X value. For a true numeric X position with a size dimension, use **Bubble** (`BubblePoint(x, y, z)`).

### Values render with the wrong decimal/grouping format
Number formatting follows the chart culture. Set it explicitly:

```csharp
.Culture("de-DE")   // 1.234,56
```

Coordinates in the SVG geometry always use invariant formatting; only displayed tick/label text is localized.

---

## Exceptions

| Exception | Typical cause | Fix |
|---|---|---|
| `ArgumentOutOfRangeException` on `Size` | Width or height ≤ 0 | Pass positive pixel sizes, e.g. `Size(700, 400)`. |
| `ArgumentException` on `YAxis` | `min` ≥ `max` | Ensure `min < max`. |
| `ArgumentOutOfRangeException` on `Animate` | Duration ≤ 0 | Pass a positive millisecond value. |
| `ArgumentOutOfRangeException` on `FillOpacity` | Value outside `0.0`–`1.0` | Clamp to that range. |
| `ArgumentOutOfRangeException` on `MarkerSize` | Size ≤ 0 | Pass a positive radius. |
| `ArgumentException` on `XAxisFormat` | Empty/whitespace format | Provide a non-empty format string. |
| `ArgumentException` on `ChartTheme.Custom(colors: [])` | Empty palette array | Provide at least one colour, or pass `null`. |
| `DirectoryNotFoundException` on `RenderToFile` | Target folder doesn't exist | Create the directory first. |
| `DataQualityException` on render | `ThrowOnDataQualityErrors()` + an `Error` finding | Inspect `GetLastDataQualityReport()` and fix the data, or remove the guard. |

### How do I see data-quality warnings without throwing?
```csharp
var report = ChartBuilder.Create()
    .Series(s => s.AddColumn("Sales", data))
    .AnalyzeDataQuality();   // or GetLastDataQualityReport() after a render

foreach (var w in report.Warnings)
    Console.WriteLine($"{w.Severity}: {w.Message}");
```

---

## Integration

### Blazor: my SVG shows as escaped text
Render it as raw markup:

```razor
@((MarkupString)ChartSvg)
```

### ASP.NET Core: what content type should I return?
```csharp
return Results.Content(svg, "image/svg+xml");   // Minimal API
return Content(svg, "image/svg+xml");           // Controller
```
Prefer `.AsStatic()` for endpoints consumed by unknown clients.

### Dependency injection: how do I register the builder?
```csharp
services.AddScoped<IChartBuilder>(_ => ChartBuilder.Create());
```
Each `ChartBuilder.Create()` is independent and stateful, so use a transient/scoped lifetime — never a singleton shared across requests.

### Can I reuse one configured builder for several charts?
Yes — build a base and `Fork()` (deep copy) per variant so mutations don't leak:

```csharp
var baseChart = ChartBuilder.Create().Title("Revenue").Size(700, 400)
    .Series(s => s.AddLine("2025", data));

string light = baseChart.RenderToSvg();
string dark  = baseChart.Fork().Theme(ChartTheme.Dark).RenderToSvg();
```

---

## Packaging & targets

### Which frameworks are supported?
`netstandard2.0`, `netstandard2.1`, `net6.0`, `net8.0`, and `net10.0`.

### Why are the async render methods missing?
`RenderToStreamAsync` / `RenderToFileAsync` / `RenderToHtmlFileAsync` are available only on **net6.0 and newer** targets. On `netstandard2.0`/`2.1`, use the synchronous `RenderToStream` / `RenderToFile`.

### Does the library pull in any third-party dependencies?
No. The library uses only the .NET base class library — nothing else is added to your dependency graph.

---

See also: [Getting Started](getting-started.md) · [Chart Types](chart-types.md) · [API Reference](api-reference.md) · [Advanced Features](advanced.md)
