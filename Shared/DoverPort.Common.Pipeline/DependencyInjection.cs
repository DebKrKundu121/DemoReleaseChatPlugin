using DoverPort.Common.Pipeline.Internal;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace DoverPort.Common.Pipeline;

public static class DependencyInjection
{
    public static PodPipelineBuilder AddPodPipeline(this IHostApplicationBuilder builder) => new(builder);

    public static void MapPodHealthCheckEndpoints(this WebApplication app)
    {
        app.UseRequestTimeouts();
        app.UseOutputCache();

        var healthChecks = app.MapGroup("");

        healthChecks
            .CacheOutput("HealthChecks")
            .WithRequestTimeout("HealthChecks");

        healthChecks.MapHealthChecks("/health").AllowAnonymous();

        healthChecks
            .MapHealthChecks("/alive", new HealthCheckOptions { Predicate = r => r.Tags.Contains("live") })
            .AllowAnonymous();
    }
}
