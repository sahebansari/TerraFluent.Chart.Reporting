using System.Net;
using System.Text;

namespace TerraFluent.AutoAnalytics.Connectors.Tests;

/// <summary>
/// A hand-rolled <see cref="HttpMessageHandler"/> so connector tests exercise the real
/// <see cref="HttpClient"/> pipeline without a mocking package or a live endpoint.
/// </summary>
public sealed class StubHandler : HttpMessageHandler
{
    private readonly Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> _respond;

    /// <summary>Every request the handler saw, so tests can assert on headers and URLs.</summary>
    public List<HttpRequestMessage> Requests { get; } = new();

    public StubHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> respond)
        => _respond = respond;

    /// <summary>Responds with a fixed body and status.</summary>
    public static StubHandler Returning(string body, HttpStatusCode status = HttpStatusCode.OK) =>
        new((_, _) => Task.FromResult(new HttpResponseMessage(status)
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json")
        }));

    /// <summary>Never responds, so the connector's own timeout is what ends the call.</summary>
    public static StubHandler Hanging() =>
        new(async (_, ct) =>
        {
            await Task.Delay(Timeout.Infinite, ct);
            return new HttpResponseMessage(HttpStatusCode.OK);
        });

    /// <summary>Fails the way a DNS or TLS error does — with the URL inside the message.</summary>
    public static StubHandler Throwing(string message) =>
        new((_, _) => throw new HttpRequestException(message));

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
    {
        Requests.Add(request);
        return _respond(request, ct);
    }
}

/// <summary>Serves a single <see cref="HttpClient"/> built over a stub handler.</summary>
public sealed class StubHttpClientFactory : IHttpClientFactory
{
    private readonly HttpMessageHandler _handler;

    public StubHttpClientFactory(HttpMessageHandler handler) => _handler = handler;

    public HttpClient CreateClient(string name) => new(_handler, disposeHandler: false);
}
