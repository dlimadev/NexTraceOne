using NexTraceOne.Catalog.Domain.Portal.Entities;

namespace NexTraceOne.Catalog.Application.Portal.Abstractions;

/// <summary>
/// Repositório de eventos de analytics do portal.
/// Permite rastrear métricas de adoção e uso do Developer Portal.
/// </summary>
public interface IPortalAnalyticsRepository
{
    void Add(PortalAnalyticsEvent analyticsEvent);
    Task<IReadOnlyList<PortalAnalyticsEvent>> GetByTypeAsync(string eventType, int page, int pageSize, CancellationToken ct = default);
    Task<int> CountByTypeAsync(string eventType, DateTimeOffset since, CancellationToken ct = default);
    Task<IReadOnlyList<PortalAnalyticsEvent>> GetTopSearchesAsync(int top, DateTimeOffset since, CancellationToken ct = default);
    Task<int> CountByApiAssetAsync(Guid apiAssetId, DateTimeOffset since, CancellationToken ct = default);
    Task<IReadOnlyList<(Guid ApiAssetId, int Count)>> GetTopApisByViewsAsync(int top, DateTimeOffset since, CancellationToken ct = default);
    Task<int> CountDistinctConsumersByApiAsync(Guid apiAssetId, DateTimeOffset since, CancellationToken ct = default);
}
