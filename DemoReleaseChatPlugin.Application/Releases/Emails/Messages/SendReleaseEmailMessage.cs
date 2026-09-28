namespace DemoReleaseChatPlugin.Application.Releases.Emails.Messages;

/// <summary>
/// One queued message PER RECIPIENT, carrying its already-rendered subject and body.
///
/// This is the deliberate exception to the identifiers-only rule. An EVENT announces something and
/// its listener should see current data; a QUEUED EMAIL is a unit of work whose content must not
/// change between queueing and delivery. SendReleaseEmailConsumer touches no database at all, which
/// is what makes a retry days later safe when the notice may have been edited since.
/// Don't "tidy" these down to ids.
/// </summary>
public sealed class SendReleaseEmailMessage
{
    public const string Topic = "DemoReleaseChatPlugin.Releases.SendReleaseEmail";

    public Guid ReleaseNoticeId { get; set; }
    public string ProjectName { get; set; }
    public string NoticeKind { get; set; }
    public string Audience { get; set; }

    public string RecipientName { get; set; }
    public string RecipientEmail { get; set; }
    public string Subject { get; set; }
    public string BodyHtml { get; set; }
}
