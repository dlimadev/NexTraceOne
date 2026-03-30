using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Knowledge.Application.Abstractions;
using NexTraceOne.Knowledge.Domain.Entities;

namespace NexTraceOne.Knowledge.Application.Features.GetKnowledgeDocumentById;

/// <summary>
/// Obtém o detalhe completo de um documento de conhecimento pelo ID.
/// P10.4: Endpoint de detalhe para o Knowledge Hub frontend.
/// </summary>
public static class GetKnowledgeDocumentById
{
    public sealed record Query(Guid DocumentId) : IQuery<Response>;

    public sealed class Handler(IKnowledgeDocumentRepository documentRepository)
        : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            var document = await documentRepository.GetByIdAsync(
                new KnowledgeDocumentId(request.DocumentId),
                cancellationToken);

            if (document is null)
                return Error.NotFound("knowledge.document.not_found", "Knowledge document not found.");

            return new Response(
                document.Id.Value,
                document.Title,
                document.Slug,
                document.Content,
                document.Summary,
                document.Category.ToString(),
                document.Status.ToString(),
                document.Tags,
                document.AuthorId,
                document.LastEditorId,
                document.Version,
                document.CreatedAt,
                document.UpdatedAt,
                document.PublishedAt);
        }
    }

    public sealed record Response(
        Guid DocumentId,
        string Title,
        string Slug,
        string Content,
        string? Summary,
        string Category,
        string Status,
        IReadOnlyList<string> Tags,
        Guid AuthorId,
        Guid? LastEditorId,
        int Version,
        DateTimeOffset CreatedAt,
        DateTimeOffset? UpdatedAt,
        DateTimeOffset? PublishedAt);
}
