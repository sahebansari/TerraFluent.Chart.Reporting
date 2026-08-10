namespace TerraFluent.Chart.Reporting.Models
{
    /// <summary>
    /// Fluent configuration for the label displayed in the center of a donut chart hole.
    /// Obtain via <c>series.DonutCenter</c> and chain methods:
    /// <code>cfg.DonutCenter.Show().Title("Total").Color("#1a202c")</code>
    /// Only rendered when <see cref="Series.DonutHolePercent"/> is greater than 0.
    /// </summary>
    public sealed class DonutCenterOptions
    {
        // ---- storage (readable by renderer) ----

        /// <summary>Whether the center label is rendered.</summary>
        public bool Enabled { get; set; } = false;

        /// <summary>
        /// Small caption rendered <em>above</em> the main value (e.g. "Total", "Revenue").
        /// Omitted when null or empty.
        /// </summary>
        public string? CenterTitle { get; set; }

        /// <summary>
        /// Custom string to display as the main value.
        /// When null the sum of all positive slice values is computed and formatted automatically.
        /// </summary>
        public string? CustomText { get; set; }

        /// <summary>Font size (px) for the main value. 0 = auto-scale based on hole radius.</summary>
        public int ValueFontSize { get; set; } = 0;

        /// <summary>Font size (px) for the title caption. 0 = auto-scale.</summary>
        public int CaptionFontSize { get; set; } = 0;

        /// <summary>Text colour of the main value. Null = chart theme text colour.</summary>
        public string? TextColor { get; set; }

        /// <summary>Text colour of the caption line. Null = chart theme text colour at reduced opacity.</summary>
        public string? TitleTextColor { get; set; }

        // ---- fluent API ----

        /// <summary>Enables the center label; auto-computes the total when no custom text is set.</summary>
        public DonutCenterOptions Show() { Enabled = true; return this; }

        /// <summary>Enables the center label and sets a custom value string.</summary>
        public DonutCenterOptions Show(string customText) { Enabled = true; CustomText = customText; return this; }

        /// <summary>Disables the center label.</summary>
        public DonutCenterOptions Hide() { Enabled = false; return this; }

        /// <summary>Sets the small caption rendered above the main value.</summary>
        public DonutCenterOptions Title(string title) { CenterTitle = title; return this; }

        /// <summary>Overrides the auto-computed total with a custom string.</summary>
        public DonutCenterOptions Text(string text) { CustomText = text; return this; }

        /// <summary>Sets the font size (px) for the main value. Pass 0 for auto-sizing.</summary>
        public DonutCenterOptions FontSize(int px) { ValueFontSize = px; return this; }

        /// <summary>Sets the font size (px) for the title caption. Pass 0 for auto-sizing.</summary>
        public DonutCenterOptions TitleFontSize(int px) { CaptionFontSize = px; return this; }

        /// <summary>Sets the text colour of the main value.</summary>
        public DonutCenterOptions Color(string color) { TextColor = color; return this; }

        /// <summary>Sets the text colour of the title caption.</summary>
        public DonutCenterOptions TitleColor(string color) { TitleTextColor = color; return this; }

        /// <summary>Returns a deep copy of this instance.</summary>
        public DonutCenterOptions Clone() => new DonutCenterOptions
        {
            Enabled        = this.Enabled,
            CenterTitle    = this.CenterTitle,
            CustomText     = this.CustomText,
            ValueFontSize  = this.ValueFontSize,
            CaptionFontSize = this.CaptionFontSize,
            TextColor      = this.TextColor,
            TitleTextColor = this.TitleTextColor,
        };
    }
}
