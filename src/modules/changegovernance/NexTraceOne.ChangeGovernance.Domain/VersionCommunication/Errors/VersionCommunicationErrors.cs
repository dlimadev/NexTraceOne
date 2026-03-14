using NexTraceOne.BuildingBlocks.Domain.Results;

namespace NexTraceOne.VersionCommunication.Domain.Errors;

/// <summary>
/// Catálogo centralizado de erros do subdomínio VersionCommunication.
/// Cada erro possui código i18n único para rastreabilidade em logs, frontend e documentação.
/// Padrão: VersionCommunication.{Entidade}.{Descrição}
/// </summary>
public static class VersionCommunicationErrors
{
    /// <summary>Plano de rollout de versão não encontrado.</summary>
    public static Error RolloutPlanNotFound(string id)
        => Error.NotFound(
            "VersionCommunication.RolloutPlan.NotFound",
            "Version rollout plan '{0}' was not found.",
            id);

    /// <summary>Transição de status inválida no plano de rollout.</summary>
    public static Error InvalidRolloutStatusTransition(string current, string target)
        => Error.Conflict(
            "VersionCommunication.RolloutPlan.InvalidStatusTransition",
            "Cannot transition rollout status from '{0}' to '{1}'.",
            current,
            target);

    /// <summary>Plano de rollout já foi concluído ou cancelado e não pode ser alterado.</summary>
    public static Error RolloutPlanAlreadyTerminal()
        => Error.Conflict(
            "VersionCommunication.RolloutPlan.AlreadyTerminal",
            "This version rollout plan has already reached a terminal state and cannot be modified.");

    /// <summary>Plano de migração de consumidor não encontrado.</summary>
    public static Error ConsumerMigrationPlanNotFound(string id)
        => Error.NotFound(
            "VersionCommunication.ConsumerMigrationPlan.NotFound",
            "Consumer migration plan '{0}' was not found.",
            id);

    /// <summary>Transição de status inválida no plano de migração do consumidor.</summary>
    public static Error InvalidConsumerMigrationTransition(string current, string target)
        => Error.Conflict(
            "VersionCommunication.ConsumerMigrationPlan.InvalidStatusTransition",
            "Cannot transition consumer migration status from '{0}' to '{1}'.",
            current,
            target);

    /// <summary>Consumidor já concluiu ou foi ignorado na migração.</summary>
    public static Error ConsumerMigrationAlreadyTerminal()
        => Error.Conflict(
            "VersionCommunication.ConsumerMigrationPlan.AlreadyTerminal",
            "This consumer migration has already reached a terminal state.");

    /// <summary>Schedule de deprecação de versão não encontrado.</summary>
    public static Error DeprecationScheduleNotFound(string id)
        => Error.NotFound(
            "VersionCommunication.DeprecationSchedule.NotFound",
            "Version deprecation schedule '{0}' was not found.",
            id);

    /// <summary>Schedule de deprecação já está em modo enforced.</summary>
    public static Error DeprecationAlreadyEnforced()
        => Error.Conflict(
            "VersionCommunication.DeprecationSchedule.AlreadyEnforced",
            "This deprecation schedule is already being enforced.");

    /// <summary>Nova data de sunset deve ser posterior à data atual.</summary>
    public static Error InvalidSunsetDateExtension(string currentDate, string newDate)
        => Error.Validation(
            "VersionCommunication.DeprecationSchedule.InvalidSunsetDateExtension",
            "New sunset date '{1}' must be after the current sunset date '{0}'.",
            currentDate,
            newDate);
}
