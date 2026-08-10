namespace TerraFluent.Chart.Reporting.Models
{
    /// <summary>
    /// Configuration for the <b>AutoInsight</b> feature — automatic statistical overlays
    /// computed and rendered entirely server-side as pure SVG elements.
    /// <para>
    /// Apply per-series via <c>SeriesBuilder.AutoInsight(ai => ai.TrendLine().MovingAverage(3)…)</c>.
    /// Works with <see cref="Enums.ChartType.Line"/>, <see cref="Enums.ChartType.Spline"/>,
    /// and <see cref="Enums.ChartType.Area"/> series.
    /// </para>
    /// </summary>
    public sealed class AutoInsightOptions
    {
        // ── Trend line ────────────────────────────────────────────────────────────

        /// <summary>
        /// When <c>true</c>, renders a dashed linear-regression trend line over the series data.
        /// Computed via ordinary least squares (pure BCL, zero external dependencies).
        /// </summary>
        public bool ShowTrendLine { get; set; }

        /// <summary>
        /// Stroke colour of the trend line.
        /// <c>null</c> inherits the theme's <c>TextColor</c>.
        /// </summary>
        public string? TrendLineColor { get; set; }

        // ── Moving average ────────────────────────────────────────────────────────

        /// <summary>
        /// When <c>true</c>, renders a simple moving-average smoothing overlay.
        /// The period is controlled by <see cref="MovingAveragePeriod"/>.
        /// </summary>
        public bool ShowMovingAverage { get; set; }

        /// <summary>Window size (number of periods) for the moving average. Default <c>3</c>.</summary>
        public int MovingAveragePeriod { get; set; } = 3;

        /// <summary>
        /// Stroke colour of the moving-average overlay.
        /// <c>null</c> → the series colour is used at reduced opacity.
        /// </summary>
        public string? MovingAverageColor { get; set; }

        // ── Anomaly band ──────────────────────────────────────────────────────────

        /// <summary>
        /// When <c>true</c>, renders a semi-transparent ±σ band behind the series to
        /// visualise the "normal" range.  Points outside the band are statistical anomalies.
        /// </summary>
        public bool ShowAnomalyBands { get; set; }

        /// <summary>
        /// Number of standard deviations that defines the normal range.
        /// Default <c>1.5</c>.
        /// </summary>
        public double AnomalyBandsSigma { get; set; } = 1.5;

        /// <summary>
        /// Fill colour of the anomaly band.
        /// <c>null</c> → derived from the parent series colour.
        /// </summary>
        public string? AnomalyBandColor { get; set; }

        // ── Peak highlights ───────────────────────────────────────────────────────

        /// <summary>
        /// When <c>true</c>, draws a ring and value label on the global maximum
        /// (green) and minimum (red) data points.
        /// </summary>
        public bool HighlightPeaks { get; set; }

        // ── Narrative summary ─────────────────────────────────────────────────────

        /// <summary>
        /// When <c>true</c>, appends a one-line narrative annotation in the top-right
        /// corner of the plot area, e.g. <em>"↑ +14% trend · 1 anomaly detected"</em>.
        /// Computed entirely server-side; safe for <c>SvgMode.Static</c> (PDF / email).
        /// </summary>
        public bool ShowNarrativeSummary { get; set; }

        internal AutoInsightOptions Clone() => new AutoInsightOptions
        {
            ShowTrendLine        = ShowTrendLine,
            TrendLineColor       = TrendLineColor,
            ShowMovingAverage    = ShowMovingAverage,
            MovingAveragePeriod  = MovingAveragePeriod,
            MovingAverageColor   = MovingAverageColor,
            ShowAnomalyBands     = ShowAnomalyBands,
            AnomalyBandsSigma    = AnomalyBandsSigma,
            AnomalyBandColor     = AnomalyBandColor,
            HighlightPeaks       = HighlightPeaks,
            ShowNarrativeSummary = ShowNarrativeSummary,
        };
    }
}
