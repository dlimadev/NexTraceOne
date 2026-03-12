using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Domain.Enums;
using NexTraceOne.BuildingBlocks.Domain.Results;
using NexTraceOne.ChangeIntelligence.Application.Abstractions;

namespace NexTraceOne.ChangeIntelligence.Application.Features.GetReleaseHistory;

/// <summary>
/// Feature: GetReleaseHistory — lista o histórico de releases de um ativo de API.
/// Estrutura VSA: Query + Validator + Handler + Response em um único arquivo.
/// </summary>
public static class GetReleaseHistory
{
    /// <summary>Query de listagem do histórico de releases de um ativo de API.</summary>
    public sealed record Query(Guid ApiAssetId, int Page = 1, int PageSize = 20) : IQuery<Response>;

    /// <summary>Valida a entrada da query de histórico de releases.</summary>
    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.ApiAssetId).NotEmpty();
            RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
            RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
        }
    }

    /// <summary>Handler que retorna o histórico de releases de um ativo de API.</summary>
    public sealed class Handler(IReleaseRepository repository) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var releases = await repository.ListByApiAssetAsync(
                request.ApiAssetId, request.Page, request.PageSize, cancellationToken);

            var dtos = releases.Select(r => new ReleaseDto(
                r.Id.Value,
                r.ServiceName,
                r.Version,
                r.Environment,
                r.Status.ToString(),
                r.ChangeLevel,
                r.ChangeScore,
                default)).ToList();

            return new Response(dtos, dtos.Count);
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

    /// <summary>Resposta da listagem do histórico de releases.</summary>
    public sealed record Response(IReadOnlyList<ReleaseDto> Releases, int TotalCount);
}
