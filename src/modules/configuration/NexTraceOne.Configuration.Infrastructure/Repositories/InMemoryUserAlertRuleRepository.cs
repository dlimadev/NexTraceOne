using System.Collections.Concurrent;
using NexTraceOne.Configuration.Application.Abstractions;
using NexTraceOne.Configuration.Domain.Entities;

namespace NexTraceOne.Configuration.Infrastructure.Repositories;

/// <summary>
/// Implementação em memória do repositório de regras de alerta.
/// Para uso no MVP1 enquanto a persistência PostgreSQL não for adicionada.
/// </summary>
public sealed class InMemoryUserAlertRuleRepository : IUserAlertRuleRepository
{
    private readonly ConcurrentDictionary<Guid, UserAlertRule> _store = new();

    public Task<UserAlertRule?> GetByIdAsync(UserAlertRuleId id, CancellationToken cancellationToken)
    {
        _store.TryGetValue(id.Value, out var rule);
        return Task.FromResult(rule);
    }

    public Task<IReadOnlyList<UserAlertRule>> ListByUserAsync(string userId, string tenantId, CancellationToken cancellationToken)
    {
        IReadOnlyList<UserAlertRule> result = _store.Values
            .Where(r => r.UserId == userId && r.TenantId == tenantId)
            .OrderByDescending(r => r.CreatedAt)
            .ToList();
        return Task.FromResult(result);
    }

    public Task AddAsync(UserAlertRule rule, CancellationToken cancellationToken)
    {
        _store[rule.Id.Value] = rule;
        return Task.CompletedTask;
    }

    public Task UpdateAsync(UserAlertRule rule, CancellationToken cancellationToken)
    {
        _store[rule.Id.Value] = rule;
        return Task.CompletedTask;
    }

    public Task DeleteAsync(UserAlertRule rule, CancellationToken cancellationToken)
    {
        _store.TryRemove(rule.Id.Value, out _);
        return Task.CompletedTask;
    }
}
