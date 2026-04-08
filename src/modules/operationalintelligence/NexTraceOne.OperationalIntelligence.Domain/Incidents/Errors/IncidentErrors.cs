using NexTraceOne.BuildingBlocks.Core.Results;

namespace NexTraceOne.OperationalIntelligence.Domain.Incidents.Errors;

/// <summary>
/// Catálogo centralizado de erros do subdomínio Incidents com códigos i18n.
/// Cada erro possui código único para rastreabilidade em logs e documentação.
/// Padrão: Incidents.{Entidade}.{Descrição}
/// </summary>
public static class IncidentErrors
{
    /// <summary>Incidente não encontrado pelo identificador informado.</summary>
    public static Error IncidentNotFound(string incidentId)
        => Error.NotFound(
            "Incidents.Incident.NotFound",
            "Incident '{0}' was not found.",
            incidentId);

    /// <summary>Serviço não encontrado ao consultar incidentes por serviço.</summary>
    public static Error ServiceNotFound(string serviceId)
        => Error.NotFound(
            "Incidents.Service.NotFound",
            "Service '{0}' was not found.",
            serviceId);

    /// <summary>Equipa não encontrada ao consultar incidentes por equipa.</summary>
    public static Error TeamNotFound(string teamId)
        => Error.NotFound(
            "Incidents.Team.NotFound",
            "Team '{0}' was not found.",
            teamId);

    /// <summary>Workflow de mitigação não encontrado pelo identificador informado.</summary>
    public static Error WorkflowNotFound(string workflowId)
        => Error.NotFound(
            "Incidents.Workflow.NotFound",
            "Mitigation workflow '{0}' was not found.",
            workflowId);

    /// <summary>Runbook não encontrado pelo identificador informado.</summary>
    public static Error RunbookNotFound(string runbookId)
        => Error.NotFound(
            "Incidents.Runbook.NotFound",
            "Runbook '{0}' was not found.",
            runbookId);

    /// <summary>Post-Incident Review não encontrado.</summary>
    public static Error PirNotFound(string incidentId)
        => Error.NotFound(
            "Incidents.PIR.NotFound",
            "Post-Incident Review for incident '{0}' was not found.",
            incidentId);

    /// <summary>Post-Incident Review já existe para este incidente.</summary>
    public static Error PirAlreadyExists(string incidentId)
        => Error.Conflict(
            "Incidents.PIR.AlreadyExists",
            "A Post-Incident Review already exists for incident '{0}'.",
            incidentId);

    /// <summary>Post-Incident Review já está concluído e não pode ser progredido.</summary>
    public static Error PirAlreadyCompleted(string reviewId)
        => Error.Conflict(
            "Incidents.PIR.AlreadyCompleted",
            "Post-Incident Review '{0}' is already completed.",
            reviewId);

    /// <summary>Ação inválida para workflow de mitigação.</summary>
    public static Error InvalidWorkflowAction(string action)
        => Error.Validation(
            "Incidents.Workflow.InvalidAction",
            "Action '{0}' is not a valid workflow action.",
            action);

    /// <summary>Narrativa de incidente não encontrada.</summary>
    public static Error NarrativeNotFound(string incidentId)
        => Error.NotFound(
            "Incidents.Narrative.NotFound",
            "No narrative exists for incident '{0}'.",
            incidentId);

    /// <summary>Narrativa de incidente já existe para este incidente.</summary>
    public static Error NarrativeAlreadyExists(string incidentId)
        => Error.Conflict(
            "Incidents.Narrative.AlreadyExists",
            "A narrative already exists for incident '{0}'.",
            incidentId);
}
