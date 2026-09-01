namespace TerraFluent.Chart.Reporting.Models
{
    /// <summary>
    /// Fluent configuration object for per-series data labels.
    /// Obtain via <c>series.DataLabel</c> and chain methods:
    /// <code>cfg.DataLabel.Show().Format("{value}%").Color("#fff").FontSize(11).Background("#7cb5ec")</code>
    /// </summary>
    public sealed class DataLabelOptions
    {
        // ------------------------------------------------------------------ storage (readable by renderer)

        /// <summary>Whether data labels are visible on this series.</summary>
        public bool    Enabled         { get; set; } = false;

        /// <summary>Format string, e.g. <c>"{value}%"</c> or <c>"${value}k"</c>.</summary>
        public string? FormatString    { get; set; }

        /// <summary>Text colour override. <c>null</c> = use the chart theme's <see cref="ChartTheme.TextColor"/>.</summary>
        public string? TextColor       { get; set; }

        /// <summary>Font size in pixels. <c>null</c> = use the CSS default (10 px).</summary>
        public int?    TextFontSize    { get; set; }

        /// <summary>Background fill colour for the label pill. <c>null</c> = transparent.</summary>
        public string? BackgroundColor { get; set; }

        /// <summary>
        /// For Pie/Donut series: radial distance as a fraction of the outer radius
        /// (0.7 = inside, 1.0 = edge, &gt;1.0 = outside with spline connector).
        /// </summary>
        public double? RadiusFraction  { get; set; }

        /// <summary>
        /// Vertical offset in pixels from the default label position.
        /// Positive values move the label <b>downward</b>; negative values move it upward.
        /// This follows the standard SVG/CSS convention (positive Y = down), consistent with
        /// <see cref="Legend.Y"/>.
        /// </summary>
        public double? VerticalOffset  { get; set; }

        // ------------------------------------------------------------------ fluent API

        /// <summary>Enables data labels for this series.</summary>
        public DataLabelOptions Show()                         { Enabled         = true;      return this; }

        /// <summary>Disables data labels for this series.</summary>
        public DataLabelOptions Hide()                         { Enabled         = false;     return this; }

        /// <summary>Sets the label format string, e.g. <c>"{value}%"</c>.</summary>
        public DataLabelOptions Format(string fmt)             { FormatString    = fmt;       return this; }

        /// <summary>Sets the label text colour.</summary>
        public DataLabelOptions Color(string color)            { TextColor       = color;     return this; }

        /// <summary>Sets the label font size in pixels.</summary>
        public DataLabelOptions FontSize(int px)               { TextFontSize    = px;        return this; }

        /// <summary>Sets the background fill of the label pill.</summary>
        public DataLabelOptions Background(string color)       { BackgroundColor = color;     return this; }

        /// <summary>
        /// For Pie/Donut: sets the radial distance as a fraction of the outer radius.
        /// Values greater than 1.0 place the label outside the slice and draw a spline connector.
        /// </summary>
        public DataLabelOptions Radius(double fraction)        { RadiusFraction  = fraction;  return this; }

        /// <summary>
        /// Shifts the label vertically by <paramref name="pixels"/> pixels from its default position.
        /// Positive values move the label <b>downward</b>; negative values move it upward
        /// (standard SVG/CSS convention, consistent with <see cref="Legend.Y"/>).
        /// </summary>
        public DataLabelOptions OffsetY(double pixels)         { VerticalOffset  = pixels;    return this; }

        // ------------------------------------------------------------------ internal

        internal DataLabelOptions Clone() => new DataLabelOptions
        {
            Enabled         = this.Enabled,
            FormatString    = this.FormatString,
            TextColor       = this.TextColor,
            TextFontSize    = this.TextFontSize,
            BackgroundColor = this.BackgroundColor,
            RadiusFraction  = this.RadiusFraction,
            VerticalOffset  = this.VerticalOffset
        };
    }
}
