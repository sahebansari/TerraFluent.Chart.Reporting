namespace TerraFluent.Chart.Reporting.Models
{
    /// <summary>
    /// A directed flow link between two <see cref="SankeyNode"/> entries in a
    /// <see cref="Enums.ChartType.Sankey"/> series.
    /// The link is rendered as a proportionally sized cubic-bezier curve.
    /// </summary>
    public class SankeyLink
    {
        /// <summary>Zero-based index of the source <see cref="SankeyNode"/>.</summary>
        public int From { get; set; }

        /// <summary>Zero-based index of the target <see cref="SankeyNode"/>.</summary>
        public int To { get; set; }

        /// <summary>Quantity flowing from <see cref="From"/> to <see cref="To"/>. Drives link thickness.</summary>
        public double Value { get; set; }

        /// <summary>Optional stroke colour override for this link. Falls back to the source node colour.</summary>
        public string? Color { get; set; }
    }
}
