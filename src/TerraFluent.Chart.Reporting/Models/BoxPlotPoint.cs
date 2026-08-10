namespace TerraFluent.Chart.Reporting.Models
{
    /// <summary>
    /// A single data point for a <see cref="Enums.ChartType.BoxPlot"/> series, describing the
    /// five-number summary of a distribution: minimum, lower quartile, median, upper quartile,
    /// and maximum.
    /// </summary>
    public struct BoxPlotPoint
    {
        /// <summary>The lowest value (bottom whisker end).</summary>
        public double Low { get; set; }
        /// <summary>The lower quartile (Q1 — bottom of the box).</summary>
        public double Q1 { get; set; }
        /// <summary>The median (Q2 — line drawn across the box).</summary>
        public double Median { get; set; }
        /// <summary>The upper quartile (Q3 — top of the box).</summary>
        public double Q3 { get; set; }
        /// <summary>The highest value (top whisker end).</summary>
        public double High { get; set; }

        /// <summary>Initialises a box-plot point from its five-number summary.</summary>
        public BoxPlotPoint(double low, double q1, double median, double q3, double high)
        {
            Low = low;
            Q1 = q1;
            Median = median;
            Q3 = q3;
            High = high;
        }
    }
}
