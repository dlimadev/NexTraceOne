using NexTraceOne.OperationalIntelligence.Application.Incidents.Features.CreateMitigationWorkflow;
using NexTraceOne.OperationalIntelligence.Application.Incidents.Features.GetIncidentCorrelation;
using NexTraceOne.OperationalIntelligence.Application.Incidents.Features.GetIncidentDetail;
using NexTraceOne.OperationalIntelligence.Application.Incidents.Features.GetIncidentEvidence;
using NexTraceOne.OperationalIntelligence.Application.Incidents.Features.GetIncidentMitigation;
using NexTraceOne.OperationalIntelligence.Application.Incidents.Features.GetMitigationHistory;
using NexTraceOne.OperationalIntelligence.Application.Incidents.Features.GetMitigationRecommendations;
using NexTraceOne.OperationalIntelligence.Application.Incidents.Features.GetMitigationValidation;
using NexTraceOne.OperationalIntelligence.Application.Incidents.Features.GetMitigationWorkflow;
using NexTraceOne.OperationalIntelligence.Application.Incidents.Features.GetRunbookDetail;
using NexTraceOne.OperationalIntelligence.Application.Incidents.Features.ListIncidents;
using NexTraceOne.OperationalIntelligence.Application.Incidents.Features.ListRunbooks;
using NexTraceOne.OperationalIntelligence.Application.Incidents.Features.RecordMitigationValidation;
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

    /// <summary>Cria um novo workflow de mitigação e retorna o ID gerado.</summary>
    CreateMitigationWorkflow.Response CreateMitigationWorkflow(
        string incidentId,
        string title,
        MitigationActionType actionType,
        RiskLevel riskLevel,
        bool requiresApproval,
        Guid? linkedRunbookId,
        IReadOnlyList<CreateMitigationWorkflow.CreateStepDto>? steps);

    /// <summary>Executa uma ação sobre um workflow de mitigação.</summary>
    UpdateMitigationWorkflowAction.Response? UpdateMitigationWorkflowAction(
        string incidentId,
        string workflowId,
        string action,
        MitigationWorkflowStatus newStatus,
        string? performedBy,
        string? reason,
        string? notes);

    /// <summary>Retorna o histórico de mitigação do incidente.</summary>
    GetMitigationHistory.Response? GetMitigationHistory(string incidentId);

    /// <summary>Retorna o estado de validação pós-mitigação de um workflow.</summary>
    GetMitigationValidation.Response? GetMitigationValidation(string incidentId, string workflowId);

    /// <summary>Regista o resultado de uma validação pós-mitigação.</summary>
    RecordMitigationValidation.Response? RecordMitigationValidation(
        string incidentId,
        string workflowId,
        ValidationStatus status,
        string? observedOutcome,
        string? validatedBy,
        IReadOnlyList<RecordMitigationValidation.ValidationCheckInput>? checks);

    // ── Runbooks ─────────────────────────────────────────────────────────

    /// <summary>Retorna a lista de runbooks.</summary>
    IReadOnlyList<ListRunbooks.RunbookSummaryDto> GetRunbooks();

    /// <summary>Retorna o detalhe de um runbook.</summary>
    GetRunbookDetail.Response? GetRunbookDetail(string runbookId);
}
