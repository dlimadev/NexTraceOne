using Microsoft.Extensions.Logging.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Abstractions;
using NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Services;
using NexTraceOne.ChangeGovernance.Domain.ChangeIntelligence.Entities;
using NexTraceOne.ChangeGovernance.Domain.ChangeIntelligence.Enums;
using NexTraceOne.Configuration.Application.Abstractions;

using GetPostReleaseReviewFeature = NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Features.GetPostReleaseReview.GetPostReleaseReview;
using RecordObservationMetricsFeature = NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Features.RecordObservationMetrics.RecordObservationMetrics;

namespace NexTraceOne.ChangeGovernance.Tests.ChangeIntelligence.Application.Features;

/// <summary>Testes do pipeline de post-change verification (P5.5).</summary>
public sealed class PostChangeVerificationTests
{
    private static readonly DateTimeOffset FixedNow = new(2025, 6, 1, 10, 0, 0, TimeSpan.Zero);

    // ── PostChangeVerificationService ─────────────────────────────────────

    [Fact]
    public void PostChangeVerificationService_ShouldClassify_Negative_OnSevereDegradation()
    {
        var sut = new PostChangeVerificationService();
        var baseline = CreateBaseline(errorRate: 0.01m, avgLatency: 100m, p95Latency: 200m);
        var window = CreateCollectedWindow(errorRate: 0.03m, avgLatency: 150m, p95Latency: 300m); // +200% error, +50% latency

        var result = sut.Compare(baseline, window, ObservationPhase.PreliminaryReview);

        result.Outcome.Should().Be(ReviewOutcome.Negative);
        result.ConfidenceScore.Should().BeGreaterThan(0m);
        result.Summary.Should().Contain("PreliminaryReview");
    }

    [Fact]
    public void PostChangeVerificationService_ShouldClassify_NeedsAttention_OnMildDegradation()
    {
        var sut = new PostChangeVerificationService();
        var baseline = CreateBaseline(errorRate: 0.02m, avgLatency: 100m, p95Latency: 200m);
        var window = CreateCollectedWindow(errorRate: 0.03m, avgLatency: 120m, p95Latency: 240m); // +50% error, +20% latency

        var result = sut.Compare(baseline, window, ObservationPhase.PreliminaryReview);

        result.Outcome.Should().Be(ReviewOutcome.NeedsAttention);
        result.ConfidenceScore.Should().BeGreaterThan(0m);
    }

    [Fact]
    public void PostChangeVerificationService_ShouldClassify_Positive_OnImprovement()
    {
        var sut = new PostChangeVerificationService();
        var baseline = CreateBaseline(errorRate: 0.05m, avgLatency: 200m, p95Latency: 400m);
        var window = CreateCollectedWindow(errorRate: 0.02m, avgLatency: 150m, p95Latency: 300m); // -60% error, -25% latency

        var result = sut.Compare(baseline, window, ObservationPhase.ConsolidatedReview);

        result.Outcome.Should().Be(ReviewOutcome.Positive);
        result.ConfidenceScore.Should().BeGreaterThan(0m);
    }

    [Fact]
    public void PostChangeVerificationService_ShouldClassify_Neutral_OnNoSignificantChange()
    {
        var sut = new PostChangeVerificationService();
        var baseline = CreateBaseline(errorRate: 0.02m, avgLatency: 100m, p95Latency: 200m);
        var window = CreateCollectedWindow(errorRate: 0.021m, avgLatency: 102m, p95Latency: 202m); // ~1% delta

        var result = sut.Compare(baseline, window, ObservationPhase.InitialObservation);

        result.Outcome.Should().Be(ReviewOutcome.Neutral);
    }

    [Fact]
    public void PostChangeVerificationService_ShouldReturn_Inconclusive_WhenWindowNotCollected()
    {
        var sut = new PostChangeVerificationService();
        var baseline = CreateBaseline(errorRate: 0.02m, avgLatency: 100m, p95Latency: 200m);
        var window = ObservationWindow.Create(
            ReleaseId.New(),
            ObservationPhase.InitialObservation,
            FixedNow,
            FixedNow.AddHours(1));

        var result = sut.Compare(baseline, window, ObservationPhase.InitialObservation);

        result.Outcome.Should().Be(ReviewOutcome.Inconclusive);
        result.ConfidenceScore.Should().Be(0m);
    }

    [Theory]
    [InlineData(ObservationPhase.InitialObservation, 0.30, 0.40)]
    [InlineData(ObservationPhase.PreliminaryReview, 0.60, 0.70)]
    [InlineData(ObservationPhase.ConsolidatedReview, 0.80, 0.90)]
    [InlineData(ObservationPhase.FinalReview, 0.90, 1.00)]
    public void PostChangeVerificationService_ShouldScale_Confidence_ByPhase(
        ObservationPhase phase, double minExpected, double maxExpected)
    {
        var sut = new PostChangeVerificationService();
        var baseline = CreateBaseline(errorRate: 0.02m, avgLatency: 100m, p95Latency: 200m);
        var window = CreateCollectedWindow(errorRate: 0.02m, avgLatency: 100m, p95Latency: 200m);

        var result = sut.Compare(baseline, window, phase);

        result.ConfidenceScore.Should().BeInRange((decimal)minExpected, (decimal)maxExpected);
    }

    // ── RecordObservationMetrics handler ─────────────────────────────────

    [Fact]
    public async Task RecordObservationMetrics_ShouldCreateWindowAndStartReview()
    {
        var release = CreateRelease();
        var baseline = CreateBaseline(errorRate: 0.02m, avgLatency: 100m, p95Latency: 200m);

        var releaseRepo = Substitute.For<IReleaseRepository>();
        var baselineRepo = Substitute.For<IReleaseBaselineRepository>();
        var windowRepo = Substitute.For<IObservationWindowRepository>();
        var reviewRepo = Substitute.For<IPostReleaseReviewRepository>();
        var uow = Substitute.For<IChangeIntelligenceUnitOfWork>();
        var dateTimeProvider = Substitute.For<IDateTimeProvider>();
        dateTimeProvider.UtcNow.Returns(FixedNow);

        releaseRepo.GetByIdAsync(Arg.Any<ReleaseId>(), Arg.Any<CancellationToken>()).Returns(release);
        baselineRepo.GetByReleaseIdAsync(Arg.Any<ReleaseId>(), Arg.Any<CancellationToken>()).Returns(baseline);
        windowRepo.GetByReleaseIdAndPhaseAsync(Arg.Any<ReleaseId>(), Arg.Any<ObservationPhase>(), Arg.Any<CancellationToken>()).Returns((ObservationWindow?)null);
        reviewRepo.GetByReleaseIdAsync(Arg.Any<ReleaseId>(), Arg.Any<CancellationToken>()).Returns((PostReleaseReview?)null);

        var sut = new RecordObservationMetricsFeature.Handler(
            releaseRepo, baselineRepo, windowRepo, reviewRepo,
            new PostChangeVerificationService(), uow, dateTimeProvider,
            CreateEnabledBehaviorService());

        var command = new RecordObservationMetricsFeature.Command(
            release.Id.Value,
            ObservationPhase.InitialObservation,
            FixedNow.AddMinutes(-30), FixedNow,
            100m, 0.02m, 102m, 205m, 310m, 1024m);

        var result = await sut.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Phase.Should().Be("InitialObservation");
        result.Value.IsNewWindow.Should().BeTrue();
        result.Value.ConfidenceScore.Should().BeGreaterThan(0m);
        windowRepo.Received(1).Add(Arg.Any<ObservationWindow>());
        reviewRepo.Received(1).Add(Arg.Any<PostReleaseReview>());
        await uow.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RecordObservationMetrics_ShouldProgressExistingReview()
    {
        var release = CreateRelease();
        var baseline = CreateBaseline(errorRate: 0.02m, avgLatency: 100m, p95Latency: 200m);
        var existingReview = PostReleaseReview.Start(release.Id, FixedNow);

        var releaseRepo = Substitute.For<IReleaseRepository>();
        var baselineRepo = Substitute.For<IReleaseBaselineRepository>();
        var windowRepo = Substitute.For<IObservationWindowRepository>();
        var reviewRepo = Substitute.For<IPostReleaseReviewRepository>();
        var uow = Substitute.For<IChangeIntelligenceUnitOfWork>();
        var dateTimeProvider = Substitute.For<IDateTimeProvider>();
        dateTimeProvider.UtcNow.Returns(FixedNow);

        releaseRepo.GetByIdAsync(Arg.Any<ReleaseId>(), Arg.Any<CancellationToken>()).Returns(release);
        baselineRepo.GetByReleaseIdAsync(Arg.Any<ReleaseId>(), Arg.Any<CancellationToken>()).Returns(baseline);
        windowRepo.GetByReleaseIdAndPhaseAsync(Arg.Any<ReleaseId>(), Arg.Any<ObservationPhase>(), Arg.Any<CancellationToken>()).Returns((ObservationWindow?)null);
        reviewRepo.GetByReleaseIdAsync(Arg.Any<ReleaseId>(), Arg.Any<CancellationToken>()).Returns(existingReview);

        var sut = new RecordObservationMetricsFeature.Handler(
            releaseRepo, baselineRepo, windowRepo, reviewRepo,
            new PostChangeVerificationService(), uow, dateTimeProvider,
            CreateEnabledBehaviorService());

        var command = new RecordObservationMetricsFeature.Command(
            release.Id.Value,
            ObservationPhase.PreliminaryReview,
            FixedNow.AddHours(-4), FixedNow,
            100m, 0.05m, 130m, 250m, 380m, 1024m); // Mild degradation vs baseline

        var result = await sut.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        reviewRepo.Received(1).Update(Arg.Any<PostReleaseReview>());
        reviewRepo.DidNotReceive().Add(Arg.Any<PostReleaseReview>());
    }

    [Fact]
    public async Task RecordObservationMetrics_ShouldFail_WhenReleaseNotFound()
    {
        var releaseRepo = Substitute.For<IReleaseRepository>();
        releaseRepo.GetByIdAsync(Arg.Any<ReleaseId>(), Arg.Any<CancellationToken>()).Returns((Release?)null);

        var sut = new RecordObservationMetricsFeature.Handler(
            releaseRepo,
            Substitute.For<IReleaseBaselineRepository>(),
            Substitute.For<IObservationWindowRepository>(),
            Substitute.For<IPostReleaseReviewRepository>(),
            new PostChangeVerificationService(),
            Substitute.For<IChangeIntelligenceUnitOfWork>(),
            Substitute.For<IDateTimeProvider>(),
            CreateEnabledBehaviorService());

        var result = await sut.Handle(
            new RecordObservationMetricsFeature.Command(Guid.NewGuid(), ObservationPhase.InitialObservation,
                FixedNow.AddMinutes(-30), FixedNow, 100m, 0.02m, 100m, 200m, 300m, 1024m),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("Release.NotFound");
    }

    [Fact]
    public async Task RecordObservationMetrics_ShouldFail_WhenBaselineNotFound()
    {
        var release = CreateRelease();
        var releaseRepo = Substitute.For<IReleaseRepository>();
        var baselineRepo = Substitute.For<IReleaseBaselineRepository>();
        releaseRepo.GetByIdAsync(Arg.Any<ReleaseId>(), Arg.Any<CancellationToken>()).Returns(release);
        baselineRepo.GetByReleaseIdAsync(Arg.Any<ReleaseId>(), Arg.Any<CancellationToken>()).Returns((ReleaseBaseline?)null);

        var sut = new RecordObservationMetricsFeature.Handler(
            releaseRepo, baselineRepo,
            Substitute.For<IObservationWindowRepository>(),
            Substitute.For<IPostReleaseReviewRepository>(),
            new PostChangeVerificationService(),
            Substitute.For<IChangeIntelligenceUnitOfWork>(),
            Substitute.For<IDateTimeProvider>(),
            CreateEnabledBehaviorService());

        var result = await sut.Handle(
            new RecordObservationMetricsFeature.Command(release.Id.Value, ObservationPhase.InitialObservation,
                FixedNow.AddMinutes(-30), FixedNow, 100m, 0.02m, 100m, 200m, 300m, 1024m),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("ReleaseBaseline.NotFound");
    }

    // ── GetPostReleaseReview handler ──────────────────────────────────────

    [Fact]
    public async Task GetPostReleaseReview_ShouldReturn_ReviewWithWindows()
    {
        var release = CreateRelease();
        var review = PostReleaseReview.Start(release.Id, FixedNow);

        var releaseRepo = Substitute.For<IReleaseRepository>();
        var reviewRepo = Substitute.For<IPostReleaseReviewRepository>();
        var windowRepo = Substitute.For<IObservationWindowRepository>();
        var baselineRepo = Substitute.For<IReleaseBaselineRepository>();

        releaseRepo.GetByIdAsync(Arg.Any<ReleaseId>(), Arg.Any<CancellationToken>()).Returns(release);
        reviewRepo.GetByReleaseIdAsync(Arg.Any<ReleaseId>(), Arg.Any<CancellationToken>()).Returns(review);
        windowRepo.ListByReleaseIdAsync(Arg.Any<ReleaseId>(), Arg.Any<CancellationToken>()).Returns(new List<ObservationWindow>());
        baselineRepo.GetByReleaseIdAsync(Arg.Any<ReleaseId>(), Arg.Any<CancellationToken>()).Returns((ReleaseBaseline?)null);

        var sut = new GetPostReleaseReviewFeature.Handler(releaseRepo, reviewRepo, windowRepo, baselineRepo);

        var result = await sut.Handle(new GetPostReleaseReviewFeature.Query(release.Id.Value), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.ReviewId.Should().Be(review.Id.Value);
        result.Value.Outcome.Should().Be("Inconclusive");
        result.Value.ObservationWindows.Should().BeEmpty();
    }

    [Fact]
    public async Task GetPostReleaseReview_ShouldFail_WhenReviewNotFound()
    {
        var release = CreateRelease();
        var releaseRepo = Substitute.For<IReleaseRepository>();
        var reviewRepo = Substitute.For<IPostReleaseReviewRepository>();
        releaseRepo.GetByIdAsync(Arg.Any<ReleaseId>(), Arg.Any<CancellationToken>()).Returns(release);
        reviewRepo.GetByReleaseIdAsync(Arg.Any<ReleaseId>(), Arg.Any<CancellationToken>()).Returns((PostReleaseReview?)null);

        var sut = new GetPostReleaseReviewFeature.Handler(
            releaseRepo, reviewRepo,
            Substitute.For<IObservationWindowRepository>(),
            Substitute.For<IReleaseBaselineRepository>());

        var result = await sut.Handle(new GetPostReleaseReviewFeature.Query(release.Id.Value), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("PostReleaseReview.NotFound");
    }

    // ── Helpers ───────────────────────────────────────────────────────────

    private static Release CreateRelease()
        => Release.Create(Guid.NewGuid(), Guid.Empty, "payments-service", "1.2.0", "staging", "github-actions", "abc123def456", FixedNow);

    private static ReleaseBaseline CreateBaseline(decimal errorRate, decimal avgLatency, decimal p95Latency)
        => ReleaseBaseline.Create(
            ReleaseId.New(), 500m, errorRate, avgLatency, p95Latency, p95Latency * 1.3m, 10240m,
            FixedNow.AddHours(-2), FixedNow.AddHours(-1), FixedNow.AddHours(-1));

    private static ObservationWindow CreateCollectedWindow(decimal errorRate, decimal avgLatency, decimal p95Latency)
    {
        var releaseId = ReleaseId.New();
        var window = ObservationWindow.Create(
            releaseId, ObservationPhase.InitialObservation, FixedNow.AddMinutes(-30), FixedNow);

        window.RecordMetrics(500m, errorRate, avgLatency, p95Latency, p95Latency * 1.3m, 10240m, FixedNow);
        return window;
    }
    private static IEnvironmentBehaviorService CreateEnabledBehaviorService()
    {
        var svc = Substitute.For<IEnvironmentBehaviorService>();
        svc.IsEnabledAsync(Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<CancellationToken>()).Returns(true);
        return svc;
    }

}