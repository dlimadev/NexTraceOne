using System.Collections.Concurrent;

using NexTraceOne.AIKnowledge.Application.Governance.Abstractions;
using NexTraceOne.AIKnowledge.Domain.Governance.Entities;
using NexTraceOne.AIKnowledge.Domain.Governance.Enums;

namespace NexTraceOne.AIKnowledge.Application.Governance.Services;

/// <summary>
/// Implementação nula do repositório de planos de execução agentic.
/// Armazena planos em memória via ConcurrentDictionary para suportar testes unitários.
/// Não persiste dados entre reinicializações — substituir por implementação EF em produção.
/// </summary>
public sealed class NullAgentExecutionPlanRepository : IAgentExecutionPlanRepository
{
    private static readonly ConcurrentDictionary<Guid, AgentExecutionPlan> _store = new();

    public Task<AgentExecutionPlan?> GetByIdAsync(AgentExecutionPlanId id, CancellationToken ct)
    {
        _store.TryGetValue(id.Value, out var plan);
        return Task.FromResult(plan);
    }

    public Task<IReadOnlyList<AgentExecutionPlan>> ListByTenantAsync(
        Guid tenantId,
        PlanStatus? statusFilter,
        int pageSize,
        CancellationToken ct)
    {
        IEnumerable<AgentExecutionPlan> query = _store.Values
            .Where(p => p.TenantId == tenantId);

        if (statusFilter.HasValue)
            query = query.Where(p => p.PlanStatus == statusFilter.Value);

        IReadOnlyList<AgentExecutionPlan> result = query
            .OrderByDescending(p => p.CreatedAt)
            .Take(pageSize)
            .ToList()
            .AsReadOnly();

        return Task.FromResult(result);
    }

    public Task AddAsync(AgentExecutionPlan plan, CancellationToken ct)
    {
        _store[plan.Id.Value] = plan;
        return Task.CompletedTask;
    }

    public Task UpdateAsync(AgentExecutionPlan plan, CancellationToken ct)
    {
        _store[plan.Id.Value] = plan;
        return Task.CompletedTask;
    }

    /// <summary>Limpa o store em memória — útil para testes.</summary>
    public static void Clear() => _store.Clear();
}
