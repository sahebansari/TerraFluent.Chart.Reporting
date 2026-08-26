using System;
using System.Collections.Generic;
using System.Linq;
using TerraFluent.AutoAnalytics.Enums;
using TerraFluent.AutoAnalytics.Profiling;
using TerraFluent.AutoAnalytics.Schema;
using TerraFluent.AutoAnalytics.Statistics;

namespace TerraFluent.AutoAnalytics.Analytics;

/// <summary>One measure projected forward, with the per-period history the projection was fitted on.</summary>
public sealed class MeasureForecast
{
    public string Measure { get; init; } = string.Empty;

    /// <summary>Calendar-period labels of the fitted history (e.g. <c>2024-01</c>).</summary>
    public IReadOnlyList<string> HistoryLabels { get; init; } = Array.Empty<string>();

    /// <summary>Per-period history values, chronologically ordered.</summary>
    public IReadOnlyList<double> HistoryValues { get; init; } = Array.Empty<double>();

    /// <summary>Calendar cadence the history was collapsed to.</summary>
    public DateGranularity Granularity { get; init; }

    /// <summary>The projected points and confidence band.</summary>
    public ForecastResult Forecast { get; init; } = new();

    public bool IsEmpty => Forecast.IsEmpty;
}

/// <summary>
/// Phase 4f — projects each measure forward from its per-period history using
/// <see cref="Statistics.Forecasting"/> (Holt / Holt-Winters). Like the trend engine, measures are
/// first collapsed to one value per calendar period so the maths sees an evenly-spaced series.
/// Deterministic: identical input always yields identical projections.
/// </summary>
public sealed class ForecastEngine
{
    /// <summary>Default number of periods projected forward.</summary>
    public const int DefaultHorizon = 3;

    /// <summary>Shortest history <see cref="Statistics.Forecasting"/> will fit.</summary>
    private const int MinPoints = 4;

    private readonly int _horizon;
    private readonly int _maxMeasures;

    public ForecastEngine(int horizon = DefaultHorizon, int maxMeasures = 6)
    {
        _horizon = horizon < 1 ? DefaultHorizon : horizon;
        _maxMeasures = maxMeasures < 1 ? 1 : maxMeasures;
    }

    /// <summary>Projects every measure with enough per-period history; skips those without.</summary>
    public IReadOnlyList<MeasureForecast> Compute(DatasetProfile profile)
    {
        if (profile is null) throw new ArgumentNullException(nameof(profile));

        var date = profile.DateColumns.FirstOrDefault();
        var granularity = date?.Date?.Granularity ?? DateGranularity.Unknown;

        var results = new List<MeasureForecast>();
        foreach (var measure in profile.Measures.Take(_maxMeasures))
        {
            if (measure.Numeric is null) continue;

            bool additive = MeasureSemantics.IsAdditive(
                measure.Profile, measure.Numeric.Min, measure.Numeric.Max);

            var periods = PeriodAggregator.AggregateLabeled(measure, date, granularity, additive);
            if (periods.Count < MinPoints) continue;

            var values = periods.Select(p => p.Value).ToList();
            // Season length is auto-detected from the series' own autocorrelation (which already
            // favours calendar lags). Forcing the calendar cadence as a hint would push every
            // monthly series through Holt-Winters, seasonal or not, and a seasonal model fitted to
            // a non-seasonal ramp projects worse than the plain linear one.
            var forecast = Forecasting.Forecast(values, _horizon);
            if (forecast.IsEmpty) continue;

            results.Add(new MeasureForecast
            {
                Measure       = measure.Name,
                HistoryLabels = periods.Select(p => PeriodAggregator.PeriodKey(p.Period, granularity)).ToList(),
                HistoryValues = values,
                Granularity   = granularity,
                Forecast      = forecast
            });
        }
        return results;
    }

}
