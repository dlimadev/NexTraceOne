using NexTraceOne.ChangeGovernance.Domain.ChangeIntelligence.Entities;
using NexTraceOne.ChangeGovernance.Domain.ChangeIntelligence.Enums;

namespace NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Abstractions;

/// <summary>
/// Contrato de repositório para pedidos de mudança externos importados de sistemas como ServiceNow e Jira.
/// Suporta consulta por sistema externo, estado e serviço afetado.
/// </summary>
public interface IExternalChangeRequestRepository
{
    /// <summary>Obtém um pedido de mudança pela chave natural (sistema externo + identificador externo).</summary>
    Task<ExternalChangeRequest?> GetByExternalIdAsync(string externalSystem, string externalId, CancellationToken ct);

    /// <summary>Lista pedidos de mudança filtrando por estado.</summary>
    Task<IReadOnlyList<ExternalChangeRequest>> ListByStatusAsync(ExternalChangeRequestStatus status, CancellationToken ct);

    /// <summary>Lista pedidos de mudança associados a um serviço específico.</summary>
    Task<IReadOnlyList<ExternalChangeRequest>> ListByServiceAsync(Guid serviceId, CancellationToken ct);

    /// <summary>Adiciona um novo pedido de mudança externo para persistência.</summary>
    void Add(ExternalChangeRequest request);
}
