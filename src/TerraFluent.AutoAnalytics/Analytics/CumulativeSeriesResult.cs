using System;
using System.Collections.Generic;
using TerraFluent.AutoAnalytics.Enums;

namespace TerraFluent.AutoAnalytics.Analytics;

/// <summary>Running cumulative sum of an additive measure over the primary date column.</summary>
public sealed class CumulativeSeriesResult
{
    public string Measure { get; init; } = string.Empty;
    public string DateColumn { get; init; } = string.Empty;
    /// <summary>Total accumulated across all periods.</summary>
    public double FinalTotal { get; init; }
    public DateGranularity Granularity { get; init; }
    /// <summary>Per-period (date, running total) in chronological order.</summary>
    public IReadOnlyList<(DateTime Period, double Cumulative)> Points { get; init; }
        = new List<(DateTime, double)>();
}
