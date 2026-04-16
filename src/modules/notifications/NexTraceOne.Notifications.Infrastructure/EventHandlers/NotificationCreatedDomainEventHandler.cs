using Microsoft.Extensions.Logging;

using NexTraceOne.BuildingBlocks.Infrastructure.EventBus.Abstractions;
using NexTraceOne.Notifications.Application.Abstractions;
using NexTraceOne.Notifications.Domain.Events;
using NexTraceOne.Notifications.Domain.StronglyTypedIds;

namespace NexTraceOne.Notifications.Infrastructure.EventHandlers;

/// <summary>
/// Handler para o domain event NotificationCreatedEvent.
/// Responsabilidade: disparar a entrega da notificação pelos canais externos elegíveis
/// (email, Microsoft Teams, etc.) via INotificationRoutingEngine + INotificationChannelDispatcher.
///
/// Fluxo de dispatch: Notification.Create() → RaiseDomainEvent(NotificationCreatedEvent) →
///   Outbox → NotificationsOutboxProcessorJob (pendente) →
///   IEventBus.PublishAsync{NotificationCreatedEvent} → este handler.
///
/// NOTA: O processador do outbox do módulo Notifications está pendente de implementação.
/// Quando implementado, este handler será invocado automaticamente após cada criação de notificação.
/// </summary>
internal sealed class NotificationCreatedDomainEventHandler(
    INotificationStore notificationStore,
    INotificationRoutingEngine routingEngine,
    IEnumerable<INotificationChannelDispatcher> dispatchers,
    ILogger<NotificationCreatedDomainEventHandler> logger)
    : IIntegrationEventHandler<NotificationCreatedEvent>
{
    public async Task HandleAsync(NotificationCreatedEvent @event, CancellationToken ct = default)
    {
        logger.LogInformation(
            "Dispatching notification {NotificationId} (category={Category}, severity={Severity}) " +
            "for recipient {RecipientUserId}.",
            @event.NotificationId, @event.Category, @event.Severity, @event.RecipientUserId);

        var notification = await notificationStore.GetByIdAsync(
            new NotificationId(@event.NotificationId), ct);

        if (notification is null)
        {
            logger.LogWarning(
                "Notification {NotificationId} not found during dispatch. " +
                "It may have been deleted before the event was processed.",
                @event.NotificationId);
            return;
        }

        var channels = await routingEngine.ResolveChannelsAsync(
            @event.RecipientUserId, @event.Category, @event.Severity, ct);

        foreach (var dispatcher in dispatchers.Where(d => channels.Contains(d.Channel)))
        {
            var dispatched = await dispatcher.DispatchAsync(notification, recipientAddress: null, ct);
            if (dispatched)
            {
                logger.LogInformation(
                    "Notification {NotificationId} dispatched via channel {Channel}.",
                    @event.NotificationId, dispatcher.ChannelName);
            }
            else
            {
                logger.LogWarning(
                    "Channel dispatcher {ChannelName} declined notification {NotificationId}.",
                    dispatcher.ChannelName, @event.NotificationId);
            }
        }
    }
}
