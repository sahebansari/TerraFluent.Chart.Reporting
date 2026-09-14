# TerraFluent.Chart.Reporting

A fluent C# library for generating **SVG charts server-side** — zero JavaScript dependency, zero external NuGet dependencies.

[![Website](https://img.shields.io/badge/website-terrafluent.dev%2Fchart-blue.svg)](https://terrafluent.dev/chart/)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://github.com/sahebansari/TerraFluent.Chart.Reporting/blob/master/LICENSE)
[![CI](https://github.com/sahebansari/TerraFluent.Chart.Reporting/actions/workflows/ci.yml/badge.svg?branch=master)](https://github.com/sahebansari/TerraFluent.Chart.Reporting/actions/workflows/ci.yml)
[![.NET](https://img.shields.io/badge/.NET-netstandard2.0%20%7C%202.1%20%7C%206%20%7C%208%20%7C%2010-512BD4.svg)](#supported-targets)
[![Dependencies: none](https://img.shields.io/badge/dependencies-none-brightgreen.svg)](#supported-targets)

- **Zero dependencies** — pure Base Class Library, nothing added to your dependency graph.
- **Zero JavaScript required** — Static and Animated modes are pure SVG, CSS, and SMIL.
- **26 chart types** — line, bar, pie, financial, flow, and specialist types, all through one fluent API.
- **Three render modes** — Static (PDF/email-safe), Animated (SMIL), Interactive (JS, browser-only).
- **18 built-in themes** plus a `ChartTheme.Custom(...)` factory for fully custom palettes.
- **Multi-target** — `netstandard2.0`, `netstandard2.1`, `net6.0`, `net8.0`, `net10.0` from one codebase.

---

## Table of Contents

- [Supported Targets](#supported-targets)
- [Installation](#installation)
- [Quick Start](#quick-start)
- [Documentation](#documentation)
- [Chart Types](#chart-types)
- [Render Modes](#render-modes)
- [Themes](#themes)
- [Pie / Donut Chart](#pie--donut-chart)
- [Axis Shortcuts](#axis-shortcuts)
- [Markers](#markers)
- [Tooltip Customisation](#tooltip-customisation)
- [Legend Position Shortcuts](#legend-position-shortcuts)
- [Fork — Chart Variants](#fork--chart-variants)
- [Dependency Injection](#dependency-injection-ichartbuilder)
- [Async Rendering](#async-rendering)
- [Input Validation](#input-validation)
- [Building from Source](#building-from-source)
- [License](#license)

---

## Supported Targets

| Framework | Version |
|---|---|
| .NET Standard | 2.0, 2.1 |
| .NET | 6.0, 8.0, 10.0 |

---

## Installation

**Via NuGet** (once published):

```bash
dotnet add package TerraFluent.Chart.Reporting
```

**Via project reference** (building from source):

```xml
<ItemGroup>
  <ProjectReference Include="..\src\TerraFluent.Chart.Reporting\TerraFluent.Chart.Reporting.csproj" />
</ItemGroup>
```

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

## Documentation

The full guide lives in [docs/](https://github.com/sahebansari/TerraFluent.Chart.Reporting/blob/master/docs/README.md):

| Guide | What it covers |
|---|---|
| [Getting Started](https://github.com/sahebansari/TerraFluent.Chart.Reporting/blob/master/docs/getting-started.md) | Install, first chart, ASP.NET Core / Blazor integration |
| [Chart Showcase](https://github.com/sahebansari/TerraFluent.Chart.Reporting/blob/master/docs/showcase.md) | Live visual gallery of every chart type and feature |
| [Chart Types](https://github.com/sahebansari/TerraFluent.Chart.Reporting/blob/master/docs/chart-types.md) | All 26 chart types with full code + SVG output |
| [Themes & Styling](https://github.com/sahebansari/TerraFluent.Chart.Reporting/blob/master/docs/themes-and-styling.md) | Built-in themes, modern styling, custom themes, colour catalogue |
| [Advanced Features](https://github.com/sahebansari/TerraFluent.Chart.Reporting/blob/master/docs/advanced.md) | Render modes, output methods, axes, stacking, Fork, DI |
| [API Reference](https://github.com/sahebansari/TerraFluent.Chart.Reporting/blob/master/docs/api-reference.md) | Complete method reference for every builder, enum, and data type |
| [Troubleshooting & FAQ](https://github.com/sahebansari/TerraFluent.Chart.Reporting/blob/master/docs/troubleshooting.md) | Common issues, exceptions, and integration answers |

---

## Chart Types

26 chart types are supported. See the [Chart Types guide](https://github.com/sahebansari/TerraFluent.Chart.Reporting/blob/master/docs/chart-types.md) for full examples.

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

18 built-in themes are included — `Default`, `Dark`, `Pastel`, `Monochrome`, `Ocean`, `Sunset`, `Forest`, `Neon`, `Minimal`, `Warm`, `Arctic`, `Business`, `Material`, `TrafficLight`, `Accessible`, `Vivid`, `HighContrast`, and `Modern`. See [Themes & Styling](https://github.com/sahebansari/TerraFluent.Chart.Reporting/blob/master/docs/themes-and-styling.md) for the full catalogue.

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

Every theme ships with `ModernStyle` on — rounded bars, gradient fills, hollow-ring markers, and (in Interactive mode) hover bands. Clone a theme and set `ModernStyle = false` for the classic flat look; see [Modern Styling](https://github.com/sahebansari/TerraFluent.Chart.Reporting/blob/master/docs/themes-and-styling.md#modern-styling).

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

## Building from Source

```bash
git clone https://github.com/sahebansari/TerraFluent.Chart.Reporting.git
cd TerraFluent.Chart.Reporting

dotnet build TerraFluent.Chart.Reporting.sln
dotnet test tests/TerraFluent.Chart.Reporting.Tests/TerraFluent.Chart.Reporting.Tests.csproj
```

The library targets `netstandard2.0`, `netstandard2.1`, `net6.0`, `net8.0`, and `net10.0` from a single project — building it requires the .NET 10 SDK (which includes the earlier runtimes needed to build the older targets).

---

## License

Released under the **MIT License** — free for commercial and personal use, modification, and redistribution. See [LICENSE](https://github.com/sahebansari/TerraFluent.Chart.Reporting/blob/master/LICENSE) for the full text.

---

Project website: **[terrafluent.dev/chart](https://terrafluent.dev/chart/)**


