namespace TerraFluent.Chart.Reporting.Enums
{
    /// <summary>
    /// Controls what is emitted in the SVG output.
    /// </summary>
    public enum SvgMode
    {
        /// <summary>Pure SVG — no CSS hover effects, no embedded JS. Safe for PDF, email.</summary>
        Static,

        /// <summary>SVG + embedded CSS for hover tooltips and transitions. Safe for browsers and Blazor.</summary>
        Animated,

        /// <summary>SVG + embedded CSS + minimal JS for click/zoom interactions. Browser only.</summary>
        Interactive
    }
}
