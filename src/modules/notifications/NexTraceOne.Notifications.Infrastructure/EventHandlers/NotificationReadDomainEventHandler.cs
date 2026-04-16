using Microsoft.Extensions.Logging;

using NexTraceOne.BuildingBlocks.Infrastructure.EventBus.Abstractions;
using NexTraceOne.Notifications.Domain.Events;

namespace NexTraceOne.Notifications.Infrastructure.EventHandlers;

/// <summary>
/// Handler para o domain event NotificationReadEvent.
/// Responsabilidade: registar em log que a notificação foi lida,
/// permitindo métricas de engagement e auditoria de leitura.
///
/// Fluxo de dispatch: Notification.MarkAsRead() → RaiseDomainEvent(NotificationReadEvent) →
///   Outbox → NotificationsOutboxProcessorJob (pendente) →
///   IEventBus.PublishAsync{NotificationReadEvent} → este handler.
///
/// NOTA: O processador do outbox do módulo Notifications está pendente de implementação.
/// Quando implementado, este handler será invocado automaticamente após cada leitura.
/// </summary>
internal sealed class NotificationReadDomainEventHandler(
    ILogger<NotificationReadDomainEventHandler> logger)
    : IIntegrationEventHandler<NotificationReadEvent>
{
    public Task HandleAsync(NotificationReadEvent @event, CancellationToken ct = default)
    {
        logger.LogInformation(
            "Notification {NotificationId} marked as read by user {RecipientUserId} at {ReadAt}.",
            @event.NotificationId, @event.RecipientUserId, @event.ReadAt);

        return Task.CompletedTask;
    }
}
