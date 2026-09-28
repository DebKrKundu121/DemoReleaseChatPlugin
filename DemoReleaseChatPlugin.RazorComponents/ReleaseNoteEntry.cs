using DemoReleaseChatPlugin.Contracts.Releases.V1.Common;
using DemoReleaseChatPlugin.Contracts.Releases.V1.GetReleases;

namespace DemoReleaseChatPlugin.RazorComponents;

public sealed class ReleaseNoteEntry
{
    public Guid Id { get; init; }
    public string Heading { get; init; } = "";
    public IReadOnlyList<string> Lines { get; init; } = [];
    public bool IsLatest { get; init; }
    public bool IsRollback { get; init; }
    public bool IsRolledBack { get; init; }

    /// Rollbacks get a "Reason:" caption above their lines.
    public string? LinesCaption { get; init; }

    public static IReadOnlyList<ReleaseNoteEntry> Build(
        GetReleasesResult? result, bool showRollbacks, int? take)
    {
        if (result?.Releases is null) return [];

        var releases = result.Releases.AsEnumerable();

        if (!showRollbacks)
            releases = releases.Where(x => x.Kind != ReleaseKinds.Rollback);

        var ordered = releases.ToList();

        if (take.HasValue)
            ordered = ordered.Take(take.Value).ToList();

        return ordered.Select(release => Build(release, result)).ToList();
    }

    private static ReleaseNoteEntry Build(ReleaseDto release, GetReleasesResult result)
    {
        var isRollback = release.Kind == ReleaseKinds.Rollback;

        return new ReleaseNoteEntry
        {
            Id = release.Id,
            Heading = BuildHeading(release, result),
            Lines = ReleaseNoteLines.From(release.Detail),
            IsLatest = result.LatestReleaseId == release.Id,
            IsRollback = isRollback,
            IsRolledBack = release.IsRolledBack,
            LinesCaption = isRollback ? "Reason:" : null
        };
    }

    private static string BuildHeading(ReleaseDto release, GetReleasesResult result)
    {
        if (release.Kind != ReleaseKinds.Rollback)
            return $"Release {release.Version} - {release.OccurredOn:dd/MM/yyyy}";

        // When the reverted release is in the same payload we can name its original date; when the
        // window excludes it, we simply leave that clause out.
        var reverted = result.Releases.FirstOrDefault(x => x.Id == release.RollbackOfReleaseId);

        return reverted is null
            ? $"Rolled back {release.Version} - {release.OccurredOn:dd/MM/yyyy}"
            : $"Rolled back {release.Version} (released {reverted.OccurredOn:dd/MM/yyyy}) - {release.OccurredOn:dd/MM/yyyy}";
    }
}
