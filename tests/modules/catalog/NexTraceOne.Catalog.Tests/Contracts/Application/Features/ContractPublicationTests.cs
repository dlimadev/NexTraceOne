using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.Catalog.Application.Contracts.Abstractions;
using NexTraceOne.Catalog.Application.Graph.Abstractions;
using NexTraceOne.Catalog.Domain.Contracts.Entities;
using NexTraceOne.Catalog.Domain.Contracts.Enums;
using NexTraceOne.Configuration.Application.Abstractions;
using NexTraceOne.Configuration.Contracts.DTOs;
using NexTraceOne.Configuration.Domain.Enums;

using DeprecateContractVersionFeature = NexTraceOne.Catalog.Application.Contracts.Features.DeprecateContractVersion.DeprecateContractVersion;
using ExportContractFeature = NexTraceOne.Catalog.Application.Contracts.Features.ExportContract.ExportContract;
using PublishDraftFeature = NexTraceOne.Catalog.Application.Contracts.Features.PublishDraft.PublishDraft;
using PublishToMarketplaceFeature = NexTraceOne.Catalog.Application.Contracts.Features.PublishToMarketplace.PublishToMarketplace;
using RegisterConsumerExpectationFeature = NexTraceOne.Catalog.Application.Contracts.Features.RegisterConsumerExpectation.RegisterConsumerExpectation;
using RegisterContractDeploymentFeature = NexTraceOne.Catalog.Application.Contracts.Features.RegisterContractDeployment.RegisterContractDeployment;

namespace NexTraceOne.Catalog.Tests.Contracts.Application.Features;

/// <summary>
/// Testes de handlers de publicação, subscrição e ciclo de vida do módulo Contracts.
/// </summary>
public sealed class ContractPublicationTests
{
    private static readonly DateTimeOffset FixedNow = new(2025, 06, 01, 10, 0, 0, TimeSpan.Zero);

    private const string BaseSpec = """{"openapi":"3.0.0","info":{"title":"Test","version":"1.0.0"},"paths":{"/users":{"get":{"responses":{"200":{"description":"OK"}}}}}}""";

    private static IContractsUnitOfWork CreateUnitOfWork() => Substitute.For<IContractsUnitOfWork>();

    // ── DeprecateContractVersion ──────────────────────────────────────

    [Fact]
    public async Task DeprecateContractVersion_Should_ReturnResponse_When_VersionExists()
    {
        var version = ContractVersion.Import(Guid.NewGuid(), "1.0.0", BaseSpec, "json", "upload").Value;
        // Transition through lifecycle to a state that allows deprecation
        version.TransitionTo(ContractLifecycleState.InReview, FixedNow);
        version.TransitionTo(ContractLifecycleState.Approved, FixedNow);

        var repository = Substitute.For<IContractVersionRepository>();
        var unitOfWork = CreateUnitOfWork();
        var dateTimeProvider = Substitute.For<IDateTimeProvider>();
        var sut = new DeprecateContractVersionFeature.Handler(repository, unitOfWork, dateTimeProvider);

        repository.GetByIdAsync(Arg.Any<ContractVersionId>(), Arg.Any<CancellationToken>()).Returns(version);
        dateTimeProvider.UtcNow.Returns(FixedNow);

        var result = await sut.Handle(
            new DeprecateContractVersionFeature.Command(version.Id.Value, "End of life", FixedNow.AddMonths(6)),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.LifecycleState.Should().Be("Deprecated");
        result.Value.DeprecationNotice.Should().Be("End of life");
        result.Value.SunsetDate.Should().Be(FixedNow.AddMonths(6));
        await unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeprecateContractVersion_Should_ReturnNotFound_When_VersionDoesNotExist()
    {
        var repository = Substitute.For<IContractVersionRepository>();
        var unitOfWork = CreateUnitOfWork();
        var dateTimeProvider = Substitute.For<IDateTimeProvider>();
        var sut = new DeprecateContractVersionFeature.Handler(repository, unitOfWork, dateTimeProvider);

        repository.GetByIdAsync(Arg.Any<ContractVersionId>(), Arg.Any<CancellationToken>())
            .Returns((ContractVersion?)null);

        var result = await sut.Handle(
            new DeprecateContractVersionFeature.Command(Guid.NewGuid(), "EOL", null),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Contracts.ContractVersion.NotFound");
        await unitOfWork.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
    }

    // ── ExportContract ────────────────────────────────────────────────

    [Fact]
    public async Task ExportContract_Should_ReturnSpecContent_When_VersionExists()
    {
        var version = ContractVersion.Import(Guid.NewGuid(), "2.0.0", BaseSpec, "json", "upload").Value;
        var repository = Substitute.For<IContractVersionRepository>();
        var sut = new ExportContractFeature.Handler(repository);

        repository.GetByIdAsync(Arg.Any<ContractVersionId>(), Arg.Any<CancellationToken>()).Returns(version);

        var result = await sut.Handle(
            new ExportContractFeature.Query(version.Id.Value),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.SemVer.Should().Be("2.0.0");
        result.Value.Format.Should().Be("json");
        result.Value.SpecContent.Should().Be(BaseSpec);
    }

    [Fact]
    public async Task ExportContract_Should_ReturnNotFound_When_VersionDoesNotExist()
    {
        var repository = Substitute.For<IContractVersionRepository>();
        var sut = new ExportContractFeature.Handler(repository);

        repository.GetByIdAsync(Arg.Any<ContractVersionId>(), Arg.Any<CancellationToken>())
            .Returns((ContractVersion?)null);

        var result = await sut.Handle(
            new ExportContractFeature.Query(Guid.NewGuid()),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Contracts.ContractVersion.NotFound");
    }

    // ── PublishDraft ──────────────────────────────────────────────────

    [Fact]
    public async Task PublishDraft_Should_ReturnNotFound_When_DraftDoesNotExist()
    {
        var draftRepository = Substitute.For<IContractDraftRepository>();
        var versionRepository = Substitute.For<IContractVersionRepository>();
        var apiAssetRepository = Substitute.For<IApiAssetRepository>();
        var serviceAssetRepository = Substitute.For<IServiceAssetRepository>();
        var unitOfWork = CreateUnitOfWork();
        var graphUnitOfWork = Substitute.For<ICatalogGraphUnitOfWork>();
        var dateTimeProvider = Substitute.For<IDateTimeProvider>();
        var sut = new PublishDraftFeature.Handler(
            draftRepository, versionRepository, apiAssetRepository,
            serviceAssetRepository, unitOfWork, graphUnitOfWork, dateTimeProvider);

        draftRepository.GetByIdAsync(Arg.Any<ContractDraftId>(), Arg.Any<CancellationToken>())
            .Returns((ContractDraft?)null);

        var result = await sut.Handle(
            new PublishDraftFeature.Command(Guid.NewGuid(), "admin"),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Contracts.Draft.NotFound");
        await unitOfWork.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task PublishDraft_Should_ReturnError_When_DraftIsNotApproved()
    {
        var draft = ContractDraft.Create("Test API", "author", ContractType.RestApi, ContractProtocol.OpenApi, Guid.NewGuid()).Value;
        // Draft starts in Editing status, not Approved
        var draftRepository = Substitute.For<IContractDraftRepository>();
        var versionRepository = Substitute.For<IContractVersionRepository>();
        var apiAssetRepository = Substitute.For<IApiAssetRepository>();
        var serviceAssetRepository = Substitute.For<IServiceAssetRepository>();
        var unitOfWork = CreateUnitOfWork();
        var graphUnitOfWork = Substitute.For<ICatalogGraphUnitOfWork>();
        var dateTimeProvider = Substitute.For<IDateTimeProvider>();
        var sut = new PublishDraftFeature.Handler(
            draftRepository, versionRepository, apiAssetRepository,
            serviceAssetRepository, unitOfWork, graphUnitOfWork, dateTimeProvider);

        draftRepository.GetByIdAsync(Arg.Any<ContractDraftId>(), Arg.Any<CancellationToken>())
            .Returns(draft);

        var result = await sut.Handle(
            new PublishDraftFeature.Command(draft.Id.Value, "admin"),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Contracts.Draft.InvalidTransition");
        await unitOfWork.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
    }

    // ── PublishToMarketplace ──────────────────────────────────────────

    [Fact]
    public async Task PublishToMarketplace_Should_ReturnResponse_When_ValidInput()
    {
        var repository = Substitute.For<IContractListingRepository>();
        var unitOfWork = CreateUnitOfWork();
        var dateTimeProvider = Substitute.For<IDateTimeProvider>();
        var sut = new PublishToMarketplaceFeature.Handler(repository, unitOfWork, dateTimeProvider);

        dateTimeProvider.UtcNow.Returns(FixedNow);

        var result = await sut.Handle(
            new PublishToMarketplaceFeature.Command(
                "contract-123", "Payments", "billing,finance", true,
                "Payment gateway contract", MarketplaceListingStatus.Published, "admin"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.ContractId.Should().Be("contract-123");
        result.Value.Category.Should().Be("Payments");
        result.Value.IsPromoted.Should().BeTrue();
        result.Value.Status.Should().Be(MarketplaceListingStatus.Published);
        await repository.Received(1).AddAsync(Arg.Any<ContractListing>(), Arg.Any<CancellationToken>());
        await unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    // ── RegisterConsumerExpectation ───────────────────────────────────

    [Fact]
    public async Task RegisterConsumerExpectation_Should_CreateNew_When_NoExistingExpectation()
    {
        var repository = Substitute.For<IConsumerExpectationRepository>();
        var unitOfWork = CreateUnitOfWork();
        var dateTimeProvider = Substitute.For<IDateTimeProvider>();
        var sut = new RegisterConsumerExpectationFeature.Handler(repository, unitOfWork, dateTimeProvider);

        repository.GetByApiAssetAndConsumerAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((ConsumerExpectation?)null);
        dateTimeProvider.UtcNow.Returns(FixedNow);

        var apiAssetId = Guid.NewGuid();
        var result = await sut.Handle(
            new RegisterConsumerExpectationFeature.Command(
                apiAssetId, "order-service", "Orders", """{"paths":["/users"]}""", "Critical dependency"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.IsNew.Should().BeTrue();
        result.Value.ConsumerServiceName.Should().Be("order-service");
        result.Value.ConsumerDomain.Should().Be("Orders");
        repository.Received(1).Add(Arg.Any<ConsumerExpectation>());
        await unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RegisterConsumerExpectation_Should_Update_When_ExistingExpectationFound()
    {
        var apiAssetId = Guid.NewGuid();
        var existing = ConsumerExpectation.Create(
            apiAssetId, "order-service", "Orders", """{"paths":["/old"]}""", null, FixedNow.AddDays(-10));
        var repository = Substitute.For<IConsumerExpectationRepository>();
        var unitOfWork = CreateUnitOfWork();
        var dateTimeProvider = Substitute.For<IDateTimeProvider>();
        var sut = new RegisterConsumerExpectationFeature.Handler(repository, unitOfWork, dateTimeProvider);

        repository.GetByApiAssetAndConsumerAsync(apiAssetId, "order-service", Arg.Any<CancellationToken>())
            .Returns(existing);
        dateTimeProvider.UtcNow.Returns(FixedNow);

        var result = await sut.Handle(
            new RegisterConsumerExpectationFeature.Command(
                apiAssetId, "order-service", "Orders", """{"paths":["/users","/orders"]}""", "Updated"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.IsNew.Should().BeFalse();
        repository.DidNotReceive().Add(Arg.Any<ConsumerExpectation>());
        await unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    // ── RegisterContractDeployment ───────────────────────────────────

    [Fact]
    public async Task RegisterContractDeployment_Should_ReturnResponse_When_VersionExists()
    {
        var version = ContractVersion.Import(Guid.NewGuid(), "1.0.0", BaseSpec, "json", "upload").Value;
        var contractVersionRepo = Substitute.For<IContractVersionRepository>();
        var deploymentRepo = Substitute.For<IContractDeploymentRepository>();
        var unitOfWork = CreateUnitOfWork();
        var clock = Substitute.For<IDateTimeProvider>();
        var sut = new RegisterContractDeploymentFeature.Handler(contractVersionRepo, deploymentRepo, unitOfWork, clock);

        contractVersionRepo.GetByIdAsync(Arg.Any<ContractVersionId>(), Arg.Any<CancellationToken>()).Returns(version);
        clock.UtcNow.Returns(FixedNow);

        var result = await sut.Handle(
            new RegisterContractDeploymentFeature.Command(
                version.Id.Value, "production", "ci-bot", "github-actions",
                ContractDeploymentStatus.Success, FixedNow, "Initial deploy"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Environment.Should().Be("production");
        result.Value.DeployedBy.Should().Be("ci-bot");
        result.Value.SourceSystem.Should().Be("github-actions");
        result.Value.Status.Should().Be("Success");
        deploymentRepo.Received(1).Add(Arg.Any<ContractDeployment>());
        await unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RegisterContractDeployment_Should_ReturnNotFound_When_VersionDoesNotExist()
    {
        var contractVersionRepo = Substitute.For<IContractVersionRepository>();
        var deploymentRepo = Substitute.For<IContractDeploymentRepository>();
        var unitOfWork = CreateUnitOfWork();
        var clock = Substitute.For<IDateTimeProvider>();
        var sut = new RegisterContractDeploymentFeature.Handler(contractVersionRepo, deploymentRepo, unitOfWork, clock);

        contractVersionRepo.GetByIdAsync(Arg.Any<ContractVersionId>(), Arg.Any<CancellationToken>())
            .Returns((ContractVersion?)null);

        var result = await sut.Handle(
            new RegisterContractDeploymentFeature.Command(
                Guid.NewGuid(), "staging", "admin", "manual",
                ContractDeploymentStatus.Pending, null, null),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Contracts.ContractVersion.NotFound");
        deploymentRepo.DidNotReceive().Add(Arg.Any<ContractDeployment>());
        await unitOfWork.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
    }
}
