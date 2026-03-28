using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.Knowledge.Application.Abstractions;
using NexTraceOne.Knowledge.Contracts;
using NexTraceOne.Knowledge.Domain.Entities;
using NexTraceOne.Knowledge.Domain.Enums;
using NexTraceOne.Knowledge.Infrastructure.Persistence;

namespace NexTraceOne.Knowledge.Infrastructure.Search;

/// <summary>
/// Serviço cross-module para ligar runbooks operacionais ao Knowledge Hub.
/// Estratégia: criar nota operacional vinculada ao runbook e relação dessa nota ao serviço.
/// Mantém baixo acoplamento e reaproveita modelo atual de KnowledgeSourceEntityType.
/// </summary>
internal sealed class RunbookKnowledgeLinkingService(
    IOperationalNoteRepository noteRepository,
    IKnowledgeRelationRepository relationRepository,
    KnowledgeDbContext unitOfWork,
    IDateTimeProvider clock) : IRunbookKnowledgeLinkingService
{
    private static readonly Guid SystemAuthorId = Guid.Parse("00000000-0000-0000-0000-000000000001");

    public async Task LinkRunbookToServiceAsync(
        Guid runbookId,
        string runbookTitle,
        string runbookDescription,
        string? linkedServiceId,
        string maintainedBy,
        CancellationToken cancellationToken = default)
    {
        if (runbookId == Guid.Empty || string.IsNullOrWhiteSpace(linkedServiceId))
            return;

        if (!Guid.TryParse(linkedServiceId, out var serviceId) || serviceId == Guid.Empty)
            return;

        var hasExistingLink = await HasExistingRunbookServiceLinkAsync(runbookId, serviceId, cancellationToken);
        if (hasExistingLink)
            return;

        var note = OperationalNote.Create(
            title: $"Runbook: {runbookTitle}",
            content: runbookDescription,
            severity: NoteSeverity.Info,
            noteType: OperationalNoteType.Observation,
            origin: "Runbook",
            authorId: ResolveAuthorId(maintainedBy),
            contextEntityId: runbookId,
            contextType: "Runbook",
            tags: ["runbook", "service-link"],
            utcNow: clock.UtcNow);

        await noteRepository.AddAsync(note, cancellationToken);

        var relation = KnowledgeRelation.Create(
            sourceEntityId: note.Id.Value,
            sourceEntityType: KnowledgeSourceEntityType.OperationalNote,
            targetEntityId: serviceId,
            targetType: RelationType.Service,
            description: "Runbook linked to service",
            context: "Runbook",
            createdById: note.AuthorId,
            utcNow: clock.UtcNow);

        await relationRepository.AddAsync(relation, cancellationToken);
        await unitOfWork.CommitAsync(cancellationToken);
    }

    private static Guid ResolveAuthorId(string maintainedBy)
    {
        if (Guid.TryParse(maintainedBy, out var authorId) && authorId != Guid.Empty)
            return authorId;

        return SystemAuthorId;
    }

    private async Task<bool> HasExistingRunbookServiceLinkAsync(
        Guid runbookId,
        Guid serviceId,
        CancellationToken cancellationToken)
    {
        var relations = await relationRepository.ListByTargetAsync(RelationType.Service, serviceId, cancellationToken);
        foreach (var relation in relations.Where(x => x.SourceEntityType == KnowledgeSourceEntityType.OperationalNote))
        {
            var note = await noteRepository.GetByIdAsync(new OperationalNoteId(relation.SourceEntityId), cancellationToken);
            if (note is null)
                continue;

            if (note.ContextType == "Runbook" && note.ContextEntityId == runbookId)
                return true;
        }

        return false;
    }
}
