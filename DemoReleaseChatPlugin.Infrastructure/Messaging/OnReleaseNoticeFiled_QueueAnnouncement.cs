using DemoReleaseChatPlugin.Domain.Releases.Events;
using DemoReleaseChatPlugin.Domain.Releases.Values;
using DemoReleaseChatPlugin.Application.Releases.Emails;
using DemoReleaseChatPlugin.Application.Releases.Emails.Messages;
using DemoReleaseChatPlugin.Application.Releases.Notices;
using DemoReleaseChatPlugin.Application.Releases.Persistence;
using DotNetCore.CAP;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DemoReleaseChatPlugin.Infrastructure.Messaging;

/// <summary>
/// Turns a filed notice into one queued email PER RECIPIENT.
///
/// Who gets what:
///   Scheduled - the mandatory list ONLY. Advance warning of a deployment window is an operational
///               concern, not a per-project one.
///   Live      - mandatory + the project's own active stakeholders.
///   Rollback  - mandatory + the project's own active stakeholders.
/// </summary>
public class OnReleaseNoticeFiled_QueueAnnouncement(
    IDemoReleaseChatPluginReadDb readDb,
    IReleaseAudienceResolver audienceResolver,
    IReleaseEmailComposer composer,
    ICapPublisher publisher,
    ILogger<OnReleaseNoticeFiled_QueueAnnouncement> logger)
    : ICapSubscribe
{
    // [CapSubscribe(nameof(X))] must match the topic the unit of work publishes, which is
    // domainEvent.GetType().Name. Rename an event class and the listener silently stops firing.
    [CapSubscribe(nameof(ScheduledReleaseFiled))]
    public async Task Handle(ScheduledReleaseFiled @event, CancellationToken cancellationToken)
    {
        // Projected rather than Include'd: Project is a separate aggregate, so the notice holds
        // only its id and this read joins for the name it needs.
        var notice = await readDb.ScheduledReleases
            .Where(x => x.Id == @event.ReleaseNoticeId)
            .Select(x => new
            {
                x.Id,
                ProjectName = readDb.Projects.Where(p => p.Id == x.ProjectId).Select(p => p.Name).FirstOrDefault(),
                x.Version,
                x.ScheduledFor,
                x.Summary,
                x.RiskLevel,
                x.RiskNotes,
                x.LeadDeveloper
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (notice is null)
        {
            LogMissingNotice(nameof(ScheduledReleaseFiled), @event.ReleaseNoticeId);
            return;
        }

        var recipients = await audienceResolver.MandatoryOnlyAsync(cancellationToken);

        var (subject, bodyHtml) = composer.Compose(new ScheduledReleaseAnnouncement
        {
            ReleaseNoticeId = notice.Id.Value,
            ProjectName = notice.ProjectName,
            Version = notice.Version,
            ScheduledFor = notice.ScheduledFor,
            Summary = notice.Summary,
            RiskLevel = notice.RiskLevel,
            RiskNotes = notice.RiskNotes,
            LeadDeveloper = notice.LeadDeveloper,
            Recipients = recipients
        });

        await QueueAsync(notice.Id, ReleaseNoticeKind.Scheduled, notice.ProjectName,
            subject, bodyHtml, recipients, cancellationToken);
    }

    [CapSubscribe(nameof(LiveReleaseRecorded))]
    public async Task Handle(LiveReleaseRecorded @event, CancellationToken cancellationToken)
    {
        var notice = await readDb.LiveReleases
            .Where(x => x.Id == @event.ReleaseNoticeId)
            .Select(x => new
            {
                x.Id,
                x.ProjectId,
                ProjectName = readDb.Projects.Where(p => p.Id == x.ProjectId).Select(p => p.Name).FirstOrDefault(),
                x.Version,
                x.DeployedOn,
                x.ReleaseNotes,
                x.LeadDeveloper
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (notice is null)
        {
            LogMissingNotice(nameof(LiveReleaseRecorded), @event.ReleaseNoticeId);
            return;
        }

        var recipients = await audienceResolver
            .MandatoryAndStakeholdersAsync(notice.ProjectId, cancellationToken);

        var (subject, bodyHtml) = composer.Compose(new LiveReleaseAnnouncement
        {
            ReleaseNoticeId = notice.Id.Value,
            ProjectName = notice.ProjectName,
            Version = notice.Version,
            DeployedOn = notice.DeployedOn,
            ReleaseNotes = notice.ReleaseNotes,
            LeadDeveloper = notice.LeadDeveloper,
            Recipients = recipients
        });

        await QueueAsync(notice.Id, ReleaseNoticeKind.Live, notice.ProjectName,
            subject, bodyHtml, recipients, cancellationToken);
    }

    [CapSubscribe(nameof(ReleaseRolledBack))]
    public async Task Handle(ReleaseRolledBack @event, CancellationToken cancellationToken)
    {
        // The reverted release's DeployedOn is pulled in by a sub-select rather than an Include,
        // because the email states when the reverted version originally went live and a notice no
        // longer holds a reference to the one it reverts - only its id.
        var notice = await readDb.Rollbacks
            .Where(x => x.Id == @event.ReleaseNoticeId)
            .Select(x => new
            {
                x.Id,
                x.ProjectId,
                ProjectName = readDb.Projects.Where(p => p.Id == x.ProjectId).Select(p => p.Name).FirstOrDefault(),
                x.Version,
                x.RolledBackOn,
                x.RollbackReason,
                OriginalDeployedOn = readDb.LiveReleases
                    .Where(l => l.Id == x.RollbackOfReleaseId).Select(l => l.DeployedOn).FirstOrDefault(),
                x.LeadDeveloper
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (notice is null)
        {
            LogMissingNotice(nameof(ReleaseRolledBack), @event.ReleaseNoticeId);
            return;
        }

        var recipients = await audienceResolver
            .MandatoryAndStakeholdersAsync(notice.ProjectId, cancellationToken);

        var (subject, bodyHtml) = composer.Compose(new RollbackAnnouncement
        {
            ReleaseNoticeId = notice.Id.Value,
            ProjectName = notice.ProjectName,
            Version = notice.Version,
            RolledBackOn = notice.RolledBackOn,
            RollbackReason = notice.RollbackReason,
            OriginalDeployedOn = notice.OriginalDeployedOn,
            LeadDeveloper = notice.LeadDeveloper,
            Recipients = recipients
        });

        await QueueAsync(notice.Id, ReleaseNoticeKind.Rollback, notice.ProjectName,
            subject, bodyHtml, recipients, cancellationToken);
    }

    private async Task QueueAsync(
        ReleaseNoticeId releaseNoticeId, ReleaseNoticeKind noticeKind, string projectName,
        string subject, string bodyHtml,
        IReadOnlyCollection<EmailRecipient> recipients, CancellationToken cancellationToken)
    {
        var messages = ReleaseEmailMessages.For(
            releaseNoticeId, noticeKind, projectName, subject, bodyHtml, recipients);

        foreach (var message in messages)
            await publisher.PublishAsync(
                SendReleaseEmailMessage.Topic, message, cancellationToken: cancellationToken);
    }

    /// Returns WITHOUT THROWING. Throwing would make CAP retry, and a retry cannot make a deleted
    /// row reappear - it would just burn ten attempts and mark the message failed.
    private void LogMissingNotice(string eventName, ReleaseNoticeId releaseNoticeId) =>
        logger.LogWarning(
            "{EventName} named release notice {ReleaseNoticeId}, which no longer exists, so no " +
            "announcement was queued. Returning without throwing: a retry cannot make the row reappear.",
            eventName, releaseNoticeId);
}
