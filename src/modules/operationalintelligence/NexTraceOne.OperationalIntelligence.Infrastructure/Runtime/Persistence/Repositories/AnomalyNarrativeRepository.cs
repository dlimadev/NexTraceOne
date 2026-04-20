using Microsoft.EntityFrameworkCore;

using NexTraceOne.BuildingBlocks.Infrastructure.Persistence;
using NexTraceOne.OperationalIntelligence.Application.Runtime.Abstractions;
using NexTraceOne.OperationalIntelligence.Domain.Runtime.Entities;

namespace NexTraceOne.OperationalIntelligence.Infrastructure.Runtime.Persistence.Repositories;

/// <summary>
/// Repositório EF Core para narrativas de anomalia (AnomalyNarrative).
/// Isolamento total: acessa apenas RuntimeIntelligenceDbContext — sem acesso cross-module.
/// </summary>
internal sealed class AnomalyNarrativeRepository(RuntimeIntelligenceDbContext context)
    : RepositoryBase<AnomalyNarrative, AnomalyNarrativeId>(context), IAnomalyNarrativeRepository
{
    /// <summary>Persiste uma nova narrativa de anomalia.</summary>
    public async Task AddAsync(AnomalyNarrative narrative, CancellationToken cancellationToken)
    {
        await context.AnomalyNarratives.AddAsync(narrative, cancellationToken);
    }

    /// <summary>Obtém a narrativa associada a um drift finding, ou null se não existir.</summary>
    public async Task<AnomalyNarrative?> GetByDriftFindingIdAsync(DriftFindingId driftFindingId, CancellationToken cancellationToken)
        => await context.AnomalyNarratives
            .SingleOrDefaultAsync(n => n.DriftFindingId == driftFindingId, cancellationToken);

    /// <summary>Persiste alterações à narrativa existente.</summary>
    public new void Update(AnomalyNarrative narrative)
    {
        context.AnomalyNarratives.Update(narrative);
    }
}
