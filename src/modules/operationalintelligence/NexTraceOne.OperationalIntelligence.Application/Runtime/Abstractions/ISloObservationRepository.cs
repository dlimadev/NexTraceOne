using NexTraceOne.OperationalIntelligence.Domain.Runtime.Entities;
using NexTraceOne.OperationalIntelligence.Domain.Runtime.Enums;

namespace NexTraceOne.OperationalIntelligence.Application.Runtime.Abstractions;

/// <summary>
/// Contrato de repositório para observações de SLO.
/// Wave J.2 — SLO Tracking (OperationalIntelligence).
/// </summary>
public interface ISloObservationRepository
{
    /// <summary>Obtém uma observação pelo identificador.</summary>
    Task<SloObservation?> GetByIdAsync(SloObservationId id, CancellationToken ct = default);

    /// <summary>Lista observações por serviço e período.</summary>
    Task<IReadOnlyList<SloObservation>> ListByServiceAsync(
        string tenantId,
        string serviceName,
        DateTimeOffset since,
        DateTimeOffset until,
        string? environment = null,
        CancellationToken ct = default);

    /// <summary>Lista observações por tenant e período (todos os serviços).</summary>
    Task<IReadOnlyList<SloObservation>> ListByTenantAsync(
        string tenantId,
        DateTimeOffset since,
        DateTimeOffset until,
        SloObservationStatus? statusFilter = null,
        CancellationToken ct = default);

    /// <summary>Adiciona uma nova observação de SLO.</summary>
    void Add(SloObservation observation);
}
