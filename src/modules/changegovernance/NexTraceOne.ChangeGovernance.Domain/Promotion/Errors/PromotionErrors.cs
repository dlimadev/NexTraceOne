using NexTraceOne.BuildingBlocks.Core.Results;

namespace NexTraceOne.ChangeGovernance.Domain.Promotion.Errors;

/// <summary>
/// Catálogo centralizado de erros do módulo Promotion.
/// Cada erro possui código único para rastreabilidade em logs e documentação.
/// Padrão: Promotion.{Entidade}.{Descrição}
/// </summary>
public static class PromotionErrors
{
    /// <summary>Ambiente de deployment não encontrado.</summary>
    public static Error EnvironmentNotFound(string id)
        => Error.NotFound(
            "Promotion.Environment.NotFound",
            "Deployment environment '{0}' was not found.",
            id);

    /// <summary>Solicitação de promoção não encontrada.</summary>
    public static Error RequestNotFound(string id)
        => Error.NotFound(
            "Promotion.Request.NotFound",
            "Promotion request '{0}' was not found.",
            id);

    /// <summary>Gate de promoção não encontrado.</summary>
    public static Error GateNotFound(string id)
        => Error.NotFound(
            "Promotion.Gate.NotFound",
            "Promotion gate '{0}' was not found.",
            id);

    /// <summary>Avaliação de gate não encontrada.</summary>
    public static Error GateEvaluationNotFound(string id)
        => Error.NotFound(
            "Promotion.GateEvaluation.NotFound",
            "Gate evaluation '{0}' was not found.",
            id);

    /// <summary>Transição de status de promoção inválida.</summary>
    public static Error InvalidStatusTransition(string current, string target)
        => Error.Conflict(
            "Promotion.Request.InvalidStatusTransition",
            "Cannot transition promotion status from '{0}' to '{1}'.",
            current,
            target);

    /// <summary>Solicitação de promoção já foi concluída e não pode ser alterada.</summary>
    public static Error AlreadyCompleted()
        => Error.Conflict(
            "Promotion.Request.AlreadyCompleted",
            "This promotion request has already been completed and cannot be modified.");

    /// <summary>Gate obrigatório não foi satisfeito.</summary>
    public static Error GateNotPassed(string gateName)
        => Error.Business(
            "Promotion.Gate.NotPassed",
            "Required promotion gate '{0}' has not been passed.",
            gateName);

    /// <summary>Todos os gates obrigatórios devem ser satisfeitos antes da aprovação.</summary>
    public static Error AllGatesRequired()
        => Error.Business(
            "Promotion.Gate.AllRequired",
            "All required promotion gates must be passed before approval.");

    /// <summary>Já existe um ambiente com o nome informado.</summary>
    public static Error DuplicateEnvironmentName(string name)
        => Error.Conflict(
            "Promotion.Environment.DuplicateName",
            "A deployment environment with the name '{0}' already exists.",
            name);
}
