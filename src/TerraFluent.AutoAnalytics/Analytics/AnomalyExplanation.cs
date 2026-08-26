using System.Collections.Generic;

namespace TerraFluent.AutoAnalytics.Analytics;

/// <summary>
/// One dimension's attempt to account for an anomalous value: the category the outlying row belongs
/// to, what that category normally looks like, and how much of the deviation it therefore explains.
/// </summary>
public sealed class AnomalyAttribution
{
    /// <summary>The dimension being tested as an explanation (e.g. <c>Region</c>).</summary>
    public string Dimension { get; init; } = string.Empty;

    /// <summary>The category the anomalous row belongs to (e.g. <c>EU</c>).</summary>
    public string Category { get; init; } = string.Empty;

    /// <summary>
    /// Typical measure value within this category, excluding the anomalous row itself — otherwise a
    /// thinly-populated category would trivially "explain" its own outlier.
    /// </summary>
    public double CategoryMedian { get; init; }

    /// <summary>Rows backing <see cref="CategoryMedian"/>.</summary>
    public int CategoryCount { get; init; }

    /// <summary>
    /// The anomalous value relative to its own category's norm. A ratio near 1 means the value is
    /// unremarkable once the category is known — the category explains it.
    /// </summary>
    public double ResidualRatio { get; init; }

    /// <summary>
    /// Fraction 0..1 of the value's deviation from the dataset norm that this category accounts for.
    /// 1 means the category fully explains the anomaly; 0 means knowing it tells you nothing.
    /// </summary>
    public double ExplainedFraction { get; init; }
}

/// <summary>
/// A deterministic account of an anomalous observation: when it occurred, how far it sits from the
/// dataset norm, and whether any single dimension accounts for it.
/// </summary>
/// <remarks>
/// The useful question about an outlier is not only "how extreme is it" but "is it extreme once you
/// know which segment it came from". A revenue figure three times the dataset median may be entirely
/// ordinary for the largest region — in which case the dimension explains it and there is nothing to
/// investigate. When no dimension brings the value near its own category's norm, the anomaly is a
/// genuine one-off (or a data-quality artefact) and deserves attention.
/// </remarks>
public sealed class AnomalyExplanation
{
    /// <summary>The measure the anomaly was detected in.</summary>
    public string Measure { get; init; } = string.Empty;

    /// <summary>Human-readable label for when the anomaly occurred (a date, or a row position).</summary>
    public string WhenLabel { get; init; } = string.Empty;

    /// <summary>Dataset row of the anomaly this explains — ties it back to its <see cref="AnomalyPoint"/>.</summary>
    public int RowIndex { get; init; }

    /// <summary>The anomalous value itself.</summary>
    public double Value { get; init; }

    /// <summary>The measure's median across the whole dataset.</summary>
    public double OverallMedian { get; init; }

    /// <summary>The anomalous value relative to the dataset median.</summary>
    public double OverallRatio { get; init; }

    /// <summary>Robust, σ-comparable magnitude of the anomaly that prompted the explanation.</summary>
    public double Magnitude { get; init; }

    /// <summary>Attributions ordered by descending <see cref="AnomalyAttribution.ExplainedFraction"/>.</summary>
    public IReadOnlyList<AnomalyAttribution> Attributions { get; init; } = new List<AnomalyAttribution>();

    /// <summary>The dimension that best accounts for the anomaly, if any was testable.</summary>
    public AnomalyAttribution? BestExplanation => Attributions.Count > 0 ? Attributions[0] : null;

    /// <summary>
    /// True when the best attribution accounts for most of the deviation — the value is normal for
    /// its segment, so the "anomaly" is really just segment mix.
    /// </summary>
    public bool IsExplained { get; init; }

    public bool IsEmpty => Attributions.Count == 0;
}
