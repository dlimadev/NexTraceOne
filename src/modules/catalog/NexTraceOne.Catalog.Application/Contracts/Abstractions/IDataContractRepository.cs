using NexTraceOne.Catalog.Domain.Entities;

namespace NexTraceOne.Catalog.Application.Contracts.Abstractions;

/// <summary>
/// Wave AQ.1 — RegisterDataContract / GetDataContractComplianceReport.
/// </summary>
public interface IDataContractRepository
{
    Task AddAsync(DataContractRecord record, CancellationToken ct);
    Task<IReadOnlyList<DataContractRecord>> ListByTenantAsync(string tenantId, CancellationToken ct);
    Task<IReadOnlyList<DataContractRecord>> ListByTeamAsync(string tenantId, string teamId, CancellationToken ct);
}
