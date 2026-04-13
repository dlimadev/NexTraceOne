using Ardalis.GuardClauses;

using FluentValidation;

using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Enums;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Abstractions;

namespace NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Features.ListReleases;

/// <summary>
/// Feature: ListReleases — lista releases de um ativo de API para consumo da API.
/// Estrutura VSA: Query + Validator + Handler + Response em um único arquivo.
/// </summary>
public static class ListReleases
{
    /// <summary>Query de listagem de releases. Quando ApiAssetId não é informado, lista todas as releases do tenant.</summary>
    public sealed record Query(Guid? ApiAssetId, int Page = 1, int PageSize = 20) : IQuery<Response>;

    /// <summary>Valida a entrada da query de listagem de releases.</summary>
    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.ApiAssetId).NotEmpty().When(x => x.ApiAssetId.HasValue);
            RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
            RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
        }
    }

    /// <summary>Handler que lista releases de um ativo de API ou todas as releases do tenant.</summary>
    public sealed class Handler(IReleaseRepository repository, ICurrentTenant currentTenant) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var releases = request.ApiAssetId.HasValue
                ? await repository.ListByApiAssetAsync(request.ApiAssetId.Value, request.Page, request.PageSize, cancellationToken)
                : await repository.ListFilteredAsync(currentTenant.Id, null, null, null, null, null, null, null, null, null, request.Page, request.PageSize, cancellationToken);

            var dtos = releases.Select(r => new ReleaseDto(
                r.Id.Value,
                r.ServiceName,
                r.Version,
                r.Environment,
                r.Status.ToString(),
                r.ChangeLevel,
                r.ChangeScore,
                r.CreatedAt)).ToList();

            return new Response(dtos);
        }
    }

    /// <summary>DTO de resumo de Release para listagem.</summary>
    public sealed record ReleaseDto(
        Guid ReleaseId,
        string ServiceName,
        string Version,
        string Environment,
        string Status,
        ChangeLevel ChangeLevel,
        decimal ChangeScore,
        DateTimeOffset CreatedAt);

    /// <summary>Resposta da listagem de releases.</summary>
    public sealed record Response(IReadOnlyList<ReleaseDto> Releases);
}
