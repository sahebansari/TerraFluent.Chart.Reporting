using System.Net.Http.Headers;
using Microsoft.AspNetCore.StaticFiles;

var builder = WebApplication.CreateBuilder(args);

// Upstream analytics/chart API. The web app proxies all /api/* calls to it (BFF pattern),
// so the browser only ever talks same-origin — no CORS or mixed-content concerns.
string apiBaseUrl = builder.Configuration.GetValue<string>("Api:BaseUrl") ?? "http://localhost:49684";

builder.Services.AddHttpClient("api", client =>
{
    client.BaseAddress = new Uri(apiBaseUrl, UriKind.Absolute);
    client.Timeout = TimeSpan.FromMinutes(2);
})
.ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
{
    // Talk to the upstream directly — never route localhost through a system/corporate proxy.
    UseProxy = false,
    // Accept the ASP.NET Core dev certificate when the upstream is HTTPS on localhost.
    ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
});

var app = builder.Build();

// Serve text-based static assets as UTF-8 — the default .css/.js mappings omit the
// charset, so browsers fall back to Windows-1252 and mangle comment glyphs (─, —).
var contentTypes = new FileExtensionContentTypeProvider();
contentTypes.Mappings[".css"]  = "text/css; charset=utf-8";
contentTypes.Mappings[".js"]   = "text/javascript; charset=utf-8";
contentTypes.Mappings[".json"] = "application/json; charset=utf-8";
contentTypes.Mappings[".svg"]  = "image/svg+xml; charset=utf-8";

app.UseDefaultFiles();
app.UseStaticFiles(new StaticFileOptions
{
    ContentTypeProvider = contentTypes,
    // Force the browser to revalidate JS/CSS/HTML every load so edits take effect without a hard
    // refresh (the server still answers 304 when unchanged, so it stays cheap). The SPA's ES modules
    // import each other by relative path, which a version query on the entry script can't cache-bust.
    OnPrepareResponse = ctx =>
    {
        string ext = System.IO.Path.GetExtension(ctx.File.Name);
        if (ext is ".js" or ".css" or ".html" or ".json")
            ctx.Context.Response.Headers["Cache-Control"] = "no-cache, must-revalidate";
    }
});

// ── Reverse proxy: forward every /api/** request to the upstream API ─────────────
app.Map("/api/{**path}", async (HttpContext ctx, IHttpClientFactory factory) =>
{
    var client = factory.CreateClient("api");

    string target = ctx.Request.Path + ctx.Request.QueryString;
    using var upstream = new HttpRequestMessage(new HttpMethod(ctx.Request.Method), target);

    // Forward the request body for methods that carry one.
    if (ctx.Request.ContentLength > 0 || ctx.Request.Headers.ContainsKey("Transfer-Encoding"))
    {
        var buffer = new MemoryStream();
        await ctx.Request.Body.CopyToAsync(buffer);
        buffer.Position = 0;
        upstream.Content = new StreamContent(buffer);
        if (!string.IsNullOrEmpty(ctx.Request.ContentType))
            upstream.Content.Headers.ContentType = MediaTypeHeaderValue.Parse(ctx.Request.ContentType);
    }

    // Forward a curated set of client headers.
    if (ctx.Request.Headers.TryGetValue("Accept", out var accept))
        upstream.Headers.TryAddWithoutValidation("Accept", (string[])accept!);
    if (ctx.Request.Headers.TryGetValue("X-Api-Key", out var apiKey))
        upstream.Headers.TryAddWithoutValidation("X-Api-Key", (string[])apiKey!);

    using var response = await client.SendAsync(upstream, HttpCompletionOption.ResponseHeadersRead, ctx.RequestAborted);

    ctx.Response.StatusCode = (int)response.StatusCode;
    if (response.Content.Headers.ContentType is { } ct)
        ctx.Response.ContentType = ct.ToString();

    await response.Content.CopyToAsync(ctx.Response.Body, ctx.RequestAborted);
});

// SPA fallback — any unmatched route serves the shell.
app.MapFallbackToFile("index.html");

app.Run();
