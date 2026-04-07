using System.Collections.Concurrent;
using NexTraceOne.Configuration.Application.Abstractions;
using NexTraceOne.Configuration.Domain.Entities;

namespace NexTraceOne.Configuration.Infrastructure.Repositories;

/// <summary>
/// Implementação em memória do repositório de regras de automação.
/// Para uso no MVP1 enquanto a persistência PostgreSQL não for adicionada.
/// </summary>
public sealed class InMemoryAutomationRuleRepository : IAutomationRuleRepository
{
    private readonly ConcurrentDictionary<Guid, AutomationRule> _store = new();

    public Task<AutomationRule?> GetByIdAsync(AutomationRuleId id, string tenantId, CancellationToken cancellationToken)
    {
        _store.TryGetValue(id.Value, out var rule);
        var result = rule?.TenantId == tenantId ? rule : null;
        return Task.FromResult(result);
    }

    public Task<IReadOnlyList<AutomationRule>> ListByTenantAsync(string tenantId, CancellationToken cancellationToken)
    {
        IReadOnlyList<AutomationRule> result = _store.Values
            .Where(r => r.TenantId == tenantId)
            .OrderByDescending(r => r.CreatedAt)
            .ToList();
        return Task.FromResult(result);
    }

    public Task<IReadOnlyList<AutomationRule>> GetByTriggerAsync(string tenantId, string trigger, CancellationToken cancellationToken)
    {
        IReadOnlyList<AutomationRule> result = _store.Values
            .Where(r => r.TenantId == tenantId && r.IsEnabled &&
                        string.Equals(r.Trigger, trigger, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(r => r.CreatedAt)
            .ToList();
        return Task.FromResult(result);
    }

    public Task AddAsync(AutomationRule rule, CancellationToken cancellationToken)
    {
        _store[rule.Id.Value] = rule;
        return Task.CompletedTask;
    }

    public Task UpdateAsync(AutomationRule rule, CancellationToken cancellationToken)
    {
        _store[rule.Id.Value] = rule;
        return Task.CompletedTask;
    }

    public Task DeleteAsync(AutomationRuleId id, CancellationToken cancellationToken)
    {
        _store.TryRemove(id.Value, out _);
        return Task.CompletedTask;
    }
}
