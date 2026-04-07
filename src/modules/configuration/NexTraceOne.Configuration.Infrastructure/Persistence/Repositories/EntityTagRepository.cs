using Microsoft.EntityFrameworkCore;
using NexTraceOne.BuildingBlocks.Core.Tags;
using NexTraceOne.Configuration.Application.Abstractions;

namespace NexTraceOne.Configuration.Infrastructure.Persistence.Repositories;

internal sealed class EntityTagRepository(ConfigurationDbContext context) : IEntityTagRepository
{
    public async Task<EntityTag?> GetByIdAsync(EntityTagId id, string tenantId, CancellationToken cancellationToken)
        => await context.EntityTags.SingleOrDefaultAsync(
            t => t.Id == id && t.TenantId == tenantId, cancellationToken);

    public async Task<IReadOnlyList<EntityTag>> ListByEntityAsync(string tenantId, string entityType, string entityId, CancellationToken cancellationToken)
        => await context.EntityTags
            .Where(t => t.TenantId == tenantId
                && t.EntityType == entityType
                && t.EntityId == entityId)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<EntityTag>> ListByKeyAsync(string tenantId, string key, CancellationToken cancellationToken)
        => await context.EntityTags
            .Where(t => t.TenantId == tenantId && t.Key == key)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

    public async Task AddAsync(EntityTag tag, CancellationToken cancellationToken)
        => await context.EntityTags.AddAsync(tag, cancellationToken);

    public Task UpdateAsync(EntityTag tag, CancellationToken cancellationToken)
    {
        context.EntityTags.Update(tag);
        return Task.CompletedTask;
    }

    public async Task DeleteAsync(EntityTagId id, CancellationToken cancellationToken)
    {
        var entity = await context.EntityTags.SingleOrDefaultAsync(t => t.Id == id, cancellationToken);
        if (entity is not null) context.EntityTags.Remove(entity);
    }
}
