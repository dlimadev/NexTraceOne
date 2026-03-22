using NexTraceOne.BuildingBlocks.Core.Primitives;
using NexTraceOne.BuildingBlocks.Core.StronglyTypedIds;
using NexTraceOne.OperationalIntelligence.Domain.Automation.Enums;

namespace NexTraceOne.OperationalIntelligence.Domain.Automation.Entities;

/// <summary>
/// Identificador fortemente tipado para AutomationAuditRecord.
/// </summary>
public sealed record AutomationAuditRecordId(Guid Value) : TypedIdBase(Value);

/// <summary>
/// Entrada imutável na trilha de auditoria de um workflow de automação.
/// Cada evento relevante no ciclo de vida do workflow gera um registo de auditoria.
/// </summary>
public sealed class AutomationAuditRecord : Entity<AutomationAuditRecordId>
{
    /// <summary>Identificador do workflow ao qual este evento pertence.</summary>
    public AutomationWorkflowRecordId WorkflowId { get; private init; } = null!;

    /// <summary>Ação que originou este registo de auditoria.</summary>
    public AutomationAuditAction Action { get; private init; }

    /// <summary>Utilizador ou sistema que realizou a ação.</summary>
    public string Actor { get; private init; } = string.Empty;

    /// <summary>Detalhes descritivos do evento.</summary>
    public string Details { get; private init; } = string.Empty;

    /// <summary>Serviço associado ao evento, se aplicável.</summary>
    public string? ServiceId { get; private init; }

    /// <summary>Equipa associada ao evento, se aplicável.</summary>
    public string? TeamId { get; private init; }

    /// <summary>Data/hora em que o evento ocorreu.</summary>
    public DateTimeOffset OccurredAt { get; private init; }

    /// <summary>Data/hora de criação do registo.</summary>
    public DateTimeOffset CreatedAt { get; private init; }

    /// <summary>Construtor privado para EF Core.</summary>
    private AutomationAuditRecord() { }

    /// <summary>
    /// Cria um novo registo de auditoria de automação.
    /// </summary>
    public static AutomationAuditRecord Create(
        AutomationWorkflowRecordId workflowId,
        AutomationAuditAction action,
        string actor,
        string details,
        DateTimeOffset utcNow,
        string? serviceId = null,
        string? teamId = null)
    {
        return new AutomationAuditRecord
        {
            Id = new AutomationAuditRecordId(Guid.NewGuid()),
            WorkflowId = workflowId,
            Action = action,
            Actor = actor,
            Details = details,
            ServiceId = serviceId,
            TeamId = teamId,
            OccurredAt = utcNow,
            CreatedAt = utcNow
        };
    }
}
