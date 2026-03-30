using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Knowledge.Application.Abstractions;
using NexTraceOne.Knowledge.Domain.Entities;
using NexTraceOne.Knowledge.Domain.Enums;

namespace NexTraceOne.Knowledge.Application.Features.UpdateKnowledgeDocument;

/// <summary>
/// Atualiza título, conteúdo, sumário, tags e/ou categoria de um KnowledgeDocument existente.
/// Incrementa a versão interna do documento. Suporta concurrency check via RowVersion.
/// </summary>
public static class UpdateKnowledgeDocument
{
    public sealed record Command(
        Guid DocumentId,
        string? Title,
        string? Content,
        string? Summary,
        DocumentCategory? Category,
        IReadOnlyList<string>? Tags,
        Guid EditorId) : ICommand<Response>;

    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.DocumentId).NotEqual(Guid.Empty);
            RuleFor(x => x.EditorId).NotEqual(Guid.Empty);
            RuleFor(x => x.Title).MaximumLength(500).When(x => x.Title is not null);
            RuleFor(x => x.Content).NotEmpty().When(x => x.Content is not null);
        }
    }

    public sealed class Handler(
        IKnowledgeDocumentRepository documentRepository,
        IDateTimeProvider clock) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            var document = await documentRepository.GetByIdAsync(
                new KnowledgeDocumentId(request.DocumentId),
                cancellationToken);

            if (document is null)
                return Error.NotFound("knowledge.document.not_found", "Knowledge document not found.");

            var now = clock.UtcNow;

            if (!string.IsNullOrWhiteSpace(request.Title) || !string.IsNullOrWhiteSpace(request.Content))
            {
                document.UpdateContent(
                    request.Title ?? document.Title,
                    request.Content ?? document.Content,
                    request.Summary ?? document.Summary,
                    request.EditorId,
                    now);
            }

            if (request.Tags is not null)
                document.UpdateTags(request.Tags, now);

            if (request.Category.HasValue)
                document.UpdateCategory(request.Category.Value, now);

            documentRepository.Update(document);

            return new Response(document.Id.Value, document.Version);
        }
    }

    public sealed record Response(Guid DocumentId, int Version);
}
