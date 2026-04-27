using Microsoft.EntityFrameworkCore;
using NexTraceOne.Governance.Application.Abstractions;
using NexTraceOne.Governance.Domain.Entities;

namespace NexTraceOne.Governance.Infrastructure.Persistence.Repositories;

public sealed class DashboardMonitorRepository(GovernanceDbContext context) : IDashboardMonitorRepository
{
    public async Task AddAsync(DashboardMonitorDefinition monitor, CancellationToken ct = default)
        => await context.DashboardMonitors.AddAsync(monitor, ct);

    public async Task<DashboardMonitorDefinition?> GetByIdAsync(Guid id, string tenantId, CancellationToken ct = default)
        => await context.DashboardMonitors
            .Where(m => m.Id == new DashboardMonitorDefinitionId(id) && m.TenantId == tenantId)
            .FirstOrDefaultAsync(ct);

    public async Task<IReadOnlyList<DashboardMonitorDefinition>> ListByDashboardAsync(Guid dashboardId, string tenantId, CancellationToken ct = default)
        => await context.DashboardMonitors
            .Where(m => m.DashboardId == dashboardId && m.TenantId == tenantId && m.Status != MonitorStatus.Deleted)
            .OrderBy(m => m.Name)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<DashboardMonitorDefinition>> ListActiveAsync(string tenantId, CancellationToken ct = default)
        => await context.DashboardMonitors
            .Where(m => m.TenantId == tenantId && m.Status == MonitorStatus.Active)
            .ToListAsync(ct);

    public Task SaveAsync(DashboardMonitorDefinition monitor, CancellationToken ct = default)
    {
        context.DashboardMonitors.Update(monitor);
        return Task.CompletedTask;
    }
}
