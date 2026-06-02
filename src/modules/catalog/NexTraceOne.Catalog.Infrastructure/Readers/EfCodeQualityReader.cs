using NexTraceOne.Catalog.Infrastructure.Persistence;

using NexTraceOne.Catalog.Application.Contracts.Abstractions;
using NexTraceOne.Catalog.Infrastructure.Contracts.Persistence;

namespace NexTraceOne.Catalog.Infrastructure.Readers;

/// <summary>
/// Implementação EF Core de <see cref="ICodeQualityReader"/>.
/// Retorna o registo de qualidade mais recente por serviço dentro do tenant.
/// Wave AQ.2 — Code Quality &amp; Static Analysis Intelligence.
/// </summary>
internal sealed class EfCodeQualityReader(ServiceCatalogDbContext contractsDbContext) : ICodeQualityReader
{
    public async Task<IReadOnlyList<CodeQualityEntry>> ListLatestByTenantAsync(
        string tenantId, CancellationToken ct)
    {
        // Selecciona apenas os registos para os quais não existe um mais recente do mesmo serviço
        var latest = await contractsDbContext.CodeQualityRecords
            .AsNoTracking()
            .Where(r => r.TenantId == tenantId
                && !contractsDbContext.CodeQualityRecords.Any(r2 =>
                    r2.TenantId == tenantId
                    && r2.ServiceId == r.ServiceId
                    && r2.AnalyzedAt > r.AnalyzedAt))
            .ToListAsync(ct);

        return latest.Select(r => new CodeQualityEntry(
            ServiceId: r.ServiceId,
            ServiceName: r.ServiceName,
            ProjectKey: r.ProjectKey,
            QualityGateStatus: r.QualityGateStatus,
            Coverage: r.Coverage,
            Bugs: r.Bugs,
            Vulnerabilities: r.Vulnerabilities,
            CodeSmells: r.CodeSmells,
            DuplicatedLinesDensity: r.DuplicatedLinesDensity,
            Branch: r.Branch,
            AnalyzedAt: r.AnalyzedAt,
            QualityGatePassed: r.QualityGateStatus.Equals("OK", StringComparison.OrdinalIgnoreCase)))
            .ToList();
    }
}
