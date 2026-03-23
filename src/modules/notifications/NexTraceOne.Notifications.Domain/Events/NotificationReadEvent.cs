using NexTraceOne.BuildingBlocks.Core.Events;
using NexTraceOne.Notifications.Domain.Enums;

namespace NexTraceOne.Notifications.Domain.Events;

/// <summary>
/// Evento de domínio emitido quando uma notificação é marcada como lida.
/// </summary>
public sealed record NotificationReadEvent(
    Guid NotificationId,
    Guid RecipientUserId,
    DateTimeOffset ReadAt) : DomainEventBase;
