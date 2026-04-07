using System.Collections.Concurrent;
using NexTraceOne.Configuration.Application.Abstractions;
using NexTraceOne.Configuration.Domain.Entities;

namespace NexTraceOne.Configuration.Infrastructure.Repositories;

/// <summary>
/// Implementação em memória do repositório de checklists de mudança.
/// Para uso no MVP1 enquanto a persistência PostgreSQL não for adicionada.
/// </summary>
public sealed class InMemoryChangeChecklistRepository : IChangeChecklistRepository
{
    private readonly ConcurrentDictionary<Guid, ChangeChecklist> _store = new();

    public Task<ChangeChecklist?> GetByIdAsync(ChangeChecklistId id, string tenantId, CancellationToken cancellationToken)
    {
        _store.TryGetValue(id.Value, out var checklist);
        var result = checklist?.TenantId == tenantId ? checklist : null;
        return Task.FromResult(result);
    }

    public Task<IReadOnlyList<ChangeChecklist>> ListByTenantAsync(string tenantId, CancellationToken cancellationToken)
    {
        IReadOnlyList<ChangeChecklist> result = _store.Values
            .Where(c => c.TenantId == tenantId)
            .OrderByDescending(c => c.CreatedAt)
            .ToList();
        return Task.FromResult(result);
    }

    public Task<IReadOnlyList<ChangeChecklist>> GetForChangeAsync(string tenantId, string changeType, string? environment, CancellationToken cancellationToken)
    {
        IReadOnlyList<ChangeChecklist> result = _store.Values
            .Where(c => c.TenantId == tenantId &&
                        string.Equals(c.ChangeType, changeType, StringComparison.OrdinalIgnoreCase) &&
                        (environment is null || c.Environment is null ||
                         string.Equals(c.Environment, environment, StringComparison.OrdinalIgnoreCase)))
            .OrderByDescending(c => c.CreatedAt)
            .ToList();
        return Task.FromResult(result);
    }

    public Task AddAsync(ChangeChecklist checklist, CancellationToken cancellationToken)
    {
        _store[checklist.Id.Value] = checklist;
        return Task.CompletedTask;
    }

    public Task UpdateAsync(ChangeChecklist checklist, CancellationToken cancellationToken)
    {
        _store[checklist.Id.Value] = checklist;
        return Task.CompletedTask;
    }

    public Task DeleteAsync(ChangeChecklistId id, CancellationToken cancellationToken)
    {
        _store.TryRemove(id.Value, out _);
        return Task.CompletedTask;
    }
}
