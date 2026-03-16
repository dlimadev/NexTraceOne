using Ardalis.GuardClauses;
using NexTraceOne.BuildingBlocks.Core;
using NexTraceOne.BuildingBlocks.Core.Primitives;
using NexTraceOne.OperationalIntelligence.Domain.Incidents.Enums;

namespace NexTraceOne.OperationalIntelligence.Domain.Incidents.Entities;

/// <summary>
/// Entidade que regista cada ação executada sobre um workflow de mitigação.
/// Garante rastreabilidade completa do ciclo de vida do workflow.
/// </summary>
public sealed class MitigationWorkflowActionLog : AuditableEntity<MitigationWorkflowActionLogId>
{
    private MitigationWorkflowActionLog() { }

    /// <summary>Id do workflow onde a ação foi executada.</summary>
    public Guid WorkflowId { get; private set; }

    /// <summary>Id do incidente associado.</summary>
    public string IncidentId { get; private set; } = string.Empty;

    /// <summary>Nome da ação executada.</summary>
    public string Action { get; private set; } = string.Empty;

    /// <summary>Novo estado após a ação.</summary>
    public MitigationWorkflowStatus NewStatus { get; private set; }

    /// <summary>Quem executou a ação.</summary>
    public string? PerformedBy { get; private set; }

    /// <summary>Razão da ação.</summary>
    public string? Reason { get; private set; }

    /// <summary>Notas adicionais.</summary>
    public string? Notes { get; private set; }

    /// <summary>Data/hora UTC da execução.</summary>
    public DateTimeOffset PerformedAt { get; private set; }

    /// <summary>Factory method para criação de um MitigationWorkflowActionLog.</summary>
    public static MitigationWorkflowActionLog Create(
        MitigationWorkflowActionLogId id,
        Guid workflowId,
        string incidentId,
        string action,
        MitigationWorkflowStatus newStatus,
        string? performedBy,
        string? reason,
        string? notes,
        DateTimeOffset performedAt)
    {
        Guard.Against.NullOrWhiteSpace(incidentId);
        Guard.Against.NullOrWhiteSpace(action);

        return new MitigationWorkflowActionLog
        {
            Id = id,
            WorkflowId = workflowId,
            IncidentId = incidentId,
            Action = action,
            NewStatus = newStatus,
            PerformedBy = performedBy,
            Reason = reason,
            Notes = notes,
            PerformedAt = performedAt,
        };
    }
}

/// <summary>Identificador fortemente tipado de MitigationWorkflowActionLog.</summary>
public sealed record MitigationWorkflowActionLogId(Guid Value) : TypedIdBase(Value)
{
    /// <summary>Cria novo Id com Guid gerado automaticamente.</summary>
    public static MitigationWorkflowActionLogId New() => new(Guid.NewGuid());

    /// <summary>Cria Id a partir de Guid existente.</summary>
    public static MitigationWorkflowActionLogId From(Guid id) => new(id);
}
