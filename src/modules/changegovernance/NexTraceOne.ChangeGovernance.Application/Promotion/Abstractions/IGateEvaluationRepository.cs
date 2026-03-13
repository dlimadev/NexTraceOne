using NexTraceOne.Promotion.Domain.Entities;

namespace NexTraceOne.Promotion.Application.Abstractions;

/// <summary>
/// Contrato de repositório para avaliações de gate de promoção.
/// </summary>
public interface IGateEvaluationRepository
{
    /// <summary>Busca uma avaliação de gate pelo identificador.</summary>
    Task<GateEvaluation?> GetByIdAsync(GateEvaluationId id, CancellationToken ct);

    /// <summary>Adiciona uma nova avaliação de gate ao contexto.</summary>
    void Add(GateEvaluation evaluation);

    /// <summary>Atualiza uma avaliação de gate existente no contexto.</summary>
    void Update(GateEvaluation evaluation);

    /// <summary>Lista avaliações de gate pelo identificador da solicitação de promoção.</summary>
    Task<IReadOnlyList<GateEvaluation>> ListByRequestIdAsync(PromotionRequestId requestId, CancellationToken ct);

    /// <summary>Lista avaliações de gate pelo identificador do gate.</summary>
    Task<IReadOnlyList<GateEvaluation>> ListByGateIdAsync(PromotionGateId gateId, CancellationToken ct);
}
