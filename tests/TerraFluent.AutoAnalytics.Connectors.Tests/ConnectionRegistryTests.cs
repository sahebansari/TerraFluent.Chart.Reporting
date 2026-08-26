using Microsoft.Extensions.Configuration;
using TerraFluent.AutoAnalytics.Connectors.Configuration;
using TerraFluent.AutoAnalytics.Connectors.Http;
using TerraFluent.AutoAnalytics.Data.Connections;
using Xunit;

namespace TerraFluent.AutoAnalytics.Connectors.Tests;

/// <summary>Covers connection configuration and the name → source resolution boundary.</summary>
public class ConnectionRegistryTests
{
    private static IConfiguration Config(Dictionary<string, string?> values) =>
        new ConfigurationBuilder().AddInMemoryCollection(values).Build();

    private static readonly Dictionary<string, string?> SampleConfig = new()
    {
        ["Connections:Sales:Kind"] = "http",
        ["Connections:Sales:Target"] = "https://internal.example.com/sales?token=SECRET",
        ["Connections:Sales:Description"] = "Nightly sales extract",
        ["Connections:Sales:MaxRows"] = "5000",
        ["Connections:Sales:TimeoutSeconds"] = "15",
        ["Connections:Sales:Settings:jsonPath"] = "data.rows",
        ["Connections:Sales:Settings:header.Authorization"] = "Bearer SECRET",

        ["Connections:Traffic:Target"] = "https://internal.example.com/traffic",
    };

    // ── Configuration binding ─────────────────────────────────────────────────

    [Fact]
    public void Registry_BindsConnectionsFromConfiguration()
    {
        var registry = new ConfigurationConnectionRegistry(Config(SampleConfig));

        var sales = registry.Find("Sales");
        Assert.NotNull(sales);
        Assert.Equal("http", sales!.Kind);
        Assert.Equal("https://internal.example.com/sales?token=SECRET", sales.Target);
        Assert.Equal(5000, sales.MaxRows);
        Assert.Equal(15, sales.TimeoutSeconds);
        Assert.Equal("data.rows", sales.Settings["jsonPath"]);
        Assert.Equal("Bearer SECRET", sales.Settings["header.Authorization"]);
    }

    [Fact]
    public void Registry_DefaultsKindToHttp()
    {
        var registry = new ConfigurationConnectionRegistry(Config(SampleConfig));
        Assert.Equal("http", registry.Find("Traffic")!.Kind);
    }

    [Fact]
    public void Registry_ResolvesNamesCaseInsensitively()
    {
        var registry = new ConfigurationConnectionRegistry(Config(SampleConfig));

        Assert.NotNull(registry.Find("sales"));
        Assert.NotNull(registry.Find("SALES"));
        Assert.Null(registry.Find("nope"));
        Assert.Null(registry.Find(""));
    }

    [Fact]
    public void Registry_SkipsAConnectionWithNoTarget()
    {
        // Half-configured entries must not register, so the failure shows at listing rather than
        // partway through someone's request.
        var registry = new ConfigurationConnectionRegistry(Config(new Dictionary<string, string?>
        {
            ["Connections:Broken:Kind"] = "http",
            ["Connections:Broken:Description"] = "no target",
        }));

        Assert.Empty(registry.List());
        Assert.Null(registry.Find("Broken"));
    }

    [Fact]
    public void Registry_IsEmptyWhenNothingIsConfigured()
    {
        var registry = new ConfigurationConnectionRegistry(Config(new Dictionary<string, string?>()));
        Assert.Empty(registry.List());
    }

    [Fact]
    public void Registry_ListReturnsDescriptorsWithoutSecrets()
    {
        var registry = new ConfigurationConnectionRegistry(Config(SampleConfig));
        var listed = registry.List();

        Assert.Equal(2, listed.Count);
        Assert.Equal(new[] { "Sales", "Traffic" }, listed.Select(d => d.Name));   // sorted by name

        // The listing is the API's response shape — serialising it must not disclose anything.
        string serialised = System.Text.Json.JsonSerializer.Serialize(listed);
        Assert.DoesNotContain("SECRET", serialised, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("internal.example.com", serialised, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Nightly sales extract", serialised);
    }

    [Fact]
    public void InMemoryRegistry_RequiresEveryConnectionToBeNamed()
    {
        Assert.Throws<ArgumentException>(() => new InMemoryConnectionRegistry(new[]
        {
            new ConnectionDefinition { Name = "", Kind = "http", Target = "https://x" }
        }));
    }

    [Fact]
    public void InMemoryRegistry_LetsALaterDefinitionOverrideAnEarlierOne()
    {
        // Configuration layering (appsettings → environment → secrets) legitimately redefines a name.
        var registry = new InMemoryConnectionRegistry(new[]
        {
            new ConnectionDefinition { Name = "Sales", Kind = "http", Target = "https://old" },
            new ConnectionDefinition { Name = "sales", Kind = "http", Target = "https://new" },
        });

        Assert.Equal("https://new", registry.Find("Sales")!.Target);
        Assert.Single(registry.List());
    }

    // ── Resolution ────────────────────────────────────────────────────────────

    private static ConnectionResolver Resolver(IConfiguration configuration) =>
        new(new ConfigurationConnectionRegistry(configuration),
            new IConnectorFactory[] { new HttpConnectorFactory(new StubHttpClientFactory(StubHandler.Returning("[]"))) });

    [Fact]
    public void Resolver_BuildsASourceForAConfiguredConnection()
    {
        var source = Resolver(Config(SampleConfig)).Resolve("Sales");
        Assert.IsType<HttpJsonDataSource>(source);
    }

    [Fact]
    public void Resolver_ReportsAnUnknownNameAsNotConfigured()
    {
        var ex = Assert.Throws<ConnectionException>(() => Resolver(Config(SampleConfig)).Resolve("Ghost"));

        Assert.True(ex.IsNotConfigured);
        Assert.Contains("No connection named 'Ghost' is configured", ex.Message);
        // Nothing about the connections that *do* exist leaks through a probe for one that does not.
        Assert.DoesNotContain("internal.example.com", ex.Message);
        Assert.DoesNotContain("SECRET", ex.Message);
    }

    [Fact]
    public void Resolver_RequiresAName()
    {
        var ex = Assert.Throws<ConnectionException>(() => Resolver(Config(SampleConfig)).Resolve("  "));
        Assert.True(ex.IsNotConfigured);
    }

    [Fact]
    public void Resolver_ReportsAKindWithNoRegisteredConnector()
    {
        var resolver = new ConnectionResolver(
            new InMemoryConnectionRegistry(new[]
            {
                new ConnectionDefinition { Name = "Warehouse", Kind = "sql", Target = "Server=db;Password=hunter2" }
            }),
            Array.Empty<IConnectorFactory>());

        var ex = Assert.Throws<ConnectionException>(() => resolver.Resolve("Warehouse"));

        Assert.False(ex.IsNotConfigured);
        Assert.Contains("no connector is registered for kind 'sql'", ex.Message);
        Assert.DoesNotContain("hunter2", ex.Message);
    }

    [Fact]
    public void Resolver_ListsWhatTheRegistryHolds()
    {
        Assert.Equal(2, Resolver(Config(SampleConfig)).List().Count);
    }
}
