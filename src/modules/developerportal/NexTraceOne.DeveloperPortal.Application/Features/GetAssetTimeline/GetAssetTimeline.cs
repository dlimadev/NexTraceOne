using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Domain.Results;

namespace NexTraceOne.DeveloperPortal.Application.Features.GetAssetTimeline;

/// <summary>
/// Feature: GetAssetTimeline — retorna histórico cronológico de eventos de uma API.
/// Inclui deployments, breaking changes, depreciações e mudanças de versão.
/// Estrutura VSA: Query + Validator + Handler + Response em um único arquivo.
/// </summary>
public static class GetAssetTimeline
{
    /// <summary>Query para obter timeline de eventos de uma API.</summary>
    public sealed record Query(Guid ApiAssetId, int Page = 1, int PageSize = 20) : IQuery<Response>;

    /// <summary>Valida os parâmetros da consulta de timeline.</summary>
    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.ApiAssetId).NotEmpty();
            RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
            RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
        }
    }

    /// <summary>
    /// Handler que retorna timeline de eventos da API.
    /// Em produção, agrega dados de releases, contratos e mudanças.
    /// </summary>
    public sealed class Handler : IQueryHandler<Query, Response>
    {
        public Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var result = new Response(
                ApiAssetId: request.ApiAssetId,
                Events: new List<TimelineEventDto>().AsReadOnly(),
                TotalCount: 0);

            return Task.FromResult(Result<Response>.Success(result));
        }
    }

    /// <summary>DTO de evento na timeline de uma API.</summary>
    public sealed record TimelineEventDto(
        string EventType,
        string Title,
        string? Description,
        string? Version,
        string? Actor,
        DateTimeOffset OccurredAt);

    /// <summary>Resposta com timeline cronológica de eventos da API.</summary>
    public sealed record Response(
        Guid ApiAssetId,
        IReadOnlyList<TimelineEventDto> Events,
        int TotalCount);
}
