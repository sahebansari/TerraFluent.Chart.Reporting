# API cheatsheet (verified against source, v2.0.0)

Every method below exists. **If you need something that isn't here, it probably doesn't exist.** Check `ChartBuilder.GetOptions()` (the raw `ChartOptions` model) before inventing an API.

Namespaces: `TerraFluent.Chart.Reporting.Builder` · `.Models` · `.Enums`

## ChartBuilder (`ChartBuilder.Create()`)

All methods return `ChartBuilder` unless stated otherwise.

```text
// Render mode
AsStatic()  AsAnimated()  AsInteractive()

// Default type for generic s.Add(...) — needed for pie: .AsPie()
AsPie() AsLine() AsArea() AsColumn() AsBar() AsSpline() AsScatter() AsWaterfall() AsGauge()
AsDataRing() AsBubble() AsHeatmap() AsColumnRange() AsAreaRange() AsFunnel() AsTreemap()
AsDumbbell() AsStream() AsGantt() AsSankey()

// Size
Size(int width, int height)   Width(int? width)   Height(int height)
ResponsiveWidth()             Responsive(bool = true)          Background(string color)

// Look
Theme(ChartTheme theme)       Colors(params string[] colors)   Colors(IEnumerable<string>)
StackNormal()                 StackPercent()
ApplyTemplate(IChartTemplate) // ChartTemplate.Revenue | KpiDashboard | TimeSeries | ExecutiveSummary | Accessible

// Titles
Title(string)   Title(Action<ChartTitle>)      // ChartTitle { Text, Align = "center", Style }
Subtitle(string) Subtitle(Action<ChartTitle>)

// Axes
Labels(params string[] labels)                 // pie slice names / X categories
XAxis(string title, params string[] categories)
XAxis(Action<Axis>)                            // Axis { Title, Categories(List), Min, Max, LabelFormat, TickInterval,
                                               //        Visible, GridLineVisible, GridLineColor, LabelRotation, Inverted, Type }
XAxisFormat(string fmt)                        // "{value} kg"
XAxisTickInterval(double interval)
XAxisDateTime(IEnumerable<DateTime> values, string? format = null)
YAxis(string title, double? min = null, double? max = null)
YAxis(Action<Axis>)
YAxisFormat(string fmt)   YAxisTickInterval(double)   YAxisLogarithmic()   YAxisInverted(bool = true)
YAxis2(Action<Axis>)      YAxis2Logarithmic()         YAxis2Inverted(bool = true)
GridLines(bool = true)    HideGridLines()

// Reference marks (primary Y axis)
PlotBand(double from, double to, string color = "rgba(68,170,213,0.15)", string? label = null)
PlotLine(double value, string color = ChartColor.Red, int width = 1, string? label = null, string? dashStyle = null)
                                               // dashStyle: "Dash" | "Dot" | "DashDot" | "LongDash" | "Solid"

// Legend / tooltip / credits
Legend(Action<LegendBuilder>)   HideLegend()
Tooltip(Action<TooltipBuilder>) DisableTooltip()
ShowCredits()  ShowCredits(CreditsPosition)  HideCredits()

// Animation (Animated/Interactive)
Animate(int ms = 800)   Animation(Action<AnimationBuilder>)   DisableAnimation()

// Series
Series(Action<ChartSeriesBuilder>)   ClearSeries()   RemoveSeries(string name)   ShowDataLabels()

// Interactive-only
ShowExportButton(string label = "⬇ SVG")   ShowExportMenu(params string[] formats) // "SVG","PNG","JPEG","PDF"
OnPointClick(string jsHandler)             RangeSelector(Action<RangeSelectorOptions>)   SyncGroup(string id)

// Layout / annotations
LabelLayout(Action<LabelLayoutBuilder>)    Annotations(Action<AnnotationBuilder>)
ShowDataTable(int rowHeight = 20, int fontSize = 10)

// Accessibility & locale
AriaLabel(string)   AriaDescription(string)   Culture(string name) / Culture(CultureInfo)   RightToLeft(bool = true)

// Data quality
ThrowOnDataQualityErrors()
AnalyzeDataQuality()        → DataQualityReport { IReadOnlyList<DataQualityWarning> Warnings; bool HasErrors }
GetLastDataQualityReport()  → DataQualityReport?     // DataQualityWarning { Severity, Message }

// Output
RenderToSvg()                → string
RenderToHtml(string? caption = null, string? cssClass = null) → string   // <figure> fragment
RenderToBytes()              → byte[]  (UTF-8)
RenderToDataUri()            → string  ("data:image/svg+xml;base64,…" — use in <img src> / email)
RenderToStream(Stream)       RenderToFile(string path)     RenderToHtmlFile(string path, caption?, cssClass?)
RenderToFileAsync / RenderToStreamAsync / RenderToHtmlFileAsync (…, CancellationToken)   // net6.0+ only

// Variants & model
Fork() / Clone() → ChartBuilder (deep copy)   GetOptions() → live ChartOptions   Build() / GetSnapshot() → ChartOptions copy
ChartBuilder.FromJson(string json)           // net6.0+ only
```

## ChartSeriesBuilder (`.Series(s => s …)`)

Every `Add*` returns the series builder, so you can chain calls. The last parameter is always `Action<SeriesBuilder>? configure = null`.

```text
// double[] or double?[]  (null = gap)
AddLine  AddSpline  AddArea  AddColumn  AddBar  AddScatter  AddRadar  AddFunnel  AddTreemap  AddStream  AddPie
    (string name, IEnumerable<double> data, configure?)
Add(string name, data, configure?)            // uses the AsX() default type

AddGauge(string name, double value, configure?)
AddDataRing(string name, double value, configure?)
AddWaterfall(string name, IEnumerable<double?> data, IEnumerable<bool>? totals = null, configure?)
                                             // totals.Length must equal data.Length (else ArgumentException)
AddBubble(name, BubblePoint[])               // new BubblePoint(x, y, z)
AddHeatmap(name, HeatmapPoint[])             // new HeatmapPoint(col, row, value); rows: cfg.HeatmapRowLabels.AddRange(...)
AddColumnRange / AddAreaRange / AddDumbbell / AddErrorBar (name, RangePoint[])   // new RangePoint(low, high)
AddBoxPlot(name, BoxPlotPoint[])             // new BoxPlotPoint(low, q1, median, q3, high)
AddCandlestick / AddOhlc (name, OhlcPoint[]) // new OhlcPoint(open, high, low, close)
AddParliament(name, ParliamentGroup[])       // new ParliamentGroup(name, color, seats)
AddGantt(name, GanttTask[])                  // new GanttTask { Name, Start, End, Color = null, Label = null }
AddSankey(name, SankeyNode[] nodes, SankeyLink[] links)
                                             // new SankeyNode { Name } ; new SankeyLink { From = 0, To = 1, Value = 100 }

// Computed overlays
AddLinearRegression(name, sourceData, configure?)
AddMovingAverage(name, sourceData, int period = 3, configure?)
AddExponentialSmoothing(name, sourceData, double alpha = 0.3, configure?)
```

## SeriesBuilder (`cfg => cfg …`)

Returns `SeriesBuilder` unless stated otherwise.

```text
Color(string)  LineWidth(int)  FillOpacity(double 0..1)
Solid() Dashed() Dotted() DashDotted() LongDashed()
LinearGradientFill(int angleDegrees, params (double offset, string color)[] stops)
RadialGradientFill(params (double offset, string color)[] stops)
PatternFill(PatternKind, string foreground, string? background = null, double size = 8)
      // PatternKind: Dots | DiagonalLines | HorizontalLines | VerticalLines | Grid | CrossHatch  (in .Models)
Border(string color, int width = 1, int radius = 0)  BorderColor(string)  BorderWidth(int)  BorderRadius(int)
MarkerSize(int)  MarkerEnabled(bool = true)  MarkerSymbol(MarkerSymbol)   // Circle|Square|Diamond|Triangle|TriangleDown
Zone(double? upTo, string color)   Zones(params (double? upTo, string color)[])
NullGap(GapPolicy)                 // Break | Connect | Zero
TargetLine(double value, string? label = null, string? color = null, string? dashStyle = null, int lineWidth = 1)
Visible(bool = true)  Hide()  ShowInLegend(bool = true)  HideFromLegend()
OnYAxis(int index)  OnSecondaryAxis()
DonutHole(double fraction 0..1)                       // e.g. 0.55  (NOT DonutHolePercent)
DonutCenter           → DonutCenterOptions property   // cfg.DonutCenter.Show().Title("Total")
Center(Action<DonutCenterOptions>)                    // lambda form
DataLabel             → DataLabelOptions property     // cfg.DataLabel.Show().Format("{value}%")
Label(Action<DataLabelOptions>)                       // lambda form, returns SeriesBuilder
ParliamentCenter / CenterLabel(Action<ParliamentCenterOptions>)
HeatmapRowLabels      → List<string>
AutoInsight(Action<AutoInsightBuilder>)               // TrendLine(color?) MovingAverage(period=3,color?)
                                                      // AnomalyBands(sigma=1.5,bandColor?) HighlightPeaks() NarrativeSummary()
WithDrilldown(ChartOptions child)  WithDrilldown(int dataIndex, ChartOptions child)   // Interactive; child = otherBuilder.Build()
```

`DataLabelOptions`: `Show() Hide() Format(string) Color(string) FontSize(int) Background(string) Radius(double) OffsetY(double)`. `Radius > 1` puts pie labels outside the slice, with a connector line.
`DonutCenterOptions`: `Show() Show(string text) Hide() Title(string) Text(string) FontSize(int) TitleFontSize(int) Color(string) TitleColor(string)`

## Sub-builders

- **LegendBuilder**: `Disable()`, `TopLeft() TopCenter() TopRight() BottomLeft() BottomCenter() BottomRight()`, `AlignLeft/Center/Right()`, `AtTop/AtMiddle/AtBottom()`, `Horizontal() Vertical()`, `Offset(x,y)`, `Padding(int) Margin(int)`, `ItemFontSize(int) ItemFontColor(string) ItemStyle(string css)`, `SymbolSize(w,h) SymbolRadius(int)`, `Border(color,width=1,radius=0)`, `BackgroundColor(string)`
- **TooltipBuilder**: `Disable()`, `Format / HeaderFormat / PointFormat(string)` (tokens `{label}` `{value}` `{series}`), `ValuePrefix(string) ValueSuffix(string)`, `BackgroundColor TextColor FontSize FontFamily`, `Border(color,width=1,radius=4)`, `Padding HideArrow NoShadow`, `NoCrosshair() CrosshairStyle(color,width=1)`, `EnableShared() EnableFollowPointer() TransitionDuration(double s)`
- **AnimationBuilder**: `Duration(int ms | TimeSpan)`, `Linear() EaseIn() EaseOut() EaseInOut() Bounce() Elastic()`
- **LabelLayoutBuilder**: `Rotation(int) AutoRotate(int max = 90) NoRotation()`, `Wrap(int maxChars = 12) NoWrap()`, `Skip(int n) AutoSkip(bool = true)`, `FontSizeRange(min,max) NoAutoScale()`, `EnableCollisionDetection() DisableCollisionDetection()`, `Stagger(int px = 10) NoStagger() HorizontalPadding(int)`
- **AnnotationBuilder** (data-space coordinates: category index, axis value): `Label(x,y,text)`, `LabelAbove/LabelBelow(x,y,text,offsetPx=18)`, `LabelLeft/LabelRight(x,y,text,offsetPx=8)`, `Line(x1,y1,x2,y2)`, `Rect(x1,y1,x2,y2)`, `Circle(x,y,radiusPx)`, `SmartLayout()`. Each takes an optional `configure` lambda.

## Themes & colours

- `ChartTheme.{Default, Dark, Pastel, Monochrome, Ocean, Sunset, Forest, Neon, Minimal, Warm, Arctic, Business, Material, TrafficLight, Accessible, Vivid, HighContrast, Modern}`
- `ChartTheme.Custom(backgroundColor?, plotBackgroundColor?, gridLineColor?, axisLineColor?, textColor?, fontFamily?, colors?, tooltipBackground?, tooltipTextColor?, positiveColor?, negativeColor?, accentColor?)`. All parameters are named and optional. `colors` must not be empty.
- `theme.Clone()` makes a copy you can adjust through settable properties: `FontScale`, `ModernStyle` (`false` gives the classic flat look), `FontFamily`, `Colors`, and others.
- `ChartColor.ChartBlue/ChartOrange/ChartGreen/ChartYellow/ChartIndigo/ChartRose/ChartTeal/ChartRed`, `ChartColor.Charcoal`, all CSS named colours (`ChartColor.SteelBlue`, …), and `ChartColor.Palette.<ThemeName>` (`string[]`).
- Colour helpers: `ChartColor.WithOpacity(hex, 0.3)`, `Lighten(hex, 0.3)`, `Darken(hex, 0.3)`, `Mix(a, b, 0.5)`, `FromRgb(r, g, b)`

## Validation exceptions

`Size(0, …)` → `ArgumentOutOfRangeException` · `YAxis(min ≥ max)` → `ArgumentException` · `FillOpacity(>1)`, `MarkerSize(0)`, `Animate(0)` → `ArgumentOutOfRangeException` · `XAxisFormat(" ")` → `ArgumentException` · `ChartTheme.Custom(colors: [])` → `ArgumentException` · waterfall totals length mismatch → `ArgumentException` · `RenderToFile` into a missing folder → `DirectoryNotFoundException` · `ThrowOnDataQualityErrors()` + bad data → `DataQualityException`
