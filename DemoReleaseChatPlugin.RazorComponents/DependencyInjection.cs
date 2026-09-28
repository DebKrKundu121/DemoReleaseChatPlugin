using DemoReleaseChatPlugin.Sdk;
using DemoReleaseChatPlugin.Sdk.V1;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DemoReleaseChatPlugin.RazorComponents;

public static class DependencyInjection
{
    public const string ApplicationNameConfigurationKey = "DemoReleaseChatPlugin:ApplicationName";

    public static IServiceCollection AddDemoReleaseChatPluginReleaseNotes(
        this IServiceCollection services,
        IConfiguration configuration,
        Action<DemoReleaseChatPluginReleaseNotesOptions>? configure = null)
    {
        var options = new DemoReleaseChatPluginReleaseNotesOptions
        {
            ApplicationName = configuration[ApplicationNameConfigurationKey] ?? ""
        };

        configure?.Invoke(options);

        if (string.IsNullOrWhiteSpace(options.ApplicationName))
            throw new InvalidOperationException(
                $"'{ApplicationNameConfigurationKey}' is not configured and no application name was " +
                "supplied, so the release notes components do not know which application to ask about.");

        // Only register the HTTP client if the consumer hasn't already registered the SDK its own way.
        if (services.All(x => x.ServiceType != typeof(IDemoReleaseChatPluginReleasesService)))
        {
            var baseUrl = options.ApiBaseUrl ?? configuration[Sdk.DependencyInjection.BaseUrlConfigurationKey];

            if (string.IsNullOrWhiteSpace(baseUrl))
                throw new InvalidOperationException(
                    $"'{Sdk.DependencyInjection.BaseUrlConfigurationKey}' is not configured, so the " +
                    "release notes components have no address to call.");

            services.AddDemoReleaseChatPluginReleasesService(client =>
            {
                client.BaseAddress = new Uri(baseUrl);
                client.Timeout = options.RequestTimeout;
            });
        }

        return services.AddDemoReleaseChatPluginReleaseNotes(o =>
        {
            o.ApplicationName = options.ApplicationName;
            o.CacheDuration = options.CacheDuration;
            o.RequestTimeout = options.RequestTimeout;
            o.ApiBaseUrl = options.ApiBaseUrl;
        });
    }

    public static IServiceCollection AddDemoReleaseChatPluginReleaseNotes(
        this IServiceCollection services, Action<DemoReleaseChatPluginReleaseNotesOptions> configure)
    {
        services.Configure(configure);
        services.AddMemoryCache();
        services.AddScoped<DemoReleaseChatPluginReleaseNotesClient>();
        return services;
    }
}
