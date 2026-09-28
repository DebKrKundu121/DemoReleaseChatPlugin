using DemoReleaseChatPlugin.Application.Releases.Emails.Messages;

namespace DemoReleaseChatPlugin.Application.Releases.Emails;

/// <summary>
/// MUST THROW on failure. A throw is how the consumer tells CAP to retry and eventually mark the
/// message failed; swallowing an error reports success and loses the email silently. This is the
/// inverse of an older never-throw contract, which existed only so a failure row could be written
/// to a table that no longer exists.
/// </summary>
public interface IReleaseNotificationSender
{
    Task SendAsync(SendReleaseEmailMessage message, CancellationToken cancellationToken);
}
