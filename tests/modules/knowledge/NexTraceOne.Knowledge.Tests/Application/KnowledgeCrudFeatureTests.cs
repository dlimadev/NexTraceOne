using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.Knowledge.Application.Abstractions;
using NexTraceOne.Knowledge.Application.Features.CreateKnowledgeDocument;
using NexTraceOne.Knowledge.Application.Features.CreateOperationalNote;
using NexTraceOne.Knowledge.Application.Features.GetKnowledgeDocumentById;
using NexTraceOne.Knowledge.Application.Features.GetKnowledgeRelationsBySource;
using NexTraceOne.Knowledge.Application.Features.ListKnowledgeDocuments;
using NexTraceOne.Knowledge.Application.Features.ListOperationalNotes;
using NexTraceOne.Knowledge.Domain.Entities;
using NexTraceOne.Knowledge.Domain.Enums;

namespace NexTraceOne.Knowledge.Tests.Application;

/// <summary>
/// Testes de unidade para as features CRUD do Knowledge Hub:
/// CreateKnowledgeDocument, CreateOperationalNote, GetKnowledgeDocumentById,
/// GetKnowledgeRelationsBySource, ListKnowledgeDocuments e ListOperationalNotes.
/// </summary>
public sealed class KnowledgeCrudFeatureTests
{
    private static readonly DateTimeOffset FixedNow = new(2026, 4, 10, 12, 0, 0, TimeSpan.Zero);

    private readonly IKnowledgeDocumentRepository _documentRepo = Substitute.For<IKnowledgeDocumentRepository>();
    private readonly IOperationalNoteRepository _noteRepo = Substitute.For<IOperationalNoteRepository>();
    private readonly IKnowledgeRelationRepository _relationRepo = Substitute.For<IKnowledgeRelationRepository>();
    private readonly IDateTimeProvider _clock = Substitute.For<IDateTimeProvider>();

    public KnowledgeCrudFeatureTests()
    {
        _clock.UtcNow.Returns(FixedNow);
    }

    // ── Feature 1: CreateKnowledgeDocument ──

    [Fact]
    public async Task CreateDocument_ValidCommand_ShouldReturnSuccessWithDocumentIdAndSlug()
    {
        var handler = new CreateKnowledgeDocument.Handler(_documentRepo, _clock);
        var command = new CreateKnowledgeDocument.Command(
            Title: "Production Runbook for Payment Service",
            Content: "## Steps\n1. Check logs\n2. Restart pod",
            Summary: "Quick runbook for payments",
            Category: DocumentCategory.Runbook,
            Tags: new List<string> { "payments", "production" },
            AuthorId: Guid.NewGuid());

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.DocumentId.Should().NotBeEmpty();
        result.Value.Slug.Should().NotBeNullOrWhiteSpace();
        result.Value.Slug.Should().Contain("production-runbook");
    }

    [Fact]
    public async Task CreateDocument_ValidCommand_ShouldCallAddAsync()
    {
        var handler = new CreateKnowledgeDocument.Handler(_documentRepo, _clock);
        var authorId = Guid.NewGuid();
        var command = new CreateKnowledgeDocument.Command(
            Title: "Architecture Decision Record",
            Content: "We decided to use PostgreSQL as the primary store.",
            Summary: null,
            Category: DocumentCategory.Architecture,
            Tags: null,
            AuthorId: authorId);

        await handler.Handle(command, CancellationToken.None);

        await _documentRepo.Received(1).AddAsync(
            Arg.Is<KnowledgeDocument>(d =>
                d.Title == "Architecture Decision Record"
                && d.Category == DocumentCategory.Architecture
                && d.AuthorId == authorId),
            Arg.Any<CancellationToken>());
    }

    // ── Feature 2: CreateOperationalNote ──

    [Fact]
    public async Task CreateNote_ValidCommand_ShouldReturnSuccessWithNoteId()
    {
        var handler = new CreateOperationalNote.Handler(_noteRepo, _clock);
        var authorId = Guid.NewGuid();
        var contextEntityId = Guid.NewGuid();
        var command = new CreateOperationalNote.Command(
            Title: "Consumer lag detected",
            Content: "Kafka consumer group payments-processor showing 5k lag",
            Severity: NoteSeverity.Warning,
            NoteType: OperationalNoteType.Observation,
            Origin: "IncidentTimeline",
            AuthorId: authorId,
            ContextEntityId: contextEntityId,
            ContextType: "Incident",
            Tags: new List<string> { "kafka", "lag" });

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.NoteId.Should().NotBeEmpty();
    }

    [Fact]
    public async Task CreateNote_ValidCommand_ShouldCallAddAsync()
    {
        var handler = new CreateOperationalNote.Handler(_noteRepo, _clock);
        var authorId = Guid.NewGuid();
        var command = new CreateOperationalNote.Command(
            Title: "Decision to rollback",
            Content: "Rolling back release v2.4.1 due to error rate spike",
            Severity: NoteSeverity.Critical,
            NoteType: OperationalNoteType.Decision,
            Origin: "Manual",
            AuthorId: authorId,
            ContextEntityId: null,
            ContextType: null,
            Tags: null);

        await handler.Handle(command, CancellationToken.None);

        await _noteRepo.Received(1).AddAsync(
            Arg.Is<OperationalNote>(n =>
                n.Title == "Decision to rollback"
                && n.Severity == NoteSeverity.Critical
                && n.NoteType == OperationalNoteType.Decision
                && n.AuthorId == authorId),
            Arg.Any<CancellationToken>());
    }

    // ── Feature 3: GetKnowledgeDocumentById ──

    [Fact]
    public async Task GetDocumentById_DocumentExists_ShouldReturnMappedResponse()
    {
        var authorId = Guid.NewGuid();
        var document = KnowledgeDocument.Create(
            "Troubleshooting Guide",
            "Check connection strings first.",
            "Quick guide",
            DocumentCategory.Troubleshooting,
            new List<string> { "database", "connectivity" },
            authorId,
            FixedNow);

        _documentRepo
            .GetByIdAsync(
                Arg.Is<KnowledgeDocumentId>(x => x.Value == document.Id.Value),
                Arg.Any<CancellationToken>())
            .Returns(document);

        var handler = new GetKnowledgeDocumentById.Handler(_documentRepo);
        var query = new GetKnowledgeDocumentById.Query(document.Id.Value);

        var result = await handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.DocumentId.Should().Be(document.Id.Value);
        result.Value.Title.Should().Be("Troubleshooting Guide");
        result.Value.Slug.Should().Be(document.Slug);
        result.Value.Content.Should().Be("Check connection strings first.");
        result.Value.Summary.Should().Be("Quick guide");
        result.Value.Category.Should().Be(nameof(DocumentCategory.Troubleshooting));
        result.Value.Status.Should().Be(nameof(DocumentStatus.Draft));
        result.Value.Tags.Should().BeEquivalentTo(new[] { "database", "connectivity" });
        result.Value.AuthorId.Should().Be(authorId);
        result.Value.Version.Should().Be(1);
        result.Value.CreatedAt.Should().Be(FixedNow);
    }

    [Fact]
    public async Task GetDocumentById_DocumentNotFound_ShouldReturnNotFoundError()
    {
        var missingId = Guid.NewGuid();
        _documentRepo
            .GetByIdAsync(
                Arg.Is<KnowledgeDocumentId>(x => x.Value == missingId),
                Arg.Any<CancellationToken>())
            .Returns((KnowledgeDocument?)null);

        var handler = new GetKnowledgeDocumentById.Handler(_documentRepo);
        var query = new GetKnowledgeDocumentById.Query(missingId);

        var result = await handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("knowledge.document.not_found");
    }

    // ── Feature 4: GetKnowledgeRelationsBySource ──

    [Fact]
    public async Task GetRelationsBySource_HasRelations_ShouldReturnMappedItems()
    {
        var sourceId = Guid.NewGuid();
        var targetServiceId = Guid.NewGuid();
        var targetContractId = Guid.NewGuid();
        var actorId = Guid.NewGuid();

        var relation1 = KnowledgeRelation.Create(
            sourceId,
            KnowledgeSourceEntityType.KnowledgeDocument,
            targetServiceId,
            RelationType.Service,
            "Primary runbook",
            "Runbook",
            actorId,
            FixedNow);

        var relation2 = KnowledgeRelation.Create(
            sourceId,
            KnowledgeSourceEntityType.KnowledgeDocument,
            targetContractId,
            RelationType.Contract,
            "API reference",
            null,
            actorId,
            FixedNow);

        _relationRepo
            .ListBySourceAsync(sourceId, Arg.Any<CancellationToken>())
            .Returns(new List<KnowledgeRelation> { relation1, relation2 });

        var handler = new GetKnowledgeRelationsBySource.Handler(_relationRepo);
        var query = new GetKnowledgeRelationsBySource.Query(sourceId);

        var result = await handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(2);

        var first = result.Value.Items[0];
        first.RelationId.Should().Be(relation1.Id.Value);
        first.SourceEntityId.Should().Be(sourceId);
        first.SourceEntityType.Should().Be(nameof(KnowledgeSourceEntityType.KnowledgeDocument));
        first.TargetEntityId.Should().Be(targetServiceId);
        first.TargetType.Should().Be(nameof(RelationType.Service));
        first.Description.Should().Be("Primary runbook");
        first.Context.Should().Be("Runbook");
        first.CreatedById.Should().Be(actorId);
        first.CreatedAt.Should().Be(FixedNow);

        var second = result.Value.Items[1];
        second.TargetType.Should().Be(nameof(RelationType.Contract));
        second.Context.Should().BeNull();
    }

    [Fact]
    public async Task GetRelationsBySource_NoRelations_ShouldReturnEmptyList()
    {
        var sourceId = Guid.NewGuid();

        _relationRepo
            .ListBySourceAsync(sourceId, Arg.Any<CancellationToken>())
            .Returns(new List<KnowledgeRelation>());

        var handler = new GetKnowledgeRelationsBySource.Handler(_relationRepo);
        var query = new GetKnowledgeRelationsBySource.Query(sourceId);

        var result = await handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().BeEmpty();
    }

    // ── Feature 5: ListKnowledgeDocuments ──

    [Fact]
    public async Task ListDocuments_WithResults_ShouldReturnPaginatedMappedItems()
    {
        var authorId = Guid.NewGuid();
        var doc1 = KnowledgeDocument.Create(
            "Runbook Alpha", "Content A", "Summary A",
            DocumentCategory.Runbook, new List<string> { "alpha" },
            authorId, FixedNow);
        var doc2 = KnowledgeDocument.Create(
            "Runbook Beta", "Content B", null,
            DocumentCategory.Runbook, null,
            authorId, FixedNow);

        _documentRepo
            .ListAsync(DocumentCategory.Runbook, null, 1, 10, Arg.Any<CancellationToken>())
            .Returns((new List<KnowledgeDocument> { doc1, doc2 } as IReadOnlyList<KnowledgeDocument>, 2));

        var handler = new ListKnowledgeDocuments.Handler(_documentRepo);
        var query = new ListKnowledgeDocuments.Query(
            Category: DocumentCategory.Runbook,
            Status: null,
            Page: 1,
            PageSize: 10);

        var result = await handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(2);
        result.Value.TotalCount.Should().Be(2);
        result.Value.Page.Should().Be(1);
        result.Value.PageSize.Should().Be(10);

        result.Value.Items[0].Title.Should().Be("Runbook Alpha");
        result.Value.Items[0].Category.Should().Be(nameof(DocumentCategory.Runbook));
        result.Value.Items[0].Status.Should().Be(nameof(DocumentStatus.Draft));
        result.Value.Items[0].AuthorId.Should().Be(authorId);
    }

    [Fact]
    public async Task ListDocuments_EmptyResults_ShouldReturnEmptyWithZeroCount()
    {
        _documentRepo
            .ListAsync(null, DocumentStatus.Published, 1, 20, Arg.Any<CancellationToken>())
            .Returns((Array.Empty<KnowledgeDocument>() as IReadOnlyList<KnowledgeDocument>, 0));

        var handler = new ListKnowledgeDocuments.Handler(_documentRepo);
        var query = new ListKnowledgeDocuments.Query(
            Category: null,
            Status: DocumentStatus.Published,
            Page: 1,
            PageSize: 20);

        var result = await handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().BeEmpty();
        result.Value.TotalCount.Should().Be(0);
    }

    // ── Feature 6: ListOperationalNotes ──

    [Fact]
    public async Task ListNotes_WithFilters_ShouldReturnPaginatedMappedItems()
    {
        var authorId = Guid.NewGuid();
        var contextEntityId = Guid.NewGuid();
        var note1 = OperationalNote.Create(
            "CPU spike observed", "CPU at 95% on node-3",
            NoteSeverity.Critical, OperationalNoteType.Observation,
            "MonitoringAlert", authorId,
            contextEntityId, "Service",
            new List<string> { "cpu", "performance" },
            FixedNow);
        var note2 = OperationalNote.Create(
            "Scaled horizontally", "Added 2 replicas",
            NoteSeverity.Critical, OperationalNoteType.Mitigation,
            "Manual", authorId,
            contextEntityId, "Service",
            null,
            FixedNow);

        _noteRepo
            .ListAsync(
                NoteSeverity.Critical, "Service", contextEntityId, false, 1, 25,
                Arg.Any<CancellationToken>())
            .Returns((new List<OperationalNote> { note1, note2 } as IReadOnlyList<OperationalNote>, 2));

        var handler = new ListOperationalNotes.Handler(_noteRepo);
        var query = new ListOperationalNotes.Query(
            Severity: NoteSeverity.Critical,
            ContextType: "Service",
            ContextEntityId: contextEntityId,
            IsResolved: false,
            Page: 1,
            PageSize: 25);

        var result = await handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(2);
        result.Value.TotalCount.Should().Be(2);
        result.Value.Page.Should().Be(1);
        result.Value.PageSize.Should().Be(25);

        var first = result.Value.Items[0];
        first.NoteId.Should().Be(note1.Id.Value);
        first.Title.Should().Be("CPU spike observed");
        first.Severity.Should().Be(nameof(NoteSeverity.Critical));
        first.NoteType.Should().Be(nameof(OperationalNoteType.Observation));
        first.Origin.Should().Be("MonitoringAlert");
        first.ContextEntityId.Should().Be(contextEntityId);
        first.ContextType.Should().Be("Service");
        first.IsResolved.Should().BeFalse();
    }

    [Fact]
    public async Task ListNotes_EmptyResults_ShouldReturnEmptyWithZeroCount()
    {
        _noteRepo
            .ListAsync(null, null, null, null, 1, 10, Arg.Any<CancellationToken>())
            .Returns((Array.Empty<OperationalNote>() as IReadOnlyList<OperationalNote>, 0));

        var handler = new ListOperationalNotes.Handler(_noteRepo);
        var query = new ListOperationalNotes.Query(
            Severity: null,
            ContextType: null,
            ContextEntityId: null,
            IsResolved: null,
            Page: 1,
            PageSize: 10);

        var result = await handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().BeEmpty();
        result.Value.TotalCount.Should().Be(0);
    }
}
