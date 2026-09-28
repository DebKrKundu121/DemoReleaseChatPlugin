using DemoReleaseChatPlugin.Application.Releases.Emails;
using DemoReleaseChatPlugin.Application.Releases.Emails.Messages;
using DotNetCore.CAP;
using Microsoft.Extensions.Logging;

namespace DemoReleaseChatPlugin.Infrastructure.Messaging;

/// <summary>
/// No try/catch. A throw is how the consumer tells CAP to retry.
/// This consumer touches no database at all, which is what makes a retry days later safe.
/// </summary>
public class SendReleaseEmailConsumer(
    IReleaseNotificationSender sender,
    ILogger<SendReleaseEmailConsumer> logger)
    : ICapSubscribe
{
    [CapSubscribe(SendReleaseEmailMessage.Topic)]
    public async Task Handle(SendReleaseEmailMessage message, CancellationToken cancellationToken)
    {
        await sender.SendAsync(message, cancellationToken);

        logger.LogInformation(
            "Release notification for {NoticeKind} notice {ReleaseNoticeId} sent to {Email}.",
            message.NoticeKind, message.ReleaseNoticeId, message.RecipientEmail);
    }
}
