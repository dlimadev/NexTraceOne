using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Abstractions;
using NexTraceOne.ChangeGovernance.Domain.ChangeIntelligence.Entities;

using RecordFeatureFlagStateFeature = NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Features.RecordFeatureFlagState.RecordFeatureFlagState;
using GetFeatureFlagAwarenessFeature = NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Features.GetFeatureFlagAwareness.GetFeatureFlagAwareness;

namespace NexTraceOne.ChangeGovernance.Tests.ChangeIntelligence.Application.Features;

/// <summary>
/// Testes das features de Feature Flag Awareness (Phase 3.3 — Change Confidence Score V2).
/// Cobre RecordFeatureFlagState (Command) e GetFeatureFlagAwareness (Query).
/// </summary>
public sealed class FeatureFlagAwarenessTests
{
    private static readonly DateTimeOffset FixedNow =
        new(2026, 4, 4, 12, 0, 0, TimeSpan.Zero);

    private readonly IReleaseRepository _releaseRepo = Substitute.For<IReleaseRepository>();
    private readonly IFeatureFlagStateRepository _flagRepo = Substitute.For<IFeatureFlagStateRepository>();
    private readonly IChangeIntelligenceUnitOfWork _uow = Substitute.For<IChangeIntelligenceUnitOfWork>();
    private readonly IDateTimeProvider _clock = Substitute.For<IDateTimeProvider>();

    private static Release CreateRelease(string service = "svc-payments")
        => Release.Create(Guid.NewGuid(), Guid.Empty, service, "1.0.0", "production",
            "https://ci/p", "abc123", FixedNow);

    public FeatureFlagAwarenessTests()
    {
        _clock.UtcNow.Returns(FixedNow);
    }

    // ── RecordFeatureFlagState ──────────────────────────────────────────────

    [Fact]
    public async Task RecordFeatureFlagState_WhenReleaseNotFound_ShouldReturnFailure()
    {
        _releaseRepo.GetByIdAsync(Arg.Any<ReleaseId>(), Arg.Any<CancellationToken>())
            .Returns((Release?)null);

        var sut = new RecordFeatureFlagStateFeature.Handler(_releaseRepo, _flagRepo, _uow, _clock);
        var result = await sut.Handle(
            new RecordFeatureFlagStateFeature.Command(
                Guid.NewGuid(), 3, 1, 1, "LaunchDarkly", null),
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task RecordFeatureFlagState_WhenValid_ShouldPersistAndReturnSuccess()
    {
        var release = CreateRelease();
        _releaseRepo.GetByIdAsync(Arg.Any<ReleaseId>(), Arg.Any<CancellationToken>())
            .Returns(release);

        var sut = new RecordFeatureFlagStateFeature.Handler(_releaseRepo, _flagRepo, _uow, _clock);
        var result = await sut.Handle(
            new RecordFeatureFlagStateFeature.Command(
                release.Id.Value,
                ActiveFlagCount: 5,
                CriticalFlagCount: 2,
                NewFeatureFlagCount: 1,
                FlagProvider: "Unleash",
                FlagsJson: null),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.ActiveFlagCount.Should().Be(5);
        result.Value.CriticalFlagCount.Should().Be(2);
        result.Value.FlagProvider.Should().Be("Unleash");
        result.Value.RecordedAt.Should().Be(FixedNow);

        _flagRepo.Received(1).Add(Arg.Any<ReleaseFeatureFlagState>());
        await _uow.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RecordFeatureFlagState_Validator_WhenFlagProviderEmpty_ShouldFail()
    {
        var validator = new RecordFeatureFlagStateFeature.Validator();
        var result = validator.Validate(new RecordFeatureFlagStateFeature.Command(
            Guid.NewGuid(), 3, 1, 1, "", null));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "FlagProvider");
    }

    // ── GetFeatureFlagAwareness ─────────────────────────────────────────────

    [Fact]
    public async Task GetFeatureFlagAwareness_WhenReleaseNotFound_ShouldReturnFailure()
    {
        _releaseRepo.GetByIdAsync(Arg.Any<ReleaseId>(), Arg.Any<CancellationToken>())
            .Returns((Release?)null);
        _flagRepo.GetLatestByReleaseIdAsync(Arg.Any<ReleaseId>(), Arg.Any<CancellationToken>())
            .Returns((ReleaseFeatureFlagState?)null);

        var sut = new GetFeatureFlagAwarenessFeature.Handler(_releaseRepo, _flagRepo);
        var result = await sut.Handle(
            new GetFeatureFlagAwarenessFeature.Query(Guid.NewGuid()),
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task GetFeatureFlagAwareness_WhenNoStateRecorded_ShouldReturnUnknown()
    {
        var release = CreateRelease();
        _releaseRepo.GetByIdAsync(Arg.Any<ReleaseId>(), Arg.Any<CancellationToken>())
            .Returns(release);
        _flagRepo.GetLatestByReleaseIdAsync(Arg.Any<ReleaseId>(), Arg.Any<CancellationToken>())
            .Returns((ReleaseFeatureFlagState?)null);

        var sut = new GetFeatureFlagAwarenessFeature.Handler(_releaseRepo, _flagRepo);
        var result = await sut.Handle(
            new GetFeatureFlagAwarenessFeature.Query(release.Id.Value),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.HasData.Should().BeFalse();
        result.Value.RiskLevel.Should().Be("Unknown");
    }

    [Theory]
    [InlineData(0, 0, "Minimal")]
    [InlineData(3, 0, "Minimal")]
    [InlineData(5, 0, "Low")]
    [InlineData(1, 1, "Medium")]
    [InlineData(5, 1, "Medium")]
    [InlineData(5, 3, "High")]
    public async Task GetFeatureFlagAwareness_RiskLevelMapping_ShouldBeCorrect(
        int activeFlags, int criticalFlags, string expectedRisk)
    {
        var release = CreateRelease();
        _releaseRepo.GetByIdAsync(Arg.Any<ReleaseId>(), Arg.Any<CancellationToken>())
            .Returns(release);

        var state = ReleaseFeatureFlagState.Create(
            release.Id, activeFlags, criticalFlags, 0, "LaunchDarkly", null, FixedNow);
        _flagRepo.GetLatestByReleaseIdAsync(Arg.Any<ReleaseId>(), Arg.Any<CancellationToken>())
            .Returns(state);

        var sut = new GetFeatureFlagAwarenessFeature.Handler(_releaseRepo, _flagRepo);
        var result = await sut.Handle(
            new GetFeatureFlagAwarenessFeature.Query(release.Id.Value),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.RiskLevel.Should().Be(expectedRisk,
            $"active={activeFlags}, critical={criticalFlags} should map to {expectedRisk}");
    }
}
