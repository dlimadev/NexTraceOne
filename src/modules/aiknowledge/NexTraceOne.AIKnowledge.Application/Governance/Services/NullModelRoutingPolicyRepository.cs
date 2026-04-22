using NexTraceOne.AIKnowledge.Application.Governance.Abstractions;
using NexTraceOne.AIKnowledge.Domain.Governance.Entities;
using NexTraceOne.AIKnowledge.Domain.Governance.Enums;

namespace NexTraceOne.AIKnowledge.Application.Governance.Services;

/// <summary>
/// Implementação nula do repositório de políticas de roteamento de modelo.
/// Retorna sempre null/lista vazia — substituir por implementação EF em produção.
/// </summary>
public sealed class NullModelRoutingPolicyRepository : IModelRoutingPolicyRepository
{
    public Task<ModelRoutingPolicy?> GetActiveAsync(
        Guid tenantId,
        PromptIntent intent,
        CancellationToken ct)
        => Task.FromResult<ModelRoutingPolicy?>(null);

    public Task<IReadOnlyList<ModelRoutingPolicy>> ListByTenantAsync(
        Guid tenantId,
        CancellationToken ct)
        => Task.FromResult<IReadOnlyList<ModelRoutingPolicy>>([]);

    public Task AddAsync(ModelRoutingPolicy policy, CancellationToken ct)
        => Task.CompletedTask;
}
