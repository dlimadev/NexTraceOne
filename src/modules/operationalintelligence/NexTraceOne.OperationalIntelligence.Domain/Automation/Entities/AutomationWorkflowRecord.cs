using NexTraceOne.BuildingBlocks.Core.Primitives;
using NexTraceOne.BuildingBlocks.Core.StronglyTypedIds;
using NexTraceOne.OperationalIntelligence.Domain.Automation.Enums;
using NexTraceOne.OperationalIntelligence.Domain.Incidents.Enums;

namespace NexTraceOne.OperationalIntelligence.Domain.Automation.Entities;

/// <summary>
/// Identificador fortemente tipado para AutomationWorkflowRecord.
/// </summary>
public sealed record AutomationWorkflowRecordId(Guid Value) : TypedIdBase(Value);

/// <summary>
/// Registo persistido de um workflow de automação operacional.
/// Isolado por tenant. Dados base são imutáveis; status é mutável conforme transições de estado.
/// </summary>
public sealed class AutomationWorkflowRecord : Entity<AutomationWorkflowRecordId>
{
    /// <summary>Identificador da ação do catálogo de automação.</summary>
    public string ActionId { get; private init; } = string.Empty;

    /// <summary>Serviço associado, se aplicável.</summary>
    public string? ServiceId { get; private init; }

    /// <summary>Incidente associado, se aplicável.</summary>
    public string? IncidentId { get; private init; }

    /// <summary>Alteração associada, se aplicável.</summary>
    public string? ChangeId { get; private init; }

    /// <summary>Justificativa para a execução do workflow.</summary>
    public string Rationale { get; private init; } = string.Empty;

    /// <summary>Utilizador que solicitou o workflow.</summary>
    public string RequestedBy { get; private init; } = string.Empty;

    /// <summary>Escopo alvo do workflow.</summary>
    public string? TargetScope { get; private init; }

    /// <summary>Ambiente alvo do workflow.</summary>
    public string? TargetEnvironment { get; private init; }

    /// <summary>Estado atual do workflow.</summary>
    public AutomationWorkflowStatus Status { get; private set; }

    /// <summary>Estado de aprovação do workflow.</summary>
    public AutomationApprovalStatus ApprovalStatus { get; private set; }

    /// <summary>Utilizador que aprovou o workflow, se aprovado.</summary>
    public string? ApprovedBy { get; private set; }

    /// <summary>Data/hora de aprovação, se aprovado.</summary>
    public DateTimeOffset? ApprovedAt { get; private set; }

    /// <summary>Nível de risco da ação associada.</summary>
    public RiskLevel RiskLevel { get; private init; }

    /// <summary>Data/hora de criação do registo.</summary>
    public DateTimeOffset CreatedAt { get; private init; }

    /// <summary>Data/hora da última atualização do registo.</summary>
    public DateTimeOffset UpdatedAt { get; private set; }

    /// <summary>Token de concorrência otimista (PostgreSQL xmin).</summary>
    public uint RowVersion { get; set; }

    /// <summary>Construtor privado para EF Core.</summary>
    private AutomationWorkflowRecord() { }

    /// <summary>
    /// Cria um novo registo de workflow de automação.
    /// </summary>
    public static AutomationWorkflowRecord Create(
        string actionId,
        string? serviceId,
        string? incidentId,
        string? changeId,
        string rationale,
        string requestedBy,
        string? targetScope,
        string? targetEnvironment,
        RiskLevel riskLevel,
        DateTimeOffset utcNow)
    {
        return new AutomationWorkflowRecord
        {
            Id = new AutomationWorkflowRecordId(Guid.NewGuid()),
            ActionId = actionId,
            ServiceId = serviceId,
            IncidentId = incidentId,
            ChangeId = changeId,
            Rationale = rationale,
            RequestedBy = requestedBy,
            TargetScope = targetScope,
            TargetEnvironment = targetEnvironment,
            RiskLevel = riskLevel,
            Status = AutomationWorkflowStatus.Draft,
            ApprovalStatus = AutomationApprovalStatus.Pending,
            CreatedAt = utcNow,
            UpdatedAt = utcNow
        };
    }

    /// <summary>Transiciona o estado do workflow.</summary>
    public void UpdateStatus(AutomationWorkflowStatus newStatus, DateTimeOffset utcNow)
    {
        Status = newStatus;
        UpdatedAt = utcNow;
    }

    /// <summary>Regista a aprovação do workflow.</summary>
    public void Approve(string approvedBy, DateTimeOffset utcNow)
    {
        ApprovedBy = approvedBy;
        ApprovedAt = utcNow;
        ApprovalStatus = AutomationApprovalStatus.Approved;
        UpdatedAt = utcNow;
    }

    /// <summary>Regista a rejeição do workflow.</summary>
    public void Reject(string rejectedBy, DateTimeOffset utcNow)
    {
        ApprovedBy = rejectedBy;
        ApprovedAt = utcNow;
        ApprovalStatus = AutomationApprovalStatus.Rejected;
        Status = AutomationWorkflowStatus.Failed;
        UpdatedAt = utcNow;
    }
}
