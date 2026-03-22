using NexTraceOne.BuildingBlocks.Core.Primitives;
using NexTraceOne.BuildingBlocks.Core.StronglyTypedIds;
using NexTraceOne.OperationalIntelligence.Domain.Automation.Enums;

namespace NexTraceOne.OperationalIntelligence.Domain.Automation.Entities;

/// <summary>
/// Identificador fortemente tipado para AutomationValidationRecord.
/// </summary>
public sealed record AutomationValidationRecordId(Guid Value) : TypedIdBase(Value);

/// <summary>
/// Registo de validação pós-execução de um workflow de automação.
/// Imutável após criação — representa o resultado observado da execução.
/// </summary>
public sealed class AutomationValidationRecord : Entity<AutomationValidationRecordId>
{
    /// <summary>Identificador do workflow ao qual esta validação pertence.</summary>
    public AutomationWorkflowRecordId WorkflowId { get; private init; } = null!;

    /// <summary>Resultado observado da execução do workflow.</summary>
    public AutomationOutcome Outcome { get; private init; }

    /// <summary>Utilizador que realizou a validação.</summary>
    public string ValidatedBy { get; private init; } = string.Empty;

    /// <summary>Notas adicionais sobre a validação.</summary>
    public string Notes { get; private init; } = string.Empty;

    /// <summary>Resultado detalhado em texto livre.</summary>
    public string? ObservedOutcome { get; private init; }

    /// <summary>Data/hora da validação.</summary>
    public DateTimeOffset ValidatedAt { get; private init; }

    /// <summary>Data/hora de criação do registo.</summary>
    public DateTimeOffset CreatedAt { get; private init; }

    /// <summary>Construtor privado para EF Core.</summary>
    private AutomationValidationRecord() { }

    /// <summary>
    /// Cria um novo registo de validação de automação.
    /// </summary>
    public static AutomationValidationRecord Create(
        AutomationWorkflowRecordId workflowId,
        AutomationOutcome outcome,
        string validatedBy,
        string notes,
        string? observedOutcome,
        DateTimeOffset utcNow)
    {
        return new AutomationValidationRecord
        {
            Id = new AutomationValidationRecordId(Guid.NewGuid()),
            WorkflowId = workflowId,
            Outcome = outcome,
            ValidatedBy = validatedBy,
            Notes = notes,
            ObservedOutcome = observedOutcome,
            ValidatedAt = utcNow,
            CreatedAt = utcNow
        };
    }
}
