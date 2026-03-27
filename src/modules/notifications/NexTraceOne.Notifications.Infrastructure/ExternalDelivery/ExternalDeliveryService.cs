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
/// Implementa retry deferido: em vez de bloquear com Task.Delay, agenda retries via
/// NotificationDelivery.ScheduleRetry(nextRetryAt), que são processados pelo
/// NotificationDeliveryRetryJob em background.
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

    /// <inheritdoc/>
    public async Task RetryDeliveryAsync(
        NotificationDelivery delivery,
        Notification notification,
        CancellationToken cancellationToken = default)
    {
        if (!_dispatcherMap.TryGetValue(delivery.Channel, out var dispatcher))
        {
            logger.LogWarning(
                "No dispatcher for channel {Channel}. Marking delivery {DeliveryId} as failed.",
                delivery.Channel, delivery.Id.Value);
            delivery.MarkFailed("No dispatcher registered for this channel.");
            await deliveryStore.SaveChangesAsync(cancellationToken);
            return;
        }

        await AttemptDispatchAsync(delivery, notification, dispatcher, cancellationToken);
        await deliveryStore.SaveChangesAsync(cancellationToken);
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
            recipientAddress: null);  // Address resolvido pelo dispatcher

        await deliveryStore.AddAsync(delivery, cancellationToken);

        await AttemptDispatchAsync(delivery, notification, dispatcher, cancellationToken);

        await deliveryStore.SaveChangesAsync(cancellationToken);
    }

    private async Task AttemptDispatchAsync(
        NotificationDelivery delivery,
        Notification notification,
        INotificationChannelDispatcher dispatcher,
        CancellationToken cancellationToken)
    {
        var opts = retryOptions.Value;

        delivery.IncrementRetry();

        try
        {
            var success = await dispatcher.DispatchAsync(notification, delivery.RecipientAddress, cancellationToken);
            if (success)
            {
                delivery.MarkDelivered();
                logger.LogInformation(
                    "External delivery succeeded: channel={Channel}, notification={NotificationId}, attempt={Attempt}",
                    delivery.Channel, notification.Id.Value, delivery.RetryCount);
            }
            else
            {
                // Dispatcher returned false (channel disabled, no recipient, etc.) — not a transient error
                delivery.MarkSkipped();
                logger.LogDebug(
                    "External delivery skipped by dispatcher: channel={Channel}, notification={NotificationId}",
                    delivery.Channel, notification.Id.Value);
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(
                ex,
                "External delivery attempt {Attempt}/{Max} failed: channel={Channel}, notification={NotificationId}",
                delivery.RetryCount, opts.MaxAttempts, delivery.Channel, notification.Id.Value);

            if (delivery.RetryCount >= opts.MaxAttempts)
            {
                // All attempts exhausted — mark permanently failed
                delivery.MarkFailed(ex.Message);
                logger.LogError(
                    "External delivery permanently failed after {MaxAttempts} attempts: channel={Channel}, notification={NotificationId}",
                    opts.MaxAttempts, delivery.Channel, notification.Id.Value);
            }
            else
            {
                // Schedule next attempt with linear backoff: baseDelay * retryCount seconds
                var delay = TimeSpan.FromSeconds(opts.BaseDelaySeconds * delivery.RetryCount);
                var nextRetryAt = DateTimeOffset.UtcNow.Add(delay);
                delivery.ScheduleRetry(nextRetryAt, ex.Message);

                logger.LogDebug(
                    "Delivery retry scheduled: channel={Channel}, notification={NotificationId}, nextRetryAt={NextRetryAt}",
                    delivery.Channel, notification.Id.Value, nextRetryAt);
            }
        }
    }
}
