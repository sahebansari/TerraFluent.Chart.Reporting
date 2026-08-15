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
        double autocorr = Lag1Autocorrelation(series);
        int seasonLength = Forecasting.DetectSeasonLength(series);

        var kind = ClassifyTrend(fit, robustSlope, growth, volatility, seasonLength);

        return new TrendResult
        {
            Measure = measure,
            OrderedBy = orderedBy,
            Kind = kind,
            Slope = fit.Slope,
            RSquared = fit.RSquared,
            GrowthRate = double.IsInfinity(growth) ? 0 : growth,
            Volatility = volatility,
            PointCount = series.Count,
            Autocorrelation = autocorr,
            SeasonLength = seasonLength
        };
    }

    // Lag-1 autocorrelation: how strongly each period resembles the one before it. Near 0 for white
    // noise (a random per-record attribute averaged per day), high for persistent/trending series.
    private static double Lag1Autocorrelation(IReadOnlyList<double> series)
    {
        int n = series.Count;
        if (n < 3) return 0;
        double mean = DescriptiveStatistics.Mean(series);
        double num = 0, denom = 0;
        for (int i = 0; i < n; i++) { double d = series[i] - mean; denom += d * d; }
        if (denom <= 0) return 0;
        for (int i = 1; i < n; i++) num += (series[i] - mean) * (series[i - 1] - mean);
        return num / denom;
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

    private static TrendKind ClassifyTrend(LinearFit fit, double robustSlope, double growth, double volatility, int seasonLength)
    {
        // Strong linear fit => directional trend.
        if (fit.RSquared >= 0.5)
        {
            if (growth > 0.05) return TrendKind.Rising;
            if (growth < -0.05) return TrendKind.Declining;
            return TrendKind.Stable;
        }

        // Even under a weak linear fit, a clear net move whose direction the robust Theil–Sen slope
        // agrees with is a genuine trend — and takes precedence over a short alternation that the
        // autocorrelation function might otherwise read as a "season".
        if (Math.Abs(growth) >= 0.05 && Math.Sign(robustSlope) == Math.Sign(growth))
            return growth > 0 ? TrendKind.Rising : TrendKind.Declining;

        // No net direction: a repeating cycle of at least three periods is real seasonality (a
        // length-2 flip-flop is treated as volatility/noise below, not a calendar season).
        if (seasonLength >= 3) return TrendKind.Seasonal;
        // High dispersion with no direction => volatile; otherwise broadly flat.
        if (volatility >= 0.35) return TrendKind.Volatile;
        return TrendKind.Stable;
    }
}

