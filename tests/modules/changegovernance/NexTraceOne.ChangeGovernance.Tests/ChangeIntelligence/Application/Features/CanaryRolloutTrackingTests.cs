using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Abstractions;
using NexTraceOne.ChangeGovernance.Domain.ChangeIntelligence.Entities;

using RecordCanaryRolloutFeature = NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Features.RecordCanaryRollout.RecordCanaryRollout;
using GetCanaryRolloutStatusFeature = NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Features.GetCanaryRolloutStatus.GetCanaryRolloutStatus;

namespace NexTraceOne.ChangeGovernance.Tests.ChangeIntelligence.Application.Features;

/// <summary>
/// Testes das features de Canary Deployment Tracking (Phase 3.3 — Change Confidence Score V2).
/// Cobre RecordCanaryRollout (Command) e GetCanaryRolloutStatus (Query).
/// </summary>
public sealed class CanaryRolloutTrackingTests
{
    private static readonly DateTimeOffset FixedNow =
        new(2026, 4, 4, 12, 0, 0, TimeSpan.Zero);

    private readonly IReleaseRepository _releaseRepo = Substitute.For<IReleaseRepository>();
    private readonly ICanaryRolloutRepository _canaryRepo = Substitute.For<ICanaryRolloutRepository>();
    private readonly IChangeIntelligenceUnitOfWork _uow = Substitute.For<IChangeIntelligenceUnitOfWork>();
    private readonly IDateTimeProvider _clock = Substitute.For<IDateTimeProvider>();

    private static Release CreateRelease(string service = "svc-payments")
        => Release.Create(Guid.NewGuid(), Guid.Empty, service, "1.0.0", "production",
            "https://ci/p", "abc123", FixedNow);

    public CanaryRolloutTrackingTests()
    {
        _clock.UtcNow.Returns(FixedNow);
    }

    // ── RecordCanaryRollout ─────────────────────────────────────────────────

    [Fact]
    public async Task RecordCanaryRollout_WhenReleaseNotFound_ShouldReturnFailure()
    {
        _releaseRepo.GetByIdAsync(Arg.Any<ReleaseId>(), Arg.Any<CancellationToken>())
            .Returns((Release?)null);

        var sut = new RecordCanaryRolloutFeature.Handler(_releaseRepo, _canaryRepo, _uow, _clock);
        var result = await sut.Handle(
            new RecordCanaryRolloutFeature.Command(Guid.NewGuid(), 25m, 2, 8, "Argo Rollouts", false, false),
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task RecordCanaryRollout_WhenValid_ShouldPersistAndReturnSuccess()
    {
        var release = CreateRelease();
        _releaseRepo.GetByIdAsync(Arg.Any<ReleaseId>(), Arg.Any<CancellationToken>())
            .Returns(release);

        var sut = new RecordCanaryRolloutFeature.Handler(_releaseRepo, _canaryRepo, _uow, _clock);
        var result = await sut.Handle(
            new RecordCanaryRolloutFeature.Command(
                release.Id.Value, 25m, 2, 8, "Argo Rollouts", false, false),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.RolloutPercentage.Should().Be(25m);
        result.Value.ActiveInstances.Should().Be(2);
        result.Value.TotalInstances.Should().Be(8);
        result.Value.IsPromoted.Should().BeFalse();
        result.Value.IsAborted.Should().BeFalse();
        result.Value.RecordedAt.Should().Be(FixedNow);

        _canaryRepo.Received(1).Add(Arg.Any<CanaryRollout>());
        await _uow.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RecordCanaryRollout_Validator_WhenPercentageOutOfRange_ShouldFail()
    {
        var validator = new RecordCanaryRolloutFeature.Validator();
        var result = validator.Validate(new RecordCanaryRolloutFeature.Command(
            Guid.NewGuid(), 110m, 2, 8, "Flagger", false, false));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "RolloutPercentage");
    }

    [Fact]
    public async Task RecordCanaryRollout_Validator_WhenPromotedAndAborted_ShouldFail()
    {
        var validator = new RecordCanaryRolloutFeature.Validator();
        var result = validator.Validate(new RecordCanaryRolloutFeature.Command(
            Guid.NewGuid(), 100m, 8, 8, "Flagger", true, true));

        result.IsValid.Should().BeFalse();
    }

    // ── GetCanaryRolloutStatus ──────────────────────────────────────────────

    [Fact]
    public async Task GetCanaryRolloutStatus_WhenReleaseNotFound_ShouldReturnFailure()
    {
        _releaseRepo.GetByIdAsync(Arg.Any<ReleaseId>(), Arg.Any<CancellationToken>())
            .Returns((Release?)null);

        var sut = new GetCanaryRolloutStatusFeature.Handler(_releaseRepo, _canaryRepo);
        var result = await sut.Handle(
            new GetCanaryRolloutStatusFeature.Query(Guid.NewGuid()),
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task GetCanaryRolloutStatus_WhenNoDataRecorded_ShouldReturnUnknown()
    {
        var release = CreateRelease();
        _releaseRepo.GetByIdAsync(Arg.Any<ReleaseId>(), Arg.Any<CancellationToken>())
            .Returns(release);
        _canaryRepo.GetLatestByReleaseIdAsync(Arg.Any<ReleaseId>(), Arg.Any<CancellationToken>())
            .Returns((CanaryRollout?)null);
        _canaryRepo.ListByReleaseIdAsync(Arg.Any<ReleaseId>(), Arg.Any<CancellationToken>())
            .Returns(new List<CanaryRollout>());

        var sut = new GetCanaryRolloutStatusFeature.Handler(_releaseRepo, _canaryRepo);
        var result = await sut.Handle(
            new GetCanaryRolloutStatusFeature.Query(release.Id.Value),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.HasData.Should().BeFalse();
        result.Value.ConfidenceBoost.Should().Be("Unknown");
    }

    [Theory]
    [InlineData(5.0, false, false, "Minimal")]
    [InlineData(15.0, false, false, "Low")]
    [InlineData(60.0, false, false, "Medium")]
    [InlineData(100.0, true, false, "High")]
    [InlineData(100.0, false, false, "High")]
    [InlineData(50.0, false, true, "Negative")]
    public async Task GetCanaryRolloutStatus_ConfidenceBoostMapping_ShouldBeCorrect(
        double rolloutPctDouble, bool isPromoted, bool isAborted, string expectedBoost)
    {
        var rolloutPct = (decimal)rolloutPctDouble;
        var release = CreateRelease();
        _releaseRepo.GetByIdAsync(Arg.Any<ReleaseId>(), Arg.Any<CancellationToken>())
            .Returns(release);

        var rollout = CanaryRollout.Create(
            release.Id, rolloutPct,
            (int)rolloutPct, 100,
            "Argo Rollouts", isPromoted, isAborted, FixedNow);

        _canaryRepo.GetLatestByReleaseIdAsync(Arg.Any<ReleaseId>(), Arg.Any<CancellationToken>())
            .Returns(rollout);
        _canaryRepo.ListByReleaseIdAsync(Arg.Any<ReleaseId>(), Arg.Any<CancellationToken>())
            .Returns(new List<CanaryRollout> { rollout });

        var sut = new GetCanaryRolloutStatusFeature.Handler(_releaseRepo, _canaryRepo);
        var result = await sut.Handle(
            new GetCanaryRolloutStatusFeature.Query(release.Id.Value),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.ConfidenceBoost.Should().Be(expectedBoost,
            $"rollout={rolloutPct}%, promoted={isPromoted}, aborted={isAborted} → {expectedBoost}");
        result.Value.SnapshotCount.Should().Be(1);
    }
}
