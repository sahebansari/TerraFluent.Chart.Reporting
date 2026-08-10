namespace TerraFluent.Chart.Reporting.Models
{
    /// <summary>
    /// A single data-quality finding produced by
    /// <see cref="TerraFluent.Chart.Reporting.Analysis.DataQualityAnalyzer"/>.
    /// </summary>
    public sealed class DataQualityWarning
    {
        /// <summary>How severe the issue is.</summary>
        public Enums.WarningSeverity Severity { get; }

        /// <summary>
        /// Short machine-readable tag that identifies the rule that fired, e.g.
        /// <c>"MissingValues"</c>, <c>"DuplicateCategories"</c>, <c>"NegativePieValues"</c>.
        /// </summary>
        public string Category { get; }

        /// <summary>Human-readable description of the finding.</summary>
        public string Message { get; }

        /// <summary>
        /// Name of the series the finding relates to, or <c>null</c> for chart-level findings
        /// (e.g. duplicate categories, broken date sequence).
        /// </summary>
        public string? SeriesName { get; }

        internal DataQualityWarning(
            Enums.WarningSeverity severity,
            string category,
            string message,
            string? seriesName = null)
        {
            Severity   = severity;
            Category   = category;
            Message    = message;
            SeriesName = seriesName;
        }

        /// <summary>Returns a formatted string representation for diagnostics.</summary>
        public override string ToString() =>
            SeriesName != null
                ? $"[{Severity}] {Category} ({SeriesName}): {Message}"
                : $"[{Severity}] {Category}: {Message}";
    }
}
