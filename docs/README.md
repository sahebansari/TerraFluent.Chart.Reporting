# TerraFluent.Chart.Reporting — Documentation

**Server-side SVG chart generation for .NET. Zero JavaScript. Zero external dependencies.**

TerraFluent.Chart.Reporting generates fully self-contained SVG charts entirely in C#, inspired by the Highcharts API design. Drop the SVG string into an HTML page, a Blazor component, a PDF report, or an email — it just works.

---

## Documentation Index

| Guide | What it covers |
|---|---|
| [Getting Started](getting-started.md) | Install, first chart, ASP.NET / Blazor integration |
| [Chart Types](chart-types.md) | All 16 chart types with full code + SVG output |
| [Themes & Styling](themes-and-styling.md) | Built-in themes, custom themes, ChartColor catalogue, series colors, borders, opacity |
| [Advanced Features](advanced.md) | Render modes, output methods, axes, plot bands, stacking, Fork, DI |
| [API Reference](api-reference.md) | Complete method table for every builder |

---

## Quick Example

```csharp
using TerraFluent.Chart.Reporting.Builder;

string svg = ChartBuilder.Create()
    .Title("Monthly Revenue")
    .Size(700, 400)
    .XAxis("Month", "Jan", "Feb", "Mar", "Apr", "May", "Jun")
    .YAxis("Revenue ($k)", min: 0)
    .AsAnimated()
    .Series(s => s
        .AddLine("Online",  new double[] { 120, 145, 132, 178, 210, 195 })
        .AddColumn("Store", new double[] {  80,  90,  85, 100,  95, 115 }))
    .RenderToSvg();
```

**Output** — a self-contained `<svg>` string ready to embed:

```xml
<svg xmlns="http://www.w3.org/2000/svg" width="700" height="400" viewBox="0 0 700 400">
  <rect width="700" height="400" fill="#ffffff"/>
  <text x="350" y="28" text-anchor="middle" font-size="16" font-weight="bold" fill="#333333">Monthly Revenue</text>
  <!-- axes, grid lines, column rects, line path, SMIL animations … -->
</svg>
```

Paste it directly into HTML or save it with `.RenderToFile("chart.svg")`.

---

## Key Facts

| Feature | Detail |
|---|---|
| **Target frameworks** | `netstandard2.0`, `netstandard2.1`, `net6.0`, `net8.0`, `net10.0` |
| **External dependencies** | None — pure BCL only |
| **Output** | Self-contained SVG string |
| **Chart types** | 16 types (Line, Spline, Area, Column, Bar, Pie/Donut, Scatter, Waterfall, Gauge, DataRing, Bubble, Heatmap, ColumnRange, AreaRange, Funnel, Treemap) |
| **Render modes** | Static (PDF/email safe), Animated (SMIL), Interactive (JS) |
| **Built-in themes** | Default, Dark, Pastel, Monochrome, Custom |

---

## Namespace Map

```
TerraFluent.Chart.Reporting
├── Builder/
│   ├── ChartBuilder          ← entry point (ChartBuilder.Create())
│   ├── IChartBuilder         ← interface for DI / test doubles
│   ├── SeriesBuilder         ← per-series config inside .Series(s => s.AddLine(...))
│   ├── LegendBuilder         ← .Legend(l => l.AtBottom().AlignCenter())
│   ├── TooltipBuilder        ← .Tooltip(t => t.Format("{label}: {value}"))
│   └── AnimationBuilder      ← .Animation(a => a.EaseInOut())
├── Models/
│   ├── ChartOptions          ← raw options snapshot
│   ├── ChartTheme            ← theme palette and typography
│   ├── ChartColor            ← 130+ named colour constants + utility methods
│   ├── Series                ← per-series data and settings
│   ├── Axis                  ← X or Y axis config
│   ├── DataLabelOptions      ← .DataLabel.Show().Format("…")
│   ├── DonutCenterOptions    ← .DonutCenter.Show().Title("Total")
│   ├── BubblePoint(X,Y,Z)    ← bubble chart data point
│   ├── RangePoint(Low,High)  ← column/area range data point
│   └── HeatmapPoint(Col,Row,Value) ← heatmap cell
├── Enums/
│   ├── ChartType             ← 16 values
│   ├── SvgMode               ← Static | Animated | Interactive
│   ├── Stacking              ← None | Normal | Percent
│   └── Easing                ← EaseOut | EaseIn | EaseInOut | Linear | Bounce | Elastic
└── Rendering/
    ├── ISvgRenderer          ← interface for custom renderers
    └── SvgRenderer           ← default implementation
```
