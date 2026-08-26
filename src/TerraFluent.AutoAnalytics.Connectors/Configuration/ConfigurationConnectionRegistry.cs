using Microsoft.Extensions.Configuration;
using TerraFluent.AutoAnalytics.Data.Connections;

namespace TerraFluent.AutoAnalytics.Connectors.Configuration;

/// <summary>
/// Reads named connections from an <see cref="IConfiguration"/> section, so credentials live wherever
/// the host puts its configuration — appsettings, environment variables, user secrets or a vault
/// provider — and never in the application's request path.
/// </summary>
/// <remarks>
/// Expected shape under the section (default <c>Connections</c>):
/// <code>
/// "Connections": {
///   "Sales": {
///     "Kind": "http",
///     "Target": "https://internal.example.com/api/sales",
///     "Description": "Nightly sales extract",
///     "MaxRows": 50000,
///     "TimeoutSeconds": 20,
///     "Settings": { "jsonPath": "data.rows", "header.Authorization": "Bearer …" }
///   }
/// }
/// </code>
/// The key is the connection name a client refers to. Entries missing a <c>Target</c> are skipped
/// rather than half-registered, so a partially-configured connection fails at startup listing rather
/// than mid-request.
/// </remarks>
public sealed class ConfigurationConnectionRegistry : IConnectionRegistry
{
    /// <summary>Configuration section read by default.</summary>
    public const string DefaultSectionName = "Connections";

    private readonly InMemoryConnectionRegistry _inner;

    public ConfigurationConnectionRegistry(IConfiguration configuration, string sectionName = DefaultSectionName)
    {
        if (configuration is null) throw new ArgumentNullException(nameof(configuration));

        var connections = new List<ConnectionDefinition>();

        foreach (var child in configuration.GetSection(sectionName).GetChildren())
        {
            string? target = child["Target"];
            if (string.IsNullOrWhiteSpace(target)) continue;

            var settings = child.GetSection("Settings")
                .GetChildren()
                .Where(s => s.Value is not null)
                .ToDictionary(s => s.Key, s => s.Value!, StringComparer.OrdinalIgnoreCase);

            connections.Add(new ConnectionDefinition
            {
                Name = child.Key,
                Kind = string.IsNullOrWhiteSpace(child["Kind"]) ? "http" : child["Kind"]!,
                Description = child["Description"],
                Target = target!,
                Settings = settings,
                MaxRows = int.TryParse(child["MaxRows"], out int rows) ? rows : null,
                TimeoutSeconds = int.TryParse(child["TimeoutSeconds"], out int seconds) ? seconds : null
            });
        }

        _inner = new InMemoryConnectionRegistry(connections);
    }

    public IReadOnlyList<ConnectionDescriptor> List() => _inner.List();

    public ConnectionDefinition? Find(string name) => _inner.Find(name);
}
