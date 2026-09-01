# Chart Types

All 26 chart types available in TerraFluent.Chart.Reporting, each with a complete code example and description of the rendered output.

---

## Quick Reference

| Chart type | Builder method | Data input | Best for |
|---|---|---|---|
| [Line](#1-line) | `AddLine` | `double[]` | Trends over time |
| [Spline](#2-spline) | `AddSpline` | `double[]` | Smooth trend curves |
| [Area](#3-area) | `AddArea` | `double[]` | Volume under a trend |
| [Column](#4-column) | `AddColumn` | `double[]` | Category comparison |
| [Bar (horizontal)](#5-bar-horizontal) | `AddBar` | `double[]` | Ranked categories |
| [Pie / Donut](#6-pie--donut) | `Add` + `.AsPie()` | `double[]` | Part-of-whole |
| [Scatter](#7-scatter) | `AddScatter` | `double[]` | Correlation / distribution |
| [Waterfall](#8-waterfall) | `AddWaterfall` | `double?[]` + `bool[]` | Cumulative P&L |
| [Gauge](#9-gauge) | `AddGauge` | single `double` | Single KPI dial |
| [DataRing](#10-dataring) | `AddDataRing` | single `double` | Full-circle KPI ring |
| [Bubble](#11-bubble) | `AddBubble` | `BubblePoint[]` | 3D scatter (X, Y, size) |
| [Heatmap](#12-heatmap) | `AddHeatmap` | `HeatmapPoint[]` | Matrix / correlation |
| [ColumnRange](#13-columnrange) | `AddColumnRange` | `RangePoint[]` | Low–high bars (temperature etc.) |
| [AreaRange](#14-arearange) | `AddAreaRange` | `RangePoint[]` | Confidence bands |
| [Funnel](#15-funnel) | `AddFunnel` | `double[]` | Pipeline / conversion |
| [Treemap](#16-treemap) | `AddTreemap` | `double[]` | Hierarchical proportions |
| [Radar](#17-radar) | `AddRadar` | `double[]` | Multivariate comparison |
| [BoxPlot](#18-boxplot) | `AddBoxPlot` | `BoxPlotPoint[]` | Statistical distribution |
| [ErrorBar](#19-errorbar) | `AddErrorBar` | `RangePoint[]` | Uncertainty / variance |
| [Candlestick](#20-candlestick) | `AddCandlestick` | `OhlcPoint[]` | OHLC price candles |
| [OHLC](#21-ohlc) | `AddOhlc` | `OhlcPoint[]` | OHLC price bars |
| [Dumbbell](#22-dumbbell) | `AddDumbbell` | `RangePoint[]` | Before/after comparison |
| [Stream](#23-stream) | `AddStream` | `double[]` | ThemeRiver flow over time |
| [Gantt](#24-gantt) | `AddGantt` | `GanttTask[]` | Project timeline |
| [Sankey](#25-sankey) | `AddSankey` | `SankeyNode[]` + `SankeyLink[]` | Flow between stages |
| [Parliament](#26-parliament) | `AddParliament` | `ParliamentGroup[]` | Seat / composition layout |

> **Computed overlays** — you can also derive series from existing data with `AddLinearRegression`, `AddMovingAverage`, and `AddExponentialSmoothing`. See the [API Reference](api-reference.md#computed-overlays).

---

## 1. Line

Connects data points with straight segments. Multi-series lines are automatically coloured from the theme palette.

```csharp
string svg = ChartBuilder.Create()
    .Title("Monthly Website Visitors")
    .Subtitle("Jan – Dec 2025")
    .Size(700, 420)
    .XAxis("Month",
        "Jan","Feb","Mar","Apr","May","Jun",
        "Jul","Aug","Sep","Oct","Nov","Dec")
    .YAxis("Visitors", min: 0)
    .AsAnimated()
    .Series(s => s
        .AddLine("Visitors", new double[]
            { 12400, 14800, 16200, 18500, 21000, 19800,
              22300, 25100, 23700, 20400, 17900, 15600 }))
    .RenderToSvg();
```

**Rendered output:** 700 × 420 px SVG. A single blue line is drawn across 12 months, animating in left-to-right on load. Y-axis starts at 0 and auto-scales to ~25 000. Grid lines are drawn from the theme.

**Multi-series line:**

```csharp
string svg = ChartBuilder.Create()
    .Title("Revenue vs Cost vs Profit")
    .Size(700, 420)
    .XAxis("Quarter", "Q1", "Q2", "Q3", "Q4")
    .YAxis("USD (thousands)", min: 0)
    .AsAnimated()
    .Series(s => s
        .AddLine("Revenue", new double[] { 320, 410, 390, 480 })
        .AddLine("Cost",    new double[] { 210, 260, 245, 290 })
        .AddLine("Profit",  new double[] { 110, 150, 145, 190 }))
    .RenderToSvg();
```

**Rendered output:** Three coloured lines (blue/orange/green from palette). The legend shows each series name. All three lines animate sequentially on load.

---

## 2. Spline

Same as Line but uses Catmull-Rom cubic Bézier curves for smooth interpolation between points.

```csharp
string svg = ChartBuilder.Create()
    .Title("Temperature Trends — Smooth Spline")
    .Size(700, 400)
    .XAxis("Week", "W1","W2","W3","W4","W5","W6","W7","W8")
    .YAxis("Temperature (°C)")
    .AsAnimated()
    .Series(s => s
        .AddSpline("London", new double[] { 18, 20, 22, 25, 27, 24, 21, 19 },
            cfg => cfg.Color(ChartColor.ChartBlue))
        .AddSpline("Madrid", new double[] { 28, 31, 34, 38, 40, 37, 33, 30 },
            cfg => cfg.Color(ChartColor.ChartOrange)))
    .RenderToSvg();
```

**Rendered output:** Two smooth curves with no sharp corners at each data point. London's curve (blue) sits below Madrid's (orange). The curves are rendered as cubic Bézier SVG paths.

> **Tip:** Use `AddSpline` instead of `AddLine` whenever the data represents a naturally continuous signal (temperature, heart rate, sensor readings).

---

## 3. Area

A line chart where the region between the line and the X-axis baseline is filled with a semi-transparent colour.

```csharp
string svg = ChartBuilder.Create()
    .Title("Daily Active Users")
    .Size(700, 420)
    .XAxis("Day", "Mon","Tue","Wed","Thu","Fri","Sat","Sun")
    .YAxis("Users", min: 0)
    .AsAnimated()
    .Series(s => s
        .AddArea("Active Users",
            new double[] { 4200, 5100, 4800, 6300, 7200, 3800, 2900 },
            cfg => cfg.Color(ChartColor.ChartBlue).FillOpacity(0.3)))
    .RenderToSvg();
```

**Rendered output:** A blue-filled area sweeps up from the baseline. The fill is 30 % opaque, so grid lines are visible through it.

**Stacked Area:**

```csharp
string svg = ChartBuilder.Create()
    .Title("Traffic by Channel")
    .Size(720, 420)
    .StackNormal()                              // stack areas cumulatively
    .XAxis("Month", "Jan","Feb","Mar","Apr","May","Jun")
    .YAxis("Sessions", min: 0)
    .AsAnimated()
    .Series(s => s
        .AddArea("Organic",  new double[] { 1200, 1350, 1500, 1700, 1900, 2100 })
        .AddArea("Direct",   new double[] {  800,  850,  900,  950, 1000, 1100 })
        .AddArea("Referral", new double[] {  400,  420,  440,  480,  520,  560 }))
    .RenderToSvg();
```

**Rendered output:** Three colour-filled bands stack on top of each other. The top edge of the topmost band shows total sessions across all channels for each month.

---

## 4. Column

Vertical bars — the most common chart for category comparisons. Supports grouped (side-by-side) and stacked layouts.

```csharp
// Single series
string svg = ChartBuilder.Create()
    .Title("Product Sales by Category")
    .Size(700, 420)
    .XAxis("Category", "Electronics","Clothing","Books","Home","Sports","Toys")
    .YAxis("Units Sold", min: 0)
    .AsAnimated()
    .Series(s => s
        .AddColumn("Units Sold",
            new double[] { 8400, 5200, 3100, 6700, 4300, 2800 }))
    .RenderToSvg();
```

**Rendered output:** Six blue columns of varying heights. Each column grows from the baseline via SMIL animation on page load.

```csharp
// Grouped columns (two series side-by-side)
string svg = ChartBuilder.Create()
    .Title("Budget vs Actual Spend")
    .Size(750, 420)
    .XAxis("Department", "Engineering","Marketing","Sales","Operations","HR")
    .YAxis("USD (thousands)", min: 0)
    .AsAnimated()
    .Series(s => s
        .AddColumn("Budget", new double[] { 500, 200, 350, 280, 120 },
            cfg => cfg.Color(ChartColor.ChartBlue))
        .AddColumn("Actual", new double[] { 470, 230, 310, 295, 108 },
            cfg => cfg.Color(ChartColor.ChartOrange)))
    .RenderToSvg();
```

**Rendered output:** For each department, two bars appear side-by-side — blue (Budget) and orange (Actual). The legend labels both series at the bottom.

```csharp
// Stacked columns (100 % normalised)
string svg = ChartBuilder.Create()
    .Title("Revenue Mix — 100 % Stacked")
    .Size(700, 420)
    .StackPercent()                             // normalise to 100 %
    .XAxis("Quarter", "Q1","Q2","Q3","Q4")
    .YAxis("%", min: 0, max: 100)
    .YAxisFormat("{value}%")
    .Series(s => s
        .AddColumn("Product A", new double[] { 180, 220, 240, 280 },
            cfg => cfg.DataLabel.Show().Format("{value}%"))
        .AddColumn("Product B", new double[] { 140, 160, 175, 200 },
            cfg => cfg.DataLabel.Show().Format("{value}%"))
        .AddColumn("Product C", new double[] {  90, 110, 130, 150 },
            cfg => cfg.DataLabel.Show().Format("{value}%")))
    .RenderToSvg();
```

**Rendered output:** Each column fills the full chart height (100 %). Three colour segments per column show the proportional contribution of each product. Data labels show percentages inside each segment.

---

## 5. Bar (Horizontal)

Horizontal bars — ideal for ranked or labelled categories where the labels are long.

```csharp
string svg = ChartBuilder.Create()
    .Title("Population by City")
    .Size(700, 420)
    .XAxis(x => x.Categories.AddRange(
        new[] { "Tokyo","Delhi","Shanghai","São Paulo","Mexico City","Cairo" }))
    .YAxis(y => { y.Title = "Population (M)"; y.Min = 0; y.Max = 40; })
    .AsAnimated()
    .Series(s => s
        .AddBar("Population (M)",
            new double[] { 37.4, 32.9, 27.1, 22.4, 21.9, 21.3 },
            cfg => cfg.Color(ChartColor.ChartBlue)))
    .RenderToSvg();
```

**Rendered output:** Six horizontal blue bars grow left-to-right from the Y-axis. Tokyo's bar is the longest. City names appear on the vertical axis.

---

## 6. Pie / Donut

A circular chart divided into slices proportional to each value. Add a donut hole with `.DonutHole(fraction)`.

```csharp
// Pie
string svg = ChartBuilder.Create()
    .AsPie()
    .Title("Market Share by Vendor")
    .Size(600, 440)
    .Labels("TerraFluent","Competitor A","Competitor B","Others")
    .Legend(l => l.AtBottom())
    .AsAnimated()
    .Series(s => s
        .Add("Market Share", new double[] { 38, 27, 21, 14 },
            cfg => cfg.DataLabel.Show().Radius(1.2).Format("{value}%")))
    .RenderToSvg();
```

**Rendered output:** A circular pie with four coloured slices. Labels are placed outside each slice (radius > 1) with spline connector lines. The legend lists the four vendors at the bottom.

```csharp
// Donut with center label
string svg = ChartBuilder.Create()
    .AsPie()
    .Title("Revenue by Region")
    .Size(620, 460)
    .Labels("North America","Europe","Asia-Pacific","Rest of World")
    .Legend(l => l.AtBottom())
    .AsAnimated()
    .Series(s => s
        .Add("Revenue %", new double[] { 42, 28, 22, 8 },
            cfg =>
            {
                cfg.DonutHole(0.55);                // 55 % hole radius
                cfg.DataLabel.Show().Radius(1.0).Format("{value}%");
                cfg.DonutCenter.Show().Title("Total");
            }))
    .RenderToSvg();
```

**Rendered output:** A donut ring with a hollow center. The summed total (100) is shown large in the center with "Total" as a caption above it. Labels sit at the edge of each slice.

---

## 7. Scatter

Dots only — no connecting line. Use when the relationship between two variables matters more than a sequence.

```csharp
string svg = ChartBuilder.Create()
    .Title("Sales Rep Performance")
    .Size(700, 420)
    .XAxis(x => x.Title = "Calls Made")
    .YAxis(y => { y.Title = "Deals Closed"; y.Min = 0; })
    .AsAnimated()
    .Series(s => s
        .AddScatter("Team A", new double[] { 62, 58, 74, 55, 80, 67, 71 },
            cfg => cfg.Color(ChartColor.ChartBlue).MarkerSize(8))
        .AddScatter("Team B", new double[] { 45, 52, 61, 48, 57, 70, 43 },
            cfg => cfg.Color(ChartColor.ChartOrange).MarkerSize(8)))
    .RenderToSvg();
```

**Rendered output:** Two sets of coloured dots plotted at their respective Y-values. No lines connect the dots. A legend distinguishes Team A (blue) from Team B (orange).

---

## 8. Waterfall

Shows incremental gains and losses leading to a running total. Positive changes are green, negative are red, totals use the series colour.

```csharp
string svg = ChartBuilder.Create()
    .Title("Annual Cash Flow Analysis")
    .Size(720, 440)
    .XAxis(x => x.Categories.AddRange(new[]
        { "Opening","Revenue","COGS","Gross Profit","OpEx","EBITDA","Tax","Net Profit" }))
    .YAxis(y => y.Title = "USD ($k)")
    .AsAnimated()
    .Series(s => s
        .AddWaterfall(
            name:   "P&L",
            data:   new double?[] { 500, 800, -320, 980, -450, 530, -120, 410 },
            totals: new[] { true, false, false, true, false, true, false, true }))
    .RenderToSvg();
```

**Rendered output:** Eight bars. "Opening", "Gross Profit", "EBITDA", and "Net Profit" are full-height bars (totals). "Revenue" and "EBITDA" increases are green floating bars. "COGS", "OpEx", and "Tax" decreases are red bars hanging from the running total. Each bar starts where the last left off.

**Data rules:**
- `data` values with `totals[i] = true` reset the running total — they draw from zero.
- `data` values with `totals[i] = false` are incremental (positive = up, negative = down).
- Pass `null` in `data` to skip a column while keeping the running total intact.

---

## 9. Gauge

A semi-circular dial showing a single value between a configurable min and max.

```csharp
string svg = ChartBuilder.Create()
    .Title("Server CPU Utilisation")
    .Size(460, 360)
    .YAxis(y => { y.Min = 0; y.Max = 100; })
    .AsAnimated()
    .Series(s => s
        .AddGauge("CPU %", 67, cfg => cfg.Color(ChartColor.ChartBlue)))
    .RenderToSvg();
```

**Rendered output:** A 180° arc (top half of a circle). A coloured filled arc spans from the left baseline to the 67 % mark. The numeric value "67" is displayed below the arc center. The needle animates in on load.

---

## 10. DataRing

A full 360° progress ring (like a circular progress bar) centered on a large value display.

```csharp
string svg = ChartBuilder.Create()
    .Title("Q2 2025 — Customer Satisfaction")
    .Size(400, 400)
    .YAxis(y => { y.Min = 0; y.Max = 100; })
    .AsAnimated()
    .Series(s => s
        .AddDataRing("CSAT Score", 87, cfg =>
        {
            cfg.Color(ChartColor.ChartBlue);
            cfg.DonutCenter
               .Title("CSAT")
               .Color("#1a202c")
               .TitleColor("#718096")
               .FontSize(42)
               .TitleFontSize(14);
        }))
    .RenderToSvg();
```

**Rendered output:** A thick circular ring fills 87 % of its circumference (clockwise from top). The value "87" is displayed large in the center with "CSAT" as a subtitle below. The ring sweeps in with a clockwise animation on load.

**KPI Dashboard — three rings side-by-side:**

```csharp
// Render three separate SVGs and combine in your layout
string cpu  = ChartBuilder.Create().Title("CPU") .Size(300,300).YAxis(y=>{y.Min=0;y.Max=100;}).AsAnimated()
    .Series(s => s.AddDataRing("CPU",  67, cfg => cfg.Color(ChartColor.ChartOrange).DonutCenter.FontSize(32)))
    .RenderToSvg();

string ram  = ChartBuilder.Create().Title("RAM") .Size(300,300).YAxis(y=>{y.Min=0;y.Max=100;}).AsAnimated()
    .Series(s => s.AddDataRing("RAM",  82, cfg => cfg.Color(ChartColor.ChartRose).DonutCenter.FontSize(32)))
    .RenderToSvg();

string disk = ChartBuilder.Create().Title("Disk").Size(300,300).YAxis(y=>{y.Min=0;y.Max=100;}).AsAnimated()
    .Series(s => s.AddDataRing("Disk", 34, cfg => cfg.Color(ChartColor.ChartGreen).DonutCenter.FontSize(32)))
    .RenderToSvg();

// Embed in a flex container in your HTML/Blazor:
// <div style="display:flex;gap:1rem">
//   @((MarkupString)cpu)  @((MarkupString)ram)  @((MarkupString)disk)
// </div>
```

---

## 11. Bubble

A scatter chart with a third dimension (Z) encoded as the bubble radius. Ideal for showing three correlated business metrics simultaneously.

```csharp
using TerraFluent.Chart.Reporting.Models;

string svg = ChartBuilder.Create()
    .Title("Market Share vs. Growth — Bubble Size = Revenue")
    .Size(700, 400)
    .XAxis(x => { x.Title = "Market Share (%)"; x.Min = 0; x.Max = 40; })
    .YAxis(y => { y.Title = "YoY Growth (%)"; y.Min = -10; y.Max = 50; })
    .AsAnimated()
    .Legend(l => l.TopRight())
    .Series(s => s
        .AddBubble("Product A", new[]
        {
            new BubblePoint(12, 28, 85),    // (X=market share, Y=growth, Z=revenue $M)
            new BubblePoint(22, 15, 120),
            new BubblePoint( 8, 42, 60),
        })
        .AddBubble("Product B", new[]
        {
            new BubblePoint(30,  5, 200),
            new BubblePoint(18, 22,  95),
            new BubblePoint(35, -5, 140),
        }))
    .RenderToSvg();
```

**Rendered output:** Circles plotted at (X, Y) coordinates. The radius of each circle is proportional to its Z value — Product B's 200-unit bubble is visibly larger than the 60-unit one. Two series are coloured differently and shown in the legend.

**`BubblePoint` struct:**

| Property | Description |
|---|---|
| `X` | Horizontal axis position |
| `Y` | Vertical axis position |
| `Z` | Bubble size (proportional to area) |

---

## 12. Heatmap

A colour-coded grid matrix. Cell colour interpolates between a cold colour and the series colour based on value intensity.

```csharp
using TerraFluent.Chart.Reporting.Models;

string svg = ChartBuilder.Create()
    .Title("Weekly Sales by Region & Day")
    .Size(700, 380)
    .XAxis("Day", "Mon","Tue","Wed","Thu","Fri")   // column labels
    .Series(s => s
        .AddHeatmap("Sales",
            new[]
            {
                // HeatmapPoint(col, row, value)
                new HeatmapPoint(0,0,42), new HeatmapPoint(1,0,58), new HeatmapPoint(2,0,73),
                new HeatmapPoint(3,0,61), new HeatmapPoint(4,0,88),
                new HeatmapPoint(0,1,31), new HeatmapPoint(1,1,45), new HeatmapPoint(2,1,52),
                new HeatmapPoint(3,1,78), new HeatmapPoint(4,1,65),
                new HeatmapPoint(0,2,67), new HeatmapPoint(1,2,83), new HeatmapPoint(2,2,91),
                new HeatmapPoint(3,2,55), new HeatmapPoint(4,2,48),
            },
            cfg =>
            {
                cfg.DataLabel.Show().Format("{value}");     // show value in each cell
                cfg.HeatmapRowLabels.AddRange(new[] { "North","South","East" });
            }))
    .RenderToSvg();
```

**Rendered output:** A 5 × 3 grid of coloured rectangles. Low values appear in a cold blue-grey; high values (like 91) appear in a saturated series colour. Row labels "North / South / East" are on the left; column labels "Mon – Fri" are on the bottom. Each cell shows its numeric value.

**`HeatmapPoint` struct:**

| Property | Description |
|---|---|
| `Col` | Zero-based column index (maps to `XAxis.Categories`) |
| `Row` | Zero-based row index (maps to `HeatmapRowLabels`) |
| `Value` | Numeric intensity controlling cell colour |

---

## 13. ColumnRange

A vertical bar spanning from a low to a high value per category. Perfect for temperature ranges, confidence intervals, or scheduling spans.

```csharp
using TerraFluent.Chart.Reporting.Models;

string svg = ChartBuilder.Create()
    .Title("Monthly Temperature Range (°C)")
    .Size(700, 400)
    .XAxis("Month",
        "Jan","Feb","Mar","Apr","May","Jun",
        "Jul","Aug","Sep","Oct","Nov","Dec")
    .YAxis(y => { y.Title = "Temperature (°C)"; y.Min = -10; y.Max = 40; y.LabelFormat = "{value}°"; })
    .AsAnimated()
    .Series(s => s
        .AddColumnRange("London",
            new[]
            {
                new RangePoint( 2,  8), new RangePoint( 2,  9), new RangePoint( 4, 13),
                new RangePoint( 6, 16), new RangePoint( 9, 20), new RangePoint(12, 23),
                new RangePoint(14, 26), new RangePoint(14, 25), new RangePoint(11, 21),
                new RangePoint( 8, 17), new RangePoint( 5, 12), new RangePoint( 3,  9),
            },
            cfg => cfg.Color(ChartColor.ChartBlue)))
    .RenderToSvg();
```

**Rendered output:** 12 blue bars — one per month — each floating between its low (bottom) and high (top) temperature. July's bar is the tallest and highest; January's is the lowest.

**`RangePoint` struct:**

| Property | Description |
|---|---|
| `Low` | Bottom of the bar / band |
| `High` | Top of the bar / band |

---

## 14. AreaRange

A filled band drawn between two lines (low and high). Typically used to visualise forecast confidence intervals or min/max ranges alongside a mean line.

```csharp
using TerraFluent.Chart.Reporting.Models;

string svg = ChartBuilder.Create()
    .Title("Revenue Forecast — Confidence Band")
    .Size(700, 380)
    .XAxis("Week", "W1","W2","W3","W4","W5","W6","W7","W8")
    .YAxis(y => { y.Title = "Revenue ($k)"; y.Min = 80; y.Max = 280; })
    .AsAnimated()
    .Series(s => s
        // Mean forecast line on top
        .AddLine("Forecast",
            new double[] { 140, 152, 161, 170, 178, 185, 192, 200 },
            cfg => cfg.Color("#2ecc71").LineWidth(2))
        // Confidence band underneath
        .AddAreaRange("Confidence Band",
            new[]
            {
                new RangePoint(120, 160), new RangePoint(130, 172), new RangePoint(138, 184),
                new RangePoint(148, 192), new RangePoint(155, 200), new RangePoint(160, 210),
                new RangePoint(167, 217), new RangePoint(174, 226),
            },
            cfg => cfg.Color("#2ecc71")))      // same colour, semi-transparent fill
    .RenderToSvg();
```

**Rendered output:** A green-tinted band fills the space between the lower and upper confidence bounds. A solid green line (the mean forecast) runs through the middle of the band. The band and line share the same green palette but the fill is semi-transparent.

---

## 15. Funnel

Stacked trapezoid stages that narrow as a value decreases. Ideal for sales pipelines, conversion funnels, and process flow analysis.

```csharp
string svg = ChartBuilder.Create()
    .Title("Sales Pipeline")
    .Size(600, 440)
    .XAxis("Stage", "Leads","Qualified","Proposal","Negotiation","Closed")
    .AsAnimated()
    .Series(s => s
        .AddFunnel("Pipeline",
            new double[] { 5000, 2800, 1400, 620, 310 },
            cfg => cfg.Label(dl => dl.Show())))
    .RenderToSvg();
```

**Rendered output:** Five trapezoids stacked vertically, each narrower than the one above it. "Leads" (5 000) is the widest; "Closed" (310) is the narrowest. Stage labels ("Leads", "Qualified", etc.) appear on the left side of each trapezoid.

**Proportionality:** Each stage's width is `value / maxValue` of the total funnel width. The library computes the widths automatically.

---

## 16. Treemap

Nested rectangles sized proportionally to their value, using a balanced binary-split layout. Excellent for portfolio allocation, budget breakdown, or disk usage visualisation.

```csharp
string svg = ChartBuilder.Create()
    .Title("Portfolio Allocation")
    .Size(700, 420)
    .XAxis("Asset",
        "US Equities","EU Equities","EM Equities",
        "Gov Bonds",  "Corp Bonds",
        "Real Estate","Commodities","Cash")
    .AsAnimated()
    .Series(s => s
        .AddTreemap("Allocation",
            new double[] { 3200, 1800, 900, 1500, 1100, 700, 400, 300 }))
    .RenderToSvg();
```

**Rendered output:** The chart area is divided into eight coloured rectangles. "US Equities" (3 200) occupies the largest rectangle (roughly half the chart). Each rectangle is labelled with its asset name and value. Colours cycle through the theme palette.

---

## 17. Radar

Plots several axes radiating from a centre, with each series drawn as a closed polygon. Ideal for comparing multiple entities across the same set of metrics.

```csharp
string svg = ChartBuilder.Create()
    .Title("Skill Assessment")
    .Size(560, 520)
    .XAxis("Skill", "Coding","Design","Testing","DevOps","Docs","Comms")
    .AsAnimated()
    .Series(s => s
        .AddRadar("Alice", new double[] { 90, 60, 75, 50, 65, 80 },
            cfg => cfg.Color(ChartColor.ChartBlue).FillOpacity(0.25))
        .AddRadar("Bob",   new double[] { 65, 85, 60, 80, 55, 70 },
            cfg => cfg.Color(ChartColor.ChartOrange).FillOpacity(0.25)))
    .RenderToSvg();
```

**Rendered output:** Six spokes labelled with each skill. Two translucent polygons (blue, orange) overlay one another so strengths and gaps are immediately visible. A legend distinguishes the two people.

---

## 18. BoxPlot

A box-and-whisker chart showing the five-number summary (min, Q1, median, Q3, max) per category. Perfect for comparing distributions.

```csharp
using TerraFluent.Chart.Reporting.Models;

string svg = ChartBuilder.Create()
    .Title("Response Time Distribution by Endpoint (ms)")
    .Size(700, 420)
    .XAxis("Endpoint", "/login","/search","/checkout","/report")
    .YAxis("ms", min: 0)
    .AsAnimated()
    .Series(s => s
        .AddBoxPlot("Latency", new[]
        {
            // BoxPlotPoint(low, q1, median, q3, high)
            new BoxPlotPoint( 40,  70,  95, 130, 210),
            new BoxPlotPoint( 55,  90, 120, 160, 260),
            new BoxPlotPoint( 80, 140, 190, 250, 400),
            new BoxPlotPoint(120, 220, 300, 410, 620),
        }))
    .RenderToSvg();
```

**Rendered output:** Four boxes, one per endpoint. Each box spans Q1–Q3 with a median line inside; whiskers extend to min and max. `/report` sits highest, revealing the slowest and most variable endpoint.

**`BoxPlotPoint` struct:** `Low`, `Q1`, `Median`, `Q3`, `High`.

---

## 19. ErrorBar

Draws a vertical whisker from a low to a high value per category — typically overlaid on a line or column series to show uncertainty.

```csharp
using TerraFluent.Chart.Reporting.Models;

string svg = ChartBuilder.Create()
    .Title("Measured Mean ± Std. Dev.")
    .Size(700, 400)
    .XAxis("Sample", "A","B","C","D","E")
    .YAxis("Value", min: 0)
    .AsAnimated()
    .Series(s => s
        .AddLine("Mean", new double[] { 30, 42, 38, 55, 48 },
            cfg => cfg.Color(ChartColor.ChartBlue))
        .AddErrorBar("± SD", new[]
        {
            new RangePoint(26, 34), new RangePoint(37, 47), new RangePoint(33, 43),
            new RangePoint(49, 61), new RangePoint(43, 53),
        }, cfg => cfg.Color(ChartColor.Charcoal)))
    .RenderToSvg();
```

**Rendered output:** A blue mean line with a grey error whisker at each point, each capped top and bottom, spanning the low–high uncertainty band.

---

## 20. Candlestick

Financial OHLC candles. Up sessions (close ≥ open) use the theme's positive colour; down sessions use the negative colour.

```csharp
using TerraFluent.Chart.Reporting.Models;

string svg = ChartBuilder.Create()
    .Title("ACME — Daily OHLC")
    .Size(720, 420)
    .XAxis("Day", "Mon","Tue","Wed","Thu","Fri")
    .YAxis("Price ($)")
    .AsAnimated()
    .Series(s => s
        .AddCandlestick("ACME", new[]
        {
            // OhlcPoint(open, high, low, close)
            new OhlcPoint(120, 128, 118, 126),
            new OhlcPoint(126, 130, 122, 123),
            new OhlcPoint(123, 133, 121, 132),
            new OhlcPoint(132, 135, 128, 129),
            new OhlcPoint(129, 140, 127, 138),
        }))
    .RenderToSvg();
```

**Rendered output:** Five candles. Each has a thin high–low wick and a thick open–close body. Green bodies mark up days, red bodies mark down days.

**`OhlcPoint` struct:** `Open`, `High`, `Low`, `Close`.

---

## 21. OHLC

The same open/high/low/close data drawn as bars instead of candles: a high–low vertical bar with a left tick (open) and a right tick (close).

```csharp
using TerraFluent.Chart.Reporting.Models;

string svg = ChartBuilder.Create()
    .Title("ACME — OHLC Bars")
    .Size(720, 420)
    .XAxis("Day", "Mon","Tue","Wed","Thu","Fri")
    .YAxis("Price ($)")
    .AsAnimated()
    .Series(s => s
        .AddOhlc("ACME", new[]
        {
            new OhlcPoint(120, 128, 118, 126),
            new OhlcPoint(126, 130, 122, 123),
            new OhlcPoint(123, 133, 121, 132),
            new OhlcPoint(132, 135, 128, 129),
            new OhlcPoint(129, 140, 127, 138),
        }))
    .RenderToSvg();
```

**Rendered output:** Five vertical bars. A left-pointing tick marks the open price and a right-pointing tick marks the close. Colour follows the same up/down convention as candlesticks.

---

## 22. Dumbbell

A dot-plot connecting a low and a high value per category — great for before/after or start/end comparisons.

```csharp
using TerraFluent.Chart.Reporting.Models;

string svg = ChartBuilder.Create()
    .Title("Salary Change After Promotion")
    .Size(700, 420)
    .XAxis("Role", "Junior","Mid","Senior","Lead","Principal")
    .YAxis("Salary ($k)", min: 0)
    .AsAnimated()
    .Series(s => s
        .AddDumbbell("Before → After", new[]
        {
            new RangePoint( 60,  72), new RangePoint( 85, 100),
            new RangePoint(110, 132), new RangePoint(140, 168),
            new RangePoint(175, 210),
        }, cfg => cfg.Color(ChartColor.ChartBlue)))
    .RenderToSvg();
```

**Rendered output:** For each role, two dots connected by a bar — the left/lower dot is the "before" value, the right/upper dot the "after". The connector length shows the size of each raise.

---

## 23. Stream

A ThemeRiver: stacked areas rendered around a centred baseline so the whole silhouette flows organically. Good for showing how a total and its composition evolve over time.

```csharp
string svg = ChartBuilder.Create()
    .Title("Genre Popularity Over Time")
    .Size(720, 420)
    .XAxis("Year", "2019","2020","2021","2022","2023","2024")
    .AsAnimated()
    .Series(s => s
        .AddStream("Pop",     new double[] { 40, 45, 52, 60, 58, 64 })
        .AddStream("Rock",    new double[] { 55, 50, 48, 44, 42, 40 })
        .AddStream("Hip-Hop", new double[] { 30, 38, 47, 55, 62, 70 })
        .AddStream("Jazz",    new double[] { 18, 17, 16, 16, 15, 15 }))
    .RenderToSvg();
```

**Rendered output:** Four coloured bands flow left-to-right around a central axis. The thickness of each band at any year is its value; the overall shape widens as total popularity grows.

---

## 24. Gantt

A horizontal project timeline. Each task is a bar spanning its start-to-end position on the X (time) axis.

```csharp
using TerraFluent.Chart.Reporting.Models;

string svg = ChartBuilder.Create()
    .Title("Release Plan (weeks)")
    .Size(760, 380)
    .XAxis(x => { x.Title = "Week"; x.Min = 0; x.Max = 12; })
    .AsAnimated()
    .Series(s => s
        .AddGantt("Schedule", new[]
        {
            new GanttTask { Name = "Design",      Start = 0, End = 3 },
            new GanttTask { Name = "Development",  Start = 2, End = 8, Color = ChartColor.ChartBlue },
            new GanttTask { Name = "Testing",      Start = 7, End = 10 },
            new GanttTask { Name = "Launch",       Start = 10, End = 12, Color = ChartColor.ChartGreen },
        }))
    .RenderToSvg();
```

**Rendered output:** Four stacked rows, each with a bar positioned along the week axis. Overlapping bars show parallel work (Design and Development overlap at weeks 2–3). Custom colours highlight key phases.

**`GanttTask` properties:** `Name`, `Start`, `End`, optional `Color`, optional `Label`.

---

## 25. Sankey

A node-link flow diagram. Nodes are stages; links carry a quantity whose thickness is proportional to its value.

```csharp
using TerraFluent.Chart.Reporting.Models;

string svg = ChartBuilder.Create()
    .Title("Website Conversion Flow")
    .Size(760, 440)
    .AsAnimated()
    .Series(s => s
        .AddSankey("Flow",
            nodes: new[]
            {
                new SankeyNode { Name = "Visitors" },   // 0
                new SankeyNode { Name = "Sign-ups" },   // 1
                new SankeyNode { Name = "Trials" },     // 2
                new SankeyNode { Name = "Paid" },       // 3
                new SankeyNode { Name = "Churned" },    // 4
            },
            links: new[]
            {
                new SankeyLink { From = 0, To = 1, Value = 1000 },
                new SankeyLink { From = 1, To = 2, Value = 620 },
                new SankeyLink { From = 2, To = 3, Value = 240 },
                new SankeyLink { From = 2, To = 4, Value = 380 },
            }))
    .RenderToSvg();
```

**Rendered output:** Five stacked nodes connected by curved ribbons. The Visitors→Sign-ups ribbon is the thickest; the split from Trials shows how many converted to Paid versus Churned. Link thickness encodes each flow's magnitude.

**`SankeyLink.From` / `To`** are zero-based indices into the `nodes` array.

---

## 26. Parliament

A semicircular seating chart — one dot per seat, grouped by party. Standard for election results and any whole-of-assembly composition.

```csharp
using TerraFluent.Chart.Reporting.Models;

string svg = ChartBuilder.Create()
    .Title("Parliament Composition (200 seats)")
    .Size(640, 400)
    .AsAnimated()
    .Series(s => s
        .AddParliament("Seats", new[]
        {
            new ParliamentGroup("Progressive", ChartColor.ChartBlue,   82),
            new ParliamentGroup("Conservative", ChartColor.ChartRed,   74),
            new ParliamentGroup("Green",        ChartColor.ChartGreen,  26),
            new ParliamentGroup("Independent",  ChartColor.Charcoal,    18),
        }))
    .RenderToSvg();
```

**Rendered output:** An arc of 200 coloured dots arranged in concentric rows, grouped left-to-right by party. Each party occupies a contiguous block sized by its seat count. A legend maps colours to parties.

**`ParliamentGroup` constructor:** `(string name, string color, int seats)`.

---

## Combining Chart Types (Mixed Charts)

You can mix any series types within a single chart:

```csharp
string svg = ChartBuilder.Create()
    .Title("Revenue & Profit Margin")
    .Size(720, 420)
    .XAxis("Quarter", "Q1","Q2","Q3","Q4")
    .YAxis("Revenue ($k)", min: 0)
    .YAxis2(y => { y.Title = "Margin (%)"; y.Min = 0; y.Max = 50; })
    .AsAnimated()
    .Series(s => s
        // Columns on the primary (left) Y-axis
        .AddColumn("Revenue", new double[] { 310, 390, 420, 510 },
            cfg => cfg.Color(ChartColor.ChartBlue))
        // Line on the secondary (right) Y-axis
        .AddLine("Margin %",  new double[] { 22, 28, 25, 31 },
            cfg => cfg.Color(ChartColor.ChartRose).LineWidth(3).OnSecondaryAxis()))
    .RenderToSvg();
```

**Rendered output:** Blue columns grow from the left axis. A pink line floats independently, scaled against the right axis (0 – 50 %). Both axes are labelled. The legend shows both series.
