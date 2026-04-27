using NexTraceOne.Governance.Domain.Entities;

namespace NexTraceOne.Governance.Application.Abstractions;

public interface IDashboardMonitorRepository
{
    Task AddAsync(DashboardMonitorDefinition monitor, CancellationToken ct = default);
    Task<DashboardMonitorDefinition?> GetByIdAsync(Guid id, string tenantId, CancellationToken ct = default);
    Task<IReadOnlyList<DashboardMonitorDefinition>> ListByDashboardAsync(Guid dashboardId, string tenantId, CancellationToken ct = default);
    Task<IReadOnlyList<DashboardMonitorDefinition>> ListActiveAsync(string tenantId, CancellationToken ct = default);
    Task SaveAsync(DashboardMonitorDefinition monitor, CancellationToken ct = default);
}
