namespace TerraFluent.Chart.Reporting.Models
{
    /// <summary>
    /// A horizontal reference line drawn at a specific Y value across the plot area.
    /// Add to <see cref="Axis.PlotLines"/>.
    /// </summary>
    public class PlotLine
    {
        /// <summary>Y value at which the line is drawn.</summary>
        public double Value { get; set; }

        /// <summary>Line stroke colour. Defaults to red.</summary>
        public string Color { get; set; } = ChartColor.Red;

        /// <summary>Stroke width in pixels.</summary>
        public int Width { get; set; } = 1;

        /// <summary>Optional dash style (e.g. "Dash", "Dot", "LongDash").</summary>
        public string? DashStyle { get; set; }

        /// <summary>Optional label shown at the right edge of the line.</summary>
        public string? Label { get; set; }
    }
}
