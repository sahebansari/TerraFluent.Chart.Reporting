using System;
using System.Collections.Generic;
using System.Globalization;
using TerraFluent.Chart.Reporting.Enums;
using TerraFluent.Chart.Reporting.Models;

namespace TerraFluent.Chart.Reporting.Analysis
{
    /// <summary>
    /// Inspects a <see cref="ChartOptions"/> instance for data-quality issues and returns a
    /// <see cref="DataQualityReport"/> containing all findings with their severity levels.
    /// <para>
    /// Call via <c>ChartBuilder.Create()…AnalyzeDataQuality()</c> or directly:
    /// <code>
    /// var report = DataQualityAnalyzer.Analyze(options);
    /// if (report.HasErrors) { /* handle */ }
    /// foreach (var w in report.Warnings) Console.WriteLine(w);
    /// </code>
    /// </para>
    /// </summary>
    public static class DataQualityAnalyzer
    {
        // Absolute skewness above this threshold is considered "extremely skewed".
        private const double SkewnessThreshold = 2.0;

        // ── Public entry point ────────────────────────────────────────────────────

        /// <summary>
        /// Runs all data-quality checks against <paramref name="options"/> and returns a
        /// <see cref="DataQualityReport"/> with every finding sorted by severity (highest first).
        /// </summary>
        /// <param name="options">The chart configuration to inspect.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="options"/> is <c>null</c>.</exception>
        public static DataQualityReport Analyze(ChartOptions options)
        {
            if (options == null) throw new ArgumentNullException(nameof(options));

            var warnings = new List<DataQualityWarning>();

            // Chart-level checks
            CheckDuplicateCategories(options, warnings);
            CheckBrokenDateSequence(options, warnings);

            // Series-level checks
            foreach (var series in options.Series)
            {
                CheckEmptyDataset(series, warnings);
                CheckMissingValues(series, warnings);
                CheckNegativePieValues(series, warnings);
                CheckInvalidPercentages(series, options, warnings);
                CheckSkewedDistribution(series, warnings);
            }

            // Sort by severity descending (Error → Warning → Info)
            warnings.Sort((a, b) => ((int)b.Severity).CompareTo((int)a.Severity));

            return new DataQualityReport(warnings);
        }

        // ── Check 1 : Missing values ──────────────────────────────────────────────

        private static void CheckMissingValues(Series series, List<DataQualityWarning> warnings)
        {
            int missing = 0;
            foreach (var v in series.Data)
                if (!v.HasValue) missing++;

            if (missing > 0)
                warnings.Add(new DataQualityWarning(
                    WarningSeverity.Warning,
                    "MissingValues",
                    $"Series '{Esc(series.Name)}' contains {missing} missing value(s) (null). " +
                    "These will appear as gaps in the chart.",
                    series.Name));
        }

        // ── Check 2 : Duplicate categories ───────────────────────────────────────

        private static void CheckDuplicateCategories(ChartOptions options, List<DataQualityWarning> warnings)
        {
            var seen  = new HashSet<string>(StringComparer.Ordinal);
            var dupes = new List<string>();

            foreach (var cat in options.XAxis.Categories)
                if (!seen.Add(cat) && !dupes.Contains(cat))
                    dupes.Add(cat);

            if (dupes.Count > 0)
            {
                string plural = dupes.Count == 1 ? "category" : "categories";
                var quoted    = new string[dupes.Count];
                for (int i = 0; i < dupes.Count; i++) quoted[i] = $"'{Esc(dupes[i])}'";

                warnings.Add(new DataQualityWarning(
                    WarningSeverity.Error,
                    "DuplicateCategories",
                    $"XAxis contains {dupes.Count} duplicate {plural}: {string.Join(", ", quoted)}. " +
                    "Duplicate labels will cause data points to overlap.",
                    null));
            }
        }

        // ── Check 3 : Negative values in Pie / Donut ─────────────────────────────

        private static void CheckNegativePieValues(Series series, List<DataQualityWarning> warnings)
        {
            if (series.Type != ChartType.Pie) return;

            int negative = 0;
            foreach (var v in series.Data)
                if (v.HasValue && v.Value < 0) negative++;

            if (negative > 0)
                warnings.Add(new DataQualityWarning(
                    WarningSeverity.Error,
                    "NegativePieValues",
                    $"Pie/Donut series '{Esc(series.Name)}' contains {negative} negative value(s). " +
                    "Negative slice values are not valid and will be rendered as zero or omitted.",
                    series.Name));
        }

        // ── Check 4 : Broken date sequences ──────────────────────────────────────

        private static void CheckBrokenDateSequence(ChartOptions options, List<DataQualityWarning> warnings)
        {
            var dates = options.XAxis.DateTimeValues;
            if (dates == null || dates.Count < 2) return;

            int outOfOrder = 0;
            int duplicates = 0;

            for (int i = 1; i < dates.Count; i++)
            {
                int cmp = DateTime.Compare(dates[i], dates[i - 1]);
                if (cmp == 0) duplicates++;
                else if (cmp < 0) outOfOrder++;
            }

            if (outOfOrder > 0 || duplicates > 0)
            {
                var parts = new List<string>(2);
                if (outOfOrder > 0) parts.Add($"{outOfOrder} out-of-order date(s)");
                if (duplicates > 0) parts.Add($"{duplicates} duplicate date(s)");

                warnings.Add(new DataQualityWarning(
                    WarningSeverity.Warning,
                    "BrokenDateSequence",
                    $"XAxis DateTime sequence contains {string.Join(" and ", parts)}. " +
                    "The time axis may render incorrectly.",
                    null));
            }
        }

        // ── Check 5 : Invalid percentages ────────────────────────────────────────

        private static void CheckInvalidPercentages(
            Series series, ChartOptions options, List<DataQualityWarning> warnings)
        {
            bool isPercentSeries =
                (options.YAxis.LabelFormat    != null && options.YAxis.LabelFormat.Contains("%")) ||
                (series.DataLabel.FormatString != null && series.DataLabel.FormatString.Contains("%"));

            if (!isPercentSeries) return;

            int outOfRange = 0;
            foreach (var v in series.Data)
                if (v.HasValue && (v.Value < 0.0 || v.Value > 100.0))
                    outOfRange++;

            if (outOfRange > 0)
                warnings.Add(new DataQualityWarning(
                    WarningSeverity.Warning,
                    "InvalidPercentage",
                    $"Series '{Esc(series.Name)}' has {outOfRange} value(s) outside the valid " +
                    "percentage range [0, 100].",
                    series.Name));
        }

        // ── Check 6 : Empty datasets ──────────────────────────────────────────────

        private static void CheckEmptyDataset(Series series, List<DataQualityWarning> warnings)
        {
            if (series.Data.Count == 0)
            {
                warnings.Add(new DataQualityWarning(
                    WarningSeverity.Error,
                    "EmptyDataset",
                    $"Series '{Esc(series.Name)}' has no data points.",
                    series.Name));
                return;
            }

            bool anyValue = false;
            foreach (var v in series.Data)
                if (v.HasValue) { anyValue = true; break; }

            if (!anyValue)
                warnings.Add(new DataQualityWarning(
                    WarningSeverity.Error,
                    "EmptyDataset",
                    $"Series '{Esc(series.Name)}' contains only null values — the dataset is effectively empty.",
                    series.Name));
        }

        // ── Check 7 : Extremely skewed distributions ──────────────────────────────

        private static void CheckSkewedDistribution(Series series, List<DataQualityWarning> warnings)
        {
            // Collect non-null values; need at least 3 for a meaningful skewness computation.
            var vals = new List<double>(series.Data.Count);
            foreach (var v in series.Data)
                if (v.HasValue) vals.Add(v.Value);

            if (vals.Count < 3) return;

            double n    = vals.Count;
            double mean = 0.0;
            foreach (var v in vals) mean += v;
            mean /= n;

            double variance = 0.0;
            foreach (var v in vals) variance += (v - mean) * (v - mean);
            variance /= n;

            double sd = Math.Sqrt(variance);
            if (sd < double.Epsilon) return; // constant series — no meaningful skewness

            // Sample skewness (Fisher–Pearson corrected)
            // g1 = [n / ((n-1)(n-2))] * sum( ((xi - mean) / sd)^3 )
            double cubedSum = 0.0;
            foreach (var v in vals)
            {
                double z = (v - mean) / sd;
                cubedSum += z * z * z;
            }

            double skewness = (n / ((n - 1.0) * (n - 2.0))) * cubedSum;

            if (Math.Abs(skewness) > SkewnessThreshold)
            {
                string direction = skewness > 0 ? "right (positive)" : "left (negative)";
                warnings.Add(new DataQualityWarning(
                    WarningSeverity.Warning,
                    "HighSkewness",
                    $"Series '{Esc(series.Name)}' has an extremely skewed {direction} distribution " +
                    $"(skewness = {skewness.ToString("F2", CultureInfo.InvariantCulture)}). " +
                    "Consider using a logarithmic Y axis or transforming the data.",
                    series.Name));
            }
        }

        // ── Helpers ───────────────────────────────────────────────────────────────

        /// <summary>Escapes single-quotes in display strings used inside warning messages.</summary>
        private static string Esc(string s) => s.Replace("'", "\\'");
    }
}
