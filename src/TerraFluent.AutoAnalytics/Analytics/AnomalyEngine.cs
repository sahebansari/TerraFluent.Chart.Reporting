using System;
using System.Collections.Generic;
using System.Linq;
using TerraFluent.AutoAnalytics.Profiling;
using TerraFluent.AutoAnalytics.Statistics;

namespace TerraFluent.AutoAnalytics.Analytics;

/// <summary>
/// Phase 5 — anomaly detection without ML. Combines the Z-score rule (|z| &gt; 3), the IQR fence
/// (below Q1 − 1.5·IQR or above Q3 + 1.5·IQR) and a sudden-change spike detector over ordered series.
/// </summary>
public sealed class AnomalyEngine
{
    private readonly double _zThreshold;
    private readonly double _iqrMultiplier;

    public AnomalyEngine(double zThreshold = 3.0, double iqrMultiplier = 1.5)
    {
        _zThreshold = zThreshold;
        _iqrMultiplier = iqrMultiplier;
    }

    /// <summary>Detects anomalies in every numeric measure column.</summary>
    public IReadOnlyList<AnomalyResult> Detect(DatasetProfile profile)
    {
        var results = new List<AnomalyResult>();
        foreach (var measure in profile.Measures)
        {
            if (measure.NumericValues.Count < 4 || measure.Numeric is null) continue;
            var anomalies = DetectColumn(measure);
            if (anomalies.Count > 0)
                results.Add(new AnomalyResult
                {
                    Measure = measure.Name,
                    Anomalies = anomalies,
                    MaxMagnitude = anomalies.Max(a => Math.Abs(a.ZScore))
                });
        }
        return results.OrderByDescending(r => r.MaxMagnitude).ToList();
    }

    private List<AnomalyPoint> DetectColumn(ColumnStatistics measure)
    {
        var values = measure.NumericValues;
        var stats = measure.Numeric!;
        double mean = stats.Mean, sd = stats.StdDev;
        double lowerFence = stats.Q1 - _iqrMultiplier * stats.Iqr;
        double upperFence = stats.Q3 + _iqrMultiplier * stats.Iqr;

        var flagged = new Dictionary<int, AnomalyPoint>();

        for (int i = 0; i < values.Count; i++)
        {
            double v = values[i];
            double z = sd == 0 ? 0 : (v - mean) / sd;

            if (sd > 0 && Math.Abs(z) > _zThreshold)
                flagged[i] = new AnomalyPoint { Index = i, Value = v, ZScore = z, Method = "zscore" };
            else if (stats.Iqr > 0 && (v < lowerFence || v > upperFence))
                flagged.TryAdd(i, new AnomalyPoint { Index = i, Value = v, ZScore = z, Method = "iqr" });
        }

        // Sudden change: point-to-point jump exceeding 3x the median absolute step.
        var steps = new List<double>();
        for (int i = 1; i < values.Count; i++) steps.Add(Math.Abs(values[i] - values[i - 1]));
        if (steps.Count > 0)
        {
            double medianStep = DescriptiveStatistics.Median(steps);
            if (medianStep > 0)
            {
                for (int i = 1; i < values.Count; i++)
                {
                    double jump = Math.Abs(values[i] - values[i - 1]);
                    if (jump > 3 * medianStep)
                    {
                        double z = sd == 0 ? 0 : (values[i] - mean) / sd;
                        flagged.TryAdd(i, new AnomalyPoint { Index = i, Value = values[i], ZScore = z, Method = "spike" });
                    }
                }
            }
        }

        return flagged.Values.OrderByDescending(a => Math.Abs(a.ZScore)).ToList();
    }
}
