using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.DeveloperPortal.Application.Abstractions;

namespace NexTraceOne.DeveloperPortal.Application.Features.GetPlaygroundHistory;

/// <summary>
/// Feature: GetPlaygroundHistory — lista histórico de sessões de playground de um utilizador.
/// Permite reutilizar requests anteriores e ver resultados passados.
/// Estrutura VSA: Query + Validator + Handler + Response em um único arquivo.
/// </summary>
public static class GetPlaygroundHistory
{
    /// <summary>Query para listar histórico de sessões de playground.</summary>
    public sealed record Query(Guid UserId, int Page = 1, int PageSize = 20) : IQuery<Response>;

    /// <summary>Valida os parâmetros da consulta de histórico.</summary>
    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.UserId).NotEmpty();
            RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
            RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
        }
    }

    /// <summary>Handler que retorna histórico de sessões de playground.</summary>
    public sealed class Handler(IPlaygroundSessionRepository repository) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var sessions = await repository.GetByUserAsync(
                request.UserId, request.Page, request.PageSize, cancellationToken);

            var dtos = sessions.Select(s => new PlaygroundSessionDto(
                s.Id.Value,
                s.ApiAssetId,
                s.ApiName,
                s.HttpMethod,
                s.RequestPath,
                s.ResponseStatusCode,
                s.DurationMs,
                s.ExecutedAt)).ToList();

            return new Response(dtos);
        }
    }

    /// <summary>DTO de sessão de playground para listagem.</summary>
    public sealed record PlaygroundSessionDto(
        Guid SessionId,
        Guid ApiAssetId,
        string ApiName,
        string HttpMethod,
        string RequestPath,
        int ResponseStatusCode,
        long DurationMs,
        DateTimeOffset ExecutedAt);

    /// <summary>Resposta com histórico de sessões de playground.</summary>
    public sealed record Response(IReadOnlyList<PlaygroundSessionDto> Sessions);
}
