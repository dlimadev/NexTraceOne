using Microsoft.EntityFrameworkCore;

using NexTraceOne.ProductAnalytics.Application.Abstractions;
using NexTraceOne.ProductAnalytics.Domain.Entities;
using NexTraceOne.ProductAnalytics.Domain.Enums;

namespace NexTraceOne.ProductAnalytics.Infrastructure.Persistence.Repositories;

/// <summary>
/// Implementação EF Core do repositório de AnalyticsEvents para o módulo Product Analytics.
/// Extraído de GovernanceDbContext em P2.3.
/// </summary>
internal sealed class AnalyticsEventRepository(ProductAnalyticsDbContext context) : IAnalyticsEventRepository
{
    public async Task AddAsync(AnalyticsEvent analyticsEvent, CancellationToken ct)
        => await context.AnalyticsEvents.AddAsync(analyticsEvent, ct);

    public async Task<long> CountAsync(
        string? persona,
        ProductModule? module,
        string? teamId,
        string? domainId,
        DateTimeOffset from,
        DateTimeOffset to,
        CancellationToken ct)
        => await ApplyFilters(context.AnalyticsEvents.AsNoTracking(), persona, module, teamId, domainId)
            .Where(e => e.OccurredAt >= from && e.OccurredAt <= to)
            .LongCountAsync(ct);

    public async Task<long> CountByEventTypeAsync(
        AnalyticsEventType eventType,
        string? persona,
        DateTimeOffset from,
        DateTimeOffset to,
        CancellationToken ct)
        => await ApplyFilters(context.AnalyticsEvents.AsNoTracking(), persona, module: null, teamId: null, domainId: null)
            .Where(e => e.EventType == eventType && e.OccurredAt >= from && e.OccurredAt <= to)
            .LongCountAsync(ct);

    public async Task<int> CountUniqueUsersAsync(
        string? persona,
        ProductModule? module,
        string? teamId,
        string? domainId,
        DateTimeOffset from,
        DateTimeOffset to,
        CancellationToken ct)
        => await ApplyFilters(context.AnalyticsEvents.AsNoTracking(), persona, module, teamId, domainId)
            .Where(e => e.OccurredAt >= from && e.OccurredAt <= to && e.UserId != null)
            .Select(e => e.UserId!)
            .Distinct()
            .CountAsync(ct);

    public async Task<int> CountActivePersonasAsync(
        string? module,
        string? teamId,
        string? domainId,
        DateTimeOffset from,
        DateTimeOffset to,
        CancellationToken ct)
    {
        ProductModule? parsedModule = null;
        if (!string.IsNullOrWhiteSpace(module) && Enum.TryParse<ProductModule>(module, ignoreCase: true, out var moduleValue))
        {
            parsedModule = moduleValue;
        }

        return await ApplyFilters(context.AnalyticsEvents.AsNoTracking(), persona: null, parsedModule, teamId, domainId)
            .Where(e => e.OccurredAt >= from && e.OccurredAt <= to && e.Persona != null)
            .Select(e => e.Persona!)
            .Distinct()
            .CountAsync(ct);
    }

    public async Task<IReadOnlyList<ModuleUsageRow>> GetTopModulesAsync(
        string? persona,
        string? teamId,
        string? domainId,
        DateTimeOffset from,
        DateTimeOffset to,
        int top,
        CancellationToken ct)
        => await ApplyFilters(context.AnalyticsEvents.AsNoTracking(), persona, module: null, teamId, domainId)
            .Where(e => e.OccurredAt >= from && e.OccurredAt <= to)
            .GroupBy(e => e.Module)
            .Select(g => new ModuleUsageRow(
                g.Key,
                EventCount: g.LongCount(),
                UniqueUsers: g.Where(x => x.UserId != null).Select(x => x.UserId!).Distinct().Count()))
            .OrderByDescending(x => x.EventCount)
            .Take(top)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<ModuleAdoptionRow>> GetModuleAdoptionAsync(
        string? persona,
        string? teamId,
        DateTimeOffset from,
        DateTimeOffset to,
        CancellationToken ct)
        => await ApplyFilters(context.AnalyticsEvents.AsNoTracking(), persona, module: null, teamId, domainId: null)
            .Where(e => e.OccurredAt >= from && e.OccurredAt <= to)
            .GroupBy(e => e.Module)
            .Select(g => new ModuleAdoptionRow(
                g.Key,
                TotalActions: g.LongCount(),
                UniqueUsers: g.Where(x => x.UserId != null).Select(x => x.UserId!).Distinct().Count()))
            .OrderByDescending(x => x.TotalActions)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<ModuleFeatureCountRow>> GetFeatureCountsAsync(
        string? persona,
        string? teamId,
        DateTimeOffset from,
        DateTimeOffset to,
        CancellationToken ct)
        => await ApplyFilters(context.AnalyticsEvents.AsNoTracking(), persona, module: null, teamId, domainId: null)
            .Where(e => e.OccurredAt >= from && e.OccurredAt <= to && e.Feature != null)
            .GroupBy(e => new { e.Module, e.Feature })
            .Select(g => new ModuleFeatureCountRow(g.Key.Module, g.Key.Feature!, g.LongCount()))
            .ToListAsync(ct);

    public async Task<IReadOnlyList<SessionEventRow>> ListSessionEventsAsync(
        string? persona,
        string? teamId,
        DateTimeOffset from,
        DateTimeOffset to,
        CancellationToken ct)
        => await ApplyFilters(context.AnalyticsEvents.AsNoTracking(), persona, module: null, teamId, domainId: null)
            .Where(e => e.OccurredAt >= from && e.OccurredAt <= to && e.SessionId != null)
            .Select(e => new SessionEventRow(e.SessionId!, e.EventType, e.OccurredAt))
            .ToListAsync(ct);

    private static IQueryable<AnalyticsEvent> ApplyFilters(
        IQueryable<AnalyticsEvent> query,
        string? persona,
        ProductModule? module,
        string? teamId,
        string? domainId)
    {
        if (!string.IsNullOrWhiteSpace(persona))
            query = query.Where(e => e.Persona == persona);

        if (module.HasValue)
            query = query.Where(e => e.Module == module.Value);

        if (!string.IsNullOrWhiteSpace(teamId))
            query = query.Where(e => e.TeamId == teamId);

        if (!string.IsNullOrWhiteSpace(domainId))
            query = query.Where(e => e.DomainId == domainId);

        return query;
    }
}
