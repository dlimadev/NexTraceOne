using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Contracts.Application.Abstractions;
using NexTraceOne.Contracts.Domain.Entities;
using NexTraceOne.Contracts.Domain.Enums;

namespace NexTraceOne.Contracts.Application.Features.ListDraftReviews;

/// <summary>
/// Feature: ListDraftReviews — lista revisões de um draft de contrato.
/// Retorna o histórico completo de aprovações e rejeições para auditoria.
/// Estrutura VSA: Query + Validator + Handler + Response em um único arquivo.
/// </summary>
public static class ListDraftReviews
{
    /// <summary>Query para listar revisões de um draft.</summary>
    public sealed record Query(Guid DraftId) : IQuery<Response>;

    /// <summary>Valida a entrada da query de listagem de revisões.</summary>
    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.DraftId).NotEmpty();
        }
    }

    /// <summary>Handler que lista revisões de um draft de contrato.</summary>
    public sealed class Handler(
        IContractReviewRepository repository) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var reviews = await repository.ListByDraftAsync(
                ContractDraftId.From(request.DraftId), cancellationToken);

            var items = reviews
                .Select(r => new ReviewItem(
                    r.Id.Value,
                    r.DraftId.Value,
                    r.ReviewedBy,
                    r.Decision,
                    r.Comment,
                    r.ReviewedAt))
                .ToList();

            return new Response(items);
        }
    }

    /// <summary>Item de revisão no histórico do draft.</summary>
    public sealed record ReviewItem(
        Guid ReviewId,
        Guid DraftId,
        string ReviewedBy,
        ReviewDecision Decision,
        string Comment,
        DateTimeOffset ReviewedAt);

    /// <summary>Resposta com lista de revisões do draft.</summary>
    public sealed record Response(IReadOnlyList<ReviewItem> Items);
}
