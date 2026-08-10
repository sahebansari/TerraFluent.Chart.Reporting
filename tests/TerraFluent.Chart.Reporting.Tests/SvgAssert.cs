using System.Xml;
using Xunit;

namespace TerraFluent.Chart.Reporting.Tests;

/// <summary>Shared assertion helpers for SVG output correctness.</summary>
internal static class SvgAssert
{
    /// <summary>
    /// Asserts that <paramref name="svg"/> is well-formed XML and its root element is &lt;svg&gt;.
    /// Throws <see cref="XmlException"/> on malformed markup so the test failure pinpoints the renderer bug.
    /// </summary>
    internal static void WellFormed(string svg)
    {
        Assert.NotNull(svg);
        Assert.NotEmpty(svg);

        var doc = new XmlDocument();
        // XmlDocument.LoadXml throws XmlException on any well-formedness violation.
        doc.LoadXml(svg);

        Assert.Equal("svg", doc.DocumentElement!.LocalName);
    }

    /// <summary>
    /// Asserts that <paramref name="svg"/> is well-formed XML and also contains <paramref name="content"/>.
    /// </summary>
    internal static void WellFormedAndContains(string svg, string content)
    {
        WellFormed(svg);
        Assert.Contains(content, svg);
    }

    /// <summary>
    /// Asserts that <paramref name="svg"/> is well-formed XML and does not contain <paramref name="content"/>.
    /// </summary>
    internal static void WellFormedAndExcludes(string svg, string content)
    {
        WellFormed(svg);
        Assert.DoesNotContain(content, svg);
    }
}
