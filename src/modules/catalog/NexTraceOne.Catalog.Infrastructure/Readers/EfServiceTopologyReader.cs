using Microsoft.EntityFrameworkCore;

using NexTraceOne.Catalog.Application.Contracts.Abstractions;
using NexTraceOne.Catalog.Domain.Graph.Enums;
using NexTraceOne.Catalog.Infrastructure.Persistence;

namespace NexTraceOne.Catalog.Infrastructure.Readers;

/// <summary>
/// Implementação real de IServiceTopologyReader.
/// Usa ServiceAssets, ApiAssets e ConsumerRelationships para mapear nós e arestas da topologia.
/// Wave AR.1 — GetServiceTopologyHealthReport.
/// </summary>
internal sealed class EfServiceTopologyReader(ServiceCatalogDbContext graphDb) : IServiceTopologyReader
{
    public async Task<IReadOnlyList<IServiceTopologyReader.ServiceNodeEntry>> ListServiceNodesByTenantAsync(
        string tenantId, CancellationToken ct)
    {
        if (!Guid.TryParse(tenantId, out var tenantGuid))
            return [];

        var services = await graphDb.ServiceAssets
            .AsNoTracking()
            .Where(s => s.TenantId == tenantGuid)
            .ToListAsync(ct);

        return services
            .Select(s => new IServiceTopologyReader.ServiceNodeEntry(
                ServiceId: s.Id.Value.ToString(),
                ServiceName: s.Name,
                ServiceTier: s.Tier.ToString(),
                IsCustomerFacing: s.ExposureType != ExposureType.Internal,
                LastUpdatedAt: s.UpdatedAt))
            .ToList();
    }

    public async Task<IReadOnlyList<IServiceTopologyReader.ServiceDependencyEntry>> ListDependenciesByTenantAsync(
        string tenantId, int freshnessThresholdDays, CancellationToken ct)
    {
        if (!Guid.TryParse(tenantId, out var tenantGuid))
            return [];

        var cutoff = DateTimeOffset.UtcNow.AddDays(-freshnessThresholdDays);

        var services = await graphDb.ServiceAssets
            .AsNoTracking()
            .Where(s => s.TenantId == tenantGuid)
            .Select(s => new { s.Id, s.Name, s.Tier })
            .ToListAsync(ct);

        if (services.Count == 0)
            return [];

        var nameToService = services.ToDictionary(s => s.Name, s => s, StringComparer.OrdinalIgnoreCase);
        var serviceIdList = services.Select(s => s.Id.Value).ToList();

        // ApiAssets deste tenant com OwnerService e ConsumerRelationships frescos
        var apiAssets = await graphDb.ApiAssets
            .AsNoTracking()
            .Where(a => serviceIdList.Contains(EF.Property<Guid>(a, "OwnerServiceId")))
            .Include(a => a.OwnerService)
            .Include(a => a.ConsumerRelationships.Where(cr => cr.LastObservedAt >= cutoff))
            .ToListAsync(ct);

        var seen = new HashSet<(string, string)>();
        var result = new List<IServiceTopologyReader.ServiceDependencyEntry>();

        foreach (var api in apiAssets)
        {
            var targetId = api.OwnerService.Id.Value.ToString();
            var targetTier = api.OwnerService.Tier.ToString();

            foreach (var cr in api.ConsumerRelationships)
            {
                if (!nameToService.TryGetValue(cr.ConsumerName, out var sourceSvc))
                    continue;

                var sourceId = sourceSvc.Id.Value.ToString();
                if (!seen.Add((sourceId, targetId))) continue;

                result.Add(new IServiceTopologyReader.ServiceDependencyEntry(
                    SourceServiceId: sourceId,
                    TargetServiceId: targetId,
                    SourceServiceTier: sourceSvc.Tier.ToString(),
                    TargetServiceTier: targetTier,
                    LastUpdatedAt: cr.LastObservedAt));
            }
        }

        return result;
    }
}
