using NexTraceOne.BuildingBlocks.Domain;
using NexTraceOne.BuildingBlocks.Domain.Primitives;

namespace NexTraceOne.Workflow.Domain.Entities;

/// <summary>
/// Aggregate Root / Entidade do módulo Workflow.
/// TODO: Implementar regras de domínio, invariantes e domain events de ApprovalDecision.
/// </summary>
public sealed class ApprovalDecision : AuditableEntity<ApprovalDecisionId>
{
    // TODO: Implementar propriedades, construtor privado e factory methods
    private ApprovalDecision() { }
}

/// <summary>Identificador fortemente tipado de ApprovalDecision.</summary>
public sealed record ApprovalDecisionId(Guid Value) : TypedIdBase(Value)
{
    /// <summary>Cria novo Id com Guid gerado automaticamente.</summary>
    public static ApprovalDecisionId New() => new(Guid.NewGuid());

    /// <summary>Cria Id a partir de Guid existente.</summary>
    public static ApprovalDecisionId From(Guid id) => new(id);
}
