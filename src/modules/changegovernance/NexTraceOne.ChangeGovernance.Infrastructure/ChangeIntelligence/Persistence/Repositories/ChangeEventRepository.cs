using Microsoft.EntityFrameworkCore;
using NexTraceOne.ChangeIntelligence.Application.Abstractions;
using NexTraceOne.ChangeIntelligence.Domain.Entities;

namespace NexTraceOne.ChangeIntelligence.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repositório de eventos de mudança na timeline de uma release.
/// </summary>
internal sealed class ChangeEventRepository(ChangeIntelligenceDbContext context) : IChangeEventRepository
{
    /// <summary>Lista eventos de uma release ordenados por data de ocorrência.</summary>
    public async Task<IReadOnlyList<ChangeEvent>> ListByReleaseIdAsync(ReleaseId releaseId, CancellationToken cancellationToken = default)
        => await context.ChangeEvents
            .Where(e => e.ReleaseId == releaseId)
            .OrderBy(e => e.OccurredAt)
            .ToListAsync(cancellationToken);

    /// <summary>Adiciona um evento de mudança.</summary>
    public void Add(ChangeEvent changeEvent)
        => context.ChangeEvents.Add(changeEvent);
}
