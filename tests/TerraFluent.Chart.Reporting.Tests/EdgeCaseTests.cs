using System;
using System.Linq;
using System.Text;
using TerraFluent.Chart.Reporting.Builder;
using TerraFluent.Chart.Reporting.Enums;
using TerraFluent.Chart.Reporting.Models;
using Xunit;

namespace TerraFluent.Chart.Reporting.Tests;

/// <summary>Edge-case and correctness tests that go beyond basic smoke coverage.</summary>
public class EdgeCaseTests
{
    // ── Empty / minimal data ─────────────────────────────────────────────────

    [Fact]
    public void RenderToSvg_EmptyDataArray_WellFormedSvg()
    {
        var svg = ChartBuilder.Create()
            .Series(s => s.AddLine("Empty", Array.Empty<double?>()))
            .RenderToSvg();

        SvgAssert.WellFormed(svg);
    }

    [Fact]
    public void RenderToSvg_SinglePoint_WellFormedSvg()
    {
        var svg = ChartBuilder.Create()
            .XAxis(x => x.Categories.Add("Jan"))
            .Series(s => s.AddLine("Solo", new double?[] { 42 }))
            .RenderToSvg();

        SvgAssert.WellFormedAndContains(svg, "42");
    }

    [Fact]
    public void RenderToSvg_AllNullData_WellFormedSvg()
    {
        var svg = ChartBuilder.Create()
            .XAxis(x => x.Categories.AddRange(new[] { "A", "B", "C" }))
            .Series(s => s.AddLine("Gaps", new double?[] { null, null, null }))
            .RenderToSvg();

        SvgAssert.WellFormed(svg);
    }

    [Fact]
    public void RenderToSvg_SinglePointColumn_WellFormedSvg()
    {
        var svg = ChartBuilder.Create()
            .XAxis(x => x.Categories.Add("Q1"))
            .Series(s => s.AddColumn("Rev", new double?[] { 999 }))
            .RenderToSvg();

        SvgAssert.WellFormed(svg);
    }

    // ── Y-axis constant / degenerate range ───────────────────────────────────

    [Fact]
    public void RenderToSvg_AllValuesIdentical_WellFormedSvg()
    {
        // min == max → axis range is zero; renderer must handle the degenerate case.
        var svg = ChartBuilder.Create()
            .XAxis(x => x.Categories.AddRange(new[] { "A", "B", "C" }))
            .Series(s => s.AddLine("Flat", new double?[] { 5, 5, 5 }))
            .RenderToSvg();

        SvgAssert.WellFormed(svg);
    }

    [Fact]
    public void RenderToSvg_AllZeroValues_WellFormedSvg()
    {
        var svg = ChartBuilder.Create()
            .XAxis(x => x.Categories.AddRange(new[] { "A", "B", "C" }))
            .Series(s => s.AddColumn("Zero", new double?[] { 0, 0, 0 }))
            .RenderToSvg();

        SvgAssert.WellFormed(svg);
    }

    // ── Non-finite inputs (NaN / Infinity) ───────────────────────────────────

    [Fact]
    public void RenderToSvg_NonFiniteValues_OutputContainsNoNaNOrInfinity()
    {
        var svg = ChartBuilder.Create()
            .XAxis(x => x.Categories.AddRange(new[] { "A", "B", "C" }))
            .Series(s => s.AddLine("Bad", new double?[] { double.NaN, double.PositiveInfinity, 1 }))
            .RenderToSvg();

        SvgAssert.WellFormed(svg);
        Assert.DoesNotContain("NaN",      svg);
        Assert.DoesNotContain("Infinity", svg);
    }

    // ── XSS / injection in user-supplied strings ─────────────────────────────

    [Fact]
    public void RenderToSvg_XssInSeriesName_EscapedInOutput()
    {
        const string malicious = "<script>alert('xss')</script>";
        var svg = ChartBuilder.Create()
            .Series(s => s.AddLine(malicious, new double?[] { 1, 2, 3 }))
            .RenderToSvg();

        SvgAssert.WellFormed(svg);
        Assert.DoesNotContain("<script>alert", svg);
    }

    [Fact]
    public void RenderToSvg_XssInAxisTitle_EscapedInOutput()
    {
        var svg = ChartBuilder.Create()
            .XAxis(x => x.Title = "<img onerror=\"alert(1)\" src=x>")
            .Series(s => s.AddLine("Data", new double?[] { 1 }))
            .RenderToSvg();

        SvgAssert.WellFormed(svg);
        Assert.DoesNotContain("<img", svg);
    }

    [Fact]
    public void RenderToSvg_XssInCategoryLabel_EscapedInOutput()
    {
        var svg = ChartBuilder.Create()
            .XAxis(x => x.Categories.Add("Q1 & Q2 <special>"))
            .Series(s => s.AddLine("Data", new double?[] { 1 }))
            .RenderToSvg();

        SvgAssert.WellFormed(svg);
        Assert.DoesNotContain("<special>", svg);
    }

    // ── RenderToDataUri round-trip ────────────────────────────────────────────

    [Fact]
    public void RenderToDataUri_PrefixIsCorrect()
    {
        var uri = ChartBuilder.Create()
            .Series(s => s.AddLine("Data", new double?[] { 1, 2, 3 }))
            .RenderToDataUri();

        Assert.StartsWith("data:image/svg+xml;base64,", uri);
    }

    [Fact]
    public void RenderToDataUri_DecodesBackToWellFormedSvg()
    {
        const string prefix = "data:image/svg+xml;base64,";
        var uri = ChartBuilder.Create()
            .Title("Round-trip test")
            .Series(s => s.AddLine("Data", new double?[] { 10, 20, 30 }))
            .RenderToDataUri();

        string base64 = uri.Substring(prefix.Length);
        string decoded = Encoding.UTF8.GetString(Convert.FromBase64String(base64));
        SvgAssert.WellFormedAndContains(decoded, "Round-trip test");
    }

    // ── Multi-series edge cases ───────────────────────────────────────────────

    [Fact]
    public void RenderToSvg_MixedSeriesTypes_WellFormedSvg()
    {
        var svg = ChartBuilder.Create()
            .XAxis(x => x.Categories.AddRange(new[] { "Jan", "Feb", "Mar" }))
            .Series(s => s
                .AddLine("Line",   new double?[] { 10, 20, 30 })
                .AddColumn("Bar",  new double?[] { 5,  15, 25 }))
            .RenderToSvg();

        SvgAssert.WellFormed(svg);
    }

    // ── Static mode guarantees ───────────────────────────────────────────────

    [Fact]
    public void RenderToSvg_StaticMode_ContainsNoScriptTag()
    {
        var svg = ChartBuilder.Create()
            .AsStatic()
            .Series(s => s.AddLine("Data", new double?[] { 1, 2, 3 }))
            .RenderToSvg();

        SvgAssert.WellFormedAndExcludes(svg, "<script");
    }

    [Fact]
    public void RenderToSvg_StaticMode_HitAreasAreAbsent()
    {
        var svg = ChartBuilder.Create()
            .AsStatic()
            .Series(s => s.AddColumn("Data", new double?[] { 10, 20 }))
            .RenderToSvg();

        SvgAssert.WellFormedAndExcludes(svg, "hit-area");
    }

    // ── Fork / Clone independence ────────────────────────────────────────────

    [Fact]
    public void Fork_OriginalAndForkAreIndependent()
    {
        var original = ChartBuilder.Create()
            .Title("Original")
            .Series(s => s.AddLine("Data", new double?[] { 1, 2, 3 }));

        var forked = original.Fork().Title("Fork");

        string svgOriginal = original.RenderToSvg();
        string svgFork     = forked.RenderToSvg();

        Assert.Contains("Original", svgOriginal);
        Assert.DoesNotContain("Fork", svgOriginal);
        Assert.Contains("Fork", svgFork);
        Assert.DoesNotContain("Original", svgFork);
    }

    // ── Guard-clause validation ──────────────────────────────────────────────

    [Fact]
    public void AddLine_NullName_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            ChartBuilder.Create()
                .Series(s => s.AddLine(null!, new double?[] { 1, 2 })));
    }

    [Fact]
    public void AddLine_NullData_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            ChartBuilder.Create()
                .Series(s => s.AddLine("Series", (double?[])null!)));
    }

    [Fact]
    public void Size_ZeroWidth_ThrowsArgumentOutOfRangeException()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            ChartBuilder.Create().Size(0, 400));
    }

    [Fact]
    public void YAxis_MinGreaterThanMax_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() =>
            ChartBuilder.Create().YAxis("Y", min: 100, max: 10));
    }

    // ── Accessibility attributes ─────────────────────────────────────────────

    [Fact]
    public void RenderToSvg_ContainsRoleImg()
    {
        var svg = ChartBuilder.Create()
            .Series(s => s.AddLine("Data", new double?[] { 1, 2, 3 }))
            .RenderToSvg();

        SvgAssert.WellFormedAndContains(svg, "role=\"img\"");
    }

    [Fact]
    public void RenderToSvg_ContainsTitleAndDescElements()
    {
        var svg = ChartBuilder.Create()
            .Title("Accessibility Chart")
            .Series(s => s.AddLine("Data", new double?[] { 1, 2, 3 }))
            .RenderToSvg();

        SvgAssert.WellFormedAndContains(svg, "<title");
        SvgAssert.WellFormedAndContains(svg, "<desc");
        SvgAssert.WellFormedAndContains(svg, "Accessibility Chart");
    }

    [Fact]
    public void AriaLabel_Override_AppearsInSvg()
    {
        var svg = ChartBuilder.Create()
            .AriaLabel("Custom accessible name")
            .Series(s => s.AddLine("Data", new double?[] { 1 }))
            .RenderToSvg();

        SvgAssert.WellFormedAndContains(svg, "Custom accessible name");
    }
}
