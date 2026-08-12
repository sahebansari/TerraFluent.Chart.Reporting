using System.Collections.Generic;
using TerraFluent.AutoAnalytics.Enums;

namespace TerraFluent.AutoAnalytics.Insights;

/// <summary>
/// A human-readable, business-friendly observation with a deterministic importance score and the
/// evidence that produced it. Immutable.
/// </summary>
public sealed class Insight
{
    public InsightKind Kind { get; init; }

    /// <summary>Short headline (e.g. "Revenue up 34%").</summary>
    public string Title { get; init; } = string.Empty;

    /// <summary>Full narrative sentence.</summary>
    public string Description { get; init; } = string.Empty;

    /// <summary>Deterministic importance 0..100 (higher = more noteworthy).</summary>
    public int ImportanceScore { get; init; }

    /// <summary>Supporting evidence key/value pairs (measures, coefficients, columns involved).</summary>
    public IReadOnlyDictionary<string, string> Evidence { get; init; } =
        new Dictionary<string, string>();

    /// <summary>Columns this insight references (for linking to recommended charts).</summary>
    public IReadOnlyList<string> RelatedColumns { get; init; } = new List<string>();
}
