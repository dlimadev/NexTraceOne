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

    /// <summary>Lista deliveries pendentes elegíveis para retry (status = Pending, retryCount abaixo do máximo).</summary>
    Task<IReadOnlyList<NotificationDelivery>> ListPendingForRetryAsync(
        int maxRetryCount,
        int batchSize = 50,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Lista deliveries com status RetryScheduled cujo NextRetryAt já foi atingido.
    /// Utilizado pelo NotificationDeliveryRetryJob para reprocessamento.
    /// </summary>
    Task<IReadOnlyList<NotificationDelivery>> ListScheduledForRetryAsync(
        DateTimeOffset now,
        int maxRetryCount,
        int batchSize = 50,
        CancellationToken cancellationToken = default);

    /// <summary>Lista deliveries associadas a uma notificação.</summary>
    Task<IReadOnlyList<NotificationDelivery>> ListByNotificationIdAsync(
        NotificationId notificationId,
        CancellationToken cancellationToken = default);

    /// <summary>Lista deliveries de um tenant com filtros opcionais de status e canal.</summary>
    Task<IReadOnlyList<NotificationDelivery>> ListByTenantAsync(
        Guid tenantId,
        DeliveryStatus? status = null,
        DeliveryChannel? channel = null,
        int skip = 0,
        int take = 50,
        CancellationToken cancellationToken = default);

    /// <summary>Persiste alterações realizadas.</summary>
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
