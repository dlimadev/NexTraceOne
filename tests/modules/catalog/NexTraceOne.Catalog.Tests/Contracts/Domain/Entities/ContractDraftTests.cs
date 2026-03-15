using NexTraceOne.Contracts.Domain.Entities;
using NexTraceOne.Contracts.Domain.Enums;

namespace NexTraceOne.Contracts.Tests.Domain.Entities;

/// <summary>
/// Testes de domínio para a entidade ContractDraft do Contract Studio.
/// Valida criação, transições de estado, edição e gestão de exemplos.
/// </summary>
public sealed class ContractDraftTests
{
    private static readonly DateTimeOffset FixedNow = new(2025, 06, 15, 10, 0, 0, TimeSpan.Zero);

    // ── Create ──────────────────────────────────────────────────────────

    [Fact]
    public void Create_Should_ReturnDraft_When_InputIsValid()
    {
        var result = ContractDraft.Create(
            "My API Contract",
            "engineer@company.com",
            ContractType.RestApi,
            ContractProtocol.OpenApi);

        result.IsSuccess.Should().BeTrue();
        result.Value.Title.Should().Be("My API Contract");
        result.Value.Author.Should().Be("engineer@company.com");
        result.Value.ContractType.Should().Be(ContractType.RestApi);
        result.Value.Protocol.Should().Be(ContractProtocol.OpenApi);
        result.Value.ProposedVersion.Should().Be("1.0.0");
        result.Value.Format.Should().Be("yaml");
        result.Value.SpecContent.Should().BeEmpty();
        result.Value.Id.Value.Should().NotBeEmpty();
    }

    [Fact]
    public void Create_Should_SetEditingStatus_When_Created()
    {
        var result = ContractDraft.Create(
            "Draft Title",
            "author@test.com",
            ContractType.Event,
            ContractProtocol.AsyncApi);

        result.IsSuccess.Should().BeTrue();
        result.Value.Status.Should().Be(DraftStatus.Editing);
    }

    // ── CreateFromAi ────────────────────────────────────────────────────

    [Fact]
    public void CreateFromAi_Should_ReturnDraft_When_AiInputIsValid()
    {
        var result = ContractDraft.CreateFromAi(
            "AI Generated Contract",
            "ai-engineer@company.com",
            ContractType.RestApi,
            ContractProtocol.OpenApi,
            "Generate a REST API for user management",
            """openapi: '3.1.0'""",
            "yaml");

        result.IsSuccess.Should().BeTrue();
        result.Value.Title.Should().Be("AI Generated Contract");
        result.Value.Author.Should().Be("ai-engineer@company.com");
        result.Value.SpecContent.Should().Be("""openapi: '3.1.0'""");
        result.Value.Format.Should().Be("yaml");
        result.Value.AiGenerationPrompt.Should().Be("Generate a REST API for user management");
    }

    [Fact]
    public void CreateFromAi_Should_SetAiGenerated_When_Created()
    {
        var result = ContractDraft.CreateFromAi(
            "AI Contract",
            "author@test.com",
            ContractType.RestApi,
            ContractProtocol.OpenApi,
            "Generate API",
            """{"openapi":"3.1.0"}""",
            "JSON");

        result.IsSuccess.Should().BeTrue();
        result.Value.IsAiGenerated.Should().BeTrue();
        result.Value.Status.Should().Be(DraftStatus.Editing);
        result.Value.Format.Should().Be("json");
    }

    // ── UpdateContent ───────────────────────────────────────────────────

    [Fact]
    public void UpdateContent_Should_Succeed_When_DraftIsEditing()
    {
        var draft = ContractDraft.Create(
            "Draft", "author@test.com", ContractType.RestApi, ContractProtocol.OpenApi).Value;

        var result = draft.UpdateContent(
            """{"openapi":"3.1.0","paths":{}}""", "json", "editor@test.com", FixedNow);

        result.IsSuccess.Should().BeTrue();
        draft.SpecContent.Should().Be("""{"openapi":"3.1.0","paths":{}}""");
        draft.Format.Should().Be("json");
        draft.LastEditedBy.Should().Be("editor@test.com");
        draft.LastEditedAt.Should().Be(FixedNow);
    }

    [Fact]
    public void UpdateContent_Should_Fail_When_DraftIsInReview()
    {
        var draft = CreateDraftWithContent();
        draft.SubmitForReview(FixedNow);

        var result = draft.UpdateContent("new content", "yaml", "editor@test.com", FixedNow);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Contracts.Draft.NotEditable");
    }

    // ── UpdateMetadata ──────────────────────────────────────────────────

    [Fact]
    public void UpdateMetadata_Should_Succeed_When_DraftIsEditing()
    {
        var draft = ContractDraft.Create(
            "Draft", "author@test.com", ContractType.RestApi, ContractProtocol.OpenApi).Value;
        var serviceId = Guid.NewGuid();

        var result = draft.UpdateMetadata(
            "Updated Title", "Updated description", "2.0.0", serviceId, "editor@test.com", FixedNow);

        result.IsSuccess.Should().BeTrue();
        draft.Title.Should().Be("Updated Title");
        draft.Description.Should().Be("Updated description");
        draft.ProposedVersion.Should().Be("2.0.0");
        draft.ServiceId.Should().Be(serviceId);
        draft.LastEditedBy.Should().Be("editor@test.com");
        draft.LastEditedAt.Should().Be(FixedNow);
    }

    // ── SubmitForReview ─────────────────────────────────────────────────

    [Fact]
    public void SubmitForReview_Should_TransitionToInReview_When_DraftHasContent()
    {
        var draft = CreateDraftWithContent();

        var result = draft.SubmitForReview(FixedNow);

        result.IsSuccess.Should().BeTrue();
        draft.Status.Should().Be(DraftStatus.InReview);
        draft.LastEditedAt.Should().Be(FixedNow);
    }

    [Fact]
    public void SubmitForReview_Should_Fail_When_SpecContentIsEmpty()
    {
        var draft = ContractDraft.Create(
            "Draft", "author@test.com", ContractType.RestApi, ContractProtocol.OpenApi).Value;

        var result = draft.SubmitForReview(FixedNow);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Contracts.ContractVersion.EmptySpecContent");
    }

    [Fact]
    public void SubmitForReview_Should_Fail_When_DraftIsNotEditing()
    {
        var draft = CreateDraftWithContent();
        draft.SubmitForReview(FixedNow);

        var result = draft.SubmitForReview(FixedNow);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Contracts.Draft.NotEditable");
    }

    // ── Approve ─────────────────────────────────────────────────────────

    [Fact]
    public void Approve_Should_TransitionToApproved_When_DraftIsInReview()
    {
        var draft = CreateDraftInReview();

        var result = draft.Approve("reviewer@test.com", FixedNow);

        result.IsSuccess.Should().BeTrue();
        draft.Status.Should().Be(DraftStatus.Approved);
        draft.LastEditedBy.Should().Be("reviewer@test.com");
        draft.LastEditedAt.Should().Be(FixedNow);
    }

    [Fact]
    public void Approve_Should_Fail_When_DraftIsNotInReview()
    {
        var draft = ContractDraft.Create(
            "Draft", "author@test.com", ContractType.RestApi, ContractProtocol.OpenApi).Value;

        var result = draft.Approve("reviewer@test.com", FixedNow);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Contracts.Draft.InvalidTransition");
    }

    // ── Reject ──────────────────────────────────────────────────────────

    [Fact]
    public void Reject_Should_ReturnToEditing_When_DraftIsInReview()
    {
        var draft = CreateDraftInReview();

        var result = draft.Reject("reviewer@test.com", FixedNow);

        result.IsSuccess.Should().BeTrue();
        draft.Status.Should().Be(DraftStatus.Editing);
        draft.LastEditedBy.Should().Be("reviewer@test.com");
        draft.LastEditedAt.Should().Be(FixedNow);
    }

    [Fact]
    public void Reject_Should_Fail_When_DraftIsNotInReview()
    {
        var draft = ContractDraft.Create(
            "Draft", "author@test.com", ContractType.RestApi, ContractProtocol.OpenApi).Value;

        var result = draft.Reject("reviewer@test.com", FixedNow);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Contracts.Draft.InvalidTransition");
    }

    // ── MarkAsPublished ─────────────────────────────────────────────────

    [Fact]
    public void MarkAsPublished_Should_TransitionToPublished_When_DraftIsApproved()
    {
        var draft = CreateDraftApproved();

        var result = draft.MarkAsPublished(FixedNow);

        result.IsSuccess.Should().BeTrue();
        draft.Status.Should().Be(DraftStatus.Published);
        draft.LastEditedAt.Should().Be(FixedNow);
    }

    [Fact]
    public void MarkAsPublished_Should_Fail_When_DraftIsNotApproved()
    {
        var draft = CreateDraftInReview();

        var result = draft.MarkAsPublished(FixedNow);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Contracts.Draft.InvalidTransition");
    }

    // ── Discard ─────────────────────────────────────────────────────────

    [Fact]
    public void Discard_Should_TransitionToDiscarded_When_DraftIsEditing()
    {
        var draft = ContractDraft.Create(
            "Draft", "author@test.com", ContractType.RestApi, ContractProtocol.OpenApi).Value;

        var result = draft.Discard(FixedNow);

        result.IsSuccess.Should().BeTrue();
        draft.Status.Should().Be(DraftStatus.Discarded);
        draft.LastEditedAt.Should().Be(FixedNow);
    }

    [Fact]
    public void Discard_Should_Fail_When_DraftIsPublished()
    {
        var draft = CreateDraftApproved();
        draft.MarkAsPublished(FixedNow);

        var result = draft.Discard(FixedNow);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Contracts.Draft.InvalidTransition");
    }

    // ── Examples ─────────────────────────────────────────────────────────

    [Fact]
    public void AddExample_Should_AddToCollection()
    {
        var draft = ContractDraft.Create(
            "Draft", "author@test.com", ContractType.RestApi, ContractProtocol.OpenApi).Value;

        var example = ContractExample.CreateForDraft(
            draft.Id, "Success Response", """{"status":"ok"}""",
            "json", "response", "author@test.com", FixedNow);

        draft.AddExample(example);

        draft.Examples.Should().HaveCount(1);
        draft.Examples[0].Name.Should().Be("Success Response");
    }

    [Fact]
    public void RemoveExample_Should_RemoveFromCollection()
    {
        var draft = ContractDraft.Create(
            "Draft", "author@test.com", ContractType.RestApi, ContractProtocol.OpenApi).Value;

        var example = ContractExample.CreateForDraft(
            draft.Id, "To Remove", """{"data":"test"}""",
            "json", "request", "author@test.com", FixedNow);

        draft.AddExample(example);
        draft.Examples.Should().HaveCount(1);

        draft.RemoveExample(example.Id);

        draft.Examples.Should().BeEmpty();
    }

    // ── Helpers ──────────────────────────────────────────────────────────

    private static ContractDraft CreateDraftWithContent()
    {
        var draft = ContractDraft.Create(
            "Draft", "author@test.com", ContractType.RestApi, ContractProtocol.OpenApi).Value;
        draft.UpdateContent("""{"openapi":"3.1.0","paths":{}}""", "json", "author@test.com", FixedNow);
        return draft;
    }

    private static ContractDraft CreateDraftInReview()
    {
        var draft = CreateDraftWithContent();
        draft.SubmitForReview(FixedNow);
        return draft;
    }

    private static ContractDraft CreateDraftApproved()
    {
        var draft = CreateDraftInReview();
        draft.Approve("reviewer@test.com", FixedNow);
        return draft;
    }
}
