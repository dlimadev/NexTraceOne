using Microsoft.EntityFrameworkCore;

using NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Abstractions;
using NexTraceOne.ChangeGovernance.Domain.ChangeIntelligence.Entities;

namespace NexTraceOne.ChangeGovernance.Infrastructure.ChangeIntelligence.Persistence.Repositories;

/// <summary>
/// Repositório de registos de canary rollout de releases.
/// </summary>
internal sealed class CanaryRolloutRepository(ChangeIntelligenceDbContext context) : ICanaryRolloutRepository
{
    /// <summary>Obtém o registo de canary rollout mais recente de uma release.</summary>
    public async Task<CanaryRollout?> GetLatestByReleaseIdAsync(ReleaseId releaseId, CancellationToken cancellationToken = default)
        => await context.CanaryRollouts
            .Where(r => r.ReleaseId == releaseId)
            .OrderByDescending(r => r.RecordedAt)
            .FirstOrDefaultAsync(cancellationToken);

    /// <summary>Lista todos os registos de canary rollout de uma release (histórico).</summary>
    public async Task<IReadOnlyList<CanaryRollout>> ListByReleaseIdAsync(ReleaseId releaseId, CancellationToken cancellationToken = default)
        => await context.CanaryRollouts
            .Where(r => r.ReleaseId == releaseId)
            .OrderByDescending(r => r.RecordedAt)
            .ToListAsync(cancellationToken);

    /// <summary>Adiciona um novo registo de canary rollout.</summary>
    public void Add(CanaryRollout rollout)
        => context.CanaryRollouts.Add(rollout);
}
