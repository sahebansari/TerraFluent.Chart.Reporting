using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace TerraFluent.Chart.Reporting.Api.Tests;

/// <summary>
/// Integration tests for the chart-render endpoints, exercised through the real HTTP pipeline.
/// </summary>
public sealed class ChartsEndpointsTests : IClassFixture<WebApplicationFactory<Program>>
{
    private const string SampleOptions =
        "{\"title\":{\"text\":\"Shared Chart\"}," +
        "\"xAxis\":{\"categories\":[\"Q1\",\"Q2\",\"Q3\",\"Q4\"]}," +
        "\"series\":[{\"name\":\"Revenue\",\"type\":\"Column\",\"data\":[120,150,170,210]}]}";

    private readonly WebApplicationFactory<Program> _factory;

    public ChartsEndpointsTests(WebApplicationFactory<Program> factory) => _factory = factory;

    private static string Encode(string json) =>
        Convert.ToBase64String(Encoding.UTF8.GetBytes(json));

    [Fact]
    public async Task SharedChart_WithEncodedOptions_ReturnsSvg()
    {
        var client = _factory.CreateClient();
        var encoded = Uri.EscapeDataString(Encode(SampleOptions));

        var response = await client.GetAsync($"/api/charts/shared?options={encoded}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("image/svg+xml", response.Content.Headers.ContentType?.MediaType);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("<svg", body);
        Assert.Contains("Shared Chart", body);
        // Theme font embedded so the SVG renders correctly when loaded via an isolated <img>.
        Assert.Contains("@font-face", body);
        Assert.Contains("data:font/woff2;base64,", body);
    }

    [Fact]
    public async Task SharedChart_SerifTheme_EmbedsThemeFontQuoted()
    {
        var client = _factory.CreateClient();
        // Sunset theme uses the serif family "Source Serif 4".
        var json = SampleOptions.Insert(1, "\"themeName\":\"Sunset\",");
        var encoded = Uri.EscapeDataString(Encode(json));

        var body = await (await client.GetAsync($"/api/charts/shared?options={encoded}"))
            .Content.ReadAsStringAsync();

        Assert.Contains("'Source Serif 4'", body);                 // multi-word name quoted in CSS
        Assert.Contains("font-family:'Source Serif 4'", body);      // embedded @font-face for the theme font
        Assert.Contains("data:font/woff2;base64,", body);
    }

    [Fact]
    public async Task RenderSvg_EmbedsThemeFont()
    {
        var client = _factory.CreateClient();
        var content = new StringContent(SampleOptions, Encoding.UTF8, "application/json");

        var response = await client.PostAsync("/api/charts/svg", content);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("image/svg+xml", response.Content.Headers.ContentType?.MediaType);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("<svg", body);
        // Downloaded/standalone SVG must carry its font so it isn't system-font on open.
        Assert.Contains("@font-face", body);
        Assert.Contains("data:font/woff2;base64,", body);
    }

    [Fact]
    public async Task SharedChart_MissingOptions_ReturnsBadRequest()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/charts/shared");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task SharedChart_InvalidBase64_ReturnsBadRequest()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/charts/shared?options=not%20base64%21%21%21");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task SharedChart_WithFontScale_EnlargesTitleFont()
    {
        var client = _factory.CreateClient();
        var baseEnc  = Uri.EscapeDataString(Encode(SampleOptions));
        var scaledEnc = Uri.EscapeDataString(Encode(
            SampleOptions.TrimEnd('}') + ",\"fontScale\":1.2}"));

        var baseSvg   = await (await client.GetAsync($"/api/charts/shared?options={baseEnc}")).Content.ReadAsStringAsync();
        var scaledSvg = await (await client.GetAsync($"/api/charts/shared?options={scaledEnc}")).Content.ReadAsStringAsync();

        Assert.Contains("chart-title", baseSvg);
        Assert.Contains("bold 16px", baseSvg);   // default FontScale 1.0
        Assert.Contains("bold 19px", scaledSvg); // 16 * 1.2 = 19.2 -> 19
    }
}
