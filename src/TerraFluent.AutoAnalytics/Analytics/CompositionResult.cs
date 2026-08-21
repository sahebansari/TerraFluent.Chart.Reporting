using System;
using System.Collections.Generic;
using TerraFluent.AutoAnalytics.Enums;

namespace TerraFluent.AutoAnalytics.Analytics;

/// <summary>
/// A measure aggregated across two dimensions simultaneously: either (date × categorical) for a
/// stacked-area breakdown over time, or (categorical × categorical) for a stacked column/bar view.
/// </summary>
public sealed class CompositionResult
{
    public string Measure { get; init; } = string.Empty;
    /// <summary>The dimension on the category (X) axis.</summary>
    public string CategoryDimension { get; init; } = string.Empty;
    /// <summary>The dimension split into stacked series.</summary>
    public string SeriesDimension { get; init; } = string.Empty;
    public bool IsAdditive { get; init; }
    /// <summary>True when CategoryDimension is the date column (→ stacked area over time).</summary>
    public bool IsDateCategory { get; init; }
    public DateGranularity Granularity { get; init; }
    /// <summary>Category axis labels in display order. For date categories these are period keys.</summary>
    public IReadOnlyList<string> Categories { get; init; } = new List<string>();
    /// <summary>For IsDateCategory=true: the DateTime anchor for each period (used for pretty label formatting).</summary>
    public IReadOnlyList<DateTime> CategoryDates { get; init; } = new List<DateTime>();
    /// <summary>Series names in display order (ordered by total, capped at 6).</summary>
    public IReadOnlyList<string> SeriesNames { get; init; } = new List<string>();
    /// <summary>Values[seriesIndex][categoryIndex]. Null where the (series, category) pair has no data.</summary>
    public IReadOnlyList<IReadOnlyList<double?>> Values { get; init; } = new List<IReadOnlyList<double?>>();
}
