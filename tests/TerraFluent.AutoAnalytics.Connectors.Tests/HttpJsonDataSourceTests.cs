using System.Net;
using System.Text;
using TerraFluent.AutoAnalytics.Connectors.Http;
using TerraFluent.AutoAnalytics.Data.Connections;
using TerraFluent.AutoAnalytics.Engine;
using Xunit;

namespace TerraFluent.AutoAnalytics.Connectors.Tests;

/// <summary>Covers the HTTP/JSON connector: fetching, shaping, bounding and failing safely.</summary>
public class HttpJsonDataSourceTests
{
    private const string SecretUrl = "https://internal.example.com/api/sales?token=SUPER-SECRET-TOKEN";
    private const string SecretHeader = "Bearer SUPER-SECRET-TOKEN";

    private const string SalesJson = """
        [{"Month":"2024-01","Region":"EU","Revenue":12000},
         {"Month":"2024-02","Region":"EU","Revenue":13500},
         {"Month":"2024-03","Region":"NA","Revenue":15000}]
        """;

    private static ConnectionDefinition Connection(
        string target = SecretUrl,
        Dictionary<string, string>? settings = null,
        int? maxRows = null,
        int? timeoutSeconds = null) => new()
    {
        Name = "Sales",
        Kind = "http",
        Description = "Nightly sales extract",
        Target = target,
        Settings = settings ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase),
        MaxRows = maxRows,
        TimeoutSeconds = timeoutSeconds
    };

    private static HttpJsonDataSource Source(StubHandler handler, ConnectionDefinition? connection = null) =>
        new(new HttpClient(handler, disposeHandler: false), connection ?? Connection());

    // ── Fetching ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task LoadAsync_ReadsAJsonArrayIntoADataset()
    {
        var dataset = await Source(StubHandler.Returning(SalesJson)).LoadAsync();

        Assert.Equal("Sales", dataset.Name);
        Assert.Equal(3, dataset.RowCount);
        Assert.Equal(new[] { "Month", "Region", "Revenue" }, dataset.Columns.Select(c => c.Name));
        Assert.Equal(12000d, dataset.GetColumn("Revenue")!.Values[0]);
    }

    [Fact]
    public async Task LoadAsync_SendsTheConfiguredHeaders()
    {
        var handler = StubHandler.Returning(SalesJson);
        var connection = Connection(settings: new(StringComparer.OrdinalIgnoreCase)
        {
            ["header.Authorization"] = SecretHeader,
            ["header.X-Tenant"] = "acme",
            ["jsonPath"] = "",            // ignored when blank
        });

        await Source(handler, connection).LoadAsync();

        var request = Assert.Single(handler.Requests);
        Assert.Equal(SecretHeader, request.Headers.GetValues("Authorization").Single());
        Assert.Equal("acme", request.Headers.GetValues("X-Tenant").Single());
        Assert.Contains("application/json", request.Headers.GetValues("Accept"));
    }

    [Fact]
    public async Task LoadAsync_ExtractsANestedArrayViaJsonPath()
    {
        const string enveloped = """
            {"meta":{"page":1},"data":{"rows":[{"A":1},{"A":2}]}}
            """;

        var connection = Connection(settings: new(StringComparer.OrdinalIgnoreCase) { ["jsonPath"] = "data.rows" });
        var dataset = await Source(StubHandler.Returning(enveloped), connection).LoadAsync();

        Assert.Equal(2, dataset.RowCount);
        Assert.Equal(1d, dataset.GetColumn("A")!.Values[0]);
    }

    [Fact]
    public async Task LoadAsync_AppliesTheRowCap()
    {
        string manyRows = "[" + string.Join(",", Enumerable.Range(0, 50).Select(i => $"{{\"N\":{i}}}")) + "]";
        var connection = Connection(maxRows: 10);

        var dataset = await Source(StubHandler.Returning(manyRows), connection).LoadAsync();

        Assert.Equal(10, dataset.RowCount);
        Assert.Equal(0d, dataset.GetColumn("N")!.Values[0]);
        Assert.Equal(9d, dataset.GetColumn("N")!.Values[9]);
    }

    [Fact]
    public async Task LoadAsync_FeedsTheAnalyticsEngineDirectly()
    {
        var result = await AnalyticsEngine.AnalyzeAsync(Source(StubHandler.Returning(SalesJson)));

        Assert.Equal(3, result.Summary.RowCount);
        Assert.Equal("Sales", result.Summary.DatasetName);
    }

    [Fact]
    public async Task LoadAsync_HonoursCallerCancellation()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // A cancelled caller must surface as cancellation, not as a connection failure.
        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => Source(StubHandler.Hanging()).LoadAsync(cts.Token));
    }

    // ── Failure modes ─────────────────────────────────────────────────────────

    [Fact]
    public async Task LoadAsync_ReportsANonSuccessStatus()
    {
        var ex = await Assert.ThrowsAsync<ConnectionException>(
            () => Source(StubHandler.Returning("nope", HttpStatusCode.Forbidden)).LoadAsync());

        Assert.Equal("Sales", ex.ConnectionName);
        Assert.Contains("HTTP 403", ex.Message);
        Assert.False(ex.IsNotConfigured);
    }

    [Fact]
    public async Task LoadAsync_ReportsAnUnreachableEndpoint()
    {
        // The underlying exception embeds the URL, as real HTTP failures do.
        var ex = await Assert.ThrowsAsync<ConnectionException>(
            () => Source(StubHandler.Throwing($"No such host is known ({SecretUrl})")).LoadAsync());

        Assert.Contains("could not be reached", ex.Message);
    }

    [Fact]
    public async Task LoadAsync_ReportsATimeout()
    {
        var connection = Connection(timeoutSeconds: 1);

        var ex = await Assert.ThrowsAsync<ConnectionException>(
            () => Source(StubHandler.Hanging(), connection).LoadAsync());

        Assert.Contains("did not respond within 1s", ex.Message);
    }

    [Fact]
    public async Task LoadAsync_ReportsAMalformedBody()
    {
        var ex = await Assert.ThrowsAsync<ConnectionException>(
            () => Source(StubHandler.Returning("{not json")).LoadAsync());

        Assert.Contains("Sales", ex.Message);
    }

    [Fact]
    public async Task LoadAsync_ReportsAMissingJsonPath()
    {
        var connection = Connection(settings: new(StringComparer.OrdinalIgnoreCase) { ["jsonPath"] = "data.rows" });

        var ex = await Assert.ThrowsAsync<ConnectionException>(
            () => Source(StubHandler.Returning("""{"other":[]}"""), connection).LoadAsync());

        Assert.Contains("no 'data.rows' property", ex.Message);
    }

    [Fact]
    public async Task LoadAsync_RejectsANonHttpTarget()
    {
        var ex = await Assert.ThrowsAsync<ConnectionException>(
            () => Source(StubHandler.Returning(SalesJson), Connection(target: "file:///etc/passwd")).LoadAsync());

        Assert.Contains("not an absolute http or https URL", ex.Message);
    }

    [Fact]
    public async Task LoadAsync_RefusesAnOversizedDeclaredBody()
    {
        var handler = new StubHandler((_, _) =>
        {
            var content = new StringContent("[]", Encoding.UTF8, "application/json");
            content.Headers.ContentLength = 128L * 1024 * 1024;   // claims 128MB
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = content });
        });

        var ex = await Assert.ThrowsAsync<ConnectionException>(() => Source(handler).LoadAsync());
        Assert.Contains("larger than the 64MB limit", ex.Message);
    }

    // ── Credential containment ────────────────────────────────────────────────
    // Every path a caller can observe must be free of the target URL and any credential. These are
    // the tests that matter most: a leak here undoes the entire point of server-side connections.

    public static TheoryData<string, Func<HttpJsonDataSourceTests, Task<ConnectionException>>> FailureModes => new()
    {
        { "non-success status", t => t.Capture(StubHandler.Returning("no", HttpStatusCode.Unauthorized), Connection()) },
        { "unreachable host",   t => t.Capture(StubHandler.Throwing($"Cannot resolve {SecretUrl}"), Connection()) },
        { "timeout",            t => t.Capture(StubHandler.Hanging(), Connection(timeoutSeconds: 1)) },
        { "malformed body",     t => t.Capture(StubHandler.Returning("{not json"), Connection()) },
        { "bad target",         t => t.Capture(StubHandler.Returning("[]"), Connection(target: "not-a-url")) },
    };

    private async Task<ConnectionException> Capture(StubHandler handler, ConnectionDefinition connection)
    {
        var withSecretHeader = new ConnectionDefinition
        {
            Name = connection.Name,
            Kind = connection.Kind,
            Target = connection.Target,
            MaxRows = connection.MaxRows,
            TimeoutSeconds = connection.TimeoutSeconds,
            Settings = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["header.Authorization"] = SecretHeader
            }
        };

        return await Assert.ThrowsAsync<ConnectionException>(
            () => Source(handler, withSecretHeader).LoadAsync());
    }

    [Theory]
    [MemberData(nameof(FailureModes))]
    public async Task Failures_NeverEchoTheTargetOrCredential(
        string scenario, Func<HttpJsonDataSourceTests, Task<ConnectionException>> run)
    {
        var ex = await run(this);

        // The client sees Message and ToString(); neither may carry the secret.
        foreach (var text in new[] { ex.Message, ex.ToString() })
        {
            Assert.DoesNotContain("SUPER-SECRET-TOKEN", text, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("internal.example.com", text, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("token=", text, StringComparison.OrdinalIgnoreCase);
        }

        // …but it does say which connection failed, so the failure is still actionable.
        Assert.Contains("Sales", ex.Message);
        Assert.False(string.IsNullOrWhiteSpace(scenario));
    }

    [Fact]
    public void ConnectionDefinition_RedactsItsTargetWhenStringified()
    {
        var connection = Connection(settings: new(StringComparer.OrdinalIgnoreCase)
        {
            ["header.Authorization"] = SecretHeader
        });

        string text = connection.ToString();

        Assert.DoesNotContain("SUPER-SECRET-TOKEN", text);
        Assert.DoesNotContain("internal.example.com", text);
        Assert.Equal("Connection 'Sales' (http)", text);
    }

    [Fact]
    public void ConnectionDescriptor_HasNowhereToPutASecret()
    {
        var descriptor = Connection().ToDescriptor();

        // Reflection over the public surface: no property may carry target or credential material.
        var propertyNames = typeof(ConnectionDescriptor)
            .GetProperties()
            .Select(p => p.Name)
            .ToList();

        Assert.DoesNotContain("Target", propertyNames);
        Assert.DoesNotContain("Settings", propertyNames);

        // And the values it does carry are the safe ones.
        Assert.Equal("Sales", descriptor.Name);
        Assert.Equal("http", descriptor.Kind);
        Assert.Equal("Nightly sales extract", descriptor.Description);
    }
}
