using NexTraceOne.BuildingBlocks.Core.Enums;
using NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Abstractions;
using NexTraceOne.ChangeGovernance.Domain.ChangeIntelligence.Entities;
using NexTraceOne.ChangeGovernance.Domain.ChangeIntelligence.Enums;

using GetChangeIntelligenceSummaryFeature = NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Features.GetChangeIntelligenceSummary.GetChangeIntelligenceSummary;

namespace NexTraceOne.ChangeGovernance.Tests.ChangeIntelligence.Application.Features;

/// <summary>
/// Testes unitários para o handler GetChangeIntelligenceSummary.
/// Verifica o agregado completo de inteligência de mudança para uma release.
/// </summary>
public sealed class GetChangeIntelligenceSummaryTests
{
    private static readonly DateTimeOffset FixedNow = new(2026, 4, 18, 21, 0, 0, TimeSpan.Zero);

    private readonly IReleaseRepository _releaseRepo = Substitute.For<IReleaseRepository>();
    private readonly IChangeScoreRepository _scoreRepo = Substitute.For<IChangeScoreRepository>();
    private readonly IBlastRadiusRepository _blastRepo = Substitute.For<IBlastRadiusRepository>();
    private readonly IExternalMarkerRepository _markerRepo = Substitute.For<IExternalMarkerRepository>();
    private readonly IReleaseBaselineRepository _baselineRepo = Substitute.For<IReleaseBaselineRepository>();
    private readonly IPostReleaseReviewRepository _reviewRepo = Substitute.For<IPostReleaseReviewRepository>();
    private readonly IRollbackAssessmentRepository _rollbackRepo = Substitute.For<IRollbackAssessmentRepository>();
    private readonly IChangeEventRepository _eventRepo = Substitute.For<IChangeEventRepository>();

    private static Release CreateRelease(string service = "svc-payments")
    {
        var r = Release.Create(
            Guid.NewGuid(), Guid.Empty, service, "2.1.0", "production",
            "https://ci/pipeline", "abc123", FixedNow.AddDays(-1));
        r.Classify(ChangeLevel.Breaking);
        r.UpdateStatus(DeploymentStatus.Running);
        r.UpdateStatus(DeploymentStatus.Succeeded);
        return r;
    }

    private GetChangeIntelligenceSummaryFeature.Handler CreateHandler() =>
        new(_releaseRepo, _scoreRepo, _blastRepo, _markerRepo,
            _baselineRepo, _reviewRepo, _rollbackRepo, _eventRepo);

    // ── Not Found ──────────────────────────────────────────────────────────

    [Fact]
    public async Task GetChangeIntelligenceSummary_WhenReleaseNotFound_ShouldReturnFailure()
    {
        _releaseRepo.GetByIdAsync(Arg.Any<ReleaseId>(), Arg.Any<CancellationToken>())
            .Returns((Release?)null);

        var sut = CreateHandler();
        var result = await sut.Handle(
            new GetChangeIntelligenceSummaryFeature.Query(Guid.NewGuid()),
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().NotBeNull();
    }

    // ── Minimal (no optional data) ─────────────────────────────────────────

    [Fact]
    public async Task GetChangeIntelligenceSummary_WhenReleaseExists_ShouldReturnReleaseData()
    {
        var release = CreateRelease();
        _releaseRepo.GetByIdAsync(Arg.Any<ReleaseId>(), Arg.Any<CancellationToken>())
            .Returns(release);
        _scoreRepo.GetByReleaseIdAsync(Arg.Any<ReleaseId>(), Arg.Any<CancellationToken>())
            .Returns((ChangeIntelligenceScore?)null);
        _blastRepo.GetByReleaseIdAsync(Arg.Any<ReleaseId>(), Arg.Any<CancellationToken>())
            .Returns((BlastRadiusReport?)null);
        _markerRepo.ListByReleaseIdAsync(Arg.Any<ReleaseId>(), Arg.Any<CancellationToken>())
            .Returns(new List<ExternalMarker>());
        _baselineRepo.GetByReleaseIdAsync(Arg.Any<ReleaseId>(), Arg.Any<CancellationToken>())
            .Returns((ReleaseBaseline?)null);
        _reviewRepo.GetByReleaseIdAsync(Arg.Any<ReleaseId>(), Arg.Any<CancellationToken>())
            .Returns((PostReleaseReview?)null);
        _rollbackRepo.GetByReleaseIdAsync(Arg.Any<ReleaseId>(), Arg.Any<CancellationToken>())
            .Returns((RollbackAssessment?)null);
        _eventRepo.ListByReleaseIdAsync(Arg.Any<ReleaseId>(), Arg.Any<CancellationToken>())
            .Returns(new List<ChangeEvent>());

        var sut = CreateHandler();
        var result = await sut.Handle(
            new GetChangeIntelligenceSummaryFeature.Query(release.Id.Value),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Release.ServiceName.Should().Be("svc-payments");
        result.Value.Release.Version.Should().Be("2.1.0");
        result.Value.Release.Environment.Should().Be("production");
        result.Value.Score.Should().BeNull();
        result.Value.BlastRadius.Should().BeNull();
        result.Value.Markers.Should().BeEmpty();
        result.Value.PostReleaseReview.Should().BeNull();
        result.Value.RollbackAssessment.Should().BeNull();
        result.Value.Timeline.Should().BeEmpty();
    }

    // ── Validator ──────────────────────────────────────────────────────────

    [Fact]
    public void GetChangeIntelligenceSummary_Validator_WithEmptyGuid_ShouldFail()
    {
        var validator = new GetChangeIntelligenceSummaryFeature.Validator();
        var result = validator.Validate(new GetChangeIntelligenceSummaryFeature.Query(Guid.Empty));
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void GetChangeIntelligenceSummary_Validator_WithValidGuid_ShouldPass()
    {
        var validator = new GetChangeIntelligenceSummaryFeature.Validator();
        var result = validator.Validate(new GetChangeIntelligenceSummaryFeature.Query(Guid.NewGuid()));
        result.IsValid.Should().BeTrue();
    }
}
