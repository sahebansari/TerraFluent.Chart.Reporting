# Chart selection: intent → chart type

Match the user's words and the **shape of their data** to a series method. When two rows fit, prefer the simpler chart.

## By prompt wording

| User says… | Use | Data shape |
|---|---|---|
| "trend", "over time", "monthly", "growth", "history" | `AddLine` (`AddSpline` for continuous signals such as temperature or sensor data) | `double[]`, one per X category |
| "volume", "cumulative", "total over time", "contribution over time" | `AddArea` (+ `.StackNormal()` for several series) | `double[]` |
| "compare", "by region/product/department", "vs" | `AddColumn` (grouped: several `AddColumn` calls) | `double[]` |
| "ranking", "top 10", "leaderboard", long category names | `AddBar` (horizontal) | `double[]` |
| "share", "breakdown", "split", "percentage of", "composition" (≤ 6 parts) | `.AsPie()` + `AddPie` (donut: `cfg.DonutHole(0.55)`) | `double[]` + `.Labels(...)` |
| "breakdown" with many parts, "allocation", "portfolio", "disk usage" | `AddTreemap` | `double[]` + X categories as names |
| "mix over time", "100 %" | `AddColumn` × N + `.StackPercent()` | `double[]` per series |
| "correlation", "relationship", "vs" of two measures | `AddBubble` for true numeric X; `AddScatter` is category-indexed only | `BubblePoint(x, y, z)` |
| "three metrics", "size = …" | `AddBubble` | `BubblePoint(x, y, z)` |
| "matrix", "by day and hour", "grid", "intensity" | `AddHeatmap` | `HeatmapPoint(col, row, value)` + `cfg.HeatmapRowLabels` |
| "bridge", "P&L", "from A to B", "what drove the change" | `AddWaterfall` | `double?[]` deltas + `bool[] totals` |
| "KPI", "utilisation", "score", "% complete" (one number) | `AddGauge` (half dial) / `AddDataRing` (full ring) | single `double`; scale via `YAxis(y => { y.Min = 0; y.Max = 100; })` |
| "pipeline", "conversion", "drop-off" (ordered stages) | `AddFunnel` | `double[]` (descending) + X categories as stage names |
| "flow", "from → to", "where users go", "energy/budget flow" | `AddSankey` | `SankeyNode[]` + `SankeyLink{From,To,Value}` (indices) |
| "skills", "profile", "multi-attribute comparison" | `AddRadar` | `double[]` per entity; X categories = axes |
| "distribution", "spread", "quartiles", "latency percentiles" | `AddBoxPlot` | `BoxPlotPoint(low, q1, median, q3, high)` |
| "min/max range", "temperature range", "low–high" | `AddColumnRange` | `RangePoint(low, high)` |
| "confidence interval", "forecast band" | `AddAreaRange` (+ `AddLine` for the mean) | `RangePoint(low, high)` |
| "± error", "uncertainty", "std dev" | `AddErrorBar` overlaid on `AddLine`/`AddColumn` | `RangePoint(low, high)` |
| "before/after", "change per item", "gap" | `AddDumbbell` | `RangePoint(before, after)` |
| "stock", "price", "OHLC", "candles" | `AddCandlestick` (or `AddOhlc` for bar style) | `OhlcPoint(open, high, low, close)` |
| "timeline", "schedule", "project plan", "roadmap" | `AddGantt` | `GanttTask { Name, Start, End, Color?, Label? }` (numeric Start/End) |
| "genre/topic popularity over time", "theme river" | `AddStream` | `double[]` per stream |
| "seats", "election", "assembly composition" | `AddParliament` | `ParliamentGroup(name, color, seats)` |

## Enhancements the prompt may ask for

| User says… | Add |
|---|---|
| "trend line", "regression" | `s.AddLinearRegression("Trend", data)` or `cfg.AutoInsight(ai => ai.TrendLine())` |
| "moving average", "smoothed" | `s.AddMovingAverage("3-mo avg", data, period: 3)` |
| "highlight peak/low", "call out max" | `cfg.AutoInsight(ai => ai.HighlightPeaks())` |
| "anomalies", "outliers" | `cfg.AutoInsight(ai => ai.AnomalyBands(1.5).NarrativeSummary())` |
| "target", "goal", "threshold", "budget line" | `.PlotLine(value, ChartColor.Red, 2, "Target", "Dash")` or `cfg.TargetLine(value, "Target")` |
| "acceptable range", "safe zone" | `.PlotBand(from, to, "rgba(46,204,113,0.15)", "Normal")` |
| "red when below X" | `cfg.Zones((X, ChartColor.Red), (null, ChartColor.ChartGreen))` |
| "show values", "labels on bars" | `cfg.DataLabel.Show().Format("{value}")` or `.ShowDataLabels()` for all series |
| "two units", "revenue and margin %" | line `cfg.OnSecondaryAxis()` + `.YAxis2(y => { y.Title = "Margin (%)"; })` |
| "log scale" | `.YAxisLogarithmic()` |
| "table under the chart" | `.ShowDataTable()` |
| "dark mode" | `.Theme(ChartTheme.Dark)` |
| "colour-blind friendly", "accessible" | `.Theme(ChartTheme.Accessible)` or `ChartTheme.HighContrast`, plus `.AriaLabel(...)`/`.AriaDescription(...)` |
| "print", "black and white" | `.Theme(ChartTheme.Monochrome).AsStatic()` (patterns: `cfg.PatternFill(PatternKind.DiagonalLines, "#000")`) |
| "brand colours" | `.Colors("#hex1", "#hex2", …)` or `ChartTheme.Custom(colors: new[] { … })` |
| "German/French number format" | `.Culture("de-DE")` |
| "Arabic/Hebrew" | `.RightToLeft()` + `.Culture("ar-SA")` |
| "dates on the X axis" | `.XAxisDateTime(dates, "MMM yy")` |

## Built-in templates (one call presets)

`.ApplyTemplate(ChartTemplate.X)`: `Revenue` (column, N0 Y-axis), `KpiDashboard` (dark, interactive, no grid or legend), `TimeSeries` (spline, 1 s animation), `ExecutiveSummary` (pastel horizontal bar, static), `Accessible` (high-contrast, static). Apply the template first, then override settings.

## Theme cheat-sheet

`Default`, `Dark`, `Pastel`, `Monochrome` (print), `Ocean`, `Sunset`, `Forest`, `Neon`, `Minimal`, `Warm`, `Arctic`, `Business` (corporate), `Material`, `TrafficLight` (status), `Accessible` (colour-blind safe), `Vivid`, `HighContrast` (WCAG AA), `Modern`. All are `ChartTheme.<Name>`.
