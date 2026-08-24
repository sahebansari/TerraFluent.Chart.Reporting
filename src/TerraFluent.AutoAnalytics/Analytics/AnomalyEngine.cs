using System;
using System.Collections.Generic;
using System.Linq;
using TerraFluent.AutoAnalytics.Profiling;
using TerraFluent.AutoAnalytics.Statistics;

namespace TerraFluent.AutoAnalytics.Analytics;

/// <summary>
/// Phase 5 — anomaly detection without ML. Combines the robust modified Z-score (median + MAD,
/// Iglewicz &amp; Hoaglin: <c>0.6745·(x − median)/MAD</c>, |Mz| &gt; 3.5), the IQR fence
/// (below Q1 − 1.5·IQR or above Q3 + 1.5·IQR) and a sudden-change spike detector over ordered
/// series. The modified Z-score is preferred over the classic mean/SD Z-score because a single
/// large outlier inflates the mean and SD enough to mask itself (and its neighbours).
/// </summary>
public sealed class AnomalyEngine
{
    private const double ModifiedZThreshold = 3.5; // Iglewicz–Hoaglin recommended cut-off.

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
                    MaxMagnitude = anomalies.Max(a => a.Magnitude)
                });
        }
        return results.OrderByDescending(r => r.MaxMagnitude).ToList();
    }

    private List<AnomalyPoint> DetectColumn(ColumnStatistics measure)
    {
        var values = measure.NumericValues;
        var stats = measure.Numeric!;
        double mean = stats.Mean, sd = stats.StdDev;
        double median = stats.Median;
        double mad = MedianAbsoluteDeviation(values, median);
        double lowerFence = stats.Q1 - _iqrMultiplier * stats.Iqr;
        double upperFence = stats.Q3 + _iqrMultiplier * stats.Iqr;

        // Robust spread on a σ-comparable scale: MAD/0.6745 ≈ σ for normal data, falling back to
        // IQR/1.349 then the (non-robust) StdDev. Used to score/rank so a robustly-detected outlier
        // is measured by a spread it did NOT inflate, instead of the classic Z the outlier deflates.
        double robustScale = mad > 0 ? mad / 0.6745 : stats.Iqr > 0 ? stats.Iqr / 1.349 : sd;

        var flagged = new Dictionary<int, AnomalyPoint>();

        for (int i = 0; i < values.Count; i++)
        {
            double v = values[i];
            double z = sd == 0 ? 0 : (v - mean) / sd;
            double robustZ = robustScale > 0 ? (v - median) / robustScale : 0;
            // Robust modified Z-score: MAD isn't inflated by the outlier itself, so it flags points
            // the classic Z-score would mask. Falls back to the classic rule when MAD collapses to 0
            // (e.g. >50% identical values).
            double modZ = mad > 0 ? 0.6745 * (v - median) / mad : 0;

            if (mad > 0 && Math.Abs(modZ) > ModifiedZThreshold)
                flagged[i] = new AnomalyPoint { Index = i, Value = v, ZScore = z, Magnitude = Math.Abs(robustZ), Method = "modified-zscore" };
            else if (mad == 0 && sd > 0 && Math.Abs(z) > _zThreshold)
                flagged[i] = new AnomalyPoint { Index = i, Value = v, ZScore = z, Magnitude = Math.Abs(robustZ), Method = "zscore" };
            else if (stats.Iqr > 0 && (v < lowerFence || v > upperFence))
                flagged.TryAdd(i, new AnomalyPoint { Index = i, Value = v, ZScore = z, Magnitude = Math.Abs(robustZ), Method = "iqr" });
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
                        double robustZ = robustScale > 0 ? (values[i] - median) / robustScale : 0;
                        flagged.TryAdd(i, new AnomalyPoint { Index = i, Value = values[i], ZScore = z, Magnitude = Math.Abs(robustZ), Method = "spike" });
                    }
                }
            }
        }

        return flagged.Values.OrderByDescending(a => a.Magnitude).ToList();
    }

    // Median of |xᵢ − median| — a robust scale estimate that (unlike the standard deviation) is
    // not inflated by the very outliers being detected.
    private static double MedianAbsoluteDeviation(IReadOnlyList<double> values, double median)
    {
        if (values.Count == 0) return 0;
        var deviations = new double[values.Count];
        for (int i = 0; i < values.Count; i++) deviations[i] = Math.Abs(values[i] - median);
        return DescriptiveStatistics.Median(deviations);
    }
}
