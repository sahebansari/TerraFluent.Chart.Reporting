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

    /// <summary>
    /// Two-tailed p-value for a correlation coefficient <paramref name="r"/> over <paramref name="n"/>
    /// paired observations, under the null hypothesis of no correlation. Uses the Student's t test
    /// <c>t = r·√((n−2)/(1−r²))</c> with <c>n−2</c> degrees of freedom. Returns 1 when the sample is
    /// too small to test. A small p-value (&lt; 0.05) means the correlation is unlikely to be noise.
    /// </summary>
    public static double PValue(double r, int n)
    {
        if (n < 3) return 1.0;
        double rAbs = Math.Min(Math.Abs(r), 0.999999999999);
        double df = n - 2;
        double t2 = rAbs * rAbs * df / (1.0 - rAbs * rAbs);
        // Two-tailed p = I_{df/(df+t²)}(df/2, 1/2), the regularised incomplete beta function.
        double x = df / (df + t2);
        return RegularizedIncompleteBeta(df / 2.0, 0.5, x);
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

    // ── Regularised incomplete beta function I_x(a,b) (Numerical Recipes) ─────────────
    // Used for the Student's t-distribution tail that yields the correlation p-value.

    private static double RegularizedIncompleteBeta(double a, double b, double x)
    {
        if (x <= 0) return 0;
        if (x >= 1) return 1;
        double bt = Math.Exp(LogGamma(a + b) - LogGamma(a) - LogGamma(b)
                             + a * Math.Log(x) + b * Math.Log(1 - x));
        // Continued fraction converges fast for x < (a+1)/(a+b+2); use the symmetry relation otherwise.
        return x < (a + 1) / (a + b + 2)
            ? bt * BetaContinuedFraction(a, b, x) / a
            : 1 - bt * BetaContinuedFraction(b, a, 1 - x) / b;
    }

    private static double BetaContinuedFraction(double a, double b, double x)
    {
        const int maxIterations = 200;
        const double epsilon = 3e-14, fpMin = 1e-300;

        double qab = a + b, qap = a + 1, qam = a - 1;
        double c = 1;
        double d = 1 - qab * x / qap;
        if (Math.Abs(d) < fpMin) d = fpMin;
        d = 1 / d;
        double h = d;

        for (int m = 1; m <= maxIterations; m++)
        {
            int m2 = 2 * m;
            double aa = m * (b - m) * x / ((qam + m2) * (a + m2));
            d = 1 + aa * d; if (Math.Abs(d) < fpMin) d = fpMin;
            c = 1 + aa / c; if (Math.Abs(c) < fpMin) c = fpMin;
            d = 1 / d;
            h *= d * c;

            aa = -(a + m) * (qab + m) * x / ((a + m2) * (qap + m2));
            d = 1 + aa * d; if (Math.Abs(d) < fpMin) d = fpMin;
            c = 1 + aa / c; if (Math.Abs(c) < fpMin) c = fpMin;
            d = 1 / d;
            double del = d * c;
            h *= del;
            if (Math.Abs(del - 1) < epsilon) break;
        }
        return h;
    }

    // Lanczos approximation of ln Γ(z) — netstandard2.0 has no Math.LogGamma.
    private static double LogGamma(double z)
    {
        double[] g =
        {
            676.5203681218851, -1259.1392167224028, 771.32342877765313,
            -176.61502916214059, 12.507343278686905, -0.13857109526572012,
            9.9843695780195716e-6, 1.5056327351493116e-7
        };
        if (z < 0.5)
            return Math.Log(Math.PI / Math.Sin(Math.PI * z)) - LogGamma(1 - z);
        z -= 1;
        double a = 0.99999999999980993;
        double t = z + 7.5;
        for (int i = 0; i < g.Length; i++) a += g[i] / (z + i + 1);
        return 0.5 * Math.Log(2 * Math.PI) + (z + 0.5) * Math.Log(t) - t + Math.Log(a);
    }
}
