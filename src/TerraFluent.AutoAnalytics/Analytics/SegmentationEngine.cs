using System;
using System.Collections.Generic;
using System.Linq;
using TerraFluent.AutoAnalytics.Profiling;
using TerraFluent.AutoAnalytics.Statistics;

namespace TerraFluent.AutoAnalytics.Analytics;

/// <summary>A described cluster: its size, share and per-measure centroid, with a readable label.</summary>
public sealed class SegmentProfile
{
    public int Index { get; init; }
    public int Size { get; init; }
    /// <summary>Fraction 0..1 of clustered rows in this segment.</summary>
    public double Share { get; init; }
    /// <summary>Centroid value per measure column.</summary>
    public IReadOnlyDictionary<string, double> Centroid { get; init; } = new Dictionary<string, double>();
    /// <summary>Human-readable label derived from the segment's dominant measure level.</summary>
    public string Label { get; init; } = string.Empty;
}

/// <summary>Deterministic clustering of rows into natural segments across the numeric measures.</summary>
public sealed class SegmentationResult
{
    public IReadOnlyList<string> Measures { get; init; } = new List<string>();
    public IReadOnlyList<SegmentProfile> Segments { get; init; } = new List<SegmentProfile>();
    public int RowsClustered { get; init; }
    public bool IsEmpty => Segments.Count == 0;
}

/// <summary>
/// Phase 4d — segments rows into natural groups over the numeric measures via deterministic k-means.
/// The segment count is chosen from the row count; centroids are reported in original units and
/// labelled by their level on the primary measure. No AI; fully reproducible.
/// </summary>
public sealed class SegmentationEngine
{
    private const int MinRows = 6;

    /// <summary>
    /// Clusters the dataset's measures into segments. Returns <see langword="null"/> when there is
    /// no measure or too few aligned rows to segment.
    /// </summary>
    public SegmentationResult? Segment(DatasetProfile profile, int? kOverride = null)
    {
        var measures = profile.Measures.Where(m => m.NumericValues.Count > 0).ToList();
        if (measures.Count == 0) return null;

        // Build feature vectors from complete-case rows only: a row contributes a point only when
        // EVERY measure is present at that row. Aligning compacted per-column lists by index would
        // pair values from different rows once any measure has a missing cell.
        int rowCount = measures.Min(m => m.NumericByRow.Count);
        var points = new List<double[]>(rowCount);
        for (int i = 0; i < rowCount; i++)
        {
            bool complete = true;
            var vec = new double[measures.Count];
            for (int j = 0; j < measures.Count; j++)
            {
                var cell = measures[j].NumericByRow[i];
                if (!cell.HasValue) { complete = false; break; }
                vec[j] = cell.Value;
            }
            if (complete) points.Add(vec);
        }

        int n = points.Count;
        if (n < MinRows) return null;

        int k = kOverride ?? Math.Clamp(n / 6, 2, 4);
        var result = KMeans.Cluster(points, k);
        if (result.K == 0) return null;

        var names = measures.Select(m => m.Name).ToList();
        int primary = 0; // label segments by their level on the first measure

        // Rank clusters by primary-measure centroid to assign High/Mid/Low labels.
        var order = Enumerable.Range(0, result.K)
            .OrderByDescending(c => result.Centroids[c][primary])
            .ToList();

        var segments = new List<SegmentProfile>(result.K);
        for (int rank = 0; rank < order.Count; rank++)
        {
            int c = order[rank];
            var centroid = new Dictionary<string, double>(names.Count, StringComparer.OrdinalIgnoreCase);
            for (int j = 0; j < names.Count; j++) centroid[names[j]] = result.Centroids[c][j];

            segments.Add(new SegmentProfile
            {
                Index = c,
                Size = result.Sizes[c],
                Share = n == 0 ? 0 : (double)result.Sizes[c] / n,
                Centroid = centroid,
                Label = LevelLabel(rank, order.Count, names[primary])
            });
        }

        return new SegmentationResult
        {
            Measures = names,
            Segments = segments.OrderByDescending(s => s.Size).ToList(),
            RowsClustered = n
        };
    }

    private static string LevelLabel(int rank, int count, string measure)
    {
        if (count <= 1) return $"All {measure}";
        if (rank == 0) return $"High {measure}";
        if (rank == count - 1) return $"Low {measure}";
        return count == 3 ? $"Mid {measure}" : $"Level {rank + 1} {measure}";
    }
}
