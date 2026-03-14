using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Catalog.Application.Graph.Abstractions;
using NexTraceOne.Catalog.Domain.Graph.Entities;
using NexTraceOne.Catalog.Domain.Graph.Errors;

namespace NexTraceOne.Catalog.Application.Graph.Features.GetAssetDetail;

/// <summary>
/// Feature: GetAssetDetail — obtém os detalhes de um ativo de API pelo seu identificador.
/// Estrutura VSA: Query + Validator + Handler + Response em um único arquivo.
/// </summary>
public static class GetAssetDetail
{
    /// <summary>Query de detalhes de um ativo de API.</summary>
    public sealed record Query(Guid ApiAssetId) : IQuery<Response>;

    /// <summary>Valida a entrada da query de detalhe.</summary>
    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.ApiAssetId).NotEmpty();
        }
    }

    /// <summary>Handler que retorna os detalhes completos de um ativo de API.</summary>
    public sealed class Handler(IApiAssetRepository apiAssetRepository) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var apiAssetId = ApiAssetId.From(request.ApiAssetId);
            var apiAsset = await apiAssetRepository.GetByIdAsync(apiAssetId, cancellationToken);
            if (apiAsset is null)
            {
                return CatalogGraphErrors.ApiAssetNotFound(request.ApiAssetId);
            }

            var consumers = apiAsset.ConsumerRelationships
                .Select(rel => new ConsumerSummary(rel.Id.Value, rel.ConsumerName, rel.SourceType, rel.ConfidenceScore, rel.LastObservedAt))
                .ToList();

            var sources = apiAsset.DiscoverySources
                .Select(src => new DiscoverySourceSummary(src.Id.Value, src.SourceType, src.ExternalReference, src.DiscoveredAt, src.ConfidenceScore))
                .ToList();

            return new Response(
                apiAsset.Id.Value,
                apiAsset.Name,
                apiAsset.RoutePattern,
                apiAsset.Version,
                apiAsset.Visibility,
                apiAsset.OwnerService.Id.Value,
                apiAsset.OwnerService.Name,
                consumers,
                sources);
        }
    }

    /// <summary>Resposta detalhada de um ativo de API.</summary>
    public sealed record Response(
        Guid ApiAssetId,
        string Name,
        string RoutePattern,
        string Version,
        string Visibility,
        Guid OwnerServiceAssetId,
        string OwnerServiceName,
        IReadOnlyList<ConsumerSummary> Consumers,
        IReadOnlyList<DiscoverySourceSummary> DiscoverySources);

    /// <summary>Resumo de um consumidor da API.</summary>
    public sealed record ConsumerSummary(
        Guid RelationshipId,
        string ConsumerName,
        string SourceType,
        decimal ConfidenceScore,
        DateTimeOffset LastObservedAt);

    /// <summary>Resumo de uma fonte de descoberta da API.</summary>
    public sealed record DiscoverySourceSummary(
        Guid DiscoverySourceId,
        string SourceType,
        string ExternalReference,
        DateTimeOffset DiscoveredAt,
        decimal ConfidenceScore);
}
