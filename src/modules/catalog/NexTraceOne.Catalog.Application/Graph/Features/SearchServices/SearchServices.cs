using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Catalog.Application.Graph.Abstractions;

namespace NexTraceOne.Catalog.Application.Graph.Features.SearchServices;

/// <summary>
/// Feature: SearchServices — pesquisa de serviços no catálogo por termo textual.
/// Busca em nome, displayName, domínio, equipa e descrição.
/// Estrutura VSA: Query + Validator + Handler + Response em um único arquivo.
/// </summary>
public static class SearchServices
{
    /// <summary>Query de pesquisa de serviços do catálogo.</summary>
    public sealed record Query(string SearchTerm) : IQuery<Response>;

    /// <summary>Valida a entrada da query de pesquisa de serviços.</summary>
    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.SearchTerm).NotEmpty().MinimumLength(2).MaximumLength(200);
        }
    }

    /// <summary>Handler que pesquisa serviços por termo textual.</summary>
    public sealed class Handler(
        IServiceAssetRepository serviceAssetRepository) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var results = await serviceAssetRepository.SearchAsync(request.SearchTerm, cancellationToken);

            var items = results
                .Select(svc => new ServiceSearchResult(
                    svc.Id.Value,
                    svc.Name,
                    svc.DisplayName,
                    svc.Description,
                    svc.ServiceType.ToString(),
                    svc.Domain,
                    svc.TeamName,
                    svc.Criticality.ToString(),
                    svc.LifecycleStatus.ToString()))
                .ToList();

            return new Response(items, items.Count);
        }
    }

    /// <summary>Resposta da pesquisa de serviços do catálogo.</summary>
    public sealed record Response(
        IReadOnlyList<ServiceSearchResult> Items,
        int TotalCount);

    /// <summary>Resultado de pesquisa de um serviço no catálogo.</summary>
    public sealed record ServiceSearchResult(
        Guid ServiceId,
        string Name,
        string DisplayName,
        string Description,
        string ServiceType,
        string Domain,
        string TeamName,
        string Criticality,
        string LifecycleStatus);
}
