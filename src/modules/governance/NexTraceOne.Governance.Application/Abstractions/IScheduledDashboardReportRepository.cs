using NexTraceOne.Governance.Domain.Entities;

namespace NexTraceOne.Governance.Application.Abstractions;

/// <summary>
/// Repositório para <see cref="ScheduledDashboardReport"/> (V3.6).
/// </summary>
public interface IScheduledDashboardReportRepository
{
    Task<ScheduledDashboardReport?> GetByIdAsync(ScheduledDashboardReportId id, string tenantId, CancellationToken ct = default);
    Task<IReadOnlyList<ScheduledDashboardReport>> ListByDashboardAsync(Guid dashboardId, string tenantId, CancellationToken ct = default);
    Task<IReadOnlyList<ScheduledDashboardReport>> ListByTenantAsync(string tenantId, CancellationToken ct = default);
    Task<IReadOnlyList<ScheduledDashboardReport>> ListDueAsync(DateTimeOffset asOf, CancellationToken ct = default);
    Task AddAsync(ScheduledDashboardReport report, CancellationToken ct = default);
    Task UpdateAsync(ScheduledDashboardReport report, CancellationToken ct = default);
    Task DeleteAsync(ScheduledDashboardReportId id, string tenantId, CancellationToken ct = default);
}
