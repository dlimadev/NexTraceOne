using NexTraceOne.AIKnowledge.Domain.Governance.Entities;
using NexTraceOne.AIKnowledge.Domain.Governance.Enums;

namespace NexTraceOne.AIKnowledge.Application.Governance.Abstractions;

/// <summary>
/// Repositório de políticas de roteamento de modelo por intenção de prompt.
/// </summary>
public interface IModelRoutingPolicyRepository
{
    /// <summary>Obtém a política activa para um tenant e intenção específica.</summary>
    Task<ModelRoutingPolicy?> GetActiveAsync(
        Guid tenantId,
        PromptIntent intent,
        CancellationToken ct);

    /// <summary>Lista todas as políticas de um tenant.</summary>
    Task<IReadOnlyList<ModelRoutingPolicy>> ListByTenantAsync(Guid tenantId, CancellationToken ct);

    /// <summary>Adiciona uma nova política.</summary>
    Task AddAsync(ModelRoutingPolicy policy, CancellationToken ct);
}
