using Microsoft.EntityFrameworkCore;

using NexTraceOne.Governance.Application.Abstractions;
using NexTraceOne.Governance.Domain.Entities;
using NexTraceOne.Governance.Domain.Enums;

namespace NexTraceOne.Governance.Infrastructure.Persistence.Repositories;

/// <summary>
/// Implementação EF Core do repositório de analytics para Governance Trends.
/// Fornece consultas agregadas para indicadores executivos.
/// </summary>
internal sealed class GovernanceAnalyticsRepository(GovernanceDbContext context) : IGovernanceAnalyticsRepository
{
    public async Task<IReadOnlyList<MonthlyCount>> GetWaiverCountsByMonthAsync(int months, CancellationToken ct)
    {
        var startDate = DateTimeOffset.UtcNow.AddMonths(-months);

        var data = await context.Waivers
            .Where(w => w.RequestedAt >= startDate)
            .GroupBy(w => new { w.RequestedAt.Year, w.RequestedAt.Month })
            .Select(g => new
            {
                Year = g.Key.Year,
                Month = g.Key.Month,
                Count = g.Count()
            })
            .OrderBy(x => x.Year).ThenBy(x => x.Month)
            .ToListAsync(ct);

        return data.Select(x => new MonthlyCount(
            Period: $"{x.Year}-{x.Month:D2}",
            Count: x.Count)).ToList();
    }

    public async Task<IReadOnlyList<MonthlyCount>> GetPublishedPackCountsByMonthAsync(int months, CancellationToken ct)
    {
        var startDate = DateTimeOffset.UtcNow.AddMonths(-months);

        var data = await context.Packs
            .Where(p => p.Status == GovernancePackStatus.Published && p.UpdatedAt >= startDate)
            .GroupBy(p => new { p.UpdatedAt.Year, p.UpdatedAt.Month })
            .Select(g => new
            {
                Year = g.Key.Year,
                Month = g.Key.Month,
                Count = g.Count()
            })
            .OrderBy(x => x.Year).ThenBy(x => x.Month)
            .ToListAsync(ct);

        return data.Select(x => new MonthlyCount(
            Period: $"{x.Year}-{x.Month:D2}",
            Count: x.Count)).ToList();
    }

    public async Task<IReadOnlyList<MonthlyCount>> GetRolloutCountsByMonthAsync(int months, CancellationToken ct)
    {
        var startDate = DateTimeOffset.UtcNow.AddMonths(-months);

        var data = await context.RolloutRecords
            .Where(r => r.InitiatedAt >= startDate)
            .GroupBy(r => new { r.InitiatedAt.Year, r.InitiatedAt.Month })
            .Select(g => new
            {
                Year = g.Key.Year,
                Month = g.Key.Month,
                Count = g.Count()
            })
            .OrderBy(x => x.Year).ThenBy(x => x.Month)
            .ToListAsync(ct);

        return data.Select(x => new MonthlyCount(
            Period: $"{x.Year}-{x.Month:D2}",
            Count: x.Count)).ToList();
    }

    public async Task<int> GetPendingWaiverCountAsync(CancellationToken ct)
        => await context.Waivers.CountAsync(w => w.Status == WaiverStatus.Pending, ct);

    public async Task<int> GetPublishedPackCountAsync(CancellationToken ct)
        => await context.Packs.CountAsync(p => p.Status == GovernancePackStatus.Published, ct);
}
