# TerraFluent.Chart.Reporting

A fluent C# library for generating **SVG charts server-side** — zero JavaScript dependency, zero external NuGet dependencies.

[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)

---

## Supported Targets

| Framework | Version |
|---|---|
| .NET Standard | 2.0, 2.1 |
| .NET | 6.0, 8.0, 10.0 |

---

## Quick Start

```csharp
string svg = ChartBuilder.Create()
    .Title("Monthly Revenue")
    .Size(700, 400)
    .XAxis("Month", "Jan", "Feb", "Mar", "Apr", "May", "Jun")
    .YAxis("Revenue ($)", min: 0)
    .Series(s => s.AddLine("2024", new double[] { 120, 145, 110, 190, 170, 210 }))
    .RenderToSvg();
```

---

## Chart Types

26 chart types are supported. See the [Chart Types guide](docs/chart-types.md) for full examples.

| Type | Method | Type | Method |
|---|---|---|---|
| Line | `AddLine` | Bubble | `AddBubble` |
| Spline | `AddSpline` | Heatmap | `AddHeatmap` |
| Area | `AddArea` | ColumnRange | `AddColumnRange` |
| Column | `AddColumn` | AreaRange | `AddAreaRange` |
| Bar | `AddBar` | Funnel | `AddFunnel` |
| Pie / Donut | `AddPie` | Treemap | `AddTreemap` |
| Scatter | `AddScatter` | Radar | `AddRadar` |
| Waterfall | `AddWaterfall` | BoxPlot | `AddBoxPlot` |
| Gauge | `AddGauge` | ErrorBar | `AddErrorBar` |
| DataRing | `AddDataRing` | Candlestick | `AddCandlestick` |
| Dumbbell | `AddDumbbell` | OHLC | `AddOhlc` |
| Stream | `AddStream` | Gantt | `AddGantt` |
| Sankey | `AddSankey` | Parliament | `AddParliament` |

---

## Render Modes

| Mode | CSS Hover | JS | Safe for |
|---|---|---|---|
| `Static` | No | No | PDF, email |
| `Animated` (default) | Yes | No | Browser, Blazor |
| `Interactive` | Yes | Yes | Browser only |

---

## Themes

```csharp
// Built-in themes
.Theme(ChartTheme.Dark)
.Theme(ChartTheme.Pastel)
.Theme(ChartTheme.Monochrome)
.Theme(ChartTheme.Default)

// Custom theme — all parameters optional
.Theme(ChartTheme.Custom(
    backgroundColor: "#1e1e2e",
    plotBackgroundColor: "#2a2a3d",
    textColor: "#cdd6f4",
    fontFamily: "JetBrains Mono",
    colors: new[] { "#89b4fa", "#a6e3a1", "#fab387" }))

// Ad-hoc palette
.Colors("#e63946", "#457b9d", "#2a9d8f")
```

---

## Pie / Donut Chart

```csharp
string svg = ChartBuilder.Create()
    .Title("Browser Share")
    .AsPie()
    .Series(s => s
        .Add("Shares", new double[] { 61, 25, 9, 5 }, cfg => cfg
            .DonutHolePercent(50)
            .DonutCenter("Browsers")))
    .RenderToSvg();
```

---

## Axis Shortcuts

```csharp
.XAxisFormat("{value} kg")          // label format string
.XAxisTickInterval("every 5")       // tick interval hint
.YAxisTickInterval("10")
```

---

## Markers

```csharp
.Series(s => s.AddLine("Sales", data, cfg => cfg
    .MarkerSize(6)          // radius in px (default 4-5)
    .MarkerEnabled(false)   // hide dots entirely
))
```

---

## Tooltip Customisation

```csharp
.Tooltip(t => t
    .HeaderFormat("Week: {label}")
    .PointFormat("Value: {value}"))
```

---

## Legend Position Shortcuts

```csharp
.Legend(l => l.TopLeft())
.Legend(l => l.TopCenter())
.Legend(l => l.TopRight())
.Legend(l => l.BottomLeft())
.Legend(l => l.BottomCenter())
.Legend(l => l.BottomRight())
```

---

## Fork — Chart Variants

Create a deep copy of a builder to produce related charts without mutation:

```csharp
var baseChart = ChartBuilder.Create()
    .Title("Revenue")
    .Size(700, 400)
    .Series(s => s.AddLine("2024", data));

var darkVariant = baseChart.Fork()
    .Theme(ChartTheme.Dark);

string lightSvg = baseChart.RenderToSvg();
string darkSvg  = darkVariant.RenderToSvg();
```

---

## Dependency Injection (`IChartBuilder`)

```csharp
// Register
services.AddTransient<IChartBuilder>(_ => ChartBuilder.Create());

// Use
public class ReportService(IChartBuilder chart)
{
    public string Build() => chart
        .Title("Revenue")
        .Series(s => s.AddLine("2024", data))
        .RenderToSvg();
}
```

---

## Async Rendering

```csharp
// .NET 6+
await chart.RenderToFileAsync("report.svg");
await chart.RenderToStreamAsync(responseStream);
```

---

## Input Validation

| Scenario | Exception |
|---|---|
| `Size(0, 400)` | `ArgumentOutOfRangeException` |
| `YAxis("Y", min: 100, max: 50)` | `ArgumentException` |
| `Animate(0)` | `ArgumentOutOfRangeException` |
| `FillOpacity(1.5)` | `ArgumentOutOfRangeException` |
| `MarkerSize(0)` | `ArgumentOutOfRangeException` |
| `XAxisFormat("  ")` | `ArgumentException` |
| `ChartTheme.Custom(colors: [])` | `ArgumentException` |
| `RenderToFile` with missing directory | `DirectoryNotFoundException` |

---

## License

Released under the **MIT License** — free for commercial and personal use, modification, and redistribution. See [LICENSE](LICENSE) for the full text.


