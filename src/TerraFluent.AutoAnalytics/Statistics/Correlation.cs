using System;
using System.Collections.Generic;

namespace TerraFluent.AutoAnalytics.Statistics;

/// <summary>Pearson and Spearman correlation coefficients over paired numeric samples.</summary>
public static class Correlation
{
    /// <summary>Pearson product-moment correlation (linear). Returns 0 for degenerate input.</summary>
    public static double Pearson(IReadOnlyList<double> x, IReadOnlyList<double> y)
    {
        int n = Math.Min(x.Count, y.Count);
        if (n < 2) return 0;

        double mx = 0, my = 0;
        for (int i = 0; i < n; i++) { mx += x[i]; my += y[i]; }
        mx /= n; my /= n;

        double sxy = 0, sxx = 0, syy = 0;
        for (int i = 0; i < n; i++)
        {
            double dx = x[i] - mx, dy = y[i] - my;
            sxy += dx * dy; sxx += dx * dx; syy += dy * dy;
        }
        double denom = Math.Sqrt(sxx * syy);
        return denom == 0 ? 0 : sxy / denom;
    }

    /// <summary>Spearman rank correlation (monotonic). Ties receive averaged ranks.</summary>
    public static double Spearman(IReadOnlyList<double> x, IReadOnlyList<double> y)
    {
        int n = Math.Min(x.Count, y.Count);
        if (n < 2) return 0;
        var rx = Rank(x, n);
        var ry = Rank(y, n);
        return Pearson(rx, ry);
    }

    private static double[] Rank(IReadOnlyList<double> values, int n)
    {
        var idx = new int[n];
        for (int i = 0; i < n; i++) idx[i] = i;
        Array.Sort(idx, (a, b) => values[a].CompareTo(values[b]));

        var ranks = new double[n];
        int j = 0;
        while (j < n)
        {
            int k = j;
            while (k + 1 < n && values[idx[k + 1]] == values[idx[j]]) k++;
            double avgRank = (j + k) / 2.0 + 1; // average of tied positions, 1-based
            for (int t = j; t <= k; t++) ranks[idx[t]] = avgRank;
            j = k + 1;
        }
        return ranks;
    }
}
