using System;
using System.Collections.Generic;
using System.Globalization;

namespace TerraFluent.Chart.Reporting.Analysis
{
    /// <summary>
    /// Pure-BCL statistical computations that power the AutoInsight overlay feature.
    /// All methods operate on <c>List&lt;double?&gt;</c> (the standard series data type)
    /// and handle <c>null</c> gaps gracefully.
    /// </summary>
    internal static class SeriesAnalyzer
    {
        // ── Linear regression ─────────────────────────────────────────────────────

        /// <summary>
        /// Computes an ordinary least-squares linear regression over the non-null data points
        /// and returns the <c>(slope, intercept)</c> of the best-fit line.
        /// Returns <c>(0, mean)</c> when there are fewer than 2 valid points.
        /// </summary>
        internal static (double slope, double intercept) LinearRegression(List<double?> data)
        {
            var pts = new List<(double x, double y)>(data.Count);
            for (int i = 0; i < data.Count; i++)
                if (data[i].HasValue) pts.Add((i, data[i]!.Value));

            if (pts.Count < 2)
                return (0, pts.Count == 1 ? pts[0].y : 0);

            double n    = pts.Count;
            double sumX = 0, sumY = 0, sumXY = 0, sumXX = 0;
            foreach (var (x, y) in pts) { sumX += x; sumY += y; sumXY += x * y; sumXX += x * x; }

            double denom = n * sumXX - sumX * sumX;
            if (Math.Abs(denom) < double.Epsilon) return (0, sumY / n);

            double slope     = (n * sumXY - sumX * sumY) / denom;
            double intercept = (sumY - slope * sumX) / n;
            return (slope, intercept);
        }

        // ── Moving average ────────────────────────────────────────────────────────

        /// <summary>
        /// Computes a simple moving average with the given <paramref name="period"/>
        /// and returns an array the same length as <paramref name="data"/>.
        /// Leading positions where the full window is not yet available are <c>null</c>.
        /// </summary>
        internal static double?[] MovingAverage(List<double?> data, int period)
        {
            if (period < 1) period = 1;
            var result = new double?[data.Count];
            for (int i = period - 1; i < data.Count; i++)
            {
                double sum = 0; int cnt = 0;
                for (int j = i - period + 1; j <= i; j++)
                    if (data[j].HasValue) { sum += data[j]!.Value; cnt++; }
                if (cnt > 0) result[i] = sum / cnt;
            }
            return result;
        }

        // ── Standard-deviation bands ──────────────────────────────────────────────

        /// <summary>
        /// Computes the mean, standard deviation, and the ±<paramref name="sigma"/> bounds
        /// of the non-null values in <paramref name="data"/>.
        /// Returns all zeros when no valid points exist.
        /// </summary>
        internal static (double mean, double stddev, double lower, double upper) StdDevBands(
            List<double?> data, double sigma)
        {
            var vals = new List<double>(data.Count);
            foreach (var v in data)
                if (v.HasValue) vals.Add(v.Value);

            if (vals.Count == 0) return (0, 0, 0, 0);

            double mean = 0;
            foreach (var v in vals) mean += v;
            mean /= vals.Count;

            double variance = 0;
            foreach (var v in vals) variance += (v - mean) * (v - mean);
            variance /= vals.Count;
            double sd = Math.Sqrt(variance);

            return (mean, sd, mean - sigma * sd, mean + sigma * sd);
        }

        // ── Peak detection ────────────────────────────────────────────────────────

        /// <summary>
        /// Finds the global maximum and minimum data points.
        /// Returns <c>(-1, 0, -1, 0)</c> when <paramref name="data"/> has no valid values.
        /// </summary>
        internal static (int maxIdx, double maxVal, int minIdx, double minVal) FindPeaks(
            List<double?> data)
        {
            int    maxIdx = -1, minIdx = -1;
            double maxVal = double.MinValue, minVal = double.MaxValue;

            for (int i = 0; i < data.Count; i++)
            {
                if (!data[i].HasValue) continue;
                double v = data[i]!.Value;
                if (v > maxVal) { maxVal = v; maxIdx = i; }
                if (v < minVal) { minVal = v; minIdx = i; }
            }

            return (maxIdx,
                    maxIdx >= 0 ? maxVal : 0,
                    minIdx,
                    minIdx >= 0 ? minVal : 0);
        }

        // ── Anomaly count ─────────────────────────────────────────────────────────

        /// <summary>
        /// Counts the number of data points that fall outside the ±<paramref name="sigma"/>
        /// standard-deviation band defined by <paramref name="mean"/> and <paramref name="stddev"/>.
        /// Returns 0 when <paramref name="stddev"/> is effectively zero.
        /// </summary>
        internal static int CountAnomalies(
            List<double?> data, double mean, double stddev, double sigma)
        {
            if (stddev < double.Epsilon) return 0;
            int count = 0;
            foreach (var v in data)
                if (v.HasValue && Math.Abs(v.Value - mean) > sigma * stddev) count++;
            return count;
        }

        // ── Narrative builder ─────────────────────────────────────────────────────

        /// <summary>
        /// Produces a concise one-line narrative string that summarises the series,
        /// for example: <c>"↑ +14% trend · 2 anomalies detected"</c>.
        /// </summary>
        internal static string BuildNarrative(
            List<double?> data, double slope, double mean, int anomalyCount)
        {
            int    n          = data.Count > 1 ? data.Count : 2;
            double totalTrend = slope * (n - 1);
            double trendPct   = mean > 0.001 ? Math.Abs(totalTrend / mean * 100.0) : 0;

            // Threshold: slope must represent > 0.5 % per period to be considered directional
            double threshold = mean > 0 ? 0.005 * mean : 0.001;

            string trendIcon, trendDesc;
            if (slope > threshold)
            {
                trendIcon = "\u2191";   // ↑
                trendDesc = "+" + trendPct.ToString("F0", CultureInfo.InvariantCulture) + "% trend";
            }
            else if (slope < -threshold)
            {
                trendIcon = "\u2193";   // ↓
                trendDesc = "\u2212" + trendPct.ToString("F0", CultureInfo.InvariantCulture) + "% trend";
            }
            else
            {
                trendIcon = "\u2192";   // →
                trendDesc = "stable trend";
            }

            string narrative = trendIcon + " " + trendDesc;
            if (anomalyCount > 0)
            {
                string noun = anomalyCount == 1 ? "anomaly" : "anomalies";
                narrative += " \u00b7 " + anomalyCount.ToString(CultureInfo.InvariantCulture)
                          + " " + noun + " detected";
            }
            return narrative;
        }

        // ── Short number formatter ────────────────────────────────────────────────

        /// <summary>
        /// Formats <paramref name="v"/> compactly: 1 500 000 → "1.5M", 3 200 → "3.2K",
        /// 42.5 → "42.5".
        /// </summary>
        internal static string FormatShort(double v)
        {
            if (Math.Abs(v) >= 1_000_000)
                return (v / 1_000_000.0).ToString("F1", CultureInfo.InvariantCulture) + "M";
            if (Math.Abs(v) >= 1_000)
                return (v / 1_000.0).ToString("F1", CultureInfo.InvariantCulture) + "K";
            return v.ToString("F1", CultureInfo.InvariantCulture);
        }
    }
}
