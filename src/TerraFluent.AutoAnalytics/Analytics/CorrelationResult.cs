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
    /// <summary>
    /// Two-tailed p-value (Student's t test) for the Pearson coefficient under the no-correlation
    /// null hypothesis. Smaller = less likely to be chance. 1 when the sample is too small to test.
    /// </summary>
    public double PValue { get; init; } = 1.0;
    /// <summary>True when the correlation is statistically significant at the 5% level.</summary>
    public bool IsSignificant => SampleSize >= 3 && PValue < 0.05;
}
