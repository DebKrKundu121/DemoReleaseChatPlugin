using DemoReleaseChatPlugin.Sdk.V1;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Refit;

namespace DemoReleaseChatPlugin.Sdk;

public static class DependencyInjection
{
    public const string BaseUrlConfigurationKey = "DemoReleaseChatPlugin:ApiBaseUrl";

    /// <summary>
    /// Throws AT STARTUP rather than at first call when the base URL is missing, naming the exact
    /// config key. A misconfiguration that fails on boot is worth far more than one that fails at 3am.
    /// </summary>
    public static IServiceCollection AddDemoReleaseChatPluginReleasesService(
        this IServiceCollection services, IConfiguration configuration)
    {
        var baseUrl = configuration[BaseUrlConfigurationKey];

        if (string.IsNullOrWhiteSpace(baseUrl))
            throw new InvalidOperationException(
                $"'{BaseUrlConfigurationKey}' is not configured, so the DemoReleaseChatPlugin SDK " +
                "has no address to call.");

        return services.AddDemoReleaseChatPluginReleasesService(client => client.BaseAddress = new Uri(baseUrl));
    }

    public static IServiceCollection AddDemoReleaseChatPluginReleasesService(
        this IServiceCollection services, Action<HttpClient> configureClient)
    {
        services.AddRefitClient<IDemoReleaseChatPluginReleasesService>().ConfigureHttpClient(configureClient);
        return services;
    }
}
