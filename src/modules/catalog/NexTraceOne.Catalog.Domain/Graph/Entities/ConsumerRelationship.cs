using Ardalis.GuardClauses;
using NexTraceOne.BuildingBlocks.Core;
using NexTraceOne.BuildingBlocks.Core.Primitives;
using NexTraceOne.BuildingBlocks.Core.StronglyTypedIds;

namespace NexTraceOne.Catalog.Domain.Graph.Entities;

/// <summary>
/// Relação entre um consumidor e uma API do grafo de engenharia.
/// </summary>
public sealed class ConsumerRelationship : Entity<ConsumerRelationshipId>
{
    private ConsumerRelationship() { }

    /// <summary>Identificador do consumidor relacionado.</summary>
    public ConsumerAssetId ConsumerAssetId { get; private set; } = ConsumerAssetId.New();

    /// <summary>Nome do consumidor para consultas rápidas.</summary>
    public string ConsumerName { get; private set; } = string.Empty;

    /// <summary>Tipo da fonte que confirmou a relação.</summary>
    public string SourceType { get; private set; } = string.Empty;

    /// <summary>Score de confiança atual da relação.</summary>
    public decimal ConfidenceScore { get; private set; }

    /// <summary>Primeira observação da relação no grafo.</summary>
    public DateTimeOffset FirstObservedAt { get; private set; }

    /// <summary>Última observação da relação no grafo.</summary>
    public DateTimeOffset LastObservedAt { get; private set; }

    /// <summary>Cria uma nova relação entre consumidor e API.</summary>
    public static ConsumerRelationship Create(ConsumerAsset consumerAsset, DiscoverySource discoverySource, DateTimeOffset observedAt)
        => new()
        {
            Id = ConsumerRelationshipId.New(),
            ConsumerAssetId = consumerAsset.Id,
            ConsumerName = Guard.Against.NullOrWhiteSpace(consumerAsset.Name),
            SourceType = Guard.Against.NullOrWhiteSpace(discoverySource.SourceType),
            ConfidenceScore = discoverySource.ConfidenceScore,
            FirstObservedAt = observedAt,
            LastObservedAt = observedAt
        };

    /// <summary>Atualiza uma relação existente com nova observação e confiança.</summary>
    public void Refresh(DiscoverySource discoverySource, DateTimeOffset observedAt)
    {
        SourceType = Guard.Against.NullOrWhiteSpace(discoverySource.SourceType);
        ConfidenceScore = discoverySource.ConfidenceScore;
        LastObservedAt = observedAt;
    }
}

/// <summary>Identificador fortemente tipado de ConsumerRelationship.</summary>
public sealed record ConsumerRelationshipId(Guid Value) : TypedIdBase(Value)
{
    /// <summary>Cria novo Id com Guid gerado automaticamente.</summary>
    public static ConsumerRelationshipId New() => new(Guid.NewGuid());

    /// <summary>Cria Id a partir de Guid existente.</summary>
    public static ConsumerRelationshipId From(Guid id) => new(id);
}
