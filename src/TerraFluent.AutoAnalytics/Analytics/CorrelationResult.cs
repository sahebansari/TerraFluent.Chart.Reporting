using TerraFluent.AutoAnalytics.Enums;

namespace TerraFluent.AutoAnalytics.Analytics;

/// <summary>A discovered pairwise correlation between two numeric measures.</summary>
public sealed class CorrelationResult
{
    public string ColumnX { get; init; } = string.Empty;
    public string ColumnY { get; init; } = string.Empty;
    /// <summary>Pearson coefficient (linear), −1..1.</summary>
    public double Pearson { get; init; }
    /// <summary>Spearman coefficient (monotonic), −1..1.</summary>
    public double Spearman { get; init; }
    public CorrelationStrength Strength { get; init; }
    /// <summary>True when the relationship is inverse (negative coefficient).</summary>
    public bool IsNegative => Pearson < 0;
    /// <summary>Number of paired observations used.</summary>
    public int SampleSize { get; init; }
}
