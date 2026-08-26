using System;
using System.Collections.Generic;

namespace TerraFluent.AutoAnalytics.Data.Connections;

/// <summary>
/// The non-secret description of a configured connection — everything a client is allowed to see.
/// </summary>
/// <remarks>
/// This type exists so listing connections cannot leak credentials by accident: it simply has
/// nowhere to put them. Anything returned to a caller should be a descriptor, never a
/// <see cref="ConnectionDefinition"/>.
/// </remarks>
public sealed class ConnectionDescriptor
{
    /// <summary>The name a client uses to select this connection.</summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>Which connector handles it (e.g. <c>http</c>).</summary>
    public string Kind { get; init; } = string.Empty;

    /// <summary>Optional human-readable description shown in a picker.</summary>
    public string? Description { get; init; }

    /// <summary>Row cap applied when this connection is read, if one is configured.</summary>
    public int? MaxRows { get; init; }
}

/// <summary>
/// A connection as the operator configured it, including the target and any credentials.
/// </summary>
/// <remarks>
/// <para><b>Never return this to a client and never place it in an exception message.</b> Call
/// <see cref="ToDescriptor"/> for anything that crosses the API boundary. <see cref="ToString"/> is
/// deliberately overridden to redact, so an accidental interpolation into a log or error cannot
/// spill the target.</para>
/// <para>Because the client selects a connection by <em>name</em> and never supplies a URL or
/// connection string, the set of reachable targets is fixed by configuration — which also closes off
/// server-side request forgery through this path.</para>
/// </remarks>
public sealed class ConnectionDefinition
{
    /// <summary>The name a client uses to select this connection.</summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>Which connector handles it (e.g. <c>http</c>).</summary>
    public string Kind { get; init; } = string.Empty;

    /// <summary>Optional human-readable description shown in a picker.</summary>
    public string? Description { get; init; }

    /// <summary>The URL or connection string. Secret — never serialise or log this.</summary>
    public string Target { get; init; } = string.Empty;

    /// <summary>Extra per-connection settings (auth headers, a query, a JSON path…). Secret.</summary>
    public IReadOnlyDictionary<string, string> Settings { get; init; } =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

    /// <summary>Maximum rows to read; <see langword="null"/> uses the connector's own default.</summary>
    public int? MaxRows { get; init; }

    /// <summary>Fetch timeout in seconds; <see langword="null"/> uses the connector's own default.</summary>
    public int? TimeoutSeconds { get; init; }

    /// <summary>Projects the safe subset of this definition for returning to a client.</summary>
    public ConnectionDescriptor ToDescriptor() => new()
    {
        Name = Name,
        Kind = Kind,
        Description = Description,
        MaxRows = MaxRows
    };

    /// <summary>Redacted — the target and settings are secret and never rendered.</summary>
    public override string ToString() => $"Connection '{Name}' ({Kind})";
}
