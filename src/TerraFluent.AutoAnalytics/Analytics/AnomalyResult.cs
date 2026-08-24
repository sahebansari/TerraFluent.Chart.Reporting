using System.Collections.Generic;

namespace TerraFluent.AutoAnalytics.Analytics;

/// <summary>A single anomalous observation within a measure column.</summary>
public sealed class AnomalyPoint
{
    /// <summary>Zero-based row index of the outlier within the non-missing value sequence.</summary>
    public int Index { get; init; }
    public double Value { get; init; }
    /// <summary>Classic Z-score relative to the column mean/StdDev (kept for reference/back-compat).</summary>
    public double ZScore { get; init; }
    /// <summary>
    /// Magnitude of the statistic that actually flagged this point, on a standard-deviation-comparable
    /// scale (robust modified Z from median/MAD for the robust rules, classic Z only as a fallback).
    /// Ranking and severity use this so a robustly-detected outlier isn't understated by the mean/SD
    /// it inflated.
    /// </summary>
    public double Magnitude { get; init; }
    /// <summary>Method that flagged it: <c>modified-zscore</c>, <c>zscore</c>, <c>iqr</c>, or <c>spike</c>.</summary>
    public string Method { get; init; } = string.Empty;
    /// <summary>Optional label (e.g. the associated category or date) for the point.</summary>
    public string? Label { get; init; }
}

/// <summary>All anomalies detected within one measure column.</summary>
public sealed class AnomalyResult
{
    public string Measure { get; init; } = string.Empty;
    public IReadOnlyList<AnomalyPoint> Anomalies { get; init; } = new List<AnomalyPoint>();
    /// <summary>Largest robust magnitude among the anomalies (0 when none).</summary>
    public double MaxMagnitude { get; init; }
}
