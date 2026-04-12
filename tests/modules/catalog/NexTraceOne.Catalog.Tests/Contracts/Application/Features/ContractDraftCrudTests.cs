using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.Catalog.Application.Contracts.Abstractions;
using NexTraceOne.Catalog.Domain.Contracts.Entities;
using NexTraceOne.Catalog.Domain.Contracts.Enums;

using AddDraftExampleFeature = NexTraceOne.Catalog.Application.Contracts.Features.AddDraftExample.AddDraftExample;
using ApproveDraftFeature = NexTraceOne.Catalog.Application.Contracts.Features.ApproveDraft.ApproveDraft;
using CreateDraftFeature = NexTraceOne.Catalog.Application.Contracts.Features.CreateDraft.CreateDraft;
using CreateSoapDraftFeature = NexTraceOne.Catalog.Application.Contracts.Features.CreateSoapDraft.CreateSoapDraft;
using ExportDraftFeature = NexTraceOne.Catalog.Application.Contracts.Features.ExportDraft.ExportDraft;

namespace NexTraceOne.Catalog.Tests.Contracts.Application.Features;

/// <summary>
/// Testes CRUD dos handlers de draft na camada Application do módulo Contracts.
/// Cobre CreateDraft, ApproveDraft, ExportDraft, AddDraftExample e CreateSoapDraft.
/// </summary>
public sealed class ContractDraftCrudTests
{
    private static readonly DateTimeOffset FixedNow = new(2025, 06, 01, 10, 0, 0, TimeSpan.Zero);

    private static IContractsUnitOfWork CreateUnitOfWork() => Substitute.For<IContractsUnitOfWork>();

    // ── Helper: cria um draft em estado InReview para testes de aprovação ──

    private static ContractDraft CreateDraftInReview()
    {
        var draft = ContractDraft.Create(
            "Review Draft", "author", ContractType.RestApi, ContractProtocol.OpenApi).Value;
        draft.UpdateContent("openapi: 3.0.0", "yaml", "author", FixedNow);
        draft.SubmitForReview(FixedNow);
        return draft;
    }

    // ── CreateDraft ───────────────────────────────────────────────────────

    [Fact]
    public async Task CreateDraft_Should_ReturnResponse_When_ValidCommand()
    {
        var repository = Substitute.For<IContractDraftRepository>();
        var unitOfWork = CreateUnitOfWork();
        var dateTimeProvider = Substitute.For<IDateTimeProvider>();
        var sut = new CreateDraftFeature.Handler(repository, unitOfWork, dateTimeProvider);

        repository.ListAsync(
                Arg.Any<DraftStatus?>(), Arg.Any<Guid?>(), Arg.Any<string?>(),
                Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(new List<ContractDraft>());
        dateTimeProvider.UtcNow.Returns(FixedNow);

        var result = await sut.Handle(
            new CreateDraftFeature.Command("My API", "author", ContractType.RestApi, ContractProtocol.OpenApi),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Title.Should().Be("My API");
        result.Value.Status.Should().Be(DraftStatus.Editing.ToString());
        repository.Received(1).Add(Arg.Any<ContractDraft>());
        await unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateDraft_Should_ReturnFailure_When_TitleIsEmpty()
    {
        var validator = new CreateDraftFeature.Validator();

        var validationResult = await validator.ValidateAsync(
            new CreateDraftFeature.Command("", "author", ContractType.RestApi, ContractProtocol.OpenApi));

        validationResult.IsValid.Should().BeFalse();
        validationResult.Errors.Should().Contain(e => e.PropertyName == "Title");
    }

    // ── ApproveDraft ──────────────────────────────────────────────────────

    [Fact]
    public async Task ApproveDraft_Should_ReturnResponse_When_DraftIsInReview()
    {
        var draft = CreateDraftInReview();
        var draftRepository = Substitute.For<IContractDraftRepository>();
        var reviewRepository = Substitute.For<IContractReviewRepository>();
        var unitOfWork = CreateUnitOfWork();
        var dateTimeProvider = Substitute.For<IDateTimeProvider>();
        var sut = new ApproveDraftFeature.Handler(draftRepository, reviewRepository, unitOfWork, dateTimeProvider);

        draftRepository.GetByIdAsync(Arg.Any<ContractDraftId>(), Arg.Any<CancellationToken>())
            .Returns(draft);
        dateTimeProvider.UtcNow.Returns(FixedNow);

        var result = await sut.Handle(
            new ApproveDraftFeature.Command(draft.Id.Value, "reviewer", "Looks good"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.DraftId.Should().Be(draft.Id.Value);
        reviewRepository.Received(1).Add(Arg.Any<ContractReview>());
        await unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ApproveDraft_Should_ReturnNotFound_When_DraftDoesNotExist()
    {
        var draftRepository = Substitute.For<IContractDraftRepository>();
        var reviewRepository = Substitute.For<IContractReviewRepository>();
        var unitOfWork = CreateUnitOfWork();
        var dateTimeProvider = Substitute.For<IDateTimeProvider>();
        var sut = new ApproveDraftFeature.Handler(draftRepository, reviewRepository, unitOfWork, dateTimeProvider);

        draftRepository.GetByIdAsync(Arg.Any<ContractDraftId>(), Arg.Any<CancellationToken>())
            .Returns((ContractDraft?)null);

        var result = await sut.Handle(
            new ApproveDraftFeature.Command(Guid.NewGuid(), "reviewer"),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Contracts.Draft.NotFound");
        await unitOfWork.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
    }

    // ── ExportDraft ───────────────────────────────────────────────────────

    [Fact]
    public async Task ExportDraft_Should_ReturnContent_When_DraftExists()
    {
        var draft = ContractDraft.Create(
            "Export Test", "author", ContractType.RestApi, ContractProtocol.OpenApi).Value;
        draft.UpdateContent("openapi: 3.0.0", "yaml", "author", FixedNow);

        var repository = Substitute.For<IContractDraftRepository>();
        var sut = new ExportDraftFeature.Handler(repository);

        repository.GetByIdAsync(Arg.Any<ContractDraftId>(), Arg.Any<CancellationToken>())
            .Returns(draft);

        var result = await sut.Handle(
            new ExportDraftFeature.Query(draft.Id.Value),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.DraftId.Should().Be(draft.Id.Value);
        result.Value.Title.Should().Be("Export Test");
        result.Value.SpecContent.Should().Be("openapi: 3.0.0");
        result.Value.Format.Should().Be("yaml");
        result.Value.Protocol.Should().Be(ContractProtocol.OpenApi.ToString());
    }

    [Fact]
    public async Task ExportDraft_Should_ReturnNotFound_When_DraftDoesNotExist()
    {
        var repository = Substitute.For<IContractDraftRepository>();
        var sut = new ExportDraftFeature.Handler(repository);

        repository.GetByIdAsync(Arg.Any<ContractDraftId>(), Arg.Any<CancellationToken>())
            .Returns((ContractDraft?)null);

        var result = await sut.Handle(
            new ExportDraftFeature.Query(Guid.NewGuid()),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Contracts.Draft.NotFound");
    }

    // ── AddDraftExample ───────────────────────────────────────────────────

    [Fact]
    public async Task AddDraftExample_Should_ReturnResponse_When_DraftExists()
    {
        var draft = ContractDraft.Create(
            "Example Draft", "author", ContractType.RestApi, ContractProtocol.OpenApi).Value;
        var repository = Substitute.For<IContractDraftRepository>();
        var unitOfWork = CreateUnitOfWork();
        var dateTimeProvider = Substitute.For<IDateTimeProvider>();
        var sut = new AddDraftExampleFeature.Handler(repository, unitOfWork, dateTimeProvider);

        repository.GetByIdAsync(Arg.Any<ContractDraftId>(), Arg.Any<CancellationToken>())
            .Returns(draft);
        dateTimeProvider.UtcNow.Returns(FixedNow);

        var result = await sut.Handle(
            new AddDraftExampleFeature.Command(
                draft.Id.Value, "GetUsers 200", "{\"users\":[]}", "json", "response", "author"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.DraftId.Should().Be(draft.Id.Value);
        result.Value.ExampleId.Should().NotBeEmpty();
        await unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AddDraftExample_Should_ReturnNotFound_When_DraftDoesNotExist()
    {
        var repository = Substitute.For<IContractDraftRepository>();
        var unitOfWork = CreateUnitOfWork();
        var dateTimeProvider = Substitute.For<IDateTimeProvider>();
        var sut = new AddDraftExampleFeature.Handler(repository, unitOfWork, dateTimeProvider);

        repository.GetByIdAsync(Arg.Any<ContractDraftId>(), Arg.Any<CancellationToken>())
            .Returns((ContractDraft?)null);

        var result = await sut.Handle(
            new AddDraftExampleFeature.Command(
                Guid.NewGuid(), "Example", "{}", "json", "response", "author"),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Contracts.Draft.NotFound");
        await unitOfWork.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
    }

    // ── CreateSoapDraft ───────────────────────────────────────────────────

    [Fact]
    public async Task CreateSoapDraft_Should_ReturnResponse_When_ValidCommand()
    {
        var draftRepository = Substitute.For<IContractDraftRepository>();
        var soapMetadataRepository = Substitute.For<ISoapDraftMetadataRepository>();
        var unitOfWork = CreateUnitOfWork();
        var dateTimeProvider = Substitute.For<IDateTimeProvider>();
        var sut = new CreateSoapDraftFeature.Handler(
            draftRepository, soapMetadataRepository, unitOfWork, dateTimeProvider);

        dateTimeProvider.UtcNow.Returns(FixedNow);

        var result = await sut.Handle(
            new CreateSoapDraftFeature.Command(
                "SOAP Service", "author", "OrderService",
                "http://example.com/orders", "1.2"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Title.Should().Be("SOAP Service");
        result.Value.Status.Should().Be(DraftStatus.Editing.ToString());
        result.Value.ServiceName.Should().Be("OrderService");
        result.Value.TargetNamespace.Should().Be("http://example.com/orders");
        result.Value.SoapVersion.Should().Be("1.2");
        draftRepository.Received(1).Add(Arg.Any<ContractDraft>());
        soapMetadataRepository.Received(1).Add(Arg.Any<SoapDraftMetadata>());
        await unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateSoapDraft_Should_ReturnFailure_When_ServiceNameIsEmpty()
    {
        var validator = new CreateSoapDraftFeature.Validator();

        var validationResult = await validator.ValidateAsync(
            new CreateSoapDraftFeature.Command(
                "SOAP Service", "author", "", "http://example.com/orders"));

        validationResult.IsValid.Should().BeFalse();
        validationResult.Errors.Should().Contain(e => e.PropertyName == "ServiceName");
    }
}
