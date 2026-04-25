using NexTraceOne.Catalog.Domain.Contracts.Entities;

namespace NexTraceOne.Catalog.Application.Contracts.Abstractions;

/// <summary>
/// Repositório para propostas de breaking change com workflow de consulta de consumidores.
/// CC-06.
/// </summary>
public interface IBreakingChangeProposalRepository
{
    /// <summary>Adiciona uma nova proposta.</summary>
    Task AddAsync(BreakingChangeProposal proposal, CancellationToken ct);

    /// <summary>Obtém uma proposta pelo identificador.</summary>
    Task<BreakingChangeProposal?> GetByIdAsync(BreakingChangeProposalId id, CancellationToken ct);

    /// <summary>Lista propostas por contrato.</summary>
    Task<IReadOnlyList<BreakingChangeProposal>> ListByContractAsync(
        Guid contractId, string tenantId, CancellationToken ct);

    /// <summary>Lista propostas activas por tenant.</summary>
    Task<IReadOnlyList<BreakingChangeProposal>> ListActiveByTenantAsync(
        string tenantId, CancellationToken ct);

    /// <summary>Actualiza uma proposta existente.</summary>
    Task UpdateAsync(BreakingChangeProposal proposal, CancellationToken ct);
}
