using NexTraceOne.Contracts.Domain.Entities;
using NexTraceOne.Contracts.Domain.Enums;

namespace NexTraceOne.Contracts.Application.Abstractions;

/// <summary>
/// Repositório para operações de persistência de drafts de contrato.
/// </summary>
public interface IContractDraftRepository
{
    /// <summary>Adiciona um novo draft de contrato ao repositório.</summary>
    void Add(ContractDraft draft);

    /// <summary>Busca um draft de contrato pelo seu identificador.</summary>
    Task<ContractDraft?> GetByIdAsync(ContractDraftId id, CancellationToken ct = default);

    /// <summary>Lista drafts de contrato com filtros opcionais e paginação.</summary>
    Task<IReadOnlyList<ContractDraft>> ListAsync(
        DraftStatus? status,
        Guid? serviceId,
        string? author,
        int page,
        int pageSize,
        CancellationToken ct = default);

    /// <summary>Conta drafts de contrato que atendem aos filtros.</summary>
    Task<int> CountAsync(
        DraftStatus? status,
        Guid? serviceId,
        string? author,
        CancellationToken ct = default);

    /// <summary>Lista drafts de contrato por estado.</summary>
    Task<IReadOnlyList<ContractDraft>> ListByStatusAsync(DraftStatus status, CancellationToken ct = default);
}
