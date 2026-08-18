using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using TerraFluent.AutoAnalytics.DependencyInjection;
using TerraFluent.Chart.Reporting.Api.Middleware;

var builder = WebApplication.CreateBuilder(args);

// Cap the request body to guard against oversized uploads.
long maxBodyBytes = builder.Configuration.GetValue("Limits:MaxRequestBodyBytes", 10L * 1024 * 1024);
builder.WebHost.ConfigureKestrel(o => o.Limits.MaxRequestBodySize = maxBodyBytes);

// ── Services ──────────────────────────────────────────────────────────────────

// Deterministic auto-analytics engine (schema → validation → profiling → analytics → insights).
builder.Services.AddAutoAnalytics();

// In-memory store for multi-turn analytic sessions.
builder.Services.AddSingleton<TerraFluent.Chart.Reporting.Api.Services.SessionStore>();

builder.Services.AddControllers()
    .AddJsonOptions(opts =>
    {
        opts.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
        opts.JsonSerializerOptions.WriteIndented = false;
        // Accept enum values as their names (e.g. "csv") in addition to their numeric values.
        opts.JsonSerializerOptions.Converters.Add(
            new System.Text.Json.Serialization.JsonStringEnumConverter());
    });

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(opts =>
{
    opts.SwaggerDoc("v1", new()
    {
        Title       = "TerraFluent Chart & Analytics API",
        Version     = "v1",
        Description = "Server-side SVG chart generation plus a deterministic auto-analytics service. " +
                      "POST a ChartOptions JSON body to render Line, Column, Pie, Area, Scatter, " +
                      "Heatmap, Candlestick and more; or POST raw CSV/JSON data to the analytics " +
                      "endpoints to get profiling, insights, anomalies, chart recommendations and " +
                      "a ready-to-embed smart dashboard — no AI, fully deterministic."
    });

    opts.AddSecurityDefinition("ApiKey", new()
    {
        Name        = "X-Api-Key",
        Type        = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
        In          = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description = "Optional API key. Required only when the server is configured with an ApiKey."
    });

    // Include XML doc comments in Swagger UI.
    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
        opts.IncludeXmlComments(xmlPath);
});

// CORS: allow any origin when "*" (or nothing) is configured; otherwise restrict to the listed origins.
string[] allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? ["*"];
builder.Services.AddCors(opts =>
    opts.AddDefaultPolicy(p =>
    {
        if (allowedOrigins.Length == 0 || allowedOrigins.Contains("*"))
            p.AllowAnyOrigin();
        else
            p.WithOrigins(allowedOrigins);
        p.AllowAnyHeader().AllowAnyMethod();
    }));

// Rate limiting (fixed window, partitioned by client IP).
bool rateLimitEnabled = builder.Configuration.GetValue("RateLimiting:Enabled", true);
int permitLimit = builder.Configuration.GetValue("RateLimiting:PermitLimit", 1000);
int windowSeconds = builder.Configuration.GetValue("RateLimiting:WindowSeconds", 60);
int queueLimit = builder.Configuration.GetValue("RateLimiting:QueueLimit", 0);
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(ctx =>
    {
        string key = ctx.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        return RateLimitPartition.GetFixedWindowLimiter(key, _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = permitLimit,
            Window = TimeSpan.FromSeconds(windowSeconds),
            QueueLimit = queueLimit
        });
    });
});

builder.Services.AddHealthChecks()
    .AddCheck<TerraFluent.Chart.Reporting.Api.Services.AnalyticsHealthCheck>("analytics", tags: ["ready"]);

// ── App pipeline ──────────────────────────────────────────────────────────────

var app = builder.Build();

// Security response headers on every response.
app.Use(async (ctx, next) =>
{
    var h = ctx.Response.Headers;
    h["X-Content-Type-Options"] = "nosniff";
    h["X-Frame-Options"] = "DENY";
    h["Referrer-Policy"] = "no-referrer";
    await next();
});

if (!app.Environment.IsDevelopment())
    app.UseHttpsRedirection();

app.UseMiddleware<ChartExceptionMiddleware>();
app.UseMiddleware<ApiKeyMiddleware>();

// OpenAPI document + UI served in all environments.
app.UseSwagger();
app.UseSwaggerUI(opts =>
{
    opts.SwaggerEndpoint("/swagger/v1/swagger.json", "TerraFluent Chart API v1");
    opts.RoutePrefix = string.Empty; // Swagger UI at root
});

app.UseCors();

if (rateLimitEnabled)
    app.UseRateLimiter();

// Liveness = process is up (no dependency checks); readiness runs checks tagged "ready".
app.MapHealthChecks("/health/live", new() { Predicate = _ => false });
app.MapHealthChecks("/health/ready", new() { Predicate = check => check.Tags.Contains("ready") });
app.MapHealthChecks("/health"); // backwards-compatible alias (all checks)

app.MapControllers();

app.Run();

// Exposes the implicit Program class to the integration-test host (WebApplicationFactory<Program>).
public partial class Program { }
