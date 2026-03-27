using Microsoft.EntityFrameworkCore;

using NexTraceOne.Notifications.Application.Abstractions;
using NexTraceOne.Notifications.Domain.Entities;
using NexTraceOne.Notifications.Domain.Enums;
using NexTraceOne.Notifications.Domain.StronglyTypedIds;

namespace NexTraceOne.Notifications.Infrastructure.Persistence.Repositories;

internal sealed class NotificationTemplateRepository(NotificationsDbContext context)
    : INotificationTemplateStore
{
    public async Task AddAsync(NotificationTemplate template, CancellationToken cancellationToken)
        => await context.Templates.AddAsync(template, cancellationToken);

    public async Task<NotificationTemplate?> GetByIdAsync(
        NotificationTemplateId id,
        CancellationToken cancellationToken)
        => await context.Templates
            .SingleOrDefaultAsync(t => t.Id == id, cancellationToken);

    public async Task<IReadOnlyList<NotificationTemplate>> ListAsync(
        Guid tenantId,
        string? eventType,
        DeliveryChannel? channel,
        bool? isActive,
        CancellationToken cancellationToken)
    {
        var query = context.Templates.Where(t => t.TenantId == tenantId);

        if (eventType is not null)
            query = query.Where(t => t.EventType == eventType);

        if (channel.HasValue)
            query = query.Where(t => t.Channel == channel.Value || t.Channel == null);

        if (isActive.HasValue)
            query = query.Where(t => t.IsActive == isActive.Value);

        return await query
            .OrderBy(t => t.EventType)
            .ThenBy(t => t.Locale)
            .ToListAsync(cancellationToken);
    }

    public async Task<NotificationTemplate?> ResolveAsync(
        Guid tenantId,
        string eventType,
        DeliveryChannel channel,
        string locale,
        CancellationToken cancellationToken)
    {
        // Prioridade: canal específico + locale > canal específico + "en" > genérico (null) + locale > genérico + "en"
        var candidates = await context.Templates
            .Where(t => t.TenantId == tenantId
                     && t.EventType == eventType
                     && t.IsActive
                     && (t.Channel == channel || t.Channel == null)
                     && (t.Locale == locale || t.Locale == "en"))
            .ToListAsync(cancellationToken);

        return candidates
            .OrderByDescending(t => t.Channel == channel ? 1 : 0)
            .ThenByDescending(t => t.Locale == locale ? 1 : 0)
            .FirstOrDefault();
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
        => context.SaveChangesAsync(cancellationToken);
}
