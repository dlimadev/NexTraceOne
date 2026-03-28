using FluentValidation;
using NexTraceOne.AIKnowledge.Domain.ExternalAI.Errors;
using NexTraceOne.AIKnowledge.Domain.ExternalAI.Enums;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;

namespace NexTraceOne.AIKnowledge.Application.ExternalAI.Features.ListKnowledgeCaptures;

/// <summary>
/// TODO: P03.x — Knowledge capture workflow not in scope for Phase 01.
/// This handler will list persisted knowledge captures when knowledge capture is prioritized.
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

    public sealed class Handler : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            // TODO: P03.x — Knowledge capture workflow not in scope for Phase 01.
            return await Task.FromResult<Result<Response>>(
                ExternalAiErrors.NotImplemented("Feature pending Phase 03"));
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
