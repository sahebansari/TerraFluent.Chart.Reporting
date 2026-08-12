using System.Collections.Generic;

namespace TerraFluent.AutoAnalytics.Engine;

/// <summary>A concise, business-user-friendly overview of the analysis.</summary>
public sealed class AnalyticsSummary
{
    public string DatasetName { get; init; } = "Dataset";
    public int RowCount { get; init; }
    public int ColumnCount { get; init; }
    public int MeasureCount { get; init; }
    public int DimensionCount { get; init; }
    public int InsightCount { get; init; }
    public int RecommendationCount { get; init; }

    /// <summary>The single most important finding, phrased as a headline.</summary>
    public string Headline { get; init; } = string.Empty;

    /// <summary>Top narratives (descending importance).</summary>
    public IReadOnlyList<string> KeyFindings { get; init; } = new List<string>();

    /// <summary>Overall data-quality verdict: <c>Clean</c>, <c>Warnings</c> or <c>Errors</c>.</summary>
    public string DataQuality { get; init; } = "Clean";
}
