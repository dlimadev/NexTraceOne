using NexTraceOne.BuildingBlocks.Domain;
using NexTraceOne.BuildingBlocks.Domain.Primitives;

namespace NexTraceOne.Workflow.Domain.Entities;

/// <summary>
/// Aggregate Root / Entidade do módulo Workflow.
/// TODO: Implementar regras de domínio, invariantes e domain events de SlaPolicy.
/// </summary>
public sealed class SlaPolicy : AuditableEntity<SlaPolicyId>
{
    // TODO: Implementar propriedades, construtor privado e factory methods
    private SlaPolicy() { }
}

/// <summary>Identificador fortemente tipado de SlaPolicy.</summary>
public sealed record SlaPolicyId(Guid Value) : TypedIdBase(Value)
{
    /// <summary>Cria novo Id com Guid gerado automaticamente.</summary>
    public static SlaPolicyId New() => new(Guid.NewGuid());

    /// <summary>Cria Id a partir de Guid existente.</summary>
    public static SlaPolicyId From(Guid id) => new(id);
}
