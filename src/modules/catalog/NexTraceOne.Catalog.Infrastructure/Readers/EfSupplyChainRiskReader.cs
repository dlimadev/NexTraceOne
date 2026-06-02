using Microsoft.EntityFrameworkCore;

using NexTraceOne.Catalog.Application.Contracts.Abstractions;
using NexTraceOne.Catalog.Domain.Graph.Enums;
using NexTraceOne.Catalog.Infrastructure.Persistence;

namespace NexTraceOne.Catalog.Infrastructure.Readers;

/// <summary>
/// Implementação real de ISupplyChainRiskReader.
/// Cruza SbomRecord (ServiceCatalogDbContext) com ServiceAsset (ServiceCatalogDbContext)
/// para identificar componentes vulneráveis e os serviços afectados no tenant.
/// Wave AO.3 — GetSupplyChainRiskReport.
/// </summary>
internal sealed class EfSupplyChainRiskReader(
    ServiceCatalogDbContext contractsDb,
    ServiceCatalogDbContext graphDb) : ISupplyChainRiskReader
{
    public async Task<IReadOnlyList<ISupplyChainRiskReader.VulnerableComponentEntry>> ListVulnerableComponentsByTenantAsync(
        string tenantId,
        CancellationToken ct)
    {
        if (!Guid.TryParse(tenantId, out var tenantGuid))
            return [];

        var sboms = await contractsDb.SbomRecords
            .AsNoTracking()
            .Where(r => r.TenantId == tenantId)
            .ToListAsync(ct);

        if (sboms.Count == 0)
            return [];

        var services = await graphDb.ServiceAssets
            .AsNoTracking()
            .Where(s => s.TenantId == tenantGuid)
            .Select(s => new { s.Id, s.ExposureType })
            .ToListAsync(ct);

        var totalServices = services.Count;
        var customerFacingIds = services
            .Where(s => s.ExposureType != ExposureType.Internal)
            .Select(s => s.Id.Value.ToString())
            .ToHashSet();

        // SBOM mais recente por serviço
        var latestByService = sboms
            .GroupBy(s => s.ServiceId)
            .Select(g => g.OrderByDescending(s => s.RecordedAt).First())
            .ToList();

        var componentToServices = new Dictionary<(string Name, string Version), List<string>>();
        var componentMetadata = new Dictionary<(string Name, string Version), (string Severity, int Count)>();

        foreach (var sbom in latestByService)
        {
            foreach (var c in sbom.Components ?? [])
            {
                if (c.CveCount <= 0) continue;

                var key = (c.Name, c.Version);

                if (!componentToServices.TryGetValue(key, out var list))
                    componentToServices[key] = list = [];
                list.Add(sbom.ServiceId);

                if (!componentMetadata.ContainsKey(key))
                    componentMetadata[key] = (c.HighestCveSeverity, c.CveCount);
            }
        }

        return componentToServices
            .Select(kvp =>
            {
                var meta = componentMetadata[kvp.Key];
                var affected = (IReadOnlyList<string>)kvp.Value;
                return new ISupplyChainRiskReader.VulnerableComponentEntry(
                    ComponentName: kvp.Key.Name,
                    ComponentVersion: kvp.Key.Version,
                    HighestCveSeverity: meta.Severity,
                    CveCount: meta.Count,
                    CvePublishedAt: null,
                    FixVersion: null,
                    DirectlyAffectedServiceIds: affected,
                    TransitivelyAffectedServiceIds: [],
                    HasCustomerFacingExposed: affected.Any(id => customerFacingIds.Contains(id)),
                    TotalServicesInTenant: totalServices);
            })
            .OrderByDescending(e => e.CveCount)
            .ThenByDescending(e => e.DirectlyAffectedServiceIds.Count)
            .ToList();
    }
}
