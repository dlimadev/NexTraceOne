using Microsoft.EntityFrameworkCore;

using NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Abstractions;
using NexTraceOne.ChangeGovernance.Domain.ChangeIntelligence.Entities;

namespace NexTraceOne.ChangeGovernance.Infrastructure.ChangeIntelligence.Persistence.Repositories;

/// <summary>
/// Repositório de estados de feature flags de releases.
/// </summary>
internal sealed class FeatureFlagStateRepository(ChangeIntelligenceDbContext context) : IFeatureFlagStateRepository
{
    /// <summary>Obtém o estado de feature flags mais recente de uma release.</summary>
    public async Task<ReleaseFeatureFlagState?> GetLatestByReleaseIdAsync(ReleaseId releaseId, CancellationToken cancellationToken = default)
        => await context.FeatureFlagStates
            .Where(s => s.ReleaseId == releaseId)
            .OrderByDescending(s => s.RecordedAt)
            .FirstOrDefaultAsync(cancellationToken);

    /// <summary>Adiciona um novo estado de feature flags.</summary>
    public void Add(ReleaseFeatureFlagState state)
        => context.FeatureFlagStates.Add(state);
}
