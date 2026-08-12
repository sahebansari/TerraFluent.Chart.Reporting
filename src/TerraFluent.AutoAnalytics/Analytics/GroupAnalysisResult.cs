using System.Collections.Generic;

namespace TerraFluent.AutoAnalytics.Analytics;

/// <summary>One aggregated group within a group-by analysis (e.g. total Revenue for "North America").</summary>
public sealed class GroupBucket
{
    public string Key { get; init; } = string.Empty;
    public double Value { get; init; }
    /// <summary>Share 0..1 of the grand total.</summary>
    public double Share { get; init; }
}

/// <summary>
/// Result of aggregating a measure by a dimension (e.g. Revenue by Region). Buckets are ordered
/// by descending value.
/// </summary>
public sealed class GroupAnalysisResult
{
    public string Dimension { get; init; } = string.Empty;
    public string Measure { get; init; } = string.Empty;
    public string Aggregation { get; init; } = "sum";
    public double Total { get; init; }
    public IReadOnlyList<GroupBucket> Buckets { get; init; } = new List<GroupBucket>();

    /// <summary>The dominant bucket, or <see langword="null"/> when empty.</summary>
    public GroupBucket? Top => Buckets.Count > 0 ? Buckets[0] : null;
}
