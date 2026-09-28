using DoverPort.Common.Pipeline;
using Microsoft.Extensions.Hosting;

namespace DemoReleaseChatPlugin.Application;

public static class DependencyInjection
{
    /// <summary>
    /// One of exactly two composition roots. WithLogging() is deliberately NOT called - it needs an
    /// Application Insights connection string this app does not have.
    /// </summary>
    public static void AddDemoReleaseChatPluginApplication(this IHostApplicationBuilder builder)
    {
        builder
            .AddPodPipeline()
            .WithValidatorsFromAssemblyContaining<ApplicationAssemblyMarker>()
            .UseDataAnnotationDisplayNames();
    }
}
