using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using NexTraceOne.Notifications.Application.Abstractions;
using NexTraceOne.Notifications.Application.ExternalDelivery;
using NexTraceOne.Notifications.Domain.Entities;
using NexTraceOne.Notifications.Domain.Enums;

namespace NexTraceOne.Notifications.Infrastructure.ExternalDelivery;

/// <summary>
/// Serviço central de entrega externa de notificações.
/// Coordena o roteamento (quais canais), dispatch (enviar) e delivery log (rastrear).
/// Implementa retry básico com controlo de tentativas.
/// </summary>
internal sealed class ExternalDeliveryService(
    INotificationRoutingEngine routingEngine,
    IEnumerable<INotificationChannelDispatcher> dispatchers,
    INotificationDeliveryStore deliveryStore,
    IOptions<DeliveryRetryOptions> retryOptions,
    ILogger<ExternalDeliveryService> logger) : IExternalDeliveryService
{
    private readonly Dictionary<DeliveryChannel, INotificationChannelDispatcher> _dispatcherMap =
        dispatchers.ToDictionary(d => d.Channel, d => d);

    /// <inheritdoc/>
    public async Task ProcessExternalDeliveryAsync(
        Notification notification,
        CancellationToken cancellationToken = default)
    {
        // 1. Resolver canais elegíveis
        var channels = await routingEngine.ResolveChannelsAsync(
            notification.RecipientUserId,
            notification.Category,
            notification.Severity,
            cancellationToken);

        // 2. Filtrar apenas canais externos (InApp é tratado internamente pelo orchestrator)
        var externalChannels = channels
            .Where(c => c is not DeliveryChannel.InApp)
            .ToList();

        if (externalChannels.Count == 0)
        {
            logger.LogDebug(
                "No external channels eligible for notification {NotificationId} (severity {Severity})",
                notification.Id.Value, notification.Severity);
            return;
        }

        // 3. Despachar para cada canal externo
        foreach (var channel in externalChannels)
        {
            await DispatchToChannelAsync(notification, channel, cancellationToken);
        }
    }

    private async Task DispatchToChannelAsync(
        Notification notification,
        DeliveryChannel channel,
        CancellationToken cancellationToken)
    {
        if (!_dispatcherMap.TryGetValue(channel, out var dispatcher))
        {
            logger.LogWarning(
                "No dispatcher registered for channel {Channel}. Skipping delivery for notification {NotificationId}.",
                channel, notification.Id.Value);
            return;
        }

        // Criar registo de delivery
        var delivery = NotificationDelivery.Create(
            notification.Id,
            channel,
            recipientAddress: null);  // Address pode ser resolvido pelo dispatcher

        await deliveryStore.AddAsync(delivery, cancellationToken);

        var maxAttempts = retryOptions.Value.MaxAttempts;
        var baseDelay = retryOptions.Value.BaseDelaySeconds;

        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                delivery.IncrementRetry();

                var success = await dispatcher.DispatchAsync(notification, null, cancellationToken);
                if (success)
                {
                    delivery.MarkDelivered();
                    logger.LogInformation(
                        "External delivery succeeded: channel={Channel}, notification={NotificationId}, attempt={Attempt}",
                        channel, notification.Id.Value, attempt);
                    break;
                }

                // Dispatcher returned false (e.g., channel disabled, no recipient)
                delivery.MarkSkipped();
                logger.LogDebug(
                    "External delivery skipped by dispatcher: channel={Channel}, notification={NotificationId}",
                    channel, notification.Id.Value);
                break;
            }
            catch (Exception ex)
            {
                logger.LogWarning(
                    ex,
                    "External delivery attempt {Attempt}/{MaxAttempts} failed: channel={Channel}, notification={NotificationId}",
                    attempt, maxAttempts, channel, notification.Id.Value);

                if (attempt >= maxAttempts)
                {
                    delivery.MarkFailed(ex.Message);
                    logger.LogError(
                        "External delivery permanently failed after {MaxAttempts} attempts: channel={Channel}, notification={NotificationId}",
                        maxAttempts, channel, notification.Id.Value);
                }
                else
                {
                    // Backoff linear simples: baseDelay * attempt
                    var delay = TimeSpan.FromSeconds(baseDelay * attempt);
                    logger.LogDebug(
                        "Waiting {Delay}s before retry {Next}/{Max} for channel={Channel}, notification={NotificationId}",
                        delay.TotalSeconds, attempt + 1, maxAttempts, channel, notification.Id.Value);
                    await Task.Delay(delay, cancellationToken);
                }
            }
        }

        await deliveryStore.SaveChangesAsync(cancellationToken);
    }
}
