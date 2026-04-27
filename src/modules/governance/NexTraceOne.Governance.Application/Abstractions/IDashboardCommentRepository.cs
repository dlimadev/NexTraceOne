using NexTraceOne.Governance.Domain.Entities;

namespace NexTraceOne.Governance.Application.Abstractions;

/// <summary>Repositório para DashboardComment (V3.7 — Real-time Collaboration).</summary>
public interface IDashboardCommentRepository
{
    Task<DashboardComment?> GetByIdAsync(DashboardCommentId id, string tenantId, CancellationToken ct = default);
    Task<IReadOnlyList<DashboardComment>> ListAsync(Guid dashboardId, string tenantId, string? widgetId, bool includeResolved, CancellationToken ct = default);
    Task AddAsync(DashboardComment comment, CancellationToken ct = default);
    Task UpdateAsync(DashboardComment comment, CancellationToken ct = default);
    Task DeleteAsync(DashboardCommentId id, string tenantId, CancellationToken ct = default);
}
