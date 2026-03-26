using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.Catalog.Application.Portal.Abstractions;
using NexTraceOne.Catalog.Domain.Portal.Entities;
using NexTraceOne.Catalog.Domain.Portal.Enums;

using GetContractPublicationStatusFeature = NexTraceOne.Catalog.Application.Portal.Features.GetContractPublicationStatus.GetContractPublicationStatus;
using GetPublicationCenterEntriesFeature = NexTraceOne.Catalog.Application.Portal.Features.GetPublicationCenterEntries.GetPublicationCenterEntries;
using PublishContractToPortalFeature = NexTraceOne.Catalog.Application.Portal.Features.PublishContractToPortal.PublishContractToPortal;
using WithdrawContractFromPortalFeature = NexTraceOne.Catalog.Application.Portal.Features.WithdrawContractFromPortal.WithdrawContractFromPortal;

namespace NexTraceOne.Catalog.Tests.Portal.Application.Features;

/// <summary>
/// Testes dos handlers do Publication Center.
/// Valida publicação, retirada, listagem e consulta de estado de publicação de contratos no Developer Portal.
/// </summary>
public sealed class PublicationCenterApplicationTests
{
    private static readonly DateTimeOffset Now = new(2025, 07, 20, 09, 0, 0, TimeSpan.Zero);
    private static readonly Guid VersionId = Guid.NewGuid();
    private static readonly Guid AssetId = Guid.NewGuid();

    // ── PublishContractToPortal ──────────────────────────────────────────────

    [Fact]
    public async Task PublishContractToPortal_Should_ReturnPublished_When_NoDuplicateExists()
    {
        var repository = Substitute.For<IContractPublicationEntryRepository>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var clock = Substitute.For<IDateTimeProvider>();
        clock.UtcNow.Returns(Now);

        ContractPublicationEntry? none = null;
        repository.GetByContractVersionIdAsync(VersionId, Arg.Any<CancellationToken>()).Returns(none);

        var sut = new PublishContractToPortalFeature.Handler(repository, unitOfWork, clock);

        var command = new PublishContractToPortalFeature.Command(
            VersionId, AssetId, "Payments API", "2.0.0", "jsmith", "Approved",
            PublicationVisibility.Internal, "Breaking: removed /v1");

        var result = await sut.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.ContractVersionId.Should().Be(VersionId);
        result.Value.Status.Should().Be("Published");
        result.Value.Visibility.Should().Be("Internal");
        result.Value.PublishedAt.Should().Be(Now);
        repository.Received(1).Add(Arg.Any<ContractPublicationEntry>());
        await unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task PublishContractToPortal_Should_ReturnFailure_When_DuplicateExists()
    {
        var repository = Substitute.For<IContractPublicationEntryRepository>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var clock = Substitute.For<IDateTimeProvider>();
        clock.UtcNow.Returns(Now);

        var existing = BuildPublishedEntry();
        repository.GetByContractVersionIdAsync(VersionId, Arg.Any<CancellationToken>()).Returns(existing);

        var sut = new PublishContractToPortalFeature.Handler(repository, unitOfWork, clock);

        var command = new PublishContractToPortalFeature.Command(
            VersionId, AssetId, "Payments API", "2.0.0", "jsmith", "Approved");

        var result = await sut.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("AlreadyExists");
    }

    [Fact]
    public async Task PublishContractToPortal_Validator_Should_Fail_When_LifecycleStateNotPublishable()
    {
        var validator = new PublishContractToPortalFeature.Validator();

        var command = new PublishContractToPortalFeature.Command(
            VersionId, AssetId, "Payments API", "2.0.0", "jsmith", "Draft");

        var result = await validator.ValidateAsync(command, CancellationToken.None);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage.Contains("Approved or Locked"));
    }

    [Fact]
    public async Task PublishContractToPortal_Validator_Should_Pass_When_LifecycleIsLocked()
    {
        var validator = new PublishContractToPortalFeature.Validator();

        var command = new PublishContractToPortalFeature.Command(
            VersionId, AssetId, "Payments API", "2.0.0", "jsmith", "Locked");

        var result = await validator.ValidateAsync(command, CancellationToken.None);

        result.IsValid.Should().BeTrue();
    }

    // ── WithdrawContractFromPortal ───────────────────────────────────────────

    [Fact]
    public async Task WithdrawContractFromPortal_Should_ReturnWithdrawn_When_EntryIsPublished()
    {
        var entryId = Guid.NewGuid();
        var repository = Substitute.For<IContractPublicationEntryRepository>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var clock = Substitute.For<IDateTimeProvider>();
        clock.UtcNow.Returns(Now);

        var entry = BuildPublishedEntry();
        repository.GetByIdAsync(ContractPublicationEntryId.From(entryId), Arg.Any<CancellationToken>())
            .Returns(entry);

        var sut = new WithdrawContractFromPortalFeature.Handler(repository, unitOfWork, clock);

        var command = new WithdrawContractFromPortalFeature.Command(entryId, "admin", "Superseded by v3");

        var result = await sut.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Status.Should().Be("Withdrawn");
        result.Value.WithdrawnBy.Should().Be("admin");
        result.Value.WithdrawalReason.Should().Be("Superseded by v3");
        await unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task WithdrawContractFromPortal_Should_ReturnFailure_When_EntryNotFound()
    {
        var repository = Substitute.For<IContractPublicationEntryRepository>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var clock = Substitute.For<IDateTimeProvider>();

        ContractPublicationEntry? none = null;
        repository.GetByIdAsync(Arg.Any<ContractPublicationEntryId>(), Arg.Any<CancellationToken>())
            .Returns(none);

        var sut = new WithdrawContractFromPortalFeature.Handler(repository, unitOfWork, clock);

        var command = new WithdrawContractFromPortalFeature.Command(Guid.NewGuid(), "admin");

        var result = await sut.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("NotFound");
    }

    // ── GetPublicationCenterEntries ──────────────────────────────────────────

    [Fact]
    public async Task GetPublicationCenterEntries_Should_ReturnEmptyList_When_NoneExist()
    {
        var repository = Substitute.For<IContractPublicationEntryRepository>();
        repository.ListAsync(Arg.Any<ContractPublicationStatus?>(), Arg.Any<Guid?>(),
            Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<ContractPublicationEntry>)Array.Empty<ContractPublicationEntry>());

        var sut = new GetPublicationCenterEntriesFeature.Handler(repository);

        var result = await sut.Handle(new GetPublicationCenterEntriesFeature.Query(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().BeEmpty();
        result.Value.TotalCount.Should().Be(0);
    }

    [Fact]
    public async Task GetPublicationCenterEntries_Should_ReturnItem_When_OneEntryExists()
    {
        var repository = Substitute.For<IContractPublicationEntryRepository>();
        var entry = BuildPublishedEntry();
        repository.ListAsync(Arg.Any<ContractPublicationStatus?>(), Arg.Any<Guid?>(),
            Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<ContractPublicationEntry>)new[] { entry });

        var sut = new GetPublicationCenterEntriesFeature.Handler(repository);

        var result = await sut.Handle(new GetPublicationCenterEntriesFeature.Query(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(1);
        result.Value.Items[0].ContractTitle.Should().Be("Payments API");
        result.Value.Items[0].Status.Should().Be("Published");
    }

    // ── GetContractPublicationStatus ─────────────────────────────────────────

    [Fact]
    public async Task GetContractPublicationStatus_Should_ReturnNotPublished_When_NoEntry()
    {
        var repository = Substitute.For<IContractPublicationEntryRepository>();
        ContractPublicationEntry? none = null;
        repository.GetByContractVersionIdAsync(VersionId, Arg.Any<CancellationToken>()).Returns(none);

        var sut = new GetContractPublicationStatusFeature.Handler(repository);

        var result = await sut.Handle(
            new GetContractPublicationStatusFeature.Query(VersionId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.IsPublished.Should().BeFalse();
        result.Value.Status.Should().Be("NotPublished");
        result.Value.PublicationEntryId.Should().BeNull();
    }

    [Fact]
    public async Task GetContractPublicationStatus_Should_ReturnPublished_When_EntryExists()
    {
        var repository = Substitute.For<IContractPublicationEntryRepository>();
        var entry = BuildPublishedEntry();
        repository.GetByContractVersionIdAsync(VersionId, Arg.Any<CancellationToken>()).Returns(entry);

        var sut = new GetContractPublicationStatusFeature.Handler(repository);

        var result = await sut.Handle(
            new GetContractPublicationStatusFeature.Query(VersionId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.IsPublished.Should().BeTrue();
        result.Value.Status.Should().Be("Published");
        result.Value.Visibility.Should().Be("Internal");
    }

    // ── Helper ───────────────────────────────────────────────────────────────

    private static ContractPublicationEntry BuildPublishedEntry()
    {
        var entry = ContractPublicationEntry.Create(
            VersionId, AssetId, "Payments API", "2.0.0", "jsmith",
            PublicationVisibility.Internal).Value;
        entry.Publish(Now);
        return entry;
    }
}
