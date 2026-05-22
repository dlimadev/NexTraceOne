using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using NexTraceOne.Catalog.Application.Contracts.Abstractions;
using NexTraceOne.Catalog.Domain.Contracts.Enums;
using NexTraceOne.Catalog.Infrastructure.Contracts.Persistence;
using NexTraceOne.Catalog.Infrastructure.Graph.Persistence;

namespace NexTraceOne.Catalog.Infrastructure.Readers;

/// <summary>
/// Implementação real de IApiVersionStrategyReader.
/// Cruza ServiceAssets, ApiAssets, ContractVersions e ContractDiffs para reportar
/// estratégia de versionamento de APIs por serviço e tenant.
/// Substitui o NullApiVersionStrategyReader (honest-null pattern).
/// </summary>
internal sealed class EfApiVersionStrategyReader(
    ContractsDbContext contractsDb,
    CatalogGraphDbContext graphDb) : IApiVersionStrategyReader
{
    private static readonly Regex SemverRegex = new(@"^\d+\.\d+\.\d+", RegexOptions.Compiled);

    public async Task<IReadOnlyList<IApiVersionStrategyReader.ServiceVersionEntry>> ListServiceVersionDataByTenantAsync(
        string tenantId,
        int lookbackDays,
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

        var serviceIdSet = services.Select(s => s.Id.Value).ToHashSet();

        var apiAssets = await graphDb.ApiAssets
            .AsNoTracking()
            .Where(a => serviceIdSet.Contains(EF.Property<Guid>(a, "OwnerServiceId")))
            .Select(a => new { a.Id, a.Name, OwnerServiceId = EF.Property<Guid>(a, "OwnerServiceId") })
            .ToListAsync(ct);

        if (apiAssets.Count == 0)
            return [];

        var apiAssetIds = apiAssets.Select(a => a.Id.Value).ToHashSet();

        var activeStates = new[]
        {
            ContractLifecycleState.Deprecated,
            ContractLifecycleState.Sunset,
            ContractLifecycleState.Retired
        };

        var contractVersions = await contractsDb.ContractVersions
            .AsNoTracking()
            .Where(v => apiAssetIds.Contains(v.ApiAssetId) && !activeStates.Contains(v.LifecycleState))
            .Select(v => new { v.ApiAssetId, v.Id, v.SemVer, v.Protocol, v.CreatedAt })
            .ToListAsync(ct);

        var breakingCutoff = DateTimeOffset.UtcNow.AddDays(-90);

        var contractDiffsWithBreaking = await contractsDb.ContractDiffs
            .AsNoTracking()
            .Where(d => apiAssetIds.Contains(d.ApiAssetId) && d.ComputedAt >= breakingCutoff)
            .Select(d => new { d.ApiAssetId, d.BreakingChanges })
            .ToListAsync(ct);

        var breakingCountByApi = contractDiffsWithBreaking
            .GroupBy(d => d.ApiAssetId)
            .ToDictionary(g => g.Key, g => g.Sum(d => d.BreakingChanges.Count));

        var versionsByApi = contractVersions
            .GroupBy(v => v.ApiAssetId)
            .ToDictionary(g => g.Key, g => g.ToList());

        var apiByService = apiAssets
            .GroupBy(a => a.OwnerServiceId)
            .ToDictionary(g => g.Key, g => g.ToList());

        var serviceMap = services.ToDictionary(s => s.Id.Value);

        var result = new List<IApiVersionStrategyReader.ServiceVersionEntry>(services.Count);

        foreach (var svc in services)
        {
            if (!apiByService.TryGetValue(svc.Id.Value, out var serviceApis))
                continue;

            foreach (var api in serviceApis)
            {
                var versions = versionsByApi.TryGetValue(api.Id.Value, out var vList) ? vList : [];
                var breakingCount = breakingCountByApi.TryGetValue(api.Id.Value, out var bc) ? bc : 0;

                var activeVersionTags = versions.Select(v => v.SemVer).Distinct().ToList();
                var semverAdherence = activeVersionTags.All(tag => SemverRegex.IsMatch(tag));

                var oldestVersion = versions
                    .OrderBy(v => v.CreatedAt)
                    .Select(v => v.SemVer)
                    .FirstOrDefault();

                double avgLifetime = 0;
                if (versions.Count >= 2)
                {
                    var sorted = versions.OrderBy(v => v.CreatedAt).ToList();
                    var lifetimes = new List<double>();
                    for (var i = 1; i < sorted.Count; i++)
                        lifetimes.Add((sorted[i].CreatedAt - sorted[i - 1].CreatedAt).TotalDays);
                    avgLifetime = lifetimes.Average();
                }

                var protocol = versions.FirstOrDefault()?.Protocol.ToString() ?? "Unknown";

                result.Add(new IApiVersionStrategyReader.ServiceVersionEntry(
                    ServiceId: svc.Id.Value.ToString(),
                    ServiceName: svc.Name,
                    OwnerTeamId: svc.TeamName,
                    Protocol: protocol,
                    ActiveVersionCount: activeVersionTags.Count,
                    SemverAdherence: semverAdherence,
                    BreakingChangesLast90d: breakingCount,
                    AvgVersionLifetimeDays: avgLifetime,
                    OldestActiveVersion: oldestVersion,
                    ActiveVersionTags: activeVersionTags));
            }
        }

        return result;
    }
}
