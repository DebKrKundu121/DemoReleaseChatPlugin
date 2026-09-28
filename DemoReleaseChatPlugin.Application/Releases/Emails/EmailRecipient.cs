using DemoReleaseChatPlugin.Domain.Releases.Values;

namespace DemoReleaseChatPlugin.Application.Releases.Emails;

/// <summary>
/// One address on a release announcement, with the reason it is there.
/// <c>Email</c> is an <see cref="EmailAddress"/>, not a string: the audience resolver projects it
/// straight out of the Stakeholder column through the value converter, and it stays a value object
/// right up to the point where <c>ReleaseEmailMessages</c> renders the queued message, which is
/// where it becomes wire data.
/// </summary>
public sealed class EmailRecipient
{
    public NotificationAudience Audience { get; set; }
    public string Name { get; set; }
    public EmailAddress Email { get; set; }
}
