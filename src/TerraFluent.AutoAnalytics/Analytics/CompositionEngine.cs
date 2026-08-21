using System;
using System.Collections.Generic;
using System.Linq;
using TerraFluent.AutoAnalytics.Enums;
using TerraFluent.AutoAnalytics.Profiling;
using TerraFluent.AutoAnalytics.Schema;

namespace TerraFluent.AutoAnalytics.Analytics;

/// <summary>
/// Produces <see cref="CompositionResult"/> records by breaking a measure down across two dimensions
/// at once. Two strategies are used:
/// <list type="bullet">
///   <item>date × categorical dim → stacked-area breakdown of the measure by segment over time</item>
///   <item>categorical × categorical → stacked column/bar view of composition within each group</item>
/// </list>
/// </summary>
internal sealed class CompositionEngine
{
    private const int MaxCompositions = 8;
    private const int MaxSeriesValues = 6;    // max stacked series (readable chart)
    private const int MaxCategoryValues = 12; // max X categories for cat×cat
    private const int MaxDateCategories = 24; // cap time periods so the x-axis stays readable

    public IReadOnlyList<CompositionResult> Compute(DatasetProfile profile)
    {
        var results = new List<CompositionResult>();
        var dims = profile.Categories.Where(c => c.Categorical is { DistinctCount: >= 2 and <= 50 }).ToList();
        var measures = profile.Measures.Where(m => m.NumericValues.Count > 0).ToList();

        // ── date × dim: how each segment contributes to the measure over time ──
        var dateCol = profile.DateColumns.FirstOrDefault();
        if (dateCol is not null)
        {
            var gran = dateCol.Date?.Granularity ?? DateGranularity.Daily;
            foreach (var dim in dims.Where(d => d.Categorical!.DistinctCount <= MaxSeriesValues))
            foreach (var measure in measures)
            {
                var r = BuildDateDimResult(dateCol, dim, measure, gran);
                if (r is not null) results.Add(r);
                if (results.Count >= MaxCompositions) return results;
            }
        }

        // ── dim1 × dim2: how one grouping is composed within another ──────────
        for (int i = 0; i < dims.Count && results.Count < MaxCompositions; i++)
        for (int j = 0; j < dims.Count && results.Count < MaxCompositions; j++)
        {
            if (i == j) continue;
            var dim2 = dims[j];
            if (dim2.Categorical!.DistinctCount > MaxSeriesValues) continue;
            foreach (var measure in measures)
            {
                var r = BuildCatCatResult(dims[i], dim2, measure);
                if (r is not null) results.Add(r);
                if (results.Count >= MaxCompositions) return results;
            }
        }

        return results;
    }

    private static CompositionResult? BuildDateDimResult(
        ColumnStatistics dateCol, ColumnStatistics dim, ColumnStatistics measure, DateGranularity gran)
    {
        // Stacking only makes sense for additive measures — a stack of averaged attributes
        // (age, rating) has no meaningful total height.
        bool additive = MeasureSemantics.IsAdditive(measure.Profile, measure.Numeric?.Min, measure.Numeric?.Max);
        if (!additive) return null;

        var dr = dateCol.DateByRow;
        var lr = dim.LabelByRow;
        var mr = measure.NumericByRow;
        int n = Math.Min(dr.Count, Math.Min(lr.Count, mr.Count));

        // Accumulate per (period key, dim value) pair.
        var acc = new Dictionary<string, Dictionary<string, (double Sum, int Count)>>();
        var firstDate = new Dictionary<string, DateTime>();
        for (int i = 0; i < n; i++)
        {
            if (!dr[i].HasValue || lr[i] is null || !mr[i].HasValue) continue;
            string pk = PeriodAggregator.PeriodKey(dr[i]!.Value, gran);
            string dv = lr[i]!;
            if (!firstDate.ContainsKey(pk)) firstDate[pk] = dr[i]!.Value;
            if (!acc.TryGetValue(pk, out var dimAcc)) acc[pk] = dimAcc = new Dictionary<string, (double, int)>();
            dimAcc.TryGetValue(dv, out var a);
            dimAcc[dv] = (a.Sum + mr[i]!.Value, a.Count + 1);
        }
        if (acc.Count < 3 || acc.Count > MaxDateCategories) return null;

        var periods = acc.Keys.OrderBy(k => firstDate[k]).ToList();

        // Top series by total; minimum 2 distinct series to justify a stacked chart.
        var seriesTotals = new Dictionary<string, double>();
        foreach (var (_, dimAcc) in acc)
        foreach (var (dv, a) in dimAcc)
        {
            seriesTotals.TryGetValue(dv, out var t);
            seriesTotals[dv] = t + a.Sum;
        }
        var seriesNames = seriesTotals.OrderByDescending(kv => kv.Value)
            .Take(MaxSeriesValues).Select(kv => kv.Key).ToList();
        if (seriesNames.Count < 2) return null;

        var values = seriesNames.Select(sn =>
            (IReadOnlyList<double?>)periods.Select(pk =>
            {
                if (!acc[pk].TryGetValue(sn, out var a)) return (double?)null;
                return (double?)a.Sum;
            }).ToList()
        ).ToList();

        // Reject sparse one-hot breakdowns (e.g. one segment per period) that stack into a jagged mess.
        if (!HasEnoughCoverage(values, periods.Count)) return null;

        return new CompositionResult
        {
            Measure = measure.Name,
            CategoryDimension = dateCol.Name,
            SeriesDimension = dim.Name,
            IsAdditive = additive,
            IsDateCategory = true,
            Granularity = gran,
            Categories = periods,
            CategoryDates = periods.Select(pk => firstDate[pk]).ToList(),
            SeriesNames = seriesNames,
            Values = values
        };
    }

    private static CompositionResult? BuildCatCatResult(
        ColumnStatistics dim1, ColumnStatistics dim2, ColumnStatistics measure)
    {
        // Stacking only makes sense for additive measures.
        bool additive = MeasureSemantics.IsAdditive(measure.Profile, measure.Numeric?.Min, measure.Numeric?.Max);
        if (!additive) return null;

        var triples = RowAlignment.LabelLabelValues(dim1, dim2, measure);
        if (triples.Count == 0) return null;

        // Aggregate by (dim1 value, dim2 value).
        var acc = new Dictionary<(string, string), (double Sum, int Count)>();
        foreach (var (d1, d2, v) in triples)
        {
            var key = (d1, d2);
            acc.TryGetValue(key, out var a);
            acc[key] = (a.Sum + v, a.Count + 1);
        }

        // Categories (X axis): dim1 values ordered by total descending.
        var cat1Totals = acc.GroupBy(kv => kv.Key.Item1)
            .ToDictionary(g => g.Key, g => g.Sum(kv => kv.Value.Sum));
        var categories = cat1Totals.OrderByDescending(kv => kv.Value)
            .Take(MaxCategoryValues).Select(kv => kv.Key).ToList();
        if (categories.Count < 2) return null;

        // Series: dim2 values ordered by total descending, capped at MaxSeriesValues.
        var cat2Totals = acc.GroupBy(kv => kv.Key.Item2)
            .ToDictionary(g => g.Key, g => g.Sum(kv => kv.Value.Sum));
        var seriesNames = cat2Totals.OrderByDescending(kv => kv.Value)
            .Take(MaxSeriesValues).Select(kv => kv.Key).ToList();
        if (seriesNames.Count < 2) return null;

        var values = seriesNames.Select(sn =>
            (IReadOnlyList<double?>)categories.Select(cat =>
            {
                var key = (cat, sn);
                if (!acc.TryGetValue(key, out var a)) return (double?)null;
                return (double?)a.Sum;
            }).ToList()
        ).ToList();

        // Reject sparse cross-tabs where categories rarely share more than one series.
        if (!HasEnoughCoverage(values, categories.Count)) return null;

        return new CompositionResult
        {
            Measure = measure.Name,
            CategoryDimension = dim1.Name,
            SeriesDimension = dim2.Name,
            IsAdditive = additive,
            IsDateCategory = false,
            Granularity = DateGranularity.Unknown,
            Categories = categories,
            SeriesNames = seriesNames,
            Values = values
        };
    }

    // A stacked chart is only honest when most categories actually carry several series at once.
    // Require, on average, at least two populated series per category — this rules out one-hot /
    // rotating data (one segment present per period) that would stack into a jagged single band.
    private static bool HasEnoughCoverage(IReadOnlyList<IReadOnlyList<double?>> values, int categoryCount)
    {
        if (categoryCount == 0) return false;
        int filled = 0;
        foreach (var row in values)
            foreach (var v in row)
                if (v.HasValue) filled++;
        return filled >= categoryCount * 2;
    }
}
