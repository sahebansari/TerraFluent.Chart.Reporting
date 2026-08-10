namespace TerraFluent.Chart.Reporting.Models
{
    /// <summary>
    /// A single task entry for a <see cref="Enums.ChartType.Gantt"/> series.
    /// Each task renders as a horizontal bar spanning from <see cref="Start"/> to <see cref="End"/>
    /// on the X (time) axis.
    /// </summary>
    public class GanttTask
    {
        /// <summary>Display name shown on the left axis for this task row.</summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>Start position on the numeric X (time) axis.</summary>
        public double Start { get; set; }

        /// <summary>End position on the numeric X (time) axis.</summary>
        public double End { get; set; }

        /// <summary>Optional bar colour override. Falls back to the series colour when <c>null</c>.</summary>
        public string? Color { get; set; }

        /// <summary>Optional text rendered inside the bar when the bar is wide enough.</summary>
        public string? Label { get; set; }
    }
}
