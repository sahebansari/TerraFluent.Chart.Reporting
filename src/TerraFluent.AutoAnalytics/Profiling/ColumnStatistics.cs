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

    /// <summary>
    /// Row-aligned numeric values: index = original dataset row, <c>null</c> where the cell is
    /// missing or non-numeric. Unlike <see cref="NumericValues"/> (which compacts out missing cells)
    /// this preserves positional alignment so cross-column analytics can pair rows correctly.
    /// </summary>
    public IReadOnlyList<double?> NumericByRow { get; init; } = Array.Empty<double?>();

    /// <summary>Row-aligned string labels: index = original row, <c>null</c> where missing.</summary>
    public IReadOnlyList<string?> LabelByRow { get; init; } = Array.Empty<string?>();

    /// <summary>Row-aligned dates: index = original row, <c>null</c> where missing/unparseable.</summary>
    public IReadOnlyList<DateTime?> DateByRow { get; init; } = Array.Empty<DateTime?>();

    public string Name => Profile.Name;
}
