using TerraFluent.AutoAnalytics.DependencyInjection;
using TerraFluent.Chart.Reporting.Api.Middleware;

var builder = WebApplication.CreateBuilder(args);

// ── Services ──────────────────────────────────────────────────────────────────

// Deterministic auto-analytics engine (schema → validation → profiling → analytics → insights).
builder.Services.AddAutoAnalytics();

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
                      "a ready-to-embed auto dashboard — no AI, fully deterministic."
    });

    // Include XML doc comments in Swagger UI.
    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
        opts.IncludeXmlComments(xmlPath);
});

// Allow all origins in development; restrict in production via configuration.
builder.Services.AddCors(opts =>
    opts.AddDefaultPolicy(p =>
        p.WithOrigins(builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
                        ?? ["*"])
         .AllowAnyHeader()
         .AllowAnyMethod()));

builder.Services.AddHealthChecks();

// ── App pipeline ──────────────────────────────────────────────────────────────

var app = builder.Build();

app.UseMiddleware<ChartExceptionMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(opts =>
    {
        opts.SwaggerEndpoint("/swagger/v1/swagger.json", "TerraFluent Chart API v1");
        opts.RoutePrefix = string.Empty; // Swagger UI at root
    });
}

app.UseCors();
app.MapHealthChecks("/health");
app.MapControllers();

app.Run();

// Exposes the implicit Program class to the integration-test host (WebApplicationFactory<Program>).
public partial class Program { }
