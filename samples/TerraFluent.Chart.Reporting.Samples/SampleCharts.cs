using System.Text;
using TerraFluent.Chart.Reporting.Builder;
using TerraFluent.Chart.Reporting.Enums;
using TerraFluent.Chart.Reporting.Models;

namespace TerraFluent.Chart.Reporting.Samples;

/// <summary>
/// Chart-generating factory methods for the sample showcase. Each returns an SVG string (or a small
/// HTML fragment). Split out from <see cref="Program"/> so the orchestration file stays lean.
/// </summary>
internal static partial class Program
{
    // ================================================================== 01

    private static string LineChart() =>
        ChartBuilder.Create()
            .Theme(GlobalTheme)
            .Title("Monthly Website Visitors")
            .Subtitle("Jan \u2013 Dec 2025")
            .Size(700, 420)
            .XAxis(x =>
            {
                x.Title = "Month";
                x.Categories.AddRange(new[]
                    { "Jan", "Feb", "Mar", "Apr", "May", "Jun",
                      "Jul", "Aug", "Sep", "Oct", "Nov", "Dec" });
            })
            .YAxis(y => { y.Title = "Visitors"; y.Min = 0; })
            .AsAnimated()
            .Series(s => s.AddLine("Visitors", new double?[]
                { 12400, 14800, 16200, 18500, 21000, 19800,
                  22300, 25100, 23700, 20400, 17900, 15600 }))
            .RenderToSvg();

    // ================================================================== 02

    private static string MultiSeriesLine() =>
        ChartBuilder.Create()
            .Theme(GlobalTheme)
            .Title("Revenue vs Cost vs Profit")
            .Subtitle("Quarterly \u2014 FY 2025")
            .Size(700, 420)
            .XAxis(x =>
            {
                x.Title = "Quarter";
                x.Categories.AddRange(new[] { "Q1", "Q2", "Q3", "Q4" });
            })
            .YAxis(y => { y.Title = "USD (thousands)"; y.Min = 0; })
            .AsAnimated()
            .Series(s => s
                .AddLine("Revenue", new double?[] { 320, 410, 390, 480 })
                .AddLine("Cost",    new double?[] { 210, 260, 245, 290 })
                .AddLine("Profit",  new double?[] { 110, 150, 145, 190 }))
            .RenderToSvg();

    // ================================================================== 03

    private static string AreaChart() =>
        ChartBuilder.Create()
            .Theme(GlobalTheme)
            .Title("Daily Active Users")
            .Subtitle("Last 10 days")
            .Size(700, 420)
            .XAxis(x =>
            {
                x.Title = "Day";
                x.Categories.AddRange(new[]
                    { "Mon", "Tue", "Wed", "Thu", "Fri",
                      "Sat", "Sun", "Mon", "Tue", "Wed" });
            })
            .YAxis(y => { y.Title = "Users"; y.Min = 0; })
            .AsAnimated()
            .Series(s => s.AddArea("Active Users", new double?[]
                { 4200, 5100, 4800, 6300, 7200, 3800, 2900, 5500, 6100, 6800 }))
            .RenderToSvg();

    // ================================================================== 04

    private static string ColumnChart() =>
        ChartBuilder.Create()
            .Theme(GlobalTheme)
            .AsAnimated()
            .Title("Product Sales by Category")
            .Subtitle("FY 2025 Annual Totals")
            .Size(700, 420)
            .XAxis(x =>
            {
                x.Title = "Category";
                x.Categories.AddRange(new[]
                    { "Electronics", "Clothing", "Books", "Home", "Sports", "Toys" });
            })
            .YAxis(y => { y.Title = "Units Sold"; y.Min = 0; })
            .Series(s => s.AddColumn("Units Sold",
                new double?[] { 8400, 5200, 3100, 6700, 4300, 2800 }))
            .RenderToSvg();

    // ================================================================== 05

    private static string GroupedColumns() =>
        ChartBuilder.Create()
            .Theme(GlobalTheme)
            .Title("Budget vs Actual Spend")
            .Subtitle("Departmental \u2014 H1 2025")
            .Size(750, 420)
            .XAxis(x =>
            {
                x.Title = "Department";
                x.Categories.AddRange(new[]
                    { "Engineering", "Marketing", "Sales", "Operations", "HR" });
            })
            .YAxis(y => { y.Title = "USD (thousands)"; y.Min = 0; })
            .AsAnimated()
            .Series(s => s
                .AddColumn("Budget", new double?[] { 500, 200, 350, 280, 120 })
                .AddColumn("Actual", new double?[] { 470, 230, 310, 295, 108 }))
            .RenderToSvg();

    // ================================================================== 06

    private static string PieChart() =>
        ChartBuilder.Create()
            .Theme(GlobalTheme)
            .AsPie()
            .Title("Market Share by Vendor")
            .Subtitle("Global \u2014 Q4 2025")
            .Size(600, 440)
            .Labels("TerraFluent", "Competitor A", "Competitor B", "Others")
            .Legend(l => l.AtBottom())
            .AsAnimated()
            .Series(s => s.Add("Market Share", new double?[] { 38, 27, 21, 14 },
                cfg => cfg.DataLabel.Show().Radius(1.2)))
            .RenderToSvg();

    // ================================================================== 07

    private static string SplineChart() =>
        ChartBuilder.Create()
            .Theme(GlobalTheme)
            .Title("Temperature Trends")
            .Subtitle("Daily avg \u00b0C \u2014 Summer 2025")
            .AsSpline()
            .Size(700, 400)
            .XAxis(x =>
            {
                x.Title = "Week";
                x.Categories.AddRange(new[] { "W1", "W2", "W3", "W4", "W5", "W6", "W7", "W8" });
            })
            .YAxis(y => y.Title = "Temperature (\u00b0C)")
            .AsAnimated()
            .Series(s => s
                .Add("London",   new double?[] { 18, 20, 22, 25, 27, 24, 21, 19 })
                .Add("Madrid",   new double?[] { 28, 31, 34, 38, 40, 37, 33, 30 })
                .AddColumn("Helsinki", new double?[] { 14, 16, 18, 20, 22, 21, 17, 14 }))
            .RenderToSvg();

    // ================================================================== 08

    private static string MixedLineAndArea() =>
        ChartBuilder.Create()
            .Theme(GlobalTheme)
            .Title("Stock Price & Volume")
            .Subtitle("TFCR \u2014 Last 8 Weeks")
            .Size(750, 420)
            .XAxis(x =>
            {
                x.Title = "Week";
                x.Categories.AddRange(new[] { "W1", "W2", "W3", "W4", "W5", "W6", "W7", "W8" });
            })
            .YAxis(y => { y.Title = "Price / Volume"; y.Min = 0; })
            .AsAnimated()
            .Series(s => s
                .AddArea("Volume", new double?[] { 1200, 980, 1450, 1100, 1600, 1350, 900, 1250 })
                .AddLine("Price",  new double?[] { 42, 44, 41, 46, 49, 47, 43, 48 },
                    cfg => cfg.LineWidth(3)))
            .RenderToSvg();

    // ================================================================== 09

    private static string StaticMode() =>
        ChartBuilder.Create()
            .Theme(GlobalTheme)
            .Title("Annual Report \u2014 Revenue Summary")
            .Subtitle("Static SVG \u2014 safe for PDF and email")
            .Size(700, 400)
            .XAxis(x => x.Categories.AddRange(new[] { "Q1", "Q2", "Q3", "Q4" }))
            .YAxis(y => { y.Title = "Revenue ($k)"; y.Min = 0; })
            .AsStatic()
            .DisableAnimation()
            .Series(s => s
                .AddColumn("Revenue", new double?[] { 320, 410, 390, 480 })
                .AddLine("Target",    new double?[] { 300, 380, 420, 450 }))
            .RenderToSvg();

    // ================================================================== 10

    private static string InteractiveMode() =>
        ChartBuilder.Create()
            .Theme(GlobalTheme)
            .Title("Interactive Sales Dashboard")
            .Subtitle("Hover over data points \u00b7 Click to highlight")
            .Size(750, 440)
            .XAxis(x =>
            {
                x.Title = "Month";
                x.Categories.AddRange(new[] { "Jan", "Feb", "Mar", "Apr", "May", "Jun" });
            })
            .YAxis(y => { y.Title = "USD (thousands)"; y.Min = 0; })
            .AsInteractive()
            .Series(s => s
                .AddLine("Online Sales",   new double?[] { 180, 210, 190, 260, 310, 280 })
                .AddLine("In-Store Sales", new double?[] { 140, 160, 175, 200, 195, 220 })
                .AddArea("Total",          new double?[] { 320, 370, 365, 460, 505, 500 }))
            .RenderToSvg();

    // ================================================================== 11

    private static string SplineSmooth() =>
        ChartBuilder.Create()
            .Theme(GlobalTheme)
            .Title("Smooth Spline \u2014 Catmull-Rom B\u00e9zier")
            .Subtitle("ChartType.Spline uses cubic B\u00e9zier control points")
            .Size(700, 400)
            .XAxis(x =>
            {
                x.Title = "Week";
                x.Categories.AddRange(new[] { "W1", "W2", "W3", "W4", "W5", "W6", "W7", "W8" });
            })
            .YAxis(y => y.Title = "Temperature (\u00b0C)")
            .AsAnimated()
            .Series(s => s
                .AddSpline("London",   new double?[] { 18, 20, 22, 25, 27, 24, 21, 19 })
                .AddSpline("Madrid",   new double?[] { 28, 31, 34, 38, 40, 37, 33, 30 })
                .AddSpline("Helsinki", new double?[] { 14, 16, 18, 20, 22, 21, 17, 14 }))
            .RenderToSvg();

    // ================================================================== 12

    private static string HorizontalBar() =>
        ChartBuilder.Create()
            .Theme(GlobalTheme)
            .Title("Population by City")
            .Subtitle("Horizontal Bar \u2014 ChartType.Bar")
            .Size(700, 420)
            .XAxis(x => x.Categories.AddRange(
                new[] { "Tokyo", "Delhi", "Shanghai", "S\u00e3o Paulo", "Mexico City", "Cairo" }))
            .YAxis(y => { y.Title = "Population (M)"; y.Min = 0; y.Max = 40; })
            .AsAnimated()
            .Series(s => s.AddBar("Population (M)",
                new double?[] { 37.4, 32.9, 27.1, 22.4, 21.9, 21.3 }))
            .RenderToSvg();

    // ================================================================== 13

    private static string ScatterChart() =>
        ChartBuilder.Create()
            .Theme(GlobalTheme)
            .Title("Height vs Weight Correlation")
            .Subtitle("ChartType.Scatter \u2014 dots only, no connecting line")
            .Size(700, 420)
            .XAxis(x => x.Title = "Index")
            .YAxis(y => { y.Title = "Value"; y.Min = 0; })
            .AsAnimated()
            .Series(s => s
                .AddScatter("Group A", new double?[] { 62, 58, 74, 55, 80, 67, 71, 59, 77, 63 })
                .AddScatter("Group B", new double?[] { 45, 52, 61, 48, 57, 70, 43, 65, 53, 68 }))
            .RenderToSvg();

    // ================================================================== 14

    private static string AnimatedColumn() =>
        ChartBuilder.Create()
            .Theme(GlobalTheme)
            .Title("Quarterly Revenue \u2014 Grow Animation")
            .Subtitle("Bars animate from baseline on load (SMIL <animate>)")
            .Size(700, 420)
            .XAxis(x =>
            {
                x.Title = "Quarter";
                x.Categories.AddRange(new[] { "Q1", "Q2", "Q3", "Q4" });
            })
            .YAxis(y => { y.Title = "Revenue ($k)"; y.Min = 0; })
            .AsAnimated()
            .Animate(900)
            .Series(s => s
                .AddColumn("2024", new double?[] { 310, 390, 420, 510 })
                .AddColumn("2025", new double?[] { 340, 430, 460, 540 }))
            .RenderToSvg();

    // ================================================================== 15

    private static string DarkTheme() =>
        ChartBuilder.Create()
            .Theme(ChartTheme.Dark)
            .Title("Server Throughput")
            .Subtitle("Dark theme \u2014 browser / dashboard use")
            .Size(700, 420)
            .XAxis(x =>
            {
                x.Title = "Hour";
                x.Categories.AddRange(new[] { "00", "04", "08", "12", "16", "20", "24" });
            })
            .YAxis(y => { y.Title = "Req/s"; y.Min = 0; })
            .AsAnimated()
            .Series(s => s
                .AddArea("API",   new double?[] { 820, 430, 1200, 1800, 1650, 1100, 640 })
                .AddLine("Cache", new double?[] { 600, 310,  900, 1300, 1200,  850, 480 }))
            .RenderToSvg();

    // ================================================================== 16

    private static string PastelTheme() =>
        ChartBuilder.Create()
            .Theme(ChartTheme.Pastel)
            .Title("Monthly Sales")
            .Subtitle("Pastel theme \u2014 soft presentation style")
            .Size(700, 420)
            .XAxis(x => x.Categories.AddRange(new[] { "Jan", "Feb", "Mar", "Apr", "May", "Jun" }))
            .YAxis(y => { y.Title = "Units"; y.Min = 0; })
            .AsAnimated()
            .Series(s => s
                .AddColumn("Product A", new double?[] { 420, 380, 510, 490, 560, 600 })
                .AddColumn("Product B", new double?[] { 310, 290, 370, 400, 430, 450 }))
            .RenderToSvg();

    // ================================================================== 17

    private static string DataLabels() =>
        ChartBuilder.Create()
            .Theme(GlobalTheme)
            .Title("Budget vs Actual \u2014 with Data Labels")
            .Subtitle("ShowDataLabels on column series")
            .Size(700, 440)
            .XAxis(x => x.Categories.AddRange(new[] { "Eng", "Mktg", "Sales", "Ops", "HR" }))
            .YAxis(y => { y.Title = "USD ($k)"; y.Min = 0; })
            .AsAnimated()
            .Series(s => s
                .AddColumn("Budget", new double?[] { 500, 200, 350, 280, 120 },
                    cfg => cfg.DataLabel.Show().FontSize(11))
                .AddColumn("Actual", new double?[] { 470, 230, 310, 295, 108 },
                    cfg => cfg.DataLabel.Show().FontSize(11)))
            .RenderToSvg();

    // ================================================================== 18

    private static string DonutChart() =>
        ChartBuilder.Create()
            .Theme(GlobalTheme)
            .AsPie()
            .Title("Revenue by Region \u2014 Donut")
            .Subtitle("DonutHolePercent = 0.55 \u00b7 DonutCenter shows auto total")
            .Size(620, 460)
            .Labels("North America", "Europe", "Asia-Pacific", "Rest of World")
            .Legend(l => l.AtBottom())
            .AsAnimated()
            .Series(s => s.Add("Revenue %", new double?[] { 42, 28, 22, 8 },
                cfg =>
                {
                    cfg.DonutHole(0.55);
                    cfg.DataLabel.Show().Radius(1).Format("{value}%");
                    cfg.DonutCenter.Show().Title("Total");
                }))
            .RenderToSvg();

    // ================================================================== 19

    private static string StackedColumns() =>
        ChartBuilder.Create()
            .Theme(GlobalTheme)
            .Title("Revenue Stack \u2014 Normal")
            .Subtitle("Stacking.Normal \u2014 see cumulative totals clearly")
            .Size(700, 420)
            .StackNormal()
            .XAxis(x => x.Categories.AddRange(new[] { "Q1", "Q2", "Q3", "Q4" }))
            .YAxis(y => { y.Title = "USD ($k)"; y.Min = 0; })
            .AsAnimated()
            .Animate()
            .Series(s => s
                .AddColumn("Product A", new double?[] { 180, 220, 240, 280 })
                .AddColumn("Product B", new double?[] { 140, 160, 175, 200 })
                .AddColumn("Product C", new double?[] {  90, 110, 130, 150 }))
            .RenderToSvg();

    // ================================================================== 20

    private static string StackedPercent() =>
        ChartBuilder.Create()
            .Theme(GlobalTheme)
            .Title("Revenue Mix \u2014 100 % Stacked")
            .Subtitle("Stacking.Percent \u2014 proportional contribution per quarter")
            .Size(700, 420)
            .StackPercent()
            .XAxis(x => x.Categories.AddRange(new[] { "Q1", "Q2", "Q3", "Q4" }))
            .YAxis(y => { y.Title = "%"; y.Min = 0; y.Max = 100; y.TickInterval = 25; y.LabelFormat = "{value}%"; })
            .AsAnimated()
            .Series(s => s
                .AddColumn("Product A", new double?[] { 180, 220, 240, 280 },
                    cfg => cfg.DataLabel.Show().Format("{value}%"))
                .AddColumn("Product B", new double?[] { 140, 160, 175, 200 },
                    cfg => cfg.DataLabel.Show().Format("{value}%"))
                .AddColumn("Product C", new double?[] {  90, 110, 130, 150 },
                    cfg => cfg.DataLabel.Show().Format("{value}%")))
            .RenderToSvg();

    // ================================================================== 21

    private static string StackedArea() =>
        ChartBuilder.Create()
            .Theme(GlobalTheme)
            .Title("Monthly Traffic by Channel")
            .Subtitle("Stacking.Normal on Area \u2014 cumulative contributions")
            .Size(720, 420)
            .StackNormal()
            .XAxis(x =>
            {
                x.Title = "Month";
                x.Categories.AddRange(new[] {
                    "Jan","Feb","Mar","Apr","May","Jun",
                    "Jul","Aug","Sep","Oct","Nov","Dec" });
            })
            .YAxis(y => { y.Title = "Sessions"; y.Min = 0; })
            .AsAnimated()
            .Animate()
            .Series(s => s
                .AddArea("Organic",  new double?[] { 1200,1350,1500,1700,1900,2100,2300,2200,2000,1800,1600,1400 })
                .AddArea("Direct",   new double?[] {  800, 850, 900, 950,1000,1100,1200,1150,1050, 950, 850, 750 })
                .AddArea("Referral", new double?[] {  400, 420, 440, 480, 520, 560, 600, 580, 540, 500, 460, 420 }))
            .RenderToSvg();

    // ================================================================== 22

    private static string SecondaryYAxis() =>
        ChartBuilder.Create()
            .Theme(GlobalTheme)
            .Title("Revenue & Profit Margin")
            .Subtitle("WithYAxis2() \u2014 columns on left axis, line on right axis")
            .Size(720, 420)
            .XAxis(x =>
            {
                x.Title = "Quarter";
                x.Categories.AddRange(new[] {
                    "Q1 2024","Q2 2024","Q3 2024","Q4 2024",
                    "Q1 2025","Q2 2025","Q3 2025","Q4 2025" });
            })
            .YAxis(y => { y.Title = "Revenue ($k)"; y.Min = 0; })
            .YAxis2(y => { y.Title = "Margin (%)"; y.Min = 0; y.Max = 50; })
            .AsAnimated()
            .Series(s => s
                .AddColumn("Revenue",  new double?[] { 310, 390, 420, 510, 340, 430, 460, 540 })
                .AddLine("Margin %",   new double?[] {  22,  28,  25,  31,  24,  32,  30,  35 },
                    cfg => { cfg.LineWidth(3); cfg.OnSecondaryAxis(); }))
            .RenderToSvg();

    // ================================================================== 23

    private static string PlotBandsAndLines() =>
        ChartBuilder.Create()
            .Theme(GlobalTheme)
            .Title("Server Response Time")
            .Subtitle("YAxis.PlotBands / PlotLines \u2014 target zones and thresholds")
            .Size(720, 420)
            .XAxis(x =>
            {
                x.Title = "Hour";
                x.Categories.AddRange(new[] {
                    "00","02","04","06","08","10","12","14","16","18","20","22" });
            })
            .YAxis(y =>
            {
                y.Title = "ms";
                y.Min = 0;
                y.PlotBands.Add(new PlotBand { From = 0,   To = 200, Color = "rgba(144,237,125,0.15)", Label = "Good" });
                y.PlotBands.Add(new PlotBand { From = 200, To = 500, Color = "rgba(247,163,92,0.15)", Label = "Acceptable" });
                y.PlotLines.Add(new PlotLine { Value = 500, Color = ChartColor.ChartRed, Width = 2, DashStyle = "Dash", Label = "SLA limit" });
            })
            .AsAnimated()
            .Series(s => s
                .AddLine("p50", new double?[] {  85, 78, 72,  90, 145, 220, 310, 280, 195, 160, 130, 105 })
                .AddLine("p99", new double?[] { 210,185,170, 240, 420, 610, 780, 720, 490, 380, 290, 230 },
                    cfg => cfg.Dashed()))
            .RenderToSvg();

    // ================================================================== 24

    private static string WaterfallDemo() =>
        ChartBuilder.Create()
            .Theme(GlobalTheme)
            .Title("Annual Cash Flow Analysis")
            .Subtitle("Waterfall \u2014 cumulative P&L")
            .Size(720, 440)
            .XAxis(x => x.Categories.AddRange(new[]
            {
                "Opening","Revenue","COGS","Gross Profit",
                "OpEx","EBITDA","Tax","Net Profit"
            }))
            .YAxis(y => y.Title = "USD ($k)")
            .AsAnimated()
            .Animate()
            .Series(s => s.AddWaterfall(
                name: "P&L",
                data: new double?[] { 500, 800, -320, 980, -450, 530, -120, 410 },
                totals: new[] { true, false, false, true, false, true, false, true }))
            .RenderToSvg();

    // ================================================================== 25

    private static string GaugeDemo() =>
        ChartBuilder.Create()
            .Theme(GlobalTheme)
            .Title("Server CPU Utilisation")
            .Subtitle("Gauge / Radial \u2014 single-value semi-circular dial")
            .Size(460, 360)
            .YAxis(y => { y.Min = 0; y.Max = 100; })
            .AsAnimated()
            .Series(s => s.AddGauge("CPU %", 67))
            .RenderToSvg();

    // ================================================================== 26

    private static string LabelRotationDemo() =>
        ChartBuilder.Create()
            .Theme(GlobalTheme)
            .Title("Monthly Sales by Product")
            .Subtitle("XAxis.LabelRotation = -45 \u2014 diagonal labels")
            .Size(720, 420)
            .XAxis(x =>
            {
                x.LabelRotation = -45;
                x.Title = "Product";
                x.Categories.AddRange(new[]
                {
                    "Premium Enterprise Suite", "Standard Business Pack",
                    "Developer Toolkit Pro", "Mobile Platform SDK",
                    "Analytics Dashboard", "Security Module Plus",
                    "Integration Gateway"
                });
            })
            .YAxis(y => { y.Title = "Units"; y.Min = 0; })
            .AsAnimated()
            .Series(s => s
                .AddColumn("Q1", new double?[] { 420, 380, 510, 290, 340, 210, 480 })
                .AddColumn("Q2", new double?[] { 460, 410, 530, 310, 380, 240, 520 }))
            .RenderToSvg();

    // ================================================================== 27

    private static string DataLabelShowcase() =>
        ChartBuilder.Create()
            .Theme(GlobalTheme)
            .Title("Data Label Feature Showcase")
            .Subtitle("Per-series: FontSize \u00b7 Format")
            .Size(760, 460)
            .XAxis(x =>
            {
                x.Title = "Quarter";
                x.Categories.AddRange(new[] { "Q1", "Q2", "Q3", "Q4" });
            })
            .YAxis(y => { y.Title = "Value ($k)"; y.Min = 0; })
            .AsAnimated()
            .Animate()
            .Series(s => s
                .AddLine("Revenue", new double?[] { 310, 420, 385, 510 },
                    cfg =>
                    {
                        cfg.LineWidth(3);
                        cfg.DataLabel.Show().FontSize(11).Format("${value}k");
                    })
                .AddColumn("Expenses", new double?[] { 180, 210, 195, 240 },
                    cfg => cfg.DataLabel.Show().FontSize(10).Format("${value}k").OffsetY(-50))
                .AddSpline("Profit", new double?[] { 130, 210, 190, 270 },
                    cfg =>
                    {
                        cfg.LineWidth(2);
                        cfg.DataLabel.Show().FontSize(13).Format("+{value}k");
                    }))
            .RenderToSvg();

    // ================================================================== 28

    private static string PieLabelPlacement() =>
        ChartBuilder.Create()
            .Theme(GlobalTheme)
            .AsPie()
            .Title("Pie \u2014 Data Label Placement")
            .Subtitle("DataLabelRadius: 0.6 (inside) \u00b7 1.0 (edge) \u00b7 1.25 (outside + connector)")
            .Size(680, 480)
            .Labels("North America", "Europe", "Asia-Pacific", "Lat. America", "Rest of World")
            .Legend(l => l.AtBottom().AlignCenter())
            .AsAnimated()
            .Series(s => s.Add("Share", new double?[] { 38, 24, 20, 11, 7 },
                cfg => cfg.DataLabel.Show().Radius(1.25).Format("{value}%").FontSize(11)))
            .RenderToSvg();

    // ================================================================== 29

    private static string LegendVerticalRight() =>
        ChartBuilder.Create()
            .Theme(GlobalTheme)
            .Title("Revenue vs Cost vs Profit")
            .Subtitle("Legend: Vertical \u00b7 AlignRight \u00b7 circular swatches")
            .Size(760, 420)
            .XAxis(x =>
            {
                x.Title = "Quarter";
                x.Categories.AddRange(new[] { "Q1", "Q2", "Q3", "Q4" });
            })
            .YAxis(y => { y.Title = "USD ($k)"; y.Min = 0; })
            .AsAnimated()
            .Legend(l => l
                .Vertical()
                .AlignRight()
                .AtMiddle()
                .Padding(12)
                .ItemFontSize(13)
                .SymbolSize(14, 14)
                .SymbolRadius(7))
            .Series(s => s
                .AddLine("Revenue", new double?[] { 320, 410, 390, 480 })
                .AddLine("Cost",    new double?[] { 210, 260, 245, 290 })
                .AddLine("Profit",  new double?[] { 110, 150, 145, 190 }))
            .RenderToSvg();

    // ================================================================== 30

    private static string LegendTopCenter() =>
        ChartBuilder.Create()
            .Theme(GlobalTheme)
            .Title("Monthly Traffic by Channel")
            .Subtitle("Legend: AtTop \u00b7 AlignCenter \u00b7 Horizontal \u00b7 Padding")
            .Size(760, 440)
            .XAxis(x =>
            {
                x.Title = "Month";
                x.Categories.AddRange(new[]
                    { "Jan","Feb","Mar","Apr","May","Jun",
                      "Jul","Aug","Sep","Oct","Nov","Dec" });
            })
            .YAxis(y => { y.Title = "Sessions"; y.Min = 0; })
            .AsAnimated()
            .Legend(l => l
                .TopCenter()
                .Horizontal()
                .Padding(8)
                .ItemFontSize(12))
            .Series(s => s
                .AddArea("Organic",  new double?[] { 1200,1350,1500,1700,1900,2100,2300,2200,2000,1800,1600,1400 })
                .AddArea("Direct",   new double?[] {  800, 850, 900, 950,1000,1100,1200,1150,1050, 950, 850, 750 })
                .AddLine("Referral", new double?[] {  400, 420, 440, 480, 520, 560, 600, 580, 540, 500, 460, 420 },
                    cfg => cfg.LineWidth(2)))
            .RenderToSvg();

    // ================================================================== 31

    private static string LegendBottomLeft() =>
        ChartBuilder.Create()
            .Theme(GlobalTheme)
            .Title("Budget vs Actual vs Forecast")
            .Subtitle("Legend: AtBottom \u00b7 AlignLeft \u00b7 large symbols \u00b7 pixel offset")
            .Size(760, 440)
            .XAxis(x =>
            {
                x.Title = "Department";
                x.Categories.AddRange(new[]
                    { "Engineering","Marketing","Sales","Operations","HR" });
            })
            .YAxis(y => { y.Title = "USD ($k)"; y.Min = 0; })
            .AsAnimated()
            .Legend(l => l
                .BottomLeft()
                .Horizontal()
                .Padding(10)
                .ItemFontSize(12)
                .SymbolSize(16, 10)
                .SymbolRadius(2)
                .Offset(x: 8, y: 4))
            .Series(s => s
                .AddColumn("Budget", new double?[] { 500, 200, 350, 280, 120 })
                .AddColumn("Actual", new double?[] { 470, 230, 310, 295, 108 })
                .AddLine("Forecast", new double?[] { 490, 215, 330, 290, 115 },
                    cfg => { cfg.Dashed(); cfg.LineWidth(2); }))
            .RenderToSvg();

    // ================================================================== 32

    private static string ColumnBorderRadius() =>
        ChartBuilder.Create()
            .Theme(GlobalTheme)
            .Title("Series Formatting \u2014 Border & Corner Radius")
            .Subtitle("BorderWidth \u00b7 BorderRadius applied independently per column series")
            .Size(760, 440)
            .XAxis(x => x.Categories.AddRange(new[] { "Q1", "Q2", "Q3", "Q4" }))
            .YAxis(y => { y.Title = "Value ($k)"; y.Min = 0; })
            .AsAnimated()
            .Legend(l => l.AtBottom().AlignCenter().Padding(8))
            .Series(s => s
                .AddColumn("Revenue", new double?[] { 320, 410, 390, 480 }, cfg =>
                {
                    cfg.BorderWidth(1);
                    cfg.BorderRadius(6);
                })
                .AddColumn("Cost", new double?[] { 210, 260, 245, 290 }, cfg =>
                {
                    cfg.BorderWidth(2);
                    cfg.BorderRadius(3);
                })
                .AddColumn("Profit", new double?[] { 110, 150, 145, 190 }, cfg =>
                {
                    cfg.BorderWidth(2);
                    cfg.BorderRadius(0);
                }))
            .RenderToSvg();

    // ================================================================== 33

    private static string ScatterMarkerBorder() =>
        ChartBuilder.Create()
            .Theme(GlobalTheme)
            .Title("Scatter \u2014 Custom Marker Borders")
            .Subtitle("Series.BorderWidth on scatter dot markers")
            .Size(700, 420)
            .XAxis(x => x.Title = "Index")
            .YAxis(y => { y.Title = "Value"; y.Min = 30; y.Max = 90; })
            .AsAnimated()
            .Series(s => s
                .AddScatter("Group A", new double?[] { 62, 58, 74, 55, 80, 67, 71, 59, 77, 63 },
                    cfg => cfg.BorderWidth(2))
                .AddScatter("Group B", new double?[] { 45, 52, 61, 48, 57, 70, 43, 65, 53, 68 },
                    cfg => cfg.BorderWidth(2)))
            .RenderToSvg();

    // ================================================================== 34

    private static string AreaFillOpacity() =>
        ChartBuilder.Create()
            .Theme(GlobalTheme)
            .Title("Area \u2014 Fill Opacity")
            .Subtitle("Series.FillOpacity: 0.15 (Organic) \u00b7 0.40 (Direct) \u00b7 0.70 (Referral)")
            .Size(760, 440)
            .XAxis(x =>
            {
                x.Title = "Month";
                x.Categories.AddRange(new[]
                    { "Jan","Feb","Mar","Apr","May","Jun",
                      "Jul","Aug","Sep","Oct","Nov","Dec" });
            })
            .YAxis(y => { y.Title = "Sessions"; y.Min = 0; })
            .AsAnimated()
            .Series(s => s
                .AddArea("Organic",  new double?[] { 1200,1350,1500,1700,1900,2100,2300,2200,2000,1800,1600,1400 },
                    cfg => cfg.FillOpacity(0.15))
                .AddArea("Direct",   new double?[] {  800, 850, 900, 950,1000,1100,1200,1150,1050, 950, 850, 750 },
                    cfg => cfg.FillOpacity(0.40))
                .AddArea("Referral", new double?[] {  400, 420, 440, 480, 520, 560, 600, 580, 540, 500, 460, 420 },
                    cfg => cfg.FillOpacity(0.70)))
            .RenderToSvg();

    // ================================================================== 35

    private static string TooltipCustomStyle() =>
        ChartBuilder.Create()
            .Theme(GlobalTheme)
            .Title("Tooltip \u2014 Custom Style")
            .Subtitle("TooltipBuilder: FontSize \u00b7 Padding \u00b7 Format \u00b7 TransitionDuration")
            .Size(760, 440)
            .XAxis(x =>
            {
                x.Title = "Quarter";
                x.Categories.AddRange(new[] { "Q1", "Q2", "Q3", "Q4" });
            })
            .YAxis(y => { y.Title = "USD ($k)"; y.Min = 0; })
            .AsAnimated()
            .Tooltip(t => t
                .FontSize(12)
                .Padding(14)
                .Format("{label}: ${value}k")
                .TransitionDuration(0.20))
            .Series(s => s
                .AddLine("Revenue",  new double?[] { 320, 410, 390, 480 }, cfg => cfg.LineWidth(3))
                .AddColumn("Expenses", new double?[] { 210, 260, 245, 290 })
                .AddLine("Profit",   new double?[] { 110, 150, 145, 190 }, cfg => cfg.Dashed()))
            .RenderToSvg();

    // ================================================================== 36

    private static string TooltipNoArrow() =>
        ChartBuilder.Create()
            .Theme(GlobalTheme)
            .Title("Tooltip \u2014 No Arrow")
            .Subtitle("HideArrow() \u00b7 custom Format \u00b7 fast transition")
            .Size(760, 440)
            .XAxis(x =>
            {
                x.Title = "Month";
                x.Categories.AddRange(new[]
                    { "Jan","Feb","Mar","Apr","May","Jun" });
            })
            .YAxis(y => { y.Title = "Sessions (k)"; y.Min = 0; })
            .AsAnimated()
            .Tooltip(t => t
                .FontSize(11)
                .Padding(12)
                .HideArrow()
                .Format("{label} \u2014 {value}k sessions")
                .TransitionDuration(0.10))
            .Series(s => s
                .AddArea("Organic", new double?[] { 4.2, 4.8, 5.1, 5.6, 6.0, 5.7 },
                    cfg => cfg.FillOpacity(0.25))
                .AddLine("Paid",    new double?[] { 1.8, 2.1, 1.9, 2.4, 2.8, 2.6 },
                    cfg => cfg.LineWidth(2)))
            .RenderToSvg();

    // ================================================================== 37

    private static string DonutCenterLabel() =>
        ChartBuilder.Create()
            .Theme(GlobalTheme)
            .AsPie()
            .Title("Donut \u2014 Center Label Showcase")
            .Subtitle("DonutCenter: auto total \u00b7 title caption \u00b7 FontSize")
            .Size(640, 480)
            .Labels("Cloud", "On-Premise", "Hybrid", "Managed Service")
            .Legend(l => l.AtBottom().AlignCenter().Padding(8))
            .AsAnimated()
            .Tooltip(t => t.Format("{label}: {value}%"))
            .Series(s => s.Add("Deployment Mix", new double?[] { 45, 25, 20, 10 },
                cfg =>
                {
                    cfg.DonutHole(0.60);
                    cfg.DataLabel.Show().Radius(1.2).Format("{value}%").FontSize(11);
                    cfg.DonutCenter
                        .Show()
                        .Title("Total %")
                        .FontSize(28)
                        .TitleFontSize(12);
                }))
            .RenderToSvg();

    // ================================================================== 38

    private static string MonthlySalesDonut() =>
        ChartBuilder.Create()
            .Theme(GlobalTheme)
            .AsPie()
            .Title("Monthly Sales by Product Line")
            .Subtitle("April 2025 \u2014 Total sales in center")
            .Size(660, 500)
            .Labels("Software", "Hardware", "Services", "Support", "Training")
            .Legend(l => l
                .AtBottom()
                .AlignCenter()
                .Padding(10)
                .ItemFontSize(12))
            .AsInteractive()
            .Tooltip(t => t.Format("{label}: ${value}k"))
            .Series(s => s.Add("Sales", new double?[] { 485, 320, 275, 145, 75 },
                cfg =>
                {
                    cfg.DonutHole(0.58);
                    cfg.DataLabel
                        .Show()
                        .Radius(1.22)
                        .Format("${value}k")
                        .FontSize(11);
                    cfg.DonutCenter
                        .Show("$1.3M")
                        .Title("Total Sales")
                        .FontSize(28)
                        .TitleFontSize(12);
                }))
            .RenderToSvg();

    // ================================================================== 39

    private static string DataRingKpi() =>
        ChartBuilder.Create()
            .Theme(GlobalTheme)
            .Title("Q2 2025 \u2014 Customer Satisfaction")
            .Subtitle("DataRing: sweep-in animation \u00b7 centre value \u00b7 title caption")
            .Size(400, 400)
            .YAxis(y => { y.Min = 0; y.Max = 100; })
            .AsAnimated()
            .Tooltip(t => t.Format("{label}: {value}%"))
            .Series(s => s.AddDataRing("CSAT Score", 87, cfg =>
                cfg.DonutCenter
                    .Title("CSAT")
                    .FontSize(42)
                    .TitleFontSize(14)))
            .RenderToSvg();

    // ================================================================== 40

    private static string DataRingDashboard()
    {
        string ring1 = ChartBuilder.Create()
            .Theme(GlobalTheme)
            .Title("CPU Usage")
            .Size(300, 300)
            .YAxis(y => { y.Min = 0; y.Max = 100; })
            .AsAnimated()
            .Series(s => s.AddDataRing("CPU", 67,
                cfg => cfg.DonutCenter.Title("CPU").FontSize(32)))
            .RenderToSvg();

        string ring2 = ChartBuilder.Create()
            .Theme(GlobalTheme)
            .Title("Memory")
            .Size(300, 300)
            .YAxis(y => { y.Min = 0; y.Max = 100; })
            .AsAnimated()
            .Series(s => s.AddDataRing("RAM", 82,
                cfg => cfg.DonutCenter.Title("RAM").FontSize(32)))
            .RenderToSvg();

        string ring3 = ChartBuilder.Create()
            .Theme(GlobalTheme)
            .Title("Disk I/O")
            .Size(300, 300)
            .YAxis(y => { y.Min = 0; y.Max = 100; })
            .AsAnimated()
            .Series(s => s.AddDataRing("Disk", 34,
                cfg => cfg.DonutCenter.Title("Disk").FontSize(32)))
            .RenderToSvg();

        var sb = new System.Text.StringBuilder();
        sb.AppendLine("<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"940\" height=\"340\" viewBox=\"0 0 940 340\">");
        sb.AppendLine($"  <foreignObject x=\"10\"  y=\"10\" width=\"300\" height=\"300\"><div xmlns=\"http://www.w3.org/1999/xhtml\">{ring1}</div></foreignObject>");
        sb.AppendLine($"  <foreignObject x=\"320\" y=\"10\" width=\"300\" height=\"300\"><div xmlns=\"http://www.w3.org/1999/xhtml\">{ring2}</div></foreignObject>");
        sb.AppendLine($"  <foreignObject x=\"630\" y=\"10\" width=\"300\" height=\"300\"><div xmlns=\"http://www.w3.org/1999/xhtml\">{ring3}</div></foreignObject>");
        sb.AppendLine("</svg>");
        return sb.ToString();
    }

    // ================================================================== 41

    private static string FluentApiShowcase() =>
        ChartBuilder.Create()
            .Theme(GlobalTheme)
            .Size(820, 480)
            .AsInteractive()
            .Animate(750)
            .Animation(a => a.EaseInOut())
            .Title("Fluent API Showcase \u2014 Channel Revenue Stack")
            .Subtitle(
                "Size \u00b7 AsInteractive \u00b7 Animate \u00b7 AnimationBuilder \u00b7 StackNormal \u00b7 " +
                "XAxis/YAxis \u00b7 YAxisFormat \u00b7 ShowDataLabels \u00b7 SeriesBuilder")
            .StackNormal()
            .XAxis("Bi-Month", "Jan\u2013Feb", "Mar\u2013Apr", "May\u2013Jun", "Jul\u2013Aug", "Sep\u2013Oct", "Nov\u2013Dec")
            .YAxis("Revenue ($k)", min: 0)
            .YAxisFormat("${value}k")
            .YAxis2(y => { y.Title = "YoY Growth %"; y.Min = 0; y.Max = 50; })
            .ShowDataLabels()
            .Legend(l => l
                .AtTop().AlignCenter().Horizontal()
                .Padding(8).ItemFontSize(11))
            .Tooltip(t => t
                .FontSize(12)
                .Padding(12)
                .HideArrow()
                .Format("{label}: ${value}k"))
            .Series(s => s
                .AddColumn("Direct",
                    new double[] { 420, 490, 560, 620, 540, 610 },
                    cfg => cfg
                        .BorderWidth(1).BorderRadius(3)
                        .Label(dl => dl.Format("${value}k").FontSize(9)))
                .AddColumn("Organic",
                    new double[] { 310, 390, 440, 470, 380, 430 },
                    cfg => cfg
                        .BorderWidth(1).BorderRadius(3)
                        .Label(dl => dl.Format("${value}k").FontSize(9)))
                .AddColumn("Referral",
                    new double[] { 180, 215, 250, 270, 210, 245 },
                    cfg => cfg
                        .BorderWidth(1).BorderRadius(3)
                        .Label(dl => dl.Format("${value}k").FontSize(9)))
                .AddLine("YoY Growth %",
                    new double[] { 12, 21, 28, 31, 24, 29 },
                    cfg => cfg
                        .LineWidth(3).Dashed()
                        .Label(dl => dl.Format("{value}%").FontSize(11))
                        .OnSecondaryAxis()))
            .RenderToSvg();

    // ================================================================== 42

    private static string FixedWidthDemo() =>
        ChartBuilder.Create()
            .Theme(GlobalTheme)
            .Size(600, 320)
            .AsAnimated()
            .Title("Fixed Width \u2014 600 px")
            .Subtitle("Width(600) pins the SVG to exactly 600 px wide.")
            .XAxis("Month", "Jan", "Feb", "Mar", "Apr", "May", "Jun")
            .YAxis("Units Sold", min: 0)
            .Series(s => s.AddColumn("Sales", new double[] { 320, 410, 390, 480, 520, 445 }))
            .RenderToSvg();

    // ================================================================== 43

    private static string ResponsiveWidthDemo() =>
        ChartBuilder.Create()
            .Theme(GlobalTheme)
            .Size(800, 320)
            .ResponsiveWidth()
            .AsAnimated()
            .Title("Responsive Width \u2014 100 %")
            .Subtitle("ResponsiveWidth() sets width=\"100%\" so the SVG fills its container.")
            .XAxis("Quarter", "Q1", "Q2", "Q3", "Q4")
            .YAxis("Revenue ($k)", min: 0)
            .Series(s => s
                .AddLine("Actuals", new double[] { 210, 280, 310, 390 })
                .AddLine("Target",  new double[] { 250, 270, 320, 380 }))
            .RenderToSvg();

    // ================================================================== 44

    private static string XAxisTickIntervalDemo() =>
        ChartBuilder.Create()
            .Theme(GlobalTheme)
            .Size(820, 380)
            .AsAnimated()
            .Title("Numeric X-Axis with TickInterval = 5")
            .Subtitle("XAxisTickInterval(5) and XAxis.Min/Max drive evenly-spaced numeric ticks.")
            .XAxis(x => { x.Min = 0; x.Max = 30; x.TickInterval = 5; x.Title = "Time (s)"; x.GridLineVisible = true; })
            .YAxis("Amplitude", min: -1.5, max: 1.5)
            .Series(s => s.AddLine("Signal",
                new double[] { 0.0, 0.87, 1.0, 0.5, -0.5, -1.0, -0.87, 0.0,
                               0.87, 1.0, 0.5, -0.5, -1.0, -0.87, 0.0, 0.87 }))
            .RenderToSvg();

    // ================================================================== 45

    private static string ForkVariants() =>
        ChartBuilder.Create()
            .Theme(GlobalTheme)
            .Size(820, 380)
            .Title("Fork Demo \u2014 Static Variant")
            .Subtitle("Fork() creates an independent copy.")
            .XAxis("Month", "Jan", "Feb", "Mar", "Apr", "May", "Jun")
            .YAxis("Score", min: 0)
            .Series(s => s
                .AddLine("Team A", new double[] { 70, 78, 82, 88, 85, 92 })
                .AddLine("Team B", new double[] { 60, 65, 74, 71, 80, 85 }))
            .Fork()
            .AsStatic()
            .Tooltip(t => t.Format("Score: {value}"))
            .RenderToSvg();

    // ================================================================== 46

    private static string BubbleDemo() =>
        ChartBuilder.Create()
            .Theme(GlobalTheme)
            .Size(700, 400)
            .AsAnimated()
            .Title("Bubble Chart \u2014 Market Share vs. Growth")
            .Subtitle("Bubble size = revenue ($M). AddBubble() with (X, Y, Z) triplets.")
            .XAxis(x => { x.Title = "Market Share (%)"; x.Min = 0; x.Max = 40; x.GridLineVisible = true; })
            .YAxis(y => { y.Title = "YoY Growth (%)"; y.Min = -10; y.Max = 50; })
            .Legend(l => l.TopRight())
            .Series(s => s
                .AddBubble("Product A",
                    new[] { new BubblePoint(12, 28, 85), new BubblePoint(22, 15, 120), new BubblePoint(8, 42, 60) })
                .AddBubble("Product B",
                    new[] { new BubblePoint(30, 5, 200), new BubblePoint(18, 22, 95), new BubblePoint(35, -5, 140) }))
            .RenderToSvg();

    // ================================================================== 47

    private static string HeatmapDemo() =>
        ChartBuilder.Create()
            .Theme(GlobalTheme)
            .Size(700, 380)
            .AsAnimated()
            .Title("Heatmap \u2014 Weekly Sales by Region & Day")
            .Subtitle("AddHeatmap() \u2014 cell colour interpolates cold\u2192hot by value.")
            .XAxis("Day", "Mon", "Tue", "Wed", "Thu", "Fri")
            .Series(s => s.AddHeatmap("Sales",
                new[]
                {
                    new HeatmapPoint(0,0,42), new HeatmapPoint(1,0,58), new HeatmapPoint(2,0,73),
                    new HeatmapPoint(3,0,61), new HeatmapPoint(4,0,88),
                    new HeatmapPoint(0,1,31), new HeatmapPoint(1,1,45), new HeatmapPoint(2,1,52),
                    new HeatmapPoint(3,1,78), new HeatmapPoint(4,1,65),
                    new HeatmapPoint(0,2,67), new HeatmapPoint(1,2,83), new HeatmapPoint(2,2,91),
                    new HeatmapPoint(3,2,55), new HeatmapPoint(4,2,48),
                },
                cfg =>
                {
                    cfg.DataLabel.Show().Format("{value}");
                    cfg.HeatmapRowLabels.AddRange(new[] { "North", "South", "East" });
                }))
            .RenderToSvg();

    // ================================================================== 48

    private static string ColumnRangeDemo() =>
        ChartBuilder.Create()
            .Theme(GlobalTheme)
            .Size(700, 400)
            .AsAnimated()
            .Title("Column Range \u2014 Monthly Temperature Range (\u00b0C)")
            .Subtitle("AddColumnRange() \u2014 each bar spans from monthly low to high.")
            .XAxis("Month", "Jan","Feb","Mar","Apr","May","Jun",
                            "Jul","Aug","Sep","Oct","Nov","Dec")
            .YAxis(y => { y.Title = "Temperature (\u00b0C)"; y.Min = -10; y.Max = 40; y.LabelFormat = "{value}\u00b0"; })
            .Series(s => s.AddColumnRange("London",
                new[]
                {
                    new RangePoint(2, 8),  new RangePoint(2, 9),  new RangePoint(4, 13),
                    new RangePoint(6, 16), new RangePoint(9, 20), new RangePoint(12, 23),
                    new RangePoint(14,26), new RangePoint(14,25), new RangePoint(11, 21),
                    new RangePoint(8, 17), new RangePoint(5, 12), new RangePoint(3, 9),
                }))
            .RenderToSvg();

    // ================================================================== 49

    private static string AreaRangeDemo() =>
        ChartBuilder.Create()
            .Theme(GlobalTheme)
            .Size(700, 380)
            .AsAnimated()
            .Title("Area Range \u2014 Forecast Confidence Band")
            .Subtitle("AddAreaRange() \u2014 filled band between lower and upper bounds.")
            .XAxis("Week", "W1","W2","W3","W4","W5","W6","W7","W8")
            .YAxis(y => { y.Title = "Revenue ($k)"; y.Min = 80; y.Max = 280; })
            .Series(s => s
                .AddLine("Forecast",
                    new double[] { 140, 152, 161, 170, 178, 185, 192, 200 },
                    cfg => cfg.LineWidth(2))
                .AddAreaRange("Confidence Band",
                    new[]
                    {
                        new RangePoint(120, 160), new RangePoint(130, 172), new RangePoint(138, 184),
                        new RangePoint(148, 192), new RangePoint(155, 200), new RangePoint(160, 210),
                        new RangePoint(167, 217), new RangePoint(174, 226),
                    }))
            .RenderToSvg();

    // ================================================================== 50

    private static string FunnelDemo() =>
        ChartBuilder.Create()
            .Theme(GlobalTheme)
            .Size(600, 440)
            .AsAnimated()
            .Title("Funnel \u2014 Sales Pipeline")
            .Subtitle("AddFunnel() \u2014 each stage narrows proportionally.")
            .XAxis("Stage", "Leads","Qualified","Proposal","Negotiation","Closed")
            .Series(s => s.AddFunnel("Pipeline",
                new double[] { 5000, 2800, 1400, 620, 310 },
                cfg => cfg.Label(dl => dl.Show())))
            .RenderToSvg();

    // ================================================================== 51

    private static string TreemapDemo() =>
        ChartBuilder.Create()
            .Theme(GlobalTheme)
            .Size(700, 420)
            .AsAnimated()
            .Title("Treemap \u2014 Portfolio Allocation")
            .Subtitle("AddTreemap() \u2014 rectangles sized by value.")
            .XAxis("Asset",
                "US Equities","EU Equities","EM Equities",
                "Gov Bonds","Corp Bonds",
                "Real Estate","Commodities","Cash")
            .Series(s => s.AddTreemap("Allocation",
                new double[] { 3200, 1800, 900, 1500, 1100, 700, 400, 300 }))
            .RenderToSvg();

    // ================================================================== 52

    private static string ClickLineChart() =>
        ChartBuilder.Create()
            .Theme(GlobalTheme)
            .Title("Monthly Revenue \u2014 Click Any Point")
            .Subtitle("OnPointClick() \u00b7 SvgMode.Interactive \u00b7 inline function handler")
            .Size(700, 420)
            .XAxis(x =>
            {
                x.Title = "Month";
                x.Categories.AddRange(new[]
                    { "Jan","Feb","Mar","Apr","May","Jun",
                      "Jul","Aug","Sep","Oct","Nov","Dec" });
            })
            .YAxis(y => { y.Title = "Revenue ($k)"; y.Min = 0; })
            .AsInteractive()
            .OnPointClick("function(pt, e) { displayPointData(pt); }")
            .Series(s => s
                .AddLine("Revenue", new double?[]
                    { 210, 245, 260, 295, 330, 310, 350, 380, 345, 290, 265, 300 })
                .AddLine("Target", new double?[]
                    { 230, 240, 255, 280, 310, 320, 330, 355, 340, 300, 270, 295 },
                    cfg => cfg.Dashed()))
            .RenderToSvg();

    // ================================================================== 53

    private static string ClickColumnChart() =>
        ChartBuilder.Create()
            .Theme(GlobalTheme)
            .Title("Product Sales \u2014 Click a Bar")
            .Subtitle("OnPointClick() \u00b7 arrow function \u00b7 logs point data to console")
            .Size(700, 420)
            .XAxis(x =>
            {
                x.Title = "Product";
                x.Categories.AddRange(new[]
                    { "Widget A","Widget B","Widget C","Widget D","Widget E","Widget F" });
            })
            .YAxis(y => { y.Title = "Units Sold"; y.Min = 0; })
            .AsInteractive()
            .OnPointClick("function(pt, e) { displayPointData(pt); }")
            .Tooltip(t => t.ValueSuffix(" units"))
            .Series(s => s
                .AddColumn("Q1 Sales", new double?[] { 4800, 3200, 5600, 2900, 4100, 3700 })
                .AddColumn("Q2 Sales", new double?[] { 5100, 3600, 5900, 3400, 4400, 4000 }))
            .RenderToSvg();

    // ================================================================== 54

    private static string ClickSharedTooltip() =>
        ChartBuilder.Create()
            .Theme(GlobalTheme)
            .Title("KPI Dashboard \u2014 Click + Shared Tooltip")
            .Subtitle("OnPointClick() + shared tooltip")
            .Size(750, 440)
            .XAxis(x =>
            {
                x.Title = "Quarter";
                x.Categories.AddRange(new[]
                    { "Q1 2024","Q2 2024","Q3 2024","Q4 2024","Q1 2025","Q2 2025" });
            })
            .YAxis(y => { y.Title = "Value ($k)"; y.Min = 0; })
            .AsInteractive()
            .Tooltip(t => t.EnableShared().ValueSuffix("k").ValueDecimals(0))
            .OnPointClick("function(pt, e) { displayPointData(pt); }")
            .Series(s => s
                .AddLine("Revenue", new double?[] { 320, 410, 390, 480, 520, 495 })
                .AddLine("Costs",   new double?[] { 210, 255, 245, 290, 310, 295 })
                .AddArea("Profit",  new double?[] { 110, 155, 145, 190, 210, 200 }))
            .RenderToSvg();

    // ================================================================== 55

    private static string LegendToggleDemo() =>
        ChartBuilder.Create()
            .Theme(GlobalTheme)
            .Title("Legend Series Toggle \u2014 Click to Hide / Show")
            .Subtitle("SvgMode.Interactive \u00b7 click any legend item to hide or restore that series")
            .Size(760, 460)
            .XAxis(x =>
            {
                x.Title = "Month";
                x.Categories.AddRange(new[]
                    { "Jan","Feb","Mar","Apr","May","Jun",
                      "Jul","Aug","Sep","Oct","Nov","Dec" });
            })
            .YAxis(y => { y.Title = "Value ($k)"; y.Min = 0; })
            .AsInteractive()
            .Legend(l => l
                .AtBottom().AlignCenter().Horizontal()
                .Padding(8).ItemFontSize(12))
            .Tooltip(t => t.Format("{label}: ${value}k"))
            .Series(s => s
                .AddLine("Revenue", new double?[]
                    { 210, 245, 260, 295, 330, 310, 350, 380, 345, 290, 265, 300 },
                    cfg => cfg.LineWidth(2))
                .AddLine("Costs", new double?[]
                    { 150, 160, 170, 175, 185, 190, 200, 205, 195, 180, 165, 155 },
                    cfg => cfg.LineWidth(2))
                .AddArea("Profit", new double?[]
                    { 60, 85, 90, 120, 145, 120, 150, 175, 150, 110, 100, 145 },
                    cfg => cfg.FillOpacity(0.25))
                .AddLine("Target", new double?[]
                    { 180, 200, 220, 240, 260, 280, 300, 310, 300, 270, 240, 220 },
                    cfg => { cfg.Dashed(); cfg.LineWidth(2); }))
            .RenderToSvg();

    // ================================================================== 56

    private static string ExportButtonDemo() =>
        ChartBuilder.Create()
            .Theme(GlobalTheme)
            .Title("Q3 2025 Revenue \u2014 SVG Export Demo")
            .Subtitle("ShowExportButton() \u2014 click \u2b07 SVG (top-right) to download")
            .Size(760, 460)
            .XAxis(x =>
            {
                x.Title = "Month";
                x.Categories.AddRange(new[] { "Jul","Aug","Sep" });
            })
            .YAxis(y => { y.Title = "Revenue ($k)"; y.Min = 0; })
            .AsInteractive()
            .ShowExportButton()
            .Legend(l => l
                .AtBottom().AlignCenter().Horizontal()
                .Padding(8).ItemFontSize(12))
            .Tooltip(t => t.Format("{label}: ${value}k"))
            .Series(s => s
                .AddColumn("Online",   new double?[] { 310, 370, 395 },
                    cfg => cfg.DataLabel.Show().Format("${value}k"))
                .AddColumn("In-Store", new double?[] { 220, 245, 260 },
                    cfg => cfg.DataLabel.Show().Format("${value}k"))
                .AddLine("Total",      new double?[] { 530, 615, 655 },
                    cfg => cfg.LineWidth(3)))
            .RenderToSvg();

    // ================================================================== 57

    private static string ExportMenuDemo() =>
        ChartBuilder.Create()
            .Theme(GlobalTheme)
            .Title("H1 2025 Performance Dashboard")
            .Subtitle("ShowExportMenu() \u2014 SVG, PNG, JPEG, PDF options top-right")
            .Size(760, 460)
            .XAxis(x =>
            {
                x.Title = "Month";
                x.Categories.AddRange(new[] { "Jan","Feb","Mar","Apr","May","Jun" });
            })
            .YAxis(y => { y.Title = "Value ($k)"; y.Min = 0; })
            .AsInteractive()
            .ShowExportMenu()
            .Legend(l => l
                .AtBottom().AlignCenter().Horizontal()
                .Padding(8).ItemFontSize(12))
            .Tooltip(t => t.Format("{label}: ${value}k"))
            .Series(s => s
                .AddColumn("Revenue", new double?[] { 310, 370, 395, 430, 480, 515 },
                    cfg => cfg.DataLabel.Show().Format("${value}k"))
                .AddColumn("Cost",    new double?[] { 210, 245, 260, 275, 295, 310 },
                    cfg => cfg.DataLabel.Show().Format("${value}k"))
                .AddLine("Profit",    new double?[] { 100, 125, 135, 155, 185, 205 },
                    cfg => cfg.LineWidth(3)))
            .RenderToSvg();

    // ================================================================== 58

    private static string AutoInsightLine() =>
        ChartBuilder.Create()
            .Theme(GlobalTheme)
            .Title("E-Commerce Revenue \u2014 AutoInsight Demo")
            .Subtitle("Trend line \u00b7 moving average \u00b7 anomaly band \u00b7 peak highlights \u00b7 narrative")
            .Size(760, 440)
            .XAxis(x =>
            {
                x.Title = "Month";
                x.Categories.AddRange(new[]
                    { "Jan","Feb","Mar","Apr","May","Jun",
                      "Jul","Aug","Sep","Oct","Nov","Dec" });
            })
            .YAxis(y => { y.Title = "Revenue ($k)"; y.Min = 0; })
            .AsAnimated()
            .Series(s => s.AddLine("Revenue", new double?[]
                    { 42, 38, 45, 49, 51, 48, 54, 52, 58, 97, 55, 62 },
                cfg => cfg
                    .LineWidth(2)
                    .AutoInsight(ai => ai
                        .AnomalyBands(sigma: 1.5)
                        .TrendLine()
                        .MovingAverage(period: 3)
                        .HighlightPeaks()
                        .NarrativeSummary())))
            .RenderToSvg();

    // ================================================================== 59

    private static string AutoInsightArea() =>
        ChartBuilder.Create()
            .Theme(GlobalTheme)
            .Title("Server Response Time \u2014 AutoInsight on Area Series")
            .Subtitle("Anomaly band \u00b7 trend line \u00b7 4-week moving average \u00b7 peak markers")
            .Size(760, 440)
            .XAxis(x =>
            {
                x.Title = "Week";
                x.Categories.AddRange(new[]
                    { "W01","W02","W03","W04","W05","W06","W07","W08",
                      "W09","W10","W11","W12","W13","W14","W15","W16" });
            })
            .YAxis(y => { y.Title = "Avg. Response (ms)"; y.Min = 0; })
            .AsAnimated()
            .Series(s => s.AddArea("Response Time", new double?[]
                    { 142, 138, 145, 151, 148, 155, 230, 149, 158, 162, 155, 160, 153, 218, 157, 161 },
                cfg => cfg
                    .FillOpacity(0.18)
                    .LineWidth(2)
                    .AutoInsight(ai => ai
                        .AnomalyBands(sigma: 1.8)
                        .TrendLine()
                        .MovingAverage(period: 4)
                        .HighlightPeaks()
                        .NarrativeSummary())))
            .RenderToSvg();

    // ================================================================== 60

    private static string ParliamentWestoria() =>
        ChartBuilder.Create()
            .Theme(GlobalTheme)
            .Title("Parliament of Westoria \u2014 14th Session")
            .Subtitle("300 seats \u00b7 distribution after general election")
            .Size(760, 480)
            .AsInteractive()
            .Legend(l => l.Horizontal().AlignLeft())
            .ShowDataLabels()
            .Series(s => s.AddParliament("Parliament of Westoria",
                new[]
                {
                    new ParliamentGroup("Progressive Alliance", "#3b82f6", 112),
                    new ParliamentGroup("Conservative Union",   "#ef4444",  98),
                    new ParliamentGroup("Centrist Democrats",   "#f59e0b",  52),
                    new ParliamentGroup("Green Coalition",      "#22c55e",  24),
                    new ParliamentGroup("Liberty Movement",     "#a855f7",  14),
                },
                cfg =>
                {
                    cfg.CenterLabel(c => c.Text("300").FontSize(24).Subtitle("seats total").SubtitleFontSize(18));
                    cfg.Label(l => { l.FontSize(18); l.FormatString = "{value}"; });
                }))
            .RenderToSvg();

    // ================================================================== 61

    private static string LogarithmicAxis() =>
        ChartBuilder.Create()
            .Theme(GlobalTheme)
            .Title("Exponential Growth on a Log Scale")
            .Subtitle("Users over time \u2014 each decade evenly spaced")
            .Size(700, 400)
            .AsAnimated()
            .XAxis(x =>
            {
                x.Title = "Quarter";
                x.Categories.AddRange(new[] { "Q1","Q2","Q3","Q4","Q5","Q6","Q7","Q8" });
            })
            .YAxis(y => y.Title = "Active Users")
            .YAxisLogarithmic()
            .Series(s => s
                .AddLine("Users",            new double?[] { 5, 30, 120, 600, 2500, 9000, 40000, 150000 })
                .AddLine("Linear baseline",  new double?[] { 5, 12,  25,  50,  100,  200,   400,    800 }))
            .RenderToSvg();

    // ================================================================== 62

    private static string DateTimeAxis()
    {
        var start  = new DateTime(2024, 1, 1);
        var dates  = new List<DateTime>();
        var visits = new double?[52];
        var rnd    = new Random(7);
        double running = 1200;
        for (int i = 0; i < 52; i++)
        {
            dates.Add(start.AddDays(i * 7));
            running += rnd.Next(-120, 180);
            visits[i] = Math.Max(200, running);
        }
        return ChartBuilder.Create()
            .Theme(GlobalTheme)
            .Title("Weekly Site Visits \u2014 2024")
            .Subtitle("Date/time X axis with auto-formatted, thinned labels")
            .Size(760, 400)
            .AsAnimated()
            .XAxis(x => x.Title = "Week")
            .XAxisDateTime(dates)
            .YAxis(y => y.Title = "Visits")
            .Series(s => s.AddArea("Visits", visits))
            .RenderToSvg();
    }

    // ================================================================== 63

    private static string RadarChart() =>
        ChartBuilder.Create()
            .Theme(GlobalTheme)
            .Title("Product Comparison")
            .Subtitle("Radar chart \u2014 five attributes, two models")
            .Size(560, 500)
            .AsAnimated()
            .XAxis(x => x.Categories.AddRange(new[] { "Speed","Power","Range","Agility","Comfort","Value" }))
            .YAxis(y => { y.Min = 0; y.Max = 100; })
            .Legend(l => l.Horizontal())
            .Series(s => s
                .AddRadar("Model A", new double?[] { 85, 70, 60, 90, 55, 75 })
                .AddRadar("Model B", new double?[] { 60, 90, 80, 55, 85, 65 }))
            .RenderToSvg();

    // ================================================================== 64

    private static string BoxPlotChart() =>
        ChartBuilder.Create()
            .Theme(GlobalTheme)
            .Title("Response Time Distribution")
            .Subtitle("Box-and-whisker \u2014 five-number summary per endpoint")
            .Size(700, 420)
            .AsAnimated()
            .XAxis(x => x.Categories.AddRange(new[] { "Login","Search","Checkout","Report","Upload" }))
            .YAxis(y => { y.Title = "Response (ms)"; y.Min = 0; y.Max = 500; })
            .Series(s => s.AddBoxPlot("Latency", new[]
            {
                new BoxPlotPoint( 40,  90, 130, 180, 260),
                new BoxPlotPoint( 60, 120, 170, 230, 340),
                new BoxPlotPoint( 80, 150, 210, 280, 420),
                new BoxPlotPoint(120, 200, 260, 330, 460),
                new BoxPlotPoint( 50, 110, 160, 210, 300),
            }))
            .RenderToSvg();

    // ================================================================== 65

    private static string ErrorBarChart() =>
        ChartBuilder.Create()
            .Theme(GlobalTheme)
            .Title("Measured Yield with Uncertainty")
            .Subtitle("Column series with I-beam error bars")
            .Size(700, 420)
            .AsAnimated()
            .XAxis(x => x.Categories.AddRange(new[] { "Batch 1","Batch 2","Batch 3","Batch 4" }))
            .YAxis(y => { y.Title = "Yield (%)"; y.Min = 0; y.Max = 100; })
            .Series(s => s
                .AddColumn("Yield", new double?[] { 62, 71, 68, 75 })
                .AddErrorBar("\u00b1 1\u03c3", new[]
                {
                    new RangePoint(56, 68),
                    new RangePoint(66, 76),
                    new RangePoint(61, 74),
                    new RangePoint(70, 81),
                }, cfg => cfg.BorderWidth(2)))
            .RenderToSvg();

    // ================================================================== 66

    private static string GradientFillChart() =>
        ChartBuilder.Create()
            .Theme(GlobalTheme)
            .Title("Traffic Over Time")
            .Subtitle("Area filled with a vertical linear gradient")
            .Size(700, 420)
            .AsAnimated()
            .XAxis(x => x.Categories.AddRange(new[] { "Mon","Tue","Wed","Thu","Fri","Sat","Sun" }))
            .YAxis(y => { y.Title = "Sessions"; y.Min = 0; })
            .Series(s => s.AddArea("Visitors", new double?[] { 120, 200, 180, 260, 320, 290, 340 },
                cfg => cfg.LinearGradientFill(90, (0.0, "#1e90ff"), (1.0, "#e8f2ff"))))
            .RenderToSvg();

    // ================================================================== 67

    private static string PatternFillChart() =>
        ChartBuilder.Create()
            .Theme(GlobalTheme)
            .Title("Regional Output")
            .Subtitle("Columns filled with SVG tile patterns")
            .Size(700, 420)
            .AsAnimated()
            .XAxis(x => x.Categories.AddRange(new[] { "North","South","East","West" }))
            .YAxis(y => { y.Title = "Units"; y.Min = 0; })
            .Series(s => s
                .AddColumn("Diagonal", new double?[] { 42, 58, 35, 66 },
                    cfg => cfg.PatternFill(PatternKind.DiagonalLines, "#2f6f4f", "#d9ecdf"))
                .AddColumn("Dots", new double?[] { 30, 48, 52, 40 },
                    cfg => cfg.PatternFill(PatternKind.Dots, "#b8722c", "#f6e6d2"))
                .AddColumn("Grid", new double?[] { 55, 38, 44, 60 },
                    cfg => cfg.PatternFill(PatternKind.Grid, "#3a5a8c", "#e2e9f4")))
            .RenderToSvg();

    // ================================================================== 68

    private static string MarkerSymbolChart() =>
        ChartBuilder.Create()
            .Theme(GlobalTheme)
            .Title("Quarterly KPIs")
            .Subtitle("Each series uses a distinct marker symbol")
            .Size(700, 420)
            .AsAnimated()
            .XAxis(x => x.Categories.AddRange(new[] { "Q1","Q2","Q3","Q4" }))
            .YAxis(y => { y.Title = "Index"; y.Min = 0; })
            .Legend(l => l.Horizontal())
            .Series(s => s
                .AddLine("Circle",   new double?[] { 20, 34, 30, 46 },
                    cfg => cfg.MarkerSize(6).MarkerSymbol(MarkerSymbol.Circle))
                .AddLine("Square",   new double?[] { 30, 26, 42, 38 },
                    cfg => cfg.MarkerSize(6).MarkerSymbol(MarkerSymbol.Square))
                .AddLine("Diamond",  new double?[] { 40, 48, 36, 54 },
                    cfg => cfg.MarkerSize(6).MarkerSymbol(MarkerSymbol.Diamond))
                .AddLine("Triangle", new double?[] { 52, 44, 58, 50 },
                    cfg => cfg.MarkerSize(6).MarkerSymbol(MarkerSymbol.Triangle)))
            .RenderToSvg();

    // ================================================================== 69

    private static string ZonesChart() =>
        ChartBuilder.Create()
            .Theme(GlobalTheme)
            .Title("Temperature Anomaly")
            .Subtitle("Line recoloured by threshold \u2014 red below 0 \u00b0C, green above")
            .Size(720, 420)
            .AsAnimated()
            .XAxis(x => x.Categories.AddRange(new[]
            {
                "Jan","Feb","Mar","Apr","May","Jun",
                "Jul","Aug","Sep","Oct","Nov","Dec"
            }))
            .YAxis(y => y.Title = "\u0394 \u00b0C")
            .Series(s => s.AddSpline("Anomaly",
                new double?[] { -4.2, -3.1, -1.0, 1.8, 3.4, 4.9, 5.6, 4.1, 2.0, -0.5, -2.4, -3.8 },
                cfg => cfg
                    .LineWidth(3)
                    .MarkerSize(4)
                    .Zones((0.0, "#d64545"), (null, "#2f9e44"))))
            .RenderToSvg();

    // ================================================================== 70

    private static string CandlestickChart() =>
        ChartBuilder.Create()
            .Theme(GlobalTheme)
            .Title("ACME Corp \u2014 Daily Price")
            .Subtitle("AddCandlestick() \u2014 OHLC bodies with high-low wicks")
            .Size(720, 420)
            .AsAnimated()
            .XAxis(x => x.Categories.AddRange(new[]
            {
                "Mon","Tue","Wed","Thu","Fri","Mon","Tue","Wed","Thu","Fri"
            }))
            .YAxis(y => { y.Title = "Price ($)"; y.Min = 90; y.Max = 130; })
            .Series(s => s.AddCandlestick("Price", new[]
            {
                new OhlcPoint(100, 108,  98, 106), new OhlcPoint(106, 112, 104, 104),
                new OhlcPoint(104, 115, 103, 113), new OhlcPoint(113, 118, 110, 111),
                new OhlcPoint(111, 120, 109, 119), new OhlcPoint(119, 122, 114, 115),
                new OhlcPoint(115, 117, 108, 110), new OhlcPoint(110, 116, 107, 116),
                new OhlcPoint(116, 125, 115, 124), new OhlcPoint(124, 128, 121, 122),
            }))
            .RenderToSvg();

    // ================================================================== 71

    private static string OhlcChart() =>
        ChartBuilder.Create()
            .Theme(GlobalTheme)
            .Title("ACME Corp \u2014 Daily Price")
            .Subtitle("AddOhlc() \u2014 high-low bar with left open tick and right close tick")
            .Size(720, 420)
            .AsAnimated()
            .XAxis(x => x.Categories.AddRange(new[]
            {
                "Mon","Tue","Wed","Thu","Fri","Mon","Tue","Wed","Thu","Fri"
            }))
            .YAxis(y => { y.Title = "Price ($)"; y.Min = 90; y.Max = 130; })
            .Series(s => s.AddOhlc("Price", new[]
            {
                new OhlcPoint(100, 108,  98, 106), new OhlcPoint(106, 112, 104, 104),
                new OhlcPoint(104, 115, 103, 113), new OhlcPoint(113, 118, 110, 111),
                new OhlcPoint(111, 120, 109, 119), new OhlcPoint(119, 122, 114, 115),
                new OhlcPoint(115, 117, 108, 110), new OhlcPoint(110, 116, 107, 116),
                new OhlcPoint(116, 125, 115, 124), new OhlcPoint(124, 128, 121, 122),
            }))
            .RenderToSvg();

    // ================================================================== 72

    private static string AnnotationsChart() =>
        ChartBuilder.Create()
            .Theme(GlobalTheme)
            .Title("Website Traffic \u2014 Annotated")
            .Subtitle("Annotations() — SmartLayout is always active")
            .Size(720, 420)
            .AsAnimated()
            .XAxis(x => x.Categories.AddRange(new[]
            {
                "Jan","Feb","Mar","Apr","May","Jun","Jul","Aug"
            }))
            .YAxis(y => { y.Title = "Visitors (k)"; y.Min = 0; y.Max = 120; })
            .Series(s => s.AddLine("Visitors",
                new double?[] { 32, 40, 38, 55, 72, 68, 95, 110 },
                cfg => cfg.LineWidth(3).MarkerSize(4)))
            .Annotations(a => a
                .Rect(5, 0, 7, 120, cfg => cfg.DashStyle = "Dash")
                .Label(6, 60, "Summer surge", cfg => { cfg.FontSize = 12; cfg.OffsetY = 6; })
                .Circle(7, 110, 14)
                // SmartLayout detects overlap with the circle marker and nudges the label clear.
                .Label(7, 110, "All-time high", cfg => { cfg.TextAnchor = "end"; cfg.OffsetX = -20; cfg.OffsetY = -25; })
                .Line(0, 32, 7, 110, cfg => { cfg.StrokeWidth = 2; cfg.DashStyle = "Dot"; }))
            .RenderToSvg();

    // ================================================================== 74

    private static string LabelLayoutChart() =>
        ChartBuilder.Create()
            .Theme(GlobalTheme)
            .Title("Label Layout — Collision Detection Demo")
            .Subtitle("LabelLayout: auto-rotate + font scale + stagger")
            .Size(760, 440)
            .AsAnimated()
            .XAxis(x => x.Categories.AddRange(new[]
            {
                "North America", "Latin America", "Western Europe",
                "Eastern Europe", "Middle East & Africa", "South Asia",
                "East Asia Pacific", "Australia & NZ"
            }))
            .YAxis(y => { y.Title = "Revenue ($M)"; y.Min = 0; })
            .LabelLayout(ll => ll
                .AutoRotate(45)
                .FontSizeRange(8, 11)
                .Stagger(8)
                .AutoSkip()
                .HorizontalPadding(4))
            .Series(s => s
                .AddColumn("2024", new double?[] { 520, 310, 480, 220, 190, 340, 610, 130 })
                .AddLine("Trend", new double?[] { 520, 310, 480, 220, 190, 340, 610, 130 },
                    cfg => cfg.LineWidth(2).MarkerSize(4)))
            .RenderToSvg();

    // ================================================================== 73

    private static string VividTheme() =>
        ChartBuilder.Create()
            .Theme(ChartTheme.Vivid)
            .Title("Global Market Performance")
            .Subtitle("ChartTheme.Vivid \u2014 HighCharts-inspired full-spectrum palette")
            .Size(720, 440)
            .AsAnimated()
            .XAxis(x => x.Categories.AddRange(new[] { "Q1", "Q2", "Q3", "Q4", "Q5", "Q6" }))
            .YAxis(y => { y.Title = "Revenue ($M)"; y.Min = 0; })
            .Series(s => s
                .AddColumn("Americas",  new double?[] { 420, 510, 480, 630, 590, 710 })
                .AddColumn("Europe",    new double?[] { 310, 360, 400, 420, 470, 530 })
                .AddColumn("Asia Pac",  new double?[] { 280, 330, 390, 450, 520, 600 })
                .AddLine("Trend",       new double?[] { 337, 400, 423, 500, 527, 613 },
                    cfg => cfg.LineWidth(2).MarkerSize(5)))
            .RenderToSvg();

    // ================================================================== 75

    private static string HighContrastThemeChart() =>
        ChartBuilder.Create()
            .Theme(ChartTheme.HighContrast)
            .Title("Quarterly Revenue — High-Contrast Theme")
            .Subtitle("ChartTheme.HighContrast \u2014 WCAG AA \u2265 4.5:1 on white")
            .Size(720, 400)
            .AsAnimated()
            .XAxis(x => x.Categories.AddRange(new[] { "Q1", "Q2", "Q3", "Q4" }))
            .YAxis(y => { y.Title = "Revenue ($k)"; y.Min = 0; })
            .Series(s => s
                .AddColumn("Product A", new double?[] { 120, 145, 132, 178 })
                .AddColumn("Product B", new double?[] {  95, 110, 128, 142 })
                .AddLine("Target",      new double?[] { 130, 130, 130, 130 },
                    cfg => cfg.Dashed().LineWidth(2).MarkerEnabled(false)))
            .RenderToSvg();

    // ================================================================== 76

    private static string NullGapPolicyChart() =>
        ChartBuilder.Create()
            .Theme(GlobalTheme)
            .Title("Null-Gap Policy Comparison")
            .Subtitle("Break (gap) vs Connect (bridge) vs Zero (baseline) for missing data points")
            .Size(720, 420)
            .AsAnimated()
            .XAxis(x => x.Categories.AddRange(new[] { "Jan", "Feb", "Mar", "Apr", "May", "Jun" }))
            .YAxis(y => { y.Title = "Value"; y.Min = 0; y.Max = 100; })
            .Series(s => s
                .AddLine("Break (gap)",    new double?[] { 20, null, null, 60, null, 90 },
                    cfg => cfg.NullGap(Enums.GapPolicy.Break))
                .AddLine("Connect (bridge)", new double?[] { 30, null, null, 70, null, 80 },
                    cfg => cfg.NullGap(Enums.GapPolicy.Connect))
                .AddLine("Zero (baseline)", new double?[] { 10, null, null, 50, null, 70 },
                    cfg => cfg.NullGap(Enums.GapPolicy.Zero)))
            .RenderToSvg();

    // ================================================================== 77

    private static string TargetLinesChart() =>
        ChartBuilder.Create()
            .Theme(GlobalTheme)
            .Title("Monthly Sales vs Targets")
            .Subtitle("TargetLine() \u2014 per-series reference lines at specific Y values")
            .Size(720, 420)
            .AsAnimated()
            .XAxis(x => x.Categories.AddRange(new[] { "Jan", "Feb", "Mar", "Apr", "May", "Jun" }))
            .YAxis(y => { y.Title = "Revenue ($k)"; y.Min = 0; })
            .Series(s => s
                .AddLine("North", new double?[] { 82, 91, 78, 105, 112, 98 },
                    cfg => cfg.TargetLine(95, "North target", null, "Dash", 2))
                .AddLine("South", new double?[] { 55, 63, 70, 68, 74, 88 },
                    cfg => cfg.TargetLine(72, "South target", null, "Dot", 2)))
            .RenderToSvg();

    // ================================================================== 78

    private static string DataUriEmbedChart()
    {
        string dataUri = ChartBuilder.Create()
            .Theme(GlobalTheme)
            .Title("Sales Overview")
            .Subtitle("Rendered as a Base64 data: URI and embedded in HTML")
            .Size(600, 360)
            .AsStatic()
            .XAxis(x => x.Categories.AddRange(new[] { "Q1", "Q2", "Q3", "Q4" }))
            .YAxis(y => { y.Title = "Units sold (k)"; y.Min = 0; })
            .Series(s => s
                .AddColumn("2024", new double?[] { 42, 58, 53, 67 })
                .AddColumn("2025", new double?[] { 55, 70, 62, 81 }))
            .RenderToDataUri();

        // Wrap the data URI in a minimal self-contained HTML fragment for display in the index.
        return $"<img src=\"{dataUri}\" alt=\"Sales Overview\" style=\"max-width:100%;height:auto;\"/>";
    }

    // ------------------------------------------------------------------ Phase 2

    private static string PercentStackedChart()
    {
        return ChartBuilder.Create()
            .Theme(GlobalTheme)
            .Title("Market Share")
            .Subtitle("100% stacked columns — Y-axis auto-labels as percentage")
            .Size(700, 400)
            .AsStatic()
            .StackPercent()
            .XAxis(x => x.Categories.AddRange(new[] { "Q1", "Q2", "Q3", "Q4" }))
            .YAxis(y => { y.Title = "Share"; y.Min = 0; y.Max = 100; })
            .Series(s => s
                .AddColumn("Product A", new double?[] { 40, 35, 45, 38 })
                .AddColumn("Product B", new double?[] { 30, 40, 25, 32 })
                .AddColumn("Product C", new double?[] { 30, 25, 30, 30 }))
            .RenderToSvg();
    }

    private static string DualAxisComboChart()
    {
        return ChartBuilder.Create()
            .Theme(GlobalTheme)
            .Title("Revenue & Growth Rate")
            .Subtitle("Column on primary Y-axis, line on secondary Y-axis")
            .Size(720, 420)
            .AsStatic()
            .XAxis(x => x.Categories.AddRange(new[] { "Jan", "Feb", "Mar", "Apr", "May", "Jun" }))
            .YAxis(y => { y.Title = "Revenue ($k)"; y.Min = 0; })
            .YAxis2(y => { y.Title = "Growth (%)"; y.Min = -20; y.Max = 60; })
            .Series(s => s
                .AddColumn("Revenue", new double?[] { 120, 145, 138, 162, 175, 190 })
                .AddLine("Growth %", new double?[] { 8, 20, -5, 17, 8, 9 },
                    cfg => cfg.OnSecondaryAxis()))
            .RenderToSvg();
    }

    private static string ComboChartDemo()
    {
        return ChartBuilder.Create()
            .Theme(GlobalTheme)
            .Title("Sales Performance")
            .Subtitle("Mixed chart: Column + Line + Area on a single plot")
            .Size(720, 420)
            .AsStatic()
            .XAxis(x => x.Categories.AddRange(new[] { "Jan", "Feb", "Mar", "Apr", "May", "Jun" }))
            .YAxis(y => { y.Title = "Units"; y.Min = 0; })
            .Series(s => s
                .AddColumn("Actual",   new double?[] { 100, 120, 115, 140, 130, 155 })
                .AddLine("Target",     new double?[] { 110, 115, 120, 125, 135, 145 })
                .AddArea("Forecast",   new double?[] { 105, 118, 116, 138, 132, 150 }))
            .RenderToSvg();
    }

    private static string InvertedAxisChart()
    {
        return ChartBuilder.Create()
            .Theme(GlobalTheme)
            .Title("Race-to-Zero Leaderboard")
            .Subtitle("YAxisInverted() — rank 1 at the top, higher rank numbers lower")
            .Size(600, 380)
            .AsStatic()
            .YAxisInverted()
            .XAxis(x => x.Categories.AddRange(new[] { "Alice", "Bob", "Carol", "Dave", "Eve" }))
            .YAxis(y => { y.Title = "Rank"; y.Min = 1; y.Max = 5; y.TickInterval = 1; })
            .Series(s => s
                .AddColumn("Position", new double?[] { 1, 3, 2, 5, 4 }))
            .RenderToSvg();
    }

    // ------------------------------------------------------------------ Phase 3

    private static string LinearRegressionChart()
    {
        double[] data = { 42, 38, 45, 49, 51, 48, 54, 52, 58, 57, 61, 65 };
        return ChartBuilder.Create()
            .Theme(GlobalTheme)
            .Title("Monthly Revenue with Trend Line")
            .Subtitle("AddLinearRegression() — least-squares fit over raw monthly data")
            .Size(720, 420)
            .AsAnimated()
            .XAxis(x => x.Categories.AddRange(new[]
                { "Jan","Feb","Mar","Apr","May","Jun","Jul","Aug","Sep","Oct","Nov","Dec" }))
            .YAxis(y => { y.Title = "Revenue ($k)"; y.Min = 30; })
            .Series(s => s
                .AddLine("Revenue", data, cfg => cfg.LineWidth(2).MarkerSize(4))
                .AddLinearRegression("Trend", data,
                    cfg => cfg.Dashed().LineWidth(2).Color("#e05b00").MarkerEnabled(false)))
            .RenderToSvg();
    }

    private static string MovingAverageChart()
    {
        double[] data = { 82, 74, 91, 68, 85, 77, 93, 89, 72, 96, 88, 80 };
        return ChartBuilder.Create()
            .Theme(GlobalTheme)
            .Title("Weekly Web Sessions — Moving Average")
            .Subtitle("AddMovingAverage(period: 3) — smooths short-term volatility")
            .Size(720, 420)
            .AsAnimated()
            .XAxis(x => x.Categories.AddRange(new[]
                { "W1","W2","W3","W4","W5","W6","W7","W8","W9","W10","W11","W12" }))
            .YAxis(y => { y.Title = "Sessions (k)"; y.Min = 60; })
            .Series(s => s
                .AddLine("Sessions", data, cfg => cfg.LineWidth(1).MarkerSize(3).FillOpacity(0))
                .AddMovingAverage("MA(3)", data, period: 3,
                    cfg => cfg.LineWidth(3).Color("#2563eb").MarkerEnabled(false)))
            .RenderToSvg();
    }

    private static string ExponentialSmoothingChart()
    {
        double[] data = { 120, 132, 101, 134, 90, 230, 210, 105, 188, 165, 147, 195 };
        return ChartBuilder.Create()
            .Theme(GlobalTheme)
            .Title("E-Commerce Orders — Exponential Smoothing")
            .Subtitle("AddExponentialSmoothing(alpha: 0.4) — reacts to recent changes while dampening noise")
            .Size(720, 420)
            .AsAnimated()
            .XAxis(x => x.Categories.AddRange(new[]
                { "Jan","Feb","Mar","Apr","May","Jun","Jul","Aug","Sep","Oct","Nov","Dec" }))
            .YAxis(y => { y.Title = "Orders"; y.Min = 0; })
            .Series(s => s
                .AddLine("Raw Orders", data, cfg => cfg.LineWidth(1).MarkerSize(3))
                .AddExponentialSmoothing("EMA (α=0.4)", data, alpha: 0.4,
                    cfg => cfg.LineWidth(3).Color("#16a34a").MarkerEnabled(false)))
            .RenderToSvg();
    }

    private static string DataTableChart()
    {
        return ChartBuilder.Create()
            .Theme(GlobalTheme)
            .Title("Quarterly Performance")
            .Subtitle("ShowDataTable() — per-category value grid appended below the chart")
            .Size(720, 380)
            .AsStatic()
            .ShowDataTable()
            .XAxis(x => x.Categories.AddRange(new[] { "Q1", "Q2", "Q3", "Q4" }))
            .YAxis(y => { y.Title = "USD ($k)"; y.Min = 0; })
            .Series(s => s
                .AddColumn("Revenue", new double?[] { 320, 410, 390, 480 })
                .AddColumn("Cost",    new double?[] { 210, 255, 245, 290 })
                .AddLine("Profit",    new double?[] { 110, 155, 145, 190 },
                    cfg => cfg.LineWidth(2).Dashed()))
            .RenderToSvg();
    }

    // ------------------------------------------------------------------ Phase 4

    private static string AriaLabelsChart()
    {
        return ChartBuilder.Create()
            .Theme(GlobalTheme)
            .Title("Q1–Q4 Revenue")
            .Subtitle("Custom ARIA label and description override")
            .Size(700, 400)
            .AsStatic()
            .AriaLabel("Revenue chart for Q1 through Q4 2025")
            .AriaDescription("Bar chart showing quarterly revenue. Q1: $320k, Q2: $410k, Q3: $390k, Q4: $480k. Q4 was the strongest quarter with a 23% increase over Q1.")
            .XAxis(x => x.Categories.AddRange(new[] { "Q1", "Q2", "Q3", "Q4" }))
            .YAxis(y => { y.Title = "Revenue ($k)"; y.Min = 0; })
            .Series(s => s.AddColumn("Revenue", new double?[] { 320, 410, 390, 480 }))
            .RenderToSvg();
    }

    private static string GermanCultureChart()
    {
        return ChartBuilder.Create()
            .Theme(GlobalTheme)
            .Title("Umsatz nach Quartal")
            .Subtitle("Culture(\"de-DE\") — Achsenbeschriftungen im deutschen Zahlenformat")
            .Size(700, 400)
            .AsStatic()
            .Culture("de-DE")
            .XAxis(x => x.Categories.AddRange(new[] { "Q1", "Q2", "Q3", "Q4" }))
            .YAxis(y => { y.Title = "Umsatz (€k)"; y.Min = 0; y.Max = 2000; })
            .Series(s => s
                .AddColumn("Produkt A", new double?[] { 1200, 1450, 1380, 1720 })
                .AddLine("Trend", new double?[] { 1250, 1350, 1500, 1600 },
                    cfg => cfg.Dashed().LineWidth(2)))
            .RenderToSvg();
    }

    private static string RtlChart()
    {
        return ChartBuilder.Create()
            .Theme(GlobalTheme)
            .Title("المبيعات الفصلية")
            .Subtitle("RightToLeft() — dir=\"rtl\" ومحتوى عربي")
            .Size(700, 400)
            .AsStatic()
            .Culture("ar-SA")
            .RightToLeft()
            .XAxis(x => x.Categories.AddRange(new[] { "الربع الأول", "الربع الثاني", "الربع الثالث", "الربع الرابع" }))
            .YAxis(y => { y.Title = "المبيعات"; y.Min = 0; })
            .Series(s => s
                .AddColumn("المنتج أ", new double?[] { 320, 410, 390, 480 })
                .AddColumn("المنتج ب", new double?[] { 210, 255, 245, 290 }))
            .RenderToSvg();
    }

    // ------------------------------------------------------------------ Phase 5

    private static string TemplateRevenueChart()
    {
        return ChartBuilder.Create()
            .ApplyTemplate(ChartTemplate.Revenue)
            .Title("Annual Revenue by Region")
            .Size(700, 400)
            .XAxis(x => x.Categories.AddRange(new[] { "Q1", "Q2", "Q3", "Q4" }))
            .YAxis(y => y.Title = "Revenue (USD)")
            .Series(s => s
                .AddColumn("North", new double?[] { 1200, 1450, 1380, 1720 })
                .AddColumn("South", new double?[] {  980, 1100, 1050, 1310 }))
            .RenderToSvg();
    }

    private static string TemplateKpiChart()
    {
        return ChartBuilder.Create()
            .ApplyTemplate(ChartTemplate.KpiDashboard)
            .Title("Monthly Active Users")
            .Size(700, 380)
            .XAxis(x => x.Categories.AddRange(new[] { "Jan", "Feb", "Mar", "Apr", "May", "Jun" }))
            .Series(s => s.AddColumn("MAU", new double?[] { 12400, 13800, 15200, 14600, 16900, 18400 }))
            .RenderToSvg();
    }

    private static string TemplateTimeSeriesChart()
    {
        return ChartBuilder.Create()
            .ApplyTemplate(ChartTemplate.TimeSeries)
            .Title("Server Response Time (ms)")
            .Size(700, 400)
            .XAxis(x => x.Categories.AddRange(new[] { "00:00", "04:00", "08:00", "12:00", "16:00", "20:00", "23:59" }))
            .YAxis(y => { y.Title = "ms"; y.Min = 0; })
            .Series(s => s
                .AddSpline("P50", new double?[] { 42, 38, 95, 120, 88, 62, 45 })
                .AddSpline("P99", new double?[] { 210, 185, 430, 550, 380, 290, 220 }))
            .RenderToSvg();
    }

    private static string TemplateExecSummaryChart()
    {
        return ChartBuilder.Create()
            .ApplyTemplate(ChartTemplate.ExecutiveSummary)
            .Title("Headcount by Department")
            .Size(700, 420)
            .XAxis(x => x.Categories.AddRange(new[] { "Engineering", "Marketing", "Sales", "Support", "Finance" }))
            .YAxis(y => y.Title = "Employees")
            .Series(s => s.AddBar("2025", new double?[] { 320, 85, 145, 110, 60 }))
            .RenderToSvg();
    }

    private static string JsonRoundTripChart()
    {
        // Build the original chart and serialise it
        string json = ChartBuilder.Create()
            .Theme(GlobalTheme)
            .Title("Deserialised Chart")
            .Subtitle("Built with ChartBuilder.FromJson() — restored from a JSON snapshot")
            .Size(700, 400)
            .AsColumn()
            .XAxis(x => x.Categories.AddRange(new[] { "Jan", "Feb", "Mar", "Apr" }))
            .YAxis(y => { y.Title = "Units"; y.Min = 0; })
            .Series(s => s
                .AddColumn("Product A", new double?[] { 320, 410, 390, 480 })
                .AddColumn("Product B", new double?[] { 210, 255, 245, 290 }))
            .Build()
#if NET6_0_OR_GREATER
            .ToJson();

        // Restore from JSON and render
        return ChartBuilder.FromJson(json).RenderToSvg();
#else
            .Title.Text; // ToJson not supported on this TFM — fallback
        _ = json;
        return ChartBuilder.Create()
            .Title("JSON round-trip requires .NET 6+")
            .Series(s => s.AddLine("N/A", new double?[] { 0 }))
            .RenderToSvg();
#endif
    }

    // ------------------------------------------------------------------ Phase 6

    private static string DumbbellChart()
    {
        var data = new[]
        {
            new RangePoint { Low = 62, High = 89 },
            new RangePoint { Low = 58, High = 82 },
            new RangePoint { Low = 70, High = 95 },
            new RangePoint { Low = 55, High = 78 },
            new RangePoint { Low = 65, High = 91 },
        };
        return ChartBuilder.Create()
            .Theme(GlobalTheme)
            .Title("Employee Satisfaction Score (Before vs After)")
            .Subtitle("Low dot = before programme, high dot = after programme")
            .Size(700, 420)
            .AsStatic()
            .XAxis(x => x.Categories.AddRange(new[] { "Engineering", "Marketing", "Sales", "Support", "Finance" }))
            .YAxis(y => { y.Title = "Score (0–100)"; y.Min = 40; y.Max = 100; })
            .Series(s => s.AddDumbbell("Score Change", data))
            .RenderToSvg();
    }

    private static string StreamChart()
    {
        return ChartBuilder.Create()
            .Theme(GlobalTheme)
            .Title("Website Traffic by Channel")
            .Subtitle("AddStream() — ThemeRiver wiggle baseline")
            .Size(700, 420)
            .AsStatic()
            .XAxis(x => x.Categories.AddRange(new[] { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug" }))
            .Series(s => s
                .AddStream("Organic",  new double?[] { 120, 135, 148, 160, 175, 190, 210, 225 })
                .AddStream("Paid",     new double?[] {  80,  90,  85,  95, 100, 110, 105, 115 })
                .AddStream("Referral", new double?[] {  45,  50,  55,  48,  60,  65,  70,  75 })
                .AddStream("Direct",   new double?[] {  60,  55,  65,  70,  68,  72,  80,  85 }))
            .RenderToSvg();
    }

    private static string GanttChart()
    {
        var tasks = new[]
        {
            new GanttTask { Name = "Requirements",  Start =  0, End =  5,  Label = "Req"  },
            new GanttTask { Name = "UI Design",     Start =  3, End =  9,  Label = "Design" },
            new GanttTask { Name = "Backend Dev",   Start =  6, End = 18,  Label = "Backend" },
            new GanttTask { Name = "Frontend Dev",  Start =  9, End = 20,  Label = "Frontend" },
            new GanttTask { Name = "Integration",   Start = 17, End = 23,  Label = "Integ." },
            new GanttTask { Name = "QA Testing",    Start = 20, End = 27,  Label = "QA" },
            new GanttTask { Name = "Deployment",    Start = 26, End = 30,  Label = "Deploy" },
        };
        return ChartBuilder.Create()
            .Theme(GlobalTheme)
            .Title("Project Sprint Timeline")
            .Subtitle("AddGantt() — tasks on numeric day axis")
            .Size(750, 480)
            .AsAnimated()
            .XAxis(x => { x.Title = "Day"; x.Min = 0; x.Max = 30; })
            .Series(s => s.AddGantt("Sprint 1", tasks))
            .RenderToSvg();
    }

    private static string SankeyChart()
    {
        var nodes = new[]
        {
            new SankeyNode { Name = "Visitors" },
            new SankeyNode { Name = "Home Page" },
            new SankeyNode { Name = "Product" },
            new SankeyNode { Name = "Cart" },
            new SankeyNode { Name = "Checkout" },
            new SankeyNode { Name = "Purchase" },
            new SankeyNode { Name = "Bounce" },
        };
        var links = new[]
        {
            new SankeyLink { From = 0, To = 1, Value = 5000 },
            new SankeyLink { From = 0, To = 6, Value = 2000 },  // bounce
            new SankeyLink { From = 1, To = 2, Value = 3200 },
            new SankeyLink { From = 1, To = 6, Value = 1800 },
            new SankeyLink { From = 2, To = 3, Value = 1800 },
            new SankeyLink { From = 3, To = 4, Value = 1200 },
            new SankeyLink { From = 4, To = 5, Value =  900 },
        };
        return ChartBuilder.Create()
            .Theme(GlobalTheme)
            .Title("E-Commerce Funnel Flow")
            .Subtitle("AddSankey() — visitors to purchase conversion")
            .Size(750, 450)
            .AsStatic()
            .Series(s => s.AddSankey("Funnel", nodes, links))
            .RenderToSvg();
    }

    // ------------------------------------------------------------------ Phase 7

    private static string RangeSelectorChart()
    {
        // Generate 3 years of weekly data (156 data points) as a seeded random walk
        // to simulate realistic time-series traffic spanning several years.
        const int weeks   = 156;   // ~3 years
        var startDate     = new DateTime(2022, 1, 3);  // Monday

        var dates     = new string[weeks];
        var organic   = new double?[weeks];
        var signups   = new double?[weeks];
        var revenue   = new double?[weeks];

        var rng = new System.Random(42);
        double vOrg = 9000, vSig = 1800, vRev = 45000;

        for (int i = 0; i < weeks; i++)
        {
            dates[i]   = startDate.AddDays(i * 7).ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);
            // Random walk with slight upward drift and seasonal oscillation
            double season = 1.0 + 0.18 * Math.Sin(2 * Math.PI * i / 52.0);
            vOrg  = Math.Max(4000,  vOrg  + (rng.NextDouble() - 0.46) * 600  * season);
            vSig  = Math.Max(600,   vSig  + (rng.NextDouble() - 0.46) * 120  * season);
            vRev  = Math.Max(15000, vRev  + (rng.NextDouble() - 0.46) * 3000 * season);
            organic[i] = Math.Round(vOrg);
            signups[i] = Math.Round(vSig);
            revenue[i] = Math.Round(vRev);
        }

        return ChartBuilder.Create()
            .Theme(GlobalTheme)
            .Title("Weekly Traffic — 3-Year Time Series with Range Navigator")
            .Subtitle("Drag the brush handles below to zoom in • Drag the centre to pan the selected window")
            .Size(880, 520)
            .AsInteractive()
            .XAxis(x =>
            {
                x.Categories.AddRange(dates);
                x.Type = Enums.AxisType.DateTime;
            })
            .YAxis(y => { y.Title = "Sessions / Revenue"; y.Min = 0; })
            .Series(s => s
                .AddLine("Organic Sessions", organic)
                .AddLine("Sign-ups",          signups)
                .AddLine("Revenue ($)",        revenue))
            .RangeSelector(rs =>
            {
                rs.Enabled      = true;
                rs.Height       = 58;
                rs.FillColor    = "rgba(100,160,255,0.22)";
                rs.HandleColor  = "#4477bb";
                // Show last 26 weeks (half-year) on initial load
                rs.InitialStart = (weeks - 26.0) / weeks;
                rs.InitialEnd   = 1.0;
            })
            .RenderToSvg();
    }

    private static string SyncTooltipPage()
    {
        // Two charts in the same card share window._tfSync["metrics-demo"].
        // Hovering any data point on one chart mirrors the tooltip on the other.
        string[] months = { "Jan", "Feb", "Mar", "Apr", "May", "Jun" };

        string svgA = ChartBuilder.Create()
            .Theme(GlobalTheme)
            .Title("Revenue ($k)")
            .Size(680, 380)
            .AsInteractive()
            .XAxis(x => x.Categories.AddRange(months))
            .YAxis(y => { y.Title = "Revenue ($k)"; y.Min = 0; })
            .Series(s => s.AddLine("Revenue", new double?[] { 42, 58, 51, 67, 72, 85 }))
            .SyncGroup("metrics-demo")
            .RenderToSvg();

        string svgB = ChartBuilder.Create()
            .Theme(GlobalTheme)
            .Title("Units Sold")
            .Size(680, 380)
            .AsInteractive()
            .XAxis(x => x.Categories.AddRange(months))
            .YAxis(y => { y.Title = "Units"; y.Min = 0; })
            .Series(s => s.AddColumn("Units", new double?[] { 210, 295, 260, 340, 370, 430 }))
            .SyncGroup("metrics-demo")
            .RenderToSvg();

        // Wrap both SVGs in a flex row so the index.html card shows them side-by-side.
        return $"<div style=\"display:flex;gap:12px;flex-wrap:wrap;justify-content:center\">{svgA}{svgB}</div>";
    }

    private static string DrilldownChart()
    {
        // Each quarter has its own child chart; data index matches quarter (0=Q1 … 3=Q4)
        ChartOptions MakeChild(string title, double?[] data, string[] categories) =>
            ChartBuilder.Create()
                .Theme(GlobalTheme)
                .Title(title)
                .Size(700, 400)
                .AsInteractive()
                .XAxis(x => x.Categories.AddRange(categories))
                .YAxis(y => { y.Title = "Revenue ($k)"; y.Min = 0; })
                .Series(s => { s.AddColumn("Monthly", data); }).Colors(new[] { "#78881d" })
                .Build();

        return ChartBuilder.Create()
            .Theme(GlobalTheme)
            .Title("Annual Sales by Quarter — click a bar to drill down")
            .Subtitle("WithDrilldown(dataIndex, child) — each bar shows its own monthly breakdown")
            .Size(700, 420)
            .AsInteractive()
            .XAxis(x => x.Categories.AddRange(new[] { "Q1", "Q2", "Q3", "Q4" }))
            .YAxis(y => { y.Title = "Revenue ($k)"; y.Min = 0; })
            .Series(s => s.AddColumn("Quarterly Sales",
                new double?[] { 120, 145, 138, 162 },
                cfg => cfg
                    .WithDrilldown(0, MakeChild("Q1 — Monthly Breakdown", new double?[] { 35, 42, 43 }, new[] { "Jan","Feb","Mar" }))
                    .WithDrilldown(1, MakeChild("Q2 — Monthly Breakdown", new double?[] { 48, 51, 46 }, new[] { "Apr","May","Jun" }))
                    .WithDrilldown(2, MakeChild("Q3 — Monthly Breakdown", new double?[] { 44, 47, 47 }, new[] { "Jul","Aug","Sep" }))
                    .WithDrilldown(3, MakeChild("Q4 — Monthly Breakdown", new double?[] { 52, 55, 55 }, new[] { "Oct","Nov","Dec" }))))
            .RenderToSvg();
    }
}
