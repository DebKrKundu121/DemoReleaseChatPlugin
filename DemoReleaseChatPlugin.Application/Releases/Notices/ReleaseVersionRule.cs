using DemoReleaseChatPlugin.Application.Releases.Persistence;
using DemoReleaseChatPlugin.Domain.Releases;
using DemoReleaseChatPlugin.Domain.Releases.Values;
using Microsoft.EntityFrameworkCore;

namespace DemoReleaseChatPlugin.Application.Releases.Notices;

/// <summary>
/// The shared "must be newer" check, used by both create-notice validators.
/// Returns A MESSAGE OR NULL rather than a bool, so the caller can add the failure with wording
/// that names both versions.
/// </summary>
internal static class ReleaseVersionRule
{
    public static async Task<string> NotNewerThanLastReleaseAsync(
        IDemoReleaseChatPluginReadDb readDb, ProjectId projectId, string version, CancellationToken cancellationToken)
    {
        var candidate = version?.Trim();

        if (string.IsNullOrWhiteSpace(candidate) || projectId is null || projectId.IsEmpty)
            return null;

        var lastReleased = await LastReleasedVersionAsync(readDb, projectId, cancellationToken);

        if (string.IsNullOrWhiteSpace(lastReleased)) return null;
        if (!ReleaseVersion.TryCompare(candidate, lastReleased, out var comparison)) return null;
        if (comparison > 0) return null;

        return $"Version {candidate} is not newer than {lastReleased}, the last release for this project. " +
               "Please give a higher version number.";
    }

    /// Rolled-back releases are excluded: if 2.1.0 was reverted, 2.1.0 is legitimately releasable again.
    private static async Task<string> LastReleasedVersionAsync(
        IDemoReleaseChatPluginReadDb readDb, ProjectId projectId, CancellationToken cancellationToken)
    {
        var rolledBackIds = readDb.Rollbacks.Select(x => x.RollbackOfReleaseId);

        return await readDb.LiveReleases
            .Where(x => x.ProjectId == projectId && !rolledBackIds.Contains(x.Id))
            .OrderByDescending(x => x.DeployedOn)
            .ThenByDescending(x => x.CreatedOn)
            .Select(x => x.Version)
            .FirstOrDefaultAsync(cancellationToken);
    }
}
