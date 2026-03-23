using NexTraceOne.BuildingBlocks.Core.Events;
using NexTraceOne.Notifications.Domain.Enums;

namespace NexTraceOne.Notifications.Domain.Events;

/// <summary>
/// Evento de domínio emitido quando uma notificação é criada na central interna.
/// Consumidores típicos: dispatchers de canais externos, audit trail.
/// </summary>
public sealed record NotificationCreatedEvent(
    Guid NotificationId,
    Guid RecipientUserId,
    NotificationCategory Category,
    NotificationSeverity Severity,
    string SourceModule,
    bool RequiresAction) : DomainEventBase;
