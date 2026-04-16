using Microsoft.EntityFrameworkCore;

using NexTraceOne.AIKnowledge.Application.Runtime.Abstractions;
using NexTraceOne.OperationalIntelligence.Infrastructure.Incidents.Persistence;

namespace NexTraceOne.AIKnowledge.Infrastructure.Context;

/// <summary>
/// Implementação do leitor de incidentes operacionais para grounding de IA.
/// Acesso somente-leitura ao IncidentDbContext.
/// </summary>
public sealed class IncidentGroundingReader(IncidentDbContext incidentDb) : IIncidentGroundingReader
{
    public async Task<IReadOnlyList<IncidentGroundingContext>> FindRecentIncidentsAsync(
        DateTimeOffset from,
        string? serviceId,
        string? environment,
        int maxResults,
        CancellationToken ct = default)
    {
        var query = incidentDb.Incidents
            .AsNoTracking()
            .Where(i => i.DetectedAt >= from);

        if (!string.IsNullOrWhiteSpace(serviceId))
            query = query.Where(i =>
                i.ServiceId == serviceId || i.ServiceName == serviceId ||
                i.ServiceName.Contains(serviceId));

        if (!string.IsNullOrWhiteSpace(environment))
            query = query.Where(i => i.Environment == environment);

        var incidents = await query
            .OrderByDescending(i => i.DetectedAt)
            .Take(maxResults)
            .ToListAsync(ct);

        return incidents.Select(i => new IncidentGroundingContext(
            IncidentId: i.Id.Value.ToString(),
            Title: i.Title,
            ServiceName: i.ServiceName,
            Severity: i.Severity.ToString(),
            Status: i.Status.ToString(),
            Environment: i.Environment,
            Description: i.Description,
            DetectedAt: i.DetectedAt)).ToList();
    }
}
