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
    public async Task<IReadOnlyList<NotificationDelivery>> ListScheduledForRetryAsync(
        DateTimeOffset now,
        int maxRetryCount,
        int batchSize = 50,
        CancellationToken cancellationToken = default)
    {
        return await context.Deliveries
            .Where(d => d.Status == DeliveryStatus.RetryScheduled
                     && d.NextRetryAt != null
                     && d.NextRetryAt <= now
                     && d.RetryCount < maxRetryCount)
            .OrderBy(d => d.NextRetryAt)
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
    public async Task<IReadOnlyList<NotificationDelivery>> ListByTenantAsync(
        Guid tenantId,
        DeliveryStatus? status = null,
        DeliveryChannel? channel = null,
        int skip = 0,
        int take = 50,
        CancellationToken cancellationToken = default)
    {
        var query = context.Deliveries
            .Join(context.Notifications,
                d => d.NotificationId,
                n => n.Id,
                (d, n) => new { Delivery = d, Notification = n })
            .Where(x => x.Notification.TenantId == tenantId)
            .Select(x => x.Delivery);

        if (status.HasValue)
            query = query.Where(d => d.Status == status.Value);

        if (channel.HasValue)
            query = query.Where(d => d.Channel == channel.Value);

        return await query
            .OrderByDescending(d => d.CreatedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        context.SaveChangesAsync(cancellationToken);
}
