# Recipes

Complete, compilable starting points. Pick the one that matches the destination, then swap in the chart from step 2 of the workflow.

All recipes assume:

```csharp
using TerraFluent.Chart.Reporting.Builder;
using TerraFluent.Chart.Reporting.Models;
using TerraFluent.Chart.Reporting.Enums;
```

---

## 1. Console app → SVG file (default for "generate a chart")

```bash
dotnet new console -n ChartGen && cd ChartGen
dotnet add package TerraFluent.Chart.Reporting
```

```csharp
// Program.cs
using TerraFluent.Chart.Reporting.Builder;
using TerraFluent.Chart.Reporting.Models;

var months  = new[] { "Jan", "Feb", "Mar", "Apr", "May", "Jun" };
var revenue = new double[] { 120, 145, 110, 190, 170, 210 };
var cost    = new double[] {  80,  90,  85, 120, 115, 130 };

Directory.CreateDirectory("out");

ChartBuilder.Create()
    .Title("Revenue vs Cost")
    .Subtitle("H1 2025, USD thousands")
    .Size(760, 420)
    .AsStatic()
    .XAxis("Month", months)
    .YAxis("USD (k)", min: 0)
    .PlotLine(150, ChartColor.Red, 2, "Target", "Dash")
    .Series(s => s
        .AddColumn("Revenue", revenue, cfg => cfg.Color(ChartColor.ChartBlue))
        .AddLine("Cost", cost, cfg => cfg.Color(ChartColor.ChartOrange).LineWidth(3)))
    .RenderToFile("out/revenue-vs-cost.svg");

Console.WriteLine("Wrote out/revenue-vs-cost.svg");
```

---

## 2. Chart from domain objects (LINQ → series)

```csharp
public sealed record Sale(string Region, int Month, decimal Amount);

public static class SalesCharts
{
    public static string RegionalTrend(IReadOnlyList<Sale> sales, SvgMode mode = SvgMode.Static)
    {
        var months  = Enumerable.Range(1, 12).ToArray();
        var labels  = months.Select(m => new DateTime(2025, m, 1).ToString("MMM")).ToArray();
        var regions = sales.Select(x => x.Region).Distinct().OrderBy(r => r).ToList();

        var builder = ChartBuilder.Create()
            .Title("Sales by Region")
            .Size(800, 420)
            .XAxis("Month", labels)
            .YAxis("Sales ($)", min: 0)
            .YAxisFormat("${value}")
            .Series(s =>
            {
                foreach (var region in regions)
                {
                    // One value per month, 0 when the region has no sales that month
                    double[] data = months
                        .Select(m => (double)sales.Where(x => x.Region == region && x.Month == m).Sum(x => x.Amount))
                        .ToArray();
                    s.AddLine(region, data);
                }
            });

        builder = mode switch
        {
            SvgMode.Interactive => builder.AsInteractive(),
            SvgMode.Animated    => builder.AsAnimated(),
            _                   => builder.AsStatic(),
        };
        return builder.RenderToSvg();
    }
}
```

---

## 3. ASP.NET Core Minimal API endpoint

```csharp
// Program.cs (web project)
using TerraFluent.Chart.Reporting.Builder;

var app = WebApplication.CreateBuilder(args).Build();

app.MapGet("/charts/kpi/{value:double}", (double value) =>
{
    string svg = ChartBuilder.Create()
        .Title("Server CPU")
        .Size(400, 320)
        .AsStatic()                                  // unknown consumer → static
        .YAxis(y => { y.Min = 0; y.Max = 100; })     // gauge scale
        .Series(s => s.AddGauge("CPU %", value))
        .RenderToSvg();

    return Results.Content(svg, "image/svg+xml");
});

app.Run();
```

In an MVC controller: `return Content(svg, "image/svg+xml");`

---

## 4. Blazor component

```razor
@* RevenueChart.razor *@
@using TerraFluent.Chart.Reporting.Builder
@using TerraFluent.Chart.Reporting.Models

<div class="chart-card">@((MarkupString)_svg)</div>

@code {
    [Parameter, EditorRequired] public double[] Values { get; set; } = default!;
    [Parameter, EditorRequired] public string[] Labels { get; set; } = default!;
    private string _svg = "";

    protected override void OnParametersSet() =>
        _svg = ChartBuilder.Create()
            .Title("Revenue")
            .Responsive()                   // fills the container width
            .Height(360)
            .AsAnimated()                   // SMIL + CSS, no JS: correct for Blazor
            .Theme(ChartTheme.Modern)
            .XAxis("Quarter", Labels)
            .YAxis("USD", min: 0)
            .Series(s => s.AddColumn("Revenue", Values, cfg => cfg.DataLabel.Show()))
            .RenderToSvg();
}
```

---

## 5. Multi-chart HTML report (static, print-friendly)

Use `Fork()` to share the base configuration across charts. Use `RenderToHtml` to get `<figure>` fragments with captions.

```csharp
var baseChart = ChartBuilder.Create()
    .Size(560, 340)
    .AsStatic()
    .Theme(ChartTheme.Business);

string trend = baseChart.Fork()
    .Title("Monthly Active Users")
    .XAxis("Month", "Jan", "Feb", "Mar", "Apr", "May", "Jun")
    .YAxis("Users", min: 0)
    .Series(s => s.AddArea("MAU", new double[] { 4200, 5100, 4800, 6300, 7200, 7900 },
        cfg => cfg.FillOpacity(0.3).AutoInsight(ai => ai.TrendLine().HighlightPeaks())))
    .RenderToHtml(caption: "Figure 1. MAU grew 88% in H1.");

string mix = baseChart.Fork()
    .Title("Revenue by Channel")
    .AsPie()
    .Labels("Direct", "Partners", "Marketplace", "Other")
    .Legend(l => l.BottomCenter())
    .Series(s => s.AddPie("Share", new double[] { 46, 27, 19, 8 }, cfg =>
    {
        cfg.DonutHole(0.55);
        cfg.DataLabel.Show().Format("{value}%");
        cfg.DonutCenter.Show().Title("Total");
    }))
    .RenderToHtml(caption: "Figure 2. Direct remains the largest channel.");

string html = $$"""
<!doctype html>
<html lang="en"><head><meta charset="utf-8"><title>H1 Report</title>
<style>
  body { font-family: Segoe UI, Arial, sans-serif; max-width: 1200px; margin: 2rem auto; }
  .grid { display: grid; grid-template-columns: repeat(auto-fit, minmax(520px, 1fr)); gap: 1.5rem; }
  @media print { .grid { display: block; } figure { break-inside: avoid; } }
</style></head>
<body><h1>H1 Report</h1><div class="grid">{{trend}}{{mix}}</div></body></html>
""";

Directory.CreateDirectory("out");
File.WriteAllText("out/report.html", html);
```

---

## 6. Interactive browser dashboard

```csharp
string svg = ChartBuilder.Create()
    .Title("Orders by Day")
    .Size(900, 440)
    .AsInteractive()                               // JS: browser only
    .Theme(ChartTheme.Dark)
    .XAxisDateTime(Enumerable.Range(0, 30).Select(i => new DateTime(2025, 6, 1).AddDays(i)), "dd MMM")
    .YAxis("Orders", min: 0)
    .Tooltip(t => t.EnableShared().ValueSuffix(" orders"))
    .ShowExportMenu("SVG", "PNG")
    .Series(s => s
        .AddSpline("Orders", Enumerable.Range(0, 30).Select(i => 200 + 40 * Math.Sin(i / 3.0) + i * 3).ToArray(),
            cfg => cfg.AutoInsight(ai => ai.AnomalyBands(1.5).NarrativeSummary())))
    .RenderToSvg();
```

---

## 7. Email body

Many email clients (Gmail, Outlook desktop) **block SVG images**. When the user needs email:

1. Render with `.AsStatic()`.
2. Inline the markup (`RenderToSvg()`) for clients that support inline SVG (Apple Mail, iOS), **or** tell the user that a PNG conversion step is needed for Gmail and Outlook. TerraFluent does not rasterise.

```csharp
string svg = ChartBuilder.Create()
    .Title("Weekly Signups").Size(600, 300).AsStatic()
    .XAxis("Week", "W1", "W2", "W3", "W4")
    .Series(s => s.AddColumn("Signups", new double[] { 320, 410, 390, 480 }))
    .RenderToSvg();
string body = $"<p>This week's signups:</p>{svg}";
```

---

## 8. Dependency injection

```csharp
// Registration: transient or scoped, NEVER singleton (builders are stateful)
builder.Services.AddTransient<IChartBuilder>(_ => ChartBuilder.Create());

public sealed class ReportService(IChartBuilder chart)
{
    public string Revenue(double[] values, string[] quarters) => chart
        .Title("Revenue")
        .AsStatic()
        .XAxis("Quarter", quarters)
        .Series(s => s.AddColumn("Revenue", values))
        .RenderToSvg();
}
```

---

## 9. Specialised types (data construction)

```csharp
// Waterfall: totals[i] = true marks absolute bars; the lengths must match
.XAxis("Step", "Opening", "Revenue", "COGS", "OpEx", "Net")
.Series(s => s.AddWaterfall("P&L",
    new double?[] { 500, 800, -320, -450, 530 },
    totals: new[] { true, false, false, false, true }))

// Heatmap: col indexes X categories, row indexes HeatmapRowLabels
.XAxis("Hour", "9", "10", "11")
.Series(s => s.AddHeatmap("Load",
    new[] { new HeatmapPoint(0, 0, 12), new HeatmapPoint(1, 0, 40), new HeatmapPoint(2, 0, 25),
            new HeatmapPoint(0, 1, 30), new HeatmapPoint(1, 1, 55), new HeatmapPoint(2, 1, 18) },
    cfg => { cfg.HeatmapRowLabels.AddRange(new[] { "Mon", "Tue" }); cfg.DataLabel.Show(); }))

// Gantt: numeric Start/End on the X axis
.XAxis(x => { x.Title = "Week"; x.Min = 0; x.Max = 12; })
.Series(s => s.AddGantt("Plan", new[]
{
    new GanttTask { Name = "Design", Start = 0, End = 3 },
    new GanttTask { Name = "Build",  Start = 2, End = 9, Color = ChartColor.ChartBlue },
    new GanttTask { Name = "Launch", Start = 9, End = 12, Label = "GA" },
}))

// Sankey: links refer to node indices
.Series(s => s.AddSankey("Flow",
    nodes: new[] { new SankeyNode { Name = "Visit" }, new SankeyNode { Name = "Trial" }, new SankeyNode { Name = "Paid" }, new SankeyNode { Name = "Churn" } },
    links: new[] { new SankeyLink { From = 0, To = 1, Value = 1000 }, new SankeyLink { From = 1, To = 2, Value = 380 }, new SankeyLink { From = 1, To = 3, Value = 620 } }))

// Candlestick
.XAxis("Day", "Mon", "Tue", "Wed")
.Series(s => s.AddCandlestick("ACME", new[] { new OhlcPoint(120, 128, 118, 126), new OhlcPoint(126, 130, 122, 123), new OhlcPoint(123, 133, 121, 132) }))

// Dual axis
.YAxis("Revenue ($k)", min: 0)
.YAxis2(y => { y.Title = "Margin (%)"; y.Min = 0; y.Max = 50; })
.Series(s => s
    .AddColumn("Revenue", new double[] { 310, 390, 420, 510 })
    .AddLine("Margin %", new double[] { 22, 28, 25, 31 }, cfg => cfg.OnSecondaryAxis()))
```
