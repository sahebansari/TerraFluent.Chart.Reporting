using System.Collections.Generic;
using TerraFluent.AutoAnalytics.Analytics;
using TerraFluent.AutoAnalytics.Insights;
using TerraFluent.AutoAnalytics.Profiling;

namespace TerraFluent.AutoAnalytics.Extensibility;

/// <summary>Contributes custom insights. Registered generators augment the built-in insight engine.</summary>
public interface IInsightGenerator
{
    IEnumerable<Insight> Generate(DatasetProfile profile, AnalyticsFindings findings);
}
