using NexTraceOne.BuildingBlocks.Domain;
using NexTraceOne.BuildingBlocks.Domain.Primitives;

namespace NexTraceOne.EngineeringGraph.Domain.Entities;

/// <summary>
/// Aggregate Root / Entidade do módulo EngineeringGraph.
/// TODO: Implementar regras de domínio, invariantes e domain events de ConsumerAsset.
/// </summary>
public sealed class ConsumerAsset : AuditableEntity<ConsumerAssetId>
{
    // TODO: Implementar propriedades, construtor privado e factory methods
    private ConsumerAsset() { }
}

/// <summary>Identificador fortemente tipado de ConsumerAsset.</summary>
public sealed record ConsumerAssetId(Guid Value) : TypedIdBase(Value)
{
    /// <summary>Cria novo Id com Guid gerado automaticamente.</summary>
    public static ConsumerAssetId New() => new(Guid.NewGuid());

    /// <summary>Cria Id a partir de Guid existente.</summary>
    public static ConsumerAssetId From(Guid id) => new(id);
}
