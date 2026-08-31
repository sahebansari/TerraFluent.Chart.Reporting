using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace TerraFluent.Chart.Reporting.Api.Tests;

/// <summary>
/// Integration tests for live connections through the real HTTP pipeline: what the API discloses,
/// and how it behaves when a connection is unknown or unreachable.
/// </summary>
public sealed class ConnectionEndpointsTests : IClassFixture<WebApplicationFactory<Program>>
{
    private const string SecretTarget = "https://internal.invalid/api/sales?token=SUPER-SECRET-TOKEN";

    private readonly WebApplicationFactory<Program> _factory;

    public ConnectionEndpointsTests(WebApplicationFactory<Program> factory) => _factory = factory;

    /// <summary>A host with two connections configured, one of which points nowhere resolvable.</summary>
    private WebApplicationFactory<Program> WithConnections() =>
        _factory.WithWebHostBuilder(builder =>
            builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Connections:Sales:Kind"] = "http",
                ["Connections:Sales:Target"] = SecretTarget,
                ["Connections:Sales:Description"] = "Nightly sales extract",
                ["Connections:Sales:MaxRows"] = "5000",
                ["Connections:Sales:TimeoutSeconds"] = "2",
                ["Connections:Sales:Settings:header.Authorization"] = "Bearer SUPER-SECRET-TOKEN",
            })));

    // ── Listing ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task Connections_ListsNamesAndDescriptionsOnly()
    {
        var client = WithConnections().CreateClient();

        var response = await client.GetAsync("/api/analytics/connections");
        response.EnsureSuccessStatusCode();

        string body = await response.Content.ReadAsStringAsync();
        var root = JsonDocument.Parse(body).RootElement;

        var sales = root.EnumerateArray().Single(c => c.GetProperty("name").GetString() == "Sales");
        Assert.Equal("http", sales.GetProperty("kind").GetString());
        Assert.Equal("Nightly sales extract", sales.GetProperty("description").GetString());
        Assert.Equal(5000, sales.GetProperty("maxRows").GetInt32());

        // The whole point of server-side connections: none of this may cross the wire.
        Assert.DoesNotContain("SUPER-SECRET-TOKEN", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("internal.invalid", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("target", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("authorization", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Connections_ReturnsAnEmptyListWhenNoneAreConfigured()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/analytics/connections");
        response.EnsureSuccessStatusCode();

        Assert.Equal(0, JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement.GetArrayLength());
    }

    // ── Using a connection ────────────────────────────────────────────────────

    [Fact]
    public async Task Analyze_WithAnUnknownConnection_Returns404()
    {
        var client = WithConnections().CreateClient();

        var response = await client.PostAsJsonAsync("/api/analytics/analyze", new { connectionName = "Ghost" });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        string body = await response.Content.ReadAsStringAsync();

        // Read the detail rather than the raw body: the serialiser escapes quotes as '.
        string detail = JsonDocument.Parse(body).RootElement.GetProperty("detail").GetString()!;
        Assert.Equal("No connection named 'Ghost' is configured.", detail);
        // Probing for a connection must not reveal the ones that do exist.
        Assert.DoesNotContain("internal.invalid", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Analyze_WhenTheSourceCannotBeReached_Returns502WithoutLeaking()
    {
        var client = WithConnections().CreateClient();

        // The target host is unresolvable, so the connector fails the fetch.
        var response = await client.PostAsJsonAsync("/api/analytics/analyze", new { connectionName = "Sales" });

        Assert.Equal(HttpStatusCode.BadGateway, response.StatusCode);
        string body = await response.Content.ReadAsStringAsync();

        Assert.Contains("Sales", body);
        Assert.DoesNotContain("SUPER-SECRET-TOKEN", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("internal.invalid", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("token=", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Analyze_RejectsARequestWithNeitherDataNorConnection()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/analytics/analyze", new { data = "" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("connectionName", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Analyze_StillAcceptsInlineDataUnchanged()
    {
        // Connections are additive: the existing inline path must be untouched.
        var client = WithConnections().CreateClient();

        var response = await client.PostAsJsonAsync("/api/analytics/analyze", new
        {
            data = "Month,Region,Revenue\n2024-01,EU,100\n2024-02,EU,110\n2024-03,EU,120\n",
            datasetName = "Inline"
        });

        response.EnsureSuccessStatusCode();
        var root = JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;
        Assert.Equal("Inline", root.GetProperty("summary").GetProperty("datasetName").GetString());
    }
}
