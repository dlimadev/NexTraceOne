using System.Text;
using System.Text.Json;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using NexTraceOne.Notifications.Application.Abstractions;
using NexTraceOne.Notifications.Application.ExternalDelivery;
using NexTraceOne.Notifications.Domain.Entities;
using NexTraceOne.Notifications.Domain.Enums;

namespace NexTraceOne.Notifications.Infrastructure.ExternalDelivery;

/// <summary>
/// Dispatcher de notificações para webhook HTTP genérico.
/// Envia o payload JSON da notificação para o endpoint do destinatário
/// (ou para o DefaultUrl configurado), permitindo integração com qualquer sistema.
/// </summary>
internal sealed class WebhookNotificationDispatcher(
    IHttpClientFactory httpClientFactory,
    IOptions<NotificationChannelOptions> channelOptions,
    ILogger<WebhookNotificationDispatcher> logger) : INotificationChannelDispatcher
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    /// <inheritdoc/>
    public string ChannelName => "Webhook";

    /// <inheritdoc/>
    public DeliveryChannel Channel => DeliveryChannel.Webhook;

    /// <inheritdoc/>
    public async Task<bool> DispatchAsync(
        Notification notification,
        string? recipientAddress,
        CancellationToken cancellationToken = default)
    {
        var webhookSettings = channelOptions.Value.Webhook;

        if (!webhookSettings.Enabled)
        {
            logger.LogDebug("Webhook channel is disabled. Skipping dispatch for notification {NotificationId}.", notification.Id.Value);
            return false;
        }

        var webhookUrl = !string.IsNullOrWhiteSpace(recipientAddress)
            ? recipientAddress
            : webhookSettings.DefaultUrl;

        if (string.IsNullOrWhiteSpace(webhookUrl))
        {
            logger.LogWarning("Webhook URL not configured and no recipient address provided for notification {NotificationId}. Skipping.", notification.Id.Value);
            return false;
        }

        var payload = JsonSerializer.Serialize(new
        {
            notificationId = notification.Id.Value,
            eventType = notification.EventType,
            category = notification.Category.ToString(),
            severity = notification.Severity.ToString(),
            title = notification.Title,
            message = notification.Message,
            sourceModule = notification.SourceModule,
            sourceEntityType = notification.SourceEntityType,
            sourceEntityId = notification.SourceEntityId,
            actionUrl = notification.ActionUrl,
            requiresAction = notification.RequiresAction,
            createdAt = notification.CreatedAt
        }, JsonOptions);

        try
        {
            using var client = httpClientFactory.CreateClient("NexTraceOneWebhook");
            client.Timeout = TimeSpan.FromSeconds(webhookSettings.TimeoutSeconds);

            using var request = new HttpRequestMessage(HttpMethod.Post, webhookUrl)
            {
                Content = new StringContent(payload, Encoding.UTF8, "application/json")
            };

            if (!string.IsNullOrWhiteSpace(webhookSettings.AuthorizationHeader))
                request.Headers.TryAddWithoutValidation("Authorization", webhookSettings.AuthorizationHeader);

            var response = await client.SendAsync(request, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                logger.LogInformation(
                    "Webhook delivered successfully for notification {NotificationId}",
                    notification.Id.Value);
                return true;
            }

            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
            logger.LogError(
                "Webhook returned {StatusCode} for notification {NotificationId}: {Response}",
                response.StatusCode, notification.Id.Value, responseBody);
            throw new HttpRequestException(
                $"Webhook returned {response.StatusCode}: {responseBody}");
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(
                ex,
                "Failed to deliver webhook for notification {NotificationId}: {Error}",
                notification.Id.Value, ex.Message);
            throw;
        }
        catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            logger.LogError(
                ex,
                "Webhook timed out for notification {NotificationId}",
                notification.Id.Value);
            throw;
        }
    }
}
