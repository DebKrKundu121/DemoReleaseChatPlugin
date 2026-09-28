using DemoReleaseChatPlugin.Domain.Releases.Values;

namespace DemoReleaseChatPlugin.Domain.Releases.Events;

public sealed class ScheduledReleaseFiled : ReleaseDomainEvent
{
    public ReleaseNoticeId ReleaseNoticeId { get; set; }
    public ProjectId ProjectId { get; set; }
}

public sealed class LiveReleaseRecorded : ReleaseDomainEvent
{
    public ReleaseNoticeId ReleaseNoticeId { get; set; }
    public ProjectId ProjectId { get; set; }
}

public sealed class ReleaseRolledBack : ReleaseDomainEvent
{
    public ReleaseNoticeId ReleaseNoticeId { get; set; }
    public ProjectId ProjectId { get; set; }
}
