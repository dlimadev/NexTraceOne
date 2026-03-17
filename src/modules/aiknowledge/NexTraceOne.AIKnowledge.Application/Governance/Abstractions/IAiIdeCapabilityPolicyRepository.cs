using NexTraceOne.AIKnowledge.Domain.Governance.Entities;
using NexTraceOne.AIKnowledge.Domain.Governance.Enums;

namespace NexTraceOne.AIKnowledge.Application.Governance.Abstractions;

/// <summary>
/// Repositório para gestão de políticas de capacidade IDE.
/// </summary>
public interface IAiIdeCapabilityPolicyRepository
{
    /// <summary>Obtém política por identificador.</summary>
    Task<AIIDECapabilityPolicy?> GetByIdAsync(AIIDECapabilityPolicyId id, CancellationToken cancellationToken);

    /// <summary>Obtém política para um tipo de cliente e persona específicos.</summary>
    Task<AIIDECapabilityPolicy?> GetByClientTypeAndPersonaAsync(
        AIClientType clientType,
        string? persona,
        CancellationToken cancellationToken);

    /// <summary>Lista políticas filtradas por tipo de cliente.</summary>
    Task<IReadOnlyList<AIIDECapabilityPolicy>> ListAsync(
        AIClientType? clientType,
        bool? isActive,
        int pageSize,
        CancellationToken cancellationToken);

    /// <summary>Adiciona nova política.</summary>
    Task AddAsync(AIIDECapabilityPolicy policy, CancellationToken cancellationToken);

    /// <summary>Atualiza política existente.</summary>
    Task UpdateAsync(AIIDECapabilityPolicy policy, CancellationToken cancellationToken);
}
