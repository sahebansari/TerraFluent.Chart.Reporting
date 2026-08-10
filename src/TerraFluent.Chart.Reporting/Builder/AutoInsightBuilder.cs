using System;
using TerraFluent.Chart.Reporting.Models;

namespace TerraFluent.Chart.Reporting.Builder
{
    /// <summary>
    /// Fluent builder for configuring per-series AutoInsight overlays.
    /// Obtain via <c>SeriesBuilder.AutoInsight(ai => …)</c>.
    /// </summary>
    public sealed class AutoInsightBuilder
    {
        private readonly AutoInsightOptions _opts = new AutoInsightOptions();

        // ── Trend line ────────────────────────────────────────────────────────────

        /// <summary>
        /// Renders a dashed linear-regression trend line computed from the series data.
        /// </summary>
        /// <param name="color">Stroke colour. <c>null</c> (default) inherits the theme's <c>TextColor</c>.</param>
        public AutoInsightBuilder TrendLine(string? color = null)
        {
            _opts.ShowTrendLine  = true;
            _opts.TrendLineColor = color;
            return this;
        }

        // ── Moving average ────────────────────────────────────────────────────────

        /// <summary>
        /// Renders a simple moving-average overlay over the series.
        /// </summary>
        /// <param name="period">Rolling window width. Default <c>3</c>.</param>
        /// <param name="color">
        /// Stroke colour. <c>null</c> (default) inherits the series colour at 85 % opacity.
        /// </param>
        public AutoInsightBuilder MovingAverage(int period = 3, string? color = null)
        {
            if (period < 1) throw new ArgumentOutOfRangeException(nameof(period), period,
                "MovingAverage period must be at least 1.");
            _opts.ShowMovingAverage    = true;
            _opts.MovingAveragePeriod  = period;
            _opts.MovingAverageColor   = color;
            return this;
        }

        // ── Anomaly bands ─────────────────────────────────────────────────────────

        /// <summary>
        /// Renders a semi-transparent ±σ shaded band behind the series.
        /// Data points outside the band are statistical anomalies.
        /// </summary>
        /// <param name="sigma">Standard-deviation multiplier. Default <c>1.5</c>.</param>
        /// <param name="bandColor">
        /// Fill colour. <c>null</c> (default) uses the series colour.
        /// </param>
        public AutoInsightBuilder AnomalyBands(double sigma = 1.5, string? bandColor = null)
        {
            if (sigma <= 0) throw new ArgumentOutOfRangeException(nameof(sigma), sigma,
                "AnomalyBands sigma must be greater than zero.");
            _opts.ShowAnomalyBands  = true;
            _opts.AnomalyBandsSigma = sigma;
            _opts.AnomalyBandColor  = bandColor;
            return this;
        }

        // ── Peak highlights ───────────────────────────────────────────────────────

        /// <summary>
        /// Draws a ring and value label on the global maximum (green) and minimum (red)
        /// data points.
        /// </summary>
        public AutoInsightBuilder HighlightPeaks()
        {
            _opts.HighlightPeaks = true;
            return this;
        }

        // ── Narrative summary ─────────────────────────────────────────────────────

        /// <summary>
        /// Appends a one-line narrative annotation in the top-right corner of the plot area,
        /// e.g. <em>"↑ +14% trend · 1 anomaly detected"</em>.
        /// Requires <see cref="AnomalyBands"/> to be enabled for the anomaly count to
        /// appear in the narrative; trend direction is always computed.
        /// </summary>
        public AutoInsightBuilder NarrativeSummary()
        {
            _opts.ShowNarrativeSummary = true;
            return this;
        }

        internal AutoInsightOptions Build() => _opts;
    }
}
