using System;

namespace TerraFluent.AutoAnalytics.Data.Connections;

/// <summary>
/// Builds a live data source from a configured connection. One implementation per
/// <see cref="ConnectionDefinition.Kind"/>.
/// </summary>
public interface IConnectorFactory
{
    /// <summary>The connection kind this factory handles (e.g. <c>http</c>), matched case-insensitively.</summary>
    string Kind { get; }

    /// <summary>Creates a source that will fetch the connection's data when awaited.</summary>
    Sources.IAsyncDataSource Create(ConnectionDefinition connection);
}

/// <summary>
/// Raised when a connection cannot be read. The message is safe to return to a client: it names the
/// connection and the reason, never the target or any credential.
/// </summary>
public sealed class ConnectionException : Exception
{
    /// <summary>The connection that failed, by name.</summary>
    public string ConnectionName { get; }

    /// <summary>
    /// True when the failure is that no such connection exists, rather than that reading it failed.
    /// Lets a host answer 404 for a bad name and 502 for an upstream that would not respond.
    /// </summary>
    public bool IsNotConfigured { get; init; }

    public ConnectionException(string connectionName, string message, Exception? inner = null)
        : base(message, inner)
        => ConnectionName = connectionName;

    /// <summary>
    /// Builds a message that describes the failure without echoing the target. Underlying exceptions
    /// from HTTP and database clients routinely embed the URL or connection string, so their text is
    /// deliberately not reused.
    /// </summary>
    public static ConnectionException Failed(string connectionName, string reason, Exception? inner = null) =>
        new(connectionName, $"Connection '{connectionName}' could not be read: {reason}", inner);
}
