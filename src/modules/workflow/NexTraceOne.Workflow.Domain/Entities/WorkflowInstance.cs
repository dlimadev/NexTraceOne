using NexTraceOne.BuildingBlocks.Domain;
using NexTraceOne.BuildingBlocks.Domain.Primitives;

namespace NexTraceOne.Workflow.Domain.Entities;

/// <summary>
/// Aggregate Root / Entidade do módulo Workflow.
/// TODO: Implementar regras de domínio, invariantes e domain events de WorkflowInstance.
/// </summary>
public sealed class WorkflowInstance : AuditableEntity<WorkflowInstanceId>
{
    // TODO: Implementar propriedades, construtor privado e factory methods
    private WorkflowInstance() { }
}

/// <summary>Identificador fortemente tipado de WorkflowInstance.</summary>
public sealed record WorkflowInstanceId(Guid Value) : TypedIdBase(Value)
{
    /// <summary>Cria novo Id com Guid gerado automaticamente.</summary>
    public static WorkflowInstanceId New() => new(Guid.NewGuid());

    /// <summary>Cria Id a partir de Guid existente.</summary>
    public static WorkflowInstanceId From(Guid id) => new(id);
}
