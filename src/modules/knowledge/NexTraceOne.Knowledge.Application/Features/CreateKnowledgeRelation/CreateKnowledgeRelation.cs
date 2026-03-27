using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Knowledge.Application.Abstractions;
using NexTraceOne.Knowledge.Domain.Entities;
using NexTraceOne.Knowledge.Domain.Enums;

namespace NexTraceOne.Knowledge.Application.Features.CreateKnowledgeRelation;

/// <summary>
/// Cria relação explícita entre conhecimento (documento/nota) e entidade de domínio alvo.
/// </summary>
public static class CreateKnowledgeRelation
{
    public sealed record Command(
        Guid SourceEntityId,
        KnowledgeSourceEntityType SourceEntityType,
        Guid TargetEntityId,
        RelationType TargetType,
        string? Description,
        string? Context,
        Guid CreatedById) : ICommand<Response>;

    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.SourceEntityId).NotEqual(Guid.Empty);
            RuleFor(x => x.TargetEntityId).NotEqual(Guid.Empty);
            RuleFor(x => x.CreatedById).NotEqual(Guid.Empty);
            RuleFor(x => x.Context).MaximumLength(100);
        }
    }

    public sealed class Handler(
        IKnowledgeDocumentRepository documentRepository,
        IOperationalNoteRepository noteRepository,
        IKnowledgeRelationRepository relationRepository,
        IDateTimeProvider clock) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            var sourceExists = request.SourceEntityType switch
            {
                KnowledgeSourceEntityType.KnowledgeDocument =>
                    await documentRepository.GetByIdAsync(new KnowledgeDocumentId(request.SourceEntityId), cancellationToken) is not null,
                KnowledgeSourceEntityType.OperationalNote =>
                    await noteRepository.GetByIdAsync(new OperationalNoteId(request.SourceEntityId), cancellationToken) is not null,
                _ => false
            };

            if (!sourceExists)
            {
                return Error.NotFound(
                    "knowledge.source.not_found",
                    "Source knowledge entity {0} was not found.",
                    request.SourceEntityId);
            }

            var alreadyLinked = (await relationRepository.ListBySourceAsync(request.SourceEntityId, cancellationToken))
                .Any(r => r.TargetType == request.TargetType && r.TargetEntityId == request.TargetEntityId);
            if (alreadyLinked)
            {
                return Error.Conflict(
                    "knowledge.relation.duplicate",
                    "A knowledge relation already exists for source {0} and target {1}.",
                    request.SourceEntityId,
                    request.TargetEntityId);
            }

            var relation = KnowledgeRelation.Create(
                request.SourceEntityId,
                request.SourceEntityType,
                request.TargetEntityId,
                request.TargetType,
                request.Description,
                request.Context,
                request.CreatedById,
                clock.UtcNow);

            await relationRepository.AddAsync(relation, cancellationToken);
            return new Response(relation.Id.Value);
        }
    }

    public sealed record Response(Guid RelationId);
}
