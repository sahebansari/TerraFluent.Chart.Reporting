namespace TerraFluent.Chart.Reporting.Builder
{
    /// <summary>
    /// A composable configuration preset that applies opinionated chart settings to a
    /// <see cref="ChartBuilder"/> via <see cref="ChartBuilder.ApplyTemplate"/>.
    /// Implement this interface to create reusable chart styles.
    /// </summary>
    public interface IChartTemplate
    {
        /// <summary>Applies this template's settings to <paramref name="builder"/>.</summary>
        void Apply(ChartBuilder builder);
    }
}
