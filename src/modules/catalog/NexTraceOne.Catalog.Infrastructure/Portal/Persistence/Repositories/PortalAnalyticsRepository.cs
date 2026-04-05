using Microsoft.EntityFrameworkCore;

using NexTraceOne.Catalog.Application.Portal.Abstractions;
using NexTraceOne.Catalog.Domain.Portal.Entities;

namespace NexTraceOne.Catalog.Infrastructure.Portal.Persistence.Repositories;

/// <summary>
/// Repositório de eventos de analytics do portal, implementando persistência via EF Core.
/// Suporta agregação por tipo de evento e consulta de pesquisas mais frequentes.
/// </summary>
internal sealed class PortalAnalyticsRepository(DeveloperPortalDbContext context) : IPortalAnalyticsRepository
{
    /// <summary>Adiciona novo evento de analytics ao contexto.</summary>
    public void Add(PortalAnalyticsEvent analyticsEvent)
        => context.PortalAnalyticsEvents.Add(analyticsEvent);

    /// <summary>Lista eventos de um tipo específico com paginação.</summary>
    public async Task<IReadOnlyList<PortalAnalyticsEvent>> GetByTypeAsync(string eventType, int page, int pageSize, CancellationToken ct = default)
        => await context.PortalAnalyticsEvents
            .Where(e => e.EventType == eventType)
            .OrderByDescending(e => e.OccurredAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

    /// <summary>Conta eventos de um tipo desde uma data específica para métricas de dashboard.</summary>
    public async Task<int> CountByTypeAsync(string eventType, DateTimeOffset since, CancellationToken ct = default)
        => await context.PortalAnalyticsEvents
            .CountAsync(e => e.EventType == eventType && e.OccurredAt >= since, ct);

    /// <summary>Retorna as pesquisas mais frequentes no período, agrupando por query e ordenando por contagem.</summary>
    public async Task<IReadOnlyList<PortalAnalyticsEvent>> GetTopSearchesAsync(int top, DateTimeOffset since, CancellationToken ct = default)
        => await context.PortalAnalyticsEvents
            .Where(e => e.EventType == "Search" && e.OccurredAt >= since && e.SearchQuery != null)
            .GroupBy(e => e.SearchQuery)
            .OrderByDescending(g => g.Count())
            .Take(top)
            .Select(g => g.First())
            .ToListAsync(ct);

    /// <summary>Conta eventos de visualização de uma API específica desde uma data.</summary>
    public async Task<int> CountByApiAssetAsync(Guid apiAssetId, DateTimeOffset since, CancellationToken ct = default)
        => await context.PortalAnalyticsEvents
            .CountAsync(e => e.EntityId == apiAssetId.ToString() && e.OccurredAt >= since, ct);

    /// <summary>Retorna as APIs mais visualizadas no período, agrupadas por EntityId.</summary>
    public async Task<IReadOnlyList<(Guid ApiAssetId, int Count)>> GetTopApisByViewsAsync(int top, DateTimeOffset since, CancellationToken ct = default)
    {
        var groups = await context.PortalAnalyticsEvents
            .Where(e => e.EventType == "ApiView" && e.OccurredAt >= since && e.EntityId != null)
            .GroupBy(e => e.EntityId!)
            .OrderByDescending(g => g.Count())
            .Take(top)
            .Select(g => new { ApiAssetIdStr = g.Key, Count = g.Count() })
            .ToListAsync(ct);

        return groups
            .Select(g => (Parsed: Guid.TryParse(g.ApiAssetIdStr, out var id) ? (Guid?)id : null, g.Count))
            .Where(x => x.Parsed.HasValue)
            .Select(x => (x.Parsed!.Value, x.Count))
            .ToList();
    }

    /// <summary>Conta consumidores distintos de uma API no período.</summary>
    public async Task<int> CountDistinctConsumersByApiAsync(Guid apiAssetId, DateTimeOffset since, CancellationToken ct = default)
        => await context.PortalAnalyticsEvents
            .Where(e => e.EntityId == apiAssetId.ToString() && e.OccurredAt >= since && e.UserId != null)
            .Select(e => e.UserId)
            .Distinct()
            .CountAsync(ct);
}
