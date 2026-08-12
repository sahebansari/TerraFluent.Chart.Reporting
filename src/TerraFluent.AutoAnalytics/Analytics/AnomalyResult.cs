using System.Collections.Generic;

namespace TerraFluent.AutoAnalytics.Analytics;

/// <summary>A single anomalous observation within a measure column.</summary>
public sealed class AnomalyPoint
{
    /// <summary>Zero-based row index of the outlier within the non-missing value sequence.</summary>
    public int Index { get; init; }
    public double Value { get; init; }
    /// <summary>Z-score relative to the column mean/StdDev.</summary>
    public double ZScore { get; init; }
    /// <summary>Method that flagged it: <c>zscore</c>, <c>iqr</c>, or <c>spike</c>.</summary>
    public string Method { get; init; } = string.Empty;
    /// <summary>Optional label (e.g. the associated category or date) for the point.</summary>
    public string? Label { get; init; }
}

/// <summary>All anomalies detected within one measure column.</summary>
public sealed class AnomalyResult
{
    public string Measure { get; init; } = string.Empty;
    public IReadOnlyList<AnomalyPoint> Anomalies { get; init; } = new List<AnomalyPoint>();
    /// <summary>Largest absolute z-score among the anomalies (0 when none).</summary>
    public double MaxMagnitude { get; init; }
}
