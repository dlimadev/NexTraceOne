using Microsoft.EntityFrameworkCore;
using NexTraceOne.Governance.Application.Abstractions;
using NexTraceOne.Governance.Application.Features.GetDashboardUsageAnalytics;
using NexTraceOne.Governance.Domain.Entities;
using NexTraceOne.Governance.Infrastructure.Persistence;

namespace NexTraceOne.Governance.Infrastructure.Persistence.Repositories;

/// <summary>Repositório EF Core para DashboardUsageEvent e analytics de uso (V3.6).</summary>
public sealed class DashboardUsageRepository(GovernanceDbContext db) : IDashboardUsageRepository
{
    public async Task AddAsync(DashboardUsageEvent evt, CancellationToken ct = default)
        => await db.DashboardUsageEvents.AddAsync(evt, ct);

    public async Task<IReadOnlyList<GetDashboardUsageAnalytics.DashboardUsageSummary>> GetAnalyticsAsync(
        string tenantId,
        Guid? dashboardId,
        DateTimeOffset from,
        DateTimeOffset to,
        CancellationToken ct = default)
    {
        var query = db.DashboardUsageEvents
            .Where(e => e.TenantId == tenantId && e.OccurredAt >= from && e.OccurredAt <= to);

        if (dashboardId.HasValue)
            query = query.Where(e => e.DashboardId == dashboardId.Value);

        var grouped = await query
            .GroupBy(e => e.DashboardId)
            .Select(g => new
            {
                DashboardId = g.Key,
                TotalViews = g.Count(e => e.EventType == "view"),
                UniqueUsers = g.Select(e => e.UserId).Distinct().Count(),
                ExportCount = g.Count(e => e.EventType == "export"),
                EmbedCount = g.Count(e => e.EventType == "embed"),
                AvgDuration = g.Where(e => e.DurationSeconds != null)
                               .Average(e => (double?)e.DurationSeconds) ?? 0.0,
                LastViewedAt = g.Max(e => (DateTimeOffset?)e.OccurredAt)
            })
            .OrderByDescending(x => x.TotalViews)
            .Take(100)
            .ToListAsync(ct);

        // join with dashboard names
        var dashboardIds = grouped.Select(g => g.DashboardId).ToList();
        var dashboardNames = await db.CustomDashboards
            .Where(d => dashboardIds.Contains(d.Id.Value))
            .Select(d => new { Id = d.Id.Value, d.Name })
            .ToDictionaryAsync(d => d.Id, d => d.Name, ct);

        // top persona per dashboard
        var topPersonas = await db.DashboardUsageEvents
            .Where(e => e.TenantId == tenantId && e.OccurredAt >= from && e.OccurredAt <= to
                        && e.Persona != null && dashboardIds.Contains(e.DashboardId))
            .GroupBy(e => new { e.DashboardId, e.Persona })
            .Select(g => new { g.Key.DashboardId, g.Key.Persona, Count = g.Count() })
            .ToListAsync(ct);

        var topPersonaByDashboard = topPersonas
            .GroupBy(x => x.DashboardId)
            .ToDictionary(g => g.Key, g => g.OrderByDescending(x => x.Count).First().Persona ?? "unknown");

        return grouped.Select(g => new GetDashboardUsageAnalytics.DashboardUsageSummary(
            g.DashboardId,
            dashboardNames.GetValueOrDefault(g.DashboardId, "Unknown"),
            g.TotalViews,
            g.UniqueUsers,
            g.ExportCount,
            g.EmbedCount,
            g.AvgDuration,
            g.LastViewedAt,
            topPersonaByDashboard.GetValueOrDefault(g.DashboardId, "unknown")
        )).ToList();
    }
}
