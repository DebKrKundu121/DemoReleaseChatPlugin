using System.Text.Json;
using DemoReleaseChatPlugin.Contracts.Releases.V1.GetReleases;
using DemoReleaseChatPlugin.Sdk.V1;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DemoReleaseChatPlugin.RazorComponents;

public enum DemoReleaseChatPluginReleasesStatus
{
    Loaded,

    /// 404 - the name doesn't match a project.
    UnknownApplication,

    /// Anything else.
    Unavailable
}

public sealed class DemoReleaseChatPluginReleasesOutcome
{
    public required DemoReleaseChatPluginReleasesStatus Status { get; init; }
    public GetReleasesResult? Value { get; init; }
}

/// Wraps the SDK with caching and turns HTTP outcomes into three named states.
public sealed class DemoReleaseChatPluginReleaseNotesClient(
    IDemoReleaseChatPluginReleasesService service,
    IMemoryCache cache,
    IOptions<DemoReleaseChatPluginReleaseNotesOptions> options,
    ILogger<DemoReleaseChatPluginReleaseNotesClient> logger)
{
    private const string CachePrefix = "DemoReleaseChatPlugin.RazorComponents:releases";

    /// Failures are cached too, but only briefly - long enough to stop a dashboard hammering a down
    /// service on every render, short enough to recover quickly.
    private static readonly TimeSpan FailureCacheDuration = TimeSpan.FromSeconds(30);

    private readonly DemoReleaseChatPluginReleaseNotesOptions _options = options.Value;

    public string ConfiguredApplicationName => _options.ApplicationName;

    public async Task<DemoReleaseChatPluginReleasesOutcome> GetReleasesAsync(
        string applicationName, DateTime? from, DateTime? to, CancellationToken cancellationToken)
    {
        var cacheKey = $"{CachePrefix}:{applicationName}:{Day(from)}:{Day(to)}";

        if (cache.TryGetValue(cacheKey, out DemoReleaseChatPluginReleasesOutcome? cached) && cached is not null)
            return cached;

        var outcome = await FetchAsync(applicationName, from, to, cancellationToken);

        var duration = outcome.Status == DemoReleaseChatPluginReleasesStatus.Loaded
            ? _options.CacheDuration
            : FailureCacheDuration;

        cache.Set(cacheKey, outcome, duration);

        return outcome;
    }

    private async Task<DemoReleaseChatPluginReleasesOutcome> FetchAsync(
        string applicationName, DateTime? from, DateTime? to, CancellationToken cancellationToken)
    {
        try
        {
            var response = await service.ExecuteRequestAsync(applicationName, from, to, cancellationToken);

            if (response.IsSuccessStatusCode && response.Content is not null)
                return new DemoReleaseChatPluginReleasesOutcome
                {
                    Status = DemoReleaseChatPluginReleasesStatus.Loaded,
                    Value = response.Content
                };

            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                logger.LogWarning(
                    "DemoReleaseChatPlugin does not recognise the application name '{ApplicationName}'. {Detail}",
                    applicationName,
                    ProblemDetail(response.Error?.Content)
                        ?? "The name must match the project's name in DemoReleaseChatPlugin exactly.");

                return new DemoReleaseChatPluginReleasesOutcome
                {
                    Status = DemoReleaseChatPluginReleasesStatus.UnknownApplication
                };
            }

            logger.LogWarning(
                "Reading releases for '{ApplicationName}' returned {StatusCode}.",
                applicationName, (int)response.StatusCode);
        }
        catch (OperationCanceledException)
        {
            // Rethrown, not swallowed - a cancelled render must not be cached as a failure.
            throw;
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Could not read releases for '{ApplicationName}'.", applicationName);
        }

        return new DemoReleaseChatPluginReleasesOutcome
        {
            Status = DemoReleaseChatPluginReleasesStatus.Unavailable
        };
    }

    private static string Day(DateTime? value) => value?.ToString("yyyyMMdd") ?? "all";

    /// Parses a ProblemDetails body for its "detail" field so the log line names the real problem.
    private static string? ProblemDetail(string? body)
    {
        if (string.IsNullOrWhiteSpace(body)) return null;

        try
        {
            using var document = JsonDocument.Parse(body);

            return document.RootElement.TryGetProperty("detail", out var detail)
                ? detail.GetString()
                : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
