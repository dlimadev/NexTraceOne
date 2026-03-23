using NexTraceOne.Notifications.Domain.Entities;
using NexTraceOne.Notifications.Domain.Enums;
using NexTraceOne.Notifications.Domain.StronglyTypedIds;

namespace NexTraceOne.Notifications.Application.ExternalDelivery;

/// <summary>
/// Abstração para persistência e consulta de delivery logs de notificações externas.
/// </summary>
public interface INotificationDeliveryStore
{
    /// <summary>Persiste um novo registo de delivery.</summary>
    Task AddAsync(NotificationDelivery delivery, CancellationToken cancellationToken = default);

    /// <summary>Obtém um delivery por Id.</summary>
    Task<NotificationDelivery?> GetByIdAsync(NotificationDeliveryId id, CancellationToken cancellationToken = default);

    /// <summary>Lista deliveries pendentes elegíveis para retry.</summary>
    Task<IReadOnlyList<NotificationDelivery>> ListPendingForRetryAsync(
        int maxRetryCount,
        int batchSize = 50,
        CancellationToken cancellationToken = default);

    /// <summary>Lista deliveries associadas a uma notificação.</summary>
    Task<IReadOnlyList<NotificationDelivery>> ListByNotificationIdAsync(
        NotificationId notificationId,
        CancellationToken cancellationToken = default);

    /// <summary>Persiste alterações realizadas.</summary>
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
