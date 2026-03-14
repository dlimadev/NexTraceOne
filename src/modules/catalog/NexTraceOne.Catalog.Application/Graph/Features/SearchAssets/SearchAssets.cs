using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Catalog.Application.Graph.Abstractions;
using NexTraceOne.Catalog.Domain.Graph.Entities;

namespace NexTraceOne.Catalog.Application.Graph.Features.SearchAssets;

/// <summary>
/// Feature: SearchAssets — pesquisa ativos de API pelo nome ou rota.
/// Estrutura VSA: Query + Validator + Handler + Response em um único arquivo.
/// </summary>
public static class SearchAssets
{
    /// <summary>Query de pesquisa de ativos de API.</summary>
    public sealed record Query(string SearchTerm) : IQuery<Response>;

    /// <summary>Valida a entrada da query de pesquisa.</summary>
    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.SearchTerm).NotEmpty().MinimumLength(2).MaximumLength(200);
        }
    }

    /// <summary>Handler que pesquisa ativos de API por nome ou rota.</summary>
    public sealed class Handler(IApiAssetRepository apiAssetRepository) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var results = await apiAssetRepository.SearchAsync(request.SearchTerm, cancellationToken);

            var items = results
                .Select(api => new AssetSummary(
                    api.Id.Value,
                    api.Name,
                    api.RoutePattern,
                    api.Version,
                    api.Visibility,
                    api.OwnerService.Name,
                    api.ConsumerRelationships.Count))
                .ToList();

            return new Response(items);
        }
    }

    /// <summary>Resposta da pesquisa de ativos de API.</summary>
    public sealed record Response(IReadOnlyList<AssetSummary> Items);

    /// <summary>Resumo de um ativo de API nos resultados de pesquisa.</summary>
    public sealed record AssetSummary(
        Guid ApiAssetId,
        string Name,
        string RoutePattern,
        string Version,
        string Visibility,
        string OwnerServiceName,
        int ConsumerCount);
}
