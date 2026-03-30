using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Knowledge.Application.Abstractions;
using NexTraceOne.Knowledge.Domain.Enums;

namespace NexTraceOne.Knowledge.Application.Features.ListKnowledgeDocuments;

/// <summary>
/// Lista documentos de conhecimento com paginação e filtros opcionais.
/// P10.4: Endpoint de listagem para o Knowledge Hub frontend.
/// </summary>
public static class ListKnowledgeDocuments
{
    public sealed record Query(
        DocumentCategory? Category,
        DocumentStatus? Status,
        int Page,
        int PageSize) : IQuery<Response>;

    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
            RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
        }
    }

    public sealed class Handler(IKnowledgeDocumentRepository documentRepository)
        : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            var (items, totalCount) = await documentRepository.ListAsync(
                request.Category,
                request.Status,
                request.Page,
                request.PageSize,
                cancellationToken);

            var dtos = items.Select(d => new KnowledgeDocumentSummaryDto(
                d.Id.Value,
                d.Title,
                d.Slug,
                d.Summary,
                d.Category.ToString(),
                d.Status.ToString(),
                d.Tags,
                d.AuthorId,
                d.Version,
                d.CreatedAt,
                d.UpdatedAt,
                d.PublishedAt)).ToArray();

            return new Response(dtos, totalCount, request.Page, request.PageSize);
        }
    }

    public sealed record KnowledgeDocumentSummaryDto(
        Guid DocumentId,
        string Title,
        string Slug,
        string? Summary,
        string Category,
        string Status,
        IReadOnlyList<string> Tags,
        Guid AuthorId,
        int Version,
        DateTimeOffset CreatedAt,
        DateTimeOffset? UpdatedAt,
        DateTimeOffset? PublishedAt);

    public sealed record Response(
        IReadOnlyList<KnowledgeDocumentSummaryDto> Items,
        int TotalCount,
        int Page,
        int PageSize);
}
