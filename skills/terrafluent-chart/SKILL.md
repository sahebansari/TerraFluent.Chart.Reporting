---
name: terrafluent-chart
description: Generate SVG charts in C#/.NET from a natural-language request using the TerraFluent.Chart.Reporting library (fluent ChartBuilder API, zero dependencies). Use when the user asks to create, draw, plot, render, or export a chart, graph, KPI gauge, dashboard tile, Gantt, Sankey, candlestick, heatmap, or other data visualization as SVG/HTML in a .NET, ASP.NET Core, Blazor, console, email, or report context, or mentions TerraFluent / ChartBuilder.
---

# TerraFluent Chart — prompt → SVG

TerraFluent.Chart.Reporting is a zero-dependency .NET library that renders **self-contained SVG** from a fluent builder. Your job: turn the user's prompt into compilable C# that produces the right chart, then verify the output.

```csharp
using TerraFluent.Chart.Reporting.Builder;   // ChartBuilder, SeriesBuilder, ChartTemplate
using TerraFluent.Chart.Reporting.Models;    // ChartTheme, ChartColor, point types (RangePoint, OhlcPoint, …)
using TerraFluent.Chart.Reporting.Enums;     // MarkerSymbol, GapPolicy, CreditsPosition, …

string svg = ChartBuilder.Create()
    .Title("Monthly Revenue")
    .Size(700, 400)
    .XAxis("Month", "Jan", "Feb", "Mar", "Apr", "May", "Jun")
    .YAxis("Revenue ($)", min: 0)
    .AsStatic()
    .Series(s => s.AddColumn("2025", new double[] { 120, 145, 110, 190, 170, 210 }))
    .RenderToSvg();
```

## Workflow

Follow these steps in order. Don't skip step 1. Most bad charts come from the wrong chart type or the wrong render mode, not from syntax errors.

### 1. Extract a chart spec from the prompt

Before writing code, pin down these fields and fill gaps with the defaults:

| Field | Question | Default if unstated |
|---|---|---|
| **Intent** | Trend, comparison, part-of-whole, distribution, flow, schedule, KPI, financial? | Infer from the data shape |
| **Data** | Categories + one or more numeric series? Structured points? | Use the user's data verbatim; if none is given, generate realistic sample data and **say so** |
| **Destination** | Browser page, Blazor, PDF/print, email, API endpoint, file? | File on disk → `AsStatic()` |
| **Size** | Fixed pixels or responsive? | `Size(700, 400)` (`Size(400, 400)` for gauge/ring) |
| **Look** | Theme, colors, dark mode, brand palette, accessibility? | Default theme; `ChartTheme.Accessible` if accessibility is mentioned |
| **Extras** | Data labels, target line, trend line, secondary axis, stacking? | Only what was asked |

### 2. Pick the chart type

Read [references/chart-selection.md](references/chart-selection.md) to map the intent and data shape to one of the 26 `Add*` methods. Quick rules:

- Time on the X axis → `AddLine` / `AddSpline` / `AddArea`
- Compare categories → `AddColumn`, or `AddBar` when labels are long or there are more than about 8 categories
- Part-of-whole with 6 or fewer slices → `AddPie` + `.AsPie()` (add `DonutHole(0.55)` for a donut); more slices → `AddTreemap` or `AddBar`
- A single KPI value → `AddGauge` (dial) or `AddDataRing` (ring); set the scale with `YAxis(y => { y.Min = 0; y.Max = 100; })`
- Two measures with different units → combine `AddColumn` with `AddLine(..., cfg => cfg.OnSecondaryAxis())` and set `YAxis2(...)`

### 3. Pick the render mode (required, never leave it implicit)

| Destination | Call | Why |
|---|---|---|
| PDF, print, email, image conversion, unknown consumer, file on disk | `.AsStatic()` | No JS, no CSS hover, no animation; renders everywhere |
| Blazor component, inline SVG in a page | `.AsAnimated()` (library default) | SMIL + CSS only, no JS |
| Stand-alone browser dashboard needing tooltips, legend toggle, export menu, drill-down | `.AsInteractive()` | Embeds `<script>`; **browser-only** |

`ShowExportMenu`, `OnPointClick`, `RangeSelector`, and `WithDrilldown` only work in Interactive mode.

### 4. Write the code

- Use **only** methods listed in [references/api-cheatsheet.md](references/api-cheatsheet.md). Don't invent methods from other chart libraries (Highcharts, Chart.js, ScottPlot). The cheatsheet is checked against the source.
- Start from the closest example in [references/recipes.md](references/recipes.md) (console app, ASP.NET Core endpoint, Blazor, multi-chart HTML report, email, DI).
- Categorical data length must match the number of X-axis categories.
- Keep chart construction in a small, named method (for example `BuildRevenueChart(IReadOnlyList<Sale> sales)`) that takes real data and returns the SVG string. Don't hard-code data in UI code.
- Use `ChartColor.*` constants or CSS color strings. Use `ChartTheme.*` presets or `ChartTheme.Custom(...)`.
- For several related charts, configure a base builder once and call `.Fork()` for each variant.

### 5. Verify

1. **Build**: run `dotnet build` and fix every compiler error. An error on a method name usually means the method was invented; check the cheatsheet.
2. **Render**: run the program so the SVG is actually produced (for example `RenderToFile("out/chart.svg")`). Create the output directory first, because `RenderToFile` throws `DirectoryNotFoundException`.
3. **Validate**: `dotnet run --file <skill-dir>/scripts/check-svg.cs -- out/chart.svg --mode static`. `<skill-dir>` is the folder that contains this SKILL.md. Use the mode you rendered with. This needs the .NET 10 SDK; if only an older SDK is available, skip this step and say so. Keep `--file`, because without it `dotnet run` inside a project folder runs the project instead. This checks that the SVG is well-formed XML, has a `viewBox`, contains drawn marks, and that Static output has no `<script>`, `:hover`, or `<animate>`.
4. **Data quality (optional)**: call `builder.AnalyzeDataQuality()` and report any `Warnings` to the user.
5. Read [references/pitfalls.md](references/pitfalls.md) if anything looks wrong (blank chart, evenly spaced scatter, invisible waterfall bar, overlapping labels).

### 6. Report back

Tell the user which chart type and render mode you chose, and why (one line each). Say whether you used sample data. Give the output file path or where the SVG is embedded.

## Installation in the target project

```bash
dotnet add package TerraFluent.Chart.Reporting
```

The package supports `netstandard2.0`, `netstandard2.1`, `net6.0`, `net8.0`, and `net10.0`. The async render methods (`RenderToFileAsync`, `RenderToStreamAsync`, `RenderToHtmlFileAsync`) and `ChartBuilder.FromJson` exist only on net6.0+.

## Hard rules

- Never emit `AsInteractive()` output into PDFs, emails, or `<img src>`, because scripts are stripped and the chart breaks.
- Never register `ChartBuilder` as a singleton. Builders are stateful, so use `AddTransient`/`AddScoped<IChartBuilder>(_ => ChartBuilder.Create())`.
- In Blazor, render the SVG string with `@((MarkupString)svg)`. Serve it from HTTP with content type `image/svg+xml`.
- Any user-supplied strings (titles, labels) are XML-escaped by the library, so don't pre-escape them.
- The `terrafluent.dev` credit label is on by default. Remove it with `.HideCredits()` only if the user asks, and never try to change its text.
