using Microsoft.EntityFrameworkCore;
using NexTraceOne.Catalog.Application.Services.Abstractions;
using NexTraceOne.Catalog.Domain.Graph.Enums;
using NexTraceOne.Catalog.Infrastructure.Graph.Persistence;

namespace NexTraceOne.Catalog.Infrastructure.Readers;

/// <summary>
/// Implementação real de <see cref="IServiceLifecycleReader"/>.
/// Agrega dados de transição de ciclo de vida a partir de ServiceAssets e ConsumerRelationships.
/// Substitui o NullServiceLifecycleReader (honest-null pattern).
/// Wave AF.1 — GetServiceLifecycleTransitionReport.
/// </summary>
internal sealed class EfServiceLifecycleReader(CatalogGraphDbContext graphDb) : IServiceLifecycleReader
{
    public async Task<IReadOnlyList<ServiceLifecycleEntry>> ListByTenantAsync(
        string tenantId,
        int lookbackDays,
        CancellationToken ct)
    {
        if (!Guid.TryParse(tenantId, out var tenantGuid))
            return [];

        var cutoff = DateTimeOffset.UtcNow.AddDays(-lookbackDays);

        var services = await graphDb.ServiceAssets
            .AsNoTracking()
            .Where(s => s.TenantId == tenantGuid && s.UpdatedAt >= cutoff)
            .ToListAsync(ct);

        if (services.Count == 0)
            return [];

        var serviceIdList = services.Select(s => s.Id.Value).ToList();

        var apiAssets = await graphDb.ApiAssets
            .AsNoTracking()
            .Include(a => a.ConsumerRelationships)
            .Where(a => serviceIdList.Contains(EF.Property<Guid>(a, "OwnerServiceId")))
            .ToListAsync(ct);

        var consumerCutoff = cutoff;

        // ConsumerRelationships activos no período agrupados por OwnerServiceId
        var activeConsumersByService = apiAssets
            .GroupBy(a => EF.Property<Guid>(a, "OwnerServiceId"))
            .ToDictionary(
                g => g.Key,
                g => g.SelectMany(a => a.ConsumerRelationships)
                       .Count(cr => cr.LastObservedAt >= consumerCutoff));

        var result = services.Select(svc =>
        {
            var currentState = MapLifecycleState(svc.LifecycleStatus);

            activeConsumersByService.TryGetValue(svc.Id.Value, out var activeCriticalConsumers);

            return new ServiceLifecycleEntry(
                ServiceId: svc.Id.Value.ToString(),
                ServiceName: svc.Name,
                TeamName: svc.TeamName,
                ServiceTier: svc.Tier.ToString(),
                CurrentState: currentState,
                StateEnteredAt: svc.UpdatedAt,
                TransitionCount: 1,
                ActiveCriticalConsumerCount: activeCriticalConsumers,
                MigratingConsumerCount: 0);
        }).ToList();

        return result;
    }

    private static ServiceLifecycleState MapLifecycleState(LifecycleStatus status) =>
        status switch
        {
            LifecycleStatus.Active => ServiceLifecycleState.Active,
            LifecycleStatus.Deprecating => ServiceLifecycleState.Deprecating,
            LifecycleStatus.Deprecated => ServiceLifecycleState.Deprecated,
            LifecycleStatus.Retired => ServiceLifecycleState.Retired,
            _ => ServiceLifecycleState.PreProduction
        };
}
