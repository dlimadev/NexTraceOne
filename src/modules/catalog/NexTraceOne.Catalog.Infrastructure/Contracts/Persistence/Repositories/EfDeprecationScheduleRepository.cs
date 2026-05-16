using Microsoft.EntityFrameworkCore;

using NexTraceOne.Catalog.Application.Contracts.Abstractions;
using NexTraceOne.Catalog.Domain.Contracts.Entities;

namespace NexTraceOne.Catalog.Infrastructure.Contracts.Persistence.Repositories;

/// <summary>
/// Implementação EF Core do repositório de DeprecationScheduleRecord.
/// Wave AV.3 — ScheduleContractDeprecation.
/// </summary>
internal sealed class EfDeprecationScheduleRepository(ContractsDbContext context) : IDeprecationScheduleRepository
{
    public async Task<DeprecationScheduleRecord?> GetByContractIdAsync(
        Guid contractId, string tenantId, CancellationToken ct)
        => await context.DeprecationSchedules
            .Where(s => s.ContractId == contractId && s.TenantId == tenantId)
            .FirstOrDefaultAsync(ct);

    public async Task UpsertAsync(DeprecationScheduleRecord record, CancellationToken ct)
    {
        var existing = await context.DeprecationSchedules
            .Where(s => s.ContractId == record.ContractId && s.TenantId == record.TenantId)
            .FirstOrDefaultAsync(ct);

        if (existing is null)
        {
            context.DeprecationSchedules.Add(record);
        }
        else
        {
            context.DeprecationSchedules.Remove(existing);
            context.DeprecationSchedules.Add(record with { Id = existing.Id });
        }
    }
}
