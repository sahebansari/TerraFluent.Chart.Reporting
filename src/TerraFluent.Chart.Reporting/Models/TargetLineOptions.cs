namespace TerraFluent.Chart.Reporting.Models
{
    /// <summary>
    /// A horizontal reference line pinned to a specific Y value and scoped to one series.
    /// Unlike <see cref="PlotLine"/> (which spans the whole chart), a target line is associated
    /// with a single series so it can represent a per-series goal, budget, or threshold.
    /// Add via <c>SeriesBuilder.TargetLine()</c>.
    /// </summary>
    public class TargetLineOptions
    {
        /// <summary>Y value at which the target line is drawn.</summary>
        public double Value { get; set; }

        /// <summary>Optional label shown at the right end of the line.</summary>
        public string? Label { get; set; }

        /// <summary>Stroke colour. Defaults to the series colour when <c>null</c> at render time.</summary>
        public string? Color { get; set; }

        /// <summary>Stroke width in pixels. Default <c>1</c>.</summary>
        public int LineWidth { get; set; } = 1;

        /// <summary>HighCharts-style dash style string (e.g. <c>"Dash"</c>, <c>"Dot"</c>). <c>null</c> = solid.</summary>
        public string? DashStyle { get; set; }
    }
}
