using System.Net;
using System.Net.Mail;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.Notifications.Application.Abstractions;
using NexTraceOne.Notifications.Application.ExternalDelivery;
using NexTraceOne.Notifications.Domain.Entities;
using NexTraceOne.Notifications.Domain.Enums;

namespace NexTraceOne.Notifications.Infrastructure.ExternalDelivery;

/// <summary>
/// Dispatcher de notificações por email via SMTP.
/// Utiliza System.Net.Mail.SmtpClient para envio real.
/// Gera HTML com template adequado via IExternalChannelTemplateResolver.
///
/// P7.2: A configuração SMTP é agora resolvida com prioridade para a entidade
/// SmtpConfiguration persistida em base de dados (P7.1). Se nenhuma configuração
/// persistida estiver disponível ou ativa, faz fallback para IOptions&lt;NotificationChannelOptions&gt;
/// (appsettings), garantindo compatibilidade backward.
/// </summary>
internal sealed class EmailNotificationDispatcher(
    IOptions<NotificationChannelOptions> channelOptions,
    ISmtpConfigurationStore smtpConfigStore,
    ICurrentTenant currentTenant,
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
        if (string.IsNullOrWhiteSpace(recipientAddress))
        {
            logger.LogWarning("No recipient email address provided for notification {NotificationId}. Skipping.", notification.Id.Value);
            return false;
        }

        // P7.2: Tentar obter configuração SMTP persistida (prioridade sobre appsettings)
        var (smtpHost, smtpPort, useSsl, fromAddress, fromName, username, password, baseUrl, isEnabled) =
            await ResolveSmtpSettingsAsync(cancellationToken);

        if (!isEnabled)
        {
            logger.LogDebug("Email channel is disabled. Skipping dispatch for notification {NotificationId}.", notification.Id.Value);
            return false;
        }

        if (string.IsNullOrWhiteSpace(smtpHost))
        {
            logger.LogWarning("SMTP host not configured. Cannot send email for notification {NotificationId}.", notification.Id.Value);
            return false;
        }

        if (string.IsNullOrWhiteSpace(fromAddress))
        {
            logger.LogWarning("From address not configured. Cannot send email for notification {NotificationId}.", notification.Id.Value);
            return false;
        }

        var emailTemplate = templateResolver.ResolveEmailTemplate(notification, baseUrl ?? string.Empty);

        using var message = new MailMessage
        {
            From = new MailAddress(fromAddress, fromName),
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
            using var client = new SmtpClient(smtpHost, smtpPort)
            {
                EnableSsl = useSsl,
                DeliveryMethod = SmtpDeliveryMethod.Network
            };

            if (!string.IsNullOrWhiteSpace(username))
            {
                client.Credentials = new NetworkCredential(username, password);
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

    /// <summary>
    /// Resolve as configurações SMTP com prioridade para configuração persistida.
    /// Faz fallback para appsettings se nenhuma configuração persistida estiver ativa.
    /// </summary>
    private async Task<(string Host, int Port, bool UseSsl, string FromAddress, string FromName,
        string? Username, string? Password, string? BaseUrl, bool IsEnabled)>
        ResolveSmtpSettingsAsync(CancellationToken cancellationToken)
    {
        try
        {
            var persistedConfig = await smtpConfigStore.GetByTenantAsync(currentTenant.Id, cancellationToken);
            if (persistedConfig is not null && persistedConfig.IsEnabled)
            {
                logger.LogDebug("Using persisted SMTP configuration for tenant {TenantId}.", currentTenant.Id);
                return (
                    persistedConfig.Host,
                    persistedConfig.Port,
                    persistedConfig.UseSsl,
                    persistedConfig.FromAddress,
                    persistedConfig.FromName,
                    persistedConfig.Username,
                    persistedConfig.EncryptedPassword, // EF Core decripta automaticamente via [EncryptedField]
                    persistedConfig.BaseUrl,
                    true
                );
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex,
                "Failed to load persisted SMTP configuration. Falling back to appsettings.");
        }

        // Fallback: appsettings via IOptions
        var emailSettings = channelOptions.Value.Email;
        return (
            emailSettings.SmtpHost ?? string.Empty,
            emailSettings.SmtpPort,
            emailSettings.UseSsl,
            emailSettings.FromAddress ?? string.Empty,
            emailSettings.FromName,
            emailSettings.Username,
            emailSettings.Password,
            emailSettings.BaseUrl,
            emailSettings.Enabled
        );
    }
}
