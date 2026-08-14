using System;
using System.Collections.Generic;
using System.Linq;
using TerraFluent.AutoAnalytics.Enums;
using TerraFluent.AutoAnalytics.Profiling;

namespace TerraFluent.AutoAnalytics.Analytics;

/// <summary>One aggregated group in a single-dimension group-by.</summary>
public sealed class AggregationBucket
{
    public string Key { get; init; } = string.Empty;
    public double Value { get; init; }
    /// <summary>Number of rows in the group.</summary>
    public int Count { get; init; }
}

/// <summary>One cell in a two-dimension pivot (row × column).</summary>
public sealed class PivotCell
{
    public string Row { get; init; } = string.Empty;
    public string Column { get; init; } = string.Empty;
    public double Value { get; init; }
    public int Count { get; init; }
}

/// <summary>The result of a group-by aggregation: either <see cref="Buckets"/> (one dimension) or
/// a pivot (<see cref="Cells"/> with <see cref="RowKeys"/>/<see cref="ColumnKeys"/>) for two.</summary>
public sealed class AggregationResult
{
    public string Measure { get; init; } = string.Empty;
    public string Dimension { get; init; } = string.Empty;
    public string? SecondDimension { get; init; }
    public AggregationKind Kind { get; init; }

    public IReadOnlyList<AggregationBucket> Buckets { get; init; } = new List<AggregationBucket>();

    public IReadOnlyList<PivotCell> Cells { get; init; } = new List<PivotCell>();
    public IReadOnlyList<string> RowKeys { get; init; } = new List<string>();
    public IReadOnlyList<string> ColumnKeys { get; init; } = new List<string>();

    public bool IsPivot => SecondDimension is not null;
}

/// <summary>
/// Deterministic group-by aggregation supporting sum/average/count/min/max/median over one or two
/// dimensions. Row alignment follows the same index convention as the rest of the analytics
/// engines (dimension label i pairs with measure value i).
/// </summary>
public sealed class AggregationEngine
{
    /// <summary>
    /// Aggregates <paramref name="measureName"/> grouped by <paramref name="dim1Name"/> and,
    /// optionally, <paramref name="dim2Name"/>. Returns <see langword="null"/> when the columns
    /// cannot be resolved or there is no aligned data.
    /// </summary>
    public AggregationResult? GroupBy(
        DatasetProfile profile, string measureName, string dim1Name,
        string? dim2Name = null, AggregationKind kind = AggregationKind.Sum)
    {
        var measure = profile.Columns.FirstOrDefault(c =>
            string.Equals(c.Name, measureName, StringComparison.OrdinalIgnoreCase) && c.Numeric is not null);
        var dim1 = profile.Columns.FirstOrDefault(c =>
            string.Equals(c.Name, dim1Name, StringComparison.OrdinalIgnoreCase) && c.Labels.Count > 0);
        if (measure is null || dim1 is null) return null;

        var dim2 = dim2Name is null ? null : profile.Columns.FirstOrDefault(c =>
            string.Equals(c.Name, dim2Name, StringComparison.OrdinalIgnoreCase) && c.Labels.Count > 0);
        if (dim2Name is not null && dim2 is null) return null;

        return dim2 is null
            ? SingleDimension(measure, dim1, kind)
            : TwoDimension(measure, dim1, dim2, kind);
    }

    private static AggregationResult SingleDimension(ColumnStatistics measure, ColumnStatistics dim, AggregationKind kind)
    {
        var groups = new Dictionary<string, List<double>>(StringComparer.OrdinalIgnoreCase);
        foreach (var (key, value) in RowAlignment.LabelValues(dim, measure))
        {
            if (!groups.TryGetValue(key, out var list)) groups[key] = list = new List<double>();
            list.Add(value);
        }

        var buckets = groups
            .Select(g => new AggregationBucket { Key = g.Key, Value = Aggregate(g.Value, kind), Count = g.Value.Count })
            .OrderByDescending(b => b.Value)
            .ToList();

        return new AggregationResult
        {
            Measure = measure.Name,
            Dimension = dim.Name,
            Kind = kind,
            Buckets = buckets
        };
    }

    private static AggregationResult TwoDimension(
        ColumnStatistics measure, ColumnStatistics dim1, ColumnStatistics dim2, AggregationKind kind)
    {
        var groups = new Dictionary<(string, string), List<double>>();
        var rowKeys = new List<string>();
        var colKeys = new List<string>();
        var rowSeen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var colSeen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var (r, c, value) in RowAlignment.LabelLabelValues(dim1, dim2, measure))
        {
            if (rowSeen.Add(r)) rowKeys.Add(r);
            if (colSeen.Add(c)) colKeys.Add(c);
            var key = (r, c);
            if (!groups.TryGetValue(key, out var list)) groups[key] = list = new List<double>();
            list.Add(value);
        }

        var cells = groups
            .Select(g => new PivotCell { Row = g.Key.Item1, Column = g.Key.Item2, Value = Aggregate(g.Value, kind), Count = g.Value.Count })
            .ToList();

        return new AggregationResult
        {
            Measure = measure.Name,
            Dimension = dim1.Name,
            SecondDimension = dim2.Name,
            Kind = kind,
            Cells = cells,
            RowKeys = rowKeys,
            ColumnKeys = colKeys
        };
    }

    private static double Aggregate(List<double> values, AggregationKind kind)
    {
        if (values.Count == 0) return 0;
        return kind switch
        {
            AggregationKind.Sum     => values.Sum(),
            AggregationKind.Average => values.Average(),
            AggregationKind.Count   => values.Count,
            AggregationKind.Min     => values.Min(),
            AggregationKind.Max     => values.Max(),
            AggregationKind.Median  => Median(values),
            _                       => values.Sum()
        };
    }

    private static double Median(List<double> values)
    {
        var sorted = values.OrderBy(v => v).ToList();
        int mid = sorted.Count / 2;
        return sorted.Count % 2 == 0 ? (sorted[mid - 1] + sorted[mid]) / 2.0 : sorted[mid];
    }
}
