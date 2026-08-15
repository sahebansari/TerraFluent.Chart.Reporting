using TerraFluent.AutoAnalytics.Enums;

namespace TerraFluent.AutoAnalytics.Analytics;

/// <summary>Result of trend analysis over an ordered measure series.</summary>
public sealed class TrendResult
{
    public string Measure { get; init; } = string.Empty;
    public string? OrderedBy { get; init; }
    public TrendKind Kind { get; init; }
    /// <summary>Regression slope per step.</summary>
    public double Slope { get; init; }
    /// <summary>R² goodness of fit (0..1) — how linear the trend is.</summary>
    public double RSquared { get; init; }
    /// <summary>Total growth from first to last point, as a fraction (0.34 = +34%).</summary>
    public double GrowthRate { get; init; }
    /// <summary>Coefficient of variation (StdDev/Mean) — volatility measure.</summary>
    public double Volatility { get; init; }
    public int PointCount { get; init; }
    /// <summary>Lag-1 autocorrelation of the per-period series (−1..1) — persistence from one period to the next.</summary>
    public double Autocorrelation { get; init; }
    /// <summary>Detected seasonal period length (0 = no clear seasonality).</summary>
    public int SeasonLength { get; init; }

    /// <summary>
    /// True when the series carries genuine temporal structure worth charting over time: a
    /// reasonably linear direction, a detected season, or meaningful period-to-period persistence.
    /// A flat cloud of noise (random per-record attribute over a transaction date) has none of these.
    /// </summary>
    public bool HasTemporalSignal =>
        (Kind is TrendKind.Rising or TrendKind.Declining && RSquared >= 0.25)
        || SeasonLength > 0
        || System.Math.Abs(Autocorrelation) >= 0.25;
}
