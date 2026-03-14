using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;

namespace NexTraceOne.DeveloperPortal.Application.Features.GetApisIConsume;

/// <summary>
/// Feature: GetApisIConsume — painel do consumidor com APIs que o utilizador/serviço consome.
/// Exibe status, breaking changes, depreciações e ações pendentes.
/// </summary>
public static class GetApisIConsume
{
    /// <summary>Query para obter APIs consumidas por um utilizador ou serviço.</summary>
    public sealed record Query(Guid UserId, int Page = 1, int PageSize = 20) : IQuery<Response>;

    /// <summary>Valida os parâmetros da consulta de APIs consumidas.</summary>
    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.UserId).NotEmpty();
            RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
            RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
        }
    }

    /// <summary>
    /// Handler que retorna APIs consumidas com status e alertas.
    /// Em produção, agrega dados do Catalog Graph e ChangeIntelligence.
    /// </summary>
    public sealed class Handler : IQueryHandler<Query, Response>
    {
        public Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var result = new Response(
                Items: new List<ConsumedApiDto>().AsReadOnly(),
                TotalCount: 0,
                PendingActions: 0,
                BreakingChangesCount: 0,
                DeprecationsCount: 0);

            return Task.FromResult(Result<Response>.Success(result));
        }
    }

    /// <summary>DTO de API consumida com status e alertas para o painel do consumidor.</summary>
    public sealed record ConsumedApiDto(
        Guid ApiAssetId,
        string ApiName,
        string? CurrentVersion,
        string? LatestVersion,
        string Status,
        bool HasBreakingChanges,
        bool IsDeprecated,
        DateTimeOffset? DeprecationDate,
        DateTimeOffset? LastChange,
        string? Owner,
        decimal RiskScore);

    /// <summary>Resposta do painel do consumidor com APIs consumidas e métricas de alerta.</summary>
    public sealed record Response(
        IReadOnlyList<ConsumedApiDto> Items,
        int TotalCount,
        int PendingActions,
        int BreakingChangesCount,
        int DeprecationsCount);
}
