using System;
using System.Collections.Generic;

namespace TerraFluent.AutoAnalytics.Statistics;

/// <summary>Ordinary least-squares fit of <c>y = Slope·x + Intercept</c> plus goodness-of-fit.</summary>
public readonly struct LinearFit
{
    public double Slope { get; }
    public double Intercept { get; }
    /// <summary>Coefficient of determination R² (0..1).</summary>
    public double RSquared { get; }

    public LinearFit(double slope, double intercept, double rSquared)
    {
        Slope = slope; Intercept = intercept; RSquared = rSquared;
    }

    /// <summary>Predicts y for a given x.</summary>
    public double Predict(double x) => Slope * x + Intercept;
}

/// <summary>Simple linear regression and smoothing helpers used by the trend engine.</summary>
public static class Regression
{
    /// <summary>Fits a least-squares line to (x, y) pairs.</summary>
    public static LinearFit Fit(IReadOnlyList<double> x, IReadOnlyList<double> y)
    {
        int n = Math.Min(x.Count, y.Count);
        if (n < 2) return new LinearFit(0, n == 1 ? y[0] : 0, 0);

        double mx = 0, my = 0;
        for (int i = 0; i < n; i++) { mx += x[i]; my += y[i]; }
        mx /= n; my /= n;

        double sxy = 0, sxx = 0, syy = 0;
        for (int i = 0; i < n; i++)
        {
            double dx = x[i] - mx, dy = y[i] - my;
            sxy += dx * dy; sxx += dx * dx; syy += dy * dy;
        }
        double slope = sxx == 0 ? 0 : sxy / sxx;
        double intercept = my - slope * mx;
        double r2 = (sxx == 0 || syy == 0) ? 0 : (sxy * sxy) / (sxx * syy);
        return new LinearFit(slope, intercept, r2);
    }

    /// <summary>Fits a line over the natural index 0..n-1 (used for evenly-spaced series).</summary>
    public static LinearFit FitOverIndex(IReadOnlyList<double> y)
    {
        var x = new double[y.Count];
        for (int i = 0; i < y.Count; i++) x[i] = i;
        return Fit(x, y);
    }

    /// <summary>
    /// Theil–Sen robust slope over the natural index: the median of the slopes between all point
    /// pairs. Unlike least squares it is insensitive to outliers (≈29% breakdown point). For large
    /// series the pair set is thinned by a stride so the cost stays bounded while the estimate holds.
    /// </summary>
    public static double TheilSenSlope(IReadOnlyList<double> y)
    {
        int n = y.Count;
        if (n < 2) return 0;

        // Cap the work at ~O(300²) pairs; stride the indices deterministically when larger.
        int stride = n <= 300 ? 1 : (int)Math.Ceiling(n / 300.0);
        var slopes = new List<double>();
        for (int i = 0; i < n; i += stride)
        for (int j = i + stride; j < n; j += stride)
            slopes.Add((y[j] - y[i]) / (j - i));

        if (slopes.Count == 0) return 0;
        slopes.Sort();
        int mid = slopes.Count / 2;
        return slopes.Count % 2 == 0 ? (slopes[mid - 1] + slopes[mid]) / 2.0 : slopes[mid];
    }

    /// <summary>Centered/trailing simple moving average with the given window (≥1).</summary>
    public static double[] MovingAverage(IReadOnlyList<double> values, int window)
    {
        if (window < 1) window = 1;
        var result = new double[values.Count];
        double sum = 0;
        var queue = new Queue<double>(window);
        for (int i = 0; i < values.Count; i++)
        {
            queue.Enqueue(values[i]); sum += values[i];
            if (queue.Count > window) sum -= queue.Dequeue();
            result[i] = sum / queue.Count;
        }
        return result;
    }

    /// <summary>Period-over-period growth rate from first to last non-zero value.</summary>
    public static double GrowthRate(IReadOnlyList<double> values)
    {
        if (values.Count < 2) return 0;
        double first = values[0], last = values[values.Count - 1];
        if (first == 0) return last == 0 ? 0 : double.PositiveInfinity;
        return (last - first) / Math.Abs(first);
    }
}
