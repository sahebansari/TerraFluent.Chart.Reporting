using System.Text;

namespace TerraFluent.Chart.Reporting.Models
{
    /// <summary>
    /// Thrown by
    /// <see cref="TerraFluent.Chart.Reporting.Builder.ChartBuilder.RenderToSvg()"/>
    /// when <c>ThrowOnDataQualityErrors()</c> has been set on the builder and at least one
    /// <see cref="Enums.WarningSeverity"/> (<c>Error</c>) finding is detected before rendering begins.
    /// <para>
    /// The full set of findings — including lower-severity warnings — is available via
    /// <see cref="Report"/>.
    /// </para>
    /// </summary>
    public sealed class DataQualityException : System.InvalidOperationException
    {
        /// <summary>The complete data-quality report that triggered this exception.</summary>
        public DataQualityReport Report { get; }

        internal DataQualityException(DataQualityReport report)
            : base(BuildMessage(report))
        {
            Report = report;
        }

        private static string BuildMessage(DataQualityReport report)
        {
            var sb = new StringBuilder(
                "Chart rendering aborted: data-quality errors were detected.");
            foreach (var w in report.Warnings)
                if (w.Severity == Enums.WarningSeverity.Error)
                    sb.Append("\n  \u2022 ").Append(w.ToString());
            return sb.ToString();
        }
    }
}
