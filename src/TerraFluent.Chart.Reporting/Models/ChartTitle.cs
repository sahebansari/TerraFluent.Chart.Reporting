namespace TerraFluent.Chart.Reporting.Models
{
    /// <summary>
    /// Chart title and subtitle configuration.
    /// </summary>
    public class ChartTitle
    {
        /// <summary>The text content of the title. <c>null</c> or empty = not rendered.</summary>
        public string? Text { get; set; }
        /// <summary>Horizontal alignment: <c>"left"</c>, <c>"center"</c> (default), or <c>"right"</c>.</summary>
        public string Align { get; set; } = "center";
        /// <summary>Inline CSS style string applied to the title <c>&lt;text&gt;</c> element. <c>null</c> = use theme default.</summary>
        public string? Style { get; set; }
    }
}
