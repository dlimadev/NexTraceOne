using Microsoft.EntityFrameworkCore;
using NexTraceOne.Configuration.Application.Abstractions;
using NexTraceOne.Configuration.Domain.Entities;

namespace NexTraceOne.Configuration.Infrastructure.Persistence.Repositories;

internal sealed class UserWatchRepository(ConfigurationDbContext context) : IUserWatchRepository
{
    public async Task<UserWatch?> GetByIdAsync(UserWatchId id, CancellationToken cancellationToken)
        => await context.UserWatches.SingleOrDefaultAsync(w => w.Id == id, cancellationToken);

    public async Task<UserWatch?> GetByEntityAsync(string userId, string tenantId, string entityType, string entityId, CancellationToken cancellationToken)
        => await context.UserWatches.SingleOrDefaultAsync(
            w => w.UserId == userId
                && w.TenantId == tenantId
                && w.EntityType == entityType
                && w.EntityId == entityId,
            cancellationToken);

    public async Task<IReadOnlyList<UserWatch>> ListByUserAsync(string userId, string tenantId, string? entityType, CancellationToken cancellationToken)
        => await context.UserWatches
            .Where(w => w.UserId == userId
                && w.TenantId == tenantId
                && (entityType == null || w.EntityType == entityType))
            .OrderByDescending(w => w.CreatedAt)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

    public async Task AddAsync(UserWatch watch, CancellationToken cancellationToken)
        => await context.UserWatches.AddAsync(watch, cancellationToken);

    public Task UpdateAsync(UserWatch watch, CancellationToken cancellationToken)
    {
        context.UserWatches.Update(watch);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(UserWatch watch, CancellationToken cancellationToken)
    {
        context.UserWatches.Remove(watch);
        return Task.CompletedTask;
    }
}
