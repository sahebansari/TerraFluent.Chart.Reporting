using System.Text.Json;
using TerraFluent.Chart.Reporting.Models;

namespace TerraFluent.Chart.Reporting.Api.Middleware;

/// <summary>
/// Converts library exceptions to RFC 7807 Problem Details responses.
/// Catches DataQualityException (422), ArgumentException (400), and unexpected errors (500).
/// </summary>
internal sealed class ChartExceptionMiddleware(RequestDelegate next, ILogger<ChartExceptionMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext ctx)
    {
        try
        {
            await next(ctx);
        }
        catch (DataQualityException ex)
        {
            await WriteProblem(ctx, 422, "data_quality_error",
                "One or more data-quality errors prevented rendering.",
                new { findings = ex.Report.Warnings.Select(w => new { severity = w.Severity.ToString(), message = w.Message }) });
        }
        catch (ArgumentException ex)
        {
            await WriteProblem(ctx, 400, "invalid_argument", ex.Message, null);
        }
        catch (NotSupportedException ex)
        {
            await WriteProblem(ctx, 400, "unsupported_operation", ex.Message, null);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unhandled exception rendering chart");
            await WriteProblem(ctx, 500, "internal_error",
                "An unexpected error occurred. Check server logs for details.", null);
        }
    }

    private static async Task WriteProblem(HttpContext ctx, int status, string type, string detail, object? extensions)
    {
        if (ctx.Response.HasStarted) return;

        ctx.Response.StatusCode  = status;
        ctx.Response.ContentType = "application/problem+json";

        var body = new Dictionary<string, object?>
        {
            ["type"]   = $"https://terrafluent.io/errors/{type}",
            ["title"]  = ReasonPhrase(status),
            ["status"] = status,
            ["detail"] = detail
        };
        if (extensions is not null)
            body["extensions"] = extensions;

        await ctx.Response.WriteAsync(JsonSerializer.Serialize(body));
    }

    private static string ReasonPhrase(int status) => status switch
    {
        400 => "Bad Request",
        422 => "Unprocessable Content",
        500 => "Internal Server Error",
        _   => "Error"
    };
}
