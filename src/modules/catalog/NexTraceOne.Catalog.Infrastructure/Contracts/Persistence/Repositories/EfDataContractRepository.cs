using Microsoft.EntityFrameworkCore;

using NexTraceOne.Catalog.Application.Contracts.Abstractions;
using NexTraceOne.Catalog.Domain.Entities;

namespace NexTraceOne.Catalog.Infrastructure.Contracts.Persistence.Repositories;

/// <summary>
/// Implementação EF Core do repositório de DataContractRecord.
/// Wave AQ.1 — RegisterDataContract / GetDataContractComplianceReport.
/// </summary>
internal sealed class EfDataContractRepository(ContractsDbContext context) : IDataContractRepository
{
    public async Task AddAsync(DataContractRecord record, CancellationToken ct)
        => await context.DataContractRecords.AddAsync(record, ct);

    public async Task<IReadOnlyList<DataContractRecord>> ListByTenantAsync(string tenantId, CancellationToken ct)
        => await context.DataContractRecords
            .Where(r => r.TenantId == tenantId)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<DataContractRecord>> ListByTeamAsync(
        string tenantId, string teamId, CancellationToken ct)
        => await context.DataContractRecords
            .Where(r => r.TenantId == tenantId && r.OwnerTeamId == teamId)
            .ToListAsync(ct);
}
