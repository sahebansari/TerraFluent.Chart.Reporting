using System;
using System.Collections.Generic;
using TerraFluent.AutoAnalytics.Schema;

namespace TerraFluent.AutoAnalytics.Profiling;

/// <summary>
/// The full statistical profile of one column: its schema <see cref="ColumnProfile"/> plus whichever
/// of the numeric/categorical/date summaries applies. Extracted non-missing numeric values and
/// string labels are retained so later analytics phases avoid re-parsing.
/// </summary>
public sealed class ColumnStatistics
{
    public ColumnProfile Profile { get; init; } = default!;

    /// <summary>Populated for numeric/currency/percentage columns.</summary>
    public NumericSummary? Numeric { get; init; }

    /// <summary>Populated for category/boolean/text columns.</summary>
    public CategoricalSummary? Categorical { get; init; }

    /// <summary>Populated for date columns.</summary>
    public DateSummary? Date { get; init; }

    /// <summary>Non-missing numeric values in row order (empty for non-numeric columns).</summary>
    public IReadOnlyList<double> NumericValues { get; init; } = Array.Empty<double>();

    /// <summary>Non-missing string labels in row order (for grouping).</summary>
    public IReadOnlyList<string> Labels { get; init; } = Array.Empty<string>();

    /// <summary>Non-missing dates in row order (empty for non-date columns).</summary>
    public IReadOnlyList<DateTime> Dates { get; init; } = Array.Empty<DateTime>();

    public string Name => Profile.Name;
}
