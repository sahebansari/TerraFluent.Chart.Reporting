using System;
using System.Collections.Generic;
using System.Linq;
using TerraFluent.AutoAnalytics.Enums;
using TerraFluent.AutoAnalytics.Profiling;
using TerraFluent.AutoAnalytics.Statistics;

namespace TerraFluent.AutoAnalytics.Analytics;

/// <summary>
/// Phase 4b — trend detection over ordered measure series using linear regression, growth rate
/// and volatility. When a date dimension exists, measures are ordered by it; otherwise natural
/// row order is used.
/// </summary>
public sealed class TrendEngine
{
    /// <summary>Detects a trend for each measure, ordered by the primary date column when present.</summary>
    public IReadOnlyList<TrendResult> DetectTrends(DatasetProfile profile)
    {
        var results = new List<TrendResult>();
        var dateColumn = profile.DateColumns.FirstOrDefault();

        foreach (var measure in profile.Measures)
        {
            var series = OrderSeries(measure, dateColumn);
            if (series.Count < 3) continue;
            results.Add(Analyse(measure.Name, dateColumn?.Name, series));
        }
        return results.OrderByDescending(r => Math.Abs(r.GrowthRate)).ToList();
    }

    private static List<double> OrderSeries(ColumnStatistics measure, ColumnStatistics? dateColumn)
    {
        // Without a date, use the values as-is. With a date, order values by date index.
        if (dateColumn is null || dateColumn.Dates.Count == 0)
            return measure.NumericValues.ToList();

        int n = Math.Min(dateColumn.Dates.Count, measure.NumericValues.Count);
        return Enumerable.Range(0, n)
            .OrderBy(i => dateColumn.Dates[i])
            .Select(i => measure.NumericValues[i])
            .ToList();
    }

    private static TrendResult Analyse(string measure, string? orderedBy, IReadOnlyList<double> series)
    {
        var fit = Regression.FitOverIndex(series);
        double growth = Regression.GrowthRate(series);
        double mean = DescriptiveStatistics.Mean(series);
        double sd = DescriptiveStatistics.StdDev(series);
        double volatility = mean == 0 ? 0 : Math.Abs(sd / mean);

        var kind = ClassifyTrend(fit, growth, volatility, series);

        return new TrendResult
        {
            Measure = measure,
            OrderedBy = orderedBy,
            Kind = kind,
            Slope = fit.Slope,
            RSquared = fit.RSquared,
            GrowthRate = double.IsInfinity(growth) ? 0 : growth,
            Volatility = volatility,
            PointCount = series.Count
        };
    }

    private static TrendKind ClassifyTrend(LinearFit fit, double growth, double volatility, IReadOnlyList<double> series)
    {
        // Strong linear fit => directional trend.
        if (fit.RSquared >= 0.5)
        {
            if (growth > 0.05) return TrendKind.Rising;
            if (growth < -0.05) return TrendKind.Declining;
            return TrendKind.Stable;
        }

        // Weak fit but high dispersion => volatile; low dispersion => stable.
        if (volatility >= 0.35) return TrendKind.Volatile;
        if (HasSeasonalSigns(series)) return TrendKind.Seasonal;
        return Math.Abs(growth) < 0.05 ? TrendKind.Stable
             : growth > 0 ? TrendKind.Rising : TrendKind.Declining;
    }

    // Cheap seasonality heuristic: count direction reversals; many regular reversals ≈ periodic.
    private static bool HasSeasonalSigns(IReadOnlyList<double> series)
    {
        if (series.Count < 6) return false;
        int reversals = 0;
        for (int i = 2; i < series.Count; i++)
        {
            double prev = series[i - 1] - series[i - 2];
            double curr = series[i] - series[i - 1];
            if (prev != 0 && curr != 0 && Math.Sign(prev) != Math.Sign(curr)) reversals++;
        }
        double ratio = (double)reversals / (series.Count - 2);
        return ratio >= 0.4 && ratio <= 0.75;
    }
}
