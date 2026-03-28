using NexTraceOne.OperationalIntelligence.Application.Incidents.Features.GetIncidentCorrelation;
using NexTraceOne.OperationalIntelligence.Application.Incidents.Features.GetIncidentDetail;
using NexTraceOne.OperationalIntelligence.Application.Incidents.Features.GetIncidentEvidence;
using NexTraceOne.OperationalIntelligence.Application.Incidents.Features.GetIncidentMitigation;
using NexTraceOne.OperationalIntelligence.Application.Incidents.Features.GetMitigationRecommendations;
using NexTraceOne.OperationalIntelligence.Application.Incidents.Features.GetMitigationValidation;
using NexTraceOne.OperationalIntelligence.Application.Incidents.Features.GetMitigationWorkflow;
using NexTraceOne.OperationalIntelligence.Application.Incidents.Features.ListIncidents;
using NexTraceOne.OperationalIntelligence.Application.Incidents.Features.UpdateMitigationWorkflowAction;
using NexTraceOne.OperationalIntelligence.Domain.Incidents.Enums;

namespace NexTraceOne.OperationalIntelligence.Application.Incidents.Abstractions;

/// <summary>
/// Abstração central de acesso a dados de incidentes, workflows de mitigação,
/// validações e runbooks. Desacopla os handlers de qualquer implementação
/// concreta de persistência.
/// </summary>
public interface IIncidentStore
{
    // ── Incidents ────────────────────────────────────────────────────────

    /// <summary>Cria um novo incidente e retorna o identificador gerado.</summary>
    CreateIncidentResult CreateIncident(CreateIncidentInput input);

    /// <summary>Obtém os dados mínimos necessários para recomputar a correlação.</summary>
    IncidentCorrelationContext? GetIncidentCorrelationContext(string incidentId);

    /// <summary>Persiste o resultado computado de correlação no incidente.</summary>
    void SaveIncidentCorrelation(string incidentId, GetIncidentCorrelation.Response correlation);

    /// <summary>Verifica se o incidente existe.</summary>
    bool IncidentExists(string incidentId);

    /// <summary>Retorna itens da listagem de incidentes (já como DTOs do handler).</summary>
    IReadOnlyList<ListIncidents.IncidentListItem> GetIncidentListItems();

    /// <summary>Retorna o detalhe consolidado do incidente.</summary>
    GetIncidentDetail.Response? GetIncidentDetail(string incidentId);

    /// <summary>Retorna a correlação do incidente.</summary>
    GetIncidentCorrelation.Response? GetIncidentCorrelation(string incidentId);

    /// <summary>Retorna as evidências do incidente.</summary>
    GetIncidentEvidence.Response? GetIncidentEvidence(string incidentId);

    /// <summary>Retorna as informações de mitigação do incidente.</summary>
    GetIncidentMitigation.Response? GetIncidentMitigation(string incidentId);

    // ── Mitigation Workflows ─────────────────────────────────────────────

    /// <summary>Retorna as recomendações de mitigação.</summary>
    GetMitigationRecommendations.Response? GetMitigationRecommendations(string incidentId);

    /// <summary>Retorna o detalhe de um workflow de mitigação.</summary>
    GetMitigationWorkflow.Response? GetMitigationWorkflow(string incidentId, string workflowId);

    /// <summary>Executa uma ação sobre um workflow de mitigação.</summary>
    UpdateMitigationWorkflowAction.Response? UpdateMitigationWorkflowAction(
        string incidentId,
        string workflowId,
        string action,
        MitigationWorkflowStatus newStatus,
        string? performedBy,
        string? reason,
        string? notes);

    /// <summary>Retorna o estado de validação pós-mitigação de um workflow.</summary>
    GetMitigationValidation.Response? GetMitigationValidation(string incidentId, string workflowId);

    // ── Runbooks e Mitigation Write/History — removidos: tratados por repositórios dedicados ─
}

/// <summary>Dados de entrada mínimos para criação de incidente.</summary>
public sealed record CreateIncidentInput(
    string Title,
    string Description,
    IncidentType IncidentType,
    IncidentSeverity Severity,
    string ServiceId,
    string ServiceDisplayName,
    string OwnerTeam,
    string? ImpactedDomain,
    string Environment,
    DateTimeOffset? DetectedAtUtc,
    Guid? TenantId = null,
    Guid? EnvironmentId = null);

/// <summary>Resultado da criação de incidente.</summary>
public sealed record CreateIncidentResult(
    Guid IncidentId,
    string Reference,
    DateTimeOffset CreatedAt);

/// <summary>Contexto operacional necessário para recomputar correlação.</summary>
public sealed record IncidentCorrelationContext(
    Guid IncidentId,
    string ServiceId,
    string ServiceDisplayName,
    string Environment,
    DateTimeOffset DetectedAtUtc);
