using Microsoft.EntityFrameworkCore;

using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.Notifications.Application.Abstractions;
using NexTraceOne.Notifications.Domain.Entities;
using NexTraceOne.Notifications.Domain.Enums;
using NexTraceOne.Notifications.Domain.StronglyTypedIds;

namespace NexTraceOne.Notifications.Infrastructure.Persistence.Repositories;

internal sealed class NotificationStoreRepository(
    NotificationsDbContext context,
    IDateTimeProvider clock) : INotificationStore
{
    public async Task AddAsync(Notification notification, CancellationToken cancellationToken)
        => await context.Notifications.AddAsync(notification, cancellationToken);

    public async Task<Notification?> GetByIdAsync(NotificationId id, CancellationToken cancellationToken)
        => await context.Notifications
            .SingleOrDefaultAsync(n => n.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Notification>> ListAsync(
        Guid recipientUserId,
        NotificationStatus? status,
        NotificationCategory? category,
        NotificationSeverity? minSeverity,
        int skip,
        int take,
        CancellationToken cancellationToken)
    {
        var query = context.Notifications
            .Where(n => n.RecipientUserId == recipientUserId);

        if (status.HasValue)
            query = query.Where(n => n.Status == status.Value);

        if (category.HasValue)
            query = query.Where(n => n.Category == category.Value);

        if (minSeverity.HasValue)
            query = query.Where(n => n.Severity >= minSeverity.Value);

        return await query
            .OrderByDescending(n => n.CreatedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> CountUnreadAsync(Guid recipientUserId, CancellationToken cancellationToken)
        => await context.Notifications
            .CountAsync(n => n.RecipientUserId == recipientUserId
                         && n.Status == NotificationStatus.Unread, cancellationToken);

    public async Task MarkAllAsReadAsync(Guid recipientUserId, CancellationToken cancellationToken)
    {
        var now = clock.UtcNow;
        await context.Notifications
            .Where(n => n.RecipientUserId == recipientUserId
                     && n.Status == NotificationStatus.Unread)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(n => n.Status, NotificationStatus.Read)
                    .SetProperty(n => n.ReadAt, now),
                cancellationToken);
    }

    public async Task<IReadOnlyList<Notification>> ListForEscalationAsync(
        Guid tenantId,
        DateTimeOffset olderThan,
        IReadOnlyList<NotificationSeverity> severities,
        int skip,
        int take,
        CancellationToken cancellationToken)
    {
        var query = context.Notifications
            .Where(n => !n.IsEscalated
                     && severities.Contains(n.Severity)
                     && (n.Status == NotificationStatus.Unread || n.Status == NotificationStatus.Read)
                     && n.CreatedAt < olderThan
                     && (n.SnoozedUntil == null || n.SnoozedUntil <= DateTimeOffset.UtcNow));

        if (tenantId != Guid.Empty)
            query = query.Where(n => n.TenantId == tenantId);

        return await query
            .OrderBy(n => n.CreatedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Notification>> ListForDigestAsync(
        Guid tenantId,
        DateTimeOffset since,
        int skip,
        int take,
        CancellationToken cancellationToken)
    {
        var query = context.Notifications
            .Where(n => n.CreatedAt >= since
                     && (n.Status == NotificationStatus.Unread || n.Status == NotificationStatus.Read)
                     && (n.Severity == NotificationSeverity.Info || n.Severity == NotificationSeverity.ActionRequired));

        if (tenantId != Guid.Empty)
            query = query.Where(n => n.TenantId == tenantId);

        return await query
            .OrderByDescending(n => n.CreatedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
        => context.SaveChangesAsync(cancellationToken);
}
