using Microsoft.EntityFrameworkCore;
using NexTraceOne.Catalog.Application.Services.Abstractions;
using NexTraceOne.Catalog.Domain.Contracts.Enums;
using NexTraceOne.Catalog.Domain.Graph.Enums;
using NexTraceOne.Catalog.Infrastructure.Contracts.Persistence;
using NexTraceOne.Catalog.Infrastructure.Graph.Persistence;

namespace NexTraceOne.Catalog.Infrastructure.Readers;

/// <summary>
/// Implementação real de <see cref="ICatalogHealthMaintenanceReader"/>.
/// Agrega dados de qualidade de manutenção do catálogo cruzando ServiceAssets,
/// ApiAssets, ContractVersions e ServiceLinks.
/// Substitui o NullCatalogHealthMaintenanceReader (honest-null pattern).
/// Wave AM.3 — GetCatalogHealthMaintenanceReport.
/// </summary>
internal sealed class EfCatalogHealthMaintenanceReader(
    CatalogGraphDbContext graphDb,
    ContractsDbContext contractsDb) : ICatalogHealthMaintenanceReader
{
    public async Task<IReadOnlyList<ICatalogHealthMaintenanceReader.ServiceMaintenanceEntry>> ListByTenantAsync(
        string tenantId,
        CancellationToken ct)
    {
        if (!Guid.TryParse(tenantId, out var tenantGuid))
            return [];

        var services = await graphDb.ServiceAssets
            .AsNoTracking()
            .Where(s => s.TenantId == tenantGuid)
            .ToListAsync(ct);

        if (services.Count == 0)
            return [];

        var serviceIdList = services.Select(s => s.Id.Value).ToList();

        var apiAssets = await graphDb.ApiAssets
            .AsNoTracking()
            .Include(a => a.OwnerService)
            .Where(a => serviceIdList.Contains(EF.Property<Guid>(a, "OwnerServiceId")))
            .ToListAsync(ct);

        var apiAssetIds = apiAssets.Select(a => a.Id.Value).ToHashSet();

        var contractVersions = apiAssetIds.Count > 0
            ? await contractsDb.ContractVersions
                .AsNoTracking()
                .Where(cv => apiAssetIds.Contains(cv.ApiAssetId))
                .Select(cv => new { cv.ApiAssetId, cv.LifecycleState })
                .ToListAsync(ct)
            : [];

        var runbookLinks = await graphDb.ServiceLinks
            .AsNoTracking()
            .Where(l => serviceIdList.Contains(l.ServiceAssetId.Value)
                        && l.Category == LinkCategory.Runbook)
            .Select(l => l.ServiceAssetId.Value)
            .ToListAsync(ct);

        // Contrato aprovado ou bloqueado por ApiAssetId
        var approvedApiIds = contractVersions
            .Where(cv => cv.LifecycleState is ContractLifecycleState.Approved or ContractLifecycleState.Locked)
            .Select(cv => cv.ApiAssetId)
            .ToHashSet();

        // ApiAssets por OwnerServiceId para lookup rápido
        var apisByService = apiAssets
            .GroupBy(a => a.OwnerService.Id.Value)
            .ToDictionary(g => g.Key, g => g.ToList());

        var result = services.Select(svc =>
        {
            var serviceIdGuid = svc.Id.Value;
            apisByService.TryGetValue(serviceIdGuid, out var ownedApis);
            ownedApis ??= [];

            var descWordCount = string.IsNullOrWhiteSpace(svc.Description)
                ? 0
                : svc.Description.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;

            var hasApprovedContract = ownedApis.Any(a => approvedApiIds.Contains(a.Id.Value));

            DateTimeOffset? lastDepUpdate = ownedApis.Count > 0
                ? ownedApis.Max(a => (DateTimeOffset?)a.OwnerService.UpdatedAt)
                : null;

            var hasRunbook = runbookLinks.Contains(serviceIdGuid);

            var lastOwnership = svc.UpdatedAt;
            var lastMaintenance = lastDepUpdate.HasValue && lastDepUpdate > lastOwnership
                ? lastDepUpdate
                : lastOwnership;

            return new ICatalogHealthMaintenanceReader.ServiceMaintenanceEntry(
                ServiceId: serviceIdGuid.ToString(),
                ServiceName: svc.Name,
                ServiceTier: svc.Tier.ToString(),
                DescriptionWordCount: descWordCount,
                LastOwnershipUpdate: lastOwnership,
                HasApprovedContract: hasApprovedContract,
                LastDependencyUpdate: lastDepUpdate,
                HasActiveRunbook: hasRunbook,
                LastMaintenanceActivity: lastMaintenance);
        }).ToList();

        return result;
    }
}
