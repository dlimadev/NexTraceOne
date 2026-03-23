using NexTraceOne.BuildingBlocks.Core.Events;
using NexTraceOne.Notifications.Domain.Enums;

namespace NexTraceOne.Notifications.Domain.Events;

/// <summary>
/// Evento de domínio emitido quando uma entrega de notificação é concluída (sucesso ou falha).
/// </summary>
public sealed record NotificationDeliveryCompletedEvent(
    Guid NotificationId,
    Guid DeliveryId,
    DeliveryChannel Channel,
    DeliveryStatus Status,
    DateTimeOffset CompletedAt) : DomainEventBase;
