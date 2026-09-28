using DemoReleaseChatPlugin.Contracts.Releases.V1;
using DemoReleaseChatPlugin.Contracts.Releases.V1.Common;
using DemoReleaseChatPlugin.Contracts.Releases.V1.GetReleases;
using Refit;

namespace DemoReleaseChatPlugin.Sdk.V1;

/// <summary>
/// Refit only - no gateway or auth tooling, because the API is anonymous and addressed directly
/// rather than through an API gateway, so there is no user context to forward.
///
/// Each endpoint appears TWICE. ExecuteAsync returns the value and throws ApiException on non-2xx;
/// ExecuteRequestAsync returns ApiResponse&lt;T&gt; so the caller can inspect the status code
/// without exception handling. The card uses the second form, because a 404 (unknown application)
/// is a NORMAL outcome it must render differently from a real failure.
/// </summary>
public interface IDemoReleaseChatPluginReleasesService
{
    [Get(ReleaseApiRoutes.GetReleases)]
    Task<GetReleasesResult> ExecuteAsync(
        string applicationName,
        [AliasAs(ReleaseApiRoutes.FromQueryParameter)] DateTime? from = null,
        [AliasAs(ReleaseApiRoutes.ToQueryParameter)] DateTime? to = null,
        CancellationToken cancellationToken = default);

    [Get(ReleaseApiRoutes.GetReleases)]
    Task<ApiResponse<GetReleasesResult>> ExecuteRequestAsync(
        string applicationName,
        [AliasAs(ReleaseApiRoutes.FromQueryParameter)] DateTime? from = null,
        [AliasAs(ReleaseApiRoutes.ToQueryParameter)] DateTime? to = null,
        CancellationToken cancellationToken = default);

    [Get(ReleaseApiRoutes.GetLatestRelease)]
    Task<ReleaseDto> ExecuteLatestAsync(string applicationName, CancellationToken cancellationToken = default);

    [Get(ReleaseApiRoutes.GetLatestRelease)]
    Task<ApiResponse<ReleaseDto>> ExecuteRequestLatestAsync(
        string applicationName, CancellationToken cancellationToken = default);
}
