using System.Collections.Generic;
using TerraFluent.AutoAnalytics.Analytics;
using TerraFluent.AutoAnalytics.Insights;
using TerraFluent.AutoAnalytics.Profiling;

namespace TerraFluent.AutoAnalytics.Extensibility;

/// <summary>
/// A pluggable custom analytics rule. Registered rules run after the built-in analytics phase and
/// may contribute additional insights derived from the profile and findings.
/// </summary>
public interface IAnalyticsRule
{
    /// <summary>Stable identifier for diagnostics and ordering.</summary>
    string Name { get; }

    /// <summary>Evaluates the rule, returning zero or more insights.</summary>
    IEnumerable<Insight> Evaluate(DatasetProfile profile, AnalyticsFindings findings);
}
