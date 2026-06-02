using Microsoft.EntityFrameworkCore;
using NexTraceOne.Catalog.Application.Graph.Abstractions;
using NexTraceOne.Catalog.Domain.Graph.Enums;
using NexTraceOne.Catalog.Infrastructure.Persistence;
using NexTraceOne.Catalog.Infrastructure.Persistence;

namespace NexTraceOne.Catalog.Infrastructure.Readers;

/// <summary>
/// Implementação real de <see cref="IKnowledgeRelationReader"/>.
/// Cruza ServiceAssets, ApiAssets, ConsumerRelationships e ServiceLinks para construir
/// o grafo de relações de conhecimento entre serviços do tenant.
/// Substitui o NullKnowledgeRelationReader (honest-null pattern).
/// Wave AB.1 — GetKnowledgeRelationGraph.
/// </summary>
internal sealed class EfKnowledgeRelationReader(
    ServiceCatalogDbContext graphDb,
    ServiceCatalogDbContext contractsDb) : IKnowledgeRelationReader
{
    public async Task<IReadOnlyList<ServiceRelationEntry>> ListServiceRelationsAsync(
        string tenantId,
        CancellationToken ct)
    {
        if (!Guid.TryParse(tenantId, out var tenantGuid))
            return [];

        var services = await graphDb.ServiceAssets
            .AsNoTracking()
            .Where(s => s.TenantId == tenantGuid)
            .Select(s => new { s.Id, s.Name, s.TeamName })
            .ToListAsync(ct);

        if (services.Count == 0)
            return [];

        var serviceIdList = services.Select(s => s.Id.Value).ToList();

        var apiAssets = await graphDb.ApiAssets
            .AsNoTracking()
            .Include(a => a.OwnerService)
            .Include(a => a.ConsumerRelationships)
            .Where(a => serviceIdList.Contains(EF.Property<Guid>(a, "OwnerServiceId")))
            .ToListAsync(ct);

        var serviceLinks = await graphDb.ServiceLinks
            .AsNoTracking()
            .Where(l => serviceIdList.Contains(l.ServiceAssetId.Value)
                        && l.Category == LinkCategory.Runbook)
            .ToListAsync(ct);

        // ConsumerName → OwnerService.Name (quais serviços este consumer depende)
        var consumerNameToOwnerName = apiAssets
            .SelectMany(a => a.ConsumerRelationships.Select(cr => new { cr.ConsumerName, OwnerName = a.OwnerService.Name }))
            .GroupBy(x => x.ConsumerName, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.Select(x => x.OwnerName).Distinct(StringComparer.OrdinalIgnoreCase).ToList(), StringComparer.OrdinalIgnoreCase);

        var runbooksByService = serviceLinks
            .GroupBy(l => l.ServiceAssetId.Value)
            .ToDictionary(g => g.Key, g => g.Select(l => l.Title).ToList());

        var result = services.Select(svc =>
        {
            var ownedApis = apiAssets.Where(a => a.OwnerService.Id.Value == svc.Id.Value).ToList();

            var publishedContracts = ownedApis.Select(a => a.Name).ToList();

            var consumedContracts = ownedApis
                .SelectMany(a => a.ConsumerRelationships)
                .Where(cr => string.Equals(cr.ConsumerName, svc.Name, StringComparison.OrdinalIgnoreCase))
                .Select(cr => cr.ConsumerName)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            // Contratos consumidos: APIs onde este serviço aparece como consumidor
            var allConsumedApiNames = apiAssets
                .Where(a => a.ConsumerRelationships.Any(cr =>
                    string.Equals(cr.ConsumerName, svc.Name, StringComparison.OrdinalIgnoreCase)))
                .Select(a => a.Name)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            // Serviços dos quais este serviço depende (é consumidor de suas APIs)
            var dependsOn = apiAssets
                .Where(a => a.ConsumerRelationships.Any(cr =>
                    string.Equals(cr.ConsumerName, svc.Name, StringComparison.OrdinalIgnoreCase))
                    && a.OwnerService.Id.Value != svc.Id.Value)
                .Select(a => a.OwnerService.Name)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            runbooksByService.TryGetValue(svc.Id.Value, out var runbooks);

            return new ServiceRelationEntry(
                ServiceName: svc.Name,
                TeamName: string.IsNullOrEmpty(svc.TeamName) ? null : svc.TeamName,
                DependsOnServices: dependsOn,
                PublishedContracts: publishedContracts,
                ConsumedContracts: allConsumedApiNames,
                AssociatedRunbooks: runbooks ?? [],
                AssociatedIncidentTypes: [],
                LastReleaseAt: null,
                LastIncidentAt: null);
        }).ToList();

        return result;
    }
}
