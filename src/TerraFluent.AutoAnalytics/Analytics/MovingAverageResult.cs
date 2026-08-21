using System;
using System.Collections.Generic;
using TerraFluent.AutoAnalytics.Enums;

namespace TerraFluent.AutoAnalytics.Analytics;

/// <summary>Simple moving average computed for one measure over the primary date column.</summary>
public sealed class MovingAverageResult
{
    public string Measure { get; init; } = string.Empty;
    public string DateColumn { get; init; } = string.Empty;
    /// <summary>Number of periods averaged in each window.</summary>
    public int WindowSize { get; init; }
    public DateGranularity Granularity { get; init; }
    /// <summary>Per-period (date, raw value, smoothed value). Smoothed is null during the warm-up window.</summary>
    public IReadOnlyList<(DateTime Period, double Raw, double? Smoothed)> Points { get; init; }
        = new List<(DateTime, double, double?)>();
}
