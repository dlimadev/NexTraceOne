using NexTraceOne.BuildingBlocks.Domain;
using NexTraceOne.BuildingBlocks.Domain.Primitives;

namespace NexTraceOne.Workflow.Domain.Entities;

/// <summary>
/// Aggregate Root / Entidade do módulo Workflow.
/// TODO: Implementar regras de domínio, invariantes e domain events de WorkflowStage.
/// </summary>
public sealed class WorkflowStage : AuditableEntity<WorkflowStageId>
{
    // TODO: Implementar propriedades, construtor privado e factory methods
    private WorkflowStage() { }
}

/// <summary>Identificador fortemente tipado de WorkflowStage.</summary>
public sealed record WorkflowStageId(Guid Value) : TypedIdBase(Value)
{
    /// <summary>Cria novo Id com Guid gerado automaticamente.</summary>
    public static WorkflowStageId New() => new(Guid.NewGuid());

    /// <summary>Cria Id a partir de Guid existente.</summary>
    public static WorkflowStageId From(Guid id) => new(id);
}
