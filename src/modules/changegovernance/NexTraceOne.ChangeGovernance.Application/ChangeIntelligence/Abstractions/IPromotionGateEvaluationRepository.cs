using NexTraceOne.ChangeGovernance.Domain.ChangeIntelligence.Entities;

namespace NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Abstractions;

/// <summary>
/// Repositório para avaliações de gates de promoção (append-only).
/// </summary>
public interface IPromotionGateEvaluationRepository
{
    /// <summary>Busca uma avaliação pelo identificador.</summary>
    Task<PromotionGateEvaluation?> GetByIdAsync(PromotionGateEvaluationId id, CancellationToken cancellationToken = default);

    /// <summary>Lista avaliações de um gate de promoção específico, ordenadas cronologicamente.</summary>
    Task<IReadOnlyList<PromotionGateEvaluation>> ListByGateAsync(PromotionGateId gateId, CancellationToken cancellationToken = default);

    /// <summary>Lista avaliações de uma mudança específica, ordenadas cronologicamente.</summary>
    Task<IReadOnlyList<PromotionGateEvaluation>> ListByChangeAsync(string changeId, CancellationToken cancellationToken = default);

    /// <summary>Adiciona uma nova avaliação de gate de promoção (append-only).</summary>
    Task AddAsync(PromotionGateEvaluation evaluation, CancellationToken cancellationToken = default);
}
