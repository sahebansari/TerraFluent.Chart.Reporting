using System.Collections.Generic;
using TerraFluent.AutoAnalytics.Analytics;
using TerraFluent.AutoAnalytics.Profiling;
using TerraFluent.AutoAnalytics.Recommendation;

namespace TerraFluent.AutoAnalytics.Extensibility;

/// <summary>Contributes custom chart recommendations. Registered recommenders augment the built-in engine.</summary>
public interface IChartRecommender
{
    IEnumerable<RecommendedChart> Recommend(DatasetProfile profile, AnalyticsFindings findings);
}
