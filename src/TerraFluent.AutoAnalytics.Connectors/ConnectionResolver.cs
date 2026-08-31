using TerraFluent.AutoAnalytics.Data.Connections;
using TerraFluent.AutoAnalytics.Data.Sources;

namespace TerraFluent.AutoAnalytics.Connectors;

/// <summary>
/// Turns a connection <em>name</em> supplied by a caller into a live data source, by looking it up
/// in the registry and handing it to the factory registered for its kind.
/// </summary>
/// <remarks>
/// This is the only place a client-supplied string meets a configured credential, so it is the
/// boundary worth reading carefully: the name is used purely as a dictionary key, and an unknown
/// name yields a plain "not configured" error rather than anything derived from the input.
/// </remarks>
public sealed class ConnectionResolver
{
    private readonly IConnectionRegistry _registry;
    private readonly Dictionary<string, IConnectorFactory> _factories;

    public ConnectionResolver(IConnectionRegistry registry, IEnumerable<IConnectorFactory> factories)
    {
        _registry = registry ?? throw new ArgumentNullException(nameof(registry));
        _factories = (factories ?? throw new ArgumentNullException(nameof(factories)))
            .ToDictionary(f => f.Kind, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>The configured connections, as descriptors safe to return to a client.</summary>
    public IReadOnlyList<ConnectionDescriptor> List() => _registry.List();

    /// <summary>
    /// Resolves a connection name to a source ready to fetch.
    /// </summary>
    /// <exception cref="ConnectionException">
    /// No connection with that name is configured, or no connector handles its kind.
    /// </exception>
    public IAsyncDataSource Resolve(string connectionName)
    {
        if (string.IsNullOrWhiteSpace(connectionName))
            throw new ConnectionException(string.Empty, "A connection name is required.") { IsNotConfigured = true };

        var connection = _registry.Find(connectionName)
            ?? throw new ConnectionException(connectionName,
                $"No connection named '{connectionName}' is configured.") { IsNotConfigured = true };

        if (!_factories.TryGetValue(connection.Kind, out var factory))
            throw ConnectionException.Failed(connection.Name,
                $"no connector is registered for kind '{connection.Kind}'.");

        return factory.Create(connection);
    }
}
