using NexTraceOne.BuildingBlocks.Core.Results;

namespace NexTraceOne.OperationalIntelligence.Domain.Runtime.Errors;

/// <summary>
/// Catálogo centralizado de erros do módulo RuntimeIntelligence com códigos i18n.
/// Cada erro possui código único para rastreabilidade em logs e documentação.
/// Padrão: RuntimeIntelligence.{Entidade}.{Descrição}
/// </summary>
public static class RuntimeIntelligenceErrors
{
    /// <summary>Snapshot de runtime não encontrado pelo identificador informado.</summary>
    public static Error SnapshotNotFound(string snapshotId)
        => Error.NotFound(
            "RuntimeIntelligence.RuntimeSnapshot.NotFound",
            "Runtime snapshot '{0}' was not found.",
            snapshotId);

    /// <summary>Baseline de runtime não encontrada pelo identificador informado.</summary>
    public static Error BaselineNotFound(string baselineId)
        => Error.NotFound(
            "RuntimeIntelligence.RuntimeBaseline.NotFound",
            "Runtime baseline '{0}' was not found.",
            baselineId);

    /// <summary>Drift finding não encontrado pelo identificador informado.</summary>
    public static Error DriftNotFound(string driftId)
        => Error.NotFound(
            "RuntimeIntelligence.DriftFinding.NotFound",
            "Drift finding '{0}' was not found.",
            driftId);

    /// <summary>Perfil de observabilidade não encontrado pelo identificador informado.</summary>
    public static Error ProfileNotFound(string profileId)
        => Error.NotFound(
            "RuntimeIntelligence.ObservabilityProfile.NotFound",
            "Observability profile '{0}' was not found.",
            profileId);

    /// <summary>Valor de métrica inválido — fora do intervalo permitido para o tipo de métrica.</summary>
    public static Error InvalidMetricValue(string metricName, decimal value, decimal min, decimal max)
        => Error.Validation(
            "RuntimeIntelligence.Metric.InvalidValue",
            "Metric '{0}' value ({1}) is outside the valid range [{2}, {3}].",
            metricName, value, min, max);

    /// <summary>Já existe uma baseline para o mesmo serviço e ambiente.</summary>
    public static Error DuplicateBaseline(string serviceName, string environment)
        => Error.Conflict(
            "RuntimeIntelligence.RuntimeBaseline.Duplicate",
            "A runtime baseline already exists for service '{0}' in environment '{1}'.",
            serviceName, environment);

    /// <summary>O drift finding já foi reconhecido anteriormente.</summary>
    public static Error AlreadyAcknowledged(string driftId)
        => Error.Conflict(
            "RuntimeIntelligence.DriftFinding.AlreadyAcknowledged",
            "Drift finding '{0}' has already been acknowledged.",
            driftId);

    /// <summary>Percentual de tolerância inválido — deve ser positivo.</summary>
    public static Error InvalidTolerance(decimal tolerance)
        => Error.Validation(
            "RuntimeIntelligence.Tolerance.Invalid",
            "Tolerance percentage ({0}) must be greater than zero.",
            tolerance);

    /// <summary>Narrativa de anomalia não encontrada para o drift finding informado.</summary>
    public static Error AnomalyNarrativeNotFound(string driftFindingId)
        => Error.NotFound(
            "RuntimeIntelligence.AnomalyNarrative.NotFound",
            "Anomaly narrative for drift finding '{0}' was not found.",
            driftFindingId);

    /// <summary>Já existe uma narrativa de anomalia para o drift finding informado.</summary>
    public static Error AnomalyNarrativeAlreadyExists(string driftFindingId)
        => Error.Conflict(
            "RuntimeIntelligence.AnomalyNarrative.AlreadyExists",
            "An anomaly narrative already exists for drift finding '{0}'.",
            driftFindingId);

    /// <summary>Relatório de drift entre ambientes não encontrado.</summary>
    public static Error DriftReportNotFound(string reportId)
        => Error.NotFound(
            "RuntimeIntelligence.EnvironmentDriftReport.NotFound",
            "Environment drift report '{0}' was not found.",
            reportId);

    /// <summary>Relatório de drift já foi revisado anteriormente.</summary>
    public static Error DriftReportAlreadyReviewed(string reportId)
        => Error.Conflict(
            "RuntimeIntelligence.EnvironmentDriftReport.AlreadyReviewed",
            "Environment drift report '{0}' has already been reviewed.",
            reportId);

    /// <summary>Relatório de drift já está marcado como stale.</summary>
    public static Error DriftReportAlreadyStale(string reportId)
        => Error.Conflict(
            "RuntimeIntelligence.EnvironmentDriftReport.AlreadyStale",
            "Environment drift report '{0}' is already marked as stale.",
            reportId);

    /// <summary>Playbook operacional não encontrado pelo identificador informado.</summary>
    public static Error PlaybookNotFound(string id)
        => Error.NotFound(
            "RuntimeIntelligence.OperationalPlaybook.NotFound",
            "Operational playbook '{0}' was not found.",
            id);

    /// <summary>Transição de estado inválida para o playbook operacional.</summary>
    public static Error PlaybookInvalidTransition(string id, string from, string to)
        => Error.Validation(
            "RuntimeIntelligence.OperationalPlaybook.InvalidTransition",
            "Cannot transition playbook '{0}' from '{1}' to '{2}'.",
            id, from, to);

    /// <summary>Execução de playbook não encontrada pelo identificador informado.</summary>
    public static Error PlaybookExecutionNotFound(string id)
        => Error.NotFound(
            "RuntimeIntelligence.PlaybookExecution.NotFound",
            "Playbook execution '{0}' was not found.",
            id);

    /// <summary>Playbook não está ativo — apenas playbooks ativos podem ser executados.</summary>
    public static Error PlaybookNotActive(string id)
        => Error.Validation(
            "RuntimeIntelligence.OperationalPlaybook.NotActive",
            "Playbook '{0}' is not active. Only active playbooks can be executed.",
            id);
}
