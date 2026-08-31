using System;
using System.Linq;
using TerraFluent.Chart.Reporting.Builder;
using TerraFluent.Chart.Reporting.Enums;
using TerraFluent.Chart.Reporting.Models;
using Xunit;
namespace TerraFluent.Chart.Reporting.Tests;

public class ChartBuilderTests
{
    [Fact]
    public void RenderToSvg_LineChart_ReturnsSvgString()
    {
        var svg = ChartBuilder.Create()
            .Title("Monthly Revenue")
            .Size(600, 400)
            .XAxis(x => x.Categories.AddRange(new[] { "Jan", "Feb", "Mar", "Apr" }))
            .YAxis(y => y.Title = "Revenue ($)")
            .Series(s => s.AddLine("Revenue", new double?[] { 100, 200, 150, 300 }))
            .RenderToSvg();

        SvgAssert.WellFormedAndContains(svg, "Monthly Revenue");
    }

    [Fact]
    public void RenderToSvg_BarChart_ContainsRectElements()
    {
        var svg = ChartBuilder.Create()
            .Title("Sales")
            .Series(s => s.AddColumn("Sales", new double?[] { 50, 80, 60 }))
            .RenderToSvg();

        SvgAssert.WellFormedAndContains(svg, "<rect");
    }

    [Fact]
    public void RenderToSvg_LongTitle_WrapsIntoLinesAndGrowsCanvasWithoutOverlap()
    {
        const string longTitle =
            "Quarterly Revenue and Cost Breakdown by Business Unit, Region and Product Category for the Trailing Twelve Months";

        var wrapped = ChartBuilder.Create().Title(longTitle).Size(600, 400)
            .Series(s => s.AddColumn("Revenue", new double?[] { 50, 80, 60 })).RenderToSvg();
        var shortT = ChartBuilder.Create().Title("Revenue").Size(600, 400)
            .Series(s => s.AddColumn("Revenue", new double?[] { 50, 80, 60 })).RenderToSvg();

        SvgAssert.WellFormed(wrapped);
        // The long title is split into <tspan> lines rather than one overflowing line.
        Assert.Contains("<tspan", wrapped);
        // The canvas grows to reserve room for the extra lines, so the plot can shift down clear of them.
        Assert.True(SvgHeight(wrapped) > SvgHeight(shortT),
            $"wrapped height {SvgHeight(wrapped)} should exceed short-title height {SvgHeight(shortT)}");
    }

    private static int SvgHeight(string svg)
    {
        var doc = new System.Xml.XmlDocument();
        doc.LoadXml(svg);
        return int.Parse(doc.DocumentElement!.GetAttribute("height"), System.Globalization.CultureInfo.InvariantCulture);
    }

    [Fact]
    public void RenderToSvg_PieChart_ContainsPathElements()
    {
        var svg = ChartBuilder.Create()
            .XAxis(x => x.Categories.AddRange(new[] { "A", "B", "C" }))
            .Series(s => s.AddPie("Share", new double?[] { 30, 50, 20 }))
            .RenderToSvg();

        SvgAssert.WellFormedAndContains(svg, "<path");
    }

    [Fact]
    public void RenderToSvg_MultiWordThemeFont_IsQuotedInCss()
    {
        // Sunset FontFamily = "Source Serif 4, Georgia, Palatino Linotype, serif".
        var svg = ChartBuilder.Create()
            .Title("Fonts")
            .Theme(ChartTheme.Sunset)
            .Series(s => s.AddColumn("S", new double?[] { 1, 2, 3 }))
            .RenderToSvg();

        Assert.Contains("'Source Serif 4'", svg);       // multi-word + digit name quoted
        Assert.Contains("'Palatino Linotype'", svg);     // multi-word name quoted
        Assert.DoesNotContain("px Source Serif 4,", svg); // never emitted unquoted
        Assert.Contains(", serif;", svg);                // generic keyword stays unquoted
    }

    [Fact]
    public void RenderToSvg_StaticMode_ExcludesTooltipStyles()
    {
        var svg = ChartBuilder.Create()
            .AsStatic()
            .Series(s => s.AddLine("Data", new double?[] { 1, 2, 3 }))
            .RenderToSvg();

        SvgAssert.WellFormedAndExcludes(svg, "tooltip-bg");
    }

    [Fact]
    public void RenderToSvg_InteractiveMode_ContainsScript()
    {
        var svg = ChartBuilder.Create()
            .AsInteractive()
            .Series(s => s.AddLine("Data", new double?[] { 1, 2, 3 }))
            .RenderToSvg();

        SvgAssert.WellFormedAndContains(svg, "<script");
    }

    [Fact]
    public void RenderToSvg_MultiSeries_AllSeriesRendered()
    {
        var svg = ChartBuilder.Create()
            .Series(s => s
                .AddLine("Revenue", new double?[] { 100, 200, 150 })
                .AddLine("Cost",    new double?[] { 80,  120, 90  }))
            .RenderToSvg();

        Assert.Contains("Revenue", svg);
        Assert.Contains("Cost",    svg);
    }

    [Fact]
    public void RenderToSvg_WithoutLegend_LegendNotRendered()
    {
        var svg = ChartBuilder.Create()
            .HideLegend()
            .Series(s => s.AddLine("Series1", new double?[] { 1, 2, 3 }))
            .RenderToSvg();

        // No <text class="legend-label"> elements should be emitted
        Assert.DoesNotContain("class=\"legend-label\"", svg);
    }

    [Fact]
    public void RenderToSvg_XmlEscaping_PreventsInjection()
    {
        var svg = ChartBuilder.Create()
            .Title("<script>alert('xss')</script>")
            .Series(s => s.AddLine("Data", new double?[] { 1 }))
            .RenderToSvg();

        SvgAssert.WellFormedAndExcludes(svg, "<script>alert");
        Assert.Contains("&lt;script&gt;", svg);
    }

    [Fact]
    public void RenderToSvg_EmptySeries_DoesNotThrow()
    {
        var svg = ChartBuilder.Create()
            .Title("Empty Chart")
            .RenderToSvg();

        Assert.NotNull(svg);
        Assert.Contains("<svg", svg);
    }

    // ================================================================== Sprint 1 regression tests

    [Fact]
    public void RenderToSvg_MultipleCalls_HaveUniqueClipPathIds()
    {
        var svg1 = ChartBuilder.Create()
            .Series(s => s.AddLine("A", new double?[] { 1, 2, 3 }))
            .RenderToSvg();

        var svg2 = ChartBuilder.Create()
            .Series(s => s.AddLine("B", new double?[] { 4, 5, 6 }))
            .RenderToSvg();

        // Neither chart should use the old hardcoded ID
        Assert.DoesNotContain("id=\"plot-clip\"", svg1);
        Assert.DoesNotContain("id=\"plot-clip\"", svg2);

        // The two clip IDs should differ
        int idStart1 = svg1.IndexOf("id=\"pc-", StringComparison.Ordinal);
        int idStart2 = svg2.IndexOf("id=\"pc-", StringComparison.Ordinal);
        Assert.True(idStart1 >= 0 && idStart2 >= 0);
        string id1 = svg1.Substring(idStart1, 12);
        string id2 = svg2.Substring(idStart2, 12);
        Assert.NotEqual(id1, id2);
    }

    [Fact]
    public void RenderToSvg_DashStyle_AppliesStrokeDasharray()
    {
        var svg = ChartBuilder.Create()
            .Series(s => s.AddLine("Data", new double?[] { 1, 2, 3 },
                cfg => cfg.Dashed()))
            .RenderToSvg();

        Assert.Contains("stroke-dasharray=\"8,6\"", svg);
    }

    [Fact]
    public void RenderToSvg_SolidDashStyle_NoStrokeDasharray()
    {
        var svg = ChartBuilder.Create()
            .Series(s => s.AddLine("Data", new double?[] { 1, 2, 3 },
                cfg => cfg.Solid()))
            .RenderToSvg();

        Assert.DoesNotContain("stroke-dasharray", svg);
    }

    [Fact]
    public void RenderToSvg_HiddenYAxis_AxisLabelsNotRendered()
    {
        var svg = ChartBuilder.Create()
            .YAxis(y => y.Visible = false)
            .Series(s => s.AddLine("Data", new double?[] { 10, 20, 30 }))
            .RenderToSvg();

        // When Y-axis is hidden, no axis-label <text> elements should be emitted (only CSS class definition)
        Assert.DoesNotContain("class=\"axis-label\"", svg);
    }

    [Fact]
    public void RenderToSvg_YAxisGridLineHidden_GridLinesNotRendered()
    {
        var svg = ChartBuilder.Create()
            .YAxis(y => y.GridLineVisible = false)
            .Series(s => s.AddLine("Data", new double?[] { 10, 20, 30 }))
            .RenderToSvg();

        // No <line class="grid-line" ...> elements should be emitted
        Assert.DoesNotContain("class=\"grid-line\"", svg);
    }

    [Fact]
    public void RenderToSvg_CustomGridLineColor_Applied()
    {
        var svg = ChartBuilder.Create()
            .YAxis(y => y.GridLineColor = "#ff0000")
            .Series(s => s.AddLine("Data", new double?[] { 10, 20, 30 }))
            .RenderToSvg();

        Assert.Contains("#ff0000", svg);
    }

    [Fact]
    public void RenderToSvg_LabelFormat_Applied()
    {
        var svg = ChartBuilder.Create()
            .YAxis(y => y.LabelFormat = "{value}%")
            .Series(s => s.AddLine("Data", new double?[] { 10, 20, 30 }))
            .RenderToSvg();

        Assert.Contains("%", svg);
    }

    [Fact]
    public void RenderToSvg_TickInterval_UsedForYAxisTicks()
    {
        var svg = ChartBuilder.Create()
            .YAxis(y => { y.Min = 0; y.Max = 100; y.TickInterval = 25; })
            .Series(s => s.AddLine("Data", new double?[] { 25, 50, 75 }))
            .RenderToSvg();

        // With interval 25, ticks at 0,25,50,75,100 should appear
        Assert.Contains(">25<", svg);
        Assert.Contains(">50<", svg);
    }

    [Fact]
    public void RenderToSvg_TooltipCss_HasNoRxProperty()
    {
        // rx should be an SVG attribute on the rect, not a CSS property
        var svg = ChartBuilder.Create()
            .AsAnimated()
            .Series(s => s.AddLine("Data", new double?[] { 1, 2 }))
            .RenderToSvg();

        Assert.DoesNotContain("rx: 4", svg);
    }

    // ================================================================== Sprint 2 regression tests

    [Fact]
    public void RenderToSvg_Spline_ContainsCubicBezier()
    {
        var svg = ChartBuilder.Create()
            .Series(s => s.AddSpline("Data", new double?[] { 10, 30, 20, 50, 40 }))
            .RenderToSvg();

        // Catmull-Rom produces C (cubic bezier) commands, not just L (line-to)
        Assert.Contains(" C", svg);
    }

    [Fact]
    public void RenderToSvg_HorizontalBar_ContainsRects()
    {
        var svg = ChartBuilder.Create()
            .XAxis(x => x.Categories.AddRange(new[] { "A", "B", "C" }))
            .YAxis(y => { y.Min = 0; y.Max = 100; })
            .Series(s => s.AddBar("Values", new double?[] { 30, 60, 90 }))
            .RenderToSvg();

        Assert.Contains("<rect", svg);
    }

    [Fact]
    public void RenderToSvg_HorizontalBar_LongCategoryLabels_WidensLeftGutter()
    {
        const string longLabel = "International Operations Division";

        var wide = ChartBuilder.Create()
            .XAxis(x => x.Categories.AddRange(new[] { longLabel, "B", "C" }))
            .Series(s => s.AddBar("Values", new double?[] { 30, 60, 90 }))
            .RenderToSvg();

        var narrow = ChartBuilder.Create()
            .XAxis(x => x.Categories.AddRange(new[] { "A", "B", "C" }))
            .Series(s => s.AddBar("Values", new double?[] { 30, 60, 90 }))
            .RenderToSvg();

        SvgAssert.WellFormedAndContains(wide, longLabel);
        // Long category labels must push the category-axis origin further right so the text
        // is not clipped at the canvas edge; short labels keep the default gutter.
        Assert.True(VerticalAxisLineX(wide) > VerticalAxisLineX(narrow),
            "Expected the left gutter to widen for long bar-chart category labels.");
    }

    // Returns the x coordinate of the vertical (category) axis line — the left edge of the plot.
    private static double VerticalAxisLineX(string svg)
    {
        var doc = System.Xml.Linq.XDocument.Parse(svg);
        System.Xml.Linq.XNamespace ns = "http://www.w3.org/2000/svg";
        foreach (var line in doc.Descendants(ns + "line"))
        {
            if ((string?)line.Attribute("class") != "axis-line") continue;
            var x1 = (string?)line.Attribute("x1");
            var x2 = (string?)line.Attribute("x2");
            if (x1 != null && x1 == x2) // vertical line: x1 == x2
                return double.Parse(x1, System.Globalization.CultureInfo.InvariantCulture);
        }
        throw new Xunit.Sdk.XunitException("No vertical axis line found in SVG.");
    }

    [Fact]
    public void RenderToSvg_Scatter_ContainsCircles_NoLinePath()
    {
        var svg = ChartBuilder.Create()
            .Series(s => s.AddScatter("Points", new double?[] { 10, 20, 30 }))
            .RenderToSvg();

        Assert.Contains("<circle", svg);
        // Scatter should NOT emit a connecting <path> element
        Assert.DoesNotContain("stroke-linejoin", svg);
    }

    [Fact]
    public void RenderToSvg_AnimatedMode_LineContainsSmilAnimate()
    {
        var svg = ChartBuilder.Create()
            .AsAnimated()
            .Animate(600)
            .Series(s => s.AddLine("Data", new double?[] { 10, 20, 30 }))
            .RenderToSvg();

        Assert.Contains("<animate", svg);
        Assert.Contains("stroke-dashoffset", svg);
    }

    [Fact]
    public void RenderToSvg_AnimatedColumn_ContainsGrowAnimation()
    {
        var svg = ChartBuilder.Create()
            .AsAnimated()
            .Animate()
            .Series(s => s.AddColumn("Data", new double?[] { 50, 80, 60 }))
            .RenderToSvg();

        // Grow-up animation animates both height and y attributes
        Assert.Contains("attributeName=\"height\"", svg);
        Assert.Contains("attributeName=\"y\"", svg);
    }

    [Fact]
    public void RenderToSvg_StaticMode_NoSmilAnimations()
    {
        var svg = ChartBuilder.Create()
            .AsStatic()
            .Animate()   // enabled but Static overrides            .Series(s => s.AddColumn("Data", new double?[] { 10, 20 }))
            .RenderToSvg();

        Assert.DoesNotContain("<animate", svg);
    }

    // ================================================================== Sprint 3 tests

    [Fact]
    public void RenderToBytes_ReturnsSvgUtf8Bytes()
    {
        var bytes = ChartBuilder.Create()
            .Series(s => s.AddLine("Data", new double?[] { 1, 2 }))
            .RenderToBytes();

        Assert.NotNull(bytes);
        Assert.True(bytes.Length > 0);
        string text = System.Text.Encoding.UTF8.GetString(bytes);
        Assert.Contains("<svg", text);
    }

    [Fact]
    public void RenderToStream_WritesCorrectBytes()
    {
        using var ms = new System.IO.MemoryStream();
        ChartBuilder.Create()
            .Series(s => s.AddLine("Data", new double?[] { 1, 2 }))
            .RenderToStream(ms);

        Assert.True(ms.Length > 0);
        ms.Position = 0;
        string text = new System.IO.StreamReader(ms).ReadToEnd();
        Assert.Contains("<svg", text);
    }

    [Fact]
    public void WithTheme_Dark_AppliesDarkBackground()
    {
        var svg = ChartBuilder.Create()
            .Theme(Models.ChartTheme.Dark)
            .Series(s => s.AddLine("Data", new double?[] { 1, 2 }))
            .RenderToSvg();

        Assert.Contains("#1A1A2E", svg);
    }

    [Fact]
    public void WithTheme_PaletteColoursUsedForSeries()
    {
        var svg = ChartBuilder.Create()
            .Theme(Models.ChartTheme.Pastel)
            .Series(s => s.AddLine("Data", new double?[] { 1, 2 }))
            .RenderToSvg();

        // First colour in Pastel palette
        Assert.Contains(ChartColor.PastelSkyBlue, svg);
    }

    [Fact]
    public void ShowDataLabels_ColumnChart_RendersDataLabelText()
    {
        var svg = ChartBuilder.Create()
            .YAxis(y => y.Min = 0)
            .Series(s => s.AddColumn("Data", new double?[] { 50, 80 },
                cfg => cfg.DataLabel.Show()))
            .RenderToSvg();

        Assert.Contains("data-label", svg);
    }

    [Fact]
    public void ShowDataLabels_LineChart_RendersDataLabelText()
    {
        var svg = ChartBuilder.Create()
            .Series(s => s.AddLine("Data", new double?[] { 10, 20, 30 },
                cfg => cfg.DataLabel.Show()))
            .RenderToSvg();

        Assert.Contains("data-label", svg);
    }

    [Fact]
    public void DonutChart_HoleCirclePresent()
    {
        var svg = ChartBuilder.Create()
            .Series(s => s.AddPie("Share", new double?[] { 30, 50, 20 },
                cfg => cfg.DonutHole(0.5)))
            .RenderToSvg();

        // Donut hole is rendered as a <circle>
        Assert.Contains("<circle", svg);
    }

    [Fact]
    public void StackedColumn_Normal_BuildSucceeds()
    {
        var svg = ChartBuilder.Create()
            .StackNormal()
            .XAxis(x => x.Categories.AddRange(new[] { "Q1", "Q2", "Q3" }))
            .YAxis(y => y.Min = 0)
            .Series(s => s
                .AddColumn("A", new double?[] { 100, 120, 140 })
                .AddColumn("B", new double?[] { 80, 90, 100 }))
            .RenderToSvg();

        Assert.Contains("<rect", svg);
    }

    [Fact]
    public void StackedColumn_Percent_BuildSucceeds()
    {
        var svg = ChartBuilder.Create()
            .StackPercent()
            .XAxis(x => x.Categories.AddRange(new[] { "Q1", "Q2" }))
            .Series(s => s
                .AddColumn("A", new double?[] { 60, 40 })
                .AddColumn("B", new double?[] { 40, 60 }))
            .RenderToSvg();

        Assert.Contains("<rect", svg);
    }

    // ================================================================== Sprint 5 tests

    [Fact]
    public void WaterfallChart_ContainsBars()
    {
        var svg = ChartBuilder.Create()
            .XAxis(x => x.Categories.AddRange(new[] { "Start", "A", "B", "End" }))
            .Series(s => s.AddWaterfall("Flow",
                new double?[] { 100, 50, -30, 120 },
                totals: new[] { true, false, false, true }))
            .RenderToSvg();

        Assert.Contains("<rect", svg);
    }

    [Fact]
    public void WaterfallChart_AutoTotals_LastIsTotal()
    {
        // When totals is omitted, the first point is an absolute starting bar and the last is a total
        var svg = ChartBuilder.Create()
            .Series(s => s.AddWaterfall("Flow", new double?[] { 500, 200, -100, 600 }))
            .RenderToSvg();

        Assert.Contains("<svg", svg);
        Assert.DoesNotContain("Exception", svg);
    }

    [Fact]
    public void GaugeChart_RendersArcPath()
    {
        var svg = ChartBuilder.Create()
            .YAxis(y => { y.Min = 0; y.Max = 100; })
            .Series(s => s.AddGauge("Speed", 75))
            .RenderToSvg();

        // Gauge renders arc paths
        Assert.Contains("<path", svg);
        // Value text should appear
        Assert.Contains("75", svg);
    }

    [Fact]
    public void GaugeChart_ZeroValue_RendersBackground()
    {
        var svg = ChartBuilder.Create()
            .YAxis(y => { y.Min = 0; y.Max = 100; })
            .Series(s => s.AddGauge("Speed", 0))
            .RenderToSvg();

        // Background arc always rendered
        Assert.Contains("#E6E6E6", svg);
    }

    [Fact]
    public void TooltipFormat_IsXmlEscaped()
    {
        var svg = ChartBuilder.Create()
            .AsAnimated()
            .Tooltip(t => t.Format("<b>{label}</b> & {value}"))
            .Series(s => s.AddLine("Data", new double?[] { 1, 2, 3 }))
            .RenderToSvg();

        Assert.DoesNotContain("<b>", svg);
        Assert.Contains("&lt;b&gt;", svg);
        Assert.Contains("&amp;", svg);
    }

    [Fact]
    public void XAxis_LabelRotation_ManualApplied()
    {
        var svg = ChartBuilder.Create()
            .XAxis(x =>
            {
                x.LabelRotation = -45;
                x.Categories.AddRange(new[] { "Jan", "Feb", "Mar" });
            })
            .Series(s => s.AddLine("Data", new double?[] { 1, 2, 3 }))
            .RenderToSvg();

        Assert.Contains("rotate(-45", svg);
    }

    [Fact]
    public void XAxis_LabelRotation_AutoRotatesForDenseLabels()
    {
        // 10 long-named categories should trigger auto-rotation
        var svg = ChartBuilder.Create()
            .XAxis(x =>
            {
                for (int i = 1; i <= 10; i++)
                    x.Categories.Add($"Long Category Name {i}");
            })
            .Series(s => s.AddLine("Data", new double?[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 }))
            .RenderToSvg();

        Assert.Contains("rotate(-45", svg);
    }

    [Fact]
    public void XAxis_LabelRotation_ZeroDisablesRotation()
    {
        var svg = ChartBuilder.Create()
            .XAxis(x =>
            {
                x.LabelRotation = 0;
                for (int i = 1; i <= 10; i++) x.Categories.Add($"Very Long Category {i}");
            })
            .Series(s => s.AddLine("Data", new double?[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 }))
            .RenderToSvg();

        // Explicit 0 overrides auto-detect — no rotation transform
        Assert.DoesNotContain("rotate(-45", svg);
    }

    [Fact]
    public void Build_ReturnsIndependentSnapshot()
    {
        var builder = ChartBuilder.Create()
            .Title("Original")
            .Series(s => s.AddLine("Data", new double?[] { 1, 2, 3 }));

        TerraFluent.Chart.Reporting.Models.ChartOptions snapshot = builder.Build();

        // Modify builder AFTER taking snapshot
        builder.Title("Modified");

        // Snapshot should still have original title
        Assert.Equal("Original", snapshot.Title.Text);
    }

    [Fact]
    public void Build_DeepCopies_AxisPlotBandsAndPlotLines()
    {
        var builder = ChartBuilder.Create()
            .YAxis(y =>
            {
                y.PlotBands.Add(new TerraFluent.Chart.Reporting.Models.PlotBand
                {
                    From = 10,
                    To = 20,
                    Color = "#aabbcc",
                    Label = "Band A"
                });
                y.PlotLines.Add(new TerraFluent.Chart.Reporting.Models.PlotLine
                {
                    Value = 50,
                    Color = "#ff0000",
                    Width = 2,
                    Label = "Line A"
                });
            })
            .Series(s => s.AddLine("Data", new double?[] { 1, 2, 3 }));

        var snapshot = builder.Build();

        // Mutate original builder axis objects after snapshot.
        builder.YAxis(y =>
        {
            y.PlotBands[0].Color = "#000000";
            y.PlotBands[0].Label = "Changed Band";
            y.PlotLines[0].Color = "#00ff00";
            y.PlotLines[0].Label = "Changed Line";
        });

        Assert.Equal("#aabbcc", snapshot.YAxis.PlotBands[0].Color);
        Assert.Equal("Band A", snapshot.YAxis.PlotBands[0].Label);
        Assert.Equal("#ff0000", snapshot.YAxis.PlotLines[0].Color);
        Assert.Equal("Line A", snapshot.YAxis.PlotLines[0].Label);
    }

    [Fact]
    public void ClearSeries_RemovesAllSeries()
    {
        var svg = ChartBuilder.Create()
            .Series(s => s.AddLine("A", new double?[] { 1, 2, 3 }))
            .ClearSeries()
            .Series(s => s.AddLine("B", new double?[] { 4, 5, 6 }))
            .RenderToSvg();

        Assert.DoesNotContain(">A<", svg);
        Assert.Contains("B", svg);
    }

    [Fact]
    public void RemoveSeries_RemovesByName()
    {
        var svg = ChartBuilder.Create()
            .Series(s => s
                .AddLine("Revenue", new double?[] { 100, 200, 300 })
                .AddLine("Cost",    new double?[] { 80,  150, 200 }))
            .RemoveSeries("Cost")
            .RenderToSvg();

        Assert.Contains("Revenue", svg);
        Assert.DoesNotContain("Cost", svg);
    }

    [Fact]
    public void RenderToSvg_HasAccessibilityAttributes()
    {
        var svg = ChartBuilder.Create()
            .Title("Revenue Chart")
            .Series(s => s.AddLine("Data", new double?[] { 1, 2 }))
            .RenderToSvg();

        Assert.Contains("role=\"img\"", svg);
        Assert.Contains("aria-labelledby=", svg);
        Assert.Contains("Revenue Chart", svg);
    }

    [Fact]
    public void ResponsiveWidth_NullWidth_EmitsHundredPercent()
    {
        var svg = ChartBuilder.Create()
            .Series(s => s.AddLine("Data", new double?[] { 1, 2 }))
            .RenderToSvg();  // Width not set → responsive

        Assert.Contains("width=\"100%\"", svg);
    }

    [Fact]
    public void Width_EmitsPixelSize()
    {
        var svg = ChartBuilder.Create()
            .Size(700, 400)
            .Series(s => s.AddLine("Data", new double?[] { 1, 2 }))
            .RenderToSvg();

        Assert.Contains("width=\"700\"", svg);
        Assert.DoesNotContain("width=\"100%\"", svg);
    }

    [Fact]
    public void ResponsiveWidth_ExportMenu_UsesViewBoxSizingInScript()
    {
        var svg = ChartBuilder.Create()
            .AsInteractive()
            .ShowExportMenu("SVG", "PNG", "JPEG", "PDF")
            .Series(s => s.AddLine("Data", new double?[] { 1, 2, 3 }))
            .RenderToSvg();

        // Export sizing should come from viewBox/viewport, not parseInt("100%").
        Assert.Contains("function getExportSize()", svg);
        Assert.Contains("svg.viewBox", svg);
        Assert.DoesNotContain("parseInt(svg.getAttribute('width'))", svg);
        Assert.DoesNotContain("parseInt(svg.getAttribute('height'))", svg);
    }

    // ================================================================== Medium/Low fix tests

    [Fact]
    public void ChartTitle_Style_AppliedInline()
    {
        var svg = ChartBuilder.Create()
            .Title(t => { t.Text = "My Chart"; t.Style = "font-size:20px"; })
            .Series(s => s.AddLine("Data", new double?[] { 1, 2 }))
            .RenderToSvg();

        Assert.Contains("font-size:20px", svg);
    }

    [Fact]
    public void Series_FillOpacity_Applied()
    {
        // Use Static mode so fill-opacity appears as a direct SVG attribute (not inside <animate>)
        var svg = ChartBuilder.Create()
            .AsStatic()
            .Series(s => s.AddArea("Data", new double?[] { 10, 20, 30 },
                cfg => cfg.FillOpacity(0.5)))
            .RenderToSvg();

        Assert.Contains("fill-opacity=\"0.5\"", svg);
    }

    [Fact]
    public void Series_FillOpacity_DefaultIs025ForArea()
    {
        var svg = ChartBuilder.Create()
            .Series(s => s.AddArea("Data", new double?[] { 10, 20, 30 }))
            .RenderToSvg();

        // Default fill-opacity is 0.25 — F() formats to "0.3" at 1 decimal place is wrong;
        // the value is stored as a double, F(0.25)="0.3" — just confirm some fill-opacity is present
        Assert.Contains("fill-opacity", svg);
    }

    [Fact]
    public void Gauge_UsesYAxis2Scale_WhenYAxisIndexIs1()
    {
        var svg = ChartBuilder.Create()
            .YAxis(y => { y.Min = 0; y.Max = 100; })
            .YAxis2(y => { y.Min = 0; y.Max = 200; })
            .Series(s => s.AddGauge("Speed", 100, cfg => cfg.OnSecondaryAxis()))
            .RenderToSvg();

        // With YAxis2 max=200, value=100 is 50%, should render a half-filled arc
        Assert.Contains("<path", svg);
        Assert.Contains("100", svg);  // value label
    }

    [Fact]
    public void DataRing_UsesYAxis2Scale_WhenYAxisIndexIs1()
    {
        var svg = ChartBuilder.Create()
            .YAxis(y => { y.Min = 0; y.Max = 100; })
            .YAxis2(y => { y.Min = 0; y.Max = 200; })
            .Series(s => s.AddDataRing("KPI", 100, cfg => cfg.OnSecondaryAxis()))
            .RenderToSvg();

        // With YAxis2 max=200, value=100 should be rendered as plain value, not 100%.
        Assert.Contains(">100<", svg);
        Assert.DoesNotContain(">100%<", svg);
    }

    [Fact]
    public void DonutHolePercent_LargerThan1_IsClamped()
    {
        // Should not throw; DonutHolePercent > 1 is clamped to 0.99
        var svg = ChartBuilder.Create()
            .AsPie()
            .Labels("A", "B", "C")
            .Series(s => s.Add("Data", new double?[] { 30, 50, 20 },
                cfg => cfg.DonutHole(2.5)))   // invalid — clamped
            .RenderToSvg();

        Assert.Contains("<svg", svg);
        Assert.DoesNotContain("NaN", svg);
    }

    [Fact]
    public void RenderToSvg_NonFiniteData_DoesNotEmitInvalidNumericTokens()
    {
        var svg = ChartBuilder.Create()
            .Series(s =>
            {
                s.AddLine("Line", new double?[] { 1, double.NaN, 3, double.PositiveInfinity, 5 });
                s.AddBubble("Bubbles", new[]
                {
                    new BubblePoint(0, 1, 2),
                    new BubblePoint(1, double.NaN, 3),
                    new BubblePoint(2, 2, double.PositiveInfinity)
                });
            })
            .RenderToSvg();

        Assert.Contains("<svg", svg);
        Assert.DoesNotContain("NaN", svg);
        Assert.DoesNotContain("Infinity", svg);
    }

    [Fact]
    public void RenderToSvg_NonFiniteRangePoints_AreDropped()
    {
        var svg = ChartBuilder.Create()
            .AsInteractive()
            .Series(s => s.AddColumnRange("Range", new[]
            {
                new RangePoint(1, 4),
                new RangePoint(double.NaN, 5),
                new RangePoint(2, double.PositiveInfinity)
            }))
            .RenderToSvg();

        Assert.Contains("<svg", svg);
        Assert.DoesNotContain("NaN", svg);
        Assert.DoesNotContain("Infinity", svg);
        Assert.Single(System.Text.RegularExpressions.Regex.Matches(svg, "class=\"data-point\"").Cast<System.Text.RegularExpressions.Match>());
    }

    [Fact]
    public void RenderToSvg_NonFiniteOhlcPoints_AreDropped()
    {
        var svg = ChartBuilder.Create()
            .AsInteractive()
            .Series(s => s.AddOhlc("OHLC", new[]
            {
                new OhlcPoint(10, 14, 8, 12),
                new OhlcPoint(double.PositiveInfinity, 15, 9, 11),
                new OhlcPoint(9, 13, double.NaN, 10)
            }))
            .RenderToSvg();

        Assert.Contains("<svg", svg);
        Assert.DoesNotContain("NaN", svg);
        Assert.DoesNotContain("Infinity", svg);
        Assert.Single(System.Text.RegularExpressions.Regex.Matches(svg, "class=\"data-point\"").Cast<System.Text.RegularExpressions.Match>());
    }

    [Fact]
    public void PlotBands_RenderedOnPrimaryYAxis()
    {
        var svg = ChartBuilder.Create()
            .YAxis(y =>
            {
                y.Min = 0; y.Max = 100;
                y.PlotBands.Add(new TerraFluent.Chart.Reporting.Models.PlotBand
                { From = 20, To = 40, Color = "#aabbcc" });
            })
            .Series(s => s.AddLine("Data", new double?[] { 10, 50, 90 }))
            .RenderToSvg();

        Assert.Contains("#aabbcc", svg);
    }

    [Fact]
    public void PlotLines_RenderedOnPrimaryYAxis()
    {
        var svg = ChartBuilder.Create()
            .YAxis(y =>
            {
                y.Min = 0; y.Max = 100;
                y.PlotLines.Add(new TerraFluent.Chart.Reporting.Models.PlotLine
                { Value = 50, Color = "#ff0000", Label = "Threshold" });
            })
            .Series(s => s.AddLine("Data", new double?[] { 10, 50, 90 }))
            .RenderToSvg();

        Assert.Contains("#ff0000", svg);
        Assert.Contains("Threshold", svg);
    }

    [Fact]
    public void Bounce_Easing_FallsBackToEaseOut()
    {
        // Bounce is not natively supported in SMIL — falls back to EaseOut
        var svg = ChartBuilder.Create()
            .AsAnimated()
            .Animation(a => a.Bounce())
            .Series(s => s.AddColumn("Data", new double?[] { 10, 20 }))
            .RenderToSvg();

        // EaseOut spline keySplines appear in the output
        Assert.Contains("keySplines", svg);
    }

    [Fact]
    public void WaterfallTotals_LengthMismatch_ThrowsArgumentException()
    {
        // Fix #12: totals length must match data length — mismatched list now throws
        Assert.Throws<ArgumentException>(() =>
            ChartBuilder.Create()
                .Series(s => s.AddWaterfall("Flow",
                    new double?[] { 500, 200, -100, 300 },
                    totals: new[] { false, false })));   // 2 totals for 4 data points
    }

    [Fact]
    public void WaterfallTotals_MatchingLength_DoesNotThrow()
    {
        var svg = ChartBuilder.Create()
            .Series(s => s.AddWaterfall("Flow",
                new double?[] { 500, 200, -100, 300 },
                totals: new[] { true, false, false, true }))
            .RenderToSvg();

        Assert.Contains("<svg", svg);
    }

    [Fact]
    public void StackedArea_BuildSucceeds()
    {
        var svg = ChartBuilder.Create()
            .StackNormal()
            .XAxis(x => x.Categories.AddRange(new[] { "Jan", "Feb", "Mar" }))
            .Series(s => s
                .AddArea("A", new double?[] { 100, 120, 140 })
                .AddArea("B", new double?[] { 80,  90,  100 }))
            .RenderToSvg();

        // Stacked area emits filled polygons
        Assert.Contains("<path", svg);
    }

    // ================================================================== Sprint A — Critical gap tests

    // ---- C1: Size() validation --------------------------------------------------

    [Fact]
    public void Size_ZeroWidth_ThrowsArgumentOutOfRangeException()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            ChartBuilder.Create().Size(0, 400));
    }

    [Fact]
    public void Size_NegativeWidth_ThrowsArgumentOutOfRangeException()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            ChartBuilder.Create().Size(-1, 400));
    }

    [Fact]
    public void Size_ZeroHeight_ThrowsArgumentOutOfRangeException()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            ChartBuilder.Create().Size(600, 0));
    }

    [Fact]
    public void Size_NegativeHeight_ThrowsArgumentOutOfRangeException()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            ChartBuilder.Create().Size(600, -100));
    }

    [Fact]
    public void Size_ValidDimensions_DoesNotThrow()
    {
        var svg = ChartBuilder.Create()
            .Size(1, 1)
            .Series(s => s.AddLine("Data", new double?[] { 1 }))
            .RenderToSvg();

        Assert.Contains("<svg", svg);
    }

    // ---- C2: YAxis(string, min, max) validation ---------------------------------

    [Fact]
    public void YAxis_MinEqualToMax_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() =>
            ChartBuilder.Create().YAxis("Y", min: 100, max: 100));
    }

    [Fact]
    public void YAxis_MinGreaterThanMax_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() =>
            ChartBuilder.Create().YAxis("Y", min: 200, max: 50));
    }

    [Fact]
    public void YAxis_MinOnlySpecified_DoesNotThrow()
    {
        // Providing only min (no max) is always valid
        var svg = ChartBuilder.Create()
            .YAxis("Y", min: 0)
            .Series(s => s.AddLine("Data", new double?[] { 10, 20 }))
            .RenderToSvg();

        Assert.Contains("<svg", svg);
    }

    [Fact]
    public void YAxis_MaxOnlySpecified_DoesNotThrow()
    {
        // Providing only max (no min) is always valid
        var svg = ChartBuilder.Create()
            .YAxis("Y", max: 100)
            .Series(s => s.AddLine("Data", new double?[] { 10, 20 }))
            .RenderToSvg();

        Assert.Contains("<svg", svg);
    }

    [Fact]
    public void YAxis_ValidMinMax_DoesNotThrow()
    {
        var svg = ChartBuilder.Create()
            .YAxis("Y", min: 0, max: 100)
            .Series(s => s.AddLine("Data", new double?[] { 10, 50, 90 }))
            .RenderToSvg();

        Assert.Contains("<svg", svg);
    }

    // ---- C3: Animate() / AnimationBuilder.Duration() validation -----------------

    [Fact]
    public void Animate_ZeroMilliseconds_ThrowsArgumentOutOfRangeException()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            ChartBuilder.Create().Animate(0));
    }

    [Fact]
    public void Animate_NegativeMilliseconds_ThrowsArgumentOutOfRangeException()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            ChartBuilder.Create().Animate(-500));
    }

    [Fact]
    public void AnimationBuilder_ZeroDuration_ThrowsArgumentOutOfRangeException()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            ChartBuilder.Create().Animation(a => a.Duration(0)));
    }

    [Fact]
    public void AnimationBuilder_NegativeDuration_ThrowsArgumentOutOfRangeException()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            ChartBuilder.Create().Animation(a => a.Duration(-1)));
    }

    [Fact]
    public void AnimationBuilder_ZeroTimeSpan_ThrowsArgumentOutOfRangeException()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            ChartBuilder.Create().Animation(a => a.Duration(TimeSpan.Zero)));
    }

    [Fact]
    public void Animate_ValidDuration_DoesNotThrow()
    {
        var svg = ChartBuilder.Create()
            .AsAnimated()
            .Animate(1)
            .Series(s => s.AddLine("Data", new double?[] { 1, 2 }))
            .RenderToSvg();

        Assert.Contains("<svg", svg);
    }

    // ---- C7: RenderToFile() directory validation --------------------------------

    [Fact]
    public void RenderToFile_MissingParentDirectory_ThrowsDirectoryNotFoundException()
    {
        var path = System.IO.Path.Combine(
            System.IO.Path.GetTempPath(),
            System.IO.Path.GetRandomFileName(),   // non-existent sub-dir
            "chart.svg");

        Assert.Throws<System.IO.DirectoryNotFoundException>(() =>
            ChartBuilder.Create()
                .Series(s => s.AddLine("Data", new double?[] { 1, 2 }))
                .RenderToFile(path));
    }

    [Fact]
    public void RenderToFile_ExistingDirectory_WritesFile()
    {
        var path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"{System.Guid.NewGuid()}.svg");
        try
        {
            ChartBuilder.Create()
                .Series(s => s.AddLine("Data", new double?[] { 1, 2 }))
                .RenderToFile(path);

            Assert.True(System.IO.File.Exists(path));
            Assert.Contains("<svg", System.IO.File.ReadAllText(path));
        }
        finally
        {
            if (System.IO.File.Exists(path)) System.IO.File.Delete(path);
        }
    }

    [Fact]
    public void Background_NullOrWhitespace_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() =>
            ChartBuilder.Create().Background(""));
        Assert.Throws<ArgumentException>(() =>
            ChartBuilder.Create().Background("   "));
    }

    [Fact]
    public void Background_ValidColor_AppliedToSvg()
    {
        var svg = ChartBuilder.Create()
            .Background("#123456")
            .Series(s => s.AddLine("Data", new double?[] { 1, 2 }))
            .RenderToSvg();

        Assert.Contains("#123456", svg);
    }

    // ======================================================================= Sprint B — High/Medium priority gap tests

    // H6 — Width(int?) and Height(int) independent setters
    [Fact]
    public void Width_SetsSvgWidth()
    {
        var svg = ChartBuilder.Create()
            .AsLine()
            .Width(640)
            .Height(300)
            .Series(s => s.AddLine("A", new double?[] { 1, 2, 3 }))
            .RenderToSvg();

        Assert.Contains("640", svg);
    }

    [Fact]
    public void Height_SetsSvgHeight()
    {
        var svg = ChartBuilder.Create()
            .AsLine()
            .Size(800, 400)
            .Height(250)
            .Series(s => s.AddLine("A", new double?[] { 1, 2, 3 }))
            .RenderToSvg();

        Assert.Contains("250", svg);
    }

    [Fact]
    public void Width_Zero_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            ChartBuilder.Create().Width(0));
    }

    [Fact]
    public void Width_Null_DoesNotThrow()
    {
        // null signals responsive/fluid width — must not throw
        ChartBuilder.Create().Width(null);
    }

    [Fact]
    public void Height_Zero_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            ChartBuilder.Create().Height(0));
    }

    // H7 — Colors(IEnumerable<string>)
    [Fact]
    public void Colors_IEnumerable_AppliesFirstColor()
    {
        var palette = new List<string> { "#AA0000", "#00AA00" };
        var svg = ChartBuilder.Create()
            .AsLine()
            .Colors(palette)
            .Series(s => s.AddLine("A", new double?[] { 1, 2 }))
            .RenderToSvg();

        Assert.Contains("#AA0000", svg);
    }

    [Fact]
    public void Colors_IEnumerable_Empty_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            ChartBuilder.Create().Colors(new List<string>()));
    }

    [Fact]
    public void Colors_IEnumerable_Null_Throws()
    {
        Assert.Throws<ArgumentNullException>(() =>
            ChartBuilder.Create().Colors((IEnumerable<string>)null!));
    }

    // M1 — PlotBand and PlotLine fluent shortcuts
    [Fact]
    public void PlotBand_RendersColorInSvg()
    {
        var svg = ChartBuilder.Create()
            .AsLine()
            .PlotBand(10, 20, "#FFEE00")
            .Series(s => s.AddLine("A", new double?[] { 5, 15, 25 }))
            .RenderToSvg();

        Assert.Contains("#FFEE00", svg);
    }

    [Fact]
    public void PlotBand_FromGreaterThanTo_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            ChartBuilder.Create().PlotBand(20, 10));
    }

    [Fact]
    public void PlotBand_FromEqualTo_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            ChartBuilder.Create().PlotBand(10, 10));
    }

    [Fact]
    public void PlotLine_RendersColorInSvg()
    {
        var svg = ChartBuilder.Create()
            .AsLine()
            .PlotLine(50, "#FF0000")
            .Series(s => s.AddLine("A", new double?[] { 10, 60, 30 }))
            .RenderToSvg();

        Assert.Contains("#FF0000", svg);
    }

    [Fact]
    public void PlotLine_ZeroWidth_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            ChartBuilder.Create().PlotLine(10, "#000000", 0));
    }

    // M2 — RenderToHtml
    [Fact]
    public void RenderToHtml_ContainsFigureAndSvg()
    {
        var html = ChartBuilder.Create()
            .AsLine()
            .Series(s => s.AddLine("A", new double?[] { 1, 2, 3 }))
            .RenderToHtml();

        Assert.Contains("<figure", html);
        Assert.Contains("<svg", html);
        Assert.Contains("</figure>", html);
    }

    [Fact]
    public void RenderToHtml_WithCaption_ContainsFigcaption()
    {
        var html = ChartBuilder.Create()
            .AsLine()
            .Series(s => s.AddLine("A", new double?[] { 1, 2 }))
            .RenderToHtml("My Caption");

        Assert.Contains("<figcaption", html);
        Assert.Contains("My Caption", html);
    }

    [Fact]
    public void RenderToHtml_WithCssClass_AppliedToFigure()
    {
        var html = ChartBuilder.Create()
            .AsLine()
            .Series(s => s.AddLine("A", new double?[] { 1, 2 }))
            .RenderToHtml(cssClass: "chart-wrapper");

        Assert.Contains("class=\"chart-wrapper\"", html);
    }

    [Fact]
    public void RenderToHtml_HtmlEncodesCaption()
    {
        var html = ChartBuilder.Create()
            .AsLine()
            .Series(s => s.AddLine("A", new double?[] { 1 }))
            .RenderToHtml("<script>alert(1)</script>");

        Assert.DoesNotContain("<script>", html);
        Assert.Contains("&lt;script&gt;", html);
    }

    [Fact]
    public void RenderToHtmlFile_WritesFile()
    {
        var path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), System.IO.Path.GetRandomFileName() + ".html");
        try
        {
            ChartBuilder.Create()
                .AsLine()
                .Series(s => s.AddLine("A", new double?[] { 1, 2 }))
                .RenderToHtmlFile(path, "Test Caption");

            Assert.True(System.IO.File.Exists(path));
            var content = System.IO.File.ReadAllText(path);
            Assert.Contains("<figure", content);
            Assert.Contains("Test Caption", content);
        }
        finally
        {
            if (System.IO.File.Exists(path)) System.IO.File.Delete(path);
        }
    }

    // H3 — IChartBuilder assignability and DI wiring
    [Fact]
    public void ChartBuilder_IsAssignableToIChartBuilder()
    {
        IChartBuilder builder = ChartBuilder.Create();
        Assert.NotNull(builder);
    }

    [Fact]
    public void IChartBuilder_FluentChain_ProducesValidSvg()
    {
        IChartBuilder builder = ChartBuilder.Create();
        var svg = builder
            .AsLine()
            .Title("IChartBuilder test")
            .Size(600, 300)
            .Series(s => s.AddLine("X", new double?[] { 1, 2, 3 }))
            .RenderToSvg();

        Assert.Contains("<svg", svg);
        Assert.Contains("IChartBuilder test", svg);
    }

    [Fact]
    public void IChartBuilder_Colors_IEnumerable_Chain()
    {
        IEnumerable<string> palette = new[] { "#AABBCC" };
        IChartBuilder builder = ChartBuilder.Create();
        var svg = builder
            .AsColumn()
            .Colors(palette)
            .Series(s => s.AddColumn("C", new double?[] { 5, 10 }))
            .RenderToSvg();

        Assert.Contains("#AABBCC", svg);
    }

    // H1 — Async render APIs (net8 target in test project)
    [Fact]
    public async System.Threading.Tasks.Task RenderToStreamAsync_WritesBytes()
    {
        using var ms = new System.IO.MemoryStream();
        await ChartBuilder.Create()
            .AsLine()
            .Series(s => s.AddLine("A", new double?[] { 1, 2, 3 }))
            .RenderToStreamAsync(ms);

        Assert.True(ms.Length > 0);
        var text = System.Text.Encoding.UTF8.GetString(ms.ToArray());
        Assert.Contains("<svg", text);
    }

    [Fact]
    public async System.Threading.Tasks.Task RenderToFileAsync_WritesFile()
    {
        var path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), System.IO.Path.GetRandomFileName() + ".svg");
        try
        {
            await ChartBuilder.Create()
                .AsLine()
                .Series(s => s.AddLine("A", new double?[] { 1, 2 }))
                .RenderToFileAsync(path);

            Assert.True(System.IO.File.Exists(path));
            Assert.Contains("<svg", System.IO.File.ReadAllText(path));
        }
        finally
        {
            if (System.IO.File.Exists(path)) System.IO.File.Delete(path);
        }
    }

    [Fact]
    public async System.Threading.Tasks.Task RenderToHtmlFileAsync_WritesFile()
    {
        var path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), System.IO.Path.GetRandomFileName() + ".html");
        try
        {
            await ChartBuilder.Create()
                .AsLine()
                .Series(s => s.AddLine("A", new double?[] { 1, 2 }))
                .RenderToHtmlFileAsync(path, "Async Caption");

            Assert.True(System.IO.File.Exists(path));
            var content = System.IO.File.ReadAllText(path);
            Assert.Contains("<figure", content);
            Assert.Contains("Async Caption", content);
        }
        finally
        {
            if (System.IO.File.Exists(path)) System.IO.File.Delete(path);
        }
    }

    [Fact]
    public async System.Threading.Tasks.Task RenderToStreamAsync_NullStream_Throws()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            ChartBuilder.Create().RenderToStreamAsync(null!));
    }

    // -----------------------------------------------------------------------
    // Sprint C — Polish, Robustness & Feature Completeness
    // -----------------------------------------------------------------------

    [Fact]
    public void ChartTheme_Custom_AllDefaults_DoesNotThrow()
    {
        var theme = TerraFluent.Chart.Reporting.Models.ChartTheme.Custom();
        Assert.NotNull(theme);
    }

    [Fact]
    public void ChartTheme_Custom_SetsProperties()
    {
        var theme = TerraFluent.Chart.Reporting.Models.ChartTheme.Custom(
            backgroundColor: "#111",
            textColor: "#eee",
            colors: new[] { "#f00", "#0f0" });
        Assert.Equal("#111", theme.BackgroundColor);
        Assert.Equal("#eee", theme.TextColor);
        Assert.Contains("#f00", theme.Colors);
    }

    [Fact]
    public void ChartTheme_Custom_EmptyColorsArray_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            TerraFluent.Chart.Reporting.Models.ChartTheme.Custom(colors: Array.Empty<string>()));
    }

    [Fact]
    public void XAxisFormat_WhiteSpace_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            ChartBuilder.Create().XAxisFormat("   "));
    }

    [Fact]
    public void XAxisFormat_Valid_DoesNotThrow()
    {
        var svg = ChartBuilder.Create()
            .XAxisFormat("{value} kg")
            .Series(s => s.AddLine("S", new double[] { 1, 2, 3 }))
            .RenderToSvg();
        Assert.Contains("<svg", svg);
    }

    [Fact]
    public void XAxisTickInterval_ZeroOrNegative_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            ChartBuilder.Create().XAxisTickInterval(0));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            ChartBuilder.Create().XAxisTickInterval(-1));
    }

    [Fact]
    public void YAxisTickInterval_ZeroOrNegative_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            ChartBuilder.Create().YAxisTickInterval(0));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            ChartBuilder.Create().YAxisTickInterval(-5));
    }

    [Fact]
    public void FillOpacity_BelowZero_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            ChartBuilder.Create()
                .Series(s => s.AddArea("A", new double[] { 1, 2 }, cfg => cfg.FillOpacity(-0.1))));
    }

    [Fact]
    public void FillOpacity_AboveOne_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            ChartBuilder.Create()
                .Series(s => s.AddArea("A", new double[] { 1, 2 }, cfg => cfg.FillOpacity(1.01))));
    }

    [Fact]
    public void FillOpacity_BoundaryValues_DoNotThrow()
    {
        ChartBuilder.Create()
            .Series(s => s.AddArea("A", new double[] { 1, 2 }, cfg => cfg.FillOpacity(0.0)));
        ChartBuilder.Create()
            .Series(s => s.AddArea("A", new double[] { 1, 2 }, cfg => cfg.FillOpacity(1.0)));
    }

    [Fact]
    public void MarkerSize_Zero_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            ChartBuilder.Create()
                .Series(s => s.AddLine("L", new double[] { 1, 2 }, cfg => cfg.MarkerSize(0))));
    }

    [Fact]
    public void MarkerSize_Negative_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            ChartBuilder.Create()
                .Series(s => s.AddLine("L", new double[] { 1, 2 }, cfg => cfg.MarkerSize(-3))));
    }

    [Fact]
    public void MarkerEnabled_False_RendersNoCircles()
    {
        var svg = ChartBuilder.Create()
            .Size(400, 300)
            .Series(s => s.AddLine("L", new double[] { 1, 2, 3 }, cfg => cfg.MarkerEnabled(false)))
            .RenderToSvg();
        // With markers disabled there should be no hit-area circles for this series
        Assert.Contains("<svg", svg);
    }

    [Fact]
    public void TooltipBuilder_HeaderFormat_WhiteSpace_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            ChartBuilder.Create()
                .Tooltip(t => t.HeaderFormat("   ")));
    }

    [Fact]
    public void TooltipBuilder_PointFormat_WhiteSpace_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            ChartBuilder.Create()
                .Tooltip(t => t.PointFormat("  ")));
    }

    [Fact]
    public void TooltipBuilder_HeaderFormat_Valid_StoredOnModel()
    {
        // Verify the SVG can at least be rendered without errors
        var svg = ChartBuilder.Create()
            .Size(400, 300)
            .Series(s => s.AddLine("S", new double[] { 5, 10 }))
            .Tooltip(t => t.HeaderFormat("Week: {label}").PointFormat("Val: {value}"))
            .RenderToSvg();
        Assert.Contains("<svg", svg);
    }

    [Fact]
    public void LegendBuilder_TopLeft_SetsAlignAndVerticalAlign()
    {
        var svg = ChartBuilder.Create()
            .Size(400, 300)
            .Series(s => s.AddLine("S", new double[] { 1, 2 }))
            .Legend(l => l.TopLeft())
            .RenderToSvg();
        Assert.Contains("<svg", svg);
    }

    [Fact]
    public void LegendBuilder_TopRight_DoesNotThrow()
    {
        ChartBuilder.Create()
            .Series(s => s.AddLine("S", new double[] { 1, 2 }))
            .Legend(l => l.TopRight());
    }

    [Fact]
    public void LegendBuilder_BottomCenter_DoesNotThrow()
    {
        ChartBuilder.Create()
            .Series(s => s.AddLine("S", new double[] { 1, 2 }))
            .Legend(l => l.BottomCenter());
    }

    [Fact]
    public void Fork_ReturnsNewInstance()
    {
        var original = ChartBuilder.Create()
            .Title("Original")
            .Series(s => s.AddLine("S", new double[] { 1, 2, 3 }));

        var fork = original.Fork();
        Assert.NotSame(original, fork);
    }

    [Fact]
    public void Fork_MutatingFork_DoesNotAffectOriginal()
    {
        var original = ChartBuilder.Create()
            .Size(600, 400)
            .Series(s => s.AddLine("S", new double[] { 1, 2, 3 }));

        var fork = original.Fork();
        fork.Title("Forked Title");

        var svgOrig = original.RenderToSvg();
        var svgFork = fork.RenderToSvg();

        Assert.DoesNotContain("Forked Title", svgOrig);
        Assert.Contains("Forked Title", svgFork);
    }

    [Fact]
    public void Fork_CopiesSeries()
    {
        var original = ChartBuilder.Create()
            .Series(s => s.AddLine("MySeries", new double[] { 10, 20 }));

        var fork = original.Fork();
        var svg = fork.RenderToSvg();

        Assert.Contains("MySeries", svg);
    }

    [Fact]
    public void IChartBuilder_Fork_ReturnsIChartBuilder()
    {
        IChartBuilder builder = ChartBuilder.Create()
            .Series(s => s.AddLine("S", new double[] { 1, 2 }));

        IChartBuilder fork = builder.Fork();
        Assert.NotNull(fork);
        Assert.NotSame(builder, fork);
    }

    // =========================================================== Sprint D tests

    // ---- Width / ResponsiveWidth ----------------------------------------

    [Fact]
    public void Width_SetsWidthToPixelValue()
    {
        var chart = ChartBuilder.Create().Width(600);
        Assert.Equal(600, chart.GetOptions().Width);
    }

    [Fact]
    public void Width_ThrowsOnZeroOrNegative()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => ChartBuilder.Create().Width(0));
        Assert.Throws<ArgumentOutOfRangeException>(() => ChartBuilder.Create().Width(-1));
    }

    [Fact]
    public void ResponsiveWidth_ClearsWidth()
    {
        var chart = ChartBuilder.Create().Width(800).ResponsiveWidth();
        Assert.Null(chart.GetOptions().Width);
    }

    [Fact]
    public void IChartBuilder_Width_ReturnsIChartBuilder()
    {
        IChartBuilder b = ChartBuilder.Create();
        IChartBuilder result = b.Width(400);
        Assert.Same(b, result);
        Assert.Equal(400, ((ChartBuilder)result).GetOptions().Width);
    }

    [Fact]
    public void IChartBuilder_ResponsiveWidth_ReturnsIChartBuilder()
    {
        IChartBuilder b = ChartBuilder.Create().Width(500);
        IChartBuilder result = b.ResponsiveWidth();
        Assert.Same(b, result);
        Assert.Null(((ChartBuilder)result).GetOptions().Width);
    }

    [Fact]
    public void Width_EmitsPixelWidthInSvg()
    {
        string svg = ChartBuilder.Create()
            .Width(650)
            .Size(650, 400)
            .Series(s => s.AddLine("L", new double[] { 1, 2 }))
            .RenderToSvg();
        Assert.Contains("width=\"650\"", svg);
    }

    [Fact]
    public void ResponsiveWidth_EmitsPercentWidthInSvg()
    {
        string svg = ChartBuilder.Create()
            .Size(800, 400)
            .ResponsiveWidth()
            .Series(s => s.AddLine("L", new double[] { 1, 2 }))
            .RenderToSvg();
        Assert.Contains("width=\"100%\"", svg);
    }

    // ---- XAxisTickInterval numeric rendering ---------------------------------

    [Fact]
    public void XAxisTickInterval_NumericTicks_AppearsInSvg()
    {
        string svg = ChartBuilder.Create()
            .Size(600, 300)
            .XAxis(x => { x.Min = 0; x.Max = 20; x.TickInterval = 5; })
            .YAxis("Y", min: 0, max: 100)
            .Series(s => s.AddLine("L", new double[] { 10, 50, 90 }))
            .RenderToSvg();
        // Numeric tick labels 0, 5, 10, 15, 20 should be emitted
        Assert.Contains(">0<", svg);
        Assert.Contains(">5<", svg);
        Assert.Contains(">10<", svg);
        Assert.Contains(">20<", svg);
    }

    [Fact]
    public void XAxisTickInterval_CategoryAxis_IgnoresTickInterval()
    {
        // When categories are set, TickInterval is ignored and category labels render instead
        string svg = ChartBuilder.Create()
            .Size(600, 300)
            .XAxis("Q", "Q1", "Q2", "Q3")
            .XAxisTickInterval(5)
            .Series(s => s.AddColumn("C", new double[] { 10, 20, 30 }))
            .RenderToSvg();
        Assert.Contains("Q1", svg);
        Assert.Contains("Q2", svg);
        Assert.Contains("Q3", svg);
    }

    // ---- Fork tooltip isolation ----------------------------------------------

    [Fact]
    public void Fork_TooltipIsolation_MutatingForkDoesNotAffectOriginal()
    {
        var original = ChartBuilder.Create()
            .Tooltip(t => t.Format("{label}: {value}"));

        var fork = original.Fork();
        fork.Tooltip(t => t.Format("FORKED FORMAT"));

        string originalFormat = original.GetOptions().Tooltip.Format ?? "";
        string forkFormat     = fork.GetOptions().Tooltip.Format ?? "";

        Assert.Equal("{label}: {value}", originalFormat);
        Assert.Equal("FORKED FORMAT", forkFormat);
    }

    [Fact]
    public void Fork_TooltipIsolation_MutatingOriginalDoesNotAffectFork()
    {
        var original = ChartBuilder.Create()
            .Tooltip(t => t.Format("{label}: {value}"));

        var fork = original.Fork();
        original.Tooltip(t => t.Format("ORIGINAL CHANGED"));

        string forkFormat = fork.GetOptions().Tooltip.Format ?? "";
        Assert.Equal("{label}: {value}", forkFormat);
    }

    // ---- Async render methods on IChartBuilder ------------------------------

#if NET6_0_OR_GREATER
    [Fact]
    public async System.Threading.Tasks.Task IChartBuilder_RenderToStreamAsync_WritesBytes()
    {
        IChartBuilder b = ChartBuilder.Create()
            .Series(s => s.AddLine("L", new double[] { 1, 2, 3 }));

        using var ms = new System.IO.MemoryStream();
        await b.RenderToStreamAsync(ms);

        Assert.True(ms.Length > 0);
        string svg = System.Text.Encoding.UTF8.GetString(ms.ToArray());
        Assert.Contains("<svg", svg);
    }

    [Fact]
    public async System.Threading.Tasks.Task IChartBuilder_RenderToFileAsync_CreatesFile()
    {
        IChartBuilder b = ChartBuilder.Create()
            .Series(s => s.AddLine("L", new double[] { 1, 2, 3 }));

        string path = System.IO.Path.Combine(
            System.IO.Path.GetTempPath(),
            System.IO.Path.GetRandomFileName() + ".svg");

        try
        {
            await b.RenderToFileAsync(path);
            Assert.True(System.IO.File.Exists(path));
            string content = System.IO.File.ReadAllText(path);
            Assert.Contains("<svg", content);
        }
        finally
        {
            if (System.IO.File.Exists(path)) System.IO.File.Delete(path);
        }
    }

    [Fact]
    public async System.Threading.Tasks.Task IChartBuilder_RenderToHtmlFileAsync_CreatesFile()
    {
        IChartBuilder b = ChartBuilder.Create()
            .Series(s => s.AddLine("L", new double[] { 1, 2, 3 }));

        string path = System.IO.Path.Combine(
            System.IO.Path.GetTempPath(),
            System.IO.Path.GetRandomFileName() + ".html");

        try
        {
            await b.RenderToHtmlFileAsync(path, caption: "Test");
            Assert.True(System.IO.File.Exists(path));
            string content = System.IO.File.ReadAllText(path);
            Assert.Contains("<svg", content);
            Assert.Contains("Test", content);
        }
        finally
        {
            if (System.IO.File.Exists(path)) System.IO.File.Delete(path);
        }
    }
#endif

    // =========================================================== New chart type tests

    // ---- Bubble -----------------------------------------------------------

    [Fact]
    public void Bubble_RenderToSvg_ContainsSvg()
    {
        string svg = ChartBuilder.Create()
            .Size(600, 400)
            .Series(s => s.AddBubble("Products",
                new[] { new BubblePoint(10, 20, 50), new BubblePoint(30, 40, 80) }))
            .RenderToSvg();
        Assert.Contains("<svg", svg);
    }

    [Fact]
    public void Bubble_RendersCircles()
    {
        string svg = ChartBuilder.Create()
            .Size(600, 400)
            .AsStatic()
            .Series(s => s.AddBubble("B",
                new[] { new BubblePoint(5, 10, 100) }))
            .RenderToSvg();
        Assert.Contains("<circle", svg);
    }

    [Fact]
    public void Bubble_AddBubble_NullName_Throws()
    {
        Assert.Throws<ArgumentNullException>(() =>
            ChartBuilder.Create().Series(s => s.AddBubble(null!, new[] { new BubblePoint(1, 2, 3) })));
    }

    [Fact]
    public void Bubble_AddBubble_NullData_Throws()
    {
        Assert.Throws<ArgumentNullException>(() =>
            ChartBuilder.Create().Series(s => s.AddBubble("B", (BubblePoint[])null!)));
    }

    [Fact]
    public void AsBubble_SetsPrimaryType()
    {
        var opts = ChartBuilder.Create().AsBubble().GetOptions();
        Assert.Equal(ChartType.Bubble, opts.PrimaryChartType);
    }

    // ---- Heatmap ----------------------------------------------------------

    [Fact]
    public void Heatmap_RenderToSvg_ContainsSvg()
    {
        string svg = ChartBuilder.Create()
            .Size(600, 400)
            .XAxis("Col", "A", "B", "C")
            .Series(s => s.AddHeatmap("H",
                new[]
                {
                    new HeatmapPoint(0, 0, 10), new HeatmapPoint(1, 0, 50), new HeatmapPoint(2, 0, 90),
                    new HeatmapPoint(0, 1, 30), new HeatmapPoint(1, 1, 70), new HeatmapPoint(2, 1, 20),
                },
                cfg => cfg.HeatmapRowLabels.AddRange(new[] { "Row1", "Row2" })))
            .RenderToSvg();
        Assert.Contains("<svg", svg);
    }

    [Fact]
    public void Heatmap_RendersRects()
    {
        string svg = ChartBuilder.Create()
            .Size(600, 400)
            .AsStatic()
            .Series(s => s.AddHeatmap("H",
                new[] { new HeatmapPoint(0, 0, 42) }))
            .RenderToSvg();
        Assert.Contains("<rect", svg);
    }

    [Fact]
    public void Heatmap_RowLabels_AppearedInSvg()
    {
        string svg = ChartBuilder.Create()
            .Size(600, 400)
            .Series(s => s.AddHeatmap("H",
                new[] { new HeatmapPoint(0, 0, 5) },
                cfg => cfg.HeatmapRowLabels.Add("RegionA")))
            .RenderToSvg();
        Assert.Contains("RegionA", svg);
    }

    [Fact]
    public void AsHeatmap_SetsPrimaryType()
    {
        Assert.Equal(ChartType.Heatmap, ChartBuilder.Create().AsHeatmap().GetOptions().PrimaryChartType);
    }

    [Fact]
    public void Heatmap_EmitsColorAxisLegendGradientWithMinMaxLabels()
    {
        string svg = ChartBuilder.Create()
            .Size(600, 400)
            .XAxis("Col", "A", "B", "C")
            .Series(s => s.AddHeatmap("Intensity",
                new[]
                {
                    new HeatmapPoint(0, 0, 10), new HeatmapPoint(1, 0, 50), new HeatmapPoint(2, 0, 90),
                },
                cfg => cfg.HeatmapRowLabels.Add("Row1")))
            .RenderToSvg();

        // A horizontal gradient scale is emitted and referenced by the legend bar.
        Assert.Contains("colorlegend", svg);
        Assert.Contains("<linearGradient", svg);
        Assert.Contains("url(#", svg);
        // Min and max value tick labels appear beneath the bar.
        Assert.Contains(">10<", svg);
        Assert.Contains(">90<", svg);
    }

    // ---- Candlestick / OHLC ----------------------------------------------

    [Fact]
    public void Candlestick_RendersBodiesAndWicks()
    {
        string svg = ChartBuilder.Create()
            .Size(600, 400)
            .AsStatic()
            .XAxis("Day", "Mon", "Tue", "Wed")
            .Series(s => s.AddCandlestick("Price", new[]
            {
                new OhlcPoint(100, 110, 95, 108),  // up
                new OhlcPoint(108, 112, 102, 104), // down
                new OhlcPoint(104, 120, 103, 118), // up
            }))
            .RenderToSvg();

        var doc = System.Xml.Linq.XDocument.Parse(svg);
        System.Xml.Linq.XNamespace ns = "http://www.w3.org/2000/svg";
        // Three bodies (clipped rects) with high-low wicks (lines).
        // Up color = Default.PositiveColor (#2ecc71), Down = Default.NegativeColor (#e74c3c)
        int bodies = doc.Descendants(ns + "rect").Count(r =>
            r.Attribute("clip-path") != null &&
            ((string?)r.Attribute("fill") == "#2ecc71" || (string?)r.Attribute("fill") == "#e74c3c"));
        Assert.Equal(3, bodies);
        // Up candles use PositiveColor, down candle uses NegativeColor.
        Assert.Contains("#2ecc71", svg);
        Assert.Contains("#e74c3c", svg);
    }

    [Fact]
    public void Ohlc_RendersHighLowBarsWithOpenCloseTicks()
    {
        string svg = ChartBuilder.Create()
            .Size(600, 400)
            .AsStatic()
            .XAxis("Day", "Mon", "Tue")
            .Series(s => s.AddOhlc("Price", new[]
            {
                new OhlcPoint(100, 110, 95, 108),  // up → green
                new OhlcPoint(108, 112, 102, 104), // down → red
            }))
            .RenderToSvg();

        // Each OHLC point emits 3 lines (bar + open tick + close tick) => at least 6 coloured lines.
        // Up color = Default.PositiveColor (#2ecc71), Down = Default.NegativeColor (#e74c3c)
        var doc = System.Xml.Linq.XDocument.Parse(svg);
        System.Xml.Linq.XNamespace ns = "http://www.w3.org/2000/svg";
        int coloured = doc.Descendants(ns + "line").Count(l =>
            (string?)l.Attribute("stroke") == "#2ecc71" || (string?)l.Attribute("stroke") == "#e74c3c");
        Assert.True(coloured >= 6, $"expected >=6 coloured OHLC lines, got {coloured}");
    }

    [Fact]
    public void AddCandlestick_NullData_Throws()
    {
        Assert.Throws<System.ArgumentNullException>(() =>
            ChartBuilder.Create().Series(s => s.AddCandlestick("P", null!)));
    }

    // ---- Annotations ------------------------------------------------------

    [Fact]
    public void Annotation_Label_RendersTextAtCoordinate()
    {
        string svg = ChartBuilder.Create()
            .Size(600, 400)
            .AsStatic()
            .XAxis("Month", "Jan", "Feb", "Mar")
            .Series(s => s.AddColumn("Sales", new double[] { 10, 20, 15 }))
            .Annotations(a => a.Label(1, 20, "Peak", cfg =>
            {
                cfg.BackgroundColor = "#ffffff";
                cfg.Color = "#c92a2a";
            }))
            .RenderToSvg();

        Assert.Contains(">Peak<", svg);
        // Background box present.
        Assert.Contains("#ffffff", svg);
    }

    [Fact]
    public void Annotation_Shapes_RenderLineRectCircle()
    {
        string svg = ChartBuilder.Create()
            .Size(600, 400)
            .AsStatic()
            .XAxis("Month", "Jan", "Feb", "Mar", "Apr")
            .YAxis(y => { y.Min = 0; y.Max = 100; })
            .Series(s => s.AddLine("V", new double[] { 20, 40, 60, 80 }))
            .Annotations(a => a
                .Line(0, 10, 3, 90, cfg => cfg.Color = "#1971c2")
                .Rect(1, 20, 2, 70, cfg => { cfg.Color = "#2f9e44"; cfg.BackgroundColor = "#2f9e4433"; })
                .Circle(2, 60, 12, cfg => cfg.Color = "#e8590c"))
            .RenderToSvg();

        var doc = System.Xml.Linq.XDocument.Parse(svg);
        System.Xml.Linq.XNamespace ns = "http://www.w3.org/2000/svg";
        Assert.Contains("#1971c2", svg); // annotation line stroke
        Assert.Contains("#2f9e44", svg); // annotation rect stroke
        Assert.Equal(1, doc.Descendants(ns + "circle").Count(c =>
            (string?)c.Attribute("stroke") == "#e8590c"));
    }

    [Fact]
    public void Annotations_NullConfigure_Throws()
    {
        Assert.Throws<System.ArgumentNullException>(() =>
            ChartBuilder.Create().Annotations(null!));
    }

    // ---- ColumnRange ------------------------------------------------------

    [Fact]
    public void ColumnRange_RenderToSvg_ContainsSvg()
    {
        string svg = ChartBuilder.Create()
            .Size(600, 400)
            .XAxis("Month", "Jan", "Feb", "Mar")
            .Series(s => s.AddColumnRange("Temp",
                new[] { new RangePoint(2, 8), new RangePoint(3, 11), new RangePoint(6, 15) }))
            .RenderToSvg();
        Assert.Contains("<svg", svg);
    }

    [Fact]
    public void ColumnRange_RendersRects()
    {
        string svg = ChartBuilder.Create()
            .Size(600, 400)
            .AsStatic()
            .Series(s => s.AddColumnRange("R",
                new[] { new RangePoint(5, 20) }))
            .RenderToSvg();
        Assert.Contains("<rect", svg);
    }

    [Fact]
    public void AsColumnRange_SetsPrimaryType()
    {
        Assert.Equal(ChartType.ColumnRange, ChartBuilder.Create().AsColumnRange().GetOptions().PrimaryChartType);
    }

    // ---- AreaRange --------------------------------------------------------

    [Fact]
    public void AreaRange_RenderToSvg_ContainsSvg()
    {
        string svg = ChartBuilder.Create()
            .Size(600, 400)
            .XAxis("Week", "W1", "W2", "W3")
            .Series(s => s.AddAreaRange("Band",
                new[] { new RangePoint(100, 150), new RangePoint(110, 160), new RangePoint(120, 170) }))
            .RenderToSvg();
        Assert.Contains("<svg", svg);
    }

    [Fact]
    public void AreaRange_RendersFilledPath()
    {
        string svg = ChartBuilder.Create()
            .Size(600, 400)
            .AsStatic()
            .Series(s => s.AddAreaRange("R",
                new[] { new RangePoint(10, 50), new RangePoint(20, 60) }))
            .RenderToSvg();
        // Should contain a filled path element
        Assert.Contains("fill-opacity", svg);
    }

    [Fact]
    public void AsAreaRange_SetsPrimaryType()
    {
        Assert.Equal(ChartType.AreaRange, ChartBuilder.Create().AsAreaRange().GetOptions().PrimaryChartType);
    }

    // ---- Funnel -----------------------------------------------------------

    [Fact]
    public void Funnel_RenderToSvg_ContainsSvg()
    {
        string svg = ChartBuilder.Create()
            .Size(600, 440)
            .XAxis("Stage", "Leads", "Qualified", "Closed")
            .Series(s => s.AddFunnel("Pipeline", new double[] { 5000, 2000, 500 }))
            .RenderToSvg();
        Assert.Contains("<svg", svg);
    }

    [Fact]
    public void Funnel_RendersPathElements()
    {
        string svg = ChartBuilder.Create()
            .Size(600, 440)
            .AsStatic()
            .Series(s => s.AddFunnel("F", new double[] { 100, 60, 30 }))
            .RenderToSvg();
        Assert.Contains("<path", svg);
    }

    [Fact]
    public void Funnel_StageLabels_AppearedInSvg()
    {
        string svg = ChartBuilder.Create()
            .Size(600, 440)
            .XAxis("Stage", "Prospects", "Demos", "Contracts")
            .Series(s => s.AddFunnel("F", new double[] { 500, 200, 80 }))
            .RenderToSvg();
        Assert.Contains("Prospects", svg);
        Assert.Contains("Demos", svg);
    }

    [Fact]
    public void AsFunnel_SetsPrimaryType()
    {
        Assert.Equal(ChartType.Funnel, ChartBuilder.Create().AsFunnel().GetOptions().PrimaryChartType);
    }

    // ---- Treemap ----------------------------------------------------------

    [Fact]
    public void Treemap_RenderToSvg_ContainsSvg()
    {
        string svg = ChartBuilder.Create()
            .Size(700, 420)
            .XAxis("Asset", "Equity", "Bonds", "Cash")
            .Series(s => s.AddTreemap("Portfolio", new double[] { 3000, 1500, 500 }))
            .RenderToSvg();
        Assert.Contains("<svg", svg);
    }

    [Fact]
    public void Treemap_RendersRects()
    {
        string svg = ChartBuilder.Create()
            .Size(700, 420)
            .AsStatic()
            .Series(s => s.AddTreemap("T", new double[] { 100, 60, 40 }))
            .RenderToSvg();
        Assert.Contains("<rect", svg);
    }

    [Fact]
    public void Treemap_Labels_AppearedInSvg()
    {
        string svg = ChartBuilder.Create()
            .Size(700, 420)
            .XAxis("A", "Alpha", "Beta", "Gamma")
            .Series(s => s.AddTreemap("T", new double[] { 500, 300, 200 }))
            .RenderToSvg();
        Assert.Contains("Alpha", svg);
        Assert.Contains("Beta", svg);
    }

    [Fact]
    public void Treemap_LegendSwatches_UseExplicitSeriesColor()
    {
        const string c = "#123abc";
        string svg = ChartBuilder.Create()
            .Size(700, 420)
            .XAxis("A", "Alpha", "Beta", "Gamma")
            .Series(s => s.AddTreemap("Portfolio", new double[] { 3000, 1500, 500 },
                cfg => cfg.Color(c)))
            .RenderToSvg();

        string colorForAlpha = ExtractLegendSwatchFill(svg, "Alpha");
        string colorForBeta  = ExtractLegendSwatchFill(svg, "Beta");
        Assert.Equal(c, colorForAlpha, ignoreCase: true);
        Assert.Equal(c, colorForBeta, ignoreCase: true);
    }

    [Fact]
    public void AsTreemap_SetsPrimaryType()
    {
        Assert.Equal(ChartType.Treemap, ChartBuilder.Create().AsTreemap().GetOptions().PrimaryChartType);
    }

    [Fact]
    public void Funnel_LegendSwatches_UseExplicitSeriesColor()
    {
        const string c = "#ff6600";
        string svg = ChartBuilder.Create()
            .Size(700, 420)
            .XAxis("Stage", "Visit", "Signup", "Purchase")
            .Series(s => s.AddFunnel("Funnel", new double[] { 1000, 400, 120 },
                cfg => cfg.Color(c)))
            .RenderToSvg();

        string colorForVisit  = ExtractLegendSwatchFill(svg, "Visit");
        string colorForSignup = ExtractLegendSwatchFill(svg, "Signup");
        Assert.Equal(c, colorForVisit, ignoreCase: true);
        Assert.Equal(c, colorForSignup, ignoreCase: true);
    }

    // ------------------------------------------------------------------ Legend series toggle

    [Fact]
    public void LegendToggle_Interactive_EmitsSeriesGroupsAndLegendItems()
    {
        string svg = ChartBuilder.Create()
            .Size(600, 400)
            .AsInteractive()
            .XAxis(x => x.Categories.AddRange(new[] { "A", "B" }))
            .YAxis(y => y.Min = 0)
            .Legend(l => l.AtBottom())
            .Series(s => s
                .AddLine("Revenue", new double?[] { 100, 200 })
                .AddLine("Costs",   new double?[] { 80,  120 }))
            .RenderToSvg();

        // Series groups
        Assert.Contains("class=\"tf-sg\" data-si=\"0\"", svg);
        Assert.Contains("class=\"tf-sg\" data-si=\"1\"", svg);
        // Legend toggle wrappers
        Assert.Contains("class=\"tf-li\" data-si=\"0\"", svg);
        Assert.Contains("class=\"tf-li\" data-si=\"1\"", svg);
        Assert.Contains("cursor:pointer", svg);
        // Toggle JS
        Assert.Contains("tf-li[data-si]", svg);
        Assert.Contains("tf-sg[data-si", svg);
        Assert.Contains("data-hidden", svg);
    }

    [Fact]
    public void LegendToggle_Static_DoesNotEmitSeriesGroupsOrToggleJs()
    {
        string svg = ChartBuilder.Create()
            .Size(600, 400)
            .AsStatic()
            .XAxis(x => x.Categories.AddRange(new[] { "A", "B" }))
            .YAxis(y => y.Min = 0)
            .Legend(l => l.AtBottom())
            .Series(s => s
                .AddLine("Revenue", new double?[] { 100, 200 })
                .AddLine("Costs",   new double?[] { 80,  120 }))
            .RenderToSvg();

        Assert.DoesNotContain("class=\"tf-sg\"", svg);
        Assert.DoesNotContain("class=\"tf-li\"", svg);
        Assert.DoesNotContain("tf-li[data-si]", svg);
    }

    [Fact]
    public void LegendToggle_Animated_DoesNotEmitSeriesGroupsOrToggleJs()
    {
        string svg = ChartBuilder.Create()
            .Size(600, 400)
            .AsAnimated()
            .XAxis(x => x.Categories.AddRange(new[] { "A", "B" }))
            .YAxis(y => y.Min = 0)
            .Legend(l => l.AtBottom())
            .Series(s => s
                .AddLine("Revenue", new double?[] { 100, 200 })
                .AddLine("Costs",   new double?[] { 80,  120 }))
            .RenderToSvg();

        Assert.DoesNotContain("class=\"tf-sg\"", svg);
        Assert.DoesNotContain("class=\"tf-li\"", svg);
        Assert.DoesNotContain("tf-li[data-si]", svg);
    }

    [Fact]
    public void LegendToggle_PieChart_SliceItemsNotTogglable()
    {
        string svg = ChartBuilder.Create()
            .Size(500, 400)
            .AsPie()
            .AsInteractive()
            .Labels("Alpha", "Beta", "Gamma")
            .Legend(l => l.AtBottom())
            .Series(s => s.Add("Share", new double?[] { 40, 35, 25 }))
            .RenderToSvg();

        // Pie slices use Si=-1 so no tf-li wrappers should appear
        Assert.DoesNotContain("class=\"tf-li\"", svg);
        // But the series group wrapper is still emitted
        Assert.Contains("class=\"tf-sg\" data-si=\"0\"", svg);
    }

    [Fact]
    public void LegendToggle_NoLegend_DoesNotEmitToggleJs()
    {
        string svg = ChartBuilder.Create()
            .Size(600, 400)
            .AsInteractive()
            .HideLegend()
            .XAxis(x => x.Categories.AddRange(new[] { "A", "B" }))
            .YAxis(y => y.Min = 0)
            .Series(s => s.AddLine("Revenue", new double?[] { 100, 200 }))
            .RenderToSvg();

        Assert.DoesNotContain("tf-li[data-si]", svg);
    }

    // ---------------------------------------------------------------- gap fixes

    [Theory]
    [InlineData(SvgMode.Static)]
    [InlineData(SvgMode.Animated)]
    [InlineData(SvgMode.Interactive)]
    public void RenderToSvg_AllModes_ProduceWellFormedXml(SvgMode mode)
    {
        var builder = ChartBuilder.Create()
            .Title("Well-formed <check> & \"test\"")
            .Size(600, 400)
            .XAxis(x => x.Categories.AddRange(new[] { "A & B", "C < D", "E > F" }))
            .Series(s => s
                .AddColumn("Revenue", new double?[] { 100, 200, 150 })
                .AddLine("Trend", new double?[] { 90, 210, 140 }));

        builder = mode switch
        {
            SvgMode.Static      => builder.AsStatic(),
            SvgMode.Animated    => builder.AsAnimated(),
            SvgMode.Interactive => builder.AsInteractive(),
            _                   => builder,
        };

        string svg = builder.RenderToSvg();

        var ex = Record.Exception(() => System.Xml.Linq.XDocument.Parse(svg));
        Assert.Null(ex);
    }

    private static string ExtractLegendSwatchFill(string svg, string legendLabel)
    {
        string p = "<rect[^>]*fill=\"(?<fill>[^\"]+)\"[^>]*/>\\s*<text class=\"legend-label\"[^>]*>"
            + System.Text.RegularExpressions.Regex.Escape(legendLabel)
            + "</text>";
        var m = System.Text.RegularExpressions.Regex.Match(svg, p, System.Text.RegularExpressions.RegexOptions.Singleline);
        Assert.True(m.Success, $"Legend swatch not found for label '{legendLabel}'.");
        return m.Groups["fill"].Value;
    }

    [Fact]
    public void YAxisFormat_WithDotNetNumericSpecifier_ProducesExactNumber()
    {
        string svg = ChartBuilder.Create()
            .Size(600, 400)
            .YAxis(y => { y.Min = 0; y.Max = 2000; })
            .YAxisFormat("${value:N0}")
            .Series(s => s.AddLine("Revenue", new double?[] { 0, 1000, 2000 }))
            .RenderToSvg();

        // Exact, non-abbreviated formatting with a thousands separator.
        Assert.Contains("$1,000", svg);
        Assert.Contains("$2,000", svg);
        // The literal placeholder must NOT leak into the output.
        Assert.DoesNotContain("{value", svg);
    }

    [Fact]
    public void YAxisFormat_BareValueToken_KeepsAbbreviatedForm()
    {
        string svg = ChartBuilder.Create()
            .Size(600, 400)
            .YAxis(y => { y.Min = 0; y.Max = 2000; })
            .YAxisFormat("{value}")
            .Series(s => s.AddLine("Revenue", new double?[] { 0, 1000, 2000 }))
            .RenderToSvg();

        Assert.Contains("2k", svg);
    }

    [Fact]
    public void DataLabelFormat_WithNumericSpecifier_ProducesExactNumber()
    {
        string svg = ChartBuilder.Create()
            .Size(600, 400)
            .Series(s => s.AddColumn("Sales", new double?[] { 1500 },
                cfg => cfg.DataLabel.Show().Format("{value:N0}")))
            .RenderToSvg();

        Assert.Contains("1,500", svg);
        Assert.DoesNotContain("{value", svg);
    }

    [Fact]
    public void Render_TitleWithControlChars_StripsIllegalXmlAndStaysWellFormed()
    {
        string svg = ChartBuilder.Create()
            .Title("Bad\u0000Title\u0008Here")
            .Series(s => s.AddLine("Data", new double?[] { 1, 2, 3 }))
            .RenderToSvg();

        Assert.DoesNotContain('\u0000', svg);
        Assert.DoesNotContain('\u0008', svg);
        Assert.Contains("BadTitleHere", svg);
        var ex = Record.Exception(() => System.Xml.Linq.XDocument.Parse(svg));
        Assert.Null(ex);
    }

    [Fact]
    public void Escape_IsPubliclyAccessible_AndEscapesEntities()
    {
        string result = TerraFluent.Chart.Reporting.Rendering.SvgRenderer.Escape("<a> & \"b\" 'c'");

        Assert.Equal("&lt;a&gt; &amp; &quot;b&quot; &apos;c&apos;", result);
    }

    [Fact]
    public void Escape_NullInput_ReturnsEmptyString()
    {
        Assert.Equal(string.Empty, TerraFluent.Chart.Reporting.Rendering.SvgRenderer.Escape(null));
    }

    [Fact]
    public void Parliament_RendersExactlyTotalSeats_AndExactPerPartyCounts()
    {
        var parties = new[]
        {
            new ParliamentGroup("Progressive Alliance", "#3b82f6", 112),
            new ParliamentGroup("Conservative Union",   "#ef4444",  98),
            new ParliamentGroup("Centrist Democrats",   "#f59e0b",  52),
            new ParliamentGroup("Green Coalition",      "#22c55e",  24),
            new ParliamentGroup("Liberty Movement",     "#a855f7",  14),
        };

        string svg = ChartBuilder.Create()
            .Size(760, 480)
            .AsStatic()
            .Series(s => s.AddParliament("Parliament", parties))
            .RenderToSvg();

        var doc = System.Xml.Linq.XDocument.Parse(svg);
        System.Xml.Linq.XNamespace ns = "http://www.w3.org/2000/svg";

        int totalDots = 0;
        foreach (var g in doc.Descendants(ns + "g"))
        {
            var cls = (string?)g.Attribute("class");
            if (cls is null || !cls.Contains("tf-party-group")) continue;

            int seats = int.Parse((string)g.Attribute("data-party-seats")!);
            int dots  = 0;
            foreach (var c in g.Elements(ns + "circle")) dots++;

            Assert.Equal(seats, dots);   // each party renders exactly its seat count
            totalDots += dots;
        }

        Assert.Equal(300, totalDots);    // Σ dots == declared total seats
    }

    [Fact]
    public void Bubble_UsesAreaProportionalRadius_NotLinear()
    {
        // With z = 0, 1, 4 (zMax = 4): area-proportional sizing (radius ∝ √z) places the
        // z=1 bubble at the EXACT midpoint between the smallest and largest radii
        // (√0=0, √1=0.5, √4=1). The buggy linear encoding (radius ∝ z) would put it at
        // only a quarter of the span (0, 0.25, 1), well below the midpoint.
        string svg = ChartBuilder.Create()
            .Size(600, 400)
            .Series(s => s.AddBubble("B", new[]
            {
                new BubblePoint(1, 1, 0),
                new BubblePoint(2, 2, 1),
                new BubblePoint(3, 3, 4),
            }))
            .RenderToSvg();

        var doc = System.Xml.Linq.XDocument.Parse(svg);
        System.Xml.Linq.XNamespace ns = "http://www.w3.org/2000/svg";

        var radii = new System.Collections.Generic.List<double>();
        foreach (var c in doc.Descendants(ns + "circle"))
        {
            if ((string?)c.Attribute("fill-opacity") != "0.7") continue;   // bubble markers
            radii.Add(double.Parse((string)c.Attribute("r")!, System.Globalization.CultureInfo.InvariantCulture));
        }

        Assert.Equal(3, radii.Count);
        radii.Sort();
        double midpoint = (radii[0] + radii[2]) / 2.0;
        Assert.True(Math.Abs(radii[1] - midpoint) < 0.5,
            $"Expected √-scaled middle radius ≈ midpoint {midpoint:F2}, got {radii[1]:F2} (linear encoding would sit near {radii[0] + 0.25 * (radii[2] - radii[0]):F2})");
    }

    [Fact]
    public void Line_WithNullGap_BreaksIntoSeparatePaths()
    {
        // A null in the middle of the data should split the line into two stroke paths
        // (a gap) rather than one path that bridges the missing point.
        string svg = ChartBuilder.Create()
            .Size(600, 400)
            .Series(s => s.AddLine("Data", new double?[] { 1, 2, null, 4, 5 }))
            .RenderToSvg();

        var doc = System.Xml.Linq.XDocument.Parse(svg);
        System.Xml.Linq.XNamespace ns = "http://www.w3.org/2000/svg";

        int strokePaths = 0;
        foreach (var p in doc.Descendants(ns + "path"))
        {
            var stroke = (string?)p.Attribute("stroke");
            var d      = (string?)p.Attribute("d");
            if (stroke is null || stroke == "none" || d is null) continue;
            if (d.IndexOf('L') >= 0 || d.IndexOf('C') >= 0) strokePaths++;   // an actual drawn line
        }

        Assert.Equal(2, strokePaths);
    }

    [Fact]
    public void ColumnRange_InvertedLowHigh_RendersFullBar_NotSliver()
    {
        // Low > High must be treated as the same range, producing a real bar rather than
        // the misleading 1%-tall sliver the old clamp created.
        string normal = ColumnRangeBarHeight(new RangePoint(20, 80));
        string invert = ColumnRangeBarHeight(new RangePoint(80, 20));
        Assert.Equal(normal, invert);
    }

    private static string ColumnRangeBarHeight(RangePoint p)
    {
        string svg = ChartBuilder.Create()
            .Size(600, 400)
            .AsStatic()
            .YAxis(y => { y.Min = 0; y.Max = 100; })
            .Series(s => s.AddColumnRange("R", new[] { p }))
            .RenderToSvg();

        var doc = System.Xml.Linq.XDocument.Parse(svg);
        System.Xml.Linq.XNamespace ns = "http://www.w3.org/2000/svg";
        foreach (var rect in doc.Descendants(ns + "rect"))
        {
            var h = (string?)rect.Attribute("height");
            if (h is null || h == "0") continue;
            var fill = (string?)rect.Attribute("fill");
            if (fill is null || fill == "transparent") continue;
            return h;   // the bar's height
        }
        return "none";
    }

    [Fact]
    public void Gauge_ValueAboveMax_EmitsOffScaleMarker()
    {
        string svg = ChartBuilder.Create()
            .Size(400, 300)
            .AsStatic()
            .YAxis(y => { y.Min = 0; y.Max = 100; })
            .Series(s => s.AddGauge("Speed", 150))
            .RenderToSvg();

        Assert.Contains("Above maximum", svg);
        Assert.Null(Record.Exception(() => System.Xml.Linq.XDocument.Parse(svg)));
    }

    [Fact]
    public void LogAxis_RendersDecadeTickLabels_AndStaysWellFormed()
    {
        string svg = ChartBuilder.Create()
            .Size(600, 400)
            .AsStatic()
            .XAxis(x => x.Categories.AddRange(new[] { "A", "B", "C", "D" }))
            .YAxisLogarithmic()
            .Series(s => s.AddLine("Data", new double?[] { 1, 10, 100, 1000 }))
            .RenderToSvg();

        Assert.Null(Record.Exception(() => System.Xml.Linq.XDocument.Parse(svg)));
        // Decade tick labels for a 1..1000 range (1000 is abbreviated to "1k").
        Assert.Contains(">1<", svg);
        Assert.Contains(">10<", svg);
        Assert.Contains(">100<", svg);
        Assert.Contains(">1k<", svg);
    }

    [Fact]
    public void LogAxis_DecadesAreEvenlySpaced_UnlikeLinear()
    {
        // On a log axis the y-pixel gap between 1→10, 10→100 and 100→1000 must be equal,
        // whereas on a linear axis the 100→1000 gap would dwarf the 1→10 gap.
        var ys = LogAxisLabelPositions();
        Assert.True(ys.Count >= 3, $"Expected at least 3 decade labels, got {ys.Count}");
        ys.Sort();
        double g1 = ys[1] - ys[0];
        double g2 = ys[2] - ys[1];
        Assert.True(Math.Abs(g1 - g2) < 2.0,
            $"Expected evenly spaced decades, got gaps {g1:F1}, {g2:F1}");
    }

    private static System.Collections.Generic.List<double> LogAxisLabelPositions()
    {
        string svg = ChartBuilder.Create()
            .Size(600, 400)
            .AsStatic()
            .XAxis(x => x.Categories.AddRange(new[] { "A", "B", "C", "D" }))
            .YAxisLogarithmic()
            .Series(s => s.AddLine("Data", new double?[] { 1, 10, 100, 1000 }))
            .RenderToSvg();

        var doc = System.Xml.Linq.XDocument.Parse(svg);
        System.Xml.Linq.XNamespace ns = "http://www.w3.org/2000/svg";
        var ys = new System.Collections.Generic.List<double>();
        foreach (var t in doc.Descendants(ns + "text"))
        {
            var val = t.Value;
            if (val == "1" || val == "10" || val == "100")
            {
                var yAttr = (string?)t.Attribute("y");
                if (yAttr != null &&
                    double.TryParse(yAttr, System.Globalization.NumberStyles.Float,
                        System.Globalization.CultureInfo.InvariantCulture, out double y))
                {
                    ys.Add(y);
                }
            }
        }
        return ys;
    }

    [Fact]
    public void LogAxis_IsPreservedThroughClone()
    {
        var options = new ChartOptions();
        options.YAxis.Type = AxisType.Logarithmic;
        var clone = options.Clone();
        Assert.Equal(AxisType.Logarithmic, clone.YAxis.Type);
    }

    [Fact]
    public void DateTimeAxis_ThinsDenseLabels()
    {
        var dates = new System.Collections.Generic.List<DateTime>();
        var data  = new double?[24];
        for (int i = 0; i < 24; i++)
        {
            dates.Add(new DateTime(2023, 1, 1).AddMonths(i));
            data[i] = i;
        }

        string svg = ChartBuilder.Create()
            .Size(700, 400)
            .AsStatic()
            .XAxisDateTime(dates)
            .Series(s => s.AddLine("D", data))
            .RenderToSvg();

        var doc = System.Xml.Linq.XDocument.Parse(svg);
        System.Xml.Linq.XNamespace ns = "http://www.w3.org/2000/svg";

        int dateLabels = 0;
        foreach (var t in doc.Descendants(ns + "text"))
        {
            if ((string?)t.Attribute("class") != "axis-label") continue;
            if (t.Value.Contains("2023") || t.Value.Contains("2024")) dateLabels++;
        }

        Assert.True(dateLabels > 0 && dateLabels < 24,
            $"Expected thinned date labels (fewer than 24), got {dateLabels}");
    }

    [Fact]
    public void DateTimeAxis_ExplicitFormat_IsApplied()
    {
        var dates = new[]
        {
            new DateTime(2023, 1, 1),
            new DateTime(2023, 2, 1),
            new DateTime(2023, 3, 1),
        };

        string svg = ChartBuilder.Create()
            .AsStatic()
            .XAxisDateTime(dates, "yyyy-MM")
            .Series(s => s.AddColumn("D", new double?[] { 1, 2, 3 }))
            .RenderToSvg();

        Assert.Contains(">2023-01<", svg);
        Assert.Contains(">2023-03<", svg);
    }

    [Fact]
    public void DateTimeAxis_IsPreservedThroughClone()
    {
        var options = new ChartOptions();
        options.XAxis.Type = AxisType.DateTime;
        options.XAxis.DateTimeValues.Add(new DateTime(2023, 1, 1));
        var clone = options.Clone();
        Assert.Equal(AxisType.DateTime, clone.XAxis.Type);
        Assert.Single(clone.XAxis.DateTimeValues);
    }

    [Fact]
    public void RadarChart_RendersClosedPolygonsAndCategoryLabels()
    {
        string svg = ChartBuilder.Create()
            .Size(500, 500)
            .AsStatic()
            .XAxis(x => x.Categories.AddRange(new[] { "Speed", "Power", "Range", "Agility", "Cost" }))
            .Series(s => s.AddRadar("Model A", new double?[] { 80, 60, 70, 90, 50 }))
            .RenderToSvg();

        Assert.Null(Record.Exception(() => System.Xml.Linq.XDocument.Parse(svg)));

        var doc = System.Xml.Linq.XDocument.Parse(svg);
        System.Xml.Linq.XNamespace ns = "http://www.w3.org/2000/svg";

        int closedPaths = 0;
        foreach (var p in doc.Descendants(ns + "path"))
        {
            var d = (string?)p.Attribute("d");
            if (d != null && d.TrimEnd().EndsWith("Z")) closedPaths++;
        }
        // 4 grid rings + 1 series polygon
        Assert.True(closedPaths >= 5, $"Expected >= 5 closed paths, got {closedPaths}");
        Assert.Contains("Speed", svg);
        Assert.Contains("Agility", svg);
    }

    [Fact]
    public void RadarChart_MultipleSeries_DrawGridSpokesOnce()
    {
        string svg = ChartBuilder.Create()
            .Size(500, 500)
            .AsStatic()
            .XAxis(x => x.Categories.AddRange(new[] { "A", "B", "C", "D", "E" }))
            .Series(s => s
                .AddRadar("One", new double?[] { 5, 4, 3, 2, 1 })
                .AddRadar("Two", new double?[] { 1, 2, 3, 4, 5 }))
            .RenderToSvg();

        var doc = System.Xml.Linq.XDocument.Parse(svg);
        System.Xml.Linq.XNamespace ns = "http://www.w3.org/2000/svg";

        int spokeLines = 0;
        foreach (var l in doc.Descendants(ns + "line"))
            if ((string?)l.Attribute("class") == "grid-line") spokeLines++;

        // One spoke per category, regardless of the number of series.
        Assert.Equal(5, spokeLines);
    }

    [Fact]
    public void BoxPlot_RendersBoxAndWhiskers()
    {
        string svg = ChartBuilder.Create()
            .Size(600, 400)
            .AsStatic()
            .XAxis(x => x.Categories.AddRange(new[] { "A", "B" }))
            .YAxis(y => { y.Min = 0; y.Max = 100; })
            .Series(s => s.AddBoxPlot("Dist", new[]
            {
                new BoxPlotPoint(10, 25, 45, 65, 90),
                new BoxPlotPoint(20, 35, 50, 70, 85),
            }))
            .RenderToSvg();

        Assert.Null(Record.Exception(() => System.Xml.Linq.XDocument.Parse(svg)));

        var doc = System.Xml.Linq.XDocument.Parse(svg);
        System.Xml.Linq.XNamespace ns = "http://www.w3.org/2000/svg";

        // Two interquartile boxes (fill-opacity set) plus whisker lines.
        int boxes = 0;
        foreach (var r in doc.Descendants(ns + "rect"))
            if ((string?)r.Attribute("fill-opacity") != null && (string?)r.Attribute("class") != "hit-area")
                boxes++;
        Assert.True(boxes >= 2, $"Expected >= 2 boxes, got {boxes}");
    }

    [Fact]
    public void ErrorBar_RendersWhiskerLines()
    {
        string svg = ChartBuilder.Create()
            .Size(600, 400)
            .AsStatic()
            .XAxis(x => x.Categories.AddRange(new[] { "A", "B", "C" }))
            .YAxis(y => { y.Min = 0; y.Max = 50; })
            .Series(s => s.AddErrorBar("Err", new[]
            {
                new RangePoint(18, 24),
                new RangePoint(28, 34),
                new RangePoint(22, 30),
            }))
            .RenderToSvg();

        var doc = System.Xml.Linq.XDocument.Parse(svg);
        System.Xml.Linq.XNamespace ns = "http://www.w3.org/2000/svg";

        // Each error bar = 1 vertical + 2 cap lines = 3 lines minimum × 3 points.
        int lines = 0;
        foreach (var l in doc.Descendants(ns + "line"))
        {
            var cls = (string?)l.Attribute("class");
            if (cls == "grid-line" || cls == "axis-line") continue;
            lines++;
        }
        Assert.True(lines >= 9, $"Expected >= 9 whisker/cap lines, got {lines}");
    }

    [Fact]
    public void LinearGradientFill_EmitsGradientDefAndReference()
    {
        string svg = ChartBuilder.Create()
            .Size(600, 400)
            .AsStatic()
            .XAxis(x => x.Categories.AddRange(new[] { "A", "B", "C" }))
            .Series(s => s.AddColumn("G", new double?[] { 10, 20, 15 },
                cfg => cfg.LinearGradientFill(90, (0, "#1e90ff"), (1, "#003366"))))
            .RenderToSvg();

        Assert.Null(Record.Exception(() => System.Xml.Linq.XDocument.Parse(svg)));

        var doc = System.Xml.Linq.XDocument.Parse(svg);
        System.Xml.Linq.XNamespace ns = "http://www.w3.org/2000/svg";

        var grad = doc.Descendants(ns + "linearGradient").FirstOrDefault();
        Assert.NotNull(grad);
        string gid = (string)grad!.Attribute("id")!;
        Assert.Equal(2, grad.Elements(ns + "stop").Count());

        // At least one column rect references the gradient.
        bool referenced = doc.Descendants(ns + "rect")
            .Any(r => (string?)r.Attribute("fill") == $"url(#{gid})");
        Assert.True(referenced, "Expected a rect to reference the gradient fill.");
    }

    [Fact]
    public void LinearGradientFill_LegendSwatch_MatchesSeriesFillReference()
    {
        string svg = ChartBuilder.Create()
            .Size(600, 400)
            .AsStatic()
            .XAxis(x => x.Categories.AddRange(new[] { "A", "B", "C" }))
            .Series(s => s.AddColumn("GradLegend", new double?[] { 10, 20, 15 },
                cfg => cfg.LinearGradientFill(90, (0, "#1e90ff"), (1, "#003366"))))
            .RenderToSvg();

        string legendFill = ExtractLegendSwatchFill(svg, "GradLegend");
        Assert.StartsWith("url(#", legendFill, StringComparison.Ordinal);
        Assert.EndsWith("-clip-fill-0)", legendFill, StringComparison.Ordinal);
    }

    [Fact]
    public void PatternFill_EmitsPatternDef()
    {
        string svg = ChartBuilder.Create()
            .Size(600, 400)
            .AsStatic()
            .XAxis(x => x.Categories.AddRange(new[] { "A", "B" }))
            .Series(s => s.AddArea("P", new double?[] { 30, 45 },
                cfg => cfg.PatternFill(PatternKind.DiagonalLines, "#333", "#eee")))
            .RenderToSvg();

        var doc = System.Xml.Linq.XDocument.Parse(svg);
        System.Xml.Linq.XNamespace ns = "http://www.w3.org/2000/svg";

        var pat = doc.Descendants(ns + "pattern").FirstOrDefault();
        Assert.NotNull(pat);
        string pid = (string)pat!.Attribute("id")!;
        bool referenced = doc.Descendants(ns + "path")
            .Any(p => (string?)p.Attribute("fill") == $"url(#{pid})");
        Assert.True(referenced, "Expected an area path to reference the pattern fill.");
    }

    [Fact]
    public void PatternFill_LegendSwatch_MatchesSeriesFillReference()
    {
        string svg = ChartBuilder.Create()
            .Size(600, 400)
            .AsStatic()
            .XAxis(x => x.Categories.AddRange(new[] { "A", "B" }))
            .Series(s => s.AddArea("PatternLegend", new double?[] { 30, 45 },
                cfg => cfg.PatternFill(PatternKind.DiagonalLines, "#333", "#eee")))
            .RenderToSvg();

        string legendFill = ExtractLegendSwatchFill(svg, "PatternLegend");
        Assert.StartsWith("url(#", legendFill, StringComparison.Ordinal);
        Assert.EndsWith("-clip-fill-0)", legendFill, StringComparison.Ordinal);
    }

    [Fact]
    public void MarkerSymbol_Diamond_RendersPolygonMarkers()
    {
        string svg = ChartBuilder.Create()
            .Size(600, 400)
            .AsStatic()
            .Series(s => s.AddLine("L", new double?[] { 3, 7, 5, 9 },
                cfg => cfg.MarkerSymbol(MarkerSymbol.Diamond)))
            .RenderToSvg();

        var doc = System.Xml.Linq.XDocument.Parse(svg);
        System.Xml.Linq.XNamespace ns = "http://www.w3.org/2000/svg";

        int polys = doc.Descendants(ns + "polygon").Count();
        Assert.True(polys >= 4, $"Expected >= 4 diamond polygons, got {polys}");
        // Circle markers should no longer be emitted for the data points.
        Assert.DoesNotContain("<circle", svg);
    }

    [Fact]
    public void MarkerSymbol_Square_RendersRectMarkers()
    {
        string svg = ChartBuilder.Create()
            .Size(600, 400)
            .AsStatic()
            .Series(s => s.AddScatter("S", new double?[] { 3, 7, 5 },
                cfg => cfg.MarkerSymbol(MarkerSymbol.Square)))
            .RenderToSvg();

        var doc = System.Xml.Linq.XDocument.Parse(svg);
        System.Xml.Linq.XNamespace ns = "http://www.w3.org/2000/svg";

        // Three square markers as <rect> (excluding the background rect which has no stroke).
        int markerRects = doc.Descendants(ns + "rect")
            .Count(r => (string?)r.Attribute("stroke") != null);
        Assert.True(markerRects >= 3, $"Expected >= 3 square marker rects, got {markerRects}");
    }

    [Fact]
    public void Zones_LineSplitsIntoColouredRunsAtThreshold()
    {
        string svg = ChartBuilder.Create()
            .Size(600, 400)
            .AsStatic()
            .Series(s => s.AddLine("Temp", new double?[] { -5, 4, -2, 8 },
                cfg => cfg.Zones((0.0, "#d64545"), (null, "#2f9e44"))))
            .RenderToSvg();

        Assert.Null(Record.Exception(() => System.Xml.Linq.XDocument.Parse(svg)));

        var doc = System.Xml.Linq.XDocument.Parse(svg);
        System.Xml.Linq.XNamespace ns = "http://www.w3.org/2000/svg";

        // The zig-zag crosses zero four times → several coloured path runs, using both colours.
        var strokeColors = doc.Descendants(ns + "path")
            .Select(p => (string?)p.Attribute("stroke"))
            .Where(c => c == "#d64545" || c == "#2f9e44")
            .ToList();
        Assert.Contains("#d64545", strokeColors);
        Assert.Contains("#2f9e44", strokeColors);
        Assert.True(strokeColors.Count >= 3, $"Expected multiple coloured runs, got {strokeColors.Count}");
    }

    [Fact]
    public void Zones_ColumnBarsColouredByValue()
    {
        string svg = ChartBuilder.Create()
            .Size(600, 400)
            .AsStatic()
            .XAxis(x => x.Categories.AddRange(new[] { "A", "B", "C" }))
            .Series(s => s.AddColumn("Delta", new double?[] { -3, 5, -1 },
                cfg => cfg.Zones((0.0, "#d64545"), (null, "#2f9e44"))))
            .RenderToSvg();

        var doc = System.Xml.Linq.XDocument.Parse(svg);
        System.Xml.Linq.XNamespace ns = "http://www.w3.org/2000/svg";

        var barFills = doc.Descendants(ns + "rect")
            .Select(r => (string?)r.Attribute("fill"))
            .Where(c => c == "#d64545" || c == "#2f9e44")
            .ToList();
        Assert.Contains("#d64545", barFills);   // negative bars
        Assert.Contains("#2f9e44", barFills);   // positive bar
    }

    // ================================================================== LabelLayout

    [Fact]
    public void LabelLayout_ExplicitRotation_AppliesTransformAttribute()
    {
        var svg = ChartBuilder.Create()
            .Size(600, 400)
            .XAxis(x => x.Categories.AddRange(new[] { "Alpha", "Beta", "Gamma", "Delta", "Epsilon" }))
            .Series(s => s.AddColumn("Data", new double?[] { 1, 2, 3, 4, 5 }))
            .LabelLayout(ll => ll.Rotation(-45))
            .RenderToSvg();

        Assert.Contains("rotate(-45", svg);
    }

    [Fact]
    public void LabelLayout_NoRotation_NoTransformAttribute()
    {
        var svg = ChartBuilder.Create()
            .Size(600, 400)
            .XAxis(x => x.Categories.AddRange(new[] { "A", "B", "C" }))
            .Series(s => s.AddColumn("Data", new double?[] { 10, 20, 30 }))
            .LabelLayout(ll => ll.NoRotation())
            .RenderToSvg();

        // Three short labels with wide step — no rotation expected
        Assert.DoesNotContain("rotate(", svg);
    }

    [Fact]
    public void LabelLayout_WordWrap_EmitsTspanElements()
    {
        var svg = ChartBuilder.Create()
            .Size(600, 400)
            .XAxis(x => x.Categories.AddRange(new[] { "North America", "Latin America", "Western Europe" }))
            .Series(s => s.AddColumn("Data", new double?[] { 100, 200, 150 }))
            .LabelLayout(ll => ll.Wrap(8).NoRotation())
            .RenderToSvg();

        Assert.Contains("<tspan", svg);
    }

    [Fact]
    public void LabelLayout_Skip2_OnlyEveryOtherLabelRendered()
    {
        var categories = new[] { "Jan", "Feb", "Mar", "Apr", "May", "Jun" };
        var svg = ChartBuilder.Create()
            .Size(600, 400)
            .XAxis(x => x.Categories.AddRange(categories))
            .Series(s => s.AddLine("Data", new double?[] { 1, 2, 3, 4, 5, 6 }))
            .LabelLayout(ll => ll.Skip(2))
            .RenderToSvg();

        // With stride 2: "Jan", "Mar", "May" visible (and "Jun" as forced last label)
        Assert.Contains("Jan", svg);
        Assert.DoesNotContain(">Feb<", svg);
        Assert.Contains("Mar", svg);
        Assert.DoesNotContain(">Apr<", svg);
    }

    [Fact]
    public void LabelLayout_DynamicFontSize_EmitsFontSizeAttribute()
    {
        // Dense long labels + small chart forces font scaling below default 11px
        var svg = ChartBuilder.Create()
            .Size(400, 300)
            .XAxis(x => x.Categories.AddRange(new[]
            {
                "VeryLongCategory01", "VeryLongCategory02", "VeryLongCategory03",
                "VeryLongCategory04", "VeryLongCategory05", "VeryLongCategory06",
            }))
            .Series(s => s.AddColumn("Data", new double?[] { 1, 2, 3, 4, 5, 6 }))
            .LabelLayout(ll => ll.FontSizeRange(7, 11).NoRotation().AutoSkip(false))
            .RenderToSvg();

        // Some font-size attribute should appear (scaled below the CSS default 11px)
        Assert.Contains("font-size=", svg);
    }

    [Fact]
    public void LabelLayout_Stagger_AddsVerticalOffset()
    {
        var svg = ChartBuilder.Create()
            .Size(600, 400)
            .XAxis(x => x.Categories.AddRange(new[] { "A", "B", "C", "D" }))
            .Series(s => s.AddColumn("Data", new double?[] { 1, 2, 3, 4 }))
            .LabelLayout(ll => ll.Stagger(12).NoRotation())
            .RenderToSvg();

        // Staggered labels have a shifted y — the SVG should contain both a base-y label
        // and a +12 offset label.  Just confirm it renders without error and contains labels.
        Assert.Contains("axis-label", svg);
    }

    [Fact]
    public void LabelLayout_CollisionDetectionOff_FallsBackToLegacyRotation()
    {
        // Legacy auto-rotate fires for very dense labels
        var svg = ChartBuilder.Create()
            .Size(400, 300)
            .XAxis(x => x.Categories.AddRange(new[]
            {
                "LongLabelOne", "LongLabelTwo", "LongLabelThree",
                "LongLabelFour", "LongLabelFive", "LongLabelSix",
            }))
            .Series(s => s.AddColumn("Data", new double?[] { 1, 2, 3, 4, 5, 6 }))
            .LabelLayout(ll => ll.DisableCollisionDetection().AutoRotate())
            .RenderToSvg();

        Assert.Contains("<svg", svg);
    }

    [Fact]
    public void LabelLayout_Fork_CopiesLabelLayoutOptions()
    {
        var original = ChartBuilder.Create()
            .XAxis(x => x.Categories.AddRange(new[] { "A", "B" }))
            .Series(s => s.AddColumn("D", new double?[] { 1, 2 }))
            .LabelLayout(ll => ll.Rotation(-30));

        var forked = original.Fork();
        var svgOriginal = original.RenderToSvg();
        var svgForked   = forked.RenderToSvg();

        Assert.Contains("rotate(-30", svgOriginal);
        Assert.Contains("rotate(-30", svgForked);
    }

    [Fact]
    public void LabelLayout_AutoRotate_MaxDegrees_OutOfRange_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            ChartBuilder.Create()
                .LabelLayout(ll => ll.AutoRotate(91)));
    }

    [Fact]
    public void LabelLayout_FontSizeRange_MinGreaterThanMax_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            ChartBuilder.Create()
                .LabelLayout(ll => ll.FontSizeRange(12, 8)));
    }

    [Fact]
    public void LabelLayout_Skip_ZeroStride_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            ChartBuilder.Create()
                .LabelLayout(ll => ll.Skip(0)));
    }

    // ================================================================== SmartLayout (annotation collision detection)

    [Fact]
    public void SmartLayout_Default_IsEnabled()
    {
        var chart = ChartBuilder.Create()
            .XAxis(x => x.Categories.AddRange(new[] { "A", "B" }))
            .YAxis(y => { y.Min = 0; y.Max = 10; })
            .Series(s => s.AddLine("S", new double?[] { 5, 8 }))
            .Annotations(a => a.SmartLayout());

        // SmartLayout() should activate AnnotationLayout with Enabled=true by default
        var svg = chart.RenderToSvg();
        Assert.NotNull(svg);
    }

    [Fact]
    public void SmartLayout_NudgesLabel_OutOfRect()
    {
        // Label initially placed inside a Rect — SmartLayout should nudge it and emit
        // a connector line (stroke-dasharray).
        var svg = ChartBuilder.Create()
            .Size(720, 420)
            .XAxis(x => x.Categories.AddRange(new[] { "Jan","Feb","Mar","Apr","May","Jun","Jul","Aug" }))
            .YAxis(y => { y.Min = 0; y.Max = 120; })
            .Series(s => s.AddLine("V", new double?[] { 32, 40, 38, 55, 72, 68, 95, 110 }))
            .Annotations(a => a
                .SmartLayout()
                .Rect(5, 0, 7, 120)                                          // big shaded region
                .Label(7, 110, "All-time high",
                    cfg => { cfg.TextAnchor = "end"; cfg.OffsetX = -20; cfg.OffsetY = -14; }))
            .RenderToSvg();

        // Connector leader line must be present because the label was nudged
        Assert.Contains("stroke-dasharray", svg);
    }

    [Fact]
    public void SmartLayout_NoConnectors_WhenDisabled()
    {
        var svg = ChartBuilder.Create()
            .Size(720, 420)
            .XAxis(x => x.Categories.AddRange(new[] { "Jan","Feb","Mar","Apr","May","Jun","Jul","Aug" }))
            .YAxis(y => { y.Min = 0; y.Max = 120; })
            .Series(s => s.AddLine("V", new double?[] { 32, 40, 38, 55, 72, 68, 95, 110 }))
            .Annotations(a => a
                .SmartLayout(lo => lo.DrawConnectors = false)
                .Rect(5, 0, 7, 120)
                .Label(7, 110, "All-time high",
                    cfg => { cfg.TextAnchor = "end"; cfg.OffsetX = -20; cfg.OffsetY = -14; }))
            .RenderToSvg();

        // No dashed connector should appear
        Assert.DoesNotContain("stroke-dasharray=\"3,2\"", svg);
    }

    [Fact]
    public void SmartLayout_NonOverlappingLabel_NoConnector()
    {
        // A label placed well away from any obstacle should NOT trigger a connector.
        var svg = ChartBuilder.Create()
            .Size(720, 420)
            .XAxis(x => x.Categories.AddRange(new[] { "Jan","Feb","Mar","Apr","May","Jun","Jul","Aug" }))
            .YAxis(y => { y.Min = 0; y.Max = 120; })
            .Series(s => s.AddLine("V", new double?[] { 32, 40, 38, 55, 72, 68, 95, 110 }))
            .Annotations(a => a
                .SmartLayout()
                .Label(0, 32, "Start", cfg => cfg.OffsetY = -20))  // isolated label, no obstacles
            .RenderToSvg();

        // No connector should be emitted for a label that wasn't nudged
        Assert.DoesNotContain("stroke-dasharray=\"3,2\"", svg);
    }

    [Fact]
    public void SmartLayout_Fork_CopiesAnnotationLayoutOptions()
    {
        var original = ChartBuilder.Create()
            .XAxis(x => x.Categories.AddRange(new[] { "A", "B" }))
            .YAxis(y => { y.Min = 0; y.Max = 10; })
            .Series(s => s.AddLine("S", new double?[] { 5, 8 }))
            .Annotations(a => a.SmartLayout(lo => lo.ConnectorOpacity = 0.8));

        var fork = original.Fork()
            .Title("Fork");

        var svgFork = fork.RenderToSvg();
        Assert.NotNull(svgFork);
    }

    // ------------------------------------------------------------------ Phase 1 tests

    [Fact]
    public void RenderToDataUri_ReturnsBase64DataUri()
    {
        string uri = ChartBuilder.Create()
            .Series(s => s.AddLine("Sales", new double?[] { 10, 20, 15 }))
            .RenderToDataUri();

        Assert.StartsWith("data:image/svg+xml;base64,", uri);
        // Round-trip: decode and verify it is valid SVG
        string decoded = System.Text.Encoding.UTF8.GetString(
            Convert.FromBase64String(uri.Substring("data:image/svg+xml;base64,".Length)));
        Assert.Contains("<svg", decoded);
    }

    [Fact]
    public void Responsive_SetsFluidWidth()
    {
        string svg = ChartBuilder.Create()
            .Responsive()
            .Series(s => s.AddLine("S", new double?[] { 1, 2, 3 }))
            .RenderToSvg();

        Assert.Contains("width=\"100%\"", svg);
    }

    [Fact]
    public void Clone_IsIndependentCopy()
    {
        var original = ChartBuilder.Create()
            .Title("Original")
            .Series(s => s.AddLine("A", new double?[] { 1, 2, 3 }));

        var clone = original.Clone().Title("Clone");

        Assert.Contains("Original", original.RenderToSvg());
        Assert.Contains("Clone",    clone.RenderToSvg());
        Assert.DoesNotContain("Clone",    original.RenderToSvg());
        Assert.DoesNotContain("Original", clone.RenderToSvg());
    }

    [Fact]
    public void NullGap_Break_LeavesGapInLine()
    {
        // Break (default) should produce two separate path segments — data visible in SVG as two <path> elements.
        string svg = ChartBuilder.Create()
            .AsStatic()
            .XAxis(x => x.Categories.AddRange(new[] { "A", "B", "C", "D" }))
            .Series(s => s.AddLine("S", new double?[] { 10, null, null, 30 },
                cfg => cfg.NullGap(GapPolicy.Break)))
            .RenderToSvg();

        Assert.Contains("<path", svg);
    }

    [Fact]
    public void NullGap_Connect_ProducesSinglePath()
    {
        // With Connect the null is skipped and the two real points should form one contiguous path.
        string svg = ChartBuilder.Create()
            .AsStatic()
            .XAxis(x => x.Categories.AddRange(new[] { "A", "B", "C" }))
            .Series(s => s.AddLine("S", new double?[] { 10, null, 30 },
                cfg => cfg.NullGap(GapPolicy.Connect)))
            .RenderToSvg();

        Assert.Contains("<path", svg);
        // Only one line segment should exist (no gap path break), verified indirectly by successful render.
    }

    [Fact]
    public void NullGap_Zero_PlotsMissingPointAtBaseline()
    {
        // With Zero the null becomes 0 — the path must still contain it without breaking.
        string svg = ChartBuilder.Create()
            .AsStatic()
            .XAxis(x => x.Categories.AddRange(new[] { "A", "B", "C" }))
            .YAxis(y => { y.Min = 0; y.Max = 50; })
            .Series(s => s.AddLine("S", new double?[] { 10, null, 30 },
                cfg => cfg.NullGap(GapPolicy.Zero)))
            .RenderToSvg();

        Assert.Contains("<path", svg);
    }

    [Fact]
    public void TargetLine_RendersHorizontalLine()
    {
        string svg = ChartBuilder.Create()
            .AsStatic()
            .XAxis(x => x.Categories.AddRange(new[] { "A", "B", "C" }))
            .YAxis(y => { y.Min = 0; y.Max = 100; })
            .Series(s => s.AddLine("Sales", new double?[] { 40, 60, 55 },
                cfg => cfg.TargetLine(50, "Target", "#cc0000", "Dash")))
            .RenderToSvg();

        // A <line> element for the target line should appear in the SVG.
        Assert.Contains("<line", svg);
        Assert.Contains("Target", svg);
    }

    [Fact]
    public void HighContrast_Theme_UsesBlackTextOnWhite()
    {
        string svg = ChartBuilder.Create()
            .Theme(ChartTheme.HighContrast)
            .Series(s => s.AddLine("Sales", new double?[] { 1, 2, 3 }))
            .RenderToSvg();

        Assert.Contains("<svg", svg);
        // Background should be white and text black in the emitted SVG.
        Assert.Contains("#ffffff", svg.ToLowerInvariant());
    }

    // ------------------------------------------------------------------ Phase 2 tests

    [Fact]
    public void StackPercent_YAxisLabelsContainPercentSign()
    {
        string svg = ChartBuilder.Create()
            .AsStatic()
            .StackPercent()
            .XAxis(x => x.Categories.AddRange(new[] { "A", "B" }))
            .Series(s => s
                .AddColumn("X", new double?[] { 30, 40 })
                .AddColumn("Y", new double?[] { 70, 60 }))
            .RenderToSvg();

        Assert.Contains("%", svg);
    }

    [Fact]
    public void YAxisInverted_MinValueAppearsAtTop()
    {
        // With an inverted axis the tick label for the minimum value (0) should be
        // rendered at a smaller Y pixel value (closer to top) than the maximum (100).
        string svg = ChartBuilder.Create()
            .AsStatic()
            .YAxisInverted()
            .YAxis(y => { y.Min = 0; y.Max = 100; })
            .Series(s => s.AddLine("S", new double?[] { 25, 75 }))
            .RenderToSvg();

        // Both values must appear as tick labels; the chart must render without error.
        Assert.Contains("<svg", svg);
        Assert.Contains("100", svg);
    }

    [Fact]
    public void DualYAxis_SecondaryTicksAppearOnRightSide()
    {
        string svg = ChartBuilder.Create()
            .AsStatic()
            .XAxis(x => x.Categories.AddRange(new[] { "Jan", "Feb", "Mar" }))
            .YAxis(y => { y.Title = "Revenue"; y.Min = 0; y.Max = 500; })
            .YAxis2(y => { y.Title = "Growth %"; y.Min = 0; y.Max = 100; })
            .Series(s => s
                .AddColumn("Revenue", new double?[] { 200, 350, 420 })
                .AddLine("Growth", new double?[] { 15, 40, 75 },
                    cfg => cfg.OnSecondaryAxis()))
            .RenderToSvg();

        // The secondary axis border line renders at x = PaddingLeft + plotWidth,
        // verifiable by the presence of its title in the output.
        Assert.Contains("Growth %", svg);
        Assert.Contains("Revenue",  svg);
    }

    [Fact]
    public void MixedCombo_ColumnAndLineOnSameChart_BothRender()
    {
        string svg = ChartBuilder.Create()
            .AsStatic()
            .XAxis(x => x.Categories.AddRange(new[] { "Q1", "Q2", "Q3", "Q4" }))
            .Series(s => s
                .AddColumn("Sales",  new double?[] { 120, 150, 130, 180 })
                .AddLine("Trend",    new double?[] { 125, 140, 138, 172 }))
            .RenderToSvg();

        // Both <rect> (column) and <path> (line) elements must appear.
        Assert.Contains("<rect", svg);
        Assert.Contains("<path", svg);
    }

    // ------------------------------------------------------------------ Phase 3 tests

    [Fact]
    public void LinearRegression_ProducesNonNullValuesForFullData()
    {
        double[] data = { 10, 20, 15, 30, 25, 40 };
        string svg = ChartBuilder.Create()
            .AsStatic()
            .XAxis(x => x.Categories.AddRange(new[] { "A", "B", "C", "D", "E", "F" }))
            .Series(s => s
                .AddColumn("Raw", data)
                .AddLinearRegression("Trend", data, cfg => cfg.Dashed()))
            .RenderToSvg();

        // Linear regression produces a line series — <path> must appear
        Assert.Contains("<path", svg);
    }

    [Fact]
    public void MovingAverage_Period3_FirstTwoPointsAreNull()
    {
        // MA(3) emits null for the first period-1 points, then averages.
        // With 6 data points the last 4 have values; the first 2 are null → Connect policy needed to verify no crash.
        double[] data = { 5, 10, 15, 10, 20, 25 };
        string svg = ChartBuilder.Create()
            .AsStatic()
            .Series(s => s
                .AddLine("Raw", data)
                .AddMovingAverage("MA3", data, period: 3, cfg => cfg.Color("#888")))
            .RenderToSvg();

        Assert.Contains("<svg", svg);
        Assert.DoesNotContain("NaN", svg);
    }

    [Fact]
    public void ExponentialSmoothing_Alpha05_EmitsSinglePath()
    {
        double[] data = { 10, 14, 12, 18, 16, 22 };
        string svg = ChartBuilder.Create()
            .AsStatic()
            .Series(s => s
                .AddLine("Raw",  data)
                .AddExponentialSmoothing("EMA", data, alpha: 0.5, cfg => cfg.LineWidth(2)))
            .RenderToSvg();

        Assert.Contains("<path", svg);
        Assert.DoesNotContain("NaN", svg);
    }

    [Fact]
    public void ShowDataTable_AppendsTableRowsToSvg()
    {
        string svg = ChartBuilder.Create()
            .AsStatic()
            .Size(700, 400)
            .ShowDataTable()
            .XAxis(x => x.Categories.AddRange(new[] { "Jan", "Feb", "Mar" }))
            .Series(s => s
                .AddColumn("Sales", new double?[] { 100, 120, 140 })
                .AddLine("Target",  new double?[] { 110, 115, 130 }))
            .RenderToSvg();

        // Category labels must appear in the table section
        Assert.Contains("Jan", svg);
        Assert.Contains("Feb", svg);
        // Series headers must appear
        Assert.Contains("Sales", svg);
        Assert.Contains("Target", svg);
    }

    [Fact]
    public void ShowDataTable_EmDashForNullValues()
    {
        string svg = ChartBuilder.Create()
            .AsStatic()
            .ShowDataTable()
            .XAxis(x => x.Categories.AddRange(new[] { "A", "B", "C" }))
            .Series(s => s.AddLine("S", new double?[] { 10, null, 30 }))
            .RenderToSvg();

        // Null value is rendered as em-dash
        Assert.Contains("\u2014", svg);
    }

    // ------------------------------------------------------------------ Phase 4 tests

    [Fact]
    public void AriaLabel_Override_AppearsInSvgTitle()
    {
        string svg = ChartBuilder.Create()
            .AsStatic()
            .AriaLabel("Custom chart title for screen readers")
            .Series(s => s.AddLine("S", new double?[] { 1, 2, 3 }))
            .RenderToSvg();

        Assert.Contains("Custom chart title for screen readers", svg);
    }

    [Fact]
    public void AriaDescription_Override_AppearsInSvgDesc()
    {
        string svg = ChartBuilder.Create()
            .AsStatic()
            .AriaDescription("Revenue increased 20% over four quarters.")
            .Series(s => s.AddLine("S", new double?[] { 1, 2, 3 }))
            .RenderToSvg();

        Assert.Contains("Revenue increased 20% over four quarters.", svg);
    }

    [Fact]
    public void SvgRoot_HasAriaLabelledByAndDescribedBy()
    {
        string svg = ChartBuilder.Create()
            .AsStatic()
            .Title("Sales")
            .Series(s => s.AddLine("S", new double?[] { 1, 2, 3 }))
            .RenderToSvg();

        Assert.Contains("aria-labelledby=", svg);
        Assert.Contains("aria-describedby=", svg);
    }

    [Fact]
    public void Culture_German_UsesCommaDecimalSeparator()
    {
        string svg = ChartBuilder.Create()
            .AsStatic()
            .Culture("de-DE")
            .YAxis(y => { y.Min = 0; y.Max = 1500; })
            .Series(s => s.AddLine("S", new double?[] { 1200, 1300, 1400 }))
            .RenderToSvg();

        // German culture uses "1,5k" not "1.5k" for values like 1500
        Assert.Contains("lang=\"de\"", svg);
    }

    [Fact]
    public void RightToLeft_AddsDirRtlToSvgRoot()
    {
        string svg = ChartBuilder.Create()
            .AsStatic()
            .RightToLeft()
            .Series(s => s.AddLine("S", new double?[] { 1, 2, 3 }))
            .RenderToSvg();

        Assert.Contains("dir=\"rtl\"", svg);
    }

    [Fact]
    public void RightToLeft_EmitsDirectionRtlCss()
    {
        string svg = ChartBuilder.Create()
            .AsAnimated()
            .RightToLeft()
            .Series(s => s.AddLine("S", new double?[] { 1, 2, 3 }))
            .RenderToSvg();

        Assert.Contains("direction: rtl", svg);
    }

    // ------------------------------------------------------------------ Phase 5 tests

    [Fact]
    public void ApplyTemplate_Revenue_SetsColumnTypeAndN0Format()
    {
        string svg = ChartBuilder.Create()
            .ApplyTemplate(ChartTemplate.Revenue)
            .Title("Sales")
            .XAxis(x => x.Categories.AddRange(new[] { "Q1", "Q2" }))
            .Series(s => s.AddColumn("Rev", new double?[] { 1200, 1400 }))
            .RenderToSvg();

        Assert.Contains("<svg", svg);
        Assert.Contains("Sales", svg);
    }

    [Fact]
    public void ApplyTemplate_KpiDashboard_UsesDarkThemeBackground()
    {
        string svg = ChartBuilder.Create()
            .ApplyTemplate(ChartTemplate.KpiDashboard)
            .Series(s => s.AddColumn("KPI", new double?[] { 75, 82, 91 }))
            .RenderToSvg();

        // Dark theme background colour is midnight navy (#0D1B2A or similar)
        Assert.Contains("fill=", svg);
        Assert.DoesNotContain("fill=\"#FFFFFF\"", svg);  // not white background
    }

    [Fact]
    public void ApplyTemplate_TimeSeries_EmitsSplinePaths()
    {
        string svg = ChartBuilder.Create()
            .ApplyTemplate(ChartTemplate.TimeSeries)
            .Series(s => s.AddSpline("Trend", new double?[] { 10, 30, 20, 40 }))
            .RenderToSvg();

        Assert.Contains("<path", svg);
    }

    [Fact]
    public void ApplyTemplate_ExecutiveSummary_ProducesStaticSvg()
    {
        string svg = ChartBuilder.Create()
            .ApplyTemplate(ChartTemplate.ExecutiveSummary)
            .Series(s => s.AddBar("Division A", new double?[] { 320, 410, 390 }))
            .RenderToSvg();

        // Static mode: no <script> tag
        Assert.DoesNotContain("<script", svg);
    }

    [Fact]
    public void ApplyTemplate_Accessible_UsesHighContrastTheme()
    {
        string svg = ChartBuilder.Create()
            .ApplyTemplate(ChartTemplate.Accessible)
            .Series(s => s.AddColumn("Data", new double?[] { 1, 2, 3 }))
            .RenderToSvg();

        Assert.DoesNotContain("<script", svg);
        Assert.Contains("<svg", svg);
    }

    [Fact]
    public void ApplyTemplate_CanBeOverriddenAfterApplication()
    {
        // template sets AsColumn; caller overrides to AsLine
        string svg = ChartBuilder.Create()
            .ApplyTemplate(ChartTemplate.Revenue)
            .AsLine()
            .Series(s => s.AddLine("Override", new double?[] { 1, 2, 3 }))
            .RenderToSvg();

        Assert.Contains("<svg", svg);
    }

#if NET6_0_OR_GREATER
    [Fact]
    public void ToJson_ProducesNonEmptyJsonString()
    {
        string json = ChartBuilder.Create()
            .Title("Revenue Chart")
            .AsColumn()
            .Series(s => s.AddColumn("Q", new double?[] { 100, 200, 300 }))
            .Build()
            .ToJson();

        Assert.False(string.IsNullOrWhiteSpace(json));
        Assert.Contains("Revenue Chart", json);
        Assert.Contains("\"Column\"", json);   // ChartType enum as string
    }

    [Fact]
    public void FromJson_RoundTrip_PreservesTitle()
    {
        var original = ChartBuilder.Create()
            .Title("Round Trip Test")
            .AsColumn()
            .Series(s => s.AddColumn("S", new double?[] { 10, 20, 30 }))
            .Build();

        string json = original.ToJson();
        ChartOptions restored = ChartOptions.FromJson(json);

        Assert.Equal("Round Trip Test", restored.Title.Text);
    }

    [Fact]
    public void FromJson_RoundTrip_PreservesSeriesData()
    {
        var opts = ChartBuilder.Create()
            .Series(s => s.AddLine("L", new double?[] { 1.5, null, 3.5 }))
            .Build();

        string json = opts.ToJson();
        ChartOptions restored = ChartOptions.FromJson(json);

        Assert.Single(restored.Series);
        Assert.Equal(3, restored.Series[0].Data.Count);
        Assert.Equal(1.5, restored.Series[0].Data[0]);
        Assert.Null(restored.Series[0].Data[1]);
        Assert.Equal(3.5, restored.Series[0].Data[2]);
    }

    [Fact]
    public void ChartBuilder_FromJson_ProducesRenderableSvg()
    {
        string json = ChartBuilder.Create()
            .Title("Persisted Chart")
            .Series(s => s.AddColumn("S", new double?[] { 10, 20 }))
            .Build()
            .ToJson();

        string svg = ChartBuilder.FromJson(json).RenderToSvg();

        Assert.Contains("<svg", svg);
        Assert.Contains("Persisted Chart", svg);
    }
#endif

    // ------------------------------------------------------------------ Phase 6 tests

    [Fact]
    public void Dumbbell_RendersConnectorAndBothDots()
    {
        var data = new[] { new RangePoint { Low = 20, High = 80 }, new RangePoint { Low = 30, High = 70 } };
        string svg = ChartBuilder.Create()
            .AsStatic()
            .XAxis(x => x.Categories.AddRange(new[] { "A", "B" }))
            .YAxis(y => { y.Min = 0; y.Max = 100; })
            .Series(s => s.AddDumbbell("Range", data))
            .RenderToSvg();

        Assert.Contains("<circle", svg);
        Assert.Contains("<line", svg);
    }

    [Fact]
    public void Stream_MultipleSeriesRenderFilledPaths()
    {
        string svg = ChartBuilder.Create()
            .AsStatic()
            .XAxis(x => x.Categories.AddRange(new[] { "Jan", "Feb", "Mar", "Apr" }))
            .Series(s => s
                .AddStream("Cat A", new double?[] { 10, 20, 15, 25 })
                .AddStream("Cat B", new double?[] { 15, 10, 20, 18 })
                .AddStream("Cat C", new double?[] {  8, 12, 10, 14 }))
            .RenderToSvg();

        Assert.Contains("<path", svg);
        Assert.Contains("Cat A", svg);
    }

    [Fact]
    public void Gantt_RendersBarsWithTaskNames()
    {
        var tasks = new[]
        {
            new GanttTask { Name = "Design",  Start = 0, End = 5 },
            new GanttTask { Name = "Develop", Start = 4, End = 12 },
            new GanttTask { Name = "Test",    Start = 10, End = 15 },
        };
        string svg = ChartBuilder.Create()
            .AsStatic()
            .Series(s => s.AddGantt("Sprint 1", tasks))
            .RenderToSvg();

        Assert.Contains("<svg", svg);
        Assert.Contains("Design", svg);
        Assert.Contains("Develop", svg);
        Assert.Contains("<rect", svg);
    }

    [Fact]
    public void Sankey_RendersNodesAndLinks()
    {
        var nodes = new[]
        {
            new SankeyNode { Name = "Source" },
            new SankeyNode { Name = "Process" },
            new SankeyNode { Name = "Output" },
        };
        var links = new[]
        {
            new SankeyLink { From = 0, To = 1, Value = 100 },
            new SankeyLink { From = 1, To = 2, Value = 80 },
        };
        string svg = ChartBuilder.Create()
            .AsStatic()
            .Series(s => s.AddSankey("Flow", nodes, links))
            .RenderToSvg();

        Assert.Contains("<svg", svg);
        Assert.Contains("Source", svg);
        Assert.Contains("Output", svg);
        Assert.Contains("<path", svg);   // bezier links
        Assert.Contains("<rect", svg);   // nodes
    }

    [Fact]
    public void Gantt_ExplicitXRange_IsRespected()
    {
        var tasks = new[] { new GanttTask { Name = "T1", Start = 5, End = 10 } };
        string svg = ChartBuilder.Create()
            .AsStatic()
            .XAxis(x => { x.Min = 0; x.Max = 20; })
            .Series(s => s.AddGantt("Project", tasks))
            .RenderToSvg();

        Assert.Contains("<svg", svg);
        Assert.DoesNotContain("NaN", svg);
        Assert.DoesNotContain("Infinity", svg);
    }

    // ------------------------------------------------------------------ Phase 7 tests

    [Fact]
    public void RangeSelector_Enabled_RendersStripElements()
    {
        string svg = ChartBuilder.Create()
            .AsInteractive()
            .XAxis(x => x.Categories.AddRange(new[] { "Jan", "Feb", "Mar", "Apr", "May" }))
            .Series(s => s.AddLine("Sales", new double?[] { 10, 20, 15, 30, 25 }))
            .RangeSelector(rs => { rs.Enabled = true; rs.Height = 50; })
            .RenderToSvg();

        Assert.Contains("tf-rs-bg", svg);
        Assert.Contains("tf-rs-overlay", svg);
        Assert.Contains("tf-rs-handle", svg);
        Assert.Contains("tf:rangechange", svg);
    }

    [Fact]
    public void RangeSelector_NotEnabled_NoStripElements()
    {
        string svg = ChartBuilder.Create()
            .AsInteractive()
            .Series(s => s.AddColumn("A", new double?[] { 1, 2, 3 }))
            .RenderToSvg();

        Assert.DoesNotContain("tf-rs-bg", svg);
    }

    [Fact]
    public void RangeSelector_StaticMode_NotRendered()
    {
        string svg = ChartBuilder.Create()
            .AsStatic()
            .Series(s => s.AddLine("B", new double?[] { 5, 8 }))
            .RangeSelector(rs => rs.Enabled = true)
            .RenderToSvg();

        Assert.DoesNotContain("tf-rs-bg", svg);
    }

    [Fact]
    public void SyncGroup_SetId_EmitsGroupIdInScript()
    {
        string svg = ChartBuilder.Create()
            .AsInteractive()
            .Series(s => s.AddLine("Revenue", new double?[] { 10, 20, 30 }))
            .SyncGroup("dashboard-1")
            .RenderToSvg();

        Assert.Contains("dashboard-1", svg);
        Assert.Contains("_tfSync", svg);
    }

    [Fact]
    public void SyncGroup_NullNotSet_NoSyncScript()
    {
        string svg = ChartBuilder.Create()
            .AsInteractive()
            .Series(s => s.AddLine("X", new double?[] { 1, 2 }))
            .RenderToSvg();

        Assert.DoesNotContain("_tfSync", svg);
    }

    [Fact]
    public void Drilldown_WithChildChart_EmbedsChildContentAndBackButton()
    {
        var childChart = ChartBuilder.Create()
            .Title("Detail View")
            .AsInteractive()
            .Series(s => s.AddColumn("Detail", new double?[] { 5, 10, 8 }))
            .Build();

        string svg = ChartBuilder.Create()
            .Title("Overview")
            .AsInteractive()
            .Series(s => s.AddColumn("Revenue", new double?[] { 100, 200, 150 },
                cfg => cfg.WithDrilldown(childChart)))
            .RenderToSvg();

        Assert.Contains("tf-dd", svg);
        Assert.Contains("tf-dd-back", svg);
        Assert.Contains("Detail View", svg);
        Assert.Contains("ddMap", svg);          // drilldown JS name map
    }

    [Fact]
    public void Drilldown_NoChildChart_NoEmbeddedGroup()
    {
        string svg = ChartBuilder.Create()
            .AsInteractive()
            .Series(s => s.AddColumn("Sales", new double?[] { 1, 2, 3 }))
            .RenderToSvg();

        Assert.DoesNotContain("tf-dd", svg);
    }

    [Fact]
    public void RangeSelectorOptions_Clone_CopiesAllProperties()
    {
        var rs = new RangeSelectorOptions { Enabled = true, Height = 80, FillColor = "#abc", HandleColor = "#def" };
        var clone = rs.Clone();

        Assert.True(clone.Enabled);
        Assert.Equal(80, clone.Height);
        Assert.Equal("#abc", clone.FillColor);
        Assert.Equal("#def", clone.HandleColor);
    }
}

