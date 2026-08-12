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
}
