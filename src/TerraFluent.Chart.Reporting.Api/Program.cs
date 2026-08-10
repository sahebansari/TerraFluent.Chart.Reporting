using TerraFluent.Chart.Reporting.Api.Middleware;

var builder = WebApplication.CreateBuilder(args);

// ── Services ──────────────────────────────────────────────────────────────────

builder.Services.AddControllers()
    .AddJsonOptions(opts =>
    {
        opts.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
        opts.JsonSerializerOptions.WriteIndented = false;
    });

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(opts =>
{
    opts.SwaggerDoc("v1", new()
    {
        Title       = "TerraFluent Chart API",
        Version     = "v1",
        Description = "Server-side SVG chart generation — zero JavaScript dependency. " +
                      "POST a ChartOptions JSON body to render Line, Column, Pie, Area, " +
                      "Scatter, Heatmap, Candlestick, and many more chart types."
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
