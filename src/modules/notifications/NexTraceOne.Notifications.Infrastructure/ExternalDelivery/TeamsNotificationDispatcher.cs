using System.Net.Http.Headers;
using System.Text;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using NexTraceOne.Notifications.Application.Abstractions;
using NexTraceOne.Notifications.Application.ExternalDelivery;
using NexTraceOne.Notifications.Domain.Entities;
using NexTraceOne.Notifications.Domain.Enums;

namespace NexTraceOne.Notifications.Infrastructure.ExternalDelivery;

/// <summary>
/// Dispatcher de notificações para Microsoft Teams via Incoming Webhook.
/// Envia Adaptive Cards estruturadas com contexto da notificação.
/// </summary>
internal sealed class TeamsNotificationDispatcher(
    IHttpClientFactory httpClientFactory,
    IOptions<NotificationChannelOptions> channelOptions,
    IExternalChannelTemplateResolver templateResolver,
    ILogger<TeamsNotificationDispatcher> logger) : INotificationChannelDispatcher
{
    /// <inheritdoc/>
    public string ChannelName => "MicrosoftTeams";

    /// <inheritdoc/>
    public DeliveryChannel Channel => DeliveryChannel.MicrosoftTeams;

    /// <inheritdoc/>
    public async Task<bool> DispatchAsync(
        Notification notification,
        string? recipientAddress,
        CancellationToken cancellationToken = default)
    {
        var teamsSettings = channelOptions.Value.Teams;

        if (!teamsSettings.Enabled)
        {
            logger.LogDebug("Teams channel is disabled. Skipping dispatch for notification {NotificationId}.", notification.Id.Value);
            return false;
        }

        // recipientAddress pode sobrepor o webhook URL configurado (para webhooks por equipa/canal)
        var webhookUrl = !string.IsNullOrWhiteSpace(recipientAddress)
            ? recipientAddress
            : teamsSettings.WebhookUrl;

        if (string.IsNullOrWhiteSpace(webhookUrl))
        {
            logger.LogWarning("Teams webhook URL not configured and no recipient address provided for notification {NotificationId}. Skipping.", notification.Id.Value);
            return false;
        }

        var teamsPayload = templateResolver.ResolveTeamsTemplate(notification, teamsSettings.BaseUrl);

        try
        {
            using var client = httpClientFactory.CreateClient("NexTraceOneTeams");
            client.Timeout = TimeSpan.FromSeconds(teamsSettings.TimeoutSeconds);

            var content = new StringContent(
                teamsPayload.JsonPayload,
                Encoding.UTF8,
                "application/json");

            var response = await client.PostAsync(webhookUrl, content, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                logger.LogInformation(
                    "Teams message sent successfully for notification {NotificationId}",
                    notification.Id.Value);
                return true;
            }

            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
            logger.LogError(
                "Teams webhook returned {StatusCode} for notification {NotificationId}: {Response}",
                response.StatusCode, notification.Id.Value, responseBody);
            throw new HttpRequestException(
                $"Teams webhook returned {response.StatusCode}: {responseBody}");
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(
                ex,
                "Failed to send Teams message for notification {NotificationId}: {Error}",
                notification.Id.Value, ex.Message);
            throw;
        }
        catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            logger.LogError(
                ex,
                "Teams webhook timed out for notification {NotificationId}",
                notification.Id.Value);
            throw;
        }
    }
}
