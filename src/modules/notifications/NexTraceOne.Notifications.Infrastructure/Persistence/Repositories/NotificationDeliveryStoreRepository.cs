using Microsoft.EntityFrameworkCore;

using NexTraceOne.Notifications.Application.ExternalDelivery;
using NexTraceOne.Notifications.Domain.Entities;
using NexTraceOne.Notifications.Domain.Enums;
using NexTraceOne.Notifications.Domain.StronglyTypedIds;

namespace NexTraceOne.Notifications.Infrastructure.Persistence.Repositories;

/// <summary>
/// Implementação de persistência para delivery logs de notificações externas.
/// </summary>
internal sealed class NotificationDeliveryStoreRepository(
    NotificationsDbContext context) : INotificationDeliveryStore
{
    /// <inheritdoc/>
    public async Task AddAsync(NotificationDelivery delivery, CancellationToken cancellationToken = default)
    {
        await context.Deliveries.AddAsync(delivery, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<NotificationDelivery?> GetByIdAsync(
        NotificationDeliveryId id,
        CancellationToken cancellationToken = default)
    {
        return await context.Deliveries
            .FirstOrDefaultAsync(d => d.Id == id, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<NotificationDelivery>> ListPendingForRetryAsync(
        int maxRetryCount,
        int batchSize = 50,
        CancellationToken cancellationToken = default)
    {
        return await context.Deliveries
            .Where(d => d.Status == DeliveryStatus.Pending && d.RetryCount < maxRetryCount)
            .OrderBy(d => d.CreatedAt)
            .Take(batchSize)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<NotificationDelivery>> ListByNotificationIdAsync(
        NotificationId notificationId,
        CancellationToken cancellationToken = default)
    {
        return await context.Deliveries
            .Where(d => d.NotificationId == notificationId)
            .OrderBy(d => d.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        context.SaveChangesAsync(cancellationToken);
}
