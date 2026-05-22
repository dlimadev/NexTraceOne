using Microsoft.EntityFrameworkCore;

using NexTraceOne.Catalog.Application.Contracts.Abstractions;
using NexTraceOne.Catalog.Infrastructure.Contracts.Persistence;
using NexTraceOne.Catalog.Infrastructure.Graph.Persistence;

namespace NexTraceOne.Catalog.Infrastructure.Readers;

/// <summary>
/// Implementação real de ISchemaEvolutionSafetyReader.
/// Cruza ContractDiff (ContractsDbContext) com ApiAssets e ServiceAssets (CatalogGraphDbContext)
/// para reportar evolução de schema com riscos de breaking changes por equipa.
/// Wave AQ.3 — GetSchemaEvolutionSafetyReport.
/// </summary>
internal sealed class EfSchemaEvolutionSafetyReader(
    ContractsDbContext contractsDb,
    CatalogGraphDbContext graphDb) : ISchemaEvolutionSafetyReader
{
    public async Task<IReadOnlyList<ISchemaEvolutionSafetyReader.TeamSchemaEvolutionEntry>> ListByTenantAsync(
        string tenantId, int lookbackDays, CancellationToken ct)
    {
        if (!Guid.TryParse(tenantId, out var tenantGuid))
            return [];

        var since = DateTimeOffset.UtcNow.AddDays(-lookbackDays);

        var services = await graphDb.ServiceAssets
            .AsNoTracking()
            .Where(s => s.TenantId == tenantGuid)
            .Select(s => new { s.Id, s.TeamName })
            .ToListAsync(ct);

        if (services.Count == 0)
            return [];

        var serviceIds = services.Select(s => s.Id.Value).ToList();
        var teamByServiceId = services.ToDictionary(s => s.Id.Value, s => s.TeamName);

        var apiAssets = await graphDb.ApiAssets
            .AsNoTracking()
            .Where(a => serviceIds.Contains(EF.Property<Guid>(a, "OwnerServiceId")))
            .Select(a => new { a.Id, a.Name, OwnerServiceId = EF.Property<Guid>(a, "OwnerServiceId") })
            .ToListAsync(ct);

        if (apiAssets.Count == 0)
            return [];

        var apiAssetIds = apiAssets.Select(a => a.Id.Value).ToList();
        var teamByApiId = apiAssets.ToDictionary(
            a => a.Id.Value,
            a => teamByServiceId.GetValueOrDefault(a.OwnerServiceId, string.Empty));
        var nameByApiId = apiAssets.ToDictionary(a => a.Id.Value, a => a.Name);

        var diffs = await contractsDb.ContractDiffs
            .AsNoTracking()
            .Where(d => apiAssetIds.Contains(d.ApiAssetId) && d.ComputedAt >= since)
            .ToListAsync(ct);

        if (diffs.Count == 0)
            return [];

        // Agrupa diffs por equipa
        var diffsByTeam = new Dictionary<string, List<(
            string ApiName, string Protocol, bool IsBreaking, DateTimeOffset ComputedAt, Guid ApiAssetId)>>();

        foreach (var diff in diffs)
        {
            var team = teamByApiId.GetValueOrDefault(diff.ApiAssetId, string.Empty);
            if (string.IsNullOrEmpty(team)) continue;

            if (!diffsByTeam.TryGetValue(team, out var list))
                diffsByTeam[team] = list = [];

            list.Add((
                nameByApiId.GetValueOrDefault(diff.ApiAssetId, "Unknown"),
                diff.Protocol.ToString(),
                diff.BreakingChanges.Count > 0,
                diff.ComputedAt,
                diff.ApiAssetId));
        }

        return diffsByTeam
            .Select(kvp =>
            {
                var teamName = kvp.Key;
                var entries = kvp.Value;
                var breaking = entries.Where(e => e.IsBreaking).ToList();

                var protocolBreaking = entries
                    .GroupBy(e => e.Protocol)
                    .Select(g => new ISchemaEvolutionSafetyReader.ProtocolBreakingEntry(
                        Protocol: g.Key,
                        Total: g.Count(),
                        Breaking: g.Count(e => e.IsBreaking)))
                    .ToList();

                var recentHigh = breaking
                    .OrderByDescending(e => e.ComputedAt)
                    .Take(5)
                    .Select(e => new ISchemaEvolutionSafetyReader.HighRiskChange(
                        ContractId: e.ApiAssetId.ToString(),
                        ContractName: e.ApiName,
                        ChangedAt: e.ComputedAt))
                    .ToList();

                return new ISchemaEvolutionSafetyReader.TeamSchemaEvolutionEntry(
                    TeamId: teamName.ToLowerInvariant().Replace(' ', '-'),
                    TeamName: teamName,
                    TotalSchemaChanges: entries.Count,
                    BreakingChanges: breaking.Count,
                    BreakingChangesWithIncidentCorrelation: 0,
                    ConsumerNotifiedBreakingChanges: 0,
                    ProtocolBreaking: protocolBreaking,
                    RecentHighRiskChanges: recentHigh);
            })
            .OrderByDescending(e => e.BreakingChanges)
            .ThenByDescending(e => e.TotalSchemaChanges)
            .ToList();
    }
}
