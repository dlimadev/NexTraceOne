using System.Text;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using NexTraceOne.Notifications.Application.Abstractions;
using NexTraceOne.Notifications.Application.ExternalDelivery;
using NexTraceOne.Notifications.Domain.Entities;
using NexTraceOne.Notifications.Domain.Enums;

namespace NexTraceOne.Notifications.Infrastructure.ExternalDelivery;

/// <summary>
/// Dispatcher de notificações para Slack via Incoming Webhook.
/// Envia mensagens estruturadas em Block Kit com contexto da notificação.
/// </summary>
internal sealed class SlackNotificationDispatcher(
    IHttpClientFactory httpClientFactory,
    IOptions<NotificationChannelOptions> channelOptions,
    IExternalChannelTemplateResolver templateResolver,
    ILogger<SlackNotificationDispatcher> logger) : INotificationChannelDispatcher
{
    /// <inheritdoc/>
    public string ChannelName => "Slack";

    /// <inheritdoc/>
    public DeliveryChannel Channel => DeliveryChannel.Slack;

    /// <inheritdoc/>
    public async Task<bool> DispatchAsync(
        Notification notification,
        string? recipientAddress,
        CancellationToken cancellationToken = default)
    {
        var slackSettings = channelOptions.Value.Slack;

        if (!slackSettings.Enabled)
        {
            logger.LogDebug("Slack channel is disabled. Skipping dispatch for notification {NotificationId}.", notification.Id.Value);
            return false;
        }

        // recipientAddress pode sobrepor o webhook URL configurado (para webhooks por equipa/canal)
        var webhookUrl = !string.IsNullOrWhiteSpace(recipientAddress)
            ? recipientAddress
            : slackSettings.WebhookUrl;

        if (string.IsNullOrWhiteSpace(webhookUrl))
        {
            logger.LogWarning("Slack webhook URL not configured and no recipient address provided for notification {NotificationId}. Skipping.", notification.Id.Value);
            return false;
        }

        var slackPayload = templateResolver.ResolveSlackTemplate(notification, slackSettings.BaseUrl);

        try
        {
            using var client = httpClientFactory.CreateClient("NexTraceOneSlack");
            client.Timeout = TimeSpan.FromSeconds(slackSettings.TimeoutSeconds);

            var content = new StringContent(
                slackPayload.JsonPayload,
                Encoding.UTF8,
                "application/json");

            var response = await client.PostAsync(webhookUrl, content, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                logger.LogInformation(
                    "Slack message sent successfully for notification {NotificationId}",
                    notification.Id.Value);
                return true;
            }

            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
            logger.LogError(
                "Slack webhook returned {StatusCode} for notification {NotificationId}: {Response}",
                response.StatusCode, notification.Id.Value, responseBody);
            throw new HttpRequestException(
                $"Slack webhook returned {response.StatusCode}: {responseBody}");
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(
                ex,
                "Failed to send Slack message for notification {NotificationId}: {Error}",
                notification.Id.Value, ex.Message);
            throw;
        }
        catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            logger.LogError(
                ex,
                "Slack webhook timed out for notification {NotificationId}",
                notification.Id.Value);
            throw;
        }
    }
}
