using System.Collections.Concurrent;
using NexTraceOne.BuildingBlocks.Core.Tags;
using NexTraceOne.Configuration.Application.Abstractions;

namespace NexTraceOne.Configuration.Infrastructure.Repositories;

/// <summary>Implementação em memória do repositório de tags de entidades.</summary>
public sealed class InMemoryEntityTagRepository : IEntityTagRepository
{
    private readonly ConcurrentDictionary<Guid, EntityTag> _store = new();

    public Task<EntityTag?> GetByIdAsync(EntityTagId id, string tenantId, CancellationToken cancellationToken)
    {
        _store.TryGetValue(id.Value, out var tag);
        return Task.FromResult(tag?.TenantId == tenantId ? tag : null);
    }

    public Task<IReadOnlyList<EntityTag>> ListByEntityAsync(string tenantId, string entityType, string entityId, CancellationToken cancellationToken)
    {
        IReadOnlyList<EntityTag> result = _store.Values
            .Where(t => t.TenantId == tenantId &&
                string.Equals(t.EntityType, entityType, StringComparison.OrdinalIgnoreCase) &&
                t.EntityId == entityId)
            .OrderBy(t => t.Key)
            .ToList();
        return Task.FromResult(result);
    }

    public Task<IReadOnlyList<EntityTag>> ListByKeyAsync(string tenantId, string key, CancellationToken cancellationToken)
    {
        IReadOnlyList<EntityTag> result = _store.Values
            .Where(t => t.TenantId == tenantId &&
                string.Equals(t.Key, key, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(t => t.CreatedAt)
            .ToList();
        return Task.FromResult(result);
    }

    public Task AddAsync(EntityTag tag, CancellationToken cancellationToken)
    {
        _store[tag.Id.Value] = tag;
        return Task.CompletedTask;
    }

    public Task UpdateAsync(EntityTag tag, CancellationToken cancellationToken)
    {
        _store[tag.Id.Value] = tag;
        return Task.CompletedTask;
    }

    public Task DeleteAsync(EntityTagId id, CancellationToken cancellationToken)
    {
        _store.TryRemove(id.Value, out _);
        return Task.CompletedTask;
    }
}
