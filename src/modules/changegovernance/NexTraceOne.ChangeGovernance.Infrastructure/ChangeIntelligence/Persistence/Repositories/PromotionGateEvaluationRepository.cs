using Microsoft.EntityFrameworkCore;

using NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Abstractions;
using NexTraceOne.ChangeGovernance.Domain.ChangeIntelligence.Entities;

namespace NexTraceOne.ChangeGovernance.Infrastructure.ChangeIntelligence.Persistence.Repositories;

/// <summary>
/// Repositório de avaliações de gates de promoção (append-only).
/// </summary>
internal sealed class PromotionGateEvaluationRepository(ChangeIntelligenceDbContext context) : IPromotionGateEvaluationRepository
{
    /// <inheritdoc />
    public async Task<PromotionGateEvaluation?> GetByIdAsync(PromotionGateEvaluationId id, CancellationToken cancellationToken = default)
        => await context.PromotionGateEvaluations
            .SingleOrDefaultAsync(e => e.Id == id, cancellationToken);

    /// <inheritdoc />
    public async Task<IReadOnlyList<PromotionGateEvaluation>> ListByGateAsync(PromotionGateId gateId, CancellationToken cancellationToken = default)
        => await context.PromotionGateEvaluations
            .Where(e => e.GateId == gateId)
            .OrderBy(e => e.EvaluatedAt)
            .ToListAsync(cancellationToken);

    /// <inheritdoc />
    public async Task<IReadOnlyList<PromotionGateEvaluation>> ListByChangeAsync(string changeId, CancellationToken cancellationToken = default)
        => await context.PromotionGateEvaluations
            .Where(e => e.ChangeId == changeId)
            .OrderBy(e => e.EvaluatedAt)
            .ToListAsync(cancellationToken);

    /// <inheritdoc />
    public async Task AddAsync(PromotionGateEvaluation evaluation, CancellationToken cancellationToken = default)
        => await context.PromotionGateEvaluations.AddAsync(evaluation, cancellationToken);
}
