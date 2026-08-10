# API Reference

Complete method reference for every builder in TerraFluent.Chart.Reporting.

---

## ChartBuilder

Entry point: `ChartBuilder.Create()`

### Factory

| Method | Returns | Description |
|---|---|---|
| `ChartBuilder.Create()` | `ChartBuilder` | Creates a new builder with the default `SvgRenderer`. |
| `ChartBuilder.Create(ISvgRenderer)` | `ChartBuilder` | Creates a new builder with a custom renderer. |
| `Fork()` | `ChartBuilder` | Returns a deep-copy of the current builder state. Subsequent changes to either builder are independent. |

---

### Chart Type (default type for `.Series(s => s.Add(…))`)

> These methods set the fallback type used by the generic `Add()` method only.  
> Typed helpers like `AddLine`, `AddColumn`, `AddArea` always use their own explicit type.

| Method | Chart type set |
|---|---|
| `AsLine()` | `ChartType.Line` |
| `AsSpline()` | `ChartType.Spline` |
| `AsArea()` | `ChartType.Area` |
| `AsColumn()` | `ChartType.Column` |
| `AsBar()` | `ChartType.Bar` |
| `AsPie()` | `ChartType.Pie` |
| `AsScatter()` | `ChartType.Scatter` |
| `AsWaterfall()` | `ChartType.Waterfall` |
| `AsGauge()` | `ChartType.Gauge` |
| `AsDataRing()` | `ChartType.DataRing` |
| `AsBubble()` | `ChartType.Bubble` |
| `AsHeatmap()` | `ChartType.Heatmap` |
| `AsColumnRange()` | `ChartType.ColumnRange` |
| `AsAreaRange()` | `ChartType.AreaRange` |
| `AsFunnel()` | `ChartType.Funnel` |
| `AsTreemap()` | `ChartType.Treemap` |

---

### Render Mode

| Method | Description |
|---|---|
| `AsStatic()` | No CSS hover, no JS, no SMIL. Safe for PDF and email. Also disables animation. |
| `AsAnimated()` | SMIL/CSS animations, no JS. Suitable for Blazor and browser embedding. |
| `AsInteractive()` | CSS hover + embedded JS interactions. Browser-only. |

---

### Canvas

| Method | Parameters | Description |
|---|---|---|
| `Size(width, height)` | `int, int` | Sets width and height in pixels. Both must be > 0. |
| `Width(width)` | `int?` | Sets only width. Pass `null` for responsive (fluid) width. |
| `Height(height)` | `int` | Sets only height. Must be > 0. |
| `ResponsiveWidth()` | — | Clears fixed width → SVG renders with `width="100%"`. |
| `Background(color)` | `string` | Overrides the active theme's background colour. |

---

### Title & Subtitle

| Method | Description |
|---|---|
| `Title(string)` | Sets title text. Pass `null` or empty to hide. |
| `Title(Action<ChartTitle>)` | Full title config (text, alignment, custom CSS style). |
| `Subtitle(string)` | Sets subtitle text below the title. |
| `Subtitle(Action<ChartTitle>)` | Full subtitle config. |

**`ChartTitle` properties** (used in the lambda overloads):

| Property | Type | Description |
|---|---|---|
| `Text` | `string?` | Title text. |
| `Align` | `string` | `"left"`, `"center"` (default), or `"right"`. |
| `Style` | `string?` | Inline CSS applied to the title `<text>` element. |

---

### Theme & Colors

| Method | Description |
|---|---|
| `Theme(ChartTheme)` | Applies a complete theme (background, palette, fonts). |
| `Colors(params string[])` | Overrides the palette colour array without changing the rest of the theme. |
| `Colors(IEnumerable<string>)` | Same but accepts any enumerable (e.g. from a database). |

**Built-in themes:** `ChartTheme.Default`, `ChartTheme.Dark`, `ChartTheme.Pastel`, `ChartTheme.Monochrome`

See [Themes & Styling](themes-and-styling.md) for details.

---

### Stacking

| Method | Description |
|---|---|
| `StackNormal()` | Column and Area series stack cumulatively (totals visible). |
| `StackPercent()` | Column and Area series normalise to 100 % of the total per category. |

---

### Axes

#### X-Axis

| Method | Description |
|---|---|
| `XAxis(Action<Axis>)` | Full X-axis config via lambda. |
| `XAxis(string title, params string[] categories)` | Shorthand: set title + categories in one call. |
| `Labels(params string[])` | Alternative shorthand — sets categories only (no title). |
| `XAxisFormat(string)` | Tick-label format string, e.g. `"{value}%"`. |
| `XAxisTickInterval(double)` | Distance between ticks in data units. Must be > 0. |

#### Y-Axis (primary)

| Method | Description |
|---|---|
| `YAxis(Action<Axis>)` | Full Y-axis config via lambda. |
| `YAxis(string title, double? min, double? max)` | Shorthand: title + optional scale bounds. |
| `YAxisFormat(string)` | Tick-label format string, e.g. `"${value}k"`. |
| `YAxisTickInterval(double)` | Distance between ticks in data units. Must be > 0. |

#### Y-Axis2 (secondary, right side)

| Method | Description |
|---|---|
| `YAxis2(Action<Axis>)` | Configures the right-hand Y-axis. Creates it if it doesn't exist. |

Bind a series to the secondary axis with `cfg.OnSecondaryAxis()` inside `AddLine(…, cfg => cfg.OnSecondaryAxis())`.

#### `Axis` Properties (inside lambda)

| Property | Type | Default | Description |
|---|---|---|---|
| `Title` | `string?` | `null` | Axis title text. |
| `Min` | `double?` | auto | Minimum scale value. |
| `Max` | `double?` | auto | Maximum scale value. |
| `Visible` | `bool` | `true` | Show/hide the axis. |
| `GridLineVisible` | `bool` | `true` | Show/hide grid lines. |
| `GridLineColor` | `string?` | theme | Grid line colour. |
| `LabelFormat` | `string?` | `null` | Tick label format (`{value}`). |
| `TickInterval` | `double?` | auto | Distance between ticks. |
| `LabelRotation` | `double` | 0 | Rotation in degrees (e.g. −45 for diagonal labels). |
| `Categories` | `List<string>` | `[]` | Category names for categorical axes. |
| `PlotBands` | `List<PlotBand>` | `[]` | Reference bands. |
| `PlotLines` | `List<PlotLine>` | `[]` | Reference lines. |

---

### Plot Annotations

#### PlotBand (shaded reference region)

```csharp
// Shorthand on ChartBuilder:
.PlotBand(from: 0, to: 200, color: "rgba(144,237,125,0.15)", label: "Good")

// Via lambda:
.YAxis(y => y.PlotBands.Add(new PlotBand { From = 0, To = 200,
    Color = "rgba(144,237,125,0.15)", Label = "Good" }))
```

| Parameter | Type | Description |
|---|---|---|
| `from` | `double` | Lower Y value of the band. Must be < `to`. |
| `to` | `double` | Upper Y value of the band. |
| `color` | `string` | Fill colour (CSS colour). Default: semi-transparent blue. |
| `label` | `string?` | Optional label at the right edge of the band. |

#### PlotLine (reference line)

```csharp
// Shorthand on ChartBuilder:
.PlotLine(value: 500, color: ChartColor.ChartRed, width: 2, label: "SLA limit", dashStyle: "Dash")

// Via lambda:
.YAxis(y => y.PlotLines.Add(new PlotLine { Value = 500,
    Color = ChartColor.ChartRed, Width = 2, DashStyle = "Dash", Label = "SLA limit" }))
```

| Parameter | Type | Description |
|---|---|---|
| `value` | `double` | Y value at which the line is drawn. |
| `color` | `string` | Stroke colour. Default: `ChartColor.Red` (`#FF0000`). |
| `width` | `int` | Stroke width px. Default: 1. |
| `label` | `string?` | Optional label at the right edge. |
| `dashStyle` | `string?` | `"Solid"`, `"Dash"`, `"Dot"`, `"DashDot"`, `"LongDash"`. |

---

### Legend

```csharp
.Legend(l => l
    .AtBottom().AlignCenter().Horizontal()
    .Padding(10).ItemFontSize(12)
    .Border("#e2e8f0", width: 1, radius: 4)
    .BackgroundColor("rgba(255,255,255,0.9)"))

.HideLegend()   // shorthand to disable
```

**`LegendBuilder` Methods:**

| Method | Description |
|---|---|
| `Disable()` | Hides the legend. |
| `AlignLeft()` / `AlignCenter()` / `AlignRight()` | Horizontal alignment. |
| `AtTop()` / `AtMiddle()` / `AtBottom()` | Vertical position. |
| `TopLeft()` / `TopCenter()` / `TopRight()` | Combined shorthand. |
| `BottomLeft()` / `BottomCenter()` / `BottomRight()` | Combined shorthand. |
| `Horizontal()` | Items flow left-to-right (default). |
| `Vertical()` | Items stack in a single column. |
| `Padding(int)` | Inner padding between border and items (px). |
| `Offset(int x, int y)` | Pixel offset from the computed position. |
| `OffsetX(int)` / `OffsetY(int)` | Individual axis offsets. |
| `ItemFontSize(int)` | Label font size (px). |
| `ItemFontColor(string)` | Label text colour. |
| `ItemStyle(string css)` | Raw CSS applied to each item `<text>`. |
| `SymbolSize(int width, int height)` | Dimensions of the colour swatch. |
| `SymbolRadius(int)` | Corner radius of the swatch (use half of width for circles). |
| `Border(string color, int width, int radius)` | Legend box border. |
| `BorderColor(string)` / `BorderWidth(int)` / `BorderRadius(int)` | Individual border properties. |
| `BackgroundColor(string)` | Legend box fill colour. |
| `Margin(int)` | Outer margin between the legend box and chart edges (px). |

---

### Tooltip

```csharp
.Tooltip(t => t
    .BackgroundColor("rgba(30,30,60,0.92)")
    .TextColor("#e8f4ff")
    .FontSize(12)
    .Border(ChartColor.ChartBlue, width: 1, radius: 6)
    .Padding(14)
    .Format("{label}: ${value}k"))

.DisableTooltip()   // shorthand to disable
```

**`TooltipBuilder` Methods:**

| Method | Description |
|---|---|
| `Disable()` | Disables tooltips. |
| `BackgroundColor(string)` | Tooltip box background colour. |
| `TextColor(string)` | Text colour inside the tooltip. |
| `FontSize(int)` | Font size (px). |
| `FontFamily(string?)` | Font family. Pass `null` to inherit the chart theme font. |
| `Border(string color, int width, int radius)` | Sets all border properties at once. |
| `BorderColor(string)` / `BorderWidth(int)` / `BorderRadius(int)` | Individual border properties. |
| `Padding(int)` | Inner padding (px). |
| `HideArrow()` | Removes the triangular pointer beneath the tooltip box. |
| `Format(string)` | Content template. Placeholders: `{label}` = series name, `{value}` = numeric value. |
| `HeaderFormat(string)` | Optional header line above the data row. |
| `PointFormat(string)` | Per-point row format (used with `HeaderFormat`). |
| `TransitionDuration(double seconds)` | CSS transition for fade-in/out. Default 0.15 s. |

---

### Animation

```csharp
.Animate(800)                        // enable with duration (ms)
.Animation(a => a.EaseInOut())       // configure easing
.DisableAnimation()                  // turn off
```

**`AnimationBuilder` Methods:**

| Method | Description |
|---|---|
| `Duration(TimeSpan)` | Animation duration. |
| `DurationMs(int)` | Duration in milliseconds (convenience overload). |
| `EaseIn()` | Starts slow, ends fast. |
| `EaseOut()` | Starts fast, ends slow (default). |
| `EaseInOut()` | Slow at both ends. |
| `Linear()` | Constant speed. |
| `Bounce()` | Bounces at the end. |
| `Elastic()` | Elastic snap at the end. |
| `Enable()` / `Disable()` | Toggle animation on/off. |

---

### Series Management

| Method | Description |
|---|---|
| `Series(Action<ChartSeriesBuilder>)` | Opens the series scope. Chain `AddLine(…)`, `AddColumn(…)`, etc. inside. |
| `ShowDataLabels()` | Enables data labels on ALL series (existing and future). |
| `ClearSeries()` | Removes all series from the chart. |
| `RemoveSeries(string name)` | Removes the first series with the given name. |

---

### Output / Rendering

| Method | Return | Description |
|---|---|---|
| `RenderToSvg()` | `string` | Renders and returns the SVG markup. |
| `RenderToHtml(caption?, cssClass?)` | `string` | SVG wrapped in `<figure>`. |
| `RenderToFile(filePath)` | `void` | Writes SVG to a file. Directory must exist. |
| `RenderToHtmlFile(filePath, caption?, cssClass?)` | `void` | Writes HTML fragment to a file. |
| `RenderToStream(stream)` | `void` | Writes UTF-8 SVG bytes to a stream. |
| `RenderToBytes()` | `byte[]` | Returns the SVG as a UTF-8 byte array. |
| `RenderToStreamAsync(stream, ct)` | `Task` | Async. `.NET 6+` only. |
| `RenderToFileAsync(filePath, ct)` | `Task` | Async. `.NET 6+` only. |
| `RenderToHtmlFileAsync(filePath, caption?, cssClass?, ct)` | `Task` | Async. `.NET 6+` only. |
| `GetOptions()` | `ChartOptions` | Returns the raw options for advanced inspection. |
| `Build()` | `ChartOptions` | Returns an independent snapshot of the current options. |

---

## ChartSeriesBuilder

Obtained inside `.Series(s => s.AddLine(…))`. All `Add*` methods return `ChartSeriesBuilder` so you can chain multiple series:

```csharp
.Series(s => s
    .AddLine("Revenue", data1, cfg => cfg.Color(ChartColor.ChartBlue))
    .AddColumn("Cost",  data2, cfg => cfg.Color(ChartColor.ChartOrange))
    .AddArea("Profit",  data3))
```

### Standard Series (double?[] or double[])

All accept an optional `Action<SeriesBuilder>` as the last parameter for per-series configuration.

| Method | Chart type | Data type |
|---|---|---|
| `Add(name, data, cfg?)` | Uses `PrimaryChartType` (set by `AsLine()` etc.) | `IEnumerable<double?>` |
| `AddLine(name, data, cfg?)` | Line | `IEnumerable<double?>` or `double[]` |
| `AddSpline(name, data, cfg?)` | Spline | `IEnumerable<double?>` or `double[]` |
| `AddArea(name, data, cfg?)` | Area | `IEnumerable<double?>` or `double[]` |
| `AddColumn(name, data, cfg?)` | Column | `IEnumerable<double?>` or `double[]` |
| `AddBar(name, data, cfg?)` | Bar (horizontal) | `IEnumerable<double?>` or `double[]` |
| `AddScatter(name, data, cfg?)` | Scatter | `IEnumerable<double?>` or `double[]` |
| `AddFunnel(name, data, cfg?)` | Funnel | `IEnumerable<double?>` or `double[]` |
| `AddTreemap(name, data, cfg?)` | Treemap | `IEnumerable<double?>` or `double[]` |

**Null gaps:** A `null` in a `double?[]` skips that data point — the line/column is not drawn at that index.

### Waterfall

```csharp
.AddWaterfall(
    name:   "P&L",
    data:   new double?[] { 500, 800, -320, 980 },
    totals: new[] { true, false, false, true })
```

| Parameter | Type | Description |
|---|---|---|
| `name` | `string` | Series display name. |
| `data` | `IEnumerable<double?>` | Values. Positive = up, negative = down. `null` = skip. |
| `totals` | `IEnumerable<bool>` | `true` at index `i` = column `i` resets to an absolute value (draws from zero). |
| `cfg` | `Action<SeriesBuilder>?` | Optional per-series styling. |

### Gauge / DataRing (single value)

```csharp
.AddGauge("CPU %", 67, cfg => cfg.Color(ChartColor.ChartBlue))
.AddDataRing("CSAT", 87, cfg => cfg.Color(ChartColor.ChartBlue))
```

| Parameter | Type | Description |
|---|---|---|
| `name` | `string` | Series display name. |
| `value` | `double` | The value to display. Must be within `YAxis.Min`–`YAxis.Max`. |
| `cfg` | `Action<SeriesBuilder>?` | Optional styling. |

### Bubble (3D scatter)

```csharp
.AddBubble("Product A", new[] { new BubblePoint(12, 28, 85) }, cfg => …)
```

| Parameter | Type | Description |
|---|---|---|
| `name` | `string` | Series display name. |
| `data` | `IEnumerable<BubblePoint>` | Collection of `(X, Y, Z)` triplets. |
| `cfg` | `Action<SeriesBuilder>?` | Optional styling. |

### Heatmap (grid cells)

```csharp
.AddHeatmap("Sales", new[] { new HeatmapPoint(0, 0, 42) }, cfg => …)
```

| Parameter | Type | Description |
|---|---|---|
| `name` | `string` | Series display name. |
| `data` | `IEnumerable<HeatmapPoint>` | Collection of `(Col, Row, Value)` triples. |
| `cfg` | `Action<SeriesBuilder>?` | Optional styling + row labels. |

### ColumnRange / AreaRange (low–high)

```csharp
.AddColumnRange("London", new[] { new RangePoint(2, 8) }, cfg => …)
.AddAreaRange("Band",     new[] { new RangePoint(120, 160) }, cfg => …)
```

| Parameter | Type | Description |
|---|---|---|
| `name` | `string` | Series display name. |
| `data` | `IEnumerable<RangePoint>` | Collection of `(Low, High)` pairs. |
| `cfg` | `Action<SeriesBuilder>?` | Optional styling. |

---

## SeriesBuilder

Obtained inside the optional `cfg => …` lambda of any `Add*` method.

### Appearance

| Method | Description |
|---|---|
| `Color(string)` | Fill / stroke colour (hex or CSS colour). |
| `LineWidth(int px)` | Line stroke width (Line, Spline, Area). |
| `Solid()` | Solid line (default). |
| `Dashed()` | Dashed line. |
| `Dotted()` | Dotted line. |
| `DashDotted()` | Alternating dash-dot. |
| `LongDashed()` | Long-dashed line. |
| `FillOpacity(double 0–1)` | Area fill transparency. Default 0.25. |

### Border (Column / Bar / Scatter)

| Method | Description |
|---|---|
| `Border(string color, int width, int radius)` | Sets all border properties at once. |
| `BorderColor(string)` | Stroke colour around bars / scatter markers. |
| `BorderWidth(int px)` | Border stroke width. |
| `BorderRadius(int px)` | Corner radius for column/bar rectangles. |

### Markers (Line / Scatter)

| Method | Description |
|---|---|
| `MarkerSize(int radiusPx)` | Radius of the dot at each data point. Must be > 0. |
| `MarkerEnabled(bool)` | Show/hide markers for Line and Spline series. |

### Visibility & Legend

| Method | Description |
|---|---|
| `Visible(bool)` | Show/hide the series in the plot area. |
| `Hide()` | Shorthand for `Visible(false)`. |
| `ShowInLegend(bool)` | Show/hide this series in the legend. |
| `HideFromLegend()` | Shorthand for `ShowInLegend(false)`. |

### Axis Binding

| Method | Description |
|---|---|
| `OnYAxis(int index)` | `0` = primary left, `1` = secondary right. |
| `OnSecondaryAxis()` | Shorthand for `OnYAxis(1)`. |

### Pie / Donut

| Method | Description |
|---|---|
| `DonutHole(double fraction)` | 0 = solid pie, 0.5 = half-radius donut, clamped to [0, 0.95]. |

### Data Labels

| Access | Description |
|---|---|
| `cfg.DataLabel` | Direct access to `DataLabelOptions` — chain its methods. |
| `cfg.Label(dl => …)` | Lambda shorthand that returns `SeriesBuilder` for chaining. |

**`DataLabelOptions` Methods:**

| Method | Description |
|---|---|
| `Show()` | Enables data labels for this series. |
| `Hide()` | Disables data labels. |
| `Format(string)` | Label text template. `{value}` = numeric value. |
| `Color(string)` | Label text colour. |
| `FontSize(int px)` | Label font size. |
| `Background(string)` | Label pill background colour. |
| `Radius(double fraction)` | Pie/Donut: radial distance from center. `> 1.0` places label outside with spline connector. |
| `OffsetY(double px)` | Vertical offset (positive = down). |

### Donut Center Label

| Access | Description |
|---|---|
| `cfg.DonutCenter` | Direct access to `DonutCenterOptions`. |
| `cfg.Center(c => …)` | Lambda shorthand. |

**`DonutCenterOptions` Methods:**

| Method | Description |
|---|---|
| `Show(string? text)` | Shows the center label. If `text` is omitted, the sum of the pie data is auto-computed. |
| `Hide()` | Hides the center label. |
| `Title(string)` | Caption displayed above the main value. |
| `Color(string)` | Main value text colour. |
| `TitleColor(string)` | Caption text colour. |
| `FontSize(int px)` | Main value font size. |
| `TitleFontSize(int px)` | Caption font size. |

### Heatmap Row Labels

| Access | Description |
|---|---|
| `cfg.HeatmapRowLabels` | `List<string>` — add row labels in row-0-first order. |

```csharp
cfg.HeatmapRowLabels.AddRange(new[] { "North", "South", "East" });
```

---

## Easing enum

| Value | SMIL equivalent | Curve description |
|---|---|---|
| `EaseOut` (default) | `ease-out` | Fast start, slow end |
| `EaseIn` | `ease-in` | Slow start, fast end |
| `EaseInOut` | `ease-in-out` | Slow at both ends |
| `Linear` | `linear` | Constant speed |
| `Bounce` | custom | Elastic bounce at finish |
| `Elastic` | custom | Overshoot + settle |

---

## SvgMode enum

| Value | CSS hover | JS | SMIL |
|---|---|---|---|
| `Static` | No | No | No |
| `Animated` | Yes | No | Yes |
| `Interactive` | Yes | Yes | Yes |

---

## ChartType enum

`Line`, `Spline`, `Area`, `Column`, `Bar`, `Pie`, `Scatter`, `Waterfall`, `Gauge`, `DataRing`, `Bubble`, `Heatmap`, `ColumnRange`, `AreaRange`, `Funnel`, `Treemap`

---

## Stacking enum

| Value | Description |
|---|---|
| `None` | Default — series plotted independently. |
| `Normal` | Each series stacks on top of the previous (shows cumulative totals). |
| `Percent` | Each series normalised to a % of the per-category total (always sums to 100 %). |

---

## ChartColor

`static class` in namespace `TerraFluent.Chart.Reporting.Models`. Provides 130+ named `public const string` colour constants and utility methods.

```csharp
using TerraFluent.Chart.Reporting.Models;
```

### Constant groups

| Group | Prefix | Example |
|---|---|---|
| Chart palette | `ChartColor.Chart…` | `ChartColor.ChartBlue` = `#7CB5EC` |
| Pastel palette | `ChartColor.Pastel…` | `ChartColor.PastelSkyBlue` = `#A8D8EA` |
| Standard CSS / web | various | `ChartColor.Red`, `ChartColor.White`, `ChartColor.Black` |

### Pre-built palettes

| Property | Type | Description |
|---|---|---|
| `ChartColor.Palette.Default` | `string[]` | HighCharts-inspired 8-colour palette |
| `ChartColor.Palette.Pastel` | `string[]` | 8 soft pastel colours |
| `ChartColor.Palette.Material` | `string[]` | Material Design palette |
| `ChartColor.Palette.Business` | `string[]` | Blues and greys |
| `ChartColor.Palette.Accessible` | `string[]` | WCAG high-contrast colours |
| `ChartColor.Palette.TrafficLight` | `string[]` | Red / amber / green |
| `ChartColor.Palette.Monochrome` | `string[]` | Greyscale range |

### Utility methods

| Method | Returns | Description |
|---|---|---|
| `FromRgb(r, g, b)` | `string` | Builds a hex string from RGB byte components. |
| `WithOpacity(hex, alpha)` | `string` | Converts a hex colour to `rgba(…, alpha)`. Alpha 0–1. |
| `Lighten(hex, amount)` | `string` | Lightens the colour by `amount` (0–1). |
| `Darken(hex, amount)` | `string` | Darkens the colour by `amount` (0–1). |
| `Mix(hexA, hexB, weight)` | `string` | Linearly blends two hex colours. `weight=0.5` = equal blend. |

See [Themes & Styling → ChartColor Catalogue](themes-and-styling.md#chartcolor-catalogue) for full usage examples.
