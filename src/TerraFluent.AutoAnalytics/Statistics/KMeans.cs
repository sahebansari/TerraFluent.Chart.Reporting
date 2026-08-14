using System;
using System.Collections.Generic;
using System.Linq;

namespace TerraFluent.AutoAnalytics.Statistics;

/// <summary>The outcome of a k-means run: per-point cluster assignments and cluster centroids.</summary>
public sealed class KMeansResult
{
    /// <summary>Cluster index (0..k-1) for each input point, in input order.</summary>
    public IReadOnlyList<int> Assignments { get; init; } = Array.Empty<int>();

    /// <summary>Centroid coordinates for each cluster (k rows × d dimensions).</summary>
    public IReadOnlyList<double[]> Centroids { get; init; } = Array.Empty<double[]>();

    /// <summary>Number of points assigned to each cluster.</summary>
    public IReadOnlyList<int> Sizes { get; init; } = Array.Empty<int>();

    /// <summary>Total within-cluster sum of squares (lower = tighter clusters).</summary>
    public double Inertia { get; init; }

    public int K => Centroids.Count;
}

/// <summary>
/// Deterministic k-means clustering. Determinism is guaranteed by seeding k-means++ initialisation
/// with a fixed RNG seed and iterating to convergence, so identical input always yields identical
/// clusters. Features are z-score standardised internally so no single column dominates the distance.
/// </summary>
public static class KMeans
{
    private const int Seed = 12345;

    /// <summary>
    /// Clusters <paramref name="points"/> (each an equal-length feature vector) into
    /// <paramref name="k"/> groups. Returns an empty result when there are fewer points than k.
    /// </summary>
    public static KMeansResult Cluster(IReadOnlyList<double[]> points, int k, int maxIterations = 100)
    {
        if (points is null || points.Count == 0 || k < 1 || points.Count < k)
            return new KMeansResult();

        int d = points[0].Length;
        double[][] data = Standardise(points, d, out double[] mean, out double[] std);

        var rng = new Random(Seed);
        double[][] centroids = InitPlusPlus(data, k, d, rng);
        var assignments = new int[data.Length];

        for (int iter = 0; iter < maxIterations; iter++)
        {
            bool changed = false;
            for (int i = 0; i < data.Length; i++)
            {
                int nearest = Nearest(data[i], centroids);
                if (nearest != assignments[i]) { assignments[i] = nearest; changed = true; }
            }

            var sums = new double[k][];
            var counts = new int[k];
            for (int c = 0; c < k; c++) sums[c] = new double[d];
            for (int i = 0; i < data.Length; i++)
            {
                int c = assignments[i];
                counts[c]++;
                for (int j = 0; j < d; j++) sums[c][j] += data[i][j];
            }
            for (int c = 0; c < k; c++)
                if (counts[c] > 0)
                    for (int j = 0; j < d; j++) centroids[c][j] = sums[c][j] / counts[c];

            if (!changed && iter > 0) break;
        }

        double inertia = 0;
        var sizes = new int[k];
        for (int i = 0; i < data.Length; i++)
        {
            sizes[assignments[i]]++;
            inertia += SquaredDistance(data[i], centroids[assignments[i]]);
        }

        // De-standardise centroids back to original units for interpretability.
        var realCentroids = new double[k][];
        for (int c = 0; c < k; c++)
        {
            realCentroids[c] = new double[d];
            for (int j = 0; j < d; j++) realCentroids[c][j] = centroids[c][j] * std[j] + mean[j];
        }

        return new KMeansResult
        {
            Assignments = assignments,
            Centroids = realCentroids,
            Sizes = sizes,
            Inertia = inertia
        };
    }

    private static double[][] Standardise(IReadOnlyList<double[]> points, int d, out double[] mean, out double[] std)
    {
        mean = new double[d];
        std = new double[d];
        foreach (var p in points)
            for (int j = 0; j < d; j++) mean[j] += p[j];
        for (int j = 0; j < d; j++) mean[j] /= points.Count;

        foreach (var p in points)
            for (int j = 0; j < d; j++) std[j] += (p[j] - mean[j]) * (p[j] - mean[j]);
        for (int j = 0; j < d; j++) std[j] = Math.Sqrt(std[j] / points.Count);
        for (int j = 0; j < d; j++) if (std[j] == 0) std[j] = 1; // avoid divide-by-zero for constant columns

        var data = new double[points.Count][];
        for (int i = 0; i < points.Count; i++)
        {
            data[i] = new double[d];
            for (int j = 0; j < d; j++) data[i][j] = (points[i][j] - mean[j]) / std[j];
        }
        return data;
    }

    private static double[][] InitPlusPlus(double[][] data, int k, int d, Random rng)
    {
        var centroids = new double[k][];
        centroids[0] = (double[])data[rng.Next(data.Length)].Clone();

        var dist = new double[data.Length];
        for (int c = 1; c < k; c++)
        {
            double total = 0;
            for (int i = 0; i < data.Length; i++)
            {
                double best = double.MaxValue;
                for (int cc = 0; cc < c; cc++) best = Math.Min(best, SquaredDistance(data[i], centroids[cc]));
                dist[i] = best;
                total += best;
            }

            double target = rng.NextDouble() * total, cum = 0;
            int chosen = data.Length - 1;
            for (int i = 0; i < data.Length; i++)
            {
                cum += dist[i];
                if (cum >= target) { chosen = i; break; }
            }
            centroids[c] = (double[])data[chosen].Clone();
        }
        return centroids;
    }

    private static int Nearest(double[] point, double[][] centroids)
    {
        int best = 0;
        double bestDist = double.MaxValue;
        for (int c = 0; c < centroids.Length; c++)
        {
            double dist = SquaredDistance(point, centroids[c]);
            if (dist < bestDist) { bestDist = dist; best = c; }
        }
        return best;
    }

    private static double SquaredDistance(double[] a, double[] b)
    {
        double sum = 0;
        for (int j = 0; j < a.Length; j++) { double diff = a[j] - b[j]; sum += diff * diff; }
        return sum;
    }
}
