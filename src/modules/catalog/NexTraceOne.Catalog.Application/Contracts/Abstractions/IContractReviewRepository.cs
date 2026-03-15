using NexTraceOne.Contracts.Domain.Entities;

namespace NexTraceOne.Contracts.Application.Abstractions;

/// <summary>
/// Repositório para operações de persistência de revisões de contrato.
/// </summary>
public interface IContractReviewRepository
{
    /// <summary>Adiciona uma nova revisão de contrato ao repositório.</summary>
    void Add(ContractReview review);

    /// <summary>Lista revisões de contrato vinculadas a um draft.</summary>
    Task<IReadOnlyList<ContractReview>> ListByDraftAsync(ContractDraftId draftId, CancellationToken ct = default);
}
