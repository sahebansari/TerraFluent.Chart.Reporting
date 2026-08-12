using System;
using System.Collections.Generic;
using TerraFluent.AutoAnalytics.Enums;

namespace TerraFluent.AutoAnalytics.Profiling;

/// <summary>A category and its frequency within a categorical column.</summary>
public sealed class CategoryFrequency
{
    public string Value { get; init; } = string.Empty;
    public int Count { get; init; }
    /// <summary>Share 0..1 of populated cells.</summary>
    public double Share { get; init; }
}

/// <summary>Distinct-count and top-N frequency distribution for a categorical column.</summary>
public sealed class CategoricalSummary
{
    public int DistinctCount { get; init; }
    public IReadOnlyList<CategoryFrequency> TopCategories { get; init; } = Array.Empty<CategoryFrequency>();
}

/// <summary>Temporal extent and inferred cadence of a date column.</summary>
public sealed class DateSummary
{
    public DateTime Start { get; init; }
    public DateTime End { get; init; }
    public TimeSpan Duration => End - Start;
    public DateGranularity Granularity { get; init; }
    public int Count { get; init; }
}
