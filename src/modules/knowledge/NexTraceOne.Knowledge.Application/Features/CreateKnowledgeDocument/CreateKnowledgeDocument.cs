using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Knowledge.Application.Abstractions;
using NexTraceOne.Knowledge.Domain.Entities;
using NexTraceOne.Knowledge.Domain.Enums;

namespace NexTraceOne.Knowledge.Application.Features.CreateKnowledgeDocument;

/// <summary>
/// Cria um KnowledgeDocument no Knowledge Hub para posterior ligação contextual.
/// </summary>
public static class CreateKnowledgeDocument
{
    public sealed record Command(
        string Title,
        string Content,
        string? Summary,
        DocumentCategory Category,
        IReadOnlyList<string>? Tags,
        Guid AuthorId) : ICommand<Response>;

    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.Title).NotEmpty().MaximumLength(500);
            RuleFor(x => x.Content).NotEmpty();
            RuleFor(x => x.AuthorId).NotEqual(Guid.Empty);
        }
    }

    public sealed class Handler(
        IKnowledgeDocumentRepository documentRepository,
        IDateTimeProvider clock) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            var document = KnowledgeDocument.Create(
                request.Title,
                request.Content,
                request.Summary,
                request.Category,
                request.Tags,
                request.AuthorId,
                clock.UtcNow);

            await documentRepository.AddAsync(document, cancellationToken);
            return new Response(document.Id.Value, document.Slug);
        }
    }

    public sealed record Response(Guid DocumentId, string Slug);
}
