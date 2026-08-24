using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using TerraFluent.AutoAnalytics.Enums;
using TerraFluent.AutoAnalytics.Profiling;

namespace TerraFluent.AutoAnalytics.Analytics;

/// <summary>
/// Collapses a measure into an evenly-spaced, chronologically-ordered per-period series so that
/// trend/forecast maths operate on one value per calendar period rather than raw per-row values
/// (which are unevenly spaced and duplicated when a period spans many rows). Additive measures are
/// summed per period; non-additive per-row attributes are averaged.
/// </summary>
internal static class PeriodAggregator
{
    /// <summary>
    /// Per-period values in chronological order. When <paramref name="date"/> is <see langword="null"/>
    /// the measure's present values are returned in row order (no aggregation possible).
    /// </summary>
    public static List<double> Aggregate(ColumnStatistics measure, ColumnStatistics? date, DateGranularity g, bool additive)
    {
        var rows = RowAlignment.DateValues(measure, date);
        if (date is null)
            return rows.Select(r => r.Value).ToList();

        var acc = new Dictionary<string, (double Sum, int Count, DateTime First)>();
        foreach (var (d, v) in rows)
        {
            string key = PeriodKey(d, g);
            if (acc.TryGetValue(key, out var a))
                acc[key] = (a.Sum + v, a.Count + 1, a.First);
            else
                acc[key] = (v, 1, d);
        }

        return acc.Values
            .OrderBy(a => a.First)
            .Select(a => additive ? a.Sum : a.Sum / a.Count)
            .ToList();
    }

    /// <summary>
    /// Per-period (anchor date, aggregated value) pairs in chronological order — one entry per
    /// distinct calendar period so a time-series chart plots one point per period rather than one
    /// per row. Additive measures are summed per period; non-additive attributes are averaged.
    /// </summary>
    public static List<(DateTime Period, double Value)> AggregateLabeled(ColumnStatistics measure, ColumnStatistics? date, DateGranularity g, bool additive)
    {
        var rows = RowAlignment.DateValues(measure, date);
        if (date is null)
            return rows.Select(r => (r.Date, r.Value)).ToList();

        var acc = new Dictionary<string, (double Sum, int Count, DateTime First)>();
        foreach (var (d, v) in rows)
        {
            string key = PeriodKey(d, g);
            if (acc.TryGetValue(key, out var a))
                acc[key] = (a.Sum + v, a.Count + 1, a.First);
            else
                acc[key] = (v, 1, d);
        }

        return acc.Values
            .OrderBy(a => a.First)
            .Select(a => (a.First, additive ? a.Sum : a.Sum / a.Count))
            .ToList();
    }

    // Groups a date into a calendar-period key. Unknown/Daily granularity keys by full day so each
    // distinct date is its own period (identity when the source already has one row per day).
    internal static string PeriodKey(DateTime d, DateGranularity g) => g switch
    {
        DateGranularity.Yearly    => d.ToString("yyyy", CultureInfo.InvariantCulture),
        DateGranularity.Quarterly => $"{d.Year} Q{(d.Month - 1) / 3 + 1}",
        DateGranularity.Monthly   => d.ToString("yyyy-MM", CultureInfo.InvariantCulture),
        DateGranularity.Weekly    => WeekKey(d),
        _                         => d.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)
    };

    private static string WeekKey(DateTime d)
    {
        // Use the ISO-8601 week-numbering year, not the calendar year, so a date in the first days
        // of January that belongs to the last ISO week of the prior year (or vice-versa) is keyed to
        // the correct year — otherwise e.g. 2021-01-01 (ISO 2020-W53) would mis-bucket as 2021-W53.
        int week = ISOWeek.GetWeekOfYear(d);
        int year = ISOWeek.GetYear(d);
        return $"{year}-W{week:00}";
    }
}
