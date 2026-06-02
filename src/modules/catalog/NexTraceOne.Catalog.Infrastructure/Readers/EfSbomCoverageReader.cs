using Microsoft.EntityFrameworkCore;
using NexTraceOne.Catalog.Application.Contracts.Abstractions;
using NexTraceOne.Catalog.Infrastructure.Persistence;

namespace NexTraceOne.Catalog.Infrastructure.Readers;

/// <summary>
/// Implementação real de ISbomCoverageReader.
/// Cruza ServiceAsset com SbomRecord (ctr_sbom_records) para reportar cobertura de SBOM por serviço.
/// Substitui o NullSbomCoverageReader (honest-null pattern).
/// </summary>
internal sealed class EfSbomCoverageReader(
    ServiceCatalogDbContext graphDb,
    ServiceCatalogDbContext contractsDb) : ISbomCoverageReader
{
    public async Task<IReadOnlyList<ISbomCoverageReader.ServiceSbomEntry>> ListByTenantAsync(
        string tenantId,
        CancellationToken ct)
    {
        if (!Guid.TryParse(tenantId, out var tenantGuid))
            return [];

        var services = await graphDb.ServiceAssets
            .AsNoTracking()
            .Where(s => s.TenantId == tenantGuid)
            .Select(s => new { s.Id, s.Name, s.TeamName, s.Tier, s.ExposureType })
            .ToListAsync(ct);

        if (services.Count == 0)
            return [];

        // SbomRecord.ServiceId e TenantId são strings
        var serviceIdStrings = services.Select(s => s.Id.Value.ToString()).ToHashSet();

        var sbomsByService = await contractsDb.SbomRecords
            .AsNoTracking()
            .Where(r => r.TenantId == tenantId && serviceIdStrings.Contains(r.ServiceId))
            .GroupBy(r => r.ServiceId)
            .Select(g => g.OrderByDescending(r => r.RecordedAt).First())
            .ToListAsync(ct);

        var sbomMap = sbomsByService.ToDictionary(r => r.ServiceId);

        var entries = services.Select(svc =>
        {
            var serviceIdStr = svc.Id.Value.ToString();
            var customerFacing = svc.ExposureType != Domain.Graph.Enums.ExposureType.Internal;

            if (!sbomMap.TryGetValue(serviceIdStr, out var sbom))
            {
                return new ISbomCoverageReader.ServiceSbomEntry(
                    ServiceId: serviceIdStr,
                    ServiceName: svc.Name,
                    TeamName: svc.TeamName,
                    ServiceTier: svc.Tier.ToString(),
                    CustomerFacing: customerFacing,
                    ComponentCount: 0,
                    HighSeverityCveCount: 0,
                    CriticalCveCount: 0,
                    OutdatedComponentCount: 0,
                    LicenseDistribution: new Dictionary<string, int>(),
                    LastSbomRecordedAt: null,
                    GplOrAgplComponents: []);
            }

            var components = sbom.Components ?? [];

            var licenses = components
                .Where(c => !string.IsNullOrWhiteSpace(c.License))
                .GroupBy(c => c.License)
                .ToDictionary(g => g.Key, g => g.Count());

            var gplComponents = components
                .Where(c => c.License is not null &&
                    (c.License.Contains("GPL", StringComparison.OrdinalIgnoreCase)
                     || c.License.Contains("AGPL", StringComparison.OrdinalIgnoreCase)))
                .Select(c => c.Name)
                .ToList();

            var criticalCount = components.Count(c =>
                string.Equals(c.HighestCveSeverity, "Critical", StringComparison.OrdinalIgnoreCase));
            var highCount = components.Count(c =>
                string.Equals(c.HighestCveSeverity, "High", StringComparison.OrdinalIgnoreCase));

            return new ISbomCoverageReader.ServiceSbomEntry(
                ServiceId: serviceIdStr,
                ServiceName: svc.Name,
                TeamName: svc.TeamName,
                ServiceTier: svc.Tier.ToString(),
                CustomerFacing: customerFacing,
                ComponentCount: components.Count,
                HighSeverityCveCount: highCount,
                CriticalCveCount: criticalCount,
                OutdatedComponentCount: 0,
                LicenseDistribution: licenses,
                LastSbomRecordedAt: sbom.RecordedAt,
                GplOrAgplComponents: gplComponents);
        }).ToList();

        return entries.OrderBy(e => e.LastSbomRecordedAt ?? DateTimeOffset.MinValue).ToList();
    }
}
