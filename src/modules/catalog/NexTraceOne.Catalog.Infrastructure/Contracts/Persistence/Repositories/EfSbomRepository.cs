using Microsoft.EntityFrameworkCore;

using NexTraceOne.Catalog.Application.Contracts.Abstractions;

namespace NexTraceOne.Catalog.Infrastructure.Contracts.Persistence.Repositories;

/// <summary>
/// Implementação EF Core do repositório de SbomRecord.
/// Wave AO.1 — Supply Chain &amp; Dependency Provenance.
/// </summary>
internal sealed class EfSbomRepository(ContractsDbContext context) : ISbomRepository
{
    public async Task AddAsync(SbomRecord record, CancellationToken ct)
        => await context.SbomRecords.AddAsync(record, ct);

    public async Task<SbomRecord?> GetLatestAsync(string serviceId, string tenantId, CancellationToken ct)
        => await context.SbomRecords
            .Where(s => s.ServiceId == serviceId && s.TenantId == tenantId)
            .OrderByDescending(s => s.RecordedAt)
            .FirstOrDefaultAsync(ct);

    public async Task<IReadOnlyList<SbomRecord>> ListByTenantAsync(string tenantId, CancellationToken ct)
        => await context.SbomRecords
            .Where(s => s.TenantId == tenantId)
            .OrderByDescending(s => s.RecordedAt)
            .ToListAsync(ct);
}
