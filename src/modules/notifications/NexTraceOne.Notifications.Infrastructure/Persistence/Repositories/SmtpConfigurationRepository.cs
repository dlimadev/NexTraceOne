using Microsoft.EntityFrameworkCore;

using NexTraceOne.Notifications.Application.Abstractions;
using NexTraceOne.Notifications.Domain.Entities;
using NexTraceOne.Notifications.Domain.StronglyTypedIds;

namespace NexTraceOne.Notifications.Infrastructure.Persistence.Repositories;

internal sealed class SmtpConfigurationRepository(NotificationsDbContext context)
    : ISmtpConfigurationStore
{
    public async Task AddAsync(SmtpConfiguration config, CancellationToken cancellationToken)
        => await context.SmtpConfigurations.AddAsync(config, cancellationToken);

    public async Task<SmtpConfiguration?> GetByTenantAsync(
        Guid tenantId,
        CancellationToken cancellationToken)
        => await context.SmtpConfigurations
            .SingleOrDefaultAsync(c => c.TenantId == tenantId, cancellationToken);

    public async Task<SmtpConfiguration?> GetByIdAsync(
        SmtpConfigurationId id,
        CancellationToken cancellationToken)
        => await context.SmtpConfigurations
            .SingleOrDefaultAsync(c => c.Id == id, cancellationToken);

    public Task SaveChangesAsync(CancellationToken cancellationToken)
        => context.SaveChangesAsync(cancellationToken);
}
