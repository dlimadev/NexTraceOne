using Microsoft.EntityFrameworkCore;

using NexTraceOne.OperationalIntelligence.Application.Incidents.Abstractions;
using NexTraceOne.OperationalIntelligence.Domain.Incidents.Entities;
using NexTraceOne.OperationalIntelligence.Infrastructure.Incidents.Persistence;

namespace NexTraceOne.OperationalIntelligence.Infrastructure.Incidents.Persistence.Repositories;

/// <summary>
/// Implementação EF Core do repositório de narrativas de incidentes.
/// Persiste e consulta entidades IncidentNarrative no IncidentDbContext.
/// </summary>
internal sealed class EfIncidentNarrativeRepository(IncidentDbContext context) : IIncidentNarrativeRepository
{
    /// <inheritdoc />
    public async Task<IncidentNarrative?> GetByIncidentIdAsync(Guid incidentId, CancellationToken cancellationToken)
        => await context.IncidentNarratives
            .SingleOrDefaultAsync(n => n.IncidentId == incidentId, cancellationToken);

    /// <inheritdoc />
    public async Task<IncidentNarrative?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        => await context.IncidentNarratives
            .SingleOrDefaultAsync(n => n.Id == new IncidentNarrativeId(id), cancellationToken);

    /// <inheritdoc />
    public async Task AddAsync(IncidentNarrative narrative, CancellationToken cancellationToken)
    {
        context.IncidentNarratives.Add(narrative);
        await context.CommitAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task UpdateAsync(IncidentNarrative narrative, CancellationToken cancellationToken)
    {
        context.IncidentNarratives.Update(narrative);
        await context.CommitAsync(cancellationToken);
    }
}
