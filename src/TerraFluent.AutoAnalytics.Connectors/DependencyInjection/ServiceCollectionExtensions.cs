using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TerraFluent.AutoAnalytics.Connectors.Configuration;
using TerraFluent.AutoAnalytics.Connectors.Http;
using TerraFluent.AutoAnalytics.Data.Connections;

namespace TerraFluent.AutoAnalytics.Connectors.DependencyInjection;

/// <summary>DI registration for the live source connectors.</summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers the connection registry, the connector factories and the resolver that maps a
    /// caller-supplied connection <em>name</em> to a live source.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">Configuration holding the connections section.</param>
    /// <param name="sectionName">Section to read; defaults to <c>Connections</c>.</param>
    /// <remarks>
    /// Safe to call when nothing is configured — the registry is simply empty, the connections
    /// endpoint returns an empty list, and every other feature is unaffected.
    /// </remarks>
    public static IServiceCollection AddAutoAnalyticsConnectors(
        this IServiceCollection services,
        IConfiguration configuration,
        string sectionName = ConfigurationConnectionRegistry.DefaultSectionName)
    {
        if (services is null) throw new ArgumentNullException(nameof(services));
        if (configuration is null) throw new ArgumentNullException(nameof(configuration));

        services.AddHttpClient(HttpConnectorFactory.HttpClientName);

        services.AddSingleton<IConnectionRegistry>(
            _ => new ConfigurationConnectionRegistry(configuration, sectionName));

        services.AddSingleton<IConnectorFactory, HttpConnectorFactory>();
        services.AddSingleton<ConnectionResolver>();

        return services;
    }
}
