using System;
using System.Collections.Generic;
using System.Linq;

namespace TerraFluent.AutoAnalytics.Data.Connections;

/// <summary>
/// The set of connections an operator has configured. Clients choose one by name; the registry is
/// the only thing that ever holds the credentials.
/// </summary>
public interface IConnectionRegistry
{
    /// <summary>The connections available, as descriptors safe to return to a client.</summary>
    IReadOnlyList<ConnectionDescriptor> List();

    /// <summary>
    /// Resolves a connection by name (case-insensitively). Returns <see langword="null"/> when no
    /// such connection is configured.
    /// </summary>
    ConnectionDefinition? Find(string name);
}

/// <summary>
/// An <see cref="IConnectionRegistry"/> over a fixed set of definitions — used by library consumers
/// that wire connections up in code, and by tests.
/// </summary>
public sealed class InMemoryConnectionRegistry : IConnectionRegistry
{
    private readonly Dictionary<string, ConnectionDefinition> _byName;

    public InMemoryConnectionRegistry(IEnumerable<ConnectionDefinition> connections)
    {
        if (connections is null) throw new ArgumentNullException(nameof(connections));

        _byName = new Dictionary<string, ConnectionDefinition>(StringComparer.OrdinalIgnoreCase);
        foreach (var connection in connections)
        {
            if (string.IsNullOrWhiteSpace(connection.Name))
                throw new ArgumentException("Every connection must have a name.", nameof(connections));

            // Last definition wins rather than throwing: configuration layering (appsettings then
            // environment then user secrets) legitimately redefines the same name.
            _byName[connection.Name] = connection;
        }
    }

    public IReadOnlyList<ConnectionDescriptor> List() =>
        _byName.Values
            .OrderBy(c => c.Name, StringComparer.OrdinalIgnoreCase)
            .Select(c => c.ToDescriptor())
            .ToList();

    public ConnectionDefinition? Find(string name) =>
        string.IsNullOrWhiteSpace(name) ? null
        : _byName.TryGetValue(name, out var definition) ? definition
        : null;
}
