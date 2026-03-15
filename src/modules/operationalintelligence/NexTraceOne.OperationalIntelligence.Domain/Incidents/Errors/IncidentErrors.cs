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
}
