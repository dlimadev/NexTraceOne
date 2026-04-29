using Microsoft.EntityFrameworkCore;
using NexTraceOne.Governance.Application.Abstractions;
using NexTraceOne.Governance.Domain.Entities;
using NexTraceOne.Governance.Infrastructure.Persistence;

namespace NexTraceOne.Governance.Infrastructure.Persistence.Repositories;

/// <summary>Repositório EF Core para ScheduledDashboardReport (V3.6).</summary>
public sealed class ScheduledDashboardReportRepository(GovernanceDbContext db)
    : IScheduledDashboardReportRepository
{
    public async Task<ScheduledDashboardReport?> GetByIdAsync(
        ScheduledDashboardReportId id, string tenantId, CancellationToken ct = default)
        => await db.ScheduledDashboardReports
            .FirstOrDefaultAsync(r => r.Id == id && r.TenantId == tenantId, ct);

    public async Task<IReadOnlyList<ScheduledDashboardReport>> ListByDashboardAsync(
        Guid dashboardId, string tenantId, CancellationToken ct = default)
        => await db.ScheduledDashboardReports
            .Where(r => r.DashboardId == dashboardId && r.TenantId == tenantId)
            .OrderBy(r => r.CreatedAt)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<ScheduledDashboardReport>> ListByTenantAsync(
        string tenantId, CancellationToken ct = default)
        => await db.ScheduledDashboardReports
            .Where(r => r.TenantId == tenantId)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<ScheduledDashboardReport>> ListDueAsync(
        DateTimeOffset asOf, CancellationToken ct = default)
        => await db.ScheduledDashboardReports
            .Where(r => r.IsActive && (r.NextRunAt == null || r.NextRunAt <= asOf))
            .ToListAsync(ct);

    public async Task AddAsync(ScheduledDashboardReport report, CancellationToken ct = default)
        => await db.ScheduledDashboardReports.AddAsync(report, ct);

    public Task UpdateAsync(ScheduledDashboardReport report, CancellationToken ct = default)
    {
        db.ScheduledDashboardReports.Update(report);
        return Task.CompletedTask;
    }

    public async Task DeleteAsync(ScheduledDashboardReportId id, string tenantId, CancellationToken ct = default)
    {
        var entity = await GetByIdAsync(id, tenantId, ct);
        if (entity is not null)
            db.ScheduledDashboardReports.Remove(entity);
    }
}
