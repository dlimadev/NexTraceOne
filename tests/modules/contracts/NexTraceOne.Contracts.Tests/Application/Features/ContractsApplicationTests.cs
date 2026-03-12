using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Domain.Enums;
using NexTraceOne.Contracts.Application.Abstractions;
using NexTraceOne.Contracts.Domain.Entities;
using ComputeSemanticDiffFeature = NexTraceOne.Contracts.Application.Features.ComputeSemanticDiff.ComputeSemanticDiff;
using CreateContractVersionFeature = NexTraceOne.Contracts.Application.Features.CreateContractVersion.CreateContractVersion;
using GetContractHistoryFeature = NexTraceOne.Contracts.Application.Features.GetContractHistory.GetContractHistory;
using ImportContractFeature = NexTraceOne.Contracts.Application.Features.ImportContract.ImportContract;
using LockContractVersionFeature = NexTraceOne.Contracts.Application.Features.LockContractVersion.LockContractVersion;

namespace NexTraceOne.Contracts.Tests.Application.Features;

/// <summary>
/// Testes de handlers da camada Application do módulo Contracts.
/// </summary>
public sealed class ContractsApplicationTests
{
    private static readonly DateTimeOffset FixedNow = new(2025, 06, 01, 10, 0, 0, TimeSpan.Zero);

    private const string BaseSpec = """{"openapi":"3.0.0","info":{"title":"Test","version":"1.0.0"},"paths":{"/users":{"get":{"responses":{"200":{"description":"OK"}}}}}}""";
    private const string AdditiveSpec = """{"openapi":"3.0.0","info":{"title":"Test","version":"1.1.0"},"paths":{"/users":{"get":{"responses":{"200":{"description":"OK"}}}},"/orders":{"get":{"responses":{"200":{"description":"OK"}}}}}}""";
    private const string BreakingSpec = """{"openapi":"3.0.0","info":{"title":"Test","version":"2.0.0"},"paths":{}}""";

    // ── ImportContract ────────────────────────────────────────────────────

    [Fact]
    public async Task ImportContract_Should_ReturnResponse_When_ApiAssetDoesNotHaveThisSemVer()
    {
        var repository = Substitute.For<IContractVersionRepository>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var dateTimeProvider = Substitute.For<IDateTimeProvider>();
        var sut = new ImportContractFeature.Handler(repository, unitOfWork, dateTimeProvider);

        repository.GetByApiAssetAndSemVerAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((ContractVersion?)null);
        dateTimeProvider.UtcNow.Returns(FixedNow);

        var result = await sut.Handle(
            new ImportContractFeature.Command(Guid.NewGuid(), "1.0.0", BaseSpec, "json", "upload"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.SemVer.Should().Be("1.0.0");
        result.Value.Format.Should().Be("json");
        repository.Received(1).Add(Arg.Any<ContractVersion>());
        await unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ImportContract_Should_ReturnConflict_When_SemVerAlreadyExistsForApiAsset()
    {
        var existing = ContractVersion.Import(Guid.NewGuid(), "1.0.0", BaseSpec, "json", "upload").Value;
        var repository = Substitute.For<IContractVersionRepository>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var dateTimeProvider = Substitute.For<IDateTimeProvider>();
        var sut = new ImportContractFeature.Handler(repository, unitOfWork, dateTimeProvider);

        repository.GetByApiAssetAndSemVerAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(existing);

        var result = await sut.Handle(
            new ImportContractFeature.Command(Guid.NewGuid(), "1.0.0", BaseSpec, "json", "upload"),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Contracts.ContractVersion.AlreadyExists");
        repository.DidNotReceive().Add(Arg.Any<ContractVersion>());
    }

    // ── CreateContractVersion ─────────────────────────────────────────────

    [Fact]
    public async Task CreateContractVersion_Should_ReturnError_When_NoPreviousVersionExists()
    {
        var repository = Substitute.For<IContractVersionRepository>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var dateTimeProvider = Substitute.For<IDateTimeProvider>();
        var sut = new CreateContractVersionFeature.Handler(repository, unitOfWork, dateTimeProvider);

        repository.GetLatestByApiAssetAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((ContractVersion?)null);

        var result = await sut.Handle(
            new CreateContractVersionFeature.Command(Guid.NewGuid(), "1.1.0", AdditiveSpec, "json", "upload"),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Contracts.ContractVersion.NoPreviousVersion");
        repository.DidNotReceive().Add(Arg.Any<ContractVersion>());
    }

    [Fact]
    public async Task CreateContractVersion_Should_ReturnResponse_When_PreviousVersionExistsAndSemVerIsNew()
    {
        var apiAssetId = Guid.NewGuid();
        var previous = ContractVersion.Import(apiAssetId, "1.0.0", BaseSpec, "json", "upload").Value;
        var repository = Substitute.For<IContractVersionRepository>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var dateTimeProvider = Substitute.For<IDateTimeProvider>();
        var sut = new CreateContractVersionFeature.Handler(repository, unitOfWork, dateTimeProvider);

        repository.GetLatestByApiAssetAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(previous);
        repository.GetByApiAssetAndSemVerAsync(Arg.Any<Guid>(), "1.1.0", Arg.Any<CancellationToken>())
            .Returns((ContractVersion?)null);
        dateTimeProvider.UtcNow.Returns(FixedNow);

        var result = await sut.Handle(
            new CreateContractVersionFeature.Command(apiAssetId, "1.1.0", AdditiveSpec, "json", "upload"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.SemVer.Should().Be("1.1.0");
        repository.Received(1).Add(Arg.Any<ContractVersion>());
        await unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    // ── GetContractHistory ────────────────────────────────────────────────

    [Fact]
    public async Task GetContractHistory_Should_ReturnOrderedVersionSummaries()
    {
        var apiAssetId = Guid.NewGuid();
        var v1 = ContractVersion.Import(apiAssetId, "1.0.0", BaseSpec, "json", "upload").Value;
        var v2 = ContractVersion.Import(apiAssetId, "1.1.0", AdditiveSpec, "json", "upload").Value;
        var repository = Substitute.For<IContractVersionRepository>();
        var sut = new GetContractHistoryFeature.Handler(repository);

        repository.ListByApiAssetAsync(apiAssetId, Arg.Any<CancellationToken>())
            .Returns(new List<ContractVersion> { v1, v2 });

        var result = await sut.Handle(new GetContractHistoryFeature.Query(apiAssetId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.ApiAssetId.Should().Be(apiAssetId);
        result.Value.Versions.Should().HaveCount(2);
        result.Value.Versions.Should().Contain(v => v.SemVer == "1.0.0");
        result.Value.Versions.Should().Contain(v => v.SemVer == "1.1.0");
    }

    // ── LockContractVersion ───────────────────────────────────────────────

    [Fact]
    public async Task LockContractVersion_Should_LockSuccessfully_When_VersionExistsAndNotLocked()
    {
        var version = ContractVersion.Import(Guid.NewGuid(), "1.0.0", BaseSpec, "json", "upload").Value;
        var repository = Substitute.For<IContractVersionRepository>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var dateTimeProvider = Substitute.For<IDateTimeProvider>();
        var sut = new LockContractVersionFeature.Handler(repository, unitOfWork, dateTimeProvider);

        repository.GetByIdAsync(Arg.Any<ContractVersionId>(), Arg.Any<CancellationToken>()).Returns(version);
        dateTimeProvider.UtcNow.Returns(FixedNow);

        var result = await sut.Handle(
            new LockContractVersionFeature.Command(version.Id.Value, "admin"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.LockedBy.Should().Be("admin");
        await unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task LockContractVersion_Should_ReturnNotFound_When_VersionDoesNotExist()
    {
        var repository = Substitute.For<IContractVersionRepository>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var dateTimeProvider = Substitute.For<IDateTimeProvider>();
        var sut = new LockContractVersionFeature.Handler(repository, unitOfWork, dateTimeProvider);

        repository.GetByIdAsync(Arg.Any<ContractVersionId>(), Arg.Any<CancellationToken>())
            .Returns((ContractVersion?)null);

        var result = await sut.Handle(
            new LockContractVersionFeature.Command(Guid.NewGuid(), "admin"),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Contracts.ContractVersion.NotFound");
        await unitOfWork.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
    }

    // ── ComputeSemanticDiff ───────────────────────────────────────────────

    [Fact]
    public async Task ComputeSemanticDiff_Should_ReturnAdditive_When_NewPathIsAdded()
    {
        var apiAssetId = Guid.NewGuid();
        var baseVersion = ContractVersion.Import(apiAssetId, "1.0.0", BaseSpec, "json", "upload").Value;
        var targetVersion = ContractVersion.Import(apiAssetId, "1.1.0", AdditiveSpec, "json", "upload").Value;
        var repository = Substitute.For<IContractVersionRepository>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var dateTimeProvider = Substitute.For<IDateTimeProvider>();
        var sut = new ComputeSemanticDiffFeature.Handler(repository, unitOfWork, dateTimeProvider);

        repository.GetByIdAsync(Arg.Is<ContractVersionId>(id => id.Value == baseVersion.Id.Value), Arg.Any<CancellationToken>())
            .Returns(baseVersion);
        repository.GetByIdAsync(Arg.Is<ContractVersionId>(id => id.Value == targetVersion.Id.Value), Arg.Any<CancellationToken>())
            .Returns(targetVersion);
        dateTimeProvider.UtcNow.Returns(FixedNow);

        var result = await sut.Handle(
            new ComputeSemanticDiffFeature.Query(baseVersion.Id.Value, targetVersion.Id.Value),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.ChangeLevel.Should().Be(ChangeLevel.Additive);
        result.Value.AdditiveChanges.Should().NotBeEmpty();
        result.Value.BreakingChanges.Should().BeEmpty();
    }

    [Fact]
    public async Task ComputeSemanticDiff_Should_ReturnBreaking_When_PathIsRemoved()
    {
        var apiAssetId = Guid.NewGuid();
        var baseVersion = ContractVersion.Import(apiAssetId, "1.0.0", BaseSpec, "json", "upload").Value;
        var targetVersion = ContractVersion.Import(apiAssetId, "2.0.0", BreakingSpec, "json", "upload").Value;
        var repository = Substitute.For<IContractVersionRepository>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var dateTimeProvider = Substitute.For<IDateTimeProvider>();
        var sut = new ComputeSemanticDiffFeature.Handler(repository, unitOfWork, dateTimeProvider);

        repository.GetByIdAsync(Arg.Is<ContractVersionId>(id => id.Value == baseVersion.Id.Value), Arg.Any<CancellationToken>())
            .Returns(baseVersion);
        repository.GetByIdAsync(Arg.Is<ContractVersionId>(id => id.Value == targetVersion.Id.Value), Arg.Any<CancellationToken>())
            .Returns(targetVersion);
        dateTimeProvider.UtcNow.Returns(FixedNow);

        var result = await sut.Handle(
            new ComputeSemanticDiffFeature.Query(baseVersion.Id.Value, targetVersion.Id.Value),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.ChangeLevel.Should().Be(ChangeLevel.Breaking);
        result.Value.BreakingChanges.Should().NotBeEmpty();
    }
}
