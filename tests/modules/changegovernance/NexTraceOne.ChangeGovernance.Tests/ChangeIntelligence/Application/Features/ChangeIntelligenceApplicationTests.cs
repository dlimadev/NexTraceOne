using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Core.Enums;
using NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Abstractions;
using NexTraceOne.ChangeGovernance.Domain.ChangeIntelligence.Entities;

using CalculateBlastRadiusFeature = NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Features.CalculateBlastRadius.CalculateBlastRadius;
using ClassifyChangeLevelFeature = NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Features.ClassifyChangeLevel.ClassifyChangeLevel;
using ComputeChangeScoreFeature = NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Features.ComputeChangeScore.ComputeChangeScore;
using GetReleaseFeature = NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Features.GetRelease.GetRelease;
using NotifyDeploymentFeature = NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Features.NotifyDeployment.NotifyDeployment;

namespace NexTraceOne.ChangeGovernance.Tests.ChangeIntelligence.Application.Features;

/// <summary>Testes de handlers da camada Application do módulo ChangeIntelligence.</summary>
public sealed class ChangeIntelligenceApplicationTests
{
    private static readonly DateTimeOffset FixedNow = new(2025, 6, 1, 10, 0, 0, TimeSpan.Zero);

    private static Release CreateRelease() =>
        Release.Create(Guid.NewGuid(), "TestService", "1.0.0", "staging", "https://ci/pipeline/1", "abc123def456", FixedNow);

    // ── NotifyDeployment ──────────────────────────────────────────────────

    [Fact]
    public async Task NotifyDeployment_Should_CreateRelease_AndReturnResponse()
    {
        var repository = Substitute.For<IReleaseRepository>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var dateTimeProvider = Substitute.For<IDateTimeProvider>();
        var sut = new NotifyDeploymentFeature.Handler(repository, unitOfWork, dateTimeProvider);

        repository.GetByApiAssetAndVersionAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((Release?)null);
        dateTimeProvider.UtcNow.Returns(FixedNow);

        var command = new NotifyDeploymentFeature.Command(
            Guid.NewGuid(), "MyService", "2.0.0", "prod", "https://ci/pipeline/42", "deadbeef1234");

        var result = await sut.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.ServiceName.Should().Be(command.ServiceName);
        result.Value.Version.Should().Be(command.Version);
        result.Value.Environment.Should().Be(command.Environment);
        repository.Received(1).Add(Arg.Any<Release>());
        await unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    // ── ClassifyChangeLevel ───────────────────────────────────────────────

    [Fact]
    public async Task ClassifyChangeLevel_Should_UpdateChangeLevel_WhenReleaseExists()
    {
        var release = CreateRelease();
        var repository = Substitute.For<IReleaseRepository>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var sut = new ClassifyChangeLevelFeature.Handler(repository, unitOfWork);

        repository.GetByIdAsync(Arg.Any<ReleaseId>(), Arg.Any<CancellationToken>())
            .Returns(release);

        var result = await sut.Handle(
            new ClassifyChangeLevelFeature.Command(release.Id.Value, ChangeLevel.Breaking),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.ChangeLevel.Should().Be(ChangeLevel.Breaking);
        await unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ClassifyChangeLevel_Should_ReturnError_WhenReleaseNotFound()
    {
        var repository = Substitute.For<IReleaseRepository>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var sut = new ClassifyChangeLevelFeature.Handler(repository, unitOfWork);

        repository.GetByIdAsync(Arg.Any<ReleaseId>(), Arg.Any<CancellationToken>())
            .Returns((Release?)null);

        var result = await sut.Handle(
            new ClassifyChangeLevelFeature.Command(Guid.NewGuid(), ChangeLevel.Breaking),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("Release.NotFound");
    }

    // ── CalculateBlastRadius ──────────────────────────────────────────────

    [Fact]
    public async Task CalculateBlastRadius_Should_CreateReport_AndReturnResponse()
    {
        var release = CreateRelease();
        var releaseRepository = Substitute.For<IReleaseRepository>();
        var blastRadiusRepository = Substitute.For<IBlastRadiusRepository>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var dateTimeProvider = Substitute.For<IDateTimeProvider>();
        var sut = new CalculateBlastRadiusFeature.Handler(releaseRepository, blastRadiusRepository, unitOfWork, dateTimeProvider);

        releaseRepository.GetByIdAsync(Arg.Any<ReleaseId>(), Arg.Any<CancellationToken>())
            .Returns(release);
        dateTimeProvider.UtcNow.Returns(FixedNow);

        var direct = new List<string> { "ServiceA", "ServiceB" };
        var transitive = new List<string> { "ServiceC" };

        var result = await sut.Handle(
            new CalculateBlastRadiusFeature.Command(release.Id.Value, direct, transitive),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TotalAffectedConsumers.Should().Be(direct.Count + transitive.Count);
        blastRadiusRepository.Received(1).Add(Arg.Any<BlastRadiusReport>());
        await unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    // ── ComputeChangeScore ────────────────────────────────────────────────

    [Fact]
    public async Task ComputeChangeScore_Should_Succeed_WithValidWeights()
    {
        var release = CreateRelease();
        var releaseRepository = Substitute.For<IReleaseRepository>();
        var scoreRepository = Substitute.For<IChangeScoreRepository>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var dateTimeProvider = Substitute.For<IDateTimeProvider>();
        var sut = new ComputeChangeScoreFeature.Handler(releaseRepository, scoreRepository, unitOfWork, dateTimeProvider);

        releaseRepository.GetByIdAsync(Arg.Any<ReleaseId>(), Arg.Any<CancellationToken>())
            .Returns(release);
        dateTimeProvider.UtcNow.Returns(FixedNow);

        var result = await sut.Handle(
            new ComputeChangeScoreFeature.Command(release.Id.Value, 0.6m, 0.4m, 0.2m),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Score.Should().BeInRange(0m, 1m);
        scoreRepository.Received(1).Add(Arg.Any<ChangeIntelligenceScore>());
        await unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    // ── GetRelease ────────────────────────────────────────────────────────

    [Fact]
    public async Task GetRelease_Should_ReturnResponse_WhenReleaseExists()
    {
        var release = CreateRelease();
        var repository = Substitute.For<IReleaseRepository>();
        var sut = new GetReleaseFeature.Handler(repository);

        repository.GetByIdAsync(Arg.Any<ReleaseId>(), Arg.Any<CancellationToken>())
            .Returns(release);

        var result = await sut.Handle(new GetReleaseFeature.Query(release.Id.Value), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.ReleaseId.Should().Be(release.Id.Value);
        result.Value.ServiceName.Should().Be(release.ServiceName);
        result.Value.Version.Should().Be(release.Version);
        result.Value.Environment.Should().Be(release.Environment);
    }

    [Fact]
    public async Task GetRelease_Should_ReturnError_WhenReleaseNotFound()
    {
        var repository = Substitute.For<IReleaseRepository>();
        var sut = new GetReleaseFeature.Handler(repository);

        repository.GetByIdAsync(Arg.Any<ReleaseId>(), Arg.Any<CancellationToken>())
            .Returns((Release?)null);

        var result = await sut.Handle(new GetReleaseFeature.Query(Guid.NewGuid()), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("Release.NotFound");
    }
}
