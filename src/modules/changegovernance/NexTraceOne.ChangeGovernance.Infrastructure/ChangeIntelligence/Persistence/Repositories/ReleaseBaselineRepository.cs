using Microsoft.EntityFrameworkCore;
using NexTraceOne.ChangeIntelligence.Application.Abstractions;
using NexTraceOne.ChangeIntelligence.Domain.Entities;

namespace NexTraceOne.ChangeIntelligence.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repositório de baselines de indicadores pré-release.
/// </summary>
internal sealed class ReleaseBaselineRepository(ChangeIntelligenceDbContext context) : IReleaseBaselineRepository
{
    /// <summary>Busca o baseline de uma release.</summary>
    public async Task<ReleaseBaseline?> GetByReleaseIdAsync(ReleaseId releaseId, CancellationToken cancellationToken = default)
        => await context.ReleaseBaselines
            .SingleOrDefaultAsync(b => b.ReleaseId == releaseId, cancellationToken);

    /// <summary>Adiciona um baseline de release.</summary>
    public void Add(ReleaseBaseline baseline)
        => context.ReleaseBaselines.Add(baseline);
}
