using Microsoft.EntityFrameworkCore;
using NexTraceOne.Governance.Application.Abstractions;
using NexTraceOne.Governance.Domain.Entities;

namespace NexTraceOne.Governance.Infrastructure.Persistence.Repositories;

internal sealed class WidgetSnapshotRepository(GovernanceDbContext context) : IWidgetSnapshotRepository
{
    public Task<WidgetSnapshot?> GetLatestBeforeAsync(
        string tenantId, Guid dashboardId, string widgetId, DateTimeOffset before, CancellationToken ct)
        => context.WidgetSnapshots
            .Where(s => s.TenantId == tenantId && s.DashboardId == dashboardId
                     && s.WidgetId == widgetId && s.CapturedAt < before)
            .OrderByDescending(s => s.CapturedAt)
            .FirstOrDefaultAsync(ct);

    public async Task<IReadOnlyList<WidgetSnapshot>> ListSinceAsync(
        string tenantId, Guid dashboardId, string widgetId, DateTimeOffset since, CancellationToken ct)
        => await context.WidgetSnapshots
            .Where(s => s.TenantId == tenantId && s.DashboardId == dashboardId
                     && s.WidgetId == widgetId && s.CapturedAt >= since)
            .OrderBy(s => s.CapturedAt)
            .ToListAsync(ct);

    public async Task AddAsync(WidgetSnapshot snapshot, CancellationToken ct)
        => await context.WidgetSnapshots.AddAsync(snapshot, ct);
}
