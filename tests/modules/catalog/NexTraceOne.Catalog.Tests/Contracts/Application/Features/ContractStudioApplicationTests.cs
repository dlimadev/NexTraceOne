using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.Catalog.Application.Contracts.Abstractions;
using NexTraceOne.Catalog.Application.Graph.Abstractions;
using NexTraceOne.Catalog.Domain.Contracts.Entities;
using NexTraceOne.Catalog.Domain.Contracts.Enums;
using NexTraceOne.Catalog.Domain.Graph.Entities;
using NexTraceOne.Catalog.Domain.Graph.Enums;

using CreateDraftFeature = NexTraceOne.Catalog.Application.Contracts.Features.CreateDraft.CreateDraft;
using ExportDraftFeature = NexTraceOne.Catalog.Application.Contracts.Features.ExportDraft.ExportDraft;
using GetDraftFeature = NexTraceOne.Catalog.Application.Contracts.Features.GetDraft.GetDraft;
using UpdateDraftContentFeature = NexTraceOne.Catalog.Application.Contracts.Features.UpdateDraftContent.UpdateDraftContent;
using ListDraftsFeature = NexTraceOne.Catalog.Application.Contracts.Features.ListDrafts.ListDrafts;
using SubmitDraftForReviewFeature = NexTraceOne.Catalog.Application.Contracts.Features.SubmitDraftForReview.SubmitDraftForReview;
using ApproveDraftFeature = NexTraceOne.Catalog.Application.Contracts.Features.ApproveDraft.ApproveDraft;
using RejectDraftFeature = NexTraceOne.Catalog.Application.Contracts.Features.RejectDraft.RejectDraft;
using PublishDraftFeature = NexTraceOne.Catalog.Application.Contracts.Features.PublishDraft.PublishDraft;
using GenerateDraftFromAiFeature = NexTraceOne.Catalog.Application.Contracts.Features.GenerateDraftFromAi.GenerateDraftFromAi;
using IAiDraftGenerator = NexTraceOne.Catalog.Application.Contracts.Features.GenerateDraftFromAi.IAiDraftGenerator;
using AddDraftExampleFeature = NexTraceOne.Catalog.Application.Contracts.Features.AddDraftExample.AddDraftExample;

namespace NexTraceOne.Catalog.Tests.Contracts.Application.Features;

public sealed class ContractStudioApplicationTests
{
    private static readonly DateTimeOffset FixedNow = new(2025, 06, 15, 10, 0, 0, TimeSpan.Zero);
    private static readonly Guid TestServiceId = Guid.NewGuid();

    private const string ValidSpec = """{"openapi":"3.1.0","info":{"title":"Test","version":"1.0.0"},"paths":{"/users":{"get":{"responses":{"200":{"description":"OK"}}}}}}""";

    private static IContractsUnitOfWork CreateUnitOfWork() => Substitute.For<IContractsUnitOfWork>();

    private static IServiceAssetRepository CreateServiceRepo()
    {
        var service = ServiceAsset.Create("TestService", "test-domain", "test-team", Guid.NewGuid());
        service.UpdateDetails("TestService", "", ServiceType.IntegrationComponent, "", Criticality.Low, LifecycleStatus.Planning, ExposureType.Internal, "", "");
        var repo = Substitute.For<IServiceAssetRepository>();
        repo.GetByIdAsync(Arg.Any<ServiceAssetId>(), Arg.Any<CancellationToken>())
            .Returns(service);
        return repo;
    }

    // ── CreateDraft ─────────────────────────────────────────────────────

    [Fact]
    public async Task CreateDraft_Should_ReturnResponse_When_InputIsValid()
    {
        var draftRepo = Substitute.For<IContractDraftRepository>();
        var unitOfWork = CreateUnitOfWork();
        var dateTimeProvider = Substitute.For<IDateTimeProvider>();
        dateTimeProvider.UtcNow.Returns(FixedNow);

        var sut = new CreateDraftFeature.Handler(draftRepo, CreateServiceRepo(), unitOfWork, dateTimeProvider);

        var result = await sut.Handle(
            new CreateDraftFeature.Command("My API Contract", "engineer@company.com", ContractType.RestApi, ContractProtocol.OpenApi, TestServiceId),
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
        var unitOfWork = CreateUnitOfWork();
        var dateTimeProvider = Substitute.For<IDateTimeProvider>();
        dateTimeProvider.UtcNow.Returns(FixedNow);

        var sut = new CreateDraftFeature.Handler(draftRepo, CreateServiceRepo(), unitOfWork, dateTimeProvider);

        var result = await sut.Handle(
            new CreateDraftFeature.Command("Event Contract", "lead@company.com", ContractType.Event, ContractProtocol.AsyncApi, TestServiceId),
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
            "Existing Draft", "author@test.com", ContractType.RestApi, ContractProtocol.OpenApi, TestServiceId).Value;

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
            "Draft", "author@test.com", ContractType.RestApi, ContractProtocol.OpenApi, TestServiceId).Value;

        var draftRepo = Substitute.For<IContractDraftRepository>();
        var unitOfWork = CreateUnitOfWork();
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
        var unitOfWork = CreateUnitOfWork();
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
            "Draft One", "author@test.com", ContractType.RestApi, ContractProtocol.OpenApi, TestServiceId).Value;
        var draft2 = ContractDraft.Create(
            "Draft Two", "author@test.com", ContractType.Event, ContractProtocol.AsyncApi, TestServiceId).Value;

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
            "Draft", "author@test.com", ContractType.RestApi, ContractProtocol.OpenApi, TestServiceId).Value;
        draft.UpdateContent(ValidSpec, "json", "author@test.com", FixedNow);

        var draftRepo = Substitute.For<IContractDraftRepository>();
        var unitOfWork = CreateUnitOfWork();
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
        var unitOfWork = CreateUnitOfWork();
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
            "Draft", "author@test.com", ContractType.RestApi, ContractProtocol.OpenApi, TestServiceId).Value;
        draft.UpdateContent(ValidSpec, "json", "author@test.com", FixedNow);
        draft.SubmitForReview(FixedNow);

        var draftRepo = Substitute.For<IContractDraftRepository>();
        var reviewRepo = Substitute.For<IContractReviewRepository>();
        var unitOfWork = CreateUnitOfWork();
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
        var unitOfWork = CreateUnitOfWork();
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
            "Draft", "author@test.com", ContractType.RestApi, ContractProtocol.OpenApi, TestServiceId).Value;
        draft.UpdateContent(ValidSpec, "json", "author@test.com", FixedNow);
        draft.SubmitForReview(FixedNow);

        var draftRepo = Substitute.For<IContractDraftRepository>();
        var reviewRepo = Substitute.For<IContractReviewRepository>();
        var unitOfWork = CreateUnitOfWork();
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
        var service = ServiceAsset.Create("payments-service", "Finance", "Payments Team", Guid.NewGuid());
        var draft = ContractDraft.Create(
            "Draft", "author@test.com", ContractType.RestApi, ContractProtocol.OpenApi, service.Id.Value).Value;
        draft.UpdateContent(ValidSpec, "json", "author@test.com", FixedNow);
        draft.SubmitForReview(FixedNow);
        draft.Approve("reviewer@test.com", FixedNow);

        var draftRepo = Substitute.For<IContractDraftRepository>();
        var versionRepo = Substitute.For<IContractVersionRepository>();
        var apiAssetRepo = Substitute.For<IApiAssetRepository>();
        var serviceAssetRepo = Substitute.For<IServiceAssetRepository>();
        var unitOfWork = CreateUnitOfWork();
        var graphUnitOfWork = Substitute.For<ICatalogGraphUnitOfWork>();
        var dateTimeProvider = Substitute.For<IDateTimeProvider>();
        dateTimeProvider.UtcNow.Returns(FixedNow);

        draftRepo.GetByIdAsync(Arg.Any<ContractDraftId>(), Arg.Any<CancellationToken>())
            .Returns(draft);
        serviceAssetRepo.GetByIdAsync(Arg.Any<ServiceAssetId>(), Arg.Any<CancellationToken>())
            .Returns(service);

        var sut = new PublishDraftFeature.Handler(
            draftRepo,
            versionRepo,
            apiAssetRepo,
            serviceAssetRepo,
            unitOfWork,
            graphUnitOfWork,
            dateTimeProvider);

        var result = await sut.Handle(
            new PublishDraftFeature.Command(draft.Id.Value, "publisher@test.com"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.ContractVersionId.Should().NotBeEmpty();
        result.Value.DraftId.Should().Be(draft.Id.Value);
        apiAssetRepo.Received(1).Add(Arg.Any<ApiAsset>());
        versionRepo.Received(1).Add(Arg.Any<ContractVersion>());
        await unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
        await graphUnitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task PublishDraft_Should_ReturnNotFound_When_DraftDoesNotExist()
    {
        var draftRepo = Substitute.For<IContractDraftRepository>();
        var versionRepo = Substitute.For<IContractVersionRepository>();
        var apiAssetRepo = Substitute.For<IApiAssetRepository>();
        var serviceAssetRepo = Substitute.For<IServiceAssetRepository>();
        var unitOfWork = CreateUnitOfWork();
        var graphUnitOfWork = Substitute.For<ICatalogGraphUnitOfWork>();
        var dateTimeProvider = Substitute.For<IDateTimeProvider>();

        draftRepo.GetByIdAsync(Arg.Any<ContractDraftId>(), Arg.Any<CancellationToken>())
            .Returns((ContractDraft?)null);

        var sut = new PublishDraftFeature.Handler(
            draftRepo,
            versionRepo,
            apiAssetRepo,
            serviceAssetRepo,
            unitOfWork,
            graphUnitOfWork,
            dateTimeProvider);

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
        var unitOfWork = CreateUnitOfWork();

        var sut = new GenerateDraftFromAiFeature.Handler(draftRepo, CreateServiceRepo(), unitOfWork);

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
        result.Value.AiGenerated.Should().BeFalse("no AI generator was injected, so template fallback was used");
        draftRepo.Received(1).Add(Arg.Any<ContractDraft>());
        await unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GenerateDraftFromAi_WithAiGenerator_Should_ReturnAiGeneratedTrue()
    {
        var draftRepo = Substitute.For<IContractDraftRepository>();
        var unitOfWork = CreateUnitOfWork();
        var aiGenerator = Substitute.For<IAiDraftGenerator>();
        aiGenerator.GenerateAsync(
                Arg.Any<ContractProtocol>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(("openapi: '3.1.0'\ninfo:\n  title: 'Test'\n  version: '1.0.0'\npaths: {}", "yaml"));

        var sut = new GenerateDraftFromAiFeature.Handler(draftRepo, CreateServiceRepo(), unitOfWork, aiGenerator);

        var result = await sut.Handle(
            new GenerateDraftFromAiFeature.Command(
                "AI Generated API",
                "engineer@company.com",
                ContractType.RestApi,
                ContractProtocol.OpenApi,
                "Generate a user management API"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.AiGenerated.Should().BeTrue("AI generator was available and succeeded");
    }

    [Fact]
    public async Task GenerateDraftFromAi_WithAiGeneratorReturningNull_Should_ReturnAiGeneratedFalse()
    {
        var draftRepo = Substitute.For<IContractDraftRepository>();
        var unitOfWork = CreateUnitOfWork();
        var aiGenerator = Substitute.For<IAiDraftGenerator>();
        aiGenerator.GenerateAsync(
                Arg.Any<ContractProtocol>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(((string Content, string Format)?)null);

        var sut = new GenerateDraftFromAiFeature.Handler(draftRepo, CreateServiceRepo(), unitOfWork, aiGenerator);

        var result = await sut.Handle(
            new GenerateDraftFromAiFeature.Command(
                "AI Generated API",
                "engineer@company.com",
                ContractType.RestApi,
                ContractProtocol.OpenApi,
                "Generate a user management API"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.AiGenerated.Should().BeFalse("AI generator returned null, so template fallback was used");
    }

    // ── AddDraftExample ─────────────────────────────────────────────────

    [Fact]
    public async Task AddDraftExample_Should_ReturnExampleId()
    {
        var draft = ContractDraft.Create(
            "Draft", "author@test.com", ContractType.RestApi, ContractProtocol.OpenApi, TestServiceId).Value;

        var draftRepo = Substitute.For<IContractDraftRepository>();
        var unitOfWork = CreateUnitOfWork();
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

    // ── ExportDraft ─────────────────────────────────────────────────────

    [Fact]
    public async Task ExportDraft_Should_ReturnSpecContent_When_DraftExists()
    {
        var draft = ContractDraft.Create(
            "Export Test API", "author@test.com", ContractType.RestApi, ContractProtocol.OpenApi, TestServiceId).Value;

        draft.UpdateContent(ValidSpec, "json", "author@test.com", FixedNow);

        var draftRepo = Substitute.For<IContractDraftRepository>();
        draftRepo.GetByIdAsync(Arg.Any<ContractDraftId>(), Arg.Any<CancellationToken>())
            .Returns(draft);

        var sut = new ExportDraftFeature.Handler(draftRepo);

        var result = await sut.Handle(
            new ExportDraftFeature.Query(draft.Id.Value),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.DraftId.Should().Be(draft.Id.Value);
        result.Value.Title.Should().Be("Export Test API");
        result.Value.SpecContent.Should().Be(ValidSpec);
        result.Value.Format.Should().Be("json");
        result.Value.ProposedVersion.Should().Be("1.0.0");
        result.Value.Protocol.Should().Be("OpenApi");
        result.Value.ContractType.Should().Be("RestApi");
    }

    [Fact]
    public async Task ExportDraft_Should_ReturnError_When_DraftNotFound()
    {
        var draftRepo = Substitute.For<IContractDraftRepository>();
        draftRepo.GetByIdAsync(Arg.Any<ContractDraftId>(), Arg.Any<CancellationToken>())
            .Returns((ContractDraft?)null);

        var sut = new ExportDraftFeature.Handler(draftRepo);

        var result = await sut.Handle(
            new ExportDraftFeature.Query(Guid.NewGuid()),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Contracts.Draft.NotFound");
    }

    [Fact]
    public async Task ExportDraft_Should_ReturnEmptySpecContent_When_DraftHasNoContent()
    {
        var draft = ContractDraft.Create(
            "Empty Draft", "author@test.com", ContractType.Soap, ContractProtocol.Wsdl, TestServiceId).Value;

        var draftRepo = Substitute.For<IContractDraftRepository>();
        draftRepo.GetByIdAsync(Arg.Any<ContractDraftId>(), Arg.Any<CancellationToken>())
            .Returns(draft);

        var sut = new ExportDraftFeature.Handler(draftRepo);

        var result = await sut.Handle(
            new ExportDraftFeature.Query(draft.Id.Value),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.SpecContent.Should().BeEmpty();
        result.Value.Format.Should().Be("yaml");
        result.Value.Protocol.Should().Be("Wsdl");
        result.Value.ContractType.Should().Be("Soap");
    }

    // ── Theory: multi-protocolo ─────────────────────────────────────────

    [Theory]
    [InlineData(ContractType.RestApi, ContractProtocol.OpenApi, "OpenApi")]
    [InlineData(ContractType.Event, ContractProtocol.AsyncApi, "AsyncApi")]
    [InlineData(ContractType.Soap, ContractProtocol.Wsdl, "Wsdl")]
    public async Task CreateDraft_Should_ReturnCorrectProtocol_ForMultipleProtocols(
        ContractType contractType,
        ContractProtocol protocol,
        string expectedProtocol)
    {
        var draftRepo = Substitute.For<IContractDraftRepository>();
        var unitOfWork = CreateUnitOfWork();
        var dateTimeProvider = Substitute.For<IDateTimeProvider>();
        dateTimeProvider.UtcNow.Returns(FixedNow);

        var sut = new CreateDraftFeature.Handler(draftRepo, CreateServiceRepo(), unitOfWork, dateTimeProvider);

        var result = await sut.Handle(
            new CreateDraftFeature.Command($"Test {expectedProtocol} Contract", "engineer@test.com", contractType, protocol, TestServiceId),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.DraftId.Should().NotBeEmpty();
        result.Value.Title.Should().Contain(expectedProtocol);
        draftRepo.Received(1).Add(Arg.Is<ContractDraft>(d =>
            d.ContractType == contractType && d.Protocol == protocol));
    }

    [Theory]
    [InlineData(ContractType.RestApi, ContractProtocol.OpenApi)]
    [InlineData(ContractType.Event, ContractProtocol.AsyncApi)]
    [InlineData(ContractType.Soap, ContractProtocol.Wsdl)]
    public async Task ExportDraft_Should_ReturnCorrectProtocol_ForMultipleProtocols(
        ContractType contractType,
        ContractProtocol protocol)
    {
        var draft = ContractDraft.Create(
            $"Draft {protocol}", "author@test.com", contractType, protocol, TestServiceId).Value;

        var draftRepo = Substitute.For<IContractDraftRepository>();
        draftRepo.GetByIdAsync(Arg.Any<ContractDraftId>(), Arg.Any<CancellationToken>())
            .Returns(draft);

        var sut = new ExportDraftFeature.Handler(draftRepo);

        var result = await sut.Handle(
            new ExportDraftFeature.Query(draft.Id.Value),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Protocol.Should().Be(protocol.ToString());
        result.Value.ContractType.Should().Be(contractType.ToString());
    }

    [Theory]
    [InlineData(ContractType.RestApi, ContractProtocol.OpenApi, "Generate a REST API")]
    [InlineData(ContractType.Event, ContractProtocol.AsyncApi, "Generate a Kafka event contract")]
    [InlineData(ContractType.Soap, ContractProtocol.Wsdl, "Generate a SOAP web service")]
    public async Task GenerateDraftFromAi_Should_Succeed_ForMultipleProtocols(
        ContractType contractType,
        ContractProtocol protocol,
        string prompt)
    {
        var draftRepo = Substitute.For<IContractDraftRepository>();
        var unitOfWork = CreateUnitOfWork();

        var sut = new GenerateDraftFromAiFeature.Handler(draftRepo, CreateServiceRepo(), unitOfWork);

        var result = await sut.Handle(
            new GenerateDraftFromAiFeature.Command(
                $"AI {protocol} Contract",
                "engineer@test.com",
                contractType,
                protocol,
                prompt),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.DraftId.Should().NotBeEmpty();
        result.Value.GeneratedContent.Should().NotBeNullOrWhiteSpace();
        draftRepo.Received(1).Add(Arg.Any<ContractDraft>());
        await unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }
}
