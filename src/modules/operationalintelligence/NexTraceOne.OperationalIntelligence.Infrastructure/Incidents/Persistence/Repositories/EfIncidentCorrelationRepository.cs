using Microsoft.EntityFrameworkCore;

using NexTraceOne.BuildingBlocks.Infrastructure.Persistence;
using NexTraceOne.OperationalIntelligence.Application.Incidents.Abstractions;
using NexTraceOne.OperationalIntelligence.Domain.Incidents.Entities;
using NexTraceOne.OperationalIntelligence.Infrastructure.Incidents.Persistence;

namespace NexTraceOne.OperationalIntelligence.Infrastructure.Incidents.Persistence.Repositories;

/// <summary>
/// Implementação EF Core do repositório de correlações dinâmicas incidente↔mudança.
/// Persiste e consulta entidades IncidentChangeCorrelation no IncidentDbContext.
/// Cada AddAsync confirma atomicamente a correlação individual para evitar perdas parciais.
/// </summary>
internal sealed class EfIncidentCorrelationRepository(IncidentDbContext context)
    : RepositoryBase<IncidentChangeCorrelation, IncidentChangeCorrelationId>(context),
      IIncidentCorrelationRepository
{
    /// <inheritdoc />
    public async Task AddAsync(IncidentChangeCorrelation correlation, CancellationToken cancellationToken)
    {
        context.ChangeCorrelations.Add(correlation);
        await context.CommitAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task AddRangeAsync(IReadOnlyList<IncidentChangeCorrelation> correlations, CancellationToken cancellationToken)
    {
        if (correlations.Count == 0)
            return;

        context.ChangeCorrelations.AddRange(correlations);
        await context.CommitAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<IncidentChangeCorrelation>> GetByIncidentIdAsync(
        Guid incidentId,
        CancellationToken cancellationToken)
        => await context.ChangeCorrelations
            .AsNoTracking()
            .Where(c => c.IncidentId == incidentId)
            .OrderBy(c => c.ConfidenceLevel)    // High(0) before Medium(1) before Low(2)
            .ThenByDescending(c => c.ChangeOccurredAt)
            .ToListAsync(cancellationToken);

    /// <inheritdoc />
    public async Task<bool> ExistsByIncidentAndChangeAsync(
        Guid incidentId,
        Guid changeId,
        CancellationToken cancellationToken)
        => await context.ChangeCorrelations
            .AnyAsync(
                c => c.IncidentId == incidentId && c.ChangeId == changeId,
                cancellationToken);
}
