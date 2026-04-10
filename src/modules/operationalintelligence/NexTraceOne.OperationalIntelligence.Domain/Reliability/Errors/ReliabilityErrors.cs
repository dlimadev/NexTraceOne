using NexTraceOne.BuildingBlocks.Core.Results;

namespace NexTraceOne.OperationalIntelligence.Domain.Reliability.Errors;

/// <summary>
/// Catálogo centralizado de erros do subdomínio Reliability com códigos i18n.
/// Cada erro possui código único para rastreabilidade em logs e documentação.
/// Padrão: Reliability.{Entidade}.{Descrição}
/// </summary>
public static class ReliabilityErrors
{
    /// <summary>Snapshot de reliability não encontrado pelo identificador informado.</summary>
    public static Error SnapshotNotFound(string snapshotId)
        => Error.NotFound(
            "Reliability.Snapshot.NotFound",
            "Reliability snapshot '{0}' was not found.",
            snapshotId);

    /// <summary>Definição SLO não encontrada pelo identificador informado.</summary>
    public static Error SloNotFound(string sloId)
        => Error.NotFound(
            "Reliability.Slo.NotFound",
            "SLO definition '{0}' was not found.",
            sloId);

    /// <summary>Definição SLA não encontrada pelo identificador informado.</summary>
    public static Error SlaNotFound(string slaId)
        => Error.NotFound(
            "Reliability.Sla.NotFound",
            "SLA definition '{0}' was not found.",
            slaId);

    /// <summary>Snapshot de burn rate não encontrado para o serviço e ambiente.</summary>
    public static Error BurnRateNotFound(string serviceId, string environment)
        => Error.NotFound(
            "Reliability.BurnRate.NotFound",
            "Burn rate snapshot not found for service '{0}' in environment '{1}'.",
            serviceId,
            environment);

    /// <summary>Snapshot de error budget não encontrado para o serviço e ambiente.</summary>
    public static Error ErrorBudgetNotFound(string serviceId, string environment)
        => Error.NotFound(
            "Reliability.ErrorBudget.NotFound",
            "Error budget snapshot not found for service '{0}' in environment '{1}'.",
            serviceId,
            environment);

    /// <summary>Tipo de SLO inválido para o serviço.</summary>
    public static Error InvalidSloType(string sloType)
        => Error.Validation(
            "Reliability.Slo.InvalidType",
            "SLO type '{0}' is not valid.",
            sloType);

    /// <summary>Meta de SLO fora do intervalo permitido (0-100%).</summary>
    public static Error InvalidSloTarget(decimal target)
        => Error.Validation(
            "Reliability.Slo.InvalidTarget",
            "SLO target ({0}%) must be between 0 and 100.",
            target);

    /// <summary>Definição de SLO duplicada para o mesmo serviço, ambiente e tipo.</summary>
    public static Error DuplicateSloDefinition(string serviceName, string environment, string sloType)
        => Error.Conflict(
            "Reliability.Slo.Duplicate",
            "An SLO definition already exists for service '{0}' in environment '{1}' with type '{2}'.",
            serviceName,
            environment,
            sloType);

    /// <summary>Serviço não encontrado ao consultar reliability.</summary>
    public static Error ServiceNotFound(string serviceId)
        => Error.NotFound(
            "Reliability.Service.NotFound",
            "Service '{0}' was not found.",
            serviceId);

    /// <summary>Error budget esgotado para o período vigente do SLO.</summary>
    public static Error ErrorBudgetExhausted(string serviceName, string sloType)
        => Error.Business(
            "Reliability.ErrorBudget.Exhausted",
            "Error budget for service '{0}' SLO '{1}' has been exhausted.",
            serviceName,
            sloType);

    /// <summary>Recomendação de self-healing não encontrada pelo identificador informado.</summary>
    public static Error HealingRecommendationNotFound(string id)
        => Error.NotFound(
            "Reliability.HealingRecommendation.NotFound",
            "Healing recommendation '{0}' was not found.",
            id);

    /// <summary>Transição de estado inválida para recomendação de self-healing.</summary>
    public static Error HealingRecommendationInvalidTransition(string id, string from, string to)
        => Error.Conflict(
            "Reliability.HealingRecommendation.InvalidTransition",
            "Cannot transition healing recommendation '{0}' from '{1}' to '{2}'.",
            id,
            from,
            to);
}
