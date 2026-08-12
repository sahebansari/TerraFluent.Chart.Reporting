using System;
using System.Collections.Generic;

namespace TerraFluent.AutoAnalytics.Statistics;

/// <summary>
/// Deterministic descriptive statistics over a numeric sample. All methods are pure and
/// culture-independent. Quartiles use the inclusive (Tukey) linear-interpolation method.
/// </summary>
public static class DescriptiveStatistics
{
    /// <summary>Arithmetic mean, or 0 for an empty sample.</summary>
    public static double Mean(IReadOnlyList<double> values)
    {
        if (values.Count == 0) return 0;
        double sum = 0;
        for (int i = 0; i < values.Count; i++) sum += values[i];
        return sum / values.Count;
    }

    /// <summary>Median of the (unsorted) sample.</summary>
    public static double Median(IReadOnlyList<double> values)
    {
        if (values.Count == 0) return 0;
        var sorted = Sorted(values);
        return Percentile(sorted, 50);
    }

    /// <summary>Most frequent value; ties resolved by smallest value. NaN when empty.</summary>
    public static double Mode(IReadOnlyList<double> values)
    {
        if (values.Count == 0) return double.NaN;
        var counts = new Dictionary<double, int>();
        foreach (var v in values) counts[v] = counts.TryGetValue(v, out var c) ? c + 1 : 1;
        double best = double.NaN; int bestCount = -1;
        foreach (var kv in counts)
            if (kv.Value > bestCount || (kv.Value == bestCount && kv.Key < best))
            { best = kv.Key; bestCount = kv.Value; }
        return best;
    }

    /// <summary>Sample variance (n-1 denominator). 0 for fewer than 2 values.</summary>
    public static double Variance(IReadOnlyList<double> values)
    {
        if (values.Count < 2) return 0;
        double mean = Mean(values), sum = 0;
        for (int i = 0; i < values.Count; i++) { double d = values[i] - mean; sum += d * d; }
        return sum / (values.Count - 1);
    }

    /// <summary>Sample standard deviation.</summary>
    public static double StdDev(IReadOnlyList<double> values) => Math.Sqrt(Variance(values));

    /// <summary>The p-th percentile (0..100) of an already-sorted sample via linear interpolation.</summary>
    public static double Percentile(IReadOnlyList<double> sorted, double p)
    {
        int n = sorted.Count;
        if (n == 0) return 0;
        if (n == 1) return sorted[0];
        double rank = (p / 100.0) * (n - 1);
        int lo = (int)Math.Floor(rank);
        int hi = (int)Math.Ceiling(rank);
        if (lo == hi) return sorted[lo];
        double frac = rank - lo;
        return sorted[lo] + frac * (sorted[hi] - sorted[lo]);
    }

    /// <summary>Fisher-Pearson sample skewness. 0 for fewer than 3 values.</summary>
    public static double Skewness(IReadOnlyList<double> values)
    {
        int n = values.Count;
        if (n < 3) return 0;
        double mean = Mean(values);
        double sd = StdDev(values);
        if (sd == 0) return 0;
        double sum = 0;
        for (int i = 0; i < n; i++) { double z = (values[i] - mean) / sd; sum += z * z * z; }
        return (double)n / ((n - 1) * (n - 2)) * sum;
    }

    /// <summary>Excess kurtosis (0 = normal). 0 for fewer than 4 values.</summary>
    public static double Kurtosis(IReadOnlyList<double> values)
    {
        int n = values.Count;
        if (n < 4) return 0;
        double mean = Mean(values);
        double sd = StdDev(values);
        if (sd == 0) return 0;
        double sum = 0;
        for (int i = 0; i < n; i++) { double z = (values[i] - mean) / sd; sum += z * z * z * z; }
        double a = (double)(n * (n + 1)) / ((n - 1) * (n - 2) * (n - 3));
        double b = 3.0 * (n - 1) * (n - 1) / ((n - 2) * (n - 3));
        return a * sum - b;
    }

    /// <summary>Returns a sorted copy of the sample.</summary>
    public static double[] Sorted(IReadOnlyList<double> values)
    {
        var arr = new double[values.Count];
        for (int i = 0; i < values.Count; i++) arr[i] = values[i];
        Array.Sort(arr);
        return arr;
    }
}
