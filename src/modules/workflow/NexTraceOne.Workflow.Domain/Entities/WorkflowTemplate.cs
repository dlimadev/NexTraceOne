using NexTraceOne.BuildingBlocks.Domain;
using NexTraceOne.BuildingBlocks.Domain.Primitives;

namespace NexTraceOne.Workflow.Domain.Entities;

/// <summary>
/// Aggregate Root / Entidade do módulo Workflow.
/// TODO: Implementar regras de domínio, invariantes e domain events de WorkflowTemplate.
/// </summary>
public sealed class WorkflowTemplate : AuditableEntity<WorkflowTemplateId>
{
    // TODO: Implementar propriedades, construtor privado e factory methods
    private WorkflowTemplate() { }
}

/// <summary>Identificador fortemente tipado de WorkflowTemplate.</summary>
public sealed record WorkflowTemplateId(Guid Value) : TypedIdBase(Value)
{
    /// <summary>Cria novo Id com Guid gerado automaticamente.</summary>
    public static WorkflowTemplateId New() => new(Guid.NewGuid());

    /// <summary>Cria Id a partir de Guid existente.</summary>
    public static WorkflowTemplateId From(Guid id) => new(id);
}
