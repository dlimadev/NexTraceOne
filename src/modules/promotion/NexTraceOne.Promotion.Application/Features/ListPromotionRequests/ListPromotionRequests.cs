using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Domain.Results;
using NexTraceOne.Promotion.Application.Abstractions;
using NexTraceOne.Promotion.Domain.Enums;

namespace NexTraceOne.Promotion.Application.Features.ListPromotionRequests;

/// <summary>
/// Feature: ListPromotionRequests — lista solicitações de promoção com filtro opcional por status.
/// Estrutura VSA: Query + Validator + Handler + Response em um único arquivo.
/// </summary>
public static class ListPromotionRequests
{
    /// <summary>Item resumido de uma solicitação de promoção na listagem.</summary>
    public sealed record PromotionRequestItem(
        Guid Id,
        Guid ReleaseId,
        string Status,
        string RequestedBy,
        DateTimeOffset RequestedAt,
        DateTimeOffset? CompletedAt);

    /// <summary>Query para listagem de solicitações de promoção.</summary>
    public sealed record Query(string? StatusFilter, int Page, int PageSize) : IQuery<Response>;

    /// <summary>Valida a entrada da query de listagem de solicitações de promoção.</summary>
    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
            RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
        }
    }

    /// <summary>Handler que lista as solicitações de promoção com paginação e filtro por status.</summary>
    public sealed class Handler(
        IPromotionRequestRepository requestRepository) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var status = PromotionStatus.Pending;
            if (!string.IsNullOrWhiteSpace(request.StatusFilter) &&
                Enum.TryParse<PromotionStatus>(request.StatusFilter, ignoreCase: true, out var parsed))
            {
                status = parsed;
            }

            var all = await requestRepository.ListByStatusAsync(status, cancellationToken);
            var totalCount = all.Count;

            var paged = all
                .Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize)
                .Select(r => new PromotionRequestItem(
                    r.Id.Value,
                    r.ReleaseId,
                    r.Status.ToString(),
                    r.RequestedBy,
                    r.RequestedAt,
                    r.CompletedAt))
                .ToList();

            return new Response(paged, totalCount, request.Page, request.PageSize);
        }
    }

    /// <summary>Resposta paginada da listagem de solicitações de promoção.</summary>
    public sealed record Response(
        IReadOnlyList<PromotionRequestItem> Items,
        int TotalCount,
        int Page,
        int PageSize);
}
