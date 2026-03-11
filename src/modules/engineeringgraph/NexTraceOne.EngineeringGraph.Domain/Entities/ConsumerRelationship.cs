using NexTraceOne.BuildingBlocks.Domain;
using NexTraceOne.BuildingBlocks.Domain.Primitives;

namespace NexTraceOne.EngineeringGraph.Domain.Entities;

/// <summary>
/// Aggregate Root / Entidade do módulo EngineeringGraph.
/// TODO: Implementar regras de domínio, invariantes e domain events de ConsumerRelationship.
/// </summary>
public sealed class ConsumerRelationship : AuditableEntity<ConsumerRelationshipId>
{
    // TODO: Implementar propriedades, construtor privado e factory methods
    private ConsumerRelationship() { }
}

/// <summary>Identificador fortemente tipado de ConsumerRelationship.</summary>
public sealed record ConsumerRelationshipId(Guid Value) : TypedIdBase(Value)
{
    /// <summary>Cria novo Id com Guid gerado automaticamente.</summary>
    public static ConsumerRelationshipId New() => new(Guid.NewGuid());

    /// <summary>Cria Id a partir de Guid existente.</summary>
    public static ConsumerRelationshipId From(Guid id) => new(id);
}
