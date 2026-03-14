using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.ChangeIntelligence.Application.Abstractions;
using NexTraceOne.ChangeIntelligence.Domain.Entities;
using NexTraceOne.ChangeIntelligence.Domain.Enums;
using RegisterExternalMarkerFeature = NexTraceOne.ChangeIntelligence.Application.Features.RegisterExternalMarker.RegisterExternalMarker;
using CreateFreezeWindowFeature = NexTraceOne.ChangeIntelligence.Application.Features.CreateFreezeWindow.CreateFreezeWindow;
using CheckFreezeConflictFeature = NexTraceOne.ChangeIntelligence.Application.Features.CheckFreezeConflict.CheckFreezeConflict;
using RecordReleaseBaselineFeature = NexTraceOne.ChangeIntelligence.Application.Features.RecordReleaseBaseline.RecordReleaseBaseline;
using StartPostReleaseReviewFeature = NexTraceOne.ChangeIntelligence.Application.Features.StartPostReleaseReview.StartPostReleaseReview;
using ProgressPostReleaseReviewFeature = NexTraceOne.ChangeIntelligence.Application.Features.ProgressPostReleaseReview.ProgressPostReleaseReview;
using AssessRollbackViabilityFeature = NexTraceOne.ChangeIntelligence.Application.Features.AssessRollbackViability.AssessRollbackViability;

namespace NexTraceOne.ChangeIntelligence.Tests.Application.Features;

/// <summary>
/// Testes de handlers da camada Application para os novos features de
/// inteligência de mudança do Change Intelligence Orchestrator.
/// </summary>
public sealed class ChangeIntelligenceExtendedApplicationTests
{
    private static readonly DateTimeOffset FixedNow = new(2025, 6, 1, 10, 0, 0, TimeSpan.Zero);

    private static Release CreateRelease() =>
        Release.Create(Guid.NewGuid(), "TestService", "1.0.0", "staging", "https://ci/pipeline/1", "abc123def456", FixedNow);

    // ═══════════════ RegisterExternalMarker ═══════════════

    [Fact]
    public async Task RegisterExternalMarker_Should_Succeed_WhenReleaseExists()
    {
        var release = CreateRelease();
        var releaseRepo = Substitute.For<IReleaseRepository>();
        var markerRepo = Substitute.For<IExternalMarkerRepository>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var clock = Substitute.For<IDateTimeProvider>();
        clock.UtcNow.Returns(FixedNow);

        releaseRepo.GetByIdAsync(Arg.Any<ReleaseId>(), Arg.Any<CancellationToken>())
            .Returns(release);

        var sut = new RegisterExternalMarkerFeature.Handler(releaseRepo, markerRepo, unitOfWork, clock);

        var result = await sut.Handle(
            new RegisterExternalMarkerFeature.Command(
                release.Id.Value, MarkerType.DeploymentStarted, "GitHub", "run-42", null, FixedNow),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.MarkerType.Should().Be(MarkerType.DeploymentStarted);
        result.Value.SourceSystem.Should().Be("GitHub");
        markerRepo.Received(1).Add(Arg.Any<ExternalMarker>());
        await unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RegisterExternalMarker_Should_Fail_WhenReleaseNotFound()
    {
        var releaseRepo = Substitute.For<IReleaseRepository>();
        var markerRepo = Substitute.For<IExternalMarkerRepository>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var clock = Substitute.For<IDateTimeProvider>();

        releaseRepo.GetByIdAsync(Arg.Any<ReleaseId>(), Arg.Any<CancellationToken>())
            .Returns((Release?)null);

        var sut = new RegisterExternalMarkerFeature.Handler(releaseRepo, markerRepo, unitOfWork, clock);

        var result = await sut.Handle(
            new RegisterExternalMarkerFeature.Command(
                Guid.NewGuid(), MarkerType.BuildCompleted, "Jenkins", "b-1", null, FixedNow),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
    }

    // ═══════════════ CreateFreezeWindow ═══════════════

    [Fact]
    public async Task CreateFreezeWindow_Should_Succeed_WithValidData()
    {
        var freezeRepo = Substitute.For<IFreezeWindowRepository>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var clock = Substitute.For<IDateTimeProvider>();
        var currentUser = Substitute.For<ICurrentUser>();
        clock.UtcNow.Returns(FixedNow);
        currentUser.UserId.Returns("admin-user");

        var sut = new CreateFreezeWindowFeature.Handler(freezeRepo, unitOfWork, clock, currentUser);

        var result = await sut.Handle(
            new CreateFreezeWindowFeature.Command(
                "Black Friday", "Peak sales", FreezeScope.Global, null,
                FixedNow, FixedNow.AddDays(3)),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Name.Should().Be("Black Friday");
        result.Value.Scope.Should().Be(FreezeScope.Global);
        freezeRepo.Received(1).Add(Arg.Any<FreezeWindow>());
        await unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    // ═══════════════ CheckFreezeConflict ═══════════════

    [Fact]
    public async Task CheckFreezeConflict_Should_ReturnConflict_WhenFreezeActive()
    {
        var freezeRepo = Substitute.For<IFreezeWindowRepository>();
        var window = FreezeWindow.Create("Test", "Reason", FreezeScope.Global, null,
            FixedNow, FixedNow.AddDays(3), "admin", FixedNow);

        freezeRepo.ListActiveAtAsync(Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns(new List<FreezeWindow> { window });

        var sut = new CheckFreezeConflictFeature.Handler(freezeRepo);

        var result = await sut.Handle(
            new CheckFreezeConflictFeature.Query(FixedNow.AddHours(1), null),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.HasConflict.Should().BeTrue();
        result.Value.ActiveFreezes.Should().HaveCount(1);
    }

    [Fact]
    public async Task CheckFreezeConflict_Should_ReturnNoConflict_WhenNoFreeze()
    {
        var freezeRepo = Substitute.For<IFreezeWindowRepository>();
        freezeRepo.ListActiveAtAsync(Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns(new List<FreezeWindow>());

        var sut = new CheckFreezeConflictFeature.Handler(freezeRepo);

        var result = await sut.Handle(
            new CheckFreezeConflictFeature.Query(FixedNow, null),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.HasConflict.Should().BeFalse();
    }

    // ═══════════════ RecordReleaseBaseline ═══════════════

    [Fact]
    public async Task RecordReleaseBaseline_Should_Succeed_WhenReleaseExists()
    {
        var release = CreateRelease();
        var releaseRepo = Substitute.For<IReleaseRepository>();
        var baselineRepo = Substitute.For<IReleaseBaselineRepository>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var clock = Substitute.For<IDateTimeProvider>();
        clock.UtcNow.Returns(FixedNow);

        releaseRepo.GetByIdAsync(Arg.Any<ReleaseId>(), Arg.Any<CancellationToken>())
            .Returns(release);

        var sut = new RecordReleaseBaselineFeature.Handler(releaseRepo, baselineRepo, unitOfWork, clock);

        var result = await sut.Handle(
            new RecordReleaseBaselineFeature.Command(
                release.Id.Value, 100m, 0.02m, 45m, 120m, 250m, 5000m,
                FixedNow.AddHours(-1), FixedNow),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        baselineRepo.Received(1).Add(Arg.Any<ReleaseBaseline>());
        await unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RecordReleaseBaseline_Should_Fail_WhenReleaseNotFound()
    {
        var releaseRepo = Substitute.For<IReleaseRepository>();
        var baselineRepo = Substitute.For<IReleaseBaselineRepository>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var clock = Substitute.For<IDateTimeProvider>();

        releaseRepo.GetByIdAsync(Arg.Any<ReleaseId>(), Arg.Any<CancellationToken>())
            .Returns((Release?)null);

        var sut = new RecordReleaseBaselineFeature.Handler(releaseRepo, baselineRepo, unitOfWork, clock);

        var result = await sut.Handle(
            new RecordReleaseBaselineFeature.Command(Guid.NewGuid(), 100m, 0.02m, 45m, 120m, 250m, 5000m,
                FixedNow.AddHours(-1), FixedNow),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
    }

    // ═══════════════ StartPostReleaseReview ═══════════════

    [Fact]
    public async Task StartPostReleaseReview_Should_Succeed_WhenNoExistingReview()
    {
        var release = CreateRelease();
        var releaseRepo = Substitute.For<IReleaseRepository>();
        var reviewRepo = Substitute.For<IPostReleaseReviewRepository>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var clock = Substitute.For<IDateTimeProvider>();
        clock.UtcNow.Returns(FixedNow);

        releaseRepo.GetByIdAsync(Arg.Any<ReleaseId>(), Arg.Any<CancellationToken>())
            .Returns(release);
        reviewRepo.GetByReleaseAsync(Arg.Any<ReleaseId>(), Arg.Any<CancellationToken>())
            .Returns((PostReleaseReview?)null);

        var sut = new StartPostReleaseReviewFeature.Handler(releaseRepo, reviewRepo, unitOfWork, clock);

        var result = await sut.Handle(
            new StartPostReleaseReviewFeature.Command(release.Id.Value),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.CurrentPhase.Should().Be("InitialObservation");
        result.Value.Outcome.Should().Be("Inconclusive");
        reviewRepo.Received(1).Add(Arg.Any<PostReleaseReview>());
    }

    [Fact]
    public async Task StartPostReleaseReview_Should_Fail_WhenReviewAlreadyExists()
    {
        var release = CreateRelease();
        var existing = PostReleaseReview.Start(release.Id, FixedNow);
        var releaseRepo = Substitute.For<IReleaseRepository>();
        var reviewRepo = Substitute.For<IPostReleaseReviewRepository>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var clock = Substitute.For<IDateTimeProvider>();

        releaseRepo.GetByIdAsync(Arg.Any<ReleaseId>(), Arg.Any<CancellationToken>())
            .Returns(release);
        reviewRepo.GetByReleaseAsync(Arg.Any<ReleaseId>(), Arg.Any<CancellationToken>())
            .Returns(existing);

        var sut = new StartPostReleaseReviewFeature.Handler(releaseRepo, reviewRepo, unitOfWork, clock);

        var result = await sut.Handle(
            new StartPostReleaseReviewFeature.Command(release.Id.Value),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task StartPostReleaseReview_Should_Fail_WhenReleaseNotFound()
    {
        var releaseRepo = Substitute.For<IReleaseRepository>();
        var reviewRepo = Substitute.For<IPostReleaseReviewRepository>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var clock = Substitute.For<IDateTimeProvider>();

        releaseRepo.GetByIdAsync(Arg.Any<ReleaseId>(), Arg.Any<CancellationToken>())
            .Returns((Release?)null);

        var sut = new StartPostReleaseReviewFeature.Handler(releaseRepo, reviewRepo, unitOfWork, clock);

        var result = await sut.Handle(
            new StartPostReleaseReviewFeature.Command(Guid.NewGuid()),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
    }

    // ═══════════════ ProgressPostReleaseReview ═══════════════

    [Fact]
    public async Task ProgressPostReleaseReview_Should_AdvancePhase()
    {
        var release = CreateRelease();
        var review = PostReleaseReview.Start(release.Id, FixedNow);
        var reviewRepo = Substitute.For<IPostReleaseReviewRepository>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var clock = Substitute.For<IDateTimeProvider>();
        clock.UtcNow.Returns(FixedNow);

        reviewRepo.GetByReleaseAsync(Arg.Any<ReleaseId>(), Arg.Any<CancellationToken>())
            .Returns(review);

        var sut = new ProgressPostReleaseReviewFeature.Handler(reviewRepo, unitOfWork, clock);

        var result = await sut.Handle(
            new ProgressPostReleaseReviewFeature.Command(
                release.Id.Value, ObservationPhase.PreliminaryReview,
                ReviewOutcome.Neutral, 0.6m, "Metrics look stable"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.CurrentPhase.Should().Be("PreliminaryReview");
        result.Value.Outcome.Should().Be("Neutral");
    }

    [Fact]
    public async Task ProgressPostReleaseReview_Should_Fail_WhenNoReview()
    {
        var reviewRepo = Substitute.For<IPostReleaseReviewRepository>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var clock = Substitute.For<IDateTimeProvider>();

        reviewRepo.GetByReleaseAsync(Arg.Any<ReleaseId>(), Arg.Any<CancellationToken>())
            .Returns((PostReleaseReview?)null);

        var sut = new ProgressPostReleaseReviewFeature.Handler(reviewRepo, unitOfWork, clock);

        var result = await sut.Handle(
            new ProgressPostReleaseReviewFeature.Command(
                Guid.NewGuid(), ObservationPhase.PreliminaryReview,
                ReviewOutcome.Neutral, 0.5m, "Test"),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
    }

    // ═══════════════ AssessRollbackViability ═══════════════

    [Fact]
    public async Task AssessRollbackViability_Should_Succeed_WithViableRollback()
    {
        var release = CreateRelease();
        var releaseRepo = Substitute.For<IReleaseRepository>();
        var rollbackRepo = Substitute.For<IRollbackAssessmentRepository>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var clock = Substitute.For<IDateTimeProvider>();
        clock.UtcNow.Returns(FixedNow);

        releaseRepo.GetByIdAsync(Arg.Any<ReleaseId>(), Arg.Any<CancellationToken>())
            .Returns(release);

        var sut = new AssessRollbackViabilityFeature.Handler(releaseRepo, rollbackRepo, unitOfWork, clock);

        var result = await sut.Handle(
            new AssessRollbackViabilityFeature.Command(
                release.Id.Value, true, "0.9.0", true, 0, 5, null, "Rollback is safe"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.IsViable.Should().BeTrue();
        result.Value.ReadinessScore.Should().BeGreaterThan(0m);
        rollbackRepo.Received(1).Add(Arg.Any<RollbackAssessment>());
    }

    [Fact]
    public async Task AssessRollbackViability_Should_ReturnZeroScore_WhenNotViable()
    {
        var release = CreateRelease();
        var releaseRepo = Substitute.For<IReleaseRepository>();
        var rollbackRepo = Substitute.For<IRollbackAssessmentRepository>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var clock = Substitute.For<IDateTimeProvider>();
        clock.UtcNow.Returns(FixedNow);

        releaseRepo.GetByIdAsync(Arg.Any<ReleaseId>(), Arg.Any<CancellationToken>())
            .Returns(release);

        var sut = new AssessRollbackViabilityFeature.Handler(releaseRepo, rollbackRepo, unitOfWork, clock);

        var result = await sut.Handle(
            new AssessRollbackViabilityFeature.Command(
                release.Id.Value, false, null, false, 0, 0, "No previous version", "Fix forward"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.IsViable.Should().BeFalse();
        result.Value.ReadinessScore.Should().Be(0m);
    }

    [Fact]
    public async Task AssessRollbackViability_Should_Fail_WhenReleaseNotFound()
    {
        var releaseRepo = Substitute.For<IReleaseRepository>();
        var rollbackRepo = Substitute.For<IRollbackAssessmentRepository>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var clock = Substitute.For<IDateTimeProvider>();

        releaseRepo.GetByIdAsync(Arg.Any<ReleaseId>(), Arg.Any<CancellationToken>())
            .Returns((Release?)null);

        var sut = new AssessRollbackViabilityFeature.Handler(releaseRepo, rollbackRepo, unitOfWork, clock);

        var result = await sut.Handle(
            new AssessRollbackViabilityFeature.Command(
                Guid.NewGuid(), true, "0.9.0", true, 0, 0, null, "Safe"),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
    }
}
