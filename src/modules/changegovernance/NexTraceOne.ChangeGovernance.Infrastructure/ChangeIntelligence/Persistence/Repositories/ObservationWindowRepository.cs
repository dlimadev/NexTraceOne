using Microsoft.EntityFrameworkCore;
using NexTraceOne.ChangeIntelligence.Application.Abstractions;
using NexTraceOne.ChangeIntelligence.Domain.Entities;
using NexTraceOne.ChangeIntelligence.Domain.Enums;

namespace NexTraceOne.ChangeIntelligence.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repositório de janelas de observação pós-release para comparação progressiva com baseline.
/// </summary>
internal sealed class ObservationWindowRepository(ChangeIntelligenceDbContext context) : IObservationWindowRepository
{
    /// <summary>Lista janelas de observação de uma release ordenadas por fase.</summary>
    public async Task<IReadOnlyList<ObservationWindow>> ListByReleaseIdAsync(ReleaseId releaseId, CancellationToken cancellationToken = default)
        => await context.ObservationWindows
            .Where(w => w.ReleaseId == releaseId)
            .OrderBy(w => w.Phase)
            .ToListAsync(cancellationToken);

    /// <summary>Busca janela de observação por release e fase.</summary>
    public async Task<ObservationWindow?> GetByReleaseIdAndPhaseAsync(ReleaseId releaseId, ObservationPhase phase, CancellationToken cancellationToken = default)
        => await context.ObservationWindows
            .SingleOrDefaultAsync(w => w.ReleaseId == releaseId && w.Phase == phase, cancellationToken);

    /// <summary>Adiciona uma janela de observação.</summary>
    public void Add(ObservationWindow window)
        => context.ObservationWindows.Add(window);
}
