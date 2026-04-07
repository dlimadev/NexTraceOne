using System.Collections.Concurrent;
using NexTraceOne.Configuration.Application.Abstractions;
using NexTraceOne.Configuration.Domain.Entities;

namespace NexTraceOne.Configuration.Infrastructure.Repositories;

/// <summary>
/// Implementação em memória do repositório de watch lists.
/// Para uso no MVP1 enquanto a persistência PostgreSQL não for adicionada.
/// </summary>
public sealed class InMemoryUserWatchRepository : IUserWatchRepository
{
    private readonly ConcurrentDictionary<Guid, UserWatch> _store = new();

    public Task<UserWatch?> GetByIdAsync(UserWatchId id, CancellationToken cancellationToken)
    {
        _store.TryGetValue(id.Value, out var watch);
        return Task.FromResult(watch);
    }

    public Task<UserWatch?> GetByEntityAsync(string userId, string tenantId, string entityType, string entityId, CancellationToken cancellationToken)
    {
        var watch = _store.Values.FirstOrDefault(w =>
            w.UserId == userId && w.TenantId == tenantId &&
            string.Equals(w.EntityType, entityType, StringComparison.OrdinalIgnoreCase) &&
            w.EntityId == entityId);
        return Task.FromResult(watch);
    }

    public Task<IReadOnlyList<UserWatch>> ListByUserAsync(string userId, string tenantId, string? entityType, CancellationToken cancellationToken)
    {
        IReadOnlyList<UserWatch> result = _store.Values
            .Where(w => w.UserId == userId && w.TenantId == tenantId &&
                (entityType is null || string.Equals(w.EntityType, entityType, StringComparison.OrdinalIgnoreCase)))
            .OrderByDescending(w => w.CreatedAt)
            .ToList();
        return Task.FromResult(result);
    }

    public Task AddAsync(UserWatch watch, CancellationToken cancellationToken)
    {
        _store[watch.Id.Value] = watch;
        return Task.CompletedTask;
    }

    public Task UpdateAsync(UserWatch watch, CancellationToken cancellationToken)
    {
        _store[watch.Id.Value] = watch;
        return Task.CompletedTask;
    }

    public Task DeleteAsync(UserWatch watch, CancellationToken cancellationToken)
    {
        _store.TryRemove(watch.Id.Value, out _);
        return Task.CompletedTask;
    }
}
