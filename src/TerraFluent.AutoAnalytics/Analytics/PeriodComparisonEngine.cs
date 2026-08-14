using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using TerraFluent.AutoAnalytics.Enums;
using TerraFluent.AutoAnalytics.Profiling;

namespace TerraFluent.AutoAnalytics.Analytics;

/// <summary>One aggregated calendar period and its change from the previous period.</summary>
public sealed class PeriodValue
{
    public string Label { get; init; } = string.Empty;
    public double Value { get; init; }
    /// <summary>Absolute change from the previous period (0 for the first period).</summary>
    public double ChangeAbs { get; init; }
    /// <summary>Fractional change from the previous period (0.12 = +12%); 0 for the first period.</summary>
    public double ChangePct { get; init; }
}

/// <summary>
/// Period-over-period comparison of a measure: the ordered per-period totals plus the latest
/// period-over-period and (when enough history exists) year-over-year change.
/// </summary>
public sealed class PeriodComparisonResult
{
    public string Measure { get; init; } = string.Empty;
    public DateGranularity Granularity { get; init; }
    public IReadOnlyList<PeriodValue> Periods { get; init; } = new List<PeriodValue>();

    /// <summary>Fractional change of the last period vs the immediately preceding one.</summary>
    public double LatestChangePct { get; init; }
    public double LatestChangeAbs { get; init; }

    /// <summary>Year-over-year fractional change of the latest period, or <see langword="null"/> when unavailable.</summary>
    public double? YearOverYearPct { get; init; }

    public bool IsEmpty => Periods.Count < 2;
}

/// <summary>
/// Phase 4e — deterministic period-over-period (MoM/QoQ/YoY) comparison. Buckets a measure's rows
/// by the primary date column's calendar period, sums per period, and computes consecutive changes.
/// No AI; identical input always yields identical results.
/// </summary>
public sealed class PeriodComparisonEngine
{
    /// <summary>
    /// Compares <paramref name="measureName"/> across calendar periods. The granularity defaults to
    /// the date column's inferred cadence but can be overridden. Returns <see langword="null"/> when
    /// there is no date column or too little data.
    /// </summary>
    public PeriodComparisonResult? Compare(DatasetProfile profile, string measureName, DateGranularity? granularity = null)
    {
        var measure = profile.Columns.FirstOrDefault(c =>
            string.Equals(c.Name, measureName, StringComparison.OrdinalIgnoreCase) && c.Numeric is not null);
        var dateColumn = profile.DateColumns.FirstOrDefault();
        if (measure is null || dateColumn is null || dateColumn.Dates.Count == 0) return null;

        DateGranularity g = Normalise(granularity ?? dateColumn.Date?.Granularity ?? DateGranularity.Monthly);

        // Pair each measure value with its row's date (complete-case), so a missing date or value
        // never buckets a value under the wrong period.
        var rows = RowAlignment.DateValues(measure, dateColumn);
        var totals = new Dictionary<string, double>();
        var firstDate = new Dictionary<string, DateTime>();
        foreach (var (date, value) in rows)
        {
            string key = PeriodKey(date, g);
            totals[key] = totals.TryGetValue(key, out var s) ? s + value : value;
            if (!firstDate.ContainsKey(key)) firstDate[key] = date;
        }

        var ordered = totals.Keys.OrderBy(k => firstDate[k]).ToList();
        if (ordered.Count < 2) return null;

        var periods = new List<PeriodValue>(ordered.Count);
        for (int i = 0; i < ordered.Count; i++)
        {
            double value = totals[ordered[i]];
            double prev = i == 0 ? value : totals[ordered[i - 1]];
            double abs = i == 0 ? 0 : value - prev;
            double pct = i == 0 || prev == 0 ? 0 : abs / Math.Abs(prev);
            periods.Add(new PeriodValue { Label = ordered[i], Value = value, ChangeAbs = abs, ChangePct = pct });
        }

        var last = periods[^1];
        double? yoy = YearOverYear(ordered, totals, firstDate, g);

        return new PeriodComparisonResult
        {
            Measure = measure.Name,
            Granularity = g,
            Periods = periods,
            LatestChangeAbs = last.ChangeAbs,
            LatestChangePct = last.ChangePct,
            YearOverYearPct = yoy
        };
    }

    // Compares the latest period to the same period one year earlier, when present.
    private static double? YearOverYear(
        List<string> ordered, Dictionary<string, double> totals, Dictionary<string, DateTime> firstDate, DateGranularity g)
    {
        if (g == DateGranularity.Yearly) return null;
        string latest = ordered[^1];
        string priorYearKey = PeriodKey(firstDate[latest].AddYears(-1), g);
        if (!totals.TryGetValue(priorYearKey, out var priorYearValue) || priorYearValue == 0) return null;
        return (totals[latest] - priorYearValue) / Math.Abs(priorYearValue);
    }

    private static DateGranularity Normalise(DateGranularity g) =>
        g is DateGranularity.Unknown or DateGranularity.Daily or DateGranularity.Weekly
            ? DateGranularity.Monthly
            : g;

    private static string PeriodKey(DateTime d, DateGranularity g) => g switch
    {
        DateGranularity.Yearly    => d.ToString("yyyy", CultureInfo.InvariantCulture),
        DateGranularity.Quarterly => $"{d.Year} Q{(d.Month - 1) / 3 + 1}",
        _                         => d.ToString("yyyy-MM", CultureInfo.InvariantCulture)
    };
}
