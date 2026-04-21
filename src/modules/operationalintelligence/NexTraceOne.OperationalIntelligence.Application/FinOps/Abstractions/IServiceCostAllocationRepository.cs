using NexTraceOne.OperationalIntelligence.Domain.FinOps.Entities;
using NexTraceOne.OperationalIntelligence.Domain.FinOps.Enums;

namespace NexTraceOne.OperationalIntelligence.Application.FinOps.Abstractions;

/// <summary>
/// Contrato de repositório para registos de alocação de custo por serviço.
/// Wave I.2 — FinOps Contextual por Serviço.
/// </summary>
public interface IServiceCostAllocationRepository
{
    /// <summary>Obtém um registo de alocação pelo identificador.</summary>
    Task<ServiceCostAllocationRecord?> GetByIdAsync(ServiceCostAllocationRecordId id, CancellationToken ct = default);

    /// <summary>Lista registos de alocação por serviço e período.</summary>
    Task<IReadOnlyList<ServiceCostAllocationRecord>> ListByServiceAsync(
        string tenantId,
        string serviceName,
        DateTimeOffset since,
        DateTimeOffset until,
        CancellationToken ct = default);

    /// <summary>Lista registos de alocação por tenant e período (todos os serviços).</summary>
    Task<IReadOnlyList<ServiceCostAllocationRecord>> ListByTenantAsync(
        string tenantId,
        DateTimeOffset since,
        DateTimeOffset until,
        string? environment = null,
        CostCategory? category = null,
        CancellationToken ct = default);

    /// <summary>Adiciona um novo registo de alocação.</summary>
    void Add(ServiceCostAllocationRecord record);
}
