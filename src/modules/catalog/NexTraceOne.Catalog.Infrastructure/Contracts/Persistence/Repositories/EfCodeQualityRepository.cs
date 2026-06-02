using NexTraceOne.Catalog.Infrastructure.Persistence;

using NexTraceOne.Catalog.Application.Contracts.Abstractions;

namespace NexTraceOne.Catalog.Infrastructure.Contracts.Persistence.Repositories;

/// <summary>
/// Implementação EF Core do repositório de CodeQualityRecord.
/// Wave AQ.2 — Code Quality &amp; Static Analysis Intelligence.
/// </summary>
internal sealed class EfCodeQualityRepository(ServiceCatalogDbContext context) : ICodeQualityRepository
{
    public async Task AddAsync(CodeQualityRecord record, CancellationToken ct)
        => await context.CodeQualityRecords.AddAsync(record, ct);

    public async Task<CodeQualityRecord?> GetLatestAsync(string serviceId, string tenantId, CancellationToken ct)
        => await context.CodeQualityRecords
            .Where(r => r.ServiceId == serviceId && r.TenantId == tenantId)
            .OrderByDescending(r => r.AnalyzedAt)
            .FirstOrDefaultAsync(ct);

    public async Task<IReadOnlyList<CodeQualityRecord>> ListByTenantAsync(string tenantId, CancellationToken ct)
        => await context.CodeQualityRecords
            .Where(r => r.TenantId == tenantId)
            .OrderByDescending(r => r.AnalyzedAt)
            .ToListAsync(ct);
}
