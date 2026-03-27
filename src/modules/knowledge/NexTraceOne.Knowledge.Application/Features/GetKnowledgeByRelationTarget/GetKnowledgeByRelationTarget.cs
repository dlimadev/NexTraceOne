using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Knowledge.Application.Abstractions;
using NexTraceOne.Knowledge.Domain.Entities;
using NexTraceOne.Knowledge.Domain.Enums;

namespace NexTraceOne.Knowledge.Application.Features.GetKnowledgeByRelationTarget;

/// <summary>
/// Consulta conhecimento ligado a uma entidade alvo (serviço, contrato, mudança, incidente).
/// </summary>
public static class GetKnowledgeByRelationTarget
{
    public sealed record Query(
        RelationType TargetType,
        Guid TargetEntityId) : IQuery<Response>;

    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.TargetEntityId).NotEqual(Guid.Empty);
        }
    }

    public sealed class Handler(
        IKnowledgeRelationRepository relationRepository,
        IKnowledgeDocumentRepository documentRepository,
        IOperationalNoteRepository noteRepository) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            var relations = await relationRepository.ListByTargetAsync(request.TargetType, request.TargetEntityId, cancellationToken);
            if (relations.Count == 0)
                return new Response([], []);

            var documentItems = new List<KnowledgeDocumentRelationItem>();
            var noteItems = new List<OperationalNoteRelationItem>();

            foreach (var relation in relations)
            {
                if (relation.SourceEntityType == KnowledgeSourceEntityType.KnowledgeDocument)
                {
                    var document = await documentRepository.GetByIdAsync(new KnowledgeDocumentId(relation.SourceEntityId), cancellationToken);
                    if (document is null)
                        continue;

                    documentItems.Add(new KnowledgeDocumentRelationItem(
                        relation.Id.Value,
                        document.Id.Value,
                        document.Title,
                        document.Slug,
                        document.Status.ToString(),
                        document.Category.ToString(),
                        relation.Description,
                        relation.Context,
                        relation.CreatedAt));
                    continue;
                }

                var note = await noteRepository.GetByIdAsync(new OperationalNoteId(relation.SourceEntityId), cancellationToken);
                if (note is null)
                    continue;

                noteItems.Add(new OperationalNoteRelationItem(
                    relation.Id.Value,
                    note.Id.Value,
                    note.Title,
                    note.Severity.ToString(),
                    note.NoteType.ToString(),
                    note.Origin,
                    note.IsResolved,
                    relation.Description,
                    relation.Context,
                    relation.CreatedAt));
            }

            return new Response(
                documentItems.OrderByDescending(x => x.RelationCreatedAt).ToArray(),
                noteItems.OrderByDescending(x => x.RelationCreatedAt).ToArray());
        }
    }

    public sealed record Response(
        IReadOnlyList<KnowledgeDocumentRelationItem> Documents,
        IReadOnlyList<OperationalNoteRelationItem> Notes);

    public sealed record KnowledgeDocumentRelationItem(
        Guid RelationId,
        Guid DocumentId,
        string Title,
        string Slug,
        string Status,
        string Category,
        string? RelationDescription,
        string? RelationContext,
        DateTimeOffset RelationCreatedAt);

    public sealed record OperationalNoteRelationItem(
        Guid RelationId,
        Guid NoteId,
        string Title,
        string Severity,
        string NoteType,
        string Origin,
        bool IsResolved,
        string? RelationDescription,
        string? RelationContext,
        DateTimeOffset RelationCreatedAt);
}
