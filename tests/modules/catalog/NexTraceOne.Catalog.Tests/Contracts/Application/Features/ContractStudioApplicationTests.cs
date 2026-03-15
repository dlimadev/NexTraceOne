using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.Contracts.Application.Abstractions;
using NexTraceOne.Contracts.Domain.Entities;
using NexTraceOne.Contracts.Domain.Enums;
using CreateDraftFeature = NexTraceOne.Contracts.Application.Features.CreateDraft.CreateDraft;
using GetDraftFeature = NexTraceOne.Contracts.Application.Features.GetDraft.GetDraft;
using UpdateDraftContentFeature = NexTraceOne.Contracts.Application.Features.UpdateDraftContent.UpdateDraftContent;
using ListDraftsFeature = NexTraceOne.Contracts.Application.Features.ListDrafts.ListDrafts;
using SubmitDraftForReviewFeature = NexTraceOne.Contracts.Application.Features.SubmitDraftForReview.SubmitDraftForReview;
using ApproveDraftFeature = NexTraceOne.Contracts.Application.Features.ApproveDraft.ApproveDraft;
using RejectDraftFeature = NexTraceOne.Contracts.Application.Features.RejectDraft.RejectDraft;
using PublishDraftFeature = NexTraceOne.Contracts.Application.Features.PublishDraft.PublishDraft;
using GenerateDraftFromAiFeature = NexTraceOne.Contracts.Application.Features.GenerateDraftFromAi.GenerateDraftFromAi;
using AddDraftExampleFeature = NexTraceOne.Contracts.Application.Features.AddDraftExample.AddDraftExample;

namespace NexTraceOne.Contracts.Tests.Application.Features;

/// <summary>
/// Testes dos handlers da camada Application para o Contract Studio.
/// Cobre criação, consulta, edição, revisão, publicação e geração por IA de drafts.
/// </summary>
public sealed class ContractStudioApplicationTests
{
    private static readonly DateTimeOffset FixedNow = new(2025, 06, 15, 10, 0, 0, TimeSpan.Zero);

    private const string ValidSpec = """{"openapi":"3.1.0","info":{"title":"Test","version":"1.0.0"},"paths":{"/users":{"get":{"responses":{"200":{"description":"OK"}}}}}}""";

    // ── CreateDraft ─────────────────────────────────────────────────────

    [Fact]
    public async Task CreateDraft_Should_ReturnResponse_When_InputIsValid()
    {
        var draftRepo = Substitute.For<IContractDraftRepository>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var dateTimeProvider = Substitute.For<IDateTimeProvider>();
        dateTimeProvider.UtcNow.Returns(FixedNow);

        var sut = new CreateDraftFeature.Handler(draftRepo, unitOfWork, dateTimeProvider);

        var result = await sut.Handle(
            new CreateDraftFeature.Command("My API Contract", "engineer@company.com", ContractType.RestApi, ContractProtocol.OpenApi, null, null),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Title.Should().Be("My API Contract");
        draftRepo.Received(1).Add(Arg.Any<ContractDraft>());
        await unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateDraft_Should_ReturnDraftId_And_Title_When_Created()
    {
        var draftRepo = Substitute.For<IContractDraftRepository>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var dateTimeProvider = Substitute.For<IDateTimeProvider>();
        dateTimeProvider.UtcNow.Returns(FixedNow);

        var sut = new CreateDraftFeature.Handler(draftRepo, unitOfWork, dateTimeProvider);

        var result = await sut.Handle(
            new CreateDraftFeature.Command("Event Contract", "lead@company.com", ContractType.Event, ContractProtocol.AsyncApi),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.DraftId.Should().NotBeEmpty();
        result.Value.Title.Should().Be("Event Contract");
        result.Value.Status.Should().Be("Editing");
        result.Value.CreatedAt.Should().Be(FixedNow);
    }

    // ── GetDraft ────────────────────────────────────────────────────────

    [Fact]
    public async Task GetDraft_Should_ReturnDraft_When_DraftExists()
    {
        var draft = ContractDraft.Create(
            "Existing Draft", "author@test.com", ContractType.RestApi, ContractProtocol.OpenApi).Value;

        var draftRepo = Substitute.For<IContractDraftRepository>();
        draftRepo.GetByIdAsync(Arg.Any<ContractDraftId>(), Arg.Any<CancellationToken>())
            .Returns(draft);

        var sut = new GetDraftFeature.Handler(draftRepo);

        var result = await sut.Handle(
            new GetDraftFeature.Query(draft.Id.Value),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Title.Should().Be("Existing Draft");
        result.Value.Author.Should().Be("author@test.com");
        result.Value.Status.Should().Be(DraftStatus.Editing);
        result.Value.ContractType.Should().Be(ContractType.RestApi);
        result.Value.Protocol.Should().Be(ContractProtocol.OpenApi);
    }

    [Fact]
    public async Task GetDraft_Should_ReturnNotFound_When_DraftDoesNotExist()
    {
        var draftRepo = Substitute.For<IContractDraftRepository>();
        draftRepo.GetByIdAsync(Arg.Any<ContractDraftId>(), Arg.Any<CancellationToken>())
            .Returns((ContractDraft?)null);

        var sut = new GetDraftFeature.Handler(draftRepo);

        var result = await sut.Handle(
            new GetDraftFeature.Query(Guid.NewGuid()),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Contracts.Draft.NotFound");
    }

    // ── UpdateDraftContent ──────────────────────────────────────────────

    [Fact]
    public async Task UpdateDraftContent_Should_Succeed_When_DraftExists()
    {
        var draft = ContractDraft.Create(
            "Draft", "author@test.com", ContractType.RestApi, ContractProtocol.OpenApi).Value;

        var draftRepo = Substitute.For<IContractDraftRepository>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var dateTimeProvider = Substitute.For<IDateTimeProvider>();
        dateTimeProvider.UtcNow.Returns(FixedNow);

        draftRepo.GetByIdAsync(Arg.Any<ContractDraftId>(), Arg.Any<CancellationToken>())
            .Returns(draft);

        var sut = new UpdateDraftContentFeature.Handler(draftRepo, unitOfWork, dateTimeProvider);

        var result = await sut.Handle(
            new UpdateDraftContentFeature.Command(draft.Id.Value, ValidSpec, "json", "editor@test.com"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.DraftId.Should().Be(draft.Id.Value);
        await unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateDraftContent_Should_ReturnNotFound_When_DraftDoesNotExist()
    {
        var draftRepo = Substitute.For<IContractDraftRepository>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var dateTimeProvider = Substitute.For<IDateTimeProvider>();

        draftRepo.GetByIdAsync(Arg.Any<ContractDraftId>(), Arg.Any<CancellationToken>())
            .Returns((ContractDraft?)null);

        var sut = new UpdateDraftContentFeature.Handler(draftRepo, unitOfWork, dateTimeProvider);

        var result = await sut.Handle(
            new UpdateDraftContentFeature.Command(Guid.NewGuid(), ValidSpec, "json", "editor@test.com"),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Contracts.Draft.NotFound");
        await unitOfWork.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
    }

    // ── ListDrafts ──────────────────────────────────────────────────────

    [Fact]
    public async Task ListDrafts_Should_ReturnPaginatedList()
    {
        var draft1 = ContractDraft.Create(
            "Draft One", "author@test.com", ContractType.RestApi, ContractProtocol.OpenApi).Value;
        var draft2 = ContractDraft.Create(
            "Draft Two", "author@test.com", ContractType.Event, ContractProtocol.AsyncApi).Value;

        var draftRepo = Substitute.For<IContractDraftRepository>();
        draftRepo.ListAsync(null, null, null, 1, 20, Arg.Any<CancellationToken>())
            .Returns(new List<ContractDraft> { draft1, draft2 });
        draftRepo.CountAsync(null, null, null, Arg.Any<CancellationToken>())
            .Returns(2);

        var sut = new ListDraftsFeature.Handler(draftRepo);

        var result = await sut.Handle(
            new ListDraftsFeature.Query(null, null, null, 1, 20),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(2);
        result.Value.TotalCount.Should().Be(2);
        result.Value.Page.Should().Be(1);
        result.Value.PageSize.Should().Be(20);
    }

    // ── SubmitDraftForReview ────────────────────────────────────────────

    [Fact]
    public async Task SubmitDraftForReview_Should_Succeed_When_DraftHasContent()
    {
        var draft = ContractDraft.Create(
            "Draft", "author@test.com", ContractType.RestApi, ContractProtocol.OpenApi).Value;
        draft.UpdateContent(ValidSpec, "json", "author@test.com", FixedNow);

        var draftRepo = Substitute.For<IContractDraftRepository>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var dateTimeProvider = Substitute.For<IDateTimeProvider>();
        dateTimeProvider.UtcNow.Returns(FixedNow);

        draftRepo.GetByIdAsync(Arg.Any<ContractDraftId>(), Arg.Any<CancellationToken>())
            .Returns(draft);

        var sut = new SubmitDraftForReviewFeature.Handler(draftRepo, unitOfWork, dateTimeProvider);

        var result = await sut.Handle(
            new SubmitDraftForReviewFeature.Command(draft.Id.Value),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.DraftId.Should().Be(draft.Id.Value);
        await unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SubmitDraftForReview_Should_ReturnNotFound_When_DraftDoesNotExist()
    {
        var draftRepo = Substitute.For<IContractDraftRepository>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var dateTimeProvider = Substitute.For<IDateTimeProvider>();

        draftRepo.GetByIdAsync(Arg.Any<ContractDraftId>(), Arg.Any<CancellationToken>())
            .Returns((ContractDraft?)null);

        var sut = new SubmitDraftForReviewFeature.Handler(draftRepo, unitOfWork, dateTimeProvider);

        var result = await sut.Handle(
            new SubmitDraftForReviewFeature.Command(Guid.NewGuid()),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Contracts.Draft.NotFound");
    }

    // ── ApproveDraft ────────────────────────────────────────────────────

    [Fact]
    public async Task ApproveDraft_Should_Succeed_When_DraftIsInReview()
    {
        var draft = ContractDraft.Create(
            "Draft", "author@test.com", ContractType.RestApi, ContractProtocol.OpenApi).Value;
        draft.UpdateContent(ValidSpec, "json", "author@test.com", FixedNow);
        draft.SubmitForReview(FixedNow);

        var draftRepo = Substitute.For<IContractDraftRepository>();
        var reviewRepo = Substitute.For<IContractReviewRepository>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var dateTimeProvider = Substitute.For<IDateTimeProvider>();
        dateTimeProvider.UtcNow.Returns(FixedNow);

        draftRepo.GetByIdAsync(Arg.Any<ContractDraftId>(), Arg.Any<CancellationToken>())
            .Returns(draft);

        var sut = new ApproveDraftFeature.Handler(draftRepo, reviewRepo, unitOfWork, dateTimeProvider);

        var result = await sut.Handle(
            new ApproveDraftFeature.Command(draft.Id.Value, "reviewer@test.com", "Looks good"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.DraftId.Should().Be(draft.Id.Value);
        reviewRepo.Received(1).Add(Arg.Any<ContractReview>());
        await unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ApproveDraft_Should_ReturnNotFound_When_DraftDoesNotExist()
    {
        var draftRepo = Substitute.For<IContractDraftRepository>();
        var reviewRepo = Substitute.For<IContractReviewRepository>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var dateTimeProvider = Substitute.For<IDateTimeProvider>();

        draftRepo.GetByIdAsync(Arg.Any<ContractDraftId>(), Arg.Any<CancellationToken>())
            .Returns((ContractDraft?)null);

        var sut = new ApproveDraftFeature.Handler(draftRepo, reviewRepo, unitOfWork, dateTimeProvider);

        var result = await sut.Handle(
            new ApproveDraftFeature.Command(Guid.NewGuid(), "reviewer@test.com"),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Contracts.Draft.NotFound");
    }

    // ── RejectDraft ─────────────────────────────────────────────────────

    [Fact]
    public async Task RejectDraft_Should_Succeed_When_DraftIsInReview()
    {
        var draft = ContractDraft.Create(
            "Draft", "author@test.com", ContractType.RestApi, ContractProtocol.OpenApi).Value;
        draft.UpdateContent(ValidSpec, "json", "author@test.com", FixedNow);
        draft.SubmitForReview(FixedNow);

        var draftRepo = Substitute.For<IContractDraftRepository>();
        var reviewRepo = Substitute.For<IContractReviewRepository>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var dateTimeProvider = Substitute.For<IDateTimeProvider>();
        dateTimeProvider.UtcNow.Returns(FixedNow);

        draftRepo.GetByIdAsync(Arg.Any<ContractDraftId>(), Arg.Any<CancellationToken>())
            .Returns(draft);

        var sut = new RejectDraftFeature.Handler(draftRepo, reviewRepo, unitOfWork, dateTimeProvider);

        var result = await sut.Handle(
            new RejectDraftFeature.Command(draft.Id.Value, "reviewer@test.com", "Needs improvement"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.DraftId.Should().Be(draft.Id.Value);
        reviewRepo.Received(1).Add(Arg.Any<ContractReview>());
        await unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    // ── PublishDraft ────────────────────────────────────────────────────

    [Fact]
    public async Task PublishDraft_Should_CreateContractVersion_When_DraftIsApproved()
    {
        var draft = ContractDraft.Create(
            "Draft", "author@test.com", ContractType.RestApi, ContractProtocol.OpenApi).Value;
        draft.UpdateContent(ValidSpec, "json", "author@test.com", FixedNow);
        draft.SubmitForReview(FixedNow);
        draft.Approve("reviewer@test.com", FixedNow);

        var draftRepo = Substitute.For<IContractDraftRepository>();
        var versionRepo = Substitute.For<IContractVersionRepository>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var dateTimeProvider = Substitute.For<IDateTimeProvider>();
        dateTimeProvider.UtcNow.Returns(FixedNow);

        draftRepo.GetByIdAsync(Arg.Any<ContractDraftId>(), Arg.Any<CancellationToken>())
            .Returns(draft);

        var sut = new PublishDraftFeature.Handler(draftRepo, versionRepo, unitOfWork, dateTimeProvider);

        var result = await sut.Handle(
            new PublishDraftFeature.Command(draft.Id.Value, "publisher@test.com"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.ContractVersionId.Should().NotBeEmpty();
        result.Value.DraftId.Should().Be(draft.Id.Value);
        versionRepo.Received(1).Add(Arg.Any<ContractVersion>());
        await unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task PublishDraft_Should_ReturnNotFound_When_DraftDoesNotExist()
    {
        var draftRepo = Substitute.For<IContractDraftRepository>();
        var versionRepo = Substitute.For<IContractVersionRepository>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var dateTimeProvider = Substitute.For<IDateTimeProvider>();

        draftRepo.GetByIdAsync(Arg.Any<ContractDraftId>(), Arg.Any<CancellationToken>())
            .Returns((ContractDraft?)null);

        var sut = new PublishDraftFeature.Handler(draftRepo, versionRepo, unitOfWork, dateTimeProvider);

        var result = await sut.Handle(
            new PublishDraftFeature.Command(Guid.NewGuid(), "publisher@test.com"),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Contracts.Draft.NotFound");
    }

    // ── GenerateDraftFromAi ─────────────────────────────────────────────

    [Fact]
    public async Task GenerateDraftFromAi_Should_ReturnDraft_When_InputIsValid()
    {
        var draftRepo = Substitute.For<IContractDraftRepository>();
        var unitOfWork = Substitute.For<IUnitOfWork>();

        var sut = new GenerateDraftFromAiFeature.Handler(draftRepo, unitOfWork);

        var result = await sut.Handle(
            new GenerateDraftFromAiFeature.Command(
                "AI Generated API",
                "engineer@company.com",
                ContractType.RestApi,
                ContractProtocol.OpenApi,
                "Generate a user management API"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.DraftId.Should().NotBeEmpty();
        result.Value.GeneratedContent.Should().NotBeNullOrWhiteSpace();
        draftRepo.Received(1).Add(Arg.Any<ContractDraft>());
        await unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    // ── AddDraftExample ─────────────────────────────────────────────────

    [Fact]
    public async Task AddDraftExample_Should_ReturnExampleId()
    {
        var draft = ContractDraft.Create(
            "Draft", "author@test.com", ContractType.RestApi, ContractProtocol.OpenApi).Value;

        var draftRepo = Substitute.For<IContractDraftRepository>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var dateTimeProvider = Substitute.For<IDateTimeProvider>();
        dateTimeProvider.UtcNow.Returns(FixedNow);

        draftRepo.GetByIdAsync(Arg.Any<ContractDraftId>(), Arg.Any<CancellationToken>())
            .Returns(draft);

        var sut = new AddDraftExampleFeature.Handler(draftRepo, unitOfWork, dateTimeProvider);

        var result = await sut.Handle(
            new AddDraftExampleFeature.Command(
                draft.Id.Value,
                "Success Response",
                """{"status":"ok","data":{"id":1}}""",
                "json",
                "response",
                "author@test.com",
                "Example of a successful response"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.ExampleId.Should().NotBeEmpty();
        result.Value.DraftId.Should().Be(draft.Id.Value);
        await unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }
}
