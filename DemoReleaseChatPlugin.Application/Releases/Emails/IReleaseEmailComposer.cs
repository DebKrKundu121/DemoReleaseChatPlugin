using DemoReleaseChatPlugin.Application.Releases.Emails.Messages;

namespace DemoReleaseChatPlugin.Application.Releases.Emails;

/// Interface in Core, implementation in Infrastructure - the implementation depends on
/// PodEmailBuilder, which is an outbound-adapter concern.
public interface IReleaseEmailComposer
{
    (string Subject, string BodyHtml) Compose(ScheduledReleaseAnnouncement announcement);
    (string Subject, string BodyHtml) Compose(LiveReleaseAnnouncement announcement);
    (string Subject, string BodyHtml) Compose(RollbackAnnouncement announcement);
}
