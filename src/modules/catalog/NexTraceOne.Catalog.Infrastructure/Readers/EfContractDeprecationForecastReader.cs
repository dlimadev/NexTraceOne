using Microsoft.EntityFrameworkCore;
using NexTraceOne.Catalog.Application.Contracts.Abstractions;
using NexTraceOne.Catalog.Domain.Contracts.Enums;
using NexTraceOne.Catalog.Infrastructure.Contracts.Persistence;
using NexTraceOne.Catalog.Infrastructure.Graph.Persistence;

namespace NexTraceOne.Catalog.Infrastructure.Readers;

/// <summary>
/// Implementação real de IContractDeprecationForecastReader.
/// Cruza ServiceAssets, ApiAssets, ContractVersions, ConsumerExpectations e DeprecationSchedules
/// para prever contratos candidatos a deprecação por tenant.
/// Substitui o NullContractDeprecationForecastReader (honest-null pattern).
/// </summary>
internal sealed class EfContractDeprecationForecastReader(
    ContractsDbContext contractsDb,
    CatalogGraphDbContext graphDb) : IContractDeprecationForecastReader
{
    public async Task<IReadOnlyList<IContractDeprecationForecastReader.ActiveContractForecastEntry>> ListActiveContractsByTenantAsync(
        string tenantId,
        CancellationToken ct)
    {
        if (!Guid.TryParse(tenantId, out var tenantGuid))
            return [];

        var services = await graphDb.ServiceAssets
            .AsNoTracking()
            .Where(s => s.TenantId == tenantGuid)
            .Select(s => new { s.Id, s.TeamName })
            .ToListAsync(ct);

        if (services.Count == 0)
            return [];

        var serviceIdSet = services.Select(s => s.Id.Value).ToHashSet();
        var serviceTeamMap = services.ToDictionary(s => s.Id.Value, s => s.TeamName);

        var apiAssets = await graphDb.ApiAssets
            .AsNoTracking()
            .Where(a => serviceIdSet.Contains(EF.Property<Guid>(a, "OwnerServiceId")))
            .Select(a => new { a.Id, a.Name, OwnerServiceId = EF.Property<Guid>(a, "OwnerServiceId") })
            .ToListAsync(ct);

        if (apiAssets.Count == 0)
            return [];

        var apiAssetIds = apiAssets.Select(a => a.Id.Value).ToHashSet();
        var apiMap = apiAssets.ToDictionary(a => a.Id.Value);

        var inactiveStates = new[]
        {
            ContractLifecycleState.Deprecated,
            ContractLifecycleState.Sunset,
            ContractLifecycleState.Retired
        };

        var activeVersions = await contractsDb.ContractVersions
            .AsNoTracking()
            .Where(v => apiAssetIds.Contains(v.ApiAssetId) && !inactiveStates.Contains(v.LifecycleState))
            .Select(v => new { v.Id, v.ApiAssetId, v.SemVer, v.Protocol, v.CreatedAt })
            .ToListAsync(ct);

        if (activeVersions.Count == 0)
            return [];

        var activeContractIds = activeVersions.Select(v => v.Id.Value).ToHashSet();

        var now = DateTimeOffset.UtcNow;
        var minus30 = now.AddDays(-30);
        var minus60 = now.AddDays(-60);

        var allConsumers = await contractsDb.ConsumerExpectations
            .AsNoTracking()
            .Where(e => apiAssetIds.Contains(e.ApiAssetId))
            .Select(e => new { e.ApiAssetId, e.IsActive, e.RegisteredAt })
            .ToListAsync(ct);

        var deprecationSchedules = await contractsDb.DeprecationSchedules
            .AsNoTracking()
            .Where(d => d.TenantId == tenantId && activeContractIds.Contains(d.ContractId))
            .ToListAsync(ct);

        var schedulesByContract = deprecationSchedules
            .GroupBy(d => d.ContractId)
            .ToDictionary(g => g.Key, g => g.ToList());

        var consumersByApi = allConsumers
            .GroupBy(e => e.ApiAssetId)
            .ToDictionary(g => g.Key, g => g.ToList());

        // Lookup para detectar versão mais nova para o mesmo ApiAsset (HasSuccessorVersion)
        var versionsByApi = activeVersions
            .GroupBy(v => v.ApiAssetId)
            .ToDictionary(g => g.Key, g => g.ToList());

        var result = new List<IContractDeprecationForecastReader.ActiveContractForecastEntry>(activeVersions.Count);

        foreach (var version in activeVersions)
        {
            if (!apiMap.TryGetValue(version.ApiAssetId, out var api))
                continue;

            var teamId = serviceTeamMap.TryGetValue(api.OwnerServiceId, out var team) ? team : null;

            var consumers = consumersByApi.TryGetValue(version.ApiAssetId, out var cList) ? cList : [];
            var currentCount = consumers.Count(e => e.IsActive);
            var prevMonthCount = consumers.Count(e => e.RegisteredAt < minus30);
            var twoMonthsAgoCount = consumers.Count(e => e.RegisteredAt < minus60);

            var schedules = schedulesByContract.TryGetValue(version.Id.Value, out var sList) ? sList : [];
            var ownerSignalled = schedules.Any(s => s.Reason != null);
            var hasSuccessor = schedules.Any(s => s.SuccessorVersionId != null)
                || (versionsByApi.TryGetValue(version.ApiAssetId, out var siblings)
                    && siblings.Any(v => v.Id.Value != version.Id.Value && v.CreatedAt > version.CreatedAt));

            var plannedDeprecations = schedules
                .Select(s => new IContractDeprecationForecastReader.PlannedDeprecationCalendarEntry(
                    ContractId: version.Id.Value,
                    ContractName: api.Name,
                    PlannedDeprecationDate: s.PlannedDeprecationDate,
                    PlannedSunsetDate: s.PlannedSunsetDate,
                    ActiveConsumerCount: currentCount))
                .ToList();

            result.Add(new IContractDeprecationForecastReader.ActiveContractForecastEntry(
                ContractId: version.Id.Value,
                ContractName: api.Name,
                ContractVersion: version.SemVer,
                Protocol: version.Protocol.ToString(),
                OwnerTeamId: teamId,
                CreatedAt: version.CreatedAt,
                HasSuccessorVersion: hasSuccessor,
                CurrentConsumerCount: currentCount,
                ConsumerCountPrevMonth: prevMonthCount,
                ConsumerCountTwoMonthsAgo: twoMonthsAgoCount,
                OwnerSignalledDeprecation: ownerSignalled,
                PlannedDeprecations: plannedDeprecations));
        }

        return result;
    }
}
