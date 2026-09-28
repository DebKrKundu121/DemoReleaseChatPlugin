using DemoReleaseChatPlugin.Application.Releases.Emails;
using DemoReleaseChatPlugin.Domain.Releases.Values;
using DemoReleaseChatPlugin.Application.Releases.Emails.Messages;

namespace DemoReleaseChatPlugin.Application.Releases.Notices;

/// <summary>
/// Fans one composed email out across recipients - ONE MESSAGE PER ADDRESS.
///
/// Sending every address inside the first listener would mean one bounce re-sending to everyone.
/// Per-recipient also keeps external stakeholders from seeing each other's addresses.
/// </summary>
public static class ReleaseEmailMessages
{
    public static IReadOnlyCollection<SendReleaseEmailMessage> For(
        ReleaseNoticeId releaseNoticeId, ReleaseNoticeKind noticeKind, string projectName,
        string subject, string bodyHtml, IReadOnlyCollection<EmailRecipient> recipients)
    {
        if (recipients is null || recipients.Count == 0)
            return [];

        return recipients.Select(recipient => new SendReleaseEmailMessage
        {
            ReleaseNoticeId = releaseNoticeId.Value,
            NoticeKind = noticeKind.Value,
            ProjectName = projectName,
            Audience = recipient.Audience.Value,
            RecipientName = recipient.Name,
            RecipientEmail = recipient.Email.Value,
            Subject = subject,
            BodyHtml = bodyHtml
        }).ToList();
    }
}
