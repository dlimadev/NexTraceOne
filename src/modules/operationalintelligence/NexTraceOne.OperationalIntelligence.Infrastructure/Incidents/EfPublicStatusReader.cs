using Microsoft.EntityFrameworkCore;

using NexTraceOne.OperationalIntelligence.Application.Incidents.Abstractions;
using NexTraceOne.OperationalIntelligence.Domain.Incidents.Enums;
using NexTraceOne.OperationalIntelligence.Infrastructure.Persistence;

namespace NexTraceOne.OperationalIntelligence.Infrastructure.Incidents;

/// <summary>
/// Implementação EF do leitor de status público.
/// Consulta apenas incidentes abertos do tenant informado, com projeção
/// restrita aos campos não sensíveis exibidos na status page.
/// </summary>
internal sealed class EfPublicStatusReader(IncidentResponseDbContext db) : IPublicStatusReader
{
    private const int MaxIncidents = 50;

    /// <inheritdoc/>
    public async Task<PublicStatusSnapshot> GetSnapshotAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        var incidents = await db.Incidents
            .AsNoTracking()
            .Where(i => i.TenantId == tenantId
                && i.Status != IncidentStatus.Resolved
                && i.Status != IncidentStatus.Closed)
            .OrderByDescending(i => i.DetectedAt)
            .Take(MaxIncidents)
            .Select(i => new PublicStatusIncident(
                i.ExternalRef,
                i.Title,
                i.Severity.ToString(),
                i.Status.ToString(),
                i.ServiceName,
                i.DetectedAt))
            .ToListAsync(cancellationToken);

        return new PublicStatusSnapshot(incidents);
    }
}
