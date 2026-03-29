using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Catalog.Application.LegacyAssets.Abstractions;
using NexTraceOne.Catalog.Domain.Graph.Enums;

namespace NexTraceOne.Catalog.Application.LegacyAssets.Features.ListLegacyAssets;

/// <summary>
/// Feature: ListLegacyAssets — lista ativos legacy do catálogo com filtros opcionais.
/// Ponto de entrada principal para o catálogo de ativos legacy do NexTraceOne.
/// Estrutura VSA: Query + Validator + Handler + Response em um único arquivo.
/// </summary>
public static class ListLegacyAssets
{
    /// <summary>Query de listagem filtrada de ativos legacy do catálogo.</summary>
    public sealed record Query(
        string? TeamName,
        string? Domain,
        Criticality? Criticality,
        LifecycleStatus? LifecycleStatus,
        string? SearchTerm) : IQuery<Response>;

    /// <summary>Valida a entrada da query de listagem de ativos legacy.</summary>
    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.SearchTerm).MaximumLength(200);
        }
    }

    /// <summary>Handler que lista ativos legacy com filtros opcionais.</summary>
    public sealed class Handler(
        IMainframeSystemRepository mainframeSystemRepository) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var systems = await mainframeSystemRepository.ListFilteredAsync(
                request.TeamName,
                request.Domain,
                request.Criticality,
                request.LifecycleStatus,
                request.SearchTerm,
                cancellationToken);

            var items = systems
                .Select(sys => new LegacyAssetSummaryDto(
                    sys.Id.Value,
                    "MainframeSystem",
                    sys.Name,
                    sys.DisplayName,
                    sys.TeamName,
                    sys.Domain,
                    sys.Criticality.ToString(),
                    sys.LifecycleStatus.ToString()))
                .ToList();

            return new Response(items);
        }
    }

    /// <summary>Resposta da listagem de ativos legacy do catálogo.</summary>
    public sealed record Response(IReadOnlyList<LegacyAssetSummaryDto> Items);

    /// <summary>Resumo de um ativo legacy na listagem do catálogo.</summary>
    public sealed record LegacyAssetSummaryDto(
        Guid Id,
        string AssetType,
        string Name,
        string DisplayName,
        string TeamName,
        string Domain,
        string Criticality,
        string LifecycleStatus);
}
