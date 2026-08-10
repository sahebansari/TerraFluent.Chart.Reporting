namespace TerraFluent.Chart.Reporting.Models
{
    /// <summary>
    /// A single cell in a <see cref="Enums.ChartType.Heatmap"/> series.
    /// The (Col, Row) pair addresses a grid cell; Value drives the fill colour.
    /// </summary>
    public struct HeatmapPoint
    {
        /// <summary>Zero-based column index — maps to <c>XAxis.Categories[Col]</c> when available.</summary>
        public int Col { get; set; }
        /// <summary>Zero-based row index — maps to <c>Series.HeatmapRowLabels[Row]</c> when available.</summary>
        public int Row { get; set; }
        /// <summary>Cell value — the colour scale is derived from the min and max values across all cells.</summary>
        public double Value { get; set; }

        /// <summary>Initialises a heatmap cell from its column, row and value.</summary>
        public HeatmapPoint(int col, int row, double value) { Col = col; Row = row; Value = value; }
    }
}
