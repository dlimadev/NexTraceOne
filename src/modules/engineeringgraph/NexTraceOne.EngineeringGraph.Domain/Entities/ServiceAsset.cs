using NexTraceOne.BuildingBlocks.Domain;
using NexTraceOne.BuildingBlocks.Domain.Primitives;

namespace NexTraceOne.EngineeringGraph.Domain.Entities;

/// <summary>
/// Aggregate Root / Entidade do módulo EngineeringGraph.
/// TODO: Implementar regras de domínio, invariantes e domain events de ServiceAsset.
/// </summary>
public sealed class ServiceAsset : AuditableEntity<ServiceAssetId>
{
    // TODO: Implementar propriedades, construtor privado e factory methods
    private ServiceAsset() { }
}

/// <summary>Identificador fortemente tipado de ServiceAsset.</summary>
public sealed record ServiceAssetId(Guid Value) : TypedIdBase(Value)
{
    /// <summary>Cria novo Id com Guid gerado automaticamente.</summary>
    public static ServiceAssetId New() => new(Guid.NewGuid());

    /// <summary>Cria Id a partir de Guid existente.</summary>
    public static ServiceAssetId From(Guid id) => new(id);
}
