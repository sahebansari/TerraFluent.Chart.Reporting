namespace TerraFluent.Chart.Reporting.Rendering
{
    /// <summary>
    /// Contract for SVG chart renderers.
    /// </summary>
    public interface ISvgRenderer
    {
        /// <summary>
        /// Renders the chart options to an SVG string.
        /// </summary>
        string Render(Models.ChartOptions options);
    }
}
