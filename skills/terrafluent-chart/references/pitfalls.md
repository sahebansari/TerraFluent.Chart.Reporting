# Pitfalls and fixes

## Methods that do NOT exist (commonly hallucinated)

| Wrong | Right |
|---|---|
| `.DonutHolePercent(50)` | `cfg.DonutHole(0.5)` (a fraction from 0 to 1) |
| `cfg.DonutCenter("Total")` (as a method) | `cfg.DonutCenter.Show().Title("Total")` or `cfg.Center(c => c.Show().Title("Total"))` |
| `.XAxisTickInterval("every 5")` | `.XAxisTickInterval(5)` (a `double`) |
| `.Type(ChartType.Pie)`, `.ChartType(...)` | `.AsPie()` + `s.AddPie(...)` / `s.Add(...)`, or a typed `AddX` method |
| `.AddSeries(...)`, `.Data(...)` | `.Series(s => s.AddLine(name, data))` |
| `.Categories(...)` | `.XAxis("Title", "A", "B")` or `.Labels("A", "B")` |
| `.Render()`, `.ToSvg()`, `.Save()` | `.RenderToSvg()`, `.RenderToFile(path)` |
| `.ExportPng()`, `.RenderToPng()` | The library does not rasterise. Use a separate SVG→PNG tool, or the Interactive export menu in the browser |
| `.Legend(false)`, `.ShowLegend(false)` | `.HideLegend()` |
| `.Stacked(true)` | `.StackNormal()` / `.StackPercent()` |
| `.Theme("dark")` | `.Theme(ChartTheme.Dark)` |
| `new ChartBuilder()` | `ChartBuilder.Create()` |
| `cfg.Label.Show()` | `cfg.DataLabel.Show()` or `cfg.Label(dl => dl.Show())` |

## Rendering problems

| Symptom | Cause | Fix |
|---|---|---|
| Blank or partially drawn chart in PDF, email, or `<img>` | Animated/Interactive output relies on SMIL or JS | `.AsStatic()` |
| Tooltips, export menu, or legend toggle do nothing | Not in Interactive mode, or SVG loaded via `<img>` | `.AsInteractive()` and embed the SVG inline in the DOM |
| Tiny chart when rasterised | Responsive width (`width="100%"`) | `.Size(800, 450)` |
| Blazor shows escaped markup | String rendered as text | `@((MarkupString)svg)` |
| Wrong fonts in exported image | `<img>`-loaded SVG can't see page `@font-face` | Use a widely available font: `ChartTheme.Custom(fontFamily: "Segoe UI, Arial, sans-serif")` |
| Overlapping X labels | Too many or too long categories | `.LabelLayout(l => l.AutoRotate(45).AutoSkip())`, `.LabelLayout(l => l.Wrap(14))`, or switch to `AddBar` |

## Data problems

| Symptom | Cause | Fix |
|---|---|---|
| Scatter points evenly spaced | `AddScatter` places points by **category index**, not numeric X | Use `AddBubble` with `BubblePoint(x, y, z)` (use a constant `z` for uniform dots) |
| Series shorter or longer than the axis | `data.Length != categories.Length` | Align the lengths; use `double?[]` with `null` for missing values |
| Gaps in a line | `null` values | `cfg.NullGap(GapPolicy.Connect)` or `GapPolicy.Zero` |
| First waterfall bar wrong or invisible | `totals` not given | Pass `totals` with the same length as `data`; `true` marks absolute or total bars |
| Gauge or ring scale wrong | Scale comes from the Y axis | `.YAxis(y => { y.Min = 0; y.Max = 100; })` |
| Stacking ignored | Stacking applies only to Column, Bar, Area, and Line | Use one of those types |
| Pie labels missing | Labels come from `.Labels(...)` (or `XAxis` categories) | `.AsPie().Labels("A", "B", …)` |
| Heatmap rows unlabeled | Row labels are per series | `cfg => cfg.HeatmapRowLabels.AddRange(new[] { "Mon", "Tue" })` |
| Sankey throws or draws nothing | Link `From`/`To` are **indices** into `nodes` | Use 0-based indices and positive values |
| Decimal separator wrong | Culture | `.Culture("en-US")` / `.Culture("de-DE")` (only label text is localised; SVG geometry is invariant) |

## Architecture problems

- **Shared mutable builder**: `ChartBuilder` is stateful. Don't cache one in a static field or register it as a DI singleton. Use `Fork()` for variants.
- **Async on netstandard**: `RenderToFileAsync` and friends exist only on net6.0+. Use the sync overloads on netstandard targets.
- **Hard-coded data in views**: build charts in a service method that takes domain data, and keep the Razor/Blazor view to `@((MarkupString)svg)`.
- **Untrusted input**: titles and labels are escaped by the library. `OnPointClick(handler)` injects raw JavaScript, so never pass user input to it.
