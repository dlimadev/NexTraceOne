using Microsoft.EntityFrameworkCore;
using NexTraceOne.Catalog.Application.Contracts.Abstractions;
using NexTraceOne.Catalog.Domain.Contracts.Enums;
using NexTraceOne.Catalog.Infrastructure.Contracts.Persistence;
using NexTraceOne.Catalog.Infrastructure.Graph.Persistence;

namespace NexTraceOne.Catalog.Infrastructure.Readers;

/// <summary>
/// Implementação real de IContractDeprecationPipelineReader.
/// Cruza ContractVersions (estados Deprecated/Sunset) com ApiAssets, ServiceAssets e
/// ConsumerExpectations para reportar o pipeline de deprecação de contratos por tenant.
/// Substitui o NullContractDeprecationPipelineReader (honest-null pattern).
/// </summary>
internal sealed class EfContractDeprecationPipelineReader(
    ContractsDbContext contractsDb,
    CatalogGraphDbContext graphDb) : IContractDeprecationPipelineReader
{
    public async Task<IReadOnlyList<IContractDeprecationPipelineReader.DeprecatedContractEntry>> ListDeprecatedContractsByTenantAsync(
        string tenantId,
        CancellationToken ct)
    {
        if (!Guid.TryParse(tenantId, out var tenantGuid))
            return [];

        var services = await graphDb.ServiceAssets
            .AsNoTracking()
            .Where(s => s.TenantId == tenantGuid)
            .Select(s => new { s.Id, s.TeamName, s.Tier })
            .ToListAsync(ct);

        if (services.Count == 0)
            return [];

        var serviceIdSet = services.Select(s => s.Id.Value).ToHashSet();
        var serviceMap = services.ToDictionary(s => s.Id.Value);

        var apiAssets = await graphDb.ApiAssets
            .AsNoTracking()
            .Where(a => serviceIdSet.Contains(EF.Property<Guid>(a, "OwnerServiceId")))
            .Select(a => new { a.Id, a.Name, OwnerServiceId = EF.Property<Guid>(a, "OwnerServiceId") })
            .ToListAsync(ct);

        if (apiAssets.Count == 0)
            return [];

        var apiAssetMap = apiAssets.ToDictionary(a => a.Id.Value);
        var apiAssetIds = apiAssets.Select(a => a.Id.Value).ToHashSet();

        var deprecatedVersions = await contractsDb.ContractVersions
            .AsNoTracking()
            .Where(v => apiAssetIds.Contains(v.ApiAssetId)
                && (v.LifecycleState == ContractLifecycleState.Deprecated
                    || v.LifecycleState == ContractLifecycleState.Sunset))
            .Select(v => new
            {
                v.Id,
                v.ApiAssetId,
                v.SemVer,
                v.Protocol,
                v.DeprecationDate,
                v.SunsetDate,
                v.CreatedAt
            })
            .ToListAsync(ct);

        if (deprecatedVersions.Count == 0)
            return [];

        var deprecatedApiAssetIds = deprecatedVersions.Select(v => v.ApiAssetId).ToHashSet();

        var activeConsumers = await contractsDb.ConsumerExpectations
            .AsNoTracking()
            .Where(e => deprecatedApiAssetIds.Contains(e.ApiAssetId) && e.IsActive)
            .Select(e => new { e.ApiAssetId, e.ConsumerServiceName })
            .ToListAsync(ct);

        var consumersByApiAsset = activeConsumers
            .GroupBy(e => e.ApiAssetId)
            .ToDictionary(g => g.Key, g => g.Select(e => e.ConsumerServiceName).ToList());

        var result = new List<IContractDeprecationPipelineReader.DeprecatedContractEntry>(deprecatedVersions.Count);

        foreach (var version in deprecatedVersions)
        {
            if (!apiAssetMap.TryGetValue(version.ApiAssetId, out var api))
                continue;

            if (!serviceMap.TryGetValue(api.OwnerServiceId, out var svc))
                continue;

            var consumers = consumersByApiAsset.TryGetValue(version.ApiAssetId, out var list) ? list : [];

            result.Add(new IContractDeprecationPipelineReader.DeprecatedContractEntry(
                ContractId: version.Id.Value,
                ContractName: api.Name,
                ContractVersion: version.SemVer,
                Protocol: version.Protocol.ToString(),
                OwnerTeamId: svc.TeamName,
                ServiceId: api.OwnerServiceId.ToString(),
                ServiceTier: svc.Tier.ToString(),
                DeprecatedAt: version.DeprecationDate ?? version.CreatedAt,
                SunsetDeadline: version.SunsetDate,
                TotalConsumers: consumers.Count,
                NotifiedConsumers: 0,
                MigratedConsumers: 0,
                BlockingConsumerIds: consumers,
                FirstNotificationSentAt: null));
        }

        return result;
    }
}
