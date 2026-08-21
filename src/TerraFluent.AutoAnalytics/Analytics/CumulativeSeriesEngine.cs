using System.Collections.Generic;
using System.Linq;
using TerraFluent.AutoAnalytics.Enums;
using TerraFluent.AutoAnalytics.Profiling;
using TerraFluent.AutoAnalytics.Schema;

namespace TerraFluent.AutoAnalytics.Analytics;

/// <summary>
/// Computes a running cumulative sum for every additive measure over the primary date column.
/// Non-additive attributes (age, rating) are skipped — a running total of averaged rows is meaningless.
/// </summary>
internal sealed class CumulativeSeriesEngine
{
    // Beyond this many periods a category-indexed area crowds the x-axis.
    private const int MaxPeriods = 24;

    public IReadOnlyList<CumulativeSeriesResult> Compute(DatasetProfile profile)
    {
        var results = new List<CumulativeSeriesResult>();
        var date = profile.DateColumns.FirstOrDefault();
        if (date is null) return results;

        var gran = date.Date?.Granularity ?? DateGranularity.Daily;

        foreach (var measure in profile.Measures)
        {
            if (!MeasureSemantics.IsAdditive(measure.Profile, measure.Numeric?.Min, measure.Numeric?.Max))
                continue;

            var ordered = PeriodAggregator.AggregateLabeled(measure, date, gran, additive: true);
            if (ordered.Count < 3 || ordered.Count > MaxPeriods) continue;

            double running = 0;
            var points = new List<(System.DateTime, double)>(ordered.Count);
            foreach (var (period, value) in ordered)
            {
                running += value;
                points.Add((period, running));
            }

            results.Add(new CumulativeSeriesResult
            {
                Measure = measure.Name,
                DateColumn = date.Name,
                FinalTotal = running,
                Granularity = gran,
                Points = points
            });
        }
        return results;
    }
}
