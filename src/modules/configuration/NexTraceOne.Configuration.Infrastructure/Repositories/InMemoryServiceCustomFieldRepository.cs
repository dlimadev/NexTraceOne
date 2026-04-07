using System.Collections.Concurrent;
using NexTraceOne.Configuration.Application.Abstractions;
using NexTraceOne.Configuration.Domain.Entities;

namespace NexTraceOne.Configuration.Infrastructure.Repositories;

/// <summary>Implementação em memória do repositório de campos personalizados de serviços.</summary>
public sealed class InMemoryServiceCustomFieldRepository : IServiceCustomFieldRepository
{
    private readonly ConcurrentDictionary<Guid, ServiceCustomField> _store = new();

    public Task<ServiceCustomField?> GetByIdAsync(ServiceCustomFieldId id, string tenantId, CancellationToken cancellationToken)
    {
        _store.TryGetValue(id.Value, out var field);
        return Task.FromResult(field?.TenantId == tenantId ? field : null);
    }

    public Task<IReadOnlyList<ServiceCustomField>> ListByTenantAsync(string tenantId, CancellationToken cancellationToken)
    {
        IReadOnlyList<ServiceCustomField> result = _store.Values
            .Where(f => f.TenantId == tenantId)
            .OrderBy(f => f.SortOrder)
            .ToList();
        return Task.FromResult(result);
    }

    public Task AddAsync(ServiceCustomField field, CancellationToken cancellationToken)
    {
        _store[field.Id.Value] = field;
        return Task.CompletedTask;
    }

    public Task UpdateAsync(ServiceCustomField field, CancellationToken cancellationToken)
    {
        _store[field.Id.Value] = field;
        return Task.CompletedTask;
    }

    public Task DeleteAsync(ServiceCustomFieldId id, CancellationToken cancellationToken)
    {
        _store.TryRemove(id.Value, out _);
        return Task.CompletedTask;
    }
}
