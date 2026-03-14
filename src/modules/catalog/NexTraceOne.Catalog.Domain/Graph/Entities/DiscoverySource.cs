using Ardalis.GuardClauses;
using NexTraceOne.BuildingBlocks.Core;
using NexTraceOne.BuildingBlocks.Core.Primitives;

namespace NexTraceOne.Catalog.Domain.Graph.Entities;

/// <summary>
/// Fonte que originou a descoberta ou inferência de um ativo do grafo.
/// </summary>
public sealed class DiscoverySource : Entity<DiscoverySourceId>
{
    private DiscoverySource() { }

    /// <summary>Tipo da fonte, como Manual, OpenTelemetry ou CatalogImport.</summary>
    public string SourceType { get; private set; } = string.Empty;

    /// <summary>Referência externa associada à descoberta.</summary>
    public string ExternalReference { get; private set; } = string.Empty;

    /// <summary>Data/hora UTC em que a descoberta foi registrada.</summary>
    public DateTimeOffset DiscoveredAt { get; private set; }

    /// <summary>Score de confiança da descoberta.</summary>
    public decimal ConfidenceScore { get; private set; }

    /// <summary>Cria uma nova fonte de descoberta validando o score de confiança.</summary>
    public static DiscoverySource Create(string sourceType, string externalReference, DateTimeOffset discoveredAt, decimal confidenceScore)
    {
        if (confidenceScore <= 0 || confidenceScore > 1)
        {
            throw new ArgumentOutOfRangeException(nameof(confidenceScore), "Confidence score must be between 0 and 1.");
        }

        return new DiscoverySource
        {
            Id = DiscoverySourceId.New(),
            SourceType = Guard.Against.NullOrWhiteSpace(sourceType),
            ExternalReference = Guard.Against.NullOrWhiteSpace(externalReference),
            DiscoveredAt = discoveredAt,
            ConfidenceScore = confidenceScore
        };
    }
}

/// <summary>Identificador fortemente tipado de DiscoverySource.</summary>
public sealed record DiscoverySourceId(Guid Value) : TypedIdBase(Value)
{
    /// <summary>Cria novo Id com Guid gerado automaticamente.</summary>
    public static DiscoverySourceId New() => new(Guid.NewGuid());

    /// <summary>Cria Id a partir de Guid existente.</summary>
    public static DiscoverySourceId From(Guid id) => new(id);
}
