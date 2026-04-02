using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using NexTraceOne.OperationalIntelligence.Contracts.Incidents.ServiceInterfaces;
using NexTraceOne.OperationalIntelligence.Domain.Incidents.Enums;
using NexTraceOne.OperationalIntelligence.Infrastructure.Incidents.Persistence;

namespace NexTraceOne.OperationalIntelligence.Infrastructure.Incidents.Services;

/// <summary>
/// Implementação do contrato público do módulo Incidents.
/// Usa IncidentDbContext diretamente para consultas de leitura otimizadas.
/// Outros módulos consomem este serviço via IIncidentModule — nunca acessam o DbContext.
/// </summary>
internal sealed class IncidentModuleService(
    IncidentDbContext context,
    ILogger<IncidentModuleService> logger) : IIncidentModule
{
    /// <inheritdoc />
    public async Task<int> CountOpenIncidentsAsync(CancellationToken cancellationToken = default)
    {
        logger.LogDebug("Counting open incidents");

        return await context.Incidents
            .AsNoTracking()
            .CountAsync(
                i => i.Status != IncidentStatus.Resolved && i.Status != IncidentStatus.Closed,
                cancellationToken);
    }

    /// <inheritdoc />
    public async Task<int> CountResolvedInLastDaysAsync(int days = 30, CancellationToken cancellationToken = default)
    {
        logger.LogDebug("Counting incidents resolved in last {Days} days", days);

        var since = DateTimeOffset.UtcNow.AddDays(-days);

        return await context.Incidents
            .AsNoTracking()
            .CountAsync(
                i => (i.Status == IncidentStatus.Resolved || i.Status == IncidentStatus.Closed)
                     && i.LastUpdatedAt >= since,
                cancellationToken);
    }

    /// <inheritdoc />
    public async Task<decimal> GetAverageResolutionHoursAsync(int days = 30, CancellationToken cancellationToken = default)
    {
        logger.LogDebug("Calculating average resolution hours for last {Days} days", days);

        var since = DateTimeOffset.UtcNow.AddDays(-days);

        var resolvedIncidents = await context.Incidents
            .AsNoTracking()
            .Where(i => (i.Status == IncidentStatus.Resolved || i.Status == IncidentStatus.Closed)
                        && i.LastUpdatedAt >= since)
            .Select(i => new { i.DetectedAt, i.LastUpdatedAt })
            .ToListAsync(cancellationToken);

        if (resolvedIncidents.Count == 0)
            return 0m;

        var totalHours = resolvedIncidents
            .Sum(i => (i.LastUpdatedAt - i.DetectedAt).TotalHours);

        return Math.Round((decimal)(totalHours / resolvedIncidents.Count), 1);
    }

    /// <inheritdoc />
    public async Task<decimal> GetRecurrenceRateAsync(int days = 30, CancellationToken cancellationToken = default)
    {
        logger.LogDebug("Calculating recurrence rate for last {Days} days", days);

        var since = DateTimeOffset.UtcNow.AddDays(-days);

        var recentIncidents = await context.Incidents
            .AsNoTracking()
            .Where(i => i.DetectedAt >= since)
            .Select(i => i.ServiceId)
            .ToListAsync(cancellationToken);

        if (recentIncidents.Count == 0)
            return 0m;

        var uniqueServices = recentIncidents.Distinct().Count();
        var recurringServices = recentIncidents
            .GroupBy(s => s)
            .Count(g => g.Count() > 1);

        return uniqueServices == 0
            ? 0m
            : Math.Round((decimal)recurringServices / uniqueServices * 100m, 1);
    }

    /// <inheritdoc />
    public async Task<IncidentTrendSummary> GetTrendSummaryAsync(int days = 30, CancellationToken cancellationToken = default)
    {
        logger.LogDebug("Getting incident trend summary for last {Days} days", days);

        var openCount = await CountOpenIncidentsAsync(cancellationToken);
        var resolvedCount = await CountResolvedInLastDaysAsync(days, cancellationToken);
        var avgHours = await GetAverageResolutionHoursAsync(days, cancellationToken);
        var recurrenceRate = await GetRecurrenceRateAsync(days, cancellationToken);

        // Trend: improving if resolved > open, declining if open > resolved * 2
        var trend = resolvedCount > openCount ? "Improving"
            : openCount > resolvedCount * 2 ? "Declining"
            : "Stable";

        return new IncidentTrendSummary(
            OpenIncidents: openCount,
            ResolvedInPeriod: resolvedCount,
            AvgResolutionHours: avgHours,
            RecurrenceRate: recurrenceRate,
            Trend: trend);
    }
}
