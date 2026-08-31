using TerraFluent.AutoAnalytics.Data.Connections;
using TerraFluent.AutoAnalytics.Data.Sources;

namespace TerraFluent.AutoAnalytics.Connectors.Http;

/// <summary>Builds <see cref="HttpJsonDataSource"/> instances for connections of kind <c>http</c>.</summary>
/// <remarks>
/// The <see cref="IHttpClientFactory"/> named client is what gives pooled sockets and lets a host
/// layer on its own resilience policies without this class knowing about them.
/// </remarks>
public sealed class HttpConnectorFactory : IConnectorFactory
{
    /// <summary>Name of the <see cref="IHttpClientFactory"/> client used for connector fetches.</summary>
    public const string HttpClientName = "autoanalytics-connector";

    private readonly IHttpClientFactory _clients;

    public HttpConnectorFactory(IHttpClientFactory clients)
        => _clients = clients ?? throw new ArgumentNullException(nameof(clients));

    public string Kind => "http";

    public IAsyncDataSource Create(ConnectionDefinition connection)
    {
        if (connection is null) throw new ArgumentNullException(nameof(connection));
        return new HttpJsonDataSource(_clients.CreateClient(HttpClientName), connection);
    }
}
