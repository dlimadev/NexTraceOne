using Microsoft.EntityFrameworkCore;
using NexTraceOne.OperationalIntelligence.Application.Incidents.Abstractions;
using NexTraceOne.OperationalIntelligence.Application.Incidents.Features.ListIncidents;
using NexTraceOne.OperationalIntelligence.Infrastructure.Incidents.Persistence;

namespace NexTraceOne.OperationalIntelligence.Infrastructure.Incidents;

/// <summary>
/// Implementação stub da superfície de consulta contextual de incidentes para a IA.
/// Filtra incidentes por TenantId e, opcionalmente, por EnvironmentId.
/// Toda consulta é tenant-isolated por design.
/// </summary>
internal sealed class IncidentContextSurface(IncidentDbContext db) : IIncidentContextSurface
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<ListIncidents.IncidentListItem>> ListByContextAsync(
        Guid tenantId,
        Guid? environmentId,
        DateTimeOffset? from,
        DateTimeOffset? to,
        CancellationToken cancellationToken = default)
    {
        var query = db.Incidents
            .AsNoTracking()
            .Where(i => i.TenantId == tenantId);

        if (environmentId.HasValue)
            query = query.Where(i => i.EnvironmentId == environmentId.Value);

        if (from.HasValue)
            query = query.Where(i => i.DetectedAt >= from.Value);

        if (to.HasValue)
            query = query.Where(i => i.DetectedAt <= to.Value);

        return await query
            .OrderByDescending(i => i.DetectedAt)
            .Select(i => new ListIncidents.IncidentListItem(
                i.Id.Value,
                i.ExternalRef,
                i.Title,
                i.Type,
                i.Severity,
                i.Status,
                i.ServiceId,
                i.ServiceName,
                i.OwnerTeam,
                i.Environment,
                i.DetectedAt,
                i.HasCorrelation,
                i.CorrelationConfidence,
                i.MitigationStatus))
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyDictionary<string, int>> GetSeverityCountByContextAsync(
        Guid tenantId,
        Guid? environmentId,
        DateTimeOffset since,
        CancellationToken cancellationToken = default)
    {
        var query = db.Incidents
            .AsNoTracking()
            .Where(i => i.TenantId == tenantId && i.DetectedAt >= since);

        if (environmentId.HasValue)
            query = query.Where(i => i.EnvironmentId == environmentId.Value);

        var counts = await query
            .GroupBy(i => i.Severity)
            .Select(g => new { Severity = g.Key.ToString(), Count = g.Count() })
            .ToListAsync(cancellationToken);

        return counts.ToDictionary(x => x.Severity, x => x.Count);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ListIncidents.IncidentListItem>> ListNonProductionSignalsAsync(
        Guid tenantId,
        DateTimeOffset since,
        CancellationToken cancellationToken = default)
    {
        return await db.Incidents
            .AsNoTracking()
            .Where(i => i.TenantId == tenantId
                && i.DetectedAt >= since
                // DEFERRED: Replace with IsProductionLike lookup via EnvironmentId
                // when Environment entity is accessible cross-module without direct coupling.
                // See docs/IMPLEMENTATION-STATUS.md — Foundation > Environments: PARTIAL
                && !new[] { "production", "prod" }.Contains(i.Environment.ToLower()))
            .OrderByDescending(i => i.DetectedAt)
            .Select(i => new ListIncidents.IncidentListItem(
                i.Id.Value,
                i.ExternalRef,
                i.Title,
                i.Type,
                i.Severity,
                i.Status,
                i.ServiceId,
                i.ServiceName,
                i.OwnerTeam,
                i.Environment,
                i.DetectedAt,
                i.HasCorrelation,
                i.CorrelationConfidence,
                i.MitigationStatus))
            .ToListAsync(cancellationToken);
    }
}
