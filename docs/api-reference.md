# API Reference

A complete method reference for every public builder, enum, theme, and data type in TerraFluent.Chart.Reporting.

All fluent methods return the builder they were called on, so every call can be chained. Unless noted otherwise, methods validate their arguments and throw `ArgumentException` / `ArgumentOutOfRangeException` / `ArgumentNullException` on invalid input.

> **Namespaces**
> - Builders: `TerraFluent.Chart.Reporting.Builder`
> - Models & data types: `TerraFluent.Chart.Reporting.Models`
> - Enums: `TerraFluent.Chart.Reporting.Enums`

---

## Contents

- [ChartBuilder](#chartbuilder) — the fluent entry point
- [Series (ChartSeriesBuilder)](#series-chartseriesbuilder) — adding data series
- [SeriesBuilder](#seriesbuilder) — per-series styling
- [LegendBuilder](#legendbuilder)
- [TooltipBuilder](#tooltipbuilder)
- [Credits / branding](#credits--branding)
- [AnimationBuilder](#animationbuilder)
- [AnnotationBuilder](#annotationbuilder)
- [LabelLayoutBuilder](#labellayoutbuilder)
- [DataLabelOptions](#datalabeloptions)
- [DonutCenterOptions](#donutcenteroptions)
- [Enums](#enums)
- [ChartTheme](#charttheme)
- [ChartColor](#chartcolor)
- [Data-point types](#data-point-types)
- [IChartBuilder](#ichartbuilder)

---

## ChartBuilder

`TerraFluent.Chart.Reporting.Builder.ChartBuilder`

The fluent entry point for building a chart. Create one with `ChartBuilder.Create()`.

### Creating a builder

| Method | Returns | Description |
|---|---|---|
| `ChartBuilder.Create()` | `ChartBuilder` | Creates a fresh builder with the default renderer. |
| `ChartBuilder.Create(ISvgRenderer renderer)` | `ChartBuilder` | Creates a builder with a custom renderer implementation. |
| `ChartBuilder.FromJson(string json)` | `ChartBuilder` | Rehydrates a builder from serialized `ChartOptions` JSON. *(net6.0+ only)* |

### Chart type (default for `Add`)

These set the fallback type used by the generic `Series(s => s.Add(...))` method. Prefer the typed `AddLine`/`AddColumn`/… helpers instead.

`AsPie()` · `AsLine()` · `AsArea()` · `AsColumn()` · `AsBar()` · `AsSpline()` · `AsScatter()` · `AsWaterfall()` · `AsGauge()` · `AsDataRing()` · `AsBubble()` · `AsHeatmap()` · `AsColumnRange()` · `AsAreaRange()` · `AsFunnel()` · `AsTreemap()` · `AsDumbbell()` · `AsStream()` · `AsGantt()` · `AsSankey()`

### Render mode

| Method | Description |
|---|---|
| `AsStatic()` | Pure SVG — no CSS hover, no JS, no animation. Safe for PDF and email. Also disables animation. |
| `AsAnimated()` | SVG + CSS hover + SMIL animation. No JavaScript. Ideal for Blazor and browser embedding. |
| `AsInteractive()` | SVG + CSS + embedded JavaScript (tooltips, legend toggle, export, drill-down). Browser only. |

### Dimensions & background

| Method | Description |
|---|---|
| `Size(int width, int height)` | Sets width and height in pixels. |
| `Width(int? width)` | Sets width; pass `null` for responsive (`width="100%"`). |
| `Height(int height)` | Sets height. |
| `ResponsiveWidth()` | Clears the fixed width — SVG renders at `width="100%"`. |
| `Responsive(bool responsive = true)` | Alias for `ResponsiveWidth()`. |
| `Background(string color)` | Sets the chart background colour. |

### Theme & palette

| Method | Description |
|---|---|
| `Theme(ChartTheme theme)` | Applies a built-in or custom theme. |
| `Colors(params string[] colors)` | Overrides the series palette. |
| `Colors(IEnumerable<string> colors)` | Overrides the series palette from any enumerable. |
| `StackNormal()` | Stacks Column/Area/Bar series cumulatively. |
| `StackPercent()` | Stacks series normalised to 100 %. |

### Title & subtitle

| Method | Description |
|---|---|
| `Title(string text)` | Sets the title text. |
| `Title(Action<ChartTitle> configure)` | Configures the title (text, alignment, style). |
| `Subtitle(string text)` | Sets the subtitle text. |
| `Subtitle(Action<ChartTitle> configure)` | Configures the subtitle. |

### Axes

| Method | Description |
|---|---|
| `Labels(params string[] labels)` | Sets slice or X-axis category labels. |
| `XAxis(Action<Axis> configure)` | Configures the X-axis. |
| `XAxis(string title, params string[] categories)` | Sets X-axis title + category labels. |
| `XAxisFormat(string format)` | X-axis tick-label format string, e.g. `"{value} kg"`. |
| `XAxisTickInterval(double interval)` | X-axis tick interval in data units (> 0). |
| `XAxisDateTime(IEnumerable<DateTime> values, string? format = null)` | Uses a datetime X-axis with auto tick thinning. |
| `YAxis(Action<Axis> configure)` | Configures the primary Y-axis. |
| `YAxis(string title, double? min = null, double? max = null)` | Sets Y-axis title + optional bounds. |
| `YAxisFormat(string format)` | Y-axis tick-label format string. |
| `YAxisTickInterval(double interval)` | Y-axis tick interval in data units (> 0). |
| `YAxisLogarithmic()` | Switches the primary Y-axis to a logarithmic scale. |
| `YAxisInverted(bool inverted = true)` | Inverts the primary Y-axis direction. |
| `YAxis2(Action<Axis> configure)` | Configures the secondary (right-hand) Y-axis. |
| `YAxis2Logarithmic()` | Logarithmic scale on the secondary Y-axis. |
| `YAxis2Inverted(bool inverted = true)` | Inverts the secondary Y-axis. |
| `GridLines(bool visible = true)` | Shows/hides plot grid lines. |
| `HideGridLines()` | Hides plot grid lines. |

### Plot bands & lines

| Method | Description |
|---|---|
| `PlotBand(double from, double to, string color = "rgba(68,170,213,0.15)", string? label = null)` | Adds a shaded reference band on the primary Y-axis. |
| `PlotLine(double value, string color = ChartColor.Red, int width = 1, string? label = null, string? dashStyle = null)` | Adds a reference line on the primary Y-axis. |

### Legend & tooltip

| Method | Description |
|---|---|
| `Legend(Action<LegendBuilder> configure)` | Configures the legend. See [LegendBuilder](#legendbuilder). |
| `HideLegend()` | Hides the legend. |
| `Tooltip(Action<TooltipBuilder> configure)` | Configures tooltips. See [TooltipBuilder](#tooltipbuilder). |
| `DisableTooltip()` | Disables tooltips. |

### Credits / branding

| Method | Description |
|---|---|
| `ShowCredits()` | Shows the fixed TerraFluent attribution label (`terrafluent.dev`). |
| `ShowCredits(CreditsPosition position)` | Shows the label in a chosen corner. |
| `HideCredits()` | Removes the attribution label. |

The label is **shown by default** and links to `https://terrafluent.dev`. Its **text and link are fixed** (set by the library) so output cannot be re-branded — callers may only show/hide it and choose its corner. It always renders as text; the hyperlink is clickable only when the SVG is embedded **inline** in a page — rasterised (PNG/JPEG/PDF) or `<img>`-loaded output keeps the visible text but not the link.

### Animation

| Method | Description |
|---|---|
| `Animate(int milliseconds = 800)` | Enables animation with the given duration. |
| `Animation(Action<AnimationBuilder> configure)` | Configures animation timing and easing. |
| `DisableAnimation()` | Disables load animations. |

### Series

| Method | Description |
|---|---|
| `Series(Action<ChartSeriesBuilder> configure)` | Opens the series scope. See [Series](#series-chartseriesbuilder). |
| `ClearSeries()` | Removes all series. |
| `RemoveSeries(string name)` | Removes the first series matching `name`. |
| `ShowDataLabels()` | Enables data labels on every series. |

### Export & interactivity (Interactive mode only)

| Method | Description |
|---|---|
| `ShowExportButton(string label = "⬇ SVG")` | Adds an SVG download button. |
| `ShowExportMenu(params string[] formats)` | Adds a multi-format export menu (`"SVG"`, `"PNG"`, `"JPEG"`, `"PDF"`). Omit for all four. |
| `OnPointClick(string handler)` | Registers a JavaScript click handler for data points. |

### Labels, annotations & advanced layout

| Method | Description |
|---|---|
| `LabelLayout(Action<LabelLayoutBuilder> configure)` | Controls X-axis label rotation, wrapping, skipping, scaling. See [LabelLayoutBuilder](#labellayoutbuilder). |
| `Annotations(Action<AnnotationBuilder> configure)` | Adds free-form labels/lines/rects/circles. See [AnnotationBuilder](#annotationbuilder). |
| `RangeSelector(Action<RangeSelectorOptions> configure)` | Adds the interactive navigator strip (Interactive mode). |
| `SyncGroup(string groupId)` | Joins a synchronised-tooltip group shared by charts on the same page. |
| `ShowDataTable(int rowHeight = 20, int fontSize = 10)` | Appends a data table of raw values beneath the chart. |
| `ApplyTemplate(IChartTemplate template)` | Applies a preset template, overwriting only the settings it touches. |

### Accessibility & localization

| Method | Description |
|---|---|
| `AriaLabel(string label)` | Overrides the accessible name (SVG `<title>` / `aria-label`). |
| `AriaDescription(string description)` | Overrides the accessible description (SVG `<desc>`). |
| `Culture(CultureInfo culture)` | Sets the culture for number formatting and the SVG `lang`. |
| `Culture(string cultureName)` | Sets the culture by name, e.g. `"de-DE"`. |
| `RightToLeft(bool rtl = true)` | Enables RTL text direction. |

### Data quality

| Method | Returns | Description |
|---|---|---|
| `ThrowOnDataQualityErrors()` | `ChartBuilder` | Throws `DataQualityException` on render when an `Error`-severity issue is found. |
| `AnalyzeDataQuality()` | `DataQualityReport` | Runs quality analysis without rendering. |
| `GetLastDataQualityReport()` | `DataQualityReport?` | Returns the report from the most recent analysis or render (`null` before the first). |

### Render / output

| Method | Returns | Description |
|---|---|---|
| `RenderToSvg()` | `string` | Renders and returns the SVG markup. |
| `RenderToHtml(string? caption = null, string? cssClass = null)` | `string` | Renders a self-contained HTML `<figure>` fragment. |
| `RenderToBytes()` | `byte[]` | Renders the SVG as UTF-8 bytes. |
| `RenderToDataUri()` | `string` | Renders as a `data:image/svg+xml;base64,…` URI. |
| `RenderToStream(Stream stream)` | `void` | Writes UTF-8 SVG bytes to a stream. |
| `RenderToFile(string filePath)` | `void` | Writes the SVG to a file. |
| `RenderToHtmlFile(string filePath, string? caption = null, string? cssClass = null)` | `void` | Writes the HTML fragment to a file. |
| `RenderToStreamAsync(Stream stream, CancellationToken ct = default)` | `Task` | Async stream write. *(net6.0+ only)* |
| `RenderToFileAsync(string filePath, CancellationToken ct = default)` | `Task` | Async file write. *(net6.0+ only)* |
| `RenderToHtmlFileAsync(string filePath, string? caption = null, string? cssClass = null, CancellationToken ct = default)` | `Task` | Async HTML file write. *(net6.0+ only)* |

### Snapshot & variants

| Method | Returns | Description |
|---|---|---|
| `Fork()` | `ChartBuilder` | Returns a new builder starting from a deep copy of the current configuration. |
| `Clone()` | `ChartBuilder` | Alias for `Fork()`. |
| `GetOptions()` | `ChartOptions` | Returns the live options object for advanced customisation. |
| `GetSnapshot()` | `ChartOptions` | Returns an independent deep-copy snapshot. |

---

## Series (ChartSeriesBuilder)

`TerraFluent.Chart.Reporting.Builder.ChartSeriesBuilder`

Obtained inside `.Series(s => …)`. Each `Add*` method appends one series and returns the series builder so multiple series can be chained. Every method accepts an optional `Action<SeriesBuilder>? configure` to style that series (see [SeriesBuilder](#seriesbuilder)).

### Cartesian series (`double[]` or `double?[]`)

`null` values in a `double?[]` are treated as gaps (see `NullGap`).

| Method | Data | Notes |
|---|---|---|
| `AddLine(name, data, configure?)` | `double[]` / `double?[]` | Straight-segment line. |
| `AddSpline(name, data, configure?)` | `double[]` / `double?[]` | Smooth Bézier curve. |
| `AddArea(name, data, configure?)` | `double[]` / `double?[]` | Filled area. |
| `AddColumn(name, data, configure?)` | `double[]` / `double?[]` | Vertical bars. |
| `AddBar(name, data, configure?)` | `double[]` / `double?[]` | Horizontal bars. |
| `AddScatter(name, data, configure?)` | `double[]` / `double?[]` | Dots only. |
| `AddRadar(name, data, configure?)` | `double[]` / `double?[]` | Radar/polar area. |
| `AddFunnel(name, data, configure?)` | `double[]` / `double?[]` | Funnel stages. |
| `AddTreemap(name, data, configure?)` | `double[]` / `double?[]` | Proportional rectangles. |
| `AddStream(name, data, configure?)` | `double[]` / `double?[]` | ThemeRiver stacked band. |
| `AddPie(name, data, configure?)` | `double[]` / `double?[]` | Pie/donut slices. |
| `Add(name, data, configure?)` | `double[]` / `double?[]` | Uses the builder's default chart type (`AsX()`). |

### Single-value series

| Method | Data | Notes |
|---|---|---|
| `AddGauge(name, double value, configure?)` | `double` | Semi-circular dial. |
| `AddDataRing(name, double value, configure?)` | `double` | Full 360° progress ring. |

### Structured-point series

| Method | Data type | Notes |
|---|---|---|
| `AddWaterfall(name, data, totals?, configure?)` | `double?[]` + `bool[]` | `totals[i]=true` marks absolute/total bars. |
| `AddBubble(name, data, configure?)` | `BubblePoint[]` | X, Y, and Z (size). |
| `AddHeatmap(name, data, configure?)` | `HeatmapPoint[]` | Col/row/value grid. |
| `AddColumnRange(name, data, configure?)` | `RangePoint[]` | Low–high vertical bars. |
| `AddAreaRange(name, data, configure?)` | `RangePoint[]` | Low–high filled band. |
| `AddDumbbell(name, data, configure?)` | `RangePoint[]` | Low–high dot pairs. |
| `AddErrorBar(name, data, configure?)` | `RangePoint[]` | Error whiskers. |
| `AddBoxPlot(name, data, configure?)` | `BoxPlotPoint[]` | Five-number summary boxes. |
| `AddCandlestick(name, data, configure?)` | `OhlcPoint[]` | OHLC candles (up/down colours). |
| `AddOhlc(name, data, configure?)` | `OhlcPoint[]` | OHLC bars. |
| `AddParliament(name, groups, configure?)` | `ParliamentGroup[]` | Semicircular seat layout. |
| `AddGantt(name, tasks, configure?)` | `GanttTask[]` | Horizontal task timeline. |
| `AddSankey(name, nodes, links, configure?)` | `SankeyNode[]` + `SankeyLink[]` | Node-link flow diagram. |

### Computed overlays

Derive a new series from existing data.

| Method | Notes |
|---|---|
| `AddLinearRegression(name, sourceData, configure?)` | Least-squares trend line. |
| `AddMovingAverage(name, sourceData, int period = 3, configure?)` | Simple moving average; first `period − 1` points are `null`. |
| `AddExponentialSmoothing(name, sourceData, double alpha = 0.3, configure?)` | Exponentially-smoothed series. |

---

## SeriesBuilder

`TerraFluent.Chart.Reporting.Builder.SeriesBuilder`

Passed to the `configure` lambda of every `Add*` method to style that one series.

### Appearance

| Method | Description |
|---|---|
| `Color(string color)` | Series colour (accepts a `ChartColor` constant or any CSS colour). |
| `LineWidth(int px)` | Line/spline stroke width. |
| `FillOpacity(double opacity)` | Area/column fill opacity `0.0`–`1.0`. |
| `Solid()` · `Dashed()` · `Dotted()` · `DashDotted()` · `LongDashed()` | Line dash style shortcuts. |

### Fill (gradient & pattern)

| Method | Description |
|---|---|
| `LinearGradientFill(int angleDegrees, params (double offset, string color)[] stops)` | Linear gradient fill. |
| `RadialGradientFill(params (double offset, string color)[] stops)` | Radial gradient fill. |
| `PatternFill(PatternKind pattern, string foreground, string? background = null, double size = 8)` | Hatched/dotted pattern fill. |
| `Fill(SeriesFill fill)` | Applies a fully-configured `SeriesFill`. |

### Border

| Method | Description |
|---|---|
| `Border(string color, int width = 1, int radius = 0)` | Sets border colour, width, and corner radius together. |
| `BorderColor(string color)` · `BorderWidth(int px)` · `BorderRadius(int px)` | Individual border properties. |

### Markers

| Method | Description |
|---|---|
| `MarkerSize(int radiusPx)` | Marker radius. |
| `MarkerEnabled(bool enabled = true)` | Show/hide markers. |
| `MarkerSymbol(MarkerSymbol symbol)` | `Circle`, `Square`, `Diamond`, `Triangle`, `TriangleDown`. |

### Thresholds & gaps

| Method | Description |
|---|---|
| `Zone(double? upTo, string color)` | Colours the series up to a threshold value (`null` = to infinity). |
| `Zones(params (double? upTo, string color)[] zones)` | Multiple threshold colour bands. |
| `NullGap(GapPolicy policy)` | How `null` points render: `Break`, `Connect`, or `Zero`. |
| `TargetLine(double value, string? label = null, string? color = null, string? dashStyle = null, int lineWidth = 1)` | Per-series target/reference line. |

### Visibility & legend

| Method | Description |
|---|---|
| `Visible(bool visible = true)` · `Hide()` | Show or hide the series. |
| `ShowInLegend(bool show = true)` · `HideFromLegend()` | Legend inclusion. |
| `OnYAxis(int index)` · `OnSecondaryAxis()` | Bind the series to a specific Y-axis. |

### Pie / donut, labels & centres

| Member | Description |
|---|---|
| `DonutHole(double fraction)` | Sets the donut hole size (0–1). |
| `DonutCenter` | Property → [DonutCenterOptions](#donutcenteroptions). |
| `DataLabel` | Property → [DataLabelOptions](#datalabeloptions). |
| `Label(Action<DataLabelOptions> configure)` | Configure data labels via lambda. |
| `ParliamentCenter` / `CenterLabel(Action<ParliamentCenterOptions>)` | Parliament centre caption. |
| `HeatmapRowLabels` | `List<string>` of heatmap row labels. |

### Drill-down & insights

| Method | Description |
|---|---|
| `WithDrilldown(ChartOptions childChart)` | Attach a child chart shown on click (Interactive). |
| `WithDrilldown(int dataIndex, ChartOptions childChart)` | Attach a child chart to a specific point. |
| `AutoInsight(Action<AutoInsightBuilder> configure)` | Auto-annotate peaks/troughs/trends. |

---

## LegendBuilder

`Legend(l => …)`

| Group | Methods |
|---|---|
| Visibility | `Disable()` |
| Horizontal align | `Align(string)`, `AlignLeft()`, `AlignCenter()`, `AlignRight()` |
| Vertical align | `VerticalAlign(string)`, `AtTop()`, `AtMiddle()`, `AtBottom()` |
| Position shorthands | `TopLeft()`, `TopCenter()`, `TopRight()`, `BottomLeft()`, `BottomCenter()`, `BottomRight()` |
| Offset | `Offset(int x, int y)`, `OffsetX(int)`, `OffsetY(int)` |
| Layout | `Layout(string)`, `Horizontal()`, `Vertical()`, `Padding(int)`, `Margin(int)` |
| Item text | `ItemStyle(string css)`, `ItemFontSize(int)`, `ItemFontColor(string)` |
| Symbol | `SymbolSize(int w, int h)`, `SymbolRadius(int)` |
| Border | `Border(string color, int width = 1, int radius = 0)`, `BorderColor(string)`, `BorderWidth(int)`, `BorderRadius(int)` |
| Background | `BackgroundColor(string)` |

---

## TooltipBuilder

`Tooltip(t => …)` *(active in Animated and Interactive modes)*

| Group | Methods |
|---|---|
| Visibility | `Disable()` |
| Background | `BackgroundColor(string)` |
| Border | `Border(string color, int width = 1, int radius = 4)`, `BorderColor(string)`, `BorderWidth(int)`, `BorderRadius(int)` |
| Text | `TextColor(string)`, `FontSize(int)`, `FontFamily(string?)` |
| Layout | `Padding(int)`, `HideArrow()`, `NoShadow()` |
| Content | `Format(string)`, `HeaderFormat(string)`, `PointFormat(string)` |
| Value formatting | `ValuePrefix(string)`, `ValueSuffix(string)` |
| Crosshair | `NoCrosshair()`, `CrosshairStyle(string color, int width = 1)` |
| Behaviour | `EnableShared()`, `EnableFollowPointer()`, `TransitionDuration(double seconds)` |

Format tokens: `{label}`, `{value}`, `{series}`.

---

## Credits / branding

The chart carries a fixed **`terrafluent.dev`** attribution label (bottom-right by default). Its text and link are set by the library and cannot be changed, so output cannot be re-branded. Callers control only visibility and corner:

```csharp
ChartBuilder.Create()
    .Series(s => s.AddColumn("Sales", data))
    // shown by default; move it or remove it:
    .ShowCredits(CreditsPosition.BottomLeft)
    .RenderToSvg();

// Remove branding entirely:
ChartBuilder.Create()....HideCredits().RenderToSvg();
```

Renders in **all** modes (including `Static`), is script/CSS-free, and survives export as text.

---

## AnimationBuilder

`Animation(a => …)`

| Group | Methods |
|---|---|
| Duration | `Duration(TimeSpan)`, `Duration(int milliseconds)` |
| Easing | `Linear()`, `EaseIn()`, `EaseOut()`, `EaseInOut()`, `Bounce()`, `Elastic()` |

---

## AnnotationBuilder

`Annotations(a => …)` — coordinates are in data space (category index / axis value).

| Group | Methods |
|---|---|
| Layout | `SmartLayout(Action<AnnotationLayoutOptions>? configure = null)` |
| Shapes | `Label(x, y, text, configure?)`, `Line(x1, y1, x2, y2, configure?)`, `Rect(x1, y1, x2, y2, configure?)`, `Circle(x, y, radiusPx, configure?)` |
| Placement helpers | `LabelAbove(x, y, text, offsetPx = 18, configure?)`, `LabelBelow(…)`, `LabelLeft(x, y, text, offsetPx = 8, configure?)`, `LabelRight(…)` |
| Raw | `Add(Annotation annotation)` |

---

## LabelLayoutBuilder

`LabelLayout(l => …)` — controls dense X-axis category labels.

| Group | Methods |
|---|---|
| Rotation | `Rotation(int degrees)`, `AutoRotate(int maxDegrees = 90)`, `NoRotation()` |
| Wrapping | `Wrap(int maxCharsPerLine = 12)`, `NoWrap()` |
| Skipping | `Skip(int n)`, `AutoSkip(bool enabled = true)` |
| Font scaling | `FontSizeRange(int min, int max)`, `NoAutoScale()` |
| Collision | `EnableCollisionDetection()`, `DisableCollisionDetection()` |
| Positioning | `Stagger(int offsetPx = 10)`, `NoStagger()`, `HorizontalPadding(int px)` |

---

## DataLabelOptions

Accessed via `cfg.DataLabel` or `cfg.Label(dl => …)`.

| Method | Description |
|---|---|
| `Show()` / `Hide()` | Enable/disable labels. |
| `Format(string fmt)` | Format string, e.g. `"{value}%"`, `"${value}k"`. |
| `Color(string color)` | Text colour. |
| `FontSize(int px)` | Text size. |
| `Background(string color)` | Pill background colour. |
| `Radius(double fraction)` | Pie/donut: label radius (`> 1` places labels outside with a connector). |
| `OffsetY(double pixels)` | Vertical nudge. |

---

## DonutCenterOptions

Accessed via `cfg.DonutCenter` on pie/donut and data-ring series.

| Method | Description |
|---|---|
| `Show()` / `Show(string customText)` / `Hide()` | Toggle the centre label. |
| `Title(string title)` | Caption shown above the value. |
| `Text(string text)` | Custom centre text (overrides the computed total). |
| `FontSize(int px)` / `TitleFontSize(int px)` | Value / caption sizes. |
| `Color(string color)` / `TitleColor(string color)` | Value / caption colours. |

---

## Enums

`TerraFluent.Chart.Reporting.Enums`

### ChartType
`Line`, `Spline`, `Area`, `Column`, `Bar`, `Pie`, `Scatter`, `Waterfall`, `Gauge`, `DataRing`, `Bubble`, `Heatmap`, `ColumnRange`, `AreaRange`, `Funnel`, `Treemap`, `Radar`, `BoxPlot`, `ErrorBar`, `Candlestick`, `Ohlc`, `Dumbbell`, `Stream`, `Gantt`, `Sankey`, `Parliament`.

### SvgMode
`Static` · `Animated` · `Interactive`.

### Easing
`Linear` · `EaseIn` · `EaseOut` · `EaseInOut` · `Bounce` · `Elastic`.

### MarkerSymbol
`Circle` · `Square` · `Diamond` · `Triangle` · `TriangleDown`.

### CreditsPosition
`BottomRight` (default) · `BottomLeft` · `TopRight` · `TopLeft`.

### AxisType
`Linear` · `Logarithmic` · `DateTime`.

### Stacking
`None` (side-by-side) · `Normal` (cumulative) · `Percent` (100 %).

### GapPolicy
`Break` (visible gaps, default) · `Connect` (skip nulls) · `Zero` (plot nulls at zero).

### WarningSeverity
`Info = 0` · `Warning = 1` · `Error = 2`.

---

## ChartTheme

`TerraFluent.Chart.Reporting.Models.ChartTheme`

### Built-in presets (`static readonly ChartTheme`)

| Preset | Style |
|---|---|
| `Default` | Blue-orange on white. |
| `Dark` | Bright palette on midnight navy. |
| `Pastel` | Soft palette on off-white. |
| `Monochrome` | Greyscale on white. |
| `Ocean` | Blue-teal on deep ocean. |
| `Sunset` | Warm palette on purple-navy. |
| `Forest` | Earthy greens on cream. |
| `Neon` | Electric palette on near-black. |
| `Minimal` | Muted Tableau-10 on white. |
| `Warm` | Earth tones on parchment. |
| `Arctic` | Cool blues on ice-blue. |
| `Business` | Corporate blue-red on white. |
| `Material` | Material Design 500 palette. |
| `TrafficLight` | Green/amber/red status palette. |
| `Accessible` | Colour-blind-safe (Wong 2011). |
| `Vivid` | Full-spectrum distinct palette. |
| `HighContrast` | WCAG AA (≥ 4.5:1) palette. |

### Properties

`BackgroundColor`, `PlotBackgroundColor`, `GridLineColor`, `AxisLineColor`, `TextColor`, `FontFamily`, `FontScale` (default `1.0`), `Colors` (`string[]`), `TooltipBackground`, `TooltipTextColor`, `PositiveColor`, `NegativeColor`, `AccentColor`.

### Factory

```csharp
ChartTheme.Custom(
    string? backgroundColor = null, string? plotBackgroundColor = null,
    string? gridLineColor = null,   string? axisLineColor = null,
    string? textColor = null,       string? fontFamily = null,
    string[]? colors = null,        string? tooltipBackground = null,
    string? tooltipTextColor = null,string? positiveColor = null,
    string? negativeColor = null,   string? accentColor = null);

ChartTheme Clone();   // copy an existing theme (e.g. to tweak FontScale)
```

---

## ChartColor

`TerraFluent.Chart.Reporting.Models.ChartColor`

130+ named `const string` colour constants plus curated palettes and utilities.

### Series palette constants

| Constant | Hex | Slot |
|---|---|---|
| `ChartBlue` | `#7CB5EC` | 1 |
| `ChartOrange` | `#F7A35C` | 2 |
| `ChartGreen` | `#90ED7D` | 3 |
| `ChartYellow` | `#E4D354` | 4 |
| `ChartIndigo` | `#8085E9` | 5 |
| `ChartRose` | `#F15C80` | 6 |
| `ChartTeal` | `#2B908F` | 7 |
| `ChartRed` | `#F45B5B` | 8 |

Also includes all standard CSS named colours (`Red`, `SteelBlue`, `ForestGreen`, …), pastel constants (`PastelSkyBlue`, …), and theme-specific tokens.

### Palette arrays (`ChartColor.Palette.*`)

`Default`, `Dark`, `Pastel`, `Monochrome`, `Ocean`, `Sunset`, `Forest`, `Neon`, `Minimal`, `Warm`, `Arctic`, `Business`, `Material`, `TrafficLight`, `Accessible`, `Vivid` (each 20 colours), and `HighContrast` (16 colours).

### Utility methods

| Method | Returns | Description |
|---|---|---|
| `FromRgb(int r, int g, int b)` | `string` | Hex from RGB components. |
| `WithOpacity(string hex, double alpha)` | `string` | `rgba(…)` with the given alpha. |
| `Lighten(string hex, double amount = 0.3)` | `string` | Lighter tint. |
| `Darken(string hex, double amount = 0.3)` | `string` | Darker shade. |
| `Mix(string hexA, string hexB, double weight = 0.5)` | `string` | Blend of two colours. |

---

## Data-point types

`TerraFluent.Chart.Reporting.Models` — inputs for structured series.

| Type | Constructor | Purpose |
|---|---|---|
| `BubblePoint` | `(double x, double y, double z)` | Bubble: position + size. |
| `HeatmapPoint` | `(int col, int row, double value)` | Heatmap cell. |
| `RangePoint` | `(double low, double high)` | ColumnRange, AreaRange, Dumbbell, ErrorBar. |
| `BoxPlotPoint` | `(double low, double q1, double median, double q3, double high)` | Box-and-whisker summary. |
| `OhlcPoint` | `(double open, double high, double low, double close)` | Candlestick / OHLC. |
| `GanttTask` | `{ Name, Start, End, Color?, Label? }` | Gantt row. |
| `ParliamentGroup` | `(string name, string color, int seats)` | Parliament seat block. |
| `SankeyNode` | `{ Name, Color? }` | Sankey node. |
| `SankeyLink` | `{ From, To, Value, Color? }` | Sankey flow (indices into the node list). |

---

## IChartBuilder

`TerraFluent.Chart.Reporting.Builder.IChartBuilder`

An interface mirroring the full `ChartBuilder` surface. Depend on `IChartBuilder` in your services for testable, decoupled code, and register the concrete builder in DI:

```csharp
services.AddScoped<IChartBuilder>(_ => ChartBuilder.Create());
```

All chaining, configuration, and render methods listed above are available on the interface. The async render methods (`RenderToStreamAsync`, `RenderToFileAsync`, `RenderToHtmlFileAsync`) are present only on `net6.0` and newer targets.

---

See also: [Getting Started](getting-started.md) · [Chart Types](chart-types.md) · [Themes & Styling](themes-and-styling.md) · [Advanced Features](advanced.md) · [Troubleshooting & FAQ](troubleshooting.md)
