using Ardalis.GuardClauses;
using NexTraceOne.BuildingBlocks.Core;
using NexTraceOne.BuildingBlocks.Core.Primitives;
using NexTraceOne.OperationalIntelligence.Domain.Incidents.Enums;

namespace NexTraceOne.OperationalIntelligence.Domain.Incidents.Entities;

/// <summary>
/// Entidade que representa um workflow de mitigação associado a um incidente.
/// Rastreia a progressão desde rascunho até conclusão ou cancelamento,
/// incluindo passos, decisões e resultados.
/// </summary>
public sealed class MitigationWorkflowRecord : AuditableEntity<MitigationWorkflowRecordId>
{
    private MitigationWorkflowRecord() { }

    /// <summary>Identificador do incidente associado (string para compatibilidade com o store).</summary>
    public string IncidentId { get; private set; } = string.Empty;

    /// <summary>Título descritivo do workflow.</summary>
    public string Title { get; private set; } = string.Empty;

    /// <summary>Estado atual do workflow.</summary>
    public MitigationWorkflowStatus Status { get; private set; }

    /// <summary>Tipo de ação de mitigação deste workflow.</summary>
    public MitigationActionType ActionType { get; private set; }

    /// <summary>Nível de risco da ação.</summary>
    public RiskLevel RiskLevel { get; private set; }

    /// <summary>Indica se requer aprovação antes da execução.</summary>
    public bool RequiresApproval { get; private set; }

    /// <summary>Id do runbook vinculado (opcional).</summary>
    public Guid? LinkedRunbookId { get; private set; }

    /// <summary>Quem aprovou o workflow.</summary>
    public string? ApprovedBy { get; private set; }

    /// <summary>Data/hora UTC da aprovação.</summary>
    public DateTimeOffset? ApprovedAt { get; private set; }

    /// <summary>Utilizador que criou o workflow.</summary>
    public string CreatedByUser { get; private set; } = string.Empty;

    /// <summary>Data/hora UTC de início da execução.</summary>
    public DateTimeOffset? StartedAt { get; private set; }

    /// <summary>Data/hora UTC de conclusão.</summary>
    public DateTimeOffset? CompletedAt { get; private set; }

    /// <summary>Resultado final do workflow.</summary>
    public MitigationOutcome? CompletedOutcome { get; private set; }

    /// <summary>Notas da conclusão.</summary>
    public string? CompletedNotes { get; private set; }

    /// <summary>Quem completou o workflow.</summary>
    public string? CompletedBy { get; private set; }

    /// <summary>Passos do workflow (JSON).</summary>
    public string? StepsJson { get; private set; }

    /// <summary>Decisões tomadas no workflow (JSON).</summary>
    public string? DecisionsJson { get; private set; }

    /// <summary>Factory method para criação de um MitigationWorkflowRecord.</summary>
    public static MitigationWorkflowRecord Create(
        MitigationWorkflowRecordId id,
        string incidentId,
        string title,
        MitigationWorkflowStatus status,
        MitigationActionType actionType,
        RiskLevel riskLevel,
        bool requiresApproval,
        string createdByUser,
        Guid? linkedRunbookId = null,
        string? stepsJson = null)
    {
        Guard.Against.NullOrWhiteSpace(incidentId);
        Guard.Against.NullOrWhiteSpace(title);
        Guard.Against.NullOrWhiteSpace(createdByUser);

        return new MitigationWorkflowRecord
        {
            Id = id,
            IncidentId = incidentId,
            Title = title,
            Status = status,
            ActionType = actionType,
            RiskLevel = riskLevel,
            RequiresApproval = requiresApproval,
            CreatedByUser = createdByUser,
            LinkedRunbookId = linkedRunbookId,
            StepsJson = stepsJson,
        };
    }

    /// <summary>Atualiza o estado do workflow.</summary>
    public void UpdateStatus(MitigationWorkflowStatus newStatus) => Status = newStatus;

    /// <summary>Define campos de aprovação.</summary>
    public void SetApproval(string approvedBy, DateTimeOffset approvedAt)
    {
        ApprovedBy = approvedBy;
        ApprovedAt = approvedAt;
    }

    /// <summary>Define campos de início de execução.</summary>
    public void SetStarted(DateTimeOffset startedAt) => StartedAt = startedAt;

    /// <summary>Define campos de conclusão.</summary>
    public void SetCompleted(DateTimeOffset completedAt, MitigationOutcome? outcome, string? notes, string? completedBy)
    {
        CompletedAt = completedAt;
        CompletedOutcome = outcome;
        CompletedNotes = notes;
        CompletedBy = completedBy;
    }

    /// <summary>Define JSON das decisões.</summary>
    public void SetDecisions(string? decisionsJson) => DecisionsJson = decisionsJson;
}

/// <summary>Identificador fortemente tipado de MitigationWorkflowRecord.</summary>
public sealed record MitigationWorkflowRecordId(Guid Value) : TypedIdBase(Value)
{
    /// <summary>Cria novo Id com Guid gerado automaticamente.</summary>
    public static MitigationWorkflowRecordId New() => new(Guid.NewGuid());

    /// <summary>Cria Id a partir de Guid existente.</summary>
    public static MitigationWorkflowRecordId From(Guid id) => new(id);
}
