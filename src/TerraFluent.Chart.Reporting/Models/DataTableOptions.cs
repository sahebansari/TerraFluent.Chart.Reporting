namespace TerraFluent.Chart.Reporting.Models
{
    /// <summary>Configuration for the optional data table rendered below a chart.</summary>
    public class DataTableOptions
    {
        /// <summary>When <see langword="true"/>, a tabular summary of series values is appended below the chart.</summary>
        public bool Visible { get; set; }

        /// <summary>Font size in pixels for all table cells. Default <c>10</c>.</summary>
        public int FontSize { get; set; } = 10;

        /// <summary>Row height in pixels. Default <c>20</c>.</summary>
        public int RowHeight { get; set; } = 20;

        internal DataTableOptions Clone() => new DataTableOptions
        {
            Visible   = this.Visible,
            FontSize  = this.FontSize,
            RowHeight = this.RowHeight
        };
    }
}
