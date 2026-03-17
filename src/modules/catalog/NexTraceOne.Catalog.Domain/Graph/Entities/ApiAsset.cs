using Ardalis.GuardClauses;
using MediatR;
using NexTraceOne.BuildingBlocks.Core;
using NexTraceOne.BuildingBlocks.Core.Primitives;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.BuildingBlocks.Core.StronglyTypedIds;
using NexTraceOne.Catalog.Domain.Graph.Errors;

namespace NexTraceOne.Catalog.Domain.Graph.Entities;

/// <summary>
/// Aggregate Root que representa uma API publicada no grafo de engenharia.
/// </summary>
public sealed class ApiAsset : AggregateRoot<ApiAssetId>
{
    private readonly List<ConsumerRelationship> _consumerRelationships = [];
    private readonly List<DiscoverySource> _discoverySources = [];

    private ApiAsset() { }

    /// <summary>Nome lógico da API.</summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>Rota principal exposta pela API.</summary>
    public string RoutePattern { get; private set; } = string.Empty;

    /// <summary>Versão semântica atual do ativo.</summary>
    public string Version { get; private set; } = string.Empty;

    /// <summary>Visibilidade da API, como Internal ou Public.</summary>
    public string Visibility { get; private set; } = string.Empty;

    /// <summary>Serviço proprietário da API.</summary>
    public ServiceAsset OwnerService { get; private set; } = null!;

    /// <summary>Relações conhecidas de consumidores da API.</summary>
    public IReadOnlyList<ConsumerRelationship> ConsumerRelationships => _consumerRelationships.AsReadOnly();

    /// <summary>Fontes de descoberta associadas ao ativo.</summary>
    public IReadOnlyList<DiscoverySource> DiscoverySources => _discoverySources.AsReadOnly();

    /// <summary>Registra um novo ativo de API no grafo.</summary>
    public static ApiAsset Register(string name, string routePattern, string version, string visibility, ServiceAsset ownerService)
        => new()
        {
            Id = ApiAssetId.New(),
            Name = Guard.Against.NullOrWhiteSpace(name),
            RoutePattern = Guard.Against.NullOrWhiteSpace(routePattern),
            Version = Guard.Against.NullOrWhiteSpace(version),
            Visibility = Guard.Against.NullOrWhiteSpace(visibility),
            OwnerService = Guard.Against.Null(ownerService)
        };

    /// <summary>Adiciona uma fonte de descoberta ao ativo evitando duplicidade por referência.</summary>
    public Result<Unit> AddDiscoverySource(DiscoverySource discoverySource)
    {
        Guard.Against.Null(discoverySource);

        var duplicateExists = _discoverySources.Any(source =>
            string.Equals(source.SourceType, discoverySource.SourceType, StringComparison.OrdinalIgnoreCase)
            && string.Equals(source.ExternalReference, discoverySource.ExternalReference, StringComparison.OrdinalIgnoreCase));

        if (duplicateExists)
        {
            return CatalogGraphErrors.DuplicateDiscoverySource(discoverySource.SourceType, discoverySource.ExternalReference);
        }

        _discoverySources.Add(discoverySource);
        return Unit.Value;
    }

    /// <summary>Mapeia ou atualiza a relação de consumo de uma API por um consumidor conhecido.</summary>
    public Result<ConsumerRelationship> MapConsumerRelationship(ConsumerAsset consumerAsset, DiscoverySource discoverySource, DateTimeOffset observedAt)
    {
        Guard.Against.Null(consumerAsset);
        Guard.Against.Null(discoverySource);

        var addDiscoverySourceResult = AddDiscoverySource(discoverySource);
        if (addDiscoverySourceResult.IsFailure && addDiscoverySourceResult.Error.Code != "CatalogGraph.DiscoverySource.Duplicate")
        {
            return addDiscoverySourceResult.Error;
        }

        var relationship = _consumerRelationships.SingleOrDefault(item => item.ConsumerAssetId == consumerAsset.Id);
        if (relationship is null)
        {
            relationship = ConsumerRelationship.Create(consumerAsset, discoverySource, observedAt);
            _consumerRelationships.Add(relationship);
            return relationship;
        }

        relationship.Refresh(discoverySource, observedAt);
        return relationship;
    }

    /// <summary>Infere uma dependência a partir de telemetria OpenTelemetry.</summary>
    public Result<ConsumerRelationship> InferDependencyFromOtel(
        string consumerName,
        string environment,
        string externalReference,
        DateTimeOffset observedAt,
        decimal confidenceScore)
    {
        var consumerAsset = ConsumerAsset.Create(consumerName, "Service", environment);
        var discoverySource = DiscoverySource.Create("OpenTelemetry", externalReference, observedAt, confidenceScore);
        return MapConsumerRelationship(consumerAsset, discoverySource, observedAt);
    }

    /// <summary>Indica se o ativo está descomissionado.</summary>
    public bool IsDecommissioned { get; private set; }

    /// <summary>Atualiza metadados do ativo sem alterar o proprietário.</summary>
    public Result<Unit> UpdateMetadata(string name, string routePattern, string version, string visibility)
    {
        if (IsDecommissioned)
        {
            return CatalogGraphErrors.ApiAssetDecommissioned(Id.Value);
        }

        Name = Guard.Against.NullOrWhiteSpace(name);
        RoutePattern = Guard.Against.NullOrWhiteSpace(routePattern);
        Version = Guard.Against.NullOrWhiteSpace(version);
        Visibility = Guard.Against.NullOrWhiteSpace(visibility);
        return Unit.Value;
    }

    /// <summary>Marca o ativo como descomissionado impedindo novos mapeamentos.</summary>
    public Result<Unit> Decommission()
    {
        if (IsDecommissioned)
        {
            return CatalogGraphErrors.ApiAssetDecommissioned(Id.Value);
        }

        IsDecommissioned = true;
        return Unit.Value;
    }

    /// <summary>Valida se uma dependência descoberta possui confiança suficiente.</summary>
    public Result<ConsumerRelationship> ValidateDiscoveredDependency(ConsumerRelationshipId relationshipId, decimal minimumConfidence)
    {
        var relationship = _consumerRelationships.SingleOrDefault(item => item.Id == relationshipId);
        if (relationship is null)
        {
            return CatalogGraphErrors.ConsumerRelationshipNotFound(relationshipId.Value);
        }

        if (relationship.ConfidenceScore < minimumConfidence)
        {
            return CatalogGraphErrors.LowConfidenceDependency(relationship.ConsumerName, relationship.ConfidenceScore, minimumConfidence);
        }

        return relationship;
    }
}

/// <summary>Identificador fortemente tipado de ApiAsset.</summary>
public sealed record ApiAssetId(Guid Value) : TypedIdBase(Value)
{
    /// <summary>Cria novo Id com Guid gerado automaticamente.</summary>
    public static ApiAssetId New() => new(Guid.NewGuid());

    /// <summary>Cria Id a partir de Guid existente.</summary>
    public static ApiAssetId From(Guid id) => new(id);
}
