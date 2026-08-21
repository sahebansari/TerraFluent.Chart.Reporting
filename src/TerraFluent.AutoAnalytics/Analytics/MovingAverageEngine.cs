using System;
using System.Collections.Generic;
using System.Linq;
using TerraFluent.AutoAnalytics.Enums;
using TerraFluent.AutoAnalytics.Profiling;
using TerraFluent.AutoAnalytics.Schema;

namespace TerraFluent.AutoAnalytics.Analytics;

/// <summary>
/// Computes an adaptive simple moving average for every measure over the primary date column.
/// Window size scales with series length so short series still produce a readable smooth curve.
/// </summary>
internal sealed class MovingAverageEngine
{
    // Beyond this many periods a category-indexed spline crowds the x-axis; the plain trend line covers it.
    private const int MaxPeriods = 24;

    public IReadOnlyList<MovingAverageResult> Compute(DatasetProfile profile)
    {
        var results = new List<MovingAverageResult>();
        var date = profile.DateColumns.FirstOrDefault();
        if (date is null) return results;

        var gran = date.Date?.Granularity ?? DateGranularity.Daily;

        foreach (var measure in profile.Measures)
        {
            bool additive = MeasureSemantics.IsAdditive(measure.Profile, measure.Numeric?.Min, measure.Numeric?.Max);
            var ordered = PeriodAggregator.AggregateLabeled(measure, date, gran, additive);
            int n = ordered.Count;
            // Need at least twice the minimum window (3) so there are meaningful smoothed points.
            if (n < 8 || n > MaxPeriods) continue;

            // Window = n/5 clamped to [3, 12] — wide enough to smooth noise, narrow enough to follow turns.
            int window = Math.Max(3, Math.Min(12, n / 5));
            var raw = ordered.Select(p => p.Value).ToList();

            var points = new List<(DateTime, double, double?)>(n);
            for (int i = 0; i < n; i++)
            {
                double? smoothed = null;
                if (i >= window - 1)
                {
                    double sum = 0;
                    for (int k = i - window + 1; k <= i; k++) sum += raw[k];
                    smoothed = sum / window;
                }
                points.Add((ordered[i].Period, raw[i], smoothed));
            }

            results.Add(new MovingAverageResult
            {
                Measure = measure.Name,
                DateColumn = date.Name,
                WindowSize = window,
                Granularity = gran,
                Points = points
            });
        }
        return results;
    }
}
