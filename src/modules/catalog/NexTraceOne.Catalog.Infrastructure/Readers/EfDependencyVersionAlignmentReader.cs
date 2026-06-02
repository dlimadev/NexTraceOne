using NexTraceOne.Catalog.Infrastructure.Persistence;

namespace NexTraceOne.Catalog.Infrastructure.Readers;

/// <summary>
/// Implementação real de IDependencyVersionAlignmentReader.
/// Cruza SbomRecords (ServiceCatalogDbContext) com ServiceAssets (ServiceCatalogDbContext) para reportar
/// alinhamento de versões de componentes por serviço. Quando não há SbomRecords, usa
/// ServiceCatalogDbContext como fallback.
/// Substitui o NullDependencyVersionAlignmentReader (honest-null pattern).
/// </summary>
internal sealed class EfDependencyVersionAlignmentReader(
    ServiceCatalogDbContext contractsDb,
    ServiceCatalogDbContext graphDb,
    ServiceCatalogDbContext depDb) : IDependencyVersionAlignmentReader
{
    public async Task<IReadOnlyList<IDependencyVersionAlignmentReader.ComponentVersionEntry>> ListComponentVersionsByTenantAsync(
        string tenantId,
        CancellationToken ct)
    {
        if (!Guid.TryParse(tenantId, out var tenantGuid))
            return [];

        var services = await graphDb.ServiceAssets
            .AsNoTracking()
            .Where(s => s.TenantId == tenantGuid)
            .Select(s => new { s.Id, s.Name, s.TeamName, s.Tier })
            .ToListAsync(ct);

        if (services.Count == 0)
            return [];

        var serviceIdStrings = services.Select(s => s.Id.Value.ToString()).ToHashSet();

        var latestSboms = await contractsDb.SbomRecords
            .AsNoTracking()
            .Where(r => r.TenantId == tenantId && serviceIdStrings.Contains(r.ServiceId))
            .GroupBy(r => r.ServiceId)
            .Select(g => g.OrderByDescending(r => r.RecordedAt).First())
            .ToListAsync(ct);

        if (latestSboms.Count > 0)
        {
            var serviceMap = services.ToDictionary(s => s.Id.Value.ToString());
            var result = new List<IDependencyVersionAlignmentReader.ComponentVersionEntry>();

            foreach (var sbom in latestSboms)
            {
                if (!serviceMap.TryGetValue(sbom.ServiceId, out var svc))
                    continue;

                foreach (var component in sbom.Components ?? [])
                {
                    result.Add(new IDependencyVersionAlignmentReader.ComponentVersionEntry(
                        ServiceId: sbom.ServiceId,
                        ServiceName: svc.Name,
                        TeamId: svc.TeamName,
                        ServiceTier: svc.Tier.ToString(),
                        ComponentName: component.Name,
                        ComponentVersion: component.Version,
                        HasKnownCve: component.CveCount > 0,
                        IngestedAt: sbom.RecordedAt));
                }
            }

            return result;
        }

        // Fallback: ServiceCatalogDbContext
        var serviceGuids = services.Select(s => s.Id.Value).ToHashSet();

        var profiles = await depDb.ServiceDependencyProfiles
            .AsNoTracking()
            .Where(p => serviceGuids.Contains(p.ServiceId))
            .Include(p => p.Dependencies)
            .ToListAsync(ct);

        var serviceMapFallback = services.ToDictionary(s => s.Id.Value);
        var fallbackResult = new List<IDependencyVersionAlignmentReader.ComponentVersionEntry>();

        foreach (var profile in profiles)
        {
            if (!serviceMapFallback.TryGetValue(profile.ServiceId, out var svc))
                continue;

            foreach (var dep in profile.Dependencies)
            {
                fallbackResult.Add(new IDependencyVersionAlignmentReader.ComponentVersionEntry(
                    ServiceId: profile.ServiceId.ToString(),
                    ServiceName: svc.Name,
                    TeamId: svc.TeamName,
                    ServiceTier: svc.Tier.ToString(),
                    ComponentName: dep.PackageName,
                    ComponentVersion: dep.Version,
                    HasKnownCve: dep.Vulnerabilities.Count > 0,
                    IngestedAt: profile.LastScanAt));
            }
        }

        return fallbackResult;
    }
}
