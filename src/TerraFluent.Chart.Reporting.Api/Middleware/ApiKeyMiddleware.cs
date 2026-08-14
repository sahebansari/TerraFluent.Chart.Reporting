namespace TerraFluent.Chart.Reporting.Api.Middleware;

/// <summary>
/// Optional API-key gate. Enforcement is opt-in: when the <c>ApiKey</c> configuration value is empty
/// the middleware is a no-op (convenient for local/dev). When a key is configured, every request must
/// present it in the <c>X-Api-Key</c> header, except health checks and the Swagger UI/JSON.
/// </summary>
internal sealed class ApiKeyMiddleware(RequestDelegate next, IConfiguration configuration)
{
    private const string HeaderName = "X-Api-Key";
    private readonly string? _apiKey = configuration["ApiKey"];

    public async Task InvokeAsync(HttpContext ctx)
    {
        if (string.IsNullOrEmpty(_apiKey) || IsExempt(ctx.Request.Path))
        {
            await next(ctx);
            return;
        }

        if (!ctx.Request.Headers.TryGetValue(HeaderName, out var provided)
            || !FixedTimeEquals(provided.ToString(), _apiKey))
        {
            ctx.Response.StatusCode = StatusCodes.Status401Unauthorized;
            ctx.Response.ContentType = "application/problem+json";
            await ctx.Response.WriteAsync(
                "{\"type\":\"https://terrafluent.io/errors/unauthorized\",\"title\":\"Unauthorized\"," +
                "\"status\":401,\"detail\":\"A valid X-Api-Key header is required.\"}");
            return;
        }

        await next(ctx);
    }

    private static bool IsExempt(PathString path) =>
        path.StartsWithSegments("/health")
        || path.StartsWithSegments("/swagger")
        || path == "/";

    // Constant-time comparison to avoid leaking key length/content via timing.
    private static bool FixedTimeEquals(string a, string b)
    {
        var ba = System.Text.Encoding.UTF8.GetBytes(a);
        var bb = System.Text.Encoding.UTF8.GetBytes(b);
        return System.Security.Cryptography.CryptographicOperations.FixedTimeEquals(ba, bb);
    }
}
