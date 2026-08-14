using System;
using System.Collections.Generic;
using System.Linq;
using TerraFluent.AutoAnalytics.Enums;
using TerraFluent.AutoAnalytics.Profiling;
using TerraFluent.AutoAnalytics.Schema;
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
        var granularity = dateColumn?.Date?.Granularity ?? DateGranularity.Unknown;

        foreach (var measure in profile.Measures)
        {
            var series = OrderSeries(measure, dateColumn, granularity);
            if (series.Count < 3) continue;
            results.Add(Analyse(measure.Name, dateColumn?.Name, series));
        }
        return results.OrderByDescending(r => Math.Abs(r.GrowthRate)).ToList();
    }

    private static List<double> OrderSeries(ColumnStatistics measure, ColumnStatistics? dateColumn, DateGranularity granularity)
    {
        // Collapse to one value per calendar period (summing additive measures, averaging others)
        // so the regression sees an evenly-spaced series instead of noisy, duplicated per-row values.
        // Complete-case row alignment inside the aggregator keeps missing cells from misaligning.
        bool additive = MeasureSemantics.IsAdditive(measure.Profile, measure.Numeric?.Min, measure.Numeric?.Max);
        return PeriodAggregator.Aggregate(measure, dateColumn, granularity, additive);
    }

    private static TrendResult Analyse(string measure, string? orderedBy, IReadOnlyList<double> series)
    {
        var fit = Regression.FitOverIndex(series);
        double robustSlope = Regression.TheilSenSlope(series);
        double growth = FittedGrowth(fit, series.Count);
        double mean = DescriptiveStatistics.Mean(series);
        double sd = DescriptiveStatistics.StdDev(series);
        double volatility = mean == 0 ? 0 : Math.Abs(sd / mean);

        var kind = ClassifyTrend(fit, robustSlope, growth, volatility, series);

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

    // Growth from the fitted trend line (predicted end vs predicted start) rather than the raw
    // first/last points — robust to a noisy or outlying endpoint that would otherwise dominate.
    private static double FittedGrowth(LinearFit fit, int n)
    {
        if (n < 2) return 0;
        double start = fit.Predict(0);
        double end   = fit.Predict(n - 1);
        if (start == 0) return end == 0 ? 0 : (end > 0 ? double.PositiveInfinity : double.NegativeInfinity);
        return (end - start) / Math.Abs(start);
    }

    private static TrendKind ClassifyTrend(LinearFit fit, double robustSlope, double growth, double volatility, IReadOnlyList<double> series)
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
        // Weak/noisy fit: use the robust Theil–Sen slope sign for direction so a single outlier
        // can't flip Rising/Declining the way an endpoint-driven growth figure might.
        if (Math.Abs(growth) < 0.05) return TrendKind.Stable;
        return robustSlope > 0 ? TrendKind.Rising : TrendKind.Declining;
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
