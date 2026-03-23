using Microsoft.EntityFrameworkCore;

using NexTraceOne.Notifications.Application.Abstractions;
using NexTraceOne.Notifications.Domain.Entities;
using NexTraceOne.Notifications.Domain.Enums;

namespace NexTraceOne.Notifications.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repositório EF Core para persistência de preferências de notificação.
/// </summary>
internal sealed class NotificationPreferenceStoreRepository(
    NotificationsDbContext context) : INotificationPreferenceStore
{
    public async Task<IReadOnlyList<NotificationPreference>> GetByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken)
        => await context.Preferences
            .Where(p => p.UserId == userId)
            .ToListAsync(cancellationToken);

    public async Task<NotificationPreference?> GetAsync(
        Guid userId,
        NotificationCategory category,
        DeliveryChannel channel,
        CancellationToken cancellationToken)
        => await context.Preferences
            .SingleOrDefaultAsync(
                p => p.UserId == userId && p.Category == category && p.Channel == channel,
                cancellationToken);

    public async Task AddAsync(
        NotificationPreference preference,
        CancellationToken cancellationToken)
        => await context.Preferences.AddAsync(preference, cancellationToken);

    public Task SaveChangesAsync(CancellationToken cancellationToken)
        => context.SaveChangesAsync(cancellationToken);
}
