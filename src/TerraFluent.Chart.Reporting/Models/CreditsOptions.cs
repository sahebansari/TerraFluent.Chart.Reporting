using TerraFluent.Chart.Reporting.Enums;

namespace TerraFluent.Chart.Reporting.Models
{
    /// <summary>
    /// The TerraFluent attribution / branding label rendered in a corner of the chart.
    /// The label text and link are <b>fixed</b> (set by the library, not the caller) so the output
    /// cannot be re-branded; callers may only show/hide it and choose its corner via
    /// <c>ShowCredits()</c> / <c>HideCredits()</c>. The label always renders as text; the hyperlink is
    /// clickable only when the SVG is embedded inline (not when rasterised to PNG/PDF or via <c>&lt;img&gt;</c>).
    /// </summary>
    public class CreditsOptions
    {
        /// <summary>The fixed brand label text.</summary>
        public const string BrandText = "terrafluent.dev";

        /// <summary>The fixed brand link (https).</summary>
        public const string BrandHref = "https://terrafluent.dev";

        /// <summary>When <c>true</c> (default), the credit label is drawn. Remove it via <c>HideCredits()</c>.</summary>
        public bool Enabled { get; set; } = true;

        /// <summary>Fixed brand label text — not configurable by callers.</summary>
        public string Text => BrandText;

        /// <summary>Fixed brand link — not configurable by callers.</summary>
        public string Href => BrandHref;

        /// <summary>Corner placement. Default <see cref="CreditsPosition.BottomRight"/>.</summary>
        public CreditsPosition Position { get; set; } = CreditsPosition.BottomRight;

        /// <summary>Text colour; <c>null</c> falls back to the theme text colour.</summary>
        public string? Color { get; set; }

        /// <summary>Font size in pixels. Default <c>9</c>.</summary>
        public double FontSize { get; set; } = 9;

        /// <summary>Text opacity (0–1). Default <c>0.6</c> for an unobtrusive look.</summary>
        public double Opacity { get; set; } = 0.6;

        /// <summary>Returns a deep copy of these options. Text/Href are fixed constants.</summary>
        public CreditsOptions Clone() => new CreditsOptions
        {
            Enabled  = this.Enabled,
            Position = this.Position,
            Color    = this.Color,
            FontSize = this.FontSize,
            Opacity  = this.Opacity
        };
    }
}
