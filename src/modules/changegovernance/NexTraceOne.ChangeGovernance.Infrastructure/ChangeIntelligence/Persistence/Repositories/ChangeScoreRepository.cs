using Microsoft.EntityFrameworkCore;

using NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Abstractions;
using NexTraceOne.ChangeGovernance.Domain.ChangeIntelligence.Entities;

namespace NexTraceOne.ChangeGovernance.Infrastructure.ChangeIntelligence.Persistence.Repositories;

/// <summary>
/// Repositório de scores de risco de mudança, implementando consultas específicas de negócio.
/// </summary>
internal sealed class ChangeScoreRepository(ChangeIntelligenceDbContext context) : IChangeScoreRepository
{
    /// <summary>Busca o score de uma release.</summary>
    public async Task<ChangeIntelligenceScore?> GetByReleaseIdAsync(ReleaseId releaseId, CancellationToken cancellationToken = default)
        => await context.ChangeScores
            .SingleOrDefaultAsync(s => s.ReleaseId == releaseId, cancellationToken);

    /// <summary>Adiciona um novo score.</summary>
    public void Add(ChangeIntelligenceScore score)
        => context.ChangeScores.Add(score);
}
