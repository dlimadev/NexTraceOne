using Microsoft.EntityFrameworkCore;

using NexTraceOne.Catalog.Application.Contracts.Abstractions;
using NexTraceOne.Catalog.Infrastructure.Contracts.Persistence;
using NexTraceOne.Catalog.Infrastructure.Graph.Persistence;

namespace NexTraceOne.Catalog.Infrastructure.Readers;

/// <summary>
/// Implementação real de ISchemaQualityReader.
/// Usa ContractScorecard (ContractsDbContext) e ApiAssets/ServiceAssets (CatalogGraphDbContext)
/// para reportar qualidade de schema por contrato publicado no tenant.
/// Wave AQ.2 — GetSchemaQualityIndexReport.
/// </summary>
internal sealed class EfSchemaQualityReader(
    ContractsDbContext contractsDb,
    CatalogGraphDbContext graphDb) : ISchemaQualityReader
{
    public async Task<IReadOnlyList<ISchemaQualityReader.ContractSchemaEntry>> ListByTenantAsync(
        string tenantId, CancellationToken ct)
    {
        if (!Guid.TryParse(tenantId, out var tenantGuid))
            return [];

        var services = await graphDb.ServiceAssets
            .AsNoTracking()
            .Where(s => s.TenantId == tenantGuid)
            .Select(s => new { s.Id, s.Tier })
            .ToListAsync(ct);

        if (services.Count == 0)
            return [];

        var serviceIds = services.Select(s => s.Id.Value).ToList();
        var tierByServiceId = services.ToDictionary(s => s.Id.Value, s => s.Tier.ToString());

        var apiAssets = await graphDb.ApiAssets
            .AsNoTracking()
            .Where(a => serviceIds.Contains(EF.Property<Guid>(a, "OwnerServiceId")))
            .Select(a => new { a.Id, a.Name, OwnerServiceId = EF.Property<Guid>(a, "OwnerServiceId") })
            .ToListAsync(ct);

        if (apiAssets.Count == 0)
            return [];

        var apiAssetIds = apiAssets.Select(a => a.Id.Value).ToList();
        var apiById = apiAssets.ToDictionary(a => a.Id.Value, a => a);

        // Versão mais recente por ApiAsset (sem versões mais novas do mesmo ativo)
        var latestVersions = await contractsDb.ContractVersions
            .AsNoTracking()
            .Where(v => apiAssetIds.Contains(v.ApiAssetId)
                && !contractsDb.ContractVersions.Any(v2 =>
                    v2.ApiAssetId == v.ApiAssetId && v2.CreatedAt > v.CreatedAt))
            .ToListAsync(ct);

        if (latestVersions.Count == 0)
            return [];

        var versionIdValues = latestVersions.Select(v => v.Id.Value).ToList();

        var scorecards = await contractsDb.ContractScorecards
            .AsNoTracking()
            .Where(s => versionIdValues.Contains(s.ContractVersionId.Value))
            .ToListAsync(ct);

        var scorecardByVersion = scorecards.ToDictionary(s => s.ContractVersionId.Value, s => s);

        return latestVersions
            .Select(v =>
            {
                var api = apiById.GetValueOrDefault(v.ApiAssetId);
                if (api is null) return null;

                scorecardByVersion.TryGetValue(v.Id.Value, out var sc);
                var tier = tierByServiceId.GetValueOrDefault(api.OwnerServiceId, "Standard");
                var totalOps = sc?.OperationCount ?? 0;
                var totalSchemas = sc?.SchemaCount ?? 0;

                return new ISchemaQualityReader.ContractSchemaEntry(
                    ContractId: v.Id.Value.ToString(),
                    ContractName: api.Name,
                    Protocol: v.Protocol.ToString(),
                    ServiceTier: tier,
                    TotalFields: totalSchemas,
                    FieldsWithDescription: sc?.HasDescriptions == true ? totalSchemas : 0,
                    FieldsWithExamples: sc?.HasExamples == true ? totalSchemas : 0,
                    OperationsWithErrorCodes: sc?.HasSecurityDefinitions == true ? totalOps : 0,
                    TotalOperations: totalOps,
                    FieldsWithConstraints: 0,
                    EnumFieldsWith3PlusValues: 0,
                    TotalEnumFields: 0);
            })
            .Where(e => e is not null)
            .ToList()!;
    }

    public async Task<IReadOnlyList<ISchemaQualityReader.SchemaQualitySnapshot>> GetMonthlySnapshotsAsync(
        string tenantId, int months, CancellationToken ct)
    {
        if (!Guid.TryParse(tenantId, out var tenantGuid))
            return [];

        var since = DateTimeOffset.UtcNow.AddMonths(-months);

        var serviceIds = await graphDb.ServiceAssets
            .AsNoTracking()
            .Where(s => s.TenantId == tenantGuid)
            .Select(s => s.Id.Value)
            .ToListAsync(ct);

        if (serviceIds.Count == 0)
            return [];

        var apiAssetIds = await graphDb.ApiAssets
            .AsNoTracking()
            .Where(a => serviceIds.Contains(EF.Property<Guid>(a, "OwnerServiceId")))
            .Select(a => a.Id.Value)
            .ToListAsync(ct);

        if (apiAssetIds.Count == 0)
            return [];

        var versionIds = await contractsDb.ContractVersions
            .AsNoTracking()
            .Where(v => apiAssetIds.Contains(v.ApiAssetId))
            .Select(v => v.Id.Value)
            .ToListAsync(ct);

        var scorecards = await contractsDb.ContractScorecards
            .AsNoTracking()
            .Where(s => versionIds.Contains(s.ContractVersionId.Value) && s.ComputedAt >= since)
            .Select(s => new { s.ComputedAt, s.QualityScore })
            .ToListAsync(ct);

        return scorecards
            .GroupBy(s => new { s.ComputedAt.Year, s.ComputedAt.Month })
            .Select(g => new ISchemaQualityReader.SchemaQualitySnapshot(
                SnapshotDate: new DateTimeOffset(g.Key.Year, g.Key.Month, 1, 0, 0, 0, TimeSpan.Zero),
                TenantSchemaHealthScore: (double)g.Average(s => s.QualityScore)))
            .OrderBy(s => s.SnapshotDate)
            .ToList();
    }
}
