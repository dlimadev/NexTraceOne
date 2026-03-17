using Ardalis.GuardClauses;
using NexTraceOne.BuildingBlocks.Core;
using NexTraceOne.BuildingBlocks.Core.Primitives;
using NexTraceOne.BuildingBlocks.Core.StronglyTypedIds;
using NexTraceOne.OperationalIntelligence.Domain.Incidents.Enums;

namespace NexTraceOne.OperationalIntelligence.Domain.Incidents.Entities;

/// <summary>
/// Entidade que regista o resultado de uma validação pós-mitigação.
/// Associa checks individuais, resultado observado e quem validou.
/// </summary>
public sealed class MitigationValidationLog : AuditableEntity<MitigationValidationLogId>
{
    private MitigationValidationLog() { }

    /// <summary>Id do incidente associado.</summary>
    public string IncidentId { get; private set; } = string.Empty;

    /// <summary>Id do workflow validado.</summary>
    public Guid WorkflowId { get; private set; }

    /// <summary>Estado da validação.</summary>
    public ValidationStatus Status { get; private set; }

    /// <summary>Resultado observado após mitigação.</summary>
    public string? ObservedOutcome { get; private set; }

    /// <summary>Quem realizou a validação.</summary>
    public string? ValidatedBy { get; private set; }

    /// <summary>Data/hora UTC da validação.</summary>
    public DateTimeOffset ValidatedAt { get; private set; }

    /// <summary>Checks individuais da validação (JSON).</summary>
    public string? ChecksJson { get; private set; }

    /// <summary>Factory method para criação de um MitigationValidationLog.</summary>
    public static MitigationValidationLog Create(
        MitigationValidationLogId id,
        string incidentId,
        Guid workflowId,
        ValidationStatus status,
        string? observedOutcome,
        string? validatedBy,
        DateTimeOffset validatedAt,
        string? checksJson = null)
    {
        Guard.Against.NullOrWhiteSpace(incidentId);

        return new MitigationValidationLog
        {
            Id = id,
            IncidentId = incidentId,
            WorkflowId = workflowId,
            Status = status,
            ObservedOutcome = observedOutcome,
            ValidatedBy = validatedBy,
            ValidatedAt = validatedAt,
            ChecksJson = checksJson,
        };
    }
}

/// <summary>Identificador fortemente tipado de MitigationValidationLog.</summary>
public sealed record MitigationValidationLogId(Guid Value) : TypedIdBase(Value)
{
    /// <summary>Cria novo Id com Guid gerado automaticamente.</summary>
    public static MitigationValidationLogId New() => new(Guid.NewGuid());

    /// <summary>Cria Id a partir de Guid existente.</summary>
    public static MitigationValidationLogId From(Guid id) => new(id);
}
