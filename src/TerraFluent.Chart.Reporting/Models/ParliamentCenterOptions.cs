namespace TerraFluent.Chart.Reporting.Models
{
    /// <summary>
    /// Fluent configuration for the label displayed in the centre of a Parliament (hemicycle) chart.
    /// Obtain via the series builder's <c>CenterLabel</c> method or <c>ParliamentCenter</c> property:
    /// <code>cfg.CenterLabel(c => c.Text("300").Subtitle("members").Color("#1a202c"))</code>
    /// Rendered by default showing the auto-computed seat total as Line 1 and "seats" as Line 2.
    /// </summary>
    public sealed class ParliamentCenterOptions
    {
        // ---- storage (readable by renderer) ----

        /// <summary>Whether the centre label is rendered. Default <c>true</c>.</summary>
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// Main (top) line of the centre label.
        /// When <c>null</c> the total seat count is computed automatically.
        /// </summary>
        public string? Line1 { get; set; }

        /// <summary>
        /// Secondary (bottom) line of the centre label.
        /// When <c>null</c> (default) "seats" is shown.
        /// Set to an empty string to suppress the subtitle entirely.
        /// </summary>
        public string? Line2 { get; set; }

        /// <summary>Font size (px) for Line 1. 0 = automatic (14 px).</summary>
        public int Line1FontSize { get; set; } = 0;

        /// <summary>Font size (px) for Line 2. 0 = automatic (11 px).</summary>
        public int Line2FontSize { get; set; } = 0;

        /// <summary>Text colour for both lines. <c>null</c> = chart theme text colour.</summary>
        public string? TextColor { get; set; }

        // ---- fluent API ----

        /// <summary>Shows the centre label (re-enables it if previously hidden).</summary>
        public ParliamentCenterOptions Show() { Enabled = true; return this; }

        /// <summary>Hides the centre label.</summary>
        public ParliamentCenterOptions Hide() { Enabled = false; return this; }

        /// <summary>Overrides the main line text. Pass <c>null</c> to restore the auto seat count.</summary>
        public ParliamentCenterOptions Text(string? text) { Line1 = text; return this; }

        /// <summary>Sets the subtitle (Line 2). Pass an empty string to suppress it.</summary>
        public ParliamentCenterOptions Subtitle(string text) { Line2 = text; return this; }

        /// <summary>Removes the subtitle line entirely.</summary>
        public ParliamentCenterOptions NoSubtitle() { Line2 = string.Empty; return this; }

        /// <summary>Sets the font size (px) for the main line. Pass 0 for automatic sizing.</summary>
        public ParliamentCenterOptions FontSize(int px) { Line1FontSize = px; return this; }

        /// <summary>Sets the font size (px) for the subtitle line. Pass 0 for automatic sizing.</summary>
        public ParliamentCenterOptions SubtitleFontSize(int px) { Line2FontSize = px; return this; }

        /// <summary>Sets the text colour for both lines.</summary>
        public ParliamentCenterOptions Color(string color) { TextColor = color; return this; }

        internal ParliamentCenterOptions Clone() => new ParliamentCenterOptions
        {
            Enabled       = Enabled,
            Line1         = Line1,
            Line2         = Line2,
            Line1FontSize = Line1FontSize,
            Line2FontSize = Line2FontSize,
            TextColor     = TextColor,
        };
    }
}
