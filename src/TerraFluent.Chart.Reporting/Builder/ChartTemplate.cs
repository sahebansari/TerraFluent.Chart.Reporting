using TerraFluent.Chart.Reporting.Models;

namespace TerraFluent.Chart.Reporting.Builder
{
    /// <summary>
    /// Built-in <see cref="IChartTemplate"/> presets. Pass to
    /// <see cref="ChartBuilder.ApplyTemplate"/> to apply in one call.
    /// </summary>
    public static class ChartTemplate
    {
        private sealed class ActionTemplate : IChartTemplate
        {
            private readonly System.Action<ChartBuilder> _apply;
            internal ActionTemplate(System.Action<ChartBuilder> apply) => _apply = apply;
            public void Apply(ChartBuilder b) => _apply(b);
        }

        /// <summary>
        /// Column chart, default palette, N0-formatted Y-axis (e.g. 1,200).
        /// Suitable for quarterly or annual revenue charts.
        /// </summary>
        public static readonly IChartTemplate Revenue = new ActionTemplate(b =>
            b.AsColumn()
             .Theme(ChartTheme.Default)
             .YAxis(y => y.LabelFormat = "{value:N0}")
             .Legend(l => l.AtBottom()));

        /// <summary>
        /// Dark-themed interactive column chart with no grid lines and no legend.
        /// Optimised for KPI tiles rendered in a browser dashboard.
        /// </summary>
        public static readonly IChartTemplate KpiDashboard = new ActionTemplate(b =>
            b.AsColumn()
             .Theme(ChartTheme.Dark)
             .AsInteractive()
             .YAxis(y => y.GridLineVisible = false)
             .HideLegend());

        /// <summary>
        /// Smooth spline line chart with a 1-second animated entry.
        /// Suitable for time-series and trend data.
        /// </summary>
        public static readonly IChartTemplate TimeSeries = new ActionTemplate(b =>
            b.AsSpline()
             .Theme(ChartTheme.Default)
             .AsAnimated()
             .Animate(1000)
             .Legend(l => l.AtBottom()));

        /// <summary>
        /// Pastel-themed horizontal bar chart in static (PDF-safe) render mode.
        /// Suitable for executive summary reports distributed as PDF or email.
        /// </summary>
        public static readonly IChartTemplate ExecutiveSummary = new ActionTemplate(b =>
            b.AsBar()
             .Theme(ChartTheme.Pastel)
             .AsStatic()
             .Legend(l => l.AtBottom()));

        /// <summary>
        /// WCAG AA-compliant high-contrast theme in static render mode.
        /// Suitable for accessibility reports and greyscale print output.
        /// </summary>
        public static readonly IChartTemplate Accessible = new ActionTemplate(b =>
            b.Theme(ChartTheme.HighContrast)
             .AsStatic());
    }
}
