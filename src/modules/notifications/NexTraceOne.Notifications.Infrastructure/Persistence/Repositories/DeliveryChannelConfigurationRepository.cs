using Microsoft.EntityFrameworkCore;

using NexTraceOne.Notifications.Application.Abstractions;
using NexTraceOne.Notifications.Domain.Entities;
using NexTraceOne.Notifications.Domain.Enums;
using NexTraceOne.Notifications.Domain.StronglyTypedIds;

namespace NexTraceOne.Notifications.Infrastructure.Persistence.Repositories;

internal sealed class DeliveryChannelConfigurationRepository(NotificationsDbContext context)
    : IDeliveryChannelConfigurationStore
{
    public async Task AddAsync(DeliveryChannelConfiguration config, CancellationToken cancellationToken)
        => await context.ChannelConfigurations.AddAsync(config, cancellationToken);

    public async Task<DeliveryChannelConfiguration?> GetByIdAsync(
        DeliveryChannelConfigurationId id,
        CancellationToken cancellationToken)
        => await context.ChannelConfigurations
            .SingleOrDefaultAsync(c => c.Id == id, cancellationToken);

    public async Task<DeliveryChannelConfiguration?> GetByChannelTypeAsync(
        Guid tenantId,
        DeliveryChannel channelType,
        CancellationToken cancellationToken)
        => await context.ChannelConfigurations
            .SingleOrDefaultAsync(
                c => c.TenantId == tenantId && c.ChannelType == channelType,
                cancellationToken);

    public async Task<IReadOnlyList<DeliveryChannelConfiguration>> ListAsync(
        Guid tenantId,
        CancellationToken cancellationToken)
        => await context.ChannelConfigurations
            .Where(c => c.TenantId == tenantId)
            .OrderBy(c => c.ChannelType)
            .ToListAsync(cancellationToken);

    public Task SaveChangesAsync(CancellationToken cancellationToken)
        => context.SaveChangesAsync(cancellationToken);
}
