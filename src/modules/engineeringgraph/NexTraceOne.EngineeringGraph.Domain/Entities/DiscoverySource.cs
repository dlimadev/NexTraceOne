using NexTraceOne.BuildingBlocks.Domain;
using NexTraceOne.BuildingBlocks.Domain.Primitives;

namespace NexTraceOne.EngineeringGraph.Domain.Entities;

/// <summary>
/// Aggregate Root / Entidade do módulo EngineeringGraph.
/// TODO: Implementar regras de domínio, invariantes e domain events de DiscoverySource.
/// </summary>
public sealed class DiscoverySource : AuditableEntity<DiscoverySourceId>
{
    // TODO: Implementar propriedades, construtor privado e factory methods
    private DiscoverySource() { }
}

/// <summary>Identificador fortemente tipado de DiscoverySource.</summary>
public sealed record DiscoverySourceId(Guid Value) : TypedIdBase(Value)
{
    /// <summary>Cria novo Id com Guid gerado automaticamente.</summary>
    public static DiscoverySourceId New() => new(Guid.NewGuid());

    /// <summary>Cria Id a partir de Guid existente.</summary>
    public static DiscoverySourceId From(Guid id) => new(id);
}
