namespace TerraFluent.Chart.Reporting.Models
{
    /// <summary>
    /// Tooltip configuration. Controls the hover popup shown on data points.
    /// Only active in <see cref="Enums.SvgMode.Animated"/> and <see cref="Enums.SvgMode.Interactive"/> render modes.
    /// </summary>
    public class TooltipOptions
    {
        // ---- visibility ----
        /// <summary>Shows or hides tooltips. Default <c>true</c>.</summary>
        public bool Enabled { get; set; } = true;

        // ---- background ----
        /// <summary>
        /// Fill colour of the tooltip box.
        /// When <c>null</c> (default) the colour is derived from the active chart theme
        /// via <see cref="Models.ChartTheme.TooltipBackground"/>.
        /// </summary>
        public string? BackgroundColor { get; set; } = null;

        // ---- border ----
        /// <summary>Border stroke colour. No border when null or <see cref="BorderWidth"/> is 0.</summary>
        public string? BorderColor { get; set; }
        /// <summary>Border stroke width (px). Default 0 = no border.</summary>
        public int BorderWidth { get; set; } = 0;
        /// <summary>Corner radius (px) of the tooltip rectangle. Default 4.</summary>
        public int BorderRadius { get; set; } = 4;

        // ---- text ----
        /// <summary>
        /// Text colour inside the tooltip.
        /// When <c>null</c> (default) the colour is derived from the active chart theme
        /// via <see cref="Models.ChartTheme.TooltipTextColor"/>.
        /// </summary>
        public string? TextColor { get; set; } = null;
        /// <summary>Font size (px) of the tooltip text. Default 13.</summary>
        public int FontSize { get; set; } = 13;
        /// <summary>Font family for tooltip text. Null inherits the chart theme font.</summary>
        public string? FontFamily { get; set; }

        // ---- layout ----
        /// <summary>Horizontal inner padding (px) between the text and the box edges. Default 10.</summary>
        public int Padding { get; set; } = 10;
        /// <summary>Shows or hides the downward-pointing arrow beneath the box. Default <c>true</c>.</summary>
        public bool ShowArrow { get; set; } = true;

        // ---- content ----
        /// <summary>
        /// Format template for the tooltip text.
        /// <c>{label}</c> is replaced by the series/slice name; <c>{value}</c> by the formatted numeric value.
        /// Default <c>null</c> produces <c>"{label}: {value}"</c>.
        /// </summary>
        public string? Format { get; set; }

        /// <summary>
        /// Optional header line shown above the data row.
        /// Use <c>{label}</c> for the series name and <c>{value}</c> for the point value.
        /// When <c>null</c> no header row is rendered.
        /// </summary>
        public string? HeaderFormat { get; set; }

        /// <summary>
        /// Format template for each data-point row when <see cref="HeaderFormat"/> is also set.
        /// Use <c>{label}</c> and <c>{value}</c> as placeholders.
        /// Falls back to <see cref="Format"/> when <c>null</c>.
        /// </summary>
        public string? PointFormat { get; set; }

        // ---- animation ----
        /// <summary>CSS transition duration (seconds) for the fade-in/out. Default 0.15.</summary>
        public double TransitionDuration { get; set; } = 0.15;

        // ---- shadow ----
        /// <summary>
        /// Renders a <c>drop-shadow</c> filter behind the tooltip box.
        /// Default <c>true</c>. Only active in Animated and Interactive render modes.
        /// </summary>
        public bool Shadow { get; set; } = true;

        // ---- crosshair ----
        /// <summary>
        /// Shows a vertical crosshair guide line from the hovered data point to the x-axis.
        /// Default <c>true</c>. Only active in Animated and Interactive render modes.
        /// </summary>
        public bool Crosshair { get; set; } = true;
        /// <summary>Stroke colour of the crosshair line. Default light blue-grey.</summary>
        public string CrosshairColor { get; set; } = "rgba(204,214,235,0.50)";
        /// <summary>Stroke width (px) of the crosshair line. Default 1.</summary>
        public int CrosshairWidth { get; set; } = 1;

        // ---- shared & follow-pointer ----
        /// <summary>
        /// When <c>true</c> and <see cref="Enums.SvgMode.Interactive"/>, a single tooltip box
        /// shows all series values at the same x-index (shared-tooltip style).
        /// Default <c>false</c>.
        /// </summary>
        public bool Shared { get; set; } = false;

        /// <summary>
        /// When <c>true</c> and <see cref="Enums.SvgMode.Interactive"/>, the tooltip floats
        /// near the mouse cursor rather than snapping to the data point position.
        /// Default <c>false</c>.
        /// </summary>
        public bool FollowPointer { get; set; } = false;

        // ---- value formatting ----
        /// <summary>String prepended to the formatted numeric value inside the tooltip. E.g. <c>"$"</c>.</summary>
        public string? ValuePrefix { get; set; }
        /// <summary>String appended to the formatted numeric value inside the tooltip. E.g. <c>" km"</c>.</summary>
        public string? ValueSuffix { get; set; }
        /// <summary>
        /// Fixed number of decimal places for the tooltip value.
        /// When <c>null</c> (default) the library auto-formats with up to two significant decimals.
        /// </summary>
        public int? ValueDecimals { get; set; }

        /// <summary>Returns a deep copy of this <see cref="TooltipOptions"/>.</summary>
        public TooltipOptions Clone() => new TooltipOptions
        {
            Enabled            = this.Enabled,
            BackgroundColor    = this.BackgroundColor,   // null preserves "use theme default"
            BorderColor        = this.BorderColor,
            BorderWidth        = this.BorderWidth,
            BorderRadius       = this.BorderRadius,
            TextColor          = this.TextColor,         // null preserves "use theme default"
            FontSize           = this.FontSize,
            FontFamily         = this.FontFamily,
            Padding            = this.Padding,
            ShowArrow          = this.ShowArrow,
            Format             = this.Format,
            HeaderFormat       = this.HeaderFormat,
            PointFormat        = this.PointFormat,
            TransitionDuration = this.TransitionDuration,
            Shadow             = this.Shadow,
            Crosshair          = this.Crosshair,
            CrosshairColor     = this.CrosshairColor,
            CrosshairWidth     = this.CrosshairWidth,
            Shared             = this.Shared,
            FollowPointer      = this.FollowPointer,
            ValuePrefix        = this.ValuePrefix,
            ValueSuffix        = this.ValueSuffix,
            ValueDecimals      = this.ValueDecimals,
        };
    }
}
