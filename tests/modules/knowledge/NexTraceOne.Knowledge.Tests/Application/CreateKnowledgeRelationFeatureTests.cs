using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.Knowledge.Application.Abstractions;
using NexTraceOne.Knowledge.Application.Features.CreateKnowledgeRelation;
using NexTraceOne.Knowledge.Application.Features.GetKnowledgeByRelationTarget;
using NexTraceOne.Knowledge.Domain.Entities;
using NexTraceOne.Knowledge.Domain.Enums;

namespace NexTraceOne.Knowledge.Tests.Application;

/// <summary>
/// Testes dos handlers mínimos de relação contextual de conhecimento (P10.3).
/// </summary>
public sealed class CreateKnowledgeRelationFeatureTests
{
    [Fact]
    public async Task CreateKnowledgeRelation_Should_CreateRelation_ForServiceTarget()
    {
        var documentRepository = Substitute.For<IKnowledgeDocumentRepository>();
        var noteRepository = Substitute.For<IOperationalNoteRepository>();
        var relationRepository = Substitute.For<IKnowledgeRelationRepository>();
        var clock = Substitute.For<IDateTimeProvider>();

        var now = new DateTimeOffset(2026, 3, 27, 16, 30, 0, TimeSpan.Zero);
        clock.UtcNow.Returns(now);

        var documentId = Guid.NewGuid();
        var targetServiceId = Guid.NewGuid();
        var authorId = Guid.NewGuid();
        var document = KnowledgeDocument.Create(
            "Service troubleshooting",
            "Runbook content",
            "Service runbook",
            DocumentCategory.Runbook,
            [],
            authorId,
            now);

        // Força ID de origem do comando para o mesmo ID do agregado criado.
        documentId = document.Id.Value;

        documentRepository
            .GetByIdAsync(Arg.Is<KnowledgeDocumentId>(x => x.Value == documentId), Arg.Any<CancellationToken>())
            .Returns(document);

        relationRepository.ListBySourceAsync(documentId, Arg.Any<CancellationToken>())
            .Returns(new List<KnowledgeRelation>());

        var sut = new CreateKnowledgeRelation.Handler(documentRepository, noteRepository, relationRepository, clock);

        var result = await sut.Handle(
            new CreateKnowledgeRelation.Command(
                documentId,
                KnowledgeSourceEntityType.KnowledgeDocument,
                targetServiceId,
                RelationType.Service,
                "Main troubleshooting knowledge",
                "Runbook",
                authorId),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await relationRepository.Received(1).AddAsync(
            Arg.Is<KnowledgeRelation>(r =>
                r.SourceEntityId == documentId
                && r.SourceEntityType == KnowledgeSourceEntityType.KnowledgeDocument
                && r.TargetEntityId == targetServiceId
                && r.TargetType == RelationType.Service
                && r.Context == "Runbook"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateKnowledgeRelation_Should_ReturnConflict_WhenDuplicateExists()
    {
        var documentRepository = Substitute.For<IKnowledgeDocumentRepository>();
        var noteRepository = Substitute.For<IOperationalNoteRepository>();
        var relationRepository = Substitute.For<IKnowledgeRelationRepository>();
        var clock = Substitute.For<IDateTimeProvider>();

        var now = new DateTimeOffset(2026, 3, 27, 16, 30, 0, TimeSpan.Zero);
        clock.UtcNow.Returns(now);

        var sourceId = Guid.NewGuid();
        var targetId = Guid.NewGuid();
        var actorId = Guid.NewGuid();

        var document = KnowledgeDocument.Create(
            "API compatibility note",
            "Details",
            null,
            DocumentCategory.Reference,
            [],
            actorId,
            now);
        sourceId = document.Id.Value;

        documentRepository
            .GetByIdAsync(Arg.Is<KnowledgeDocumentId>(x => x.Value == sourceId), Arg.Any<CancellationToken>())
            .Returns(document);

        var existingRelation = KnowledgeRelation.Create(
            sourceId,
            KnowledgeSourceEntityType.KnowledgeDocument,
            targetId,
            RelationType.Contract,
            "Already linked",
            "Reference",
            actorId,
            now);

        relationRepository.ListBySourceAsync(sourceId, Arg.Any<CancellationToken>())
            .Returns([existingRelation]);

        var sut = new CreateKnowledgeRelation.Handler(documentRepository, noteRepository, relationRepository, clock);

        var result = await sut.Handle(
            new CreateKnowledgeRelation.Command(
                sourceId,
                KnowledgeSourceEntityType.KnowledgeDocument,
                targetId,
                RelationType.Contract,
                "Duplicated",
                "Reference",
                actorId),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("knowledge.relation.duplicate");
    }

    [Fact]
    public async Task GetKnowledgeByRelationTarget_Should_ReturnDocumentsAndNotes_ForIncidentTarget()
    {
        var relationRepository = Substitute.For<IKnowledgeRelationRepository>();
        var documentRepository = Substitute.For<IKnowledgeDocumentRepository>();
        var noteRepository = Substitute.For<IOperationalNoteRepository>();

        var now = new DateTimeOffset(2026, 3, 27, 16, 30, 0, TimeSpan.Zero);
        var actorId = Guid.NewGuid();
        var incidentId = Guid.NewGuid();

        var document = KnowledgeDocument.Create(
            "Incident post-mortem",
            "Post-mortem body",
            "Summary",
            DocumentCategory.PostMortem,
            [],
            actorId,
            now);

        var note = OperationalNote.Create(
            "Mitigation step",
            "Restarted consumer",
            NoteSeverity.Warning,
            OperationalNoteType.Mitigation,
            "IncidentTimeline",
            actorId,
            incidentId,
            "Incident",
            [],
            now);

        var documentRelation = KnowledgeRelation.Create(
            document.Id.Value,
            KnowledgeSourceEntityType.KnowledgeDocument,
            incidentId,
            RelationType.Incident,
            "Primary post-mortem",
            "PostMortem",
            actorId,
            now);

        var noteRelation = KnowledgeRelation.Create(
            note.Id.Value,
            KnowledgeSourceEntityType.OperationalNote,
            incidentId,
            RelationType.Incident,
            "Mitigation timeline",
            "Mitigation",
            actorId,
            now);

        relationRepository.ListByTargetAsync(RelationType.Incident, incidentId, Arg.Any<CancellationToken>())
            .Returns([documentRelation, noteRelation]);
        documentRepository.GetByIdAsync(Arg.Is<KnowledgeDocumentId>(x => x.Value == document.Id.Value), Arg.Any<CancellationToken>())
            .Returns(document);
        noteRepository.GetByIdAsync(Arg.Is<OperationalNoteId>(x => x.Value == note.Id.Value), Arg.Any<CancellationToken>())
            .Returns(note);

        var sut = new GetKnowledgeByRelationTarget.Handler(relationRepository, documentRepository, noteRepository);

        var result = await sut.Handle(
            new GetKnowledgeByRelationTarget.Query(RelationType.Incident, incidentId),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Documents.Should().ContainSingle();
        result.Value.Notes.Should().ContainSingle();
        result.Value.Documents[0].Category.Should().Be("PostMortem");
        result.Value.Notes[0].NoteType.Should().Be("Mitigation");
        result.Value.Notes[0].Origin.Should().Be("IncidentTimeline");
    }
}
