using System.Collections.Generic;

namespace TerraFluent.Chart.Reporting.Models
{
    /// <summary>
    /// Aggregated result of a data-quality analysis run against a <see cref="ChartOptions"/> object
    /// by <see cref="TerraFluent.Chart.Reporting.Analysis.DataQualityAnalyzer"/>.
    /// </summary>
    public sealed class DataQualityReport
    {
        /// <summary>
        /// All findings produced by the analysis, ordered by severity descending
        /// (<see cref="Enums.WarningSeverity.Error"/> first).
        /// </summary>
        public IReadOnlyList<DataQualityWarning> Warnings { get; }

        /// <summary>
        /// <c>true</c> when at least one <see cref="Enums.WarningSeverity.Error"/> finding
        /// was raised. The chart output should not be trusted when this is <c>true</c>.
        /// </summary>
        public bool HasErrors { get; }

        /// <summary>
        /// <c>true</c> when no findings of any severity exist — data is considered clean.
        /// </summary>
        public bool IsClean { get; }

        internal DataQualityReport(IReadOnlyList<DataQualityWarning> warnings)
        {
            Warnings  = warnings;
            IsClean   = warnings.Count == 0;

            foreach (var w in warnings)
            {
                if (w.Severity == Enums.WarningSeverity.Error)
                {
                    HasErrors = true;
                    break;
                }
            }
        }
    }
}
