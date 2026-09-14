# Themes & Styling

Everything you need to control the visual appearance of your charts: themes, colour palettes, series styling, data labels, and tooltips.

---

## Built-in Themes

Apply a theme with `.Theme(ChartTheme.X)`:

```csharp
ChartBuilder.Create()
    .Theme(ChartTheme.Dark)
    // …
```

| Theme | Background | Text | Palette style | Best for |
|---|---|---|---|---|
| `ChartTheme.Default` | `#ffffff` white | `#333333` dark | Blue-orange | General use |
| `ChartTheme.Dark` | `#1a1a2e` navy | `#f0f0ff` light | Neon-adjacent bright palette | Browser dashboards |
| `ChartTheme.Pastel` | `#fafafa` off-white | `#555555` grey | Soft muted palette | Presentations, reports |
| `ChartTheme.Monochrome` | `#ffffff` white | `#000000` black | Greyscale only | Print, greyscale PDFs |
| `ChartTheme.Ocean` | `#0d1b2a` deep navy | `#c8e6ff` pale blue | Blue-teal palette | Analytics dashboards |
| `ChartTheme.Sunset` | `#1a0a2e` purple-navy | `#ffe8c8` warm cream | Vivid warm palette | Editorial, marketing |
| `ChartTheme.Forest` | `#f6f4ee` warm cream | `#2c2a1a` earthy brown | Earthy greens | Nature / ESG reporting |
| `ChartTheme.Neon` | `#0a0a0a` near-black | `#f0f0ff` light | Electric high-contrast palette | Dark dashboards, streaming overlays |
| `ChartTheme.Minimal` | `#ffffff` white | `#333333` graphite | Muted professional palette | Clean editorial reports |
| `ChartTheme.Warm` | `#faf3e0` parchment | `#3e2723` espresso | Amber-brown earth tones | Warm, print-style reports |
| `ChartTheme.Arctic` | `#f0f8ff` ice blue | `#1c2e4a` polar navy | Cool crisp blues | Clean corporate dashboards |
| `ChartTheme.Business` | `#ffffff` white | `#333333` graphite | Corporate blue-red palette | Executive summaries |
| `ChartTheme.Material` | `#ffffff` white | `#444444` charcoal | Material Design 500-level palette | Modern web dashboards |
| `ChartTheme.TrafficLight` | `#fafafa` off-white | `#333333` graphite | Green/amber/red status palette | KPI / status dashboards |
| `ChartTheme.Accessible` | `#ffffff` white | `#333333` graphite | Colour-blind-safe (Wong 2011) | Accessibility-critical charts |
| `ChartTheme.Vivid` | `#ffffff` white | `#2c3e50` slate | Full-spectrum distinct palette | High-impact presentations |
| `ChartTheme.HighContrast` | `#ffffff` white | `#000000` black | WCAG AA (≥ 4.5:1) palette | Accessibility-critical charts |
| `ChartTheme.Modern` | `#f8f8ff` ghost white | `#2c3e50` slate | Material palette + `ModernStyle` on | Default recommendation for new dashboards |

See [Modern Styling](#modern-styling) below for what `ModernStyle` changes, and [ChartColor Catalogue](#chartcolor-catalogue) for the exact hex values behind each palette.

### Dark Theme Example

```csharp
string svg = ChartBuilder.Create()
    .Theme(ChartTheme.Dark)
    .Title("Server Throughput")
    .Size(700, 420)
    .XAxis("Hour", "00","04","08","12","16","20","24")
    .YAxis("Req/s", min: 0)
    .AsAnimated()
    .Series(s => s
        .AddArea("API",   new double[] { 820, 430, 1200, 1800, 1650, 1100, 640 })
        .AddLine("Cache", new double[] { 600, 310,  900, 1300, 1200,  850, 480 }))
    .RenderToSvg();
```

**Output:** Navy blue background (`#1a1a2e`), light axis labels, bright palette area/line. Grid lines rendered in muted `#3a3a5e`. Title text is `#f0f0ff`.

### Pastel Theme Example

```csharp
string svg = ChartBuilder.Create()
    .Theme(ChartTheme.Pastel)
    .Title("Monthly Sales")
    .Size(700, 420)
    .XAxis(x => x.Categories.AddRange(new[] { "Jan","Feb","Mar","Apr","May","Jun" }))
    .YAxis("Units", min: 0)
    .AsAnimated()
    .Series(s => s
        .AddColumn("Product A", new double[] { 420, 380, 510, 490, 560, 600 })
        .AddColumn("Product B", new double[] { 310, 290, 370, 400, 430, 450 }))
    .RenderToSvg();
```

**Output:** Off-white `#fafafa` background, very light grid lines, soft muted palette (`#a8d8ea`, `#aa96da`, …). Suitable for presentations and printed reports.

---

## Modern Styling

Every built-in theme — including `Default` — has `ChartTheme.ModernStyle` set to `true`. It applies a consistent set of refinements uniformly across **Static**, **Animated**, and **Interactive** render modes:

- Softly rounded corners on column/bar rectangles.
- A subtle top-lighter fill gradient with soft elevation shadow on columns, bars, and areas.
- Hollow-ring line/scatter markers instead of solid dots.
- Crisper separators between pie/donut slices.
- Fading (gradient) area-chart fills instead of a flat opacity.
- Lighter, horizontal-only grid lines.
- In **Interactive** mode only: a full-height/width **hover band** that highlights the entire category column or row on mouse-over, in addition to the point-level tooltip.

`ChartTheme.Modern` is a dedicated preset (Material palette on a ghost-white background) for teams that want the modern look front-and-centre, but the styling itself is not tied to that one preset — it is on by default for every theme above.

To opt out and get the classic flat look, clone a theme and turn it off:

```csharp
var classic = ChartTheme.Dark.Clone();
classic.ModernStyle = false;

string svg = ChartBuilder.Create()
    .Theme(classic)
    .AsInteractive()
    .Series(s => s.AddColumn("Sales", new double[] { 420, 380, 510, 490 }))
    .RenderToSvg();
```

With `ModernStyle = false`, columns/bars render with sharp corners and a flat fill, markers render as solid dots, and hover bands are omitted from Interactive output.

---

## Custom Theme

Create a one-off theme with `ChartTheme.Custom(…)`:

```csharp
var brandTheme = ChartTheme.Custom(
    backgroundColor:     "#0d1117",    // GitHub-dark background
    plotBackgroundColor: "none",
    gridLineColor:       "#21262d",
    axisLineColor:       "#30363d",
    textColor:           "#e6edf3",
    fontFamily:          "'Segoe UI', sans-serif",
    colors: new[] { "#58a6ff", "#3fb950", "#f78166", "#d29922", "#a371f7" }
);

string svg = ChartBuilder.Create()
    .Theme(brandTheme)
    .Title("Custom Brand Theme")
    .Size(700, 420)
    .XAxis("Quarter", "Q1","Q2","Q3","Q4")
    .YAxis("Revenue ($k)", min: 0)
    .AsAnimated()
    .Series(s => s
        .AddLine("Revenue", new double[] { 310, 420, 380, 510 })
        .AddLine("Target",  new double[] { 340, 400, 420, 500 }))
    .RenderToSvg();
```

**Output:** Dark background styled like GitHub's dark mode. Blue and green lines are drawn against a near-black `#0d1117` background with subtle grid lines.

**`ChartTheme.Custom` parameters** (all optional — unset properties fall back to `Default`):

| Parameter | Description |
|---|---|
| `backgroundColor` | SVG/page background colour. |
| `plotBackgroundColor` | Plot area fill. Use `"none"` for transparent. |
| `gridLineColor` | Colour of horizontal grid lines. |
| `axisLineColor` | Colour of axis border lines. |
| `textColor` | Default text fill colour (title, labels, legend). |
| `fontFamily` | CSS font-family for all text. |
| `colors` | Ordered palette array — must have at least one entry when provided. |

---

## Overriding Palette Only

Use `.Colors(…)` to swap the palette without changing the rest of the active theme:

```csharp
string svg = ChartBuilder.Create()
    .Theme(ChartTheme.Dark)                           // dark background + text
    .Colors(ChartColor.ChartRose, ChartColor.ChartYellow, ChartColor.ChartTeal)          // but use a custom 3-colour palette
    .Title("Sales Channels")
    .Size(700, 420)
    .XAxis("Month", "Jan","Feb","Mar","Apr","May","Jun")
    .YAxis("Units", min: 0)
    .AsAnimated()
    .Series(s => s
        .AddColumn("Online", new double[] { 180, 210, 190, 260, 310, 280 })
        .AddColumn("Store",  new double[] { 140, 160, 175, 200, 195, 220 })
        .AddLine("Total",    new double[] { 320, 370, 365, 460, 505, 500 }))
    .RenderToSvg();
```

**Output:** Dark theme background and text, but the three series use pink, yellow, and teal instead of the dark theme's default palette.

Colours wrap: if you have 5 series and 3 colours, series 4 and 5 use colours 1 and 2 again.

---

## ChartColor Catalogue

`ChartColor` (namespace `TerraFluent.Chart.Reporting.Models`) provides 400+ named colour constants as `public const string` values. Use them anywhere a colour string is accepted — no hex codes to remember.

```csharp
using TerraFluent.Chart.Reporting.Models;

// Palette override
.Colors(ChartColor.ChartBlue, ChartColor.ChartOrange, ChartColor.ChartGreen)

// Per-series colour
.AddLine("Sales", data, cfg => cfg.Color(ChartColor.ChartRose))

// Plot annotation
.PlotLine(500, color: ChartColor.Red, width: 2, label: "SLA limit")
```

### Chart Palette Constants

| Constant | Hex | Default series slot |
|---|---|---|
| `ChartColor.ChartBlue` | `#7CB5EC` | 1st |
| `ChartColor.ChartOrange` | `#F7A35C` | 2nd |
| `ChartColor.ChartGreen` | `#90ED7D` | 3rd |
| `ChartColor.ChartYellow` | `#E4D354` | 4th |
| `ChartColor.ChartIndigo` | `#8085E9` | 5th |
| `ChartColor.ChartRose` | `#F15C80` | 6th |
| `ChartColor.ChartTeal` | `#2B908F` | 7th |
| `ChartColor.ChartRed` | `#F45B5B` | 8th |
| `ChartColor.Charcoal` | `#434348` | Dark accent |

### Pastel Palette Constants

| Constant | Hex |
|---|---|
| `ChartColor.PastelSkyBlue` | `#A8D8EA` |
| `ChartColor.PastelPurple` | `#AA96DA` |
| `ChartColor.PastelPink` | `#FCBAD3` |
| `ChartColor.PastelMint` | `#B5EAD7` |
| `ChartColor.PastelPeach` | `#FFDAC1` |
| `ChartColor.PastelPeriwinkle` | `#C7CEEA` |
| `ChartColor.PastelLime` | `#E2F0CB` |
| `ChartColor.PastelLemon` | `#FFFFD2` |

### Pre-built Palettes

Apply a complete curated palette in one call:

```csharp
.Colors(ChartColor.Palette.Default)      // Default palette (same as no override)
.Colors(ChartColor.Palette.Pastel)       // soft pastels — matches ChartTheme.Pastel
.Colors(ChartColor.Palette.Material)     // Material Design colours
.Colors(ChartColor.Palette.Business)     // professional blues / greys
.Colors(ChartColor.Palette.Accessible)   // WCAG high-contrast
.Colors(ChartColor.Palette.TrafficLight) // red / amber / green
.Colors(ChartColor.Palette.Monochrome)   // greyscale range
```

### Colour Utility Methods

```csharp
// Semi-transparent variant → "rgba(124,181,236,0.25)"
string faded   = ChartColor.WithOpacity(ChartColor.ChartBlue, 0.25);

// Tint / shade
string lighter = ChartColor.Lighten(ChartColor.ChartBlue, 0.20);
string darker  = ChartColor.Darken(ChartColor.ChartBlue, 0.20);

// From RGB components → "#7CB5EC"
string custom  = ChartColor.FromRgb(124, 181, 236);

// Blend two colours at equal weight
string mixed   = ChartColor.Mix(ChartColor.ChartBlue, ChartColor.White, 0.5);
```

> The full catalogue (400+ constants: CSS named colours, web-safe primaries, chart palette, pastels, theme-specific tokens, and more) is defined in `Models/ChartColor.cs`.

---

## Per-Series Colour

Override a single series colour inside the `cfg =>` lambda:

```csharp
.Series(s => s
    .AddLine("Revenue", data, cfg => cfg.Color(ChartColor.ChartBlue))
    .AddLine("Cost",    data, cfg => cfg.Color(ChartColor.ChartOrange))
    .AddLine("Profit",  data, cfg => cfg.Color(ChartColor.ChartGreen)))
```

---

## Line Styling

```csharp
.AddLine("Target", data, cfg => cfg
    .Color(ChartColor.ChartRose)
    .LineWidth(3)        // stroke width (px), default 2
    .Dashed()            // "Solid" | "Dashed" | "Dotted" | "DashDotted" | "LongDashed"
    .MarkerEnabled(false))
```

**`Dashed` variants:**

| Method | SVG stroke-dasharray | Looks like |
|---|---|---|
| `Solid()` | (none) | ——————— |
| `Dashed()` | `8 4` | - - - - - - |
| `Dotted()` | `2 3` | · · · · · · |
| `DashDotted()` | `8 4 2 4` | -·-·-·-·- |
| `LongDashed()` | `16 6` | ——— ——— |

**Example — dashed reference line on a line chart:**

```csharp
string svg = ChartBuilder.Create()
    .Title("Actuals vs Target")
    .Size(700, 400)
    .XAxis("Q", "Q1","Q2","Q3","Q4")
    .YAxis("Revenue ($k)", min: 0)
    .AsAnimated()
    .Series(s => s
        .AddColumn("Actuals", new double[] { 310, 390, 420, 510 },
            cfg => cfg.Color(ChartColor.ChartBlue))
        .AddLine("Target", new double[] { 300, 380, 420, 450 },
            cfg => cfg.Color(ChartColor.ChartRose).LineWidth(2).Dashed()))
    .RenderToSvg();
```

**Output:** Blue columns for actuals. A pink dashed horizontal reference line for the target. The dashes distinguish the target clearly without overwhelming the data.

---

## Area Fill Opacity

```csharp
.AddArea("Organic", data, cfg => cfg.Color(ChartColor.ChartBlue).FillOpacity(0.15))
.AddArea("Direct",  data, cfg => cfg.Color(ChartColor.ChartGreen).FillOpacity(0.40))
.AddArea("Paid",    data, cfg => cfg.Color(ChartColor.ChartOrange).FillOpacity(0.70))
```

**`FillOpacity`** accepts values from `0.0` (fully transparent) to `1.0` (fully opaque). Default is `0.25` for normal area, `0.7` for stacked area.

**Output:** Three overlapping area series, each at a different opacity. The Organic area at 15 % barely tints the background; the Paid area at 70 % is visually dominant.

---

## Column / Bar Borders & Corner Radius

```csharp
.AddColumn("Revenue", data, cfg => cfg
    .Color(ChartColor.ChartBlue)
    .BorderColor("#1a6090")    // contrasting outline
    .BorderWidth(1)            // border thickness (px)
    .BorderRadius(6))          // rounded top corners (px)
```

**Example — three styled column series:**

```csharp
string svg = ChartBuilder.Create()
    .Title("Series Formatting — Border & Radius")
    .Size(760, 440)
    .XAxis("Q", "Q1","Q2","Q3","Q4")
    .YAxis("Value ($k)", min: 0)
    .AsAnimated()
    .Series(s => s
        .AddColumn("Revenue", new double[] { 320, 410, 390, 480 }, cfg => cfg
            .Color(ChartColor.ChartBlue).BorderColor("#1a6090").BorderWidth(1).BorderRadius(6))
        .AddColumn("Cost",    new double[] { 210, 260, 245, 290 }, cfg => cfg
            .Color(ChartColor.ChartOrange).BorderColor("#7a3a00").BorderWidth(2).BorderRadius(3))
        .AddColumn("Profit",  new double[] { 110, 150, 145, 190 }, cfg => cfg
            .Color(ChartColor.ChartGreen).BorderColor("#1e7a00").BorderWidth(2).BorderRadius(0)))
    .RenderToSvg();
```

**Output:** Three grouped column series. Revenue has rounded tops with a blue border; Cost has a dark orange border with subtle corners; Profit has a dark green thick border and sharp square corners.

---

## Scatter Marker Styling

```csharp
.AddScatter("Group A", data, cfg => cfg
    .Color(ChartColor.ChartBlue)          // dot fill
    .BorderColor("#e6b800")    // gold ring around dot
    .BorderWidth(2)            // ring thickness
    .MarkerSize(6))            // dot radius (px)
```

**Output:** Sky-blue dots with a gold ring border. Use this to differentiate overlapping points or to match corporate brand colours.

---

## Data Labels

Show a value label on or near each data point.

```csharp
// Enable on all series at once:
ChartBuilder.Create()
    .ShowDataLabels()
    .Series(s => s.AddColumn("Sales", data))

// Enable per-series with custom formatting:
.AddColumn("Budget", data, cfg => cfg
    .DataLabel
    .Show()
    .Format("${value}k")
    .Color("#1a6090")
    .FontSize(11)
    .Background("rgba(124,181,236,0.25)"))
```

**`DataLabelOptions` quick reference:**

| Method | Example | Result |
|---|---|---|
| `.Show()` | — | Enables labels |
| `.Format("{value}%")` | `38%` | Value with suffix |
| `.Format("${value}k")` | `$320k` | Currency format |
| `.Color(ChartColor.White)` | — | White text on coloured background |
| `.FontSize(11)` | — | Smaller text for dense charts |
| `.Background(ChartColor.ChartBlue)` | — | Solid pill background |
| `.Background("rgba(…,0.25)")` | — | Tinted transparent pill |
| `.Radius(1.25)` | — | Pie: label outside with connector |
| `.OffsetY(-10)` | — | Nudge label 10 px upward |

**Three label styles on one chart:**

```csharp
string svg = ChartBuilder.Create()
    .Title("Data Label Showcase")
    .Size(760, 460)
    .XAxis("Quarter", "Q1","Q2","Q3","Q4")
    .YAxis("Value ($k)", min: 0)
    .AsAnimated()
    .Series(s => s
        // Style 1: solid pill — white text on series colour
        .AddLine("Revenue", new double[] { 310, 420, 385, 510 }, cfg =>
        {
            cfg.Color(ChartColor.ChartBlue).LineWidth(3);
            cfg.DataLabel.Show().Color("#fff").FontSize(11)
               .Background(ChartColor.ChartBlue).Format("${value}k");
        })
        // Style 2: tinted transparent background
        .AddColumn("Expenses", new double[] { 180, 210, 195, 240 }, cfg =>
        {
            cfg.Color(ChartColor.ChartOrange);
            cfg.DataLabel.Show().Color("#7a2f00").FontSize(10)
               .Background("rgba(247,163,92,0.3)").Format("${value}k");
        })
        // Style 3: no background, larger font
        .AddSpline("Profit", new double[] { 130, 210, 190, 270 }, cfg =>
        {
            cfg.Color(ChartColor.ChartGreen).LineWidth(2);
            cfg.DataLabel.Show().Color("#1a5e00").FontSize(13)
               .Format("+{value}k").Background("#cfffcf");
        }))
    .RenderToSvg();
```

**Output:** Revenue labels appear as solid blue pills. Expense column labels float above each bar with a light orange tint. Profit spline labels use a green-tinted background with larger text.

---

## Pie / Donut Label Placement

Use `DataLabel.Radius(fraction)` to position slice labels:

| Fraction | Placement | Connector |
|---|---|---|
| `0.6` | Inside the slice | No |
| `1.0` | At the slice edge | No |
| `> 1.0` e.g. `1.25` | Outside the pie | Yes — spline connector |

```csharp
string svg = ChartBuilder.Create()
    .AsPie()
    .Title("Market Share")
    .Size(680, 480)
    .Labels("North America","Europe","Asia-Pacific","Lat. America","Rest of World")
    .Legend(l => l.AtBottom())
    .AsAnimated()
    .Series(s => s
        .Add("Share", new double[] { 38, 24, 20, 11, 7 }, cfg =>
            cfg.DataLabel.Show()
               .Radius(1.25)           // outside, with connector lines
               .Format("{value}%")
               .FontSize(11)
               .Color("#333333")
               .Background("rgba(255,255,255,0.85)")))
    .RenderToSvg();
```

**Output:** Each slice has a label outside the pie arc, connected to its slice by a thin curved spline line. Labels show "38%", "24%", etc. on a semi-transparent white pill.

---

## Custom Tooltip Styling

```csharp
string svg = ChartBuilder.Create()
    .Title("Tooltip — Dark Style")
    .Size(760, 440)
    .XAxis("Quarter", "Q1","Q2","Q3","Q4")
    .YAxis("USD ($k)", min: 0)
    .AsAnimated()
    .Tooltip(t => t
        .BackgroundColor("rgba(30,30,60,0.92)")
        .TextColor("#e8f4ff")
        .FontSize(12)
        .Border(ChartColor.ChartBlue, width: 1, radius: 6)
        .Padding(14)
        .Format("{label}: ${value}k")
        .TransitionDuration(0.20))
    .Series(s => s
        .AddLine("Revenue", new double[] { 320, 410, 390, 480 },
            cfg => cfg.Color(ChartColor.ChartBlue).LineWidth(3))
        .AddColumn("Cost", new double[] { 210, 260, 245, 290 },
            cfg => cfg.Color(ChartColor.ChartOrange)))
    .RenderToSvg();
```

**Output (Interactive / Animated mode):** When hovering a data point, a dark navy tooltip appears with a blue border, light text, and shows e.g. "Revenue: $410k". It fades in over 200 ms.

**Light tooltip without arrow:**

```csharp
.Tooltip(t => t
    .BackgroundColor("rgba(255,255,255,0.95)")
    .TextColor("#1a202c")
    .FontSize(11)
    .Border("#cbd5e0", width: 1, radius: 4)
    .Padding(12)
    .HideArrow()
    .Format("{label} — {value}k sessions"))
```

**Output:** White box with no triangular pointer, subtle grey border, dark text. Minimal and clean.

---

## Legend Styling

```csharp
// Vertical legend on the right side
.Legend(l => l
    .Vertical()
    .AlignRight()
    .AtMiddle()
    .Padding(12)
    .ItemFontSize(13)
    .ItemFontColor("#2d3748")
    .SymbolSize(14, 14)
    .SymbolRadius(7)             // fully circular swatch
    .Border("#cbd5e0", width: 1, radius: 6)
    .BackgroundColor("rgba(255,255,255,0.92)"))

// Horizontal legend at the top
.Legend(l => l
    .AtTop()
    .AlignCenter()
    .Horizontal()
    .Padding(8)
    .ItemFontSize(12)
    .Border("#4a5568", width: 1, radius: 4)
    .BackgroundColor("rgba(255,255,255,0.85)"))
```

**Output (vertical right):** A bordered legend box floats on the right side of the chart with circular coloured swatches and slightly larger font than the default.

**Output (top center):** A horizontal legend bar sits above the plot area with a subtle border, centred.
