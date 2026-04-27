using NexTraceOne.Governance.Application.Features.GetDashboardUsageAnalytics;
using NexTraceOne.Governance.Domain.Entities;

namespace NexTraceOne.Governance.Application.Abstractions;

/// <summary>
/// Repositório para <see cref="DashboardUsageEvent"/> e analytics de uso (V3.6).
/// </summary>
public interface IDashboardUsageRepository
{
    Task AddAsync(DashboardUsageEvent evt, CancellationToken ct = default);

    Task<IReadOnlyList<GetDashboardUsageAnalytics.DashboardUsageSummary>> GetAnalyticsAsync(
        string tenantId,
        Guid? dashboardId,
        DateTimeOffset from,
        DateTimeOffset to,
        CancellationToken ct = default);
}
