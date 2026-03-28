using Ardalis.GuardClauses;

using FluentValidation;

using NexTraceOne.AIKnowledge.Application.ExternalAI.Abstractions;
using NexTraceOne.AIKnowledge.Domain.ExternalAI.Enums;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;

namespace NexTraceOne.AIKnowledge.Application.ExternalAI.Features.ListKnowledgeCaptures;

/// <summary>
/// Feature: ListKnowledgeCaptures — lista captures de conhecimento persistidos com filtros
/// opcionais de status, categoria, tags, texto livre e janela temporal. Suporta paginação.
/// </summary>
public static class ListKnowledgeCaptures
{
    /// <summary>Query para listar captures de conhecimento com filtros opcionais.</summary>
    public sealed record Query(
        KnowledgeStatus? Status,
        string? Category,
        string? Tags,
        string? TextFilter,
        DateTimeOffset? From,
        DateTimeOffset? To,
        int Page = 1,
        int PageSize = 20) : IQuery<Response>;

    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
            RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
            RuleFor(x => x.Category).MaximumLength(200).When(x => x.Category is not null);
            RuleFor(x => x.Tags).MaximumLength(500).When(x => x.Tags is not null);
            RuleFor(x => x.TextFilter).MaximumLength(500).When(x => x.TextFilter is not null);
        }
    }

    public sealed class Handler(
        IKnowledgeCaptureRepository captureRepository) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var (items, total) = await captureRepository.ListAsync(
                request.Status,
                request.Category,
                request.Tags,
                request.TextFilter,
                request.From,
                request.To,
                request.Page,
                request.PageSize,
                cancellationToken);

            var totalPages = (int)Math.Ceiling(total / (double)request.PageSize);

            var mapped = items.Select(c => new CaptureItem(
                c.Id.Value,
                c.ConsultationId.Value,
                c.Title,
                c.Category,
                c.Tags,
                c.Status.ToString(),
                c.ReuseCount,
                c.CapturedAt,
                c.ReviewedBy,
                c.ReviewedAt,
                c.RejectionReason)).ToList();

            return new Response(mapped, total, request.Page, request.PageSize, totalPages);
        }
    }

    /// <summary>Lista paginada de captures de conhecimento.</summary>
    public sealed record Response(
        IReadOnlyList<CaptureItem> Items,
        int TotalCount,
        int Page,
        int PageSize,
        int TotalPages);

    /// <summary>Item de captura de conhecimento para listagem.</summary>
    public sealed record CaptureItem(
        Guid CaptureId,
        Guid ConsultationId,
        string Title,
        string Category,
        string Tags,
        string Status,
        int ReuseCount,
        DateTimeOffset CapturedAt,
        string? ReviewedBy,
        DateTimeOffset? ReviewedAt,
        string? RejectionReason);
}
