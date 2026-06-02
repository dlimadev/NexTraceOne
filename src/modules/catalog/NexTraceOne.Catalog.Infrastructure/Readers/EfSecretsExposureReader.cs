using Microsoft.EntityFrameworkCore;
using NexTraceOne.Catalog.Application.Services.Abstractions;
using NexTraceOne.Catalog.Domain.Contracts.Enums;
using NexTraceOne.Catalog.Infrastructure.Persistence;
using NexTraceOne.Catalog.Infrastructure.Persistence;

namespace NexTraceOne.Catalog.Infrastructure.Readers;

/// <summary>
/// Implementação real de ISecretsExposureReader.
/// Carrega ContractVersions activos (Draft/Approved/Locked) de ApiAssets pertencentes ao tenant
/// para que o handler de exposição de segredos possa aplicar pattern matching.
/// Substitui o NullSecretsExposureReader (honest-null pattern).
/// </summary>
internal sealed class EfSecretsExposureReader(
    ServiceCatalogDbContext contractsDb,
    ServiceCatalogDbContext graphDb) : ISecretsExposureReader
{
    public async Task<IReadOnlyList<ArtifactTextEntry>> ListArtifactTextsAsync(
        string tenantId,
        int maxArtifacts,
        CancellationToken ct)
    {
        if (!Guid.TryParse(tenantId, out var tenantGuid))
            return [];

        var services = await graphDb.ServiceAssets
            .AsNoTracking()
            .Where(s => s.TenantId == tenantGuid)
            .Select(s => new { s.Id, s.Name })
            .ToListAsync(ct);

        if (services.Count == 0)
            return [];

        var serviceIdSet = services.Select(s => s.Id.Value).ToHashSet();
        var serviceNameMap = services.ToDictionary(s => s.Id.Value, s => s.Name);

        var apiAssets = await graphDb.ApiAssets
            .AsNoTracking()
            .Where(a => serviceIdSet.Contains(EF.Property<Guid>(a, "OwnerServiceId")))
            .Select(a => new { a.Id, a.Name, OwnerServiceId = EF.Property<Guid>(a, "OwnerServiceId") })
            .ToListAsync(ct);

        if (apiAssets.Count == 0)
            return [];

        var apiAssetIds = apiAssets.Select(a => a.Id.Value).ToHashSet();
        var apiMap = apiAssets.ToDictionary(a => a.Id.Value);

        var scanStates = new[]
        {
            ContractLifecycleState.Draft,
            ContractLifecycleState.Approved,
            ContractLifecycleState.Locked
        };

        // Obter a versão mais recente por ApiAsset dentro dos estados activos
        var latestVersions = await contractsDb.ContractVersions
            .AsNoTracking()
            .Where(v => apiAssetIds.Contains(v.ApiAssetId) && scanStates.Contains(v.LifecycleState))
            .GroupBy(v => v.ApiAssetId)
            .Select(g => g.OrderByDescending(v => v.CreatedAt).First())
            .Take(maxArtifacts)
            .ToListAsync(ct);

        var result = new List<ArtifactTextEntry>(latestVersions.Count);

        foreach (var version in latestVersions)
        {
            if (!apiMap.TryGetValue(version.ApiAssetId, out var api))
                continue;

            var serviceName = serviceNameMap.TryGetValue(api.OwnerServiceId, out var sn) ? sn : string.Empty;

            result.Add(new ArtifactTextEntry(
                ArtifactId: version.Id.Value.ToString(),
                ArtifactType: "Contract",
                ServiceName: serviceName,
                Title: $"{api.Name} v{version.SemVer}",
                Content: version.SpecContent));
        }

        return result;
    }
}
