namespace TerraFluent.Chart.Reporting.Enums
{
    /// <summary>
    /// Severity level of a <see cref="TerraFluent.Chart.Reporting.Models.DataQualityWarning"/>
    /// produced by <see cref="TerraFluent.Chart.Reporting.Analysis.DataQualityAnalyzer"/>.
    /// </summary>
    public enum WarningSeverity
    {
        /// <summary>
        /// Informational notice — data is valid but a characteristic is worth noting
        /// (e.g. a mildly skewed distribution).
        /// </summary>
        Info = 0,

        /// <summary>
        /// Potential data-quality issue that may reduce chart accuracy or readability
        /// (e.g. missing values, high skewness, out-of-order dates).
        /// </summary>
        Warning = 1,

        /// <summary>
        /// Critical data-quality issue that will produce incorrect or meaningless chart output
        /// (e.g. negative pie slices, duplicate categories, completely empty dataset).
        /// </summary>
        Error = 2
    }
}
