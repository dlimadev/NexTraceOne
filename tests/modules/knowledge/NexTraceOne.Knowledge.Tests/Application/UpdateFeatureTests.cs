using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.Knowledge.Application.Abstractions;
using NexTraceOne.Knowledge.Application.Features.UpdateKnowledgeDocument;
using NexTraceOne.Knowledge.Application.Features.UpdateOperationalNote;
using NexTraceOne.Knowledge.Domain.Entities;
using NexTraceOne.Knowledge.Domain.Enums;

namespace NexTraceOne.Knowledge.Tests.Application;

/// <summary>
/// Testes de unidade para as features de atualização: UpdateKnowledgeDocument e UpdateOperationalNote.
/// </summary>
public sealed class UpdateFeatureTests
{
    private static readonly DateTimeOffset FixedNow = new(2026, 3, 30, 14, 0, 0, TimeSpan.Zero);

    private readonly IKnowledgeDocumentRepository _documentRepo = Substitute.For<IKnowledgeDocumentRepository>();
    private readonly IOperationalNoteRepository _noteRepo = Substitute.For<IOperationalNoteRepository>();
    private readonly IDateTimeProvider _clock = Substitute.For<IDateTimeProvider>();

    public UpdateFeatureTests()
    {
        _clock.UtcNow.Returns(FixedNow);
    }

    // ── UpdateKnowledgeDocument ──

    [Fact]
    public async Task UpdateDocument_ValidData_ShouldReturnSuccess()
    {
        var document = KnowledgeDocument.Create(
            "Original Title", "Original Content", null,
            DocumentCategory.Runbook, null, Guid.NewGuid(), FixedNow.AddHours(-1));

        _documentRepo.GetByIdAsync(document.Id, Arg.Any<CancellationToken>())
            .Returns(document);

        var handler = new UpdateKnowledgeDocument.Handler(_documentRepo, _clock);
        var command = new UpdateKnowledgeDocument.Command(
            DocumentId: document.Id.Value,
            Title: "Updated Title",
            Content: "Updated Content",
            Summary: "A summary",
            Category: null,
            Tags: null,
            EditorId: Guid.NewGuid());

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.DocumentId.Should().Be(document.Id.Value);
        result.Value.Version.Should().BeGreaterThan(0);
        _documentRepo.Received(1).Update(document);
    }

    [Fact]
    public async Task UpdateDocument_DocumentNotFound_ShouldReturnNotFound()
    {
        var fakeId = Guid.NewGuid();
        _documentRepo.GetByIdAsync(new KnowledgeDocumentId(fakeId), Arg.Any<CancellationToken>())
            .Returns((KnowledgeDocument?)null);

        var handler = new UpdateKnowledgeDocument.Handler(_documentRepo, _clock);
        var command = new UpdateKnowledgeDocument.Command(
            fakeId, "New Title", null, null, null, null, Guid.NewGuid());

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("knowledge.document.not_found");
    }

    [Fact]
    public async Task UpdateDocument_OnlyTags_ShouldUpdateTags()
    {
        var document = KnowledgeDocument.Create(
            "Title", "Content", null,
            DocumentCategory.Architecture, null, Guid.NewGuid(), FixedNow.AddHours(-1));

        _documentRepo.GetByIdAsync(document.Id, Arg.Any<CancellationToken>())
            .Returns(document);

        var handler = new UpdateKnowledgeDocument.Handler(_documentRepo, _clock);
        var newTags = new List<string> { "kubernetes", "deployment" };
        var command = new UpdateKnowledgeDocument.Command(
            document.Id.Value, null, null, null, null, newTags, Guid.NewGuid());

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        document.Tags.Should().BeEquivalentTo(newTags);
    }

    [Fact]
    public async Task UpdateDocument_OnlyCategory_ShouldUpdateCategory()
    {
        var document = KnowledgeDocument.Create(
            "Title", "Content", null,
            DocumentCategory.Runbook, null, Guid.NewGuid(), FixedNow.AddHours(-1));

        _documentRepo.GetByIdAsync(document.Id, Arg.Any<CancellationToken>())
            .Returns(document);

        var handler = new UpdateKnowledgeDocument.Handler(_documentRepo, _clock);
        var command = new UpdateKnowledgeDocument.Command(
            document.Id.Value, null, null, null, DocumentCategory.Architecture, null, Guid.NewGuid());

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        document.Category.Should().Be(DocumentCategory.Architecture);
    }

    [Fact]
    public async Task UpdateDocument_Validator_ShouldRejectEmptyDocumentId()
    {
        var validator = new UpdateKnowledgeDocument.Validator();
        var command = new UpdateKnowledgeDocument.Command(
            Guid.Empty, "Title", "Content", null, null, null, Guid.NewGuid());

        var validation = await validator.ValidateAsync(command);

        validation.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task UpdateDocument_Validator_ShouldRejectEmptyEditorId()
    {
        var validator = new UpdateKnowledgeDocument.Validator();
        var command = new UpdateKnowledgeDocument.Command(
            Guid.NewGuid(), "Title", "Content", null, null, null, Guid.Empty);

        var validation = await validator.ValidateAsync(command);

        validation.IsValid.Should().BeFalse();
    }

    // ── UpdateOperationalNote ──

    [Fact]
    public async Task UpdateNote_ValidData_ShouldReturnSuccess()
    {
        var note = OperationalNote.Create(
            "Original Title", "Original Content",
            NoteSeverity.Warning, OperationalNoteType.Observation,
            "Manual", Guid.NewGuid(), null, null, null, FixedNow.AddHours(-1));

        _noteRepo.GetByIdAsync(note.Id, Arg.Any<CancellationToken>())
            .Returns(note);

        var handler = new UpdateOperationalNote.Handler(_noteRepo, _clock);
        var command = new UpdateOperationalNote.Command(
            NoteId: note.Id.Value,
            Title: "Updated Note Title",
            Content: "Updated Note Content",
            Severity: NoteSeverity.Critical,
            NoteType: null,
            Tags: null,
            Resolve: null,
            EditorId: Guid.NewGuid());

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.NoteId.Should().Be(note.Id.Value);
        _noteRepo.Received(1).Update(note);
    }

    [Fact]
    public async Task UpdateNote_NoteNotFound_ShouldReturnNotFound()
    {
        var fakeId = Guid.NewGuid();
        _noteRepo.GetByIdAsync(new OperationalNoteId(fakeId), Arg.Any<CancellationToken>())
            .Returns((OperationalNote?)null);

        var handler = new UpdateOperationalNote.Handler(_noteRepo, _clock);
        var command = new UpdateOperationalNote.Command(
            fakeId, "Title", null, null, null, null, null, Guid.NewGuid());

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("knowledge.note.not_found");
    }

    [Fact]
    public async Task UpdateNote_ResolveTrue_ShouldResolveNote()
    {
        var note = OperationalNote.Create(
            "Title", "Content",
            NoteSeverity.Info, OperationalNoteType.Decision,
            "Manual", Guid.NewGuid(), null, null, null, FixedNow.AddHours(-1));

        _noteRepo.GetByIdAsync(note.Id, Arg.Any<CancellationToken>())
            .Returns(note);

        var handler = new UpdateOperationalNote.Handler(_noteRepo, _clock);
        var command = new UpdateOperationalNote.Command(
            note.Id.Value, null, null, null, null, null, Resolve: true, Guid.NewGuid());

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.IsResolved.Should().BeTrue();
        note.IsResolved.Should().BeTrue();
    }

    [Fact]
    public async Task UpdateNote_ReopenResolved_ShouldReopenNote()
    {
        var note = OperationalNote.Create(
            "Title", "Content",
            NoteSeverity.Info, OperationalNoteType.Decision,
            "Manual", Guid.NewGuid(), null, null, null, FixedNow.AddHours(-1));
        note.Resolve(FixedNow);

        _noteRepo.GetByIdAsync(note.Id, Arg.Any<CancellationToken>())
            .Returns(note);

        var handler = new UpdateOperationalNote.Handler(_noteRepo, _clock);
        var command = new UpdateOperationalNote.Command(
            note.Id.Value, null, null, null, null, null, Resolve: false, Guid.NewGuid());

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.IsResolved.Should().BeFalse();
        note.IsResolved.Should().BeFalse();
    }

    [Fact]
    public async Task UpdateNote_UpdateSeverity_ShouldChangeSeverity()
    {
        var note = OperationalNote.Create(
            "Title", "Content",
            NoteSeverity.Info, OperationalNoteType.Decision,
            "Manual", Guid.NewGuid(), null, null, null, FixedNow.AddHours(-1));

        _noteRepo.GetByIdAsync(note.Id, Arg.Any<CancellationToken>())
            .Returns(note);

        var handler = new UpdateOperationalNote.Handler(_noteRepo, _clock);
        var command = new UpdateOperationalNote.Command(
            note.Id.Value, null, null, NoteSeverity.Critical, null, null, null, Guid.NewGuid());

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        note.Severity.Should().Be(NoteSeverity.Critical);
    }

    [Fact]
    public async Task UpdateNote_Validator_ShouldRejectEmptyNoteId()
    {
        var validator = new UpdateOperationalNote.Validator();
        var command = new UpdateOperationalNote.Command(
            Guid.Empty, "Title", null, null, null, null, null, Guid.NewGuid());

        var validation = await validator.ValidateAsync(command);

        validation.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task UpdateNote_Validator_ShouldRejectEmptyEditorId()
    {
        var validator = new UpdateOperationalNote.Validator();
        var command = new UpdateOperationalNote.Command(
            Guid.NewGuid(), null, null, null, null, null, null, Guid.Empty);

        var validation = await validator.ValidateAsync(command);

        validation.IsValid.Should().BeFalse();
    }
}
