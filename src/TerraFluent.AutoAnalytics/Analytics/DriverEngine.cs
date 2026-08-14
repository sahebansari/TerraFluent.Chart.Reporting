using System;
using System.Collections.Generic;
using System.Linq;
using TerraFluent.AutoAnalytics.Profiling;

namespace TerraFluent.AutoAnalytics.Analytics;

/// <summary>The change contribution of a single category between two periods.</summary>
public sealed class DriverContribution
{
    public string Category { get; init; } = string.Empty;
    public double FirstHalf { get; init; }
    public double SecondHalf { get; init; }

    /// <summary>Second-half total minus first-half total for this category.</summary>
    public double Delta { get; init; }

    /// <summary>Signed share of the measure's net change attributable to this category (may exceed 1 or be negative).</summary>
    public double ShareOfChange { get; init; }
}

/// <summary>
/// Deterministic driver (root-cause) analysis: decomposes a measure's period-over-period change
/// across the categories of a dimension, identifying which category drove the movement most.
/// </summary>
public sealed class DriverResult
{
    public string Measure { get; init; } = string.Empty;
    public string Dimension { get; init; } = string.Empty;
    public double TotalChange { get; init; }
    public bool Increased => TotalChange >= 0;

    /// <summary>Per-category contributions, ordered by descending absolute delta.</summary>
    public IReadOnlyList<DriverContribution> Contributions { get; init; } = new List<DriverContribution>();

    public DriverContribution? TopDriver => Contributions.Count > 0 ? Contributions[0] : null;
}

/// <summary>
/// Phase 4c — explains <em>why</em> a measure changed by splitting the date-ordered rows into an
/// earlier and later half and attributing the net change to the categories of a dimension. Falls
/// back to row order when no date column is present. No AI; fully reproducible.
/// </summary>
public sealed class DriverEngine
{
    /// <summary>
    /// Explains the change in <paramref name="measureName"/> across the best (or specified) dimension.
    /// Returns <see langword="null"/> when there is insufficient data to attribute a change.
    /// </summary>
    public DriverResult? Explain(DatasetProfile profile, string measureName, string? dimensionName = null)
    {
        var measure = profile.Columns.FirstOrDefault(c =>
            string.Equals(c.Name, measureName, StringComparison.OrdinalIgnoreCase) && c.Numeric is not null);
        if (measure is null) return null;

        var dimension = dimensionName is not null
            ? profile.Categories.FirstOrDefault(c => string.Equals(c.Name, dimensionName, StringComparison.OrdinalIgnoreCase))
            : profile.Categories.FirstOrDefault(c => c.Categorical is { DistinctCount: >= 2 and <= 50 });
        if (dimension is null) return null;

        // Pair (label, value) by ORIGINAL ROW (complete-case) and order by the primary date, so a
        // missing date/label/value never misaligns the earlier/later split.
        var ordered = RowAlignment.LabelValuesOrderedByDate(dimension, measure, profile.DateColumns.FirstOrDefault());
        int n = ordered.Count;
        if (n < 4) return null;

        int mid = n / 2;
        var first = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
        var second = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);

        for (int k = 0; k < n; k++)
        {
            var (cat, val) = ordered[k];
            var bucket = k < mid ? first : second;
            bucket[cat] = bucket.TryGetValue(cat, out var s) ? s + val : val;
        }

        double totalChange = second.Values.Sum() - first.Values.Sum();

        var categories = first.Keys.Union(second.Keys, StringComparer.OrdinalIgnoreCase);
        var contributions = categories
            .Select(cat =>
            {
                double f = first.TryGetValue(cat, out var fv) ? fv : 0;
                double s = second.TryGetValue(cat, out var sv) ? sv : 0;
                double delta = s - f;
                return new DriverContribution
                {
                    Category = cat,
                    FirstHalf = f,
                    SecondHalf = s,
                    Delta = delta,
                    ShareOfChange = totalChange == 0 ? 0 : delta / totalChange
                };
            })
            .OrderByDescending(c => Math.Abs(c.Delta))
            .ToList();

        return new DriverResult
        {
            Measure = measure.Name,
            Dimension = dimension.Name,
            TotalChange = totalChange,
            Contributions = contributions
        };
    }
}
