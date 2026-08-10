namespace TerraFluent.Chart.Reporting.Models
{
    /// <summary>
    /// A named node (stage or entity) in a <see cref="Enums.ChartType.Sankey"/> series.
    /// Nodes are rendered as tall rectangles; links connect them with proportionally sized curves.
    /// </summary>
    public class SankeyNode
    {
        /// <summary>Display name rendered beside the node rectangle.</summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>Optional fill colour override. Falls back to the theme palette when <c>null</c>.</summary>
        public string? Color { get; set; }
    }
}
