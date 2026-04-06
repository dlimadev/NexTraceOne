using FluentAssertions;

using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.Catalog.Application.Contracts.Abstractions;

using NSubstitute;

using NexTraceOne.Catalog.Application.Graph.Abstractions;
using NexTraceOne.Catalog.Domain.Contracts.Entities;
using NexTraceOne.Catalog.Domain.Contracts.Enums;
using NexTraceOne.Catalog.Domain.Graph.Entities;
using ListContractsFeature = NexTraceOne.Catalog.Application.Contracts.Features.ListContracts.ListContracts;
using GetContractsSummaryFeature = NexTraceOne.Catalog.Application.Contracts.Features.GetContractsSummary.GetContractsSummary;
using ListContractsByServiceFeature = NexTraceOne.Catalog.Application.Contracts.Features.ListContractsByService.ListContractsByService;
using RejectDraftFeature = NexTraceOne.Catalog.Application.Contracts.Features.RejectDraft.RejectDraft;
using ApproveDraftFeature = NexTraceOne.Catalog.Application.Contracts.Features.ApproveDraft.ApproveDraft;
using SubmitDraftForReviewFeature = NexTraceOne.Catalog.Application.Contracts.Features.SubmitDraftForReview.SubmitDraftForReview;
using GetDraftFeature = NexTraceOne.Catalog.Application.Contracts.Features.GetDraft.GetDraft;

namespace NexTraceOne.Catalog.Tests.Contracts.Application.Features;

/// <summary>
/// Testes dos novos handlers de Contract Governance da Fase 4.2 — listagem,
/// resumo e relação service→contracts.
/// </summary>
public sealed class ContractGovernanceApplicationTests
{
    // ── ListContracts ──────────────────────────────────────────────────

    [Fact]
    public async Task ListContracts_Should_ReturnPaginatedResult()
    {
        var contractRepository = Substitute.For<IContractVersionRepository>();
        var apiRepository = Substitute.For<IApiAssetRepository>();
        var sut = new ListContractsFeature.Handler(contractRepository, apiRepository);

        var contract = ContractVersion.Import(
            Guid.NewGuid(), "1.0.0", "{}", "json", "upload", ContractProtocol.OpenApi).Value;

        contractRepository.ListLatestPerApiAssetAsync(null, null, null, 1, 20, Arg.Any<CancellationToken>())
            .Returns((new List<ContractVersion> { contract }, 1));

        apiRepository.ListByApiAssetIdsAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(new Dictionary<Guid, ApiAsset>());

        var result = await sut.Handle(
            new ListContractsFeature.Query(null, null, null, 1, 20),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(1);
        result.Value.TotalCount.Should().Be(1);
        result.Value.Items[0].Protocol.Should().Be("OpenApi");
        result.Value.Items[0].LifecycleState.Should().Be("Draft");
    }

    [Fact]
    public async Task ListContracts_Should_PassFilters_To_Repository()
    {
        var contractRepository = Substitute.For<IContractVersionRepository>();
        var apiRepository = Substitute.For<IApiAssetRepository>();
        var sut = new ListContractsFeature.Handler(contractRepository, apiRepository);

        contractRepository.ListLatestPerApiAssetAsync(
            ContractProtocol.AsyncApi, ContractLifecycleState.Approved, "test", 2, 10,
            Arg.Any<CancellationToken>())
            .Returns((new List<ContractVersion>(), 0));

        var result = await sut.Handle(
            new ListContractsFeature.Query(ContractProtocol.AsyncApi, ContractLifecycleState.Approved, "test", 2, 10),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().BeEmpty();
        await contractRepository.Received(1).ListLatestPerApiAssetAsync(
            ContractProtocol.AsyncApi, ContractLifecycleState.Approved, "test", 2, 10,
            Arg.Any<CancellationToken>());
    }

    // ── GetContractsSummary ──────────────────────────────────────────

    [Fact]
    public async Task GetContractsSummary_Should_ReturnAggregatedCounts()
    {
        var repository = Substitute.For<IContractVersionRepository>();
        var sut = new GetContractsSummaryFeature.Handler(repository);

        var summaryData = new ContractSummaryData(
            TotalVersions: 10,
            DistinctContracts: 5,
            DraftCount: 2,
            InReviewCount: 1,
            ApprovedCount: 3,
            LockedCount: 2,
            DeprecatedCount: 2,
            ByProtocol: new List<ProtocolCount>
            {
                new("OpenApi", 7),
                new("AsyncApi", 3)
            });

        repository.GetSummaryAsync(Arg.Any<CancellationToken>()).Returns(summaryData);

        var result = await sut.Handle(
            new GetContractsSummaryFeature.Query(),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TotalVersions.Should().Be(10);
        result.Value.DistinctContracts.Should().Be(5);
        result.Value.DraftCount.Should().Be(2);
        result.Value.ByProtocol.Should().HaveCount(2);
        result.Value.ByProtocol[0].Protocol.Should().Be("OpenApi");
    }

    // ── ListContractsByService ───────────────────────────────────────

    [Fact]
    public async Task ListContractsByService_Should_ReturnContracts_WhenServiceHasApis()
    {
        var apiRepo = Substitute.For<IApiAssetRepository>();
        var contractRepo = Substitute.For<IContractVersionRepository>();
        var sut = new ListContractsByServiceFeature.Handler(apiRepo, contractRepo);

        var service = ServiceAsset.Create("test-service", "Finance", "Team A");
        var api = ApiAsset.Register("Payments API", "/api/payments", "1.0.0", "Public", service);

        apiRepo.ListByServiceIdAsync(Arg.Any<ServiceAssetId>(), Arg.Any<CancellationToken>())
            .Returns(new List<ApiAsset> { api });

        var contract = ContractVersion.Import(
            api.Id.Value, "1.0.0", "{}", "json", "upload", ContractProtocol.OpenApi).Value;

        contractRepo.ListByApiAssetIdsAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(new List<ContractVersion> { contract });

        var result = await sut.Handle(
            new ListContractsByServiceFeature.Query(service.Id.Value),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Contracts.Should().HaveCount(1);
        result.Value.Contracts[0].ApiName.Should().Be("Payments API");
        result.Value.Contracts[0].Protocol.Should().Be("OpenApi");
    }

    [Fact]
    public async Task ListContractsByService_Should_ReturnEmpty_WhenServiceHasNoApis()
    {
        var apiRepo = Substitute.For<IApiAssetRepository>();
        var contractRepo = Substitute.For<IContractVersionRepository>();
        var sut = new ListContractsByServiceFeature.Handler(apiRepo, contractRepo);

        apiRepo.ListByServiceIdAsync(Arg.Any<ServiceAssetId>(), Arg.Any<CancellationToken>())
            .Returns(new List<ApiAsset>());

        var result = await sut.Handle(
            new ListContractsByServiceFeature.Query(Guid.NewGuid()),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Contracts.Should().BeEmpty();
        result.Value.TotalCount.Should().Be(0);
    }
}

/// <summary>
/// Testes das funcionalidades de ciclo de vida de draft: rejeição com motivo,
/// re-submissão após rejeição, aprovação e verificação de transição de estado.
/// </summary>
public sealed class ContractDraftReviewCycleTests
{
    private static readonly DateTimeOffset FixedNow = new(2025, 06, 15, 10, 0, 0, TimeSpan.Zero);

    private const string ValidSpec = """{"openapi":"3.1.0","info":{"title":"Test","version":"1.0.0"},"paths":{"/users":{"get":{"responses":{"200":{"description":"OK"}}}}}}""";

    private static IContractsUnitOfWork CreateUnitOfWork() => Substitute.For<IContractsUnitOfWork>();

    private static ContractDraft CreateDraftInReview()
    {
        var draft = ContractDraft.Create(
            "Governance Draft", "author@test.com", ContractType.RestApi, ContractProtocol.OpenApi).Value;
        draft.UpdateContent(ValidSpec, "json", "author@test.com", FixedNow);
        draft.SubmitForReview(FixedNow);
        return draft;
    }

    // ── RejectDraft com motivo ──────────────────────────────────────────

    [Fact]
    public async Task RejectDraft_WithReason_Should_ReturnToDraft_State()
    {
        var draft = CreateDraftInReview();
        draft.Status.Should().Be(DraftStatus.InReview);

        var draftRepo = Substitute.For<IContractDraftRepository>();
        var reviewRepo = Substitute.For<IContractReviewRepository>();
        var unitOfWork = CreateUnitOfWork();
        var dateTimeProvider = Substitute.For<IDateTimeProvider>();
        dateTimeProvider.UtcNow.Returns(FixedNow);

        draftRepo.GetByIdAsync(Arg.Any<ContractDraftId>(), Arg.Any<CancellationToken>())
            .Returns(draft);

        var sut = new RejectDraftFeature.Handler(draftRepo, reviewRepo, unitOfWork, dateTimeProvider);

        var result = await sut.Handle(
            new RejectDraftFeature.Command(draft.Id.Value, "reviewer@test.com", "Missing required fields"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        draft.Status.Should().Be(DraftStatus.Editing);
        reviewRepo.Received(1).Add(Arg.Any<ContractReview>());
        await unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RejectDraft_WithoutComment_Should_Succeed()
    {
        var draft = CreateDraftInReview();

        var draftRepo = Substitute.For<IContractDraftRepository>();
        var reviewRepo = Substitute.For<IContractReviewRepository>();
        var unitOfWork = CreateUnitOfWork();
        var dateTimeProvider = Substitute.For<IDateTimeProvider>();
        dateTimeProvider.UtcNow.Returns(FixedNow);

        draftRepo.GetByIdAsync(Arg.Any<ContractDraftId>(), Arg.Any<CancellationToken>())
            .Returns(draft);

        var sut = new RejectDraftFeature.Handler(draftRepo, reviewRepo, unitOfWork, dateTimeProvider);

        var result = await sut.Handle(
            new RejectDraftFeature.Command(draft.Id.Value, "reviewer@test.com"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        draft.Status.Should().Be(DraftStatus.Editing);
    }

    [Fact]
    public async Task RejectDraft_Should_ReturnNotFound_When_DraftDoesNotExist()
    {
        var draftRepo = Substitute.For<IContractDraftRepository>();
        var reviewRepo = Substitute.For<IContractReviewRepository>();
        var unitOfWork = CreateUnitOfWork();
        var dateTimeProvider = Substitute.For<IDateTimeProvider>();

        draftRepo.GetByIdAsync(Arg.Any<ContractDraftId>(), Arg.Any<CancellationToken>())
            .Returns((ContractDraft?)null);

        var sut = new RejectDraftFeature.Handler(draftRepo, reviewRepo, unitOfWork, dateTimeProvider);

        var result = await sut.Handle(
            new RejectDraftFeature.Command(Guid.NewGuid(), "reviewer@test.com", "reason"),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Contracts.Draft.NotFound");
    }

    // ── Re-submissão após rejeição ─────────────────────────────────────

    [Fact]
    public async Task ResubmitAfterRejection_Should_TransitionToInReview()
    {
        var draft = CreateDraftInReview();
        draft.Reject("reviewer@test.com", FixedNow);
        draft.Status.Should().Be(DraftStatus.Editing);

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
        draft.Status.Should().Be(DraftStatus.InReview);
        await unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    // ── Aprovação e verificação de transição ───────────────────────────

    [Fact]
    public async Task ApproveDraft_Should_TransitionToApproved_State()
    {
        var draft = CreateDraftInReview();

        var draftRepo = Substitute.For<IContractDraftRepository>();
        var reviewRepo = Substitute.For<IContractReviewRepository>();
        var unitOfWork = CreateUnitOfWork();
        var dateTimeProvider = Substitute.For<IDateTimeProvider>();
        dateTimeProvider.UtcNow.Returns(FixedNow);

        draftRepo.GetByIdAsync(Arg.Any<ContractDraftId>(), Arg.Any<CancellationToken>())
            .Returns(draft);

        var sut = new ApproveDraftFeature.Handler(draftRepo, reviewRepo, unitOfWork, dateTimeProvider);

        var result = await sut.Handle(
            new ApproveDraftFeature.Command(draft.Id.Value, "lead@test.com", "LGTM"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.DraftId.Should().Be(draft.Id.Value);
        draft.Status.Should().Be(DraftStatus.Approved);
        reviewRepo.Received(1).Add(Arg.Any<ContractReview>());
        await unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ApproveDraft_Should_ReturnError_When_DraftIsEditing()
    {
        var draft = ContractDraft.Create(
            "Draft", "author@test.com", ContractType.RestApi, ContractProtocol.OpenApi).Value;
        draft.Status.Should().Be(DraftStatus.Editing);

        var draftRepo = Substitute.For<IContractDraftRepository>();
        var reviewRepo = Substitute.For<IContractReviewRepository>();
        var unitOfWork = CreateUnitOfWork();
        var dateTimeProvider = Substitute.For<IDateTimeProvider>();
        dateTimeProvider.UtcNow.Returns(FixedNow);

        draftRepo.GetByIdAsync(Arg.Any<ContractDraftId>(), Arg.Any<CancellationToken>())
            .Returns(draft);

        var sut = new ApproveDraftFeature.Handler(draftRepo, reviewRepo, unitOfWork, dateTimeProvider);

        var result = await sut.Handle(
            new ApproveDraftFeature.Command(draft.Id.Value, "lead@test.com"),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("InvalidTransition");
    }

    // ── Obter draft não encontrado ──────────────────────────────────────

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

    // ── Ciclo completo: submit → approve → state Published ─────────────

    [Fact]
    public void DraftLifecycle_Submit_Approve_MarkPublished_Should_Succeed()
    {
        var draft = ContractDraft.Create(
            "Full Cycle Draft", "author@test.com", ContractType.RestApi, ContractProtocol.OpenApi).Value;
        draft.UpdateContent(ValidSpec, "json", "author@test.com", FixedNow);

        var submitResult = draft.SubmitForReview(FixedNow);
        submitResult.IsSuccess.Should().BeTrue();
        draft.Status.Should().Be(DraftStatus.InReview);

        var approveResult = draft.Approve("reviewer@test.com", FixedNow);
        approveResult.IsSuccess.Should().BeTrue();
        draft.Status.Should().Be(DraftStatus.Approved);

        var publishResult = draft.MarkAsPublished(FixedNow);
        publishResult.IsSuccess.Should().BeTrue();
        draft.Status.Should().Be(DraftStatus.Published);
    }
}
