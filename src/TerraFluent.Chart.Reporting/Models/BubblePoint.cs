namespace TerraFluent.Chart.Reporting.Models
{
    /// <summary>
    /// A single data point for a <see cref="Enums.ChartType.Bubble"/> series.
    /// The Z value drives the rendered bubble radius (largest Z = largest circle).
    /// </summary>
    public struct BubblePoint
    {
        /// <summary>Horizontal position on the X axis.</summary>
        public double X { get; set; }
        /// <summary>Vertical position on the Y axis.</summary>
        public double Y { get; set; }
        /// <summary>Bubble size — larger values render as larger circles.</summary>
        public double Z { get; set; }

        /// <summary>Initialises a bubble point from its three components.</summary>
        public BubblePoint(double x, double y, double z) { X = x; Y = y; Z = z; }
    }
}
