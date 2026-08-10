using System.Collections.Generic;

namespace TerraFluent.Chart.Reporting.Models
{
    /// <summary>
    /// Configuration for a chart axis. Mirrors HighCharts <c>xAxis</c> / <c>yAxis</c>.
    /// </summary>
    public class Axis
    {
        /// <summary>Axis title label displayed alongside the axis line. <c>null</c> = no title.</summary>
        public string? Title { get; set; }
        /// <summary>
        /// Scale type for the axis. <see cref="Enums.AxisType.Linear"/> (default) spaces ticks evenly;
        /// <see cref="Enums.AxisType.Logarithmic"/> spaces them by base-10 logarithm.
        /// </summary>
        public Enums.AxisType Type { get; set; } = Enums.AxisType.Linear;
        /// <summary>Ordered list of string labels for each category tick on the axis.</summary>
        public List<string> Categories { get; set; } = new List<string>();
        /// <summary>
        /// Source date/time values for a <see cref="Enums.AxisType.DateTime"/> X axis. The builder
        /// formats these into <see cref="Categories"/>; kept for reference and future use.
        /// </summary>
        public List<System.DateTime> DateTimeValues { get; set; } = new List<System.DateTime>();
        /// <summary>
        /// Explicit .NET date format string (e.g. <c>"MMM yyyy"</c>) for a
        /// <see cref="Enums.AxisType.DateTime"/> axis. <c>null</c> = auto-selected from the data span.
        /// </summary>
        public string? DateTimeLabelFormat { get; set; }
        /// <summary>Minimum value of the axis scale. <c>null</c> = auto-computed from data.</summary>
        public double? Min { get; set; }
        /// <summary>Maximum value of the axis scale. <c>null</c> = auto-computed from data.</summary>
        public double? Max { get; set; }
        /// <summary>Whether to render the axis line and labels. Default <c>true</c>.</summary>
        public bool Visible { get; set; } = true;
        /// <summary>Whether to draw horizontal/vertical grid lines across the plot area. Default <c>true</c>.</summary>
        public bool GridLineVisible { get; set; } = true;
        /// <summary>
        /// Stroke colour for grid lines. <c>null</c> (default) defers to the active
        /// <see cref="ChartTheme.GridLineColor"/>; an explicit value overrides the theme.
        /// </summary>
        public string? GridLineColor { get; set; }
        /// <summary>Number format string for tick labels, e.g. <c>"{value}%"</c>. <c>null</c> = auto.</summary>
        public string? LabelFormat { get; set; }
        /// <summary>
        /// Spacing between axis ticks in data units (e.g. <c>5.0</c> for every 5 units).
        /// <c>null</c> = auto-computed from the data range. Replaces the former <c>string</c> overload;
        /// the renderer uses this value directly without parsing.
        /// </summary>
        public double? TickInterval { get; set; }

        /// <summary>Horizontal reference bands drawn across the plot area.</summary>
        public List<PlotBand> PlotBands { get; set; } = new List<PlotBand>();

        /// <summary>Horizontal reference lines drawn at specific Y values.</summary>
        public List<PlotLine> PlotLines { get; set; } = new List<PlotLine>();

        /// <summary>
        /// Rotation angle in degrees for X-axis category labels.
        /// Negative = counter-clockwise (e.g. -45 = diagonal, -90 = vertical).
        /// Default <c>null</c> = auto-detect: rotates by -45 when labels would overlap.
        /// </summary>
        public int? LabelRotation { get; set; }

        /// <summary>
        /// When <c>true</c>, the Y-axis direction is flipped: the minimum value is displayed at
        /// the top and the maximum at the bottom. Useful for ranking charts (rank 1 at top),
        /// depth charts, or any domain where smaller values are "higher".
        /// Default <c>false</c> (normal ascending bottom-to-top direction).
        /// </summary>
        public bool Inverted { get; set; }
    }
}
