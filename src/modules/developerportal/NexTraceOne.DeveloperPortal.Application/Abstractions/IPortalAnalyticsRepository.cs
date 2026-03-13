using NexTraceOne.DeveloperPortal.Domain.Entities;

namespace NexTraceOne.DeveloperPortal.Application.Abstractions;

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
}
