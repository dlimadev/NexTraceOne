using System.Collections.Concurrent;
using NexTraceOne.Configuration.Application.Abstractions;
using NexTraceOne.Configuration.Domain.Entities;

namespace NexTraceOne.Configuration.Infrastructure.Repositories;

/// <summary>
/// Implementação em memória do repositório de templates de contrato.
/// Para uso no MVP1 enquanto a persistência PostgreSQL não for adicionada.
/// </summary>
public sealed class InMemoryContractTemplateRepository : IContractTemplateRepository
{
    private readonly ConcurrentDictionary<Guid, ContractTemplate> _store = new();

    public Task<ContractTemplate?> GetByIdAsync(ContractTemplateId id, string tenantId, CancellationToken cancellationToken)
    {
        _store.TryGetValue(id.Value, out var template);
        var result = template?.TenantId == tenantId ? template : null;
        return Task.FromResult(result);
    }

    public Task<IReadOnlyList<ContractTemplate>> ListByTenantAsync(string tenantId, string? contractType, CancellationToken cancellationToken)
    {
        IReadOnlyList<ContractTemplate> result = _store.Values
            .Where(t => t.TenantId == tenantId &&
                        (contractType is null ||
                         string.Equals(t.ContractType, contractType, StringComparison.OrdinalIgnoreCase)))
            .OrderByDescending(t => t.CreatedAt)
            .ToList();
        return Task.FromResult(result);
    }

    public Task AddAsync(ContractTemplate template, CancellationToken cancellationToken)
    {
        _store[template.Id.Value] = template;
        return Task.CompletedTask;
    }

    public Task DeleteAsync(ContractTemplateId id, CancellationToken cancellationToken)
    {
        _store.TryRemove(id.Value, out _);
        return Task.CompletedTask;
    }
}
