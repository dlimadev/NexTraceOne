using Microsoft.EntityFrameworkCore;

using NexTraceOne.BuildingBlocks.Infrastructure.Persistence;
using NexTraceOne.ChangeGovernance.Application.Promotion.Abstractions;
using NexTraceOne.ChangeGovernance.Domain.Promotion.Entities;

namespace NexTraceOne.ChangeGovernance.Infrastructure.Promotion.Persistence.Repositories;

/// <summary>
/// Repositório de avaliações de gate de promoção, implementando consultas específicas de negócio.
/// </summary>
internal sealed class GateEvaluationRepository(PromotionDbContext context)
    : RepositoryBase<GateEvaluation, GateEvaluationId>(context), IGateEvaluationRepository
{
    /// <summary>Busca uma avaliação de gate pelo identificador.</summary>
    public override async Task<GateEvaluation?> GetByIdAsync(GateEvaluationId id, CancellationToken ct = default)
        => await context.GateEvaluations.SingleOrDefaultAsync(e => e.Id == id, ct);

    /// <summary>Lista avaliações de gate pelo identificador da solicitação de promoção.</summary>
    public async Task<IReadOnlyList<GateEvaluation>> ListByRequestIdAsync(PromotionRequestId requestId, CancellationToken ct)
        => await context.GateEvaluations
            .Where(e => e.PromotionRequestId == requestId)
            .OrderBy(e => e.EvaluatedAt)
            .ToListAsync(ct);

    /// <summary>Lista avaliações de gate pelo identificador do gate.</summary>
    public async Task<IReadOnlyList<GateEvaluation>> ListByGateIdAsync(PromotionGateId gateId, CancellationToken ct)
        => await context.GateEvaluations
            .Where(e => e.PromotionGateId == gateId)
            .OrderByDescending(e => e.EvaluatedAt)
            .ToListAsync(ct);
}
