using Microsoft.EntityFrameworkCore;
using NexTraceOne.ChangeIntelligence.Application.Abstractions;
using NexTraceOne.ChangeIntelligence.Domain.Entities;

namespace NexTraceOne.ChangeIntelligence.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repositório de avaliações de viabilidade de rollback.
/// </summary>
internal sealed class RollbackAssessmentRepository(ChangeIntelligenceDbContext context) : IRollbackAssessmentRepository
{
    /// <summary>Busca a avaliação de rollback de uma release.</summary>
    public async Task<RollbackAssessment?> GetByReleaseIdAsync(ReleaseId releaseId, CancellationToken cancellationToken = default)
        => await context.RollbackAssessments
            .SingleOrDefaultAsync(a => a.ReleaseId == releaseId, cancellationToken);

    /// <summary>Adiciona uma avaliação de rollback.</summary>
    public void Add(RollbackAssessment assessment)
        => context.RollbackAssessments.Add(assessment);
}
