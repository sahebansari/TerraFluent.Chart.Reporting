using System;
using System.Collections.Generic;
using System.Linq;

namespace TerraFluent.AutoAnalytics.Statistics;

/// <summary>A single projected point with a symmetric confidence interval.</summary>
public sealed class ForecastPoint
{
    /// <summary>1-based horizon step (1 = next period).</summary>
    public int Step { get; init; }
    public double Value { get; init; }
    public double Lower { get; init; }
    public double Upper { get; init; }
}

/// <summary>The output of a forecast: projected points plus the trajectory relative to the last actual.</summary>
public sealed class ForecastResult
{
    public IReadOnlyList<ForecastPoint> Points { get; init; } = new List<ForecastPoint>();
    public double LastActual { get; init; }

    /// <summary>Fraction change from the last actual to the final projected point (0.2 = +20%).</summary>
    public double ProjectedChange { get; init; }

    /// <summary>The smoothing method used: <c>"holt"</c> (linear) or <c>"holt-winters"</c> (seasonal).</summary>
    public string Method { get; init; } = "holt";

    /// <summary>Detected/applied seasonal period (0 = none/non-seasonal).</summary>
    public int SeasonLength { get; init; }

    public bool IsEmpty => Points.Count == 0;
}

/// <summary>
/// Deterministic time-series forecasting. Uses Holt's linear method (double exponential smoothing)
/// for trended series and Holt-Winters additive (triple exponential smoothing) when a seasonal
/// period is detected. Smoothing parameters are chosen by minimising in-sample one-step-ahead SSE
/// (a small deterministic grid search), so results are reproducible with no AI. Confidence bands
/// are derived from the residuals of the fitted model.
/// </summary>
public static class Forecasting
{
    // Coarse-but-effective smoothing-parameter grid. Deterministic and cheap; avoids the arbitrary
    // fixed 0.5/0.3 that can badly under/over-smooth a given series.
    private static readonly double[] Grid = { 0.05, 0.1, 0.2, 0.3, 0.4, 0.5, 0.6, 0.7, 0.8, 0.9 };

    /// <summary>
    /// Projects <paramref name="horizon"/> future points, automatically choosing Holt-Winters
    /// (seasonal) when a season is supplied or detected and there is enough history, otherwise
    /// Holt's linear method. Returns an empty result for series shorter than four points.
    /// </summary>
    /// <param name="series">Evenly-spaced observations in chronological order.</param>
    /// <param name="horizon">Number of future periods to project (≥1).</param>
    /// <param name="seasonLength">
    /// Known seasonal period (e.g. 12 monthly, 4 quarterly). <c>null</c> = auto-detect. Seasonality
    /// is applied only when at least two full seasons of history are available.
    /// </param>
    public static ForecastResult Forecast(IReadOnlyList<double> series, int horizon = 3, int? seasonLength = null)
    {
        if (series is null || series.Count < 4 || horizon < 1) return new ForecastResult();

        int season = seasonLength ?? DetectSeasonLength(series);
        if (season >= 2 && series.Count >= season * 2)
            return HoltWinters(series, horizon, season);
        return Holt(series, horizon);
    }

    /// <summary>
    /// Holt's linear method (double exponential smoothing). When <paramref name="alpha"/> or
    /// <paramref name="beta"/> is <c>null</c> they are chosen by minimising in-sample SSE.
    /// Returns an empty result for series shorter than four points.
    /// </summary>
    public static ForecastResult Holt(IReadOnlyList<double> series, int horizon = 3, double? alpha = null, double? beta = null)
    {
        if (series is null || series.Count < 4 || horizon < 1)
            return new ForecastResult();

        (double a, double b) = (alpha.HasValue && beta.HasValue)
            ? (alpha.Value, beta.Value)
            : OptimiseHolt(series);

        var (level, trend, residuals) = RunHolt(series, a, b);
        double sigma = StandardDeviation(residuals);
        double lastActual = series[^1];

        var points = new List<ForecastPoint>(horizon);
        for (int h = 1; h <= horizon; h++)
        {
            double value = level + h * trend;
            double band = 1.96 * sigma * Math.Sqrt(h); // random-walk error growth
            points.Add(new ForecastPoint { Step = h, Value = value, Lower = value - band, Upper = value + band });
        }

        return BuildResult(points, lastActual, "holt", 0);
    }

    /// <summary>
    /// Holt-Winters additive method (triple exponential smoothing) for series with a repeating
    /// seasonal pattern of length <paramref name="seasonLength"/>. Smoothing parameters are
    /// optimised by in-sample SSE. Falls back to <see cref="Holt"/> when there is too little history.
    /// </summary>
    public static ForecastResult HoltWinters(IReadOnlyList<double> series, int horizon, int seasonLength)
    {
        if (series is null || horizon < 1) return new ForecastResult();
        int L = seasonLength;
        if (L < 2 || series.Count < L * 2) return Holt(series, horizon);

        var (a, b, g) = OptimiseHoltWinters(series, L);
        var (level, trend, seasonals, residuals) = RunHoltWinters(series, L, a, b, g);
        double sigma = StandardDeviation(residuals);
        double lastActual = series[^1];

        var points = new List<ForecastPoint>(horizon);
        for (int h = 1; h <= horizon; h++)
        {
            double seasonal = seasonals[(series.Count + h - 1) % L];
            double value = level + h * trend + seasonal;
            double band = 1.96 * sigma * Math.Sqrt(h);
            points.Add(new ForecastPoint { Step = h, Value = value, Lower = value - band, Upper = value + band });
        }

        return BuildResult(points, lastActual, "holt-winters", L);
    }

    private static ForecastResult BuildResult(List<ForecastPoint> points, double lastActual, string method, int season)
    {
        double finalValue = points[^1].Value;
        double change = lastActual == 0 ? 0 : (finalValue - lastActual) / Math.Abs(lastActual);
        return new ForecastResult
        {
            Points = points,
            LastActual = lastActual,
            ProjectedChange = change,
            Method = method,
            SeasonLength = season
        };
    }

    // ── Holt core ────────────────────────────────────────────────────────────────

    private static (double Level, double Trend, List<double> Residuals) RunHolt(
        IReadOnlyList<double> series, double alpha, double beta)
    {
        double level = series[0];
        double trend = series[1] - series[0];
        var residuals = new List<double>(series.Count);

        for (int t = 1; t < series.Count; t++)
        {
            double forecast = level + trend;             // one-step-ahead
            residuals.Add(series[t] - forecast);
            double prevLevel = level;
            level = alpha * series[t] + (1 - alpha) * (level + trend);
            trend = beta * (level - prevLevel) + (1 - beta) * trend;
        }
        return (level, trend, residuals);
    }

    private static (double Alpha, double Beta) OptimiseHolt(IReadOnlyList<double> series)
    {
        double bestSse = double.MaxValue;
        double bestA = 0.5, bestB = 0.3;
        foreach (double a in Grid)
        foreach (double b in Grid)
        {
            var (_, _, residuals) = RunHolt(series, a, b);
            double sse = Sse(residuals);
            if (sse < bestSse) { bestSse = sse; bestA = a; bestB = b; }
        }
        return (bestA, bestB);
    }

    // ── Holt-Winters core (additive) ──────────────────────────────────────────────

    private static (double Level, double Trend, double[] Seasonals, List<double> Residuals) RunHoltWinters(
        IReadOnlyList<double> series, int L, double alpha, double beta, double gamma)
    {
        // Initial level = mean of the first season; initial trend = mean per-step change between
        // the first two seasons; initial seasonals = first-season deviations from the level.
        double level = 0;
        for (int i = 0; i < L; i++) level += series[i];
        level /= L;

        double trend = 0;
        for (int i = 0; i < L; i++) trend += (series[L + i] - series[i]) / L;
        trend /= L;

        var seasonals = new double[L];
        for (int i = 0; i < L; i++) seasonals[i] = series[i] - level;

        var residuals = new List<double>(series.Count);
        for (int t = L; t < series.Count; t++)
        {
            int s = t % L;
            double forecast = level + trend + seasonals[s]; // one-step-ahead
            residuals.Add(series[t] - forecast);

            double prevLevel = level;
            double detrended = series[t] - seasonals[s];
            level = alpha * detrended + (1 - alpha) * (level + trend);
            trend = beta * (level - prevLevel) + (1 - beta) * trend;
            seasonals[s] = gamma * (series[t] - level) + (1 - gamma) * seasonals[s];
        }
        return (level, trend, seasonals, residuals);
    }

    private static (double Alpha, double Beta, double Gamma) OptimiseHoltWinters(IReadOnlyList<double> series, int L)
    {
        double bestSse = double.MaxValue;
        double bestA = 0.3, bestB = 0.1, bestG = 0.3;
        foreach (double a in Grid)
        foreach (double b in Grid)
        foreach (double g in Grid)
        {
            var (_, _, _, residuals) = RunHoltWinters(series, L, a, b, g);
            double sse = Sse(residuals);
            if (sse < bestSse) { bestSse = sse; bestA = a; bestB = b; bestG = g; }
        }
        return (bestA, bestB, bestG);
    }

    // ── Seasonality detection ──────────────────────────────────────────────────────

    /// <summary>
    /// Detects a seasonal period via the sample autocorrelation function. Returns the lag (2..n/2)
    /// with the strongest autocorrelation above a significance floor, preferring common calendar
    /// periods (12, 4, 7). Returns 0 when no clear seasonality is present.
    /// </summary>
    public static int DetectSeasonLength(IReadOnlyList<double> series)
    {
        int n = series.Count;
        if (n < 8) return 0;

        double mean = 0;
        for (int i = 0; i < n; i++) mean += series[i];
        mean /= n;

        double denom = 0;
        for (int i = 0; i < n; i++) { double d = series[i] - mean; denom += d * d; }
        if (denom <= 0) return 0;

        int bestLag = 0;
        double bestAcf = 0.3; // significance floor
        int maxLag = n / 2;
        for (int lag = 2; lag <= maxLag; lag++)
        {
            double num = 0;
            for (int i = lag; i < n; i++) num += (series[i] - mean) * (series[i - lag] - mean);
            double acf = num / denom;
            // Prefer calendar-friendly lags on ties/near-ties.
            double weighted = acf + (lag is 12 or 4 or 7 ? 0.02 : 0);
            if (weighted > bestAcf) { bestAcf = weighted; bestLag = lag; }
        }
        return bestLag;
    }

    // ── helpers ────────────────────────────────────────────────────────────────────

    private static double Sse(IReadOnlyList<double> residuals)
    {
        double sum = 0;
        for (int i = 0; i < residuals.Count; i++) sum += residuals[i] * residuals[i];
        return sum;
    }

    private static double StandardDeviation(IReadOnlyList<double> values)
    {
        if (values.Count < 2) return 0;
        double mean = values.Average();
        double sumSq = values.Sum(v => (v - mean) * (v - mean));
        return Math.Sqrt(sumSq / (values.Count - 1));
    }
}
