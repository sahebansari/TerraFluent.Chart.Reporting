using Microsoft.Extensions.Diagnostics.HealthChecks;
using TerraFluent.AutoAnalytics.Engine;

namespace TerraFluent.Chart.Reporting.Api.Services;

/// <summary>
/// Readiness probe that exercises the analytics pipeline end-to-end on a tiny in-memory sample, so
/// <c>/health/ready</c> reports unhealthy if the engine cannot actually produce a result.
/// </summary>
internal sealed class AnalyticsHealthCheck(AnalyticsEngine engine) : IHealthCheck
{
    private const string Sample = "A,B\n1,2\n3,4\n";

    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var result = engine.Run(new AutoAnalytics.Data.Sources.CsvDataSource(Sample).Load());
            return Task.FromResult(result.Profile.RowCount == 2
                ? HealthCheckResult.Healthy("Analytics engine is operational.")
                : HealthCheckResult.Unhealthy("Analytics engine produced an unexpected result."));
        }
        catch (Exception ex)
        {
            return Task.FromResult(HealthCheckResult.Unhealthy("Analytics engine threw.", ex));
        }
    }
}
