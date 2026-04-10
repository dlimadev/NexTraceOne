using NexTraceOne.BuildingBlocks.Core.Results;

namespace NexTraceOne.ChangeGovernance.Domain.ChangeIntelligence.Errors;

/// <summary>
/// Catálogo centralizado de erros do subdomínio PromotionGate com códigos i18n.
/// Cada erro possui código único para rastreabilidade em logs e documentação.
/// Padrão: ChangeIntelligence.PromotionGate.{Descrição}
/// </summary>
public static class PromotionGateErrors
{
    /// <summary>Gate de promoção não encontrado.</summary>
    public static Error GateNotFound(string gateId)
        => Error.NotFound("ChangeIntelligence.PromotionGate.NotFound", "Promotion gate '{0}' was not found.", gateId);

    /// <summary>Avaliação de gate de promoção não encontrada.</summary>
    public static Error EvaluationNotFound(string evaluationId)
        => Error.NotFound("ChangeIntelligence.PromotionGateEvaluation.NotFound", "Promotion gate evaluation '{0}' was not found.", evaluationId);

    /// <summary>Gate de promoção já está inativo.</summary>
    public static Error GateAlreadyInactive()
        => Error.Conflict("ChangeIntelligence.PromotionGate.AlreadyInactive", "Promotion gate is already inactive.");

    /// <summary>Gate de promoção já está ativo.</summary>
    public static Error GateAlreadyActive()
        => Error.Conflict("ChangeIntelligence.PromotionGate.AlreadyActive", "Promotion gate is already active.");
}
