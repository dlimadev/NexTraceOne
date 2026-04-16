using NexTraceOne.ChangeGovernance.Domain.ChangeIntelligence.Entities;
using NexTraceOne.ChangeGovernance.Domain.ChangeIntelligence.Enums;

namespace NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Abstractions;

/// <summary>Contrato de repositório para ReleaseApprovalRequest.</summary>
public interface IApprovalRequestRepository
{
    /// <summary>Busca um pedido de aprovação pelo seu identificador.</summary>
    Task<ReleaseApprovalRequest?> GetByIdAsync(ReleaseApprovalRequestId id, CancellationToken cancellationToken = default);

    /// <summary>Lista pedidos de aprovação activos (Pending) de uma release.</summary>
    Task<IReadOnlyList<ReleaseApprovalRequest>> ListPendingByReleaseIdAsync(ReleaseId releaseId, CancellationToken cancellationToken = default);

    /// <summary>Lista todos os pedidos de aprovação de uma release.</summary>
    Task<IReadOnlyList<ReleaseApprovalRequest>> ListByReleaseIdAsync(ReleaseId releaseId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Localiza um pedido de aprovação pelo hash do callback token.
    /// Usado para validar respostas inbound de sistemas externos.
    /// </summary>
    Task<ReleaseApprovalRequest?> GetByCallbackTokenHashAsync(string tokenHash, CancellationToken cancellationToken = default);

    /// <summary>Adiciona um novo pedido de aprovação.</summary>
    void Add(ReleaseApprovalRequest request);
}
