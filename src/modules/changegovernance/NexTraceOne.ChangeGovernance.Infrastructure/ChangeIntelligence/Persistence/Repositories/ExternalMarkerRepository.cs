using Microsoft.EntityFrameworkCore;
using NexTraceOne.ChangeIntelligence.Application.Abstractions;
using NexTraceOne.ChangeIntelligence.Domain.Entities;

namespace NexTraceOne.ChangeIntelligence.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repositório de marcadores externos de ferramentas CI/CD.
/// </summary>
internal sealed class ExternalMarkerRepository(ChangeIntelligenceDbContext context) : IExternalMarkerRepository
{
    /// <summary>Lista marcadores de uma release ordenados por data de ocorrência.</summary>
    public async Task<IReadOnlyList<ExternalMarker>> ListByReleaseIdAsync(ReleaseId releaseId, CancellationToken cancellationToken = default)
        => await context.ExternalMarkers
            .Where(m => m.ReleaseId == releaseId)
            .OrderBy(m => m.OccurredAt)
            .ToListAsync(cancellationToken);

    /// <summary>Adiciona um marcador externo.</summary>
    public void Add(ExternalMarker marker)
        => context.ExternalMarkers.Add(marker);
}
