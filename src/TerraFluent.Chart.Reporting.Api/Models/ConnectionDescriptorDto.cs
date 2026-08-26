using TerraFluent.AutoAnalytics.Data.Connections;

namespace TerraFluent.Chart.Reporting.Api.Models;

/// <summary>
/// A configured live connection, as exposed to clients.
/// </summary>
/// <remarks>
/// Deliberately has no field for the target URL, connection string, headers or any other credential
/// material — mapping from <see cref="ConnectionDescriptor"/> means there is nowhere for a secret to
/// land even if one were added upstream by mistake.
/// </remarks>
public sealed record ConnectionDescriptorDto
{
    /// <summary>The name to pass as <c>connectionName</c> on an analytics request.</summary>
    public required string Name { get; init; }

    /// <summary>Which connector serves it (e.g. <c>http</c>).</summary>
    public required string Kind { get; init; }

    /// <summary>Optional human-readable description.</summary>
    public string? Description { get; init; }

    /// <summary>Row cap applied when reading this connection, if configured.</summary>
    public int? MaxRows { get; init; }

    public static ConnectionDescriptorDto From(ConnectionDescriptor d) => new()
    {
        Name = d.Name,
        Kind = d.Kind,
        Description = d.Description,
        MaxRows = d.MaxRows
    };
}
