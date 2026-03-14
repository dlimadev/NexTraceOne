using Ardalis.GuardClauses;
using NexTraceOne.BuildingBlocks.Core;
using NexTraceOne.BuildingBlocks.Core.Primitives;

namespace NexTraceOne.EngineeringGraph.Domain.Entities;

/// <summary>
/// Consumidor conhecido de uma API no grafo de engenharia.
/// </summary>
public sealed class ConsumerAsset : Entity<ConsumerAssetId>
{
    private ConsumerAsset() { }

    /// <summary>Nome único do consumidor.</summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>Tipo do consumidor, como Service ou Job.</summary>
    public string Kind { get; private set; } = string.Empty;

    /// <summary>Ambiente principal do consumidor.</summary>
    public string Environment { get; private set; } = string.Empty;

    /// <summary>Cria um novo consumidor conhecido do grafo.</summary>
    public static ConsumerAsset Create(string name, string kind, string environment)
        => new()
        {
            Id = ConsumerAssetId.New(),
            Name = Guard.Against.NullOrWhiteSpace(name),
            Kind = Guard.Against.NullOrWhiteSpace(kind),
            Environment = Guard.Against.NullOrWhiteSpace(environment)
        };
}

/// <summary>Identificador fortemente tipado de ConsumerAsset.</summary>
public sealed record ConsumerAssetId(Guid Value) : TypedIdBase(Value)
{
    /// <summary>Cria novo Id com Guid gerado automaticamente.</summary>
    public static ConsumerAssetId New() => new(Guid.NewGuid());

    /// <summary>Cria Id a partir de Guid existente.</summary>
    public static ConsumerAssetId From(Guid id) => new(id);
}
