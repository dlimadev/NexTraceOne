using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.Catalog.Application.Contracts.Abstractions;
using NexTraceOne.Catalog.Application.Graph.Abstractions;
using NexTraceOne.Catalog.Domain.Contracts.Entities;
using NexTraceOne.Catalog.Domain.Contracts.Enums;
using NexTraceOne.Catalog.Domain.Contracts.Services;
using NexTraceOne.Catalog.Domain.Contracts.ValueObjects;

using TransitionLifecycleStateFeature = NexTraceOne.Catalog.Application.Contracts.Features.TransitionLifecycleState.TransitionLifecycleState;
using SignContractVersionFeature = NexTraceOne.Catalog.Application.Contracts.Features.SignContractVersion.SignContractVersion;
using DeprecateContractVersionFeature = NexTraceOne.Catalog.Application.Contracts.Features.DeprecateContractVersion.DeprecateContractVersion;
using VerifySignatureFeature = NexTraceOne.Catalog.Application.Contracts.Features.VerifySignature.VerifySignature;
using GetContractVersionDetailFeature = NexTraceOne.Catalog.Application.Contracts.Features.GetContractVersionDetail.GetContractVersionDetail;

namespace NexTraceOne.Catalog.Tests.Contracts.Application.Features;

/// <summary>
/// Testes dos handlers das novas features: lifecycle, assinatura, deprecação, verificação e detalhe.
/// </summary>
public sealed class ContractsNewFeaturesTests
{
    private static readonly DateTimeOffset FixedNow = new(2025, 06, 01, 10, 0, 0, TimeSpan.Zero);
    private const string SampleSpec = """{"openapi":"3.1.0","info":{"title":"Test","version":"1.0.0"},"paths":{"/users":{"get":{"responses":{"200":{"description":"OK"}}}}}}""";

    private static ContractVersion CreateContractInState(ContractLifecycleState targetState)
    {
        var result = ContractVersion.Import(Guid.NewGuid(), "1.0.0", SampleSpec, "json", "upload");
        var contract = result.Value;

        if (targetState >= ContractLifecycleState.InReview)
            contract.TransitionTo(ContractLifecycleState.InReview, FixedNow);
        if (targetState >= ContractLifecycleState.Approved)
            contract.TransitionTo(ContractLifecycleState.Approved, FixedNow);
        if (targetState >= ContractLifecycleState.Locked)
            contract.TransitionTo(ContractLifecycleState.Locked, FixedNow);

        return contract;
    }

    // ── TransitionLifecycleState ────────────────────────────────

    [Fact]
    public async Task TransitionLifecycle_Should_Succeed_WhenValidTransition()
    {
        var contract = CreateContractInState(ContractLifecycleState.Draft);
        var repository = Substitute.For<IContractVersionRepository>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var dateTimeProvider = Substitute.For<IDateTimeProvider>();
        dateTimeProvider.UtcNow.Returns(FixedNow);

        repository.GetByIdAsync(Arg.Any<ContractVersionId>(), Arg.Any<CancellationToken>())
            .Returns(contract);

        var handler = new TransitionLifecycleStateFeature.Handler(repository, unitOfWork, dateTimeProvider);
        var result = await handler.Handle(
            new TransitionLifecycleStateFeature.Command(contract.Id.Value, ContractLifecycleState.InReview),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.PreviousState.Should().Be(ContractLifecycleState.Draft);
        result.Value.CurrentState.Should().Be(ContractLifecycleState.InReview);
    }

    [Fact]
    public async Task TransitionLifecycle_Should_Fail_WhenInvalidTransition()
    {
        var contract = CreateContractInState(ContractLifecycleState.Draft);
        var repository = Substitute.For<IContractVersionRepository>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var dateTimeProvider = Substitute.For<IDateTimeProvider>();
        dateTimeProvider.UtcNow.Returns(FixedNow);

        repository.GetByIdAsync(Arg.Any<ContractVersionId>(), Arg.Any<CancellationToken>())
            .Returns(contract);

        var handler = new TransitionLifecycleStateFeature.Handler(repository, unitOfWork, dateTimeProvider);
        var result = await handler.Handle(
            new TransitionLifecycleStateFeature.Command(contract.Id.Value, ContractLifecycleState.Locked),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Contracts.Lifecycle.InvalidTransition");
    }

    [Fact]
    public async Task TransitionLifecycle_Should_ReturnNotFound_WhenContractMissing()
    {
        var repository = Substitute.For<IContractVersionRepository>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var dateTimeProvider = Substitute.For<IDateTimeProvider>();
        dateTimeProvider.UtcNow.Returns(FixedNow);

        repository.GetByIdAsync(Arg.Any<ContractVersionId>(), Arg.Any<CancellationToken>())
            .Returns((ContractVersion?)null);

        var handler = new TransitionLifecycleStateFeature.Handler(repository, unitOfWork, dateTimeProvider);
        var result = await handler.Handle(
            new TransitionLifecycleStateFeature.Command(Guid.NewGuid(), ContractLifecycleState.InReview),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Contracts.ContractVersion.NotFound");
    }

    // ── SignContractVersion ─────────────────────────────────────

    [Fact]
    public async Task SignContract_Should_Succeed_WhenContractIsApproved()
    {
        var contract = CreateContractInState(ContractLifecycleState.Approved);
        var repository = Substitute.For<IContractVersionRepository>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var dateTimeProvider = Substitute.For<IDateTimeProvider>();
        var currentUser = Substitute.For<ICurrentUser>();
        dateTimeProvider.UtcNow.Returns(FixedNow);
        currentUser.Id.Returns("admin@test.com");

        repository.GetByIdAsync(Arg.Any<ContractVersionId>(), Arg.Any<CancellationToken>())
            .Returns(contract);

        var handler = new SignContractVersionFeature.Handler(repository, unitOfWork, dateTimeProvider, currentUser);
        var result = await handler.Handle(
            new SignContractVersionFeature.Command(contract.Id.Value),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Fingerprint.Should().NotBeNullOrWhiteSpace();
        result.Value.Algorithm.Should().Be("SHA-256");
        result.Value.SignedBy.Should().Be("admin@test.com");
    }

    [Fact]
    public async Task SignContract_Should_Fail_WhenContractIsDraft()
    {
        var contract = CreateContractInState(ContractLifecycleState.Draft);
        var repository = Substitute.For<IContractVersionRepository>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var dateTimeProvider = Substitute.For<IDateTimeProvider>();
        var currentUser = Substitute.For<ICurrentUser>();
        dateTimeProvider.UtcNow.Returns(FixedNow);
        currentUser.Id.Returns("admin");

        repository.GetByIdAsync(Arg.Any<ContractVersionId>(), Arg.Any<CancellationToken>())
            .Returns(contract);

        var handler = new SignContractVersionFeature.Handler(repository, unitOfWork, dateTimeProvider, currentUser);
        var result = await handler.Handle(
            new SignContractVersionFeature.Command(contract.Id.Value),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Contracts.Signing.InvalidState");
    }

    // ── DeprecateContractVersion ────────────────────────────────

    [Fact]
    public async Task DeprecateContract_Should_Succeed_WhenContractIsLocked()
    {
        var contract = CreateContractInState(ContractLifecycleState.Locked);
        var repository = Substitute.For<IContractVersionRepository>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var dateTimeProvider = Substitute.For<IDateTimeProvider>();
        dateTimeProvider.UtcNow.Returns(FixedNow);

        repository.GetByIdAsync(Arg.Any<ContractVersionId>(), Arg.Any<CancellationToken>())
            .Returns(contract);

        var sunset = FixedNow.AddMonths(6);
        var handler = new DeprecateContractVersionFeature.Handler(repository, unitOfWork, dateTimeProvider);
        var result = await handler.Handle(
            new DeprecateContractVersionFeature.Command(contract.Id.Value, "Moving to v2", sunset),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.LifecycleState.Should().Be("Deprecated");
        result.Value.DeprecationNotice.Should().Be("Moving to v2");
        result.Value.SunsetDate.Should().Be(sunset);
    }

    // ── VerifySignature ─────────────────────────────────────────

    [Fact]
    public async Task VerifySignature_Should_ReturnValid_WhenSignatureMatches()
    {
        var contract = CreateContractInState(ContractLifecycleState.Approved);
        // Sign the contract first
        var canonical = ContractCanonicalizer.Canonicalize(
            contract.SpecContent, contract.Format);
        var signature = ContractSignature.Create(
            canonical, "admin", FixedNow);
        contract.Sign(signature);

        var repository = Substitute.For<IContractVersionRepository>();
        repository.GetByIdAsync(Arg.Any<ContractVersionId>(), Arg.Any<CancellationToken>())
            .Returns(contract);

        var handler = new VerifySignatureFeature.Handler(repository);
        var result = await handler.Handle(
            new VerifySignatureFeature.Query(contract.Id.Value),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.HasSignature.Should().BeTrue();
        result.Value.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task VerifySignature_Should_ReturnNoSignature_WhenNotSigned()
    {
        var contract = CreateContractInState(ContractLifecycleState.Draft);
        var repository = Substitute.For<IContractVersionRepository>();
        repository.GetByIdAsync(Arg.Any<ContractVersionId>(), Arg.Any<CancellationToken>())
            .Returns(contract);

        var handler = new VerifySignatureFeature.Handler(repository);
        var result = await handler.Handle(
            new VerifySignatureFeature.Query(contract.Id.Value),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.HasSignature.Should().BeFalse();
        result.Value.IsValid.Should().BeFalse();
    }

    // ── GetContractVersionDetail ────────────────────────────────

    [Fact]
    public async Task GetDetail_Should_ReturnFullDetails()
    {
        var contract = CreateContractInState(ContractLifecycleState.Approved);
        var repository = Substitute.For<IContractVersionRepository>();
        var apiAssetRepository = Substitute.For<IApiAssetRepository>();
        repository.GetByIdAsync(Arg.Any<ContractVersionId>(), Arg.Any<CancellationToken>())
            .Returns(contract);
        apiAssetRepository.GetByIdAsync(Arg.Any<Catalog.Domain.Graph.Entities.ApiAssetId>(), Arg.Any<CancellationToken>())
            .Returns((Catalog.Domain.Graph.Entities.ApiAsset?)null);

        var handler = new GetContractVersionDetailFeature.Handler(repository, apiAssetRepository);
        var result = await handler.Handle(
            new GetContractVersionDetailFeature.Query(contract.Id.Value),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.SemVer.Should().Be("1.0.0");
        result.Value.Protocol.Should().Be(ContractProtocol.OpenApi);
        result.Value.LifecycleState.Should().Be(ContractLifecycleState.Approved);
        result.Value.Format.Should().Be("json");
    }

    [Fact]
    public async Task GetDetail_Should_ReturnNotFound_WhenMissing()
    {
        var repository = Substitute.For<IContractVersionRepository>();
        var apiAssetRepository = Substitute.For<IApiAssetRepository>();
        repository.GetByIdAsync(Arg.Any<ContractVersionId>(), Arg.Any<CancellationToken>())
            .Returns((ContractVersion?)null);

        var handler = new GetContractVersionDetailFeature.Handler(repository, apiAssetRepository);
        var result = await handler.Handle(
            new GetContractVersionDetailFeature.Query(Guid.NewGuid()),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Contracts.ContractVersion.NotFound");
    }
}
