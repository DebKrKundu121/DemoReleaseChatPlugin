using DemoReleaseChatPlugin.Domain.Releases.Values;

namespace DemoReleaseChatPlugin.Application.Releases.Emails.Messages;

public sealed class ScheduledReleaseAnnouncement
{
    public Guid ReleaseNoticeId { get; set; }
    public string ProjectName { get; set; }
    public string Version { get; set; }
    public DateTime ScheduledFor { get; set; }
    public string Summary { get; set; }
    public RiskLevel RiskLevel { get; set; }
    public string RiskNotes { get; set; }
    public string LeadDeveloper { get; set; }
    public IReadOnlyCollection<EmailRecipient> Recipients { get; set; } = [];
}

public sealed class LiveReleaseAnnouncement
{
    public Guid ReleaseNoticeId { get; set; }
    public string ProjectName { get; set; }
    public string Version { get; set; }
    public DateTime DeployedOn { get; set; }
    public string ReleaseNotes { get; set; }
    public string LeadDeveloper { get; set; }
    public IReadOnlyCollection<EmailRecipient> Recipients { get; set; } = [];
}

public sealed class RollbackAnnouncement
{
    public Guid ReleaseNoticeId { get; set; }
    public string ProjectName { get; set; }
    public string Version { get; set; }
    public DateTime RolledBackOn { get; set; }
    public string RollbackReason { get; set; }
    public DateTime OriginalDeployedOn { get; set; }
    public string LeadDeveloper { get; set; }
    public IReadOnlyCollection<EmailRecipient> Recipients { get; set; } = [];
}
