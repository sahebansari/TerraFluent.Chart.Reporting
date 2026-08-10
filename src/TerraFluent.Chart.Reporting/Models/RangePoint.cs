namespace TerraFluent.Chart.Reporting.Models
{
    /// <summary>
    /// A single data point for <see cref="Enums.ChartType.ColumnRange"/> and
    /// <see cref="Enums.ChartType.AreaRange"/> series.
    /// Each point carries a (Low, High) pair that defines the extent of one bar or band segment.
    /// </summary>
    public struct RangePoint
    {
        /// <summary>Lower bound of the range (bottom of the column or lower boundary of the area band).</summary>
        public double Low { get; set; }
        /// <summary>Upper bound of the range (top of the column or upper boundary of the area band).</summary>
        public double High { get; set; }

        /// <summary>Initialises a range point from its low and high values.</summary>
        public RangePoint(double low, double high) { Low = low; High = high; }
    }
}
