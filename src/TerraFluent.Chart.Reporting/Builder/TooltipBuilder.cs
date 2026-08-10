using System;
using TerraFluent.Chart.Reporting.Models;

namespace TerraFluent.Chart.Reporting.Builder
{
    /// <summary>
    /// Fluent builder for configuring a chart tooltip.
    /// Obtain an instance via <see cref="ChartBuilder.Tooltip(Action{TooltipBuilder})"/>.
    /// </summary>
    public sealed class TooltipBuilder
    {
        private readonly TooltipOptions _tooltip;

        internal TooltipBuilder(TooltipOptions tooltip)
        {
            _tooltip = tooltip ?? throw new ArgumentNullException(nameof(tooltip));
        }

        // ------------------------------------------------------------------ visibility

        /// <summary>Disables tooltips entirely.</summary>
        public TooltipBuilder Disable() { _tooltip.Enabled = false; return this; }

        // ------------------------------------------------------------------ background

        /// <summary>Sets the tooltip box background colour.</summary>
        public TooltipBuilder BackgroundColor(string color)
        {
            if (string.IsNullOrWhiteSpace(color)) throw new ArgumentException("Color must not be empty.", nameof(color));
            _tooltip.BackgroundColor = color;
            return this;
        }

        // ------------------------------------------------------------------ border

        /// <summary>Sets all border properties at once.</summary>
        public TooltipBuilder Border(string color, int width = 1, int radius = 4)
        {
            if (string.IsNullOrWhiteSpace(color)) throw new ArgumentException("Color must not be empty.", nameof(color));
            if (width < 0) throw new ArgumentOutOfRangeException(nameof(width));
            if (radius < 0) throw new ArgumentOutOfRangeException(nameof(radius));
            _tooltip.BorderColor  = color;
            _tooltip.BorderWidth  = width;
            _tooltip.BorderRadius = radius;
            return this;
        }

        /// <summary>Sets the tooltip border colour.</summary>
        public TooltipBuilder BorderColor(string color)
        {
            if (string.IsNullOrWhiteSpace(color)) throw new ArgumentException("Color must not be empty.", nameof(color));
            _tooltip.BorderColor = color;
            return this;
        }

        /// <summary>Sets the tooltip border stroke-width (px).</summary>
        public TooltipBuilder BorderWidth(int width)
        {
            if (width < 0) throw new ArgumentOutOfRangeException(nameof(width));
            _tooltip.BorderWidth = width;
            return this;
        }

        /// <summary>Sets the corner radius (px) of the tooltip rectangle.</summary>
        public TooltipBuilder BorderRadius(int radius)
        {
            if (radius < 0) throw new ArgumentOutOfRangeException(nameof(radius));
            _tooltip.BorderRadius = radius;
            return this;
        }

        // ------------------------------------------------------------------ text

        /// <summary>Sets the tooltip text colour.</summary>
        public TooltipBuilder TextColor(string color)
        {
            if (string.IsNullOrWhiteSpace(color)) throw new ArgumentException("Color must not be empty.", nameof(color));
            _tooltip.TextColor = color;
            return this;
        }

        /// <summary>Sets the tooltip font size (px).</summary>
        public TooltipBuilder FontSize(int px)
        {
            if (px <= 0) throw new ArgumentOutOfRangeException(nameof(px), "Font size must be > 0.");
            _tooltip.FontSize = px;
            return this;
        }

        /// <summary>Sets the tooltip font family. Pass <c>null</c> to inherit the chart theme font.</summary>
        public TooltipBuilder FontFamily(string? family)
        {
            _tooltip.FontFamily = family;
            return this;
        }

        // ------------------------------------------------------------------ layout

        /// <summary>Sets the horizontal inner padding (px) between the tooltip text and box edges.</summary>
        public TooltipBuilder Padding(int px)
        {
            if (px < 0) throw new ArgumentOutOfRangeException(nameof(px), "Padding must be >= 0.");
            _tooltip.Padding = px;
            return this;
        }

        /// <summary>Hides the arrow/notch beneath the tooltip box.</summary>
        public TooltipBuilder HideArrow() { _tooltip.ShowArrow = false; return this; }

        // ------------------------------------------------------------------ content

        /// <summary>
        /// Sets the tooltip content format template.
        /// Use <c>{label}</c> for the series/slice name and <c>{value}</c> for the numeric value.
        /// Example: <c>"{label}: ${value}k"</c>
        /// </summary>
        public TooltipBuilder Format(string format)
        {
            if (string.IsNullOrWhiteSpace(format)) throw new ArgumentException("Format must not be empty.", nameof(format));
            _tooltip.Format = format;
            return this;
        }

        /// <summary>
        /// Sets an optional header line rendered above the data row in the tooltip.
        /// Use <c>{label}</c> for the series name and <c>{value}</c> for the point value.
        /// </summary>
        public TooltipBuilder HeaderFormat(string format)
        {
            if (string.IsNullOrWhiteSpace(format)) throw new ArgumentException("Format must not be empty.", nameof(format));
            _tooltip.HeaderFormat = format;
            return this;
        }

        /// <summary>
        /// Sets the per-point row format when <see cref="HeaderFormat"/> is also configured.
        /// Use <c>{label}</c> and <c>{value}</c> as placeholders.
        /// </summary>
        public TooltipBuilder PointFormat(string format)
        {
            if (string.IsNullOrWhiteSpace(format)) throw new ArgumentException("Format must not be empty.", nameof(format));
            _tooltip.PointFormat = format;
            return this;
        }

        // ------------------------------------------------------------------ animation

        /// <summary>Sets the CSS transition duration (seconds) for tooltip fade-in/out. Default 0.15.</summary>
        public TooltipBuilder TransitionDuration(double seconds)
        {
            if (seconds < 0) throw new ArgumentOutOfRangeException(nameof(seconds));
            _tooltip.TransitionDuration = seconds;
            return this;
        }

        // ------------------------------------------------------------------ shadow

        /// <summary>Disables the drop-shadow behind the tooltip box.</summary>
        public TooltipBuilder NoShadow() { _tooltip.Shadow = false; return this; }

        // ------------------------------------------------------------------ crosshair

        /// <summary>Disables the vertical crosshair guide line.</summary>
        public TooltipBuilder NoCrosshair() { _tooltip.Crosshair = false; return this; }

        /// <summary>
        /// Sets all crosshair visual properties at once.
        /// </summary>
        /// <param name="color">Stroke colour of the crosshair line.</param>
        /// <param name="width">Stroke width (px). Default 1.</param>
        public TooltipBuilder CrosshairStyle(string color, int width = 1)
        {
            if (string.IsNullOrWhiteSpace(color)) throw new ArgumentException("Color must not be empty.", nameof(color));
            if (width <= 0) throw new ArgumentOutOfRangeException(nameof(width));
            _tooltip.Crosshair      = true;
            _tooltip.CrosshairColor = color;
            _tooltip.CrosshairWidth = width;
            return this;
        }

        // ------------------------------------------------------------------ shared & follow-pointer

        /// <summary>
        /// Enables shared tooltip mode: in <see cref="Enums.SvgMode.Interactive"/> render mode,
        /// a single tooltip box lists all series values at the same x-index.
        /// </summary>
        public TooltipBuilder EnableShared() { _tooltip.Shared = true; return this; }

        /// <summary>
        /// Enables follow-pointer mode: in <see cref="Enums.SvgMode.Interactive"/> render mode,
        /// the tooltip floats near the mouse cursor instead of snapping to the data point.
        /// </summary>
        public TooltipBuilder EnableFollowPointer() { _tooltip.FollowPointer = true; return this; }

        // ------------------------------------------------------------------ value formatting

        /// <summary>Sets a string to prepend to every formatted tooltip value. E.g. <c>"$"</c>.</summary>
        public TooltipBuilder ValuePrefix(string prefix)
        {
            _tooltip.ValuePrefix = prefix ?? string.Empty;
            return this;
        }

        /// <summary>Sets a string to append to every formatted tooltip value. E.g. <c>" km"</c>.</summary>
        public TooltipBuilder ValueSuffix(string suffix)
        {
            _tooltip.ValueSuffix = suffix ?? string.Empty;
            return this;
        }

        /// <summary>
        /// Fixes the number of decimal places shown in tooltip values.
        /// Pass <c>0</c> for integers; omit (default) for auto-formatting.
        /// </summary>
        public TooltipBuilder ValueDecimals(int decimals)
        {
            if (decimals < 0) throw new ArgumentOutOfRangeException(nameof(decimals));
            _tooltip.ValueDecimals = decimals;
            return this;
        }
    }
}
