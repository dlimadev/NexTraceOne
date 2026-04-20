using Microsoft.EntityFrameworkCore;

using NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Abstractions;
using NexTraceOne.ChangeGovernance.Domain.ChangeIntelligence.Entities;

namespace NexTraceOne.ChangeGovernance.Infrastructure.ChangeIntelligence.Persistence.Repositories;

/// <summary>
/// Repositório do breakdown de confiança de mudanças (Change Confidence Score 2.0).
/// Implementa consultas específicas de negócio para ChangeConfidenceBreakdown.
/// </summary>
internal sealed class ChangeConfidenceBreakdownRepository(ChangeIntelligenceDbContext context)
    : IChangeConfidenceBreakdownRepository
{
    /// <summary>Busca o breakdown de confiança de uma release, incluindo sub-scores.</summary>
    public async Task<ChangeConfidenceBreakdown?> GetByReleaseIdAsync(ReleaseId releaseId, CancellationToken cancellationToken = default)
        => await context.ConfidenceBreakdowns
            .Include(x => x.SubScores)
            .FirstOrDefaultAsync(x => x.ReleaseId == releaseId, cancellationToken);

    /// <summary>Adiciona um novo breakdown de confiança ao contexto.</summary>
    public void Add(ChangeConfidenceBreakdown breakdown)
        => context.ConfidenceBreakdowns.Add(breakdown);
}
