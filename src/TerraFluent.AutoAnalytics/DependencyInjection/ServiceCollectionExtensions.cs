using System;
using Microsoft.Extensions.DependencyInjection;
using TerraFluent.AutoAnalytics.Engine;
using TerraFluent.AutoAnalytics.Insights;
using TerraFluent.AutoAnalytics.Profiling;
using TerraFluent.AutoAnalytics.Recommendation;
using TerraFluent.AutoAnalytics.Schema;
using TerraFluent.AutoAnalytics.Validation;

namespace TerraFluent.AutoAnalytics.DependencyInjection;

/// <summary>DI registration for the AutoAnalytics engine and its phase services.</summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers the AutoAnalytics engine and all phase engines as singletons (they are stateless
    /// and thread-safe). Plugins can be registered separately as <c>IAnalyticsRule</c>,
    /// <c>IInsightGenerator</c>, <c>IChartRecommender</c> or <c>IAnomalyDetector</c> and supplied via
    /// <see cref="AnalyticsOptions"/>.
    /// </summary>
    public static IServiceCollection AddAutoAnalytics(this IServiceCollection services)
    {
        if (services is null) throw new ArgumentNullException(nameof(services));

        services.AddSingleton<SemanticInferenceEngine>();
        services.AddSingleton<SchemaDiscoveryEngine>();
        services.AddSingleton<DataValidationEngine>();
        services.AddSingleton<DataProfilingEngine>();
        services.AddSingleton<InsightScorer>();
        services.AddSingleton<InsightGenerationEngine>();
        services.AddSingleton<ChartRecommendationEngine>();
        services.AddSingleton<AnalyticsEngine>();

        return services;
    }
}
