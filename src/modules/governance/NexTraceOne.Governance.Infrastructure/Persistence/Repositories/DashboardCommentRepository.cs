using Microsoft.EntityFrameworkCore;
using NexTraceOne.Governance.Application.Abstractions;
using NexTraceOne.Governance.Domain.Entities;

namespace NexTraceOne.Governance.Infrastructure.Persistence.Repositories;

/// <summary>Repositório EF Core para DashboardComment (V3.7).</summary>
public sealed class DashboardCommentRepository(GovernanceDbContext db) : IDashboardCommentRepository
{
    public async Task<DashboardComment?> GetByIdAsync(DashboardCommentId id, string tenantId, CancellationToken ct = default)
        => await db.DashboardComments.FirstOrDefaultAsync(c => c.Id == id && c.TenantId == tenantId, ct);

    public async Task<IReadOnlyList<DashboardComment>> ListAsync(
        Guid dashboardId, string tenantId, string? widgetId, bool includeResolved, CancellationToken ct = default)
    {
        var query = db.DashboardComments
            .Where(c => c.DashboardId == dashboardId && c.TenantId == tenantId);

        if (widgetId is not null)
            query = query.Where(c => c.WidgetId == widgetId);

        if (!includeResolved)
            query = query.Where(c => !c.IsResolved);

        return await query.OrderBy(c => c.CreatedAt).ToListAsync(ct);
    }

    public async Task AddAsync(DashboardComment comment, CancellationToken ct = default)
        => await db.DashboardComments.AddAsync(comment, ct);

    public Task UpdateAsync(DashboardComment comment, CancellationToken ct = default)
    {
        db.DashboardComments.Update(comment);
        return Task.CompletedTask;
    }

    public async Task DeleteAsync(DashboardCommentId id, string tenantId, CancellationToken ct = default)
    {
        var entity = await GetByIdAsync(id, tenantId, ct);
        if (entity is not null)
            db.DashboardComments.Remove(entity);
    }
}
