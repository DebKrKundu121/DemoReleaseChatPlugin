using DoverPort.Common.Models;

namespace DemoReleaseChatPlugin.Application.Releases.Publishing;

/// <summary>
/// A seam for pushing release notes into the target application. Only a no-op is registered today;
/// CreateLiveReleaseHandler already calls it, so retry and fallback alerting can attach behind this
/// interface when it is implemented. Keep the seam.
/// </summary>
public interface IReleaseNotesPublisher
{
    Task<Result> PublishAsync(
        Guid releaseNoticeId, string projectName, string releaseNotes, CancellationToken cancellationToken);
}
