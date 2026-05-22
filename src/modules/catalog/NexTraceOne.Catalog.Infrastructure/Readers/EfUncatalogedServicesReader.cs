using Microsoft.EntityFrameworkCore;
using NexTraceOne.Catalog.Application.Services.Abstractions;
using NexTraceOne.Catalog.Infrastructure.Graph.Persistence;

namespace NexTraceOne.Catalog.Infrastructure.Readers;

/// <summary>
/// Implementação real de <see cref="IUncatalogedServicesReader"/>.
/// Cruza DiscoveredServices (sem MatchedServiceAssetId) com ServiceAssets para detectar
/// serviços observados em telemetria que ainda não foram registados no catálogo.
/// Substitui o NullUncatalogedServicesReader (honest-null pattern).
/// Wave AM.1 — GetUncatalogedServicesReport.
/// </summary>
internal sealed class EfUncatalogedServicesReader(CatalogGraphDbContext graphDb) : IUncatalogedServicesReader
{
    public async Task<IUncatalogedServicesReader.UncatalogedServicesSummary> GetSummaryAsync(
        string tenantId,
        int lookbackDays,
        CancellationToken ct)
    {
        if (!Guid.TryParse(tenantId, out var tenantGuid))
            return new IUncatalogedServicesReader.UncatalogedServicesSummary(0, []);

        var catalogedCount = await graphDb.ServiceAssets
            .AsNoTracking()
            .CountAsync(s => s.TenantId == tenantGuid, ct);

        var cutoff = DateTimeOffset.UtcNow.AddDays(-lookbackDays);

        // DiscoveredService não tem TenantId directo — filtramos por MatchedServiceAssetId IS NULL
        // e LastSeenAt dentro do período. Agrupamos por ServiceName para deduplicar.
        var discovered = await graphDb.DiscoveredServices
            .AsNoTracking()
            .Where(d => d.MatchedServiceAssetId == null && d.LastSeenAt >= cutoff)
            .ToListAsync(ct);

        var grouped = discovered
            .GroupBy(d => d.ServiceName, StringComparer.OrdinalIgnoreCase)
            .Select(g =>
            {
                var latest = g.OrderByDescending(d => d.LastSeenAt).First();
                var earliest = g.OrderBy(d => d.FirstSeenAt).First();

                var envs = g.Select(d => d.Environment)
                    .Where(e => !string.IsNullOrEmpty(e))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();

                var dailyCallCount = lookbackDays > 0
                    ? (int)(g.Sum(d => d.TraceCount) / lookbackDays)
                    : 0;

                return new IUncatalogedServicesReader.UncatalogedServiceEntry(
                    ServiceName: latest.ServiceName,
                    FirstSeen: earliest.FirstSeenAt,
                    LastSeen: latest.LastSeenAt,
                    DailyCallCount: dailyCallCount,
                    ObservedEnvironments: envs,
                    PossibleOwner: null);
            })
            .OrderByDescending(e => e.LastSeen)
            .ToList();

        return new IUncatalogedServicesReader.UncatalogedServicesSummary(
            CatalogedServiceCount: catalogedCount,
            UncatalogedServices: grouped);
    }
}
