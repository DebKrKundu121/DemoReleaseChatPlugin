using DemoReleaseChatPlugin.Application.Releases.Emails;
using DemoReleaseChatPlugin.Application.Releases.Emails.Messages;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Retry;
using SendGrid;
using SendGrid.Helpers.Mail;

namespace DemoReleaseChatPlugin.Infrastructure.Emails;

/// <summary>
/// To swap SendGrid for SMTP, implement IReleaseNotificationSender against SmtpClient or MailKit and
/// change one line in AddReleaseNotifications. Nothing else in the module knows what sends the mail.
/// </summary>
public class SendGridReleaseNotificationSender(
    ISendGridClient client,
    ReleaseEmailSettings settings,
    ILogger<SendGridReleaseNotificationSender> logger)
    : IReleaseNotificationSender
{
    public async Task SendAsync(SendReleaseEmailMessage message, CancellationToken cancellationToken)
    {
        if (!settings.IsConfigured)
            throw new InvalidOperationException(
                "SendGrid is not configured, so the release notification could not be sent. Set " +
                "SendGrid:ApiKey and EmailSettings:FromEmail.");

        var email = new SendGridMessage
        {
            From = new EmailAddress(settings.FromEmail, settings.FromName),
            Subject = message.Subject,
            HtmlContent = message.BodyHtml
        };

        email.AddTo(new EmailAddress(message.RecipientEmail, message.RecipientName));

        var response = await GetRetryPolicy()
            .ExecuteAsync(async () => await client.SendEmailAsync(email, cancellationToken));

        if (response.IsSuccessStatusCode)
            return;

        var responseBody = await response.Body.ReadAsStringAsync(cancellationToken);

        logger.LogError(
            "SendGrid rejected the release notification to {Email}: {StatusCode} {Body}",
            message.RecipientEmail, (int)response.StatusCode, responseBody);

        throw new InvalidOperationException(
            $"SendGrid returned {(int)response.StatusCode} for {message.RecipientEmail}: {responseBody}");
    }

    /// <summary>
    /// Retries only on 5xx and 429. A 400 or 401 is a configuration problem that retrying cannot
    /// fix, so it throws straight through to CAP, which surfaces it on the dashboard.
    /// Backoff is 3^attempt seconds: 3s, 9s, 27s.
    ///
    /// This is the inner of two retry layers: Polly rides out a momentary 503 within one attempt;
    /// CAP's FailedRetryCount = 10 retries over minutes and survives a process restart.
    /// </summary>
    private static AsyncRetryPolicy<Response> GetRetryPolicy()
    {
        const int maxRetries = 3;
        const int waitSeconds = 3;

        return Policy
            .Handle<Exception>()
            .OrResult<Response>(response =>
            {
                var statusCode = (int)response.StatusCode;
                return statusCode is >= 500 and <= 599 or 429;
            })
            .WaitAndRetryAsync(maxRetries, attempt => TimeSpan.FromSeconds(Math.Pow(waitSeconds, attempt)));
    }
}
