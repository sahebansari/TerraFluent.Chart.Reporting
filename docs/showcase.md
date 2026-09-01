# Chart Showcase

A live visual catalogue of every chart type and feature in TerraFluent.Chart.Reporting — each entry is a self-contained SVG rendered entirely server-side, with zero JavaScript dependency.

> **[▶ Open the interactive showcase (showcase.html)](showcase.html)** — a single browsable page with all charts rendered inline. Best viewed in a browser.

For copy-paste code behind each chart type, see **[Chart Types](chart-types.md)**.

_Catalogue of 102 charts · generated 2026-09-01 UTC._

| # | Chart | What it demonstrates |
|---|---|---|
| 01 | Line Chart | Single-series animated line chart with monthly visitors data. |
| 02 | Multi-Series Line | Three lines (Revenue, Cost, Profit). |
| 03 | Area Chart | Filled area chart showing daily active users over 10 days. |
| 04 | Column Chart | Single-series column chart — product sales by category. |
| 05 | Grouped Columns | Two column series side-by-side for Budget vs Actual spend. |
| 06 | Pie Chart | Pie chart with legend showing market share by vendor. |
| 07 | Spline Chart | Smooth spline curves — temperature trends for three cities. |
| 08 | Mixed Line + Area | Area series (Volume) combined with a Line series (Price). |
| 09 | Static Mode (PDF / Email) | SvgMode.Static — no CSS hover rules, no embedded JS. |
| 10 | Interactive Mode (Browser) | SvgMode.Interactive — CSS hover tooltips + embedded JS. |
| 11 | True Spline (Bézier) | Catmull-Rom cubic Bézier curves. |
| 12 | Horizontal Bar Chart | ChartType.Bar — bars grow left-to-right. |
| 13 | Scatter Chart | ChartType.Scatter — dots only, no connecting line. |
| 14 | Animated Column (SMIL) | Bars grow from baseline on load via SMIL. |
| 15 | Dark Theme | ChartTheme.Dark — navy background, light text. |
| 16 | Pastel Theme | ChartTheme.Pastel — soft off-white background. |
| 17 | Data Labels | ShowDataLabels — values rendered on bars and line points. |
| 18 | Donut Chart | Pie chart with DonutHolePercent = 0.55 creating a donut hole. |
| 19 | Stacked Columns (Normal) | Stacking.Normal — bars are stacked so totals are visible. |
| 20 | Stacked Columns (100 %) | Stacking.Percent — each column normalised to 100 %. |
| 21 | Stacked Area | Stacking.Normal on Area series. |
| 22 | Secondary Y-Axis | WithYAxis2() — columns on left axis, line on right axis. |
| 23 | Plot Bands & Reference Lines | YAxis.PlotBands and YAxis.PlotLines. |
| 24 | Waterfall Chart | ChartType.Waterfall — incremental running-total chart. |
| 25 | Gauge / Radial Chart | ChartType.Gauge — semi-circular dial. |
| 26 | X-Axis Label Rotation | Axis.LabelRotation = -45 — diagonal labels. |
| 27 | Data Label Showcase | DataLabel: FontSize · Format — three distinct styles on one chart. |
| 28 | Pie — Label Placement | DataLabelRadius: inside, at edge, and outside with connector. |
| 29 | Legend — Vertical Right | LegendBuilder: Vertical() · AlignRight() · SymbolRadius. |
| 30 | Legend — Top Center | LegendBuilder: AtTop() · AlignCenter() · Horizontal() · Padding. |
| 31 | Legend — Bottom Left | LegendBuilder: AtBottom() · AlignLeft() · SymbolSize · Offset. |
| 32 | Column — Border & Corner Radius | Series.BorderWidth · BorderRadius per column series. |
| 33 | Scatter — Marker Borders | Series.BorderWidth on scatter dot markers. |
| 34 | Area — Fill Opacity | Series.FillOpacity: 0.15 · 0.40 · 0.70 — three overlapping area series. |
| 35 | Tooltip — Custom Style | TooltipBuilder: FontSize · Padding · Format · TransitionDuration. |
| 36 | Tooltip — No Arrow | TooltipBuilder: HideArrow() · custom Format template. |
| 37 | Donut — Center Label | DonutCenter: auto total · title caption · font size. |
| 38 | Monthly Sales — USD Donut | Donut chart with USD data labels and DonutCenter total. |
| 39 | Data Ring — KPI | ChartType.DataRing — full 360° progress ring. |
| 40 | Data Ring — Dashboard Trio | Three DataRing charts side-by-side as a KPI dashboard. |
| 41 | Fluent API Showcase | Size · AsInteractive · Animate · StackNormal · YAxisFormat · ShowDataLabels · SeriesBuilder. |
| 42 | Fixed Width Chart | Width(600) — pins the SVG to exactly 600 px wide. |
| 43 | Responsive Width Chart | ResponsiveWidth() — SVG fills its container. |
| 44 | X-Axis Numeric Tick Interval | XAxisTickInterval — numeric X-axis with explicit tick spacing. |
| 45 | Fork — Chart Variants | Fork() — produce a Static and an Animated variant. |
| 46 | Bubble Chart | AddBubble() — scatter with a third dimension (Z). |
| 47 | Heatmap | AddHeatmap() — colour-coded matrix. |
| 48 | Column Range | AddColumnRange() — each category spans from low to high. |
| 49 | Area Range | AddAreaRange() — filled band between a lower and upper line. |
| 50 | Funnel Chart | AddFunnel() — stacked trapezoid stages. |
| 51 | Treemap | AddTreemap() — nested rectangles sized proportionally. |
| 52 | Data Point Click — Line Chart | OnPointClick() — click any data point to see its details. |
| 53 | Data Point Click — Column Chart | OnPointClick() — two column series; click any bar. |
| 54 | Data Point Click — Shared Tooltip | OnPointClick() combined with shared tooltip. |
| 55 | Legend Series Toggle | SvgMode.Interactive — click any legend item to hide/show that series. |
| 56 | SVG Export Button | ShowExportButton() — download button appears top-right. |
| 57 | Multi-Format Export Menu | ShowExportMenu() — SVG, PNG, JPEG, and PDF options. |
| 58 | AutoInsight — Line Chart | AnomalyBands · TrendLine · MovingAverage · HighlightPeaks · NarrativeSummary. |
| 59 | AutoInsight — Area Chart | AutoInsight on an Area series — pure SVG, no JavaScript. |
| 60 | Parliament Chart | AddParliament() — semicircular hemicycle seating diagram. |
| 61 | Logarithmic Y Axis | YAxisLogarithmic() — base-10 log scale. |
| 62 | Date/Time X Axis | XAxisDateTime() — auto-formatted by span. |
| 63 | Radar / Spider Chart | AddRadar() — closed polygon, great for multivariate comparisons. |
| 64 | Box-and-Whisker Plot | AddBoxPlot() — five-number summary per category. |
| 65 | Column + Error Bars | AddErrorBar() — I-beam uncertainty whiskers. |
| 66 | Gradient-Filled Area | LinearGradientFill() — vertical gradient via SVG <linearGradient>. |
| 67 | Pattern-Filled Columns | PatternFill() — diagonal lines, dots, grid via SVG <pattern>. |
| 68 | Marker Symbol Shapes | MarkerSymbol() — circle, square, diamond, and triangle. |
| 69 | Threshold Zones | Zones() — recolour a line by value band. |
| 70 | Candlestick | AddCandlestick() — OHLC bodies with wicks. |
| 71 | OHLC Bars | AddOhlc() — high-low bar with open/close ticks. |
| 72 | Annotations | Annotations() — labels, lines, rectangles and circles. |
| 73 | Vivid Theme | ChartTheme.Vivid — full-spectrum palette. |
| 74 | Label Layout Builder | LabelLayout() — rotation, wrap, stagger, font scaling, collision detection. |
| 75 | High-Contrast Theme | ChartTheme.HighContrast — WCAG AA palette, all colours ≥ 4.5:1 on white. |
| 76 | Null-Gap Policy | NullGap() — Break (gap), Connect (bridge), Zero (baseline) for missing values. |
| 77 | Per-Series Target Lines | TargetLine() — horizontal reference lines scoped to a single series. |
| 78 | RenderToDataUri() | Embeds the chart as a Base64 data: URI inside an HTML img tag. |
| 79 | 100% Stacked Columns | StackPercent() — Y-axis automatically labels 0% … 100%. |
| 80 | Dual-Axis Combo Chart | Column (primary Y) + Line (secondary Y) on one chart. |
| 81 | Mixed-Type Combo Chart | Column + Line + Area series rendered together. |
| 82 | Inverted Y-Axis | YAxisInverted() — minimum at the top, ranking style. |
| 83 | Linear Regression Overlay | AddLinearRegression() — least-squares trend line over raw data. |
| 84 | Moving Average Overlay | AddMovingAverage() — simple 3-period moving average over a line series. |
| 85 | Exponential Smoothing Overlay | AddExponentialSmoothing() — EMA overlay (α = 0.4) for noisy data. |
| 86 | Data Table Toggle | ShowDataTable() — appends a per-category value grid below the chart. |
| 87 | Custom ARIA Labels | AriaLabel() + AriaDescription() — custom accessible title and description. |
| 88 | German Culture (de-DE) | Culture("de-DE") — number formatting uses comma decimal separator. |
| 89 | Right-to-Left Chart | RightToLeft() — dir="rtl" on SVG root, direction:rtl CSS on all text. |
| 90 | Template: Revenue | ChartTemplate.Revenue — column chart with N0-formatted Y-axis. |
| 91 | Template: KPI Dashboard | ChartTemplate.KpiDashboard — dark theme, interactive, no grid/legend. |
| 92 | Template: Time Series | ChartTemplate.TimeSeries — spline, animated, 1 s entry. |
| 93 | Template: Executive Summary | ChartTemplate.ExecutiveSummary — pastel bar chart, static/PDF-safe. |
| 94 | JSON Round-Trip | ChartOptions.ToJson() / ChartBuilder.FromJson() — persist and restore a chart. |
| 95 | Dumbbell / Dot-Plot | AddDumbbell() — two dots per category connected by a vertical line. |
| 96 | Stream Graph (ThemeRiver) | AddStream() — stacked areas with a centered wiggle baseline. |
| 97 | Gantt / Timeline | AddGantt() — horizontal task bars on a numeric time axis. |
| 98 | Sankey Flow Diagram | AddSankey() — node-link flow diagram with cubic-bezier links. |
| 99 | Range Selector (Navigator) | RangeSelector() — interactive brush strip below the chart fires tf:rangechange. |
| 100 | Synchronized Tooltips | SyncGroup() — two charts share the same hover group so tooltips mirror each other. |
| 101 | Drill-Down Chart | WithDrilldown() — click a column to open a child detail chart; Back returns to overview. |
| 102 | Grid Lines Toggle | GridLines(false) / HideGridLines() — show or hide the background plot grid. |

