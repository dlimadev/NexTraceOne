using System.Net;
using System.Net.Mail;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using NexTraceOne.Notifications.Application.Abstractions;
using NexTraceOne.Notifications.Application.ExternalDelivery;
using NexTraceOne.Notifications.Domain.Entities;
using NexTraceOne.Notifications.Domain.Enums;

namespace NexTraceOne.Notifications.Infrastructure.ExternalDelivery;

/// <summary>
/// Dispatcher de notificações por email via SMTP.
/// Utiliza System.Net.Mail.SmtpClient para envio real.
/// Gera HTML com template adequado via IExternalChannelTemplateResolver.
/// </summary>
internal sealed class EmailNotificationDispatcher(
    IOptions<NotificationChannelOptions> channelOptions,
    IExternalChannelTemplateResolver templateResolver,
    ILogger<EmailNotificationDispatcher> logger) : INotificationChannelDispatcher
{
    /// <inheritdoc/>
    public string ChannelName => "Email";

    /// <inheritdoc/>
    public DeliveryChannel Channel => DeliveryChannel.Email;

    /// <inheritdoc/>
    public async Task<bool> DispatchAsync(
        Notification notification,
        string? recipientAddress,
        CancellationToken cancellationToken = default)
    {
        var emailSettings = channelOptions.Value.Email;

        if (!emailSettings.Enabled)
        {
            logger.LogDebug("Email channel is disabled. Skipping dispatch for notification {NotificationId}.", notification.Id.Value);
            return false;
        }

        if (string.IsNullOrWhiteSpace(recipientAddress))
        {
            logger.LogWarning("No recipient email address provided for notification {NotificationId}. Skipping.", notification.Id.Value);
            return false;
        }

        if (string.IsNullOrWhiteSpace(emailSettings.SmtpHost))
        {
            logger.LogWarning("SMTP host not configured. Cannot send email for notification {NotificationId}.", notification.Id.Value);
            return false;
        }

        if (string.IsNullOrWhiteSpace(emailSettings.FromAddress))
        {
            logger.LogWarning("From address not configured. Cannot send email for notification {NotificationId}.", notification.Id.Value);
            return false;
        }

        var emailTemplate = templateResolver.ResolveEmailTemplate(notification, emailSettings.BaseUrl);

        using var message = new MailMessage
        {
            From = new MailAddress(emailSettings.FromAddress, emailSettings.FromName),
            Subject = emailTemplate.Subject,
            Body = emailTemplate.HtmlBody,
            IsBodyHtml = true
        };
        message.To.Add(new MailAddress(recipientAddress));

        // Adicionar plain text como vista alternativa
        if (!string.IsNullOrWhiteSpace(emailTemplate.PlainTextBody))
        {
            var plainView = AlternateView.CreateAlternateViewFromString(
                emailTemplate.PlainTextBody, null, "text/plain");
            message.AlternateViews.Add(plainView);
        }

        try
        {
            using var client = new SmtpClient(emailSettings.SmtpHost, emailSettings.SmtpPort)
            {
                EnableSsl = emailSettings.UseSsl,
                DeliveryMethod = SmtpDeliveryMethod.Network
            };

            if (!string.IsNullOrWhiteSpace(emailSettings.Username))
            {
                client.Credentials = new NetworkCredential(emailSettings.Username, emailSettings.Password);
            }

#pragma warning disable CA2016 // SmtpClient.SendMailAsync does not accept CancellationToken in all overloads
            await client.SendMailAsync(message);
#pragma warning restore CA2016

            logger.LogInformation(
                "Email sent successfully for notification {NotificationId} to {Recipient}",
                notification.Id.Value, recipientAddress);
            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Failed to send email for notification {NotificationId} to {Recipient}: {Error}",
                notification.Id.Value, recipientAddress, ex.Message);
            throw;
        }
    }
}
