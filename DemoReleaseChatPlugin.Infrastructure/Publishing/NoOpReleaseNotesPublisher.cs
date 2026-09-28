using DemoReleaseChatPlugin.Application.Releases.Publishing;
using DoverPort.Common.Models;
using Microsoft.Extensions.Logging;

namespace DemoReleaseChatPlugin.Infrastructure.Publishing;

public class NoOpReleaseNotesPublisher(ILogger<NoOpReleaseNotesPublisher> logger) : IReleaseNotesPublisher
{
    public Task<Result> PublishAsync(
        Guid releaseNoticeId, string projectName, string releaseNotes, CancellationToken cancellationToken)
    {
        logger.LogDebug(
            "Release notes publishing is not implemented yet; skipping push for {ProjectName} ({ReleaseNoticeId}).",
            projectName, releaseNoticeId);

        return Task.FromResult(Result.OK());
    }
}
