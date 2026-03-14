using NexTraceOne.ChangeIntelligence.Domain.Entities;
using NexTraceOne.ChangeIntelligence.Domain.Enums;

namespace NexTraceOne.ChangeIntelligence.Tests.Domain.Entities;

/// <summary>
/// Testes de domínio para as entidades de inteligência de mudança
/// adicionadas ao Change Intelligence Orchestrator.
/// </summary>
public sealed class ChangeIntelligenceDomainExtendedTests
{
    private static readonly DateTimeOffset FixedNow = new(2025, 6, 1, 10, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset FixedLater = FixedNow.AddHours(4);

    private static ReleaseId NewReleaseId() => ReleaseId.From(Guid.NewGuid());

    // ═══════════════ ExternalMarker ═══════════════

    [Fact]
    public void ExternalMarker_Create_ShouldSetAllProperties()
    {
        var releaseId = NewReleaseId();
        var marker = ExternalMarker.Create(
            releaseId, MarkerType.DeploymentStarted, "GitHub", "run-123", "{}", FixedNow, FixedLater);

        marker.ReleaseId.Should().Be(releaseId);
        marker.MarkerType.Should().Be(MarkerType.DeploymentStarted);
        marker.SourceSystem.Should().Be("GitHub");
        marker.ExternalId.Should().Be("run-123");
        marker.Payload.Should().Be("{}");
        marker.OccurredAt.Should().Be(FixedNow);
        marker.ReceivedAt.Should().Be(FixedLater);
        marker.Id.Value.Should().NotBeEmpty();
    }

    [Fact]
    public void ExternalMarker_Create_ShouldRejectNullReleaseId()
    {
        var act = () => ExternalMarker.Create(null!, MarkerType.BuildCompleted, "Jenkins", "b-1", null, FixedNow, FixedNow);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void ExternalMarker_Create_ShouldRejectEmptySourceSystem()
    {
        var act = () => ExternalMarker.Create(NewReleaseId(), MarkerType.BuildCompleted, "", "b-1", null, FixedNow, FixedNow);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void ExternalMarker_Create_ShouldRejectEmptyExternalId()
    {
        var act = () => ExternalMarker.Create(NewReleaseId(), MarkerType.BuildCompleted, "GitHub", "", null, FixedNow, FixedNow);
        act.Should().Throw<ArgumentException>();
    }

    // ═══════════════ FreezeWindow ═══════════════

    [Fact]
    public void FreezeWindow_Create_ShouldSetAllProperties()
    {
        var window = FreezeWindow.Create("Black Friday", "Peak sales period", FreezeScope.Global, null,
            FixedNow, FixedLater, "admin", FixedNow);

        window.Name.Should().Be("Black Friday");
        window.Reason.Should().Be("Peak sales period");
        window.Scope.Should().Be(FreezeScope.Global);
        window.IsActive.Should().BeTrue();
        window.StartsAt.Should().Be(FixedNow);
        window.EndsAt.Should().Be(FixedLater);
    }

    [Fact]
    public void FreezeWindow_Create_ShouldRejectEndBeforeStart()
    {
        var act = () => FreezeWindow.Create("Test", "Reason", FreezeScope.Global, null,
            FixedLater, FixedNow, "admin", FixedNow);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void FreezeWindow_Create_ShouldRequireScopeValueForNonGlobal()
    {
        var act = () => FreezeWindow.Create("Test", "Reason", FreezeScope.Tenant, null,
            FixedNow, FixedLater, "admin", FixedNow);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void FreezeWindow_Create_GlobalScope_ShouldAllowNullScopeValue()
    {
        var window = FreezeWindow.Create("Test", "Reason", FreezeScope.Global, null,
            FixedNow, FixedLater, "admin", FixedNow);
        window.ScopeValue.Should().BeNull();
    }

    [Fact]
    public void FreezeWindow_IsInEffectAt_ShouldReturnTrueWhenWithinWindow()
    {
        var window = FreezeWindow.Create("Test", "Reason", FreezeScope.Global, null,
            FixedNow, FixedLater, "admin", FixedNow);
        window.IsInEffectAt(FixedNow.AddHours(2)).Should().BeTrue();
    }

    [Fact]
    public void FreezeWindow_IsInEffectAt_ShouldReturnFalseWhenOutsideWindow()
    {
        var window = FreezeWindow.Create("Test", "Reason", FreezeScope.Global, null,
            FixedNow, FixedLater, "admin", FixedNow);
        window.IsInEffectAt(FixedLater.AddHours(1)).Should().BeFalse();
    }

    [Fact]
    public void FreezeWindow_Deactivate_ShouldSucceed()
    {
        var window = FreezeWindow.Create("Test", "Reason", FreezeScope.Global, null,
            FixedNow, FixedLater, "admin", FixedNow);
        var result = window.Deactivate();
        result.IsSuccess.Should().BeTrue();
        window.IsActive.Should().BeFalse();
    }

    [Fact]
    public void FreezeWindow_Deactivate_ShouldFailWhenAlreadyInactive()
    {
        var window = FreezeWindow.Create("Test", "Reason", FreezeScope.Global, null,
            FixedNow, FixedLater, "admin", FixedNow);
        window.Deactivate();
        var result = window.Deactivate();
        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void FreezeWindow_IsInEffectAt_ShouldReturnFalseWhenDeactivated()
    {
        var window = FreezeWindow.Create("Test", "Reason", FreezeScope.Global, null,
            FixedNow, FixedLater, "admin", FixedNow);
        window.Deactivate();
        window.IsInEffectAt(FixedNow.AddHours(2)).Should().BeFalse();
    }

    // ═══════════════ ReleaseBaseline ═══════════════

    [Fact]
    public void ReleaseBaseline_Create_ShouldSetAllMetrics()
    {
        var releaseId = NewReleaseId();
        var baseline = ReleaseBaseline.Create(releaseId, 100m, 0.02m, 45m, 120m, 250m, 5000m,
            FixedNow, FixedLater, FixedLater);

        baseline.ReleaseId.Should().Be(releaseId);
        baseline.RequestsPerMinute.Should().Be(100m);
        baseline.ErrorRate.Should().Be(0.02m);
        baseline.AvgLatencyMs.Should().Be(45m);
        baseline.P95LatencyMs.Should().Be(120m);
        baseline.P99LatencyMs.Should().Be(250m);
        baseline.Throughput.Should().Be(5000m);
    }

    [Fact]
    public void ReleaseBaseline_Create_ShouldRejectNullReleaseId()
    {
        var act = () => ReleaseBaseline.Create(null!, 100m, 0.02m, 45m, 120m, 250m, 5000m,
            FixedNow, FixedLater, FixedLater);
        act.Should().Throw<ArgumentNullException>();
    }

    // ═══════════════ ObservationWindow ═══════════════

    [Fact]
    public void ObservationWindow_Create_ShouldSetPhaseAndDates()
    {
        var releaseId = NewReleaseId();
        var window = ObservationWindow.Create(releaseId, ObservationPhase.InitialObservation,
            FixedNow, FixedLater);

        window.ReleaseId.Should().Be(releaseId);
        window.Phase.Should().Be(ObservationPhase.InitialObservation);
        window.StartsAt.Should().Be(FixedNow);
        window.EndsAt.Should().Be(FixedLater);
        window.IsCollected.Should().BeFalse();
    }

    [Fact]
    public void ObservationWindow_Create_ShouldRejectEndBeforeStart()
    {
        var act = () => ObservationWindow.Create(NewReleaseId(), ObservationPhase.InitialObservation,
            FixedLater, FixedNow);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void ObservationWindow_RecordMetrics_ShouldSucceed()
    {
        var window = ObservationWindow.Create(NewReleaseId(), ObservationPhase.InitialObservation,
            FixedNow, FixedLater);
        var result = window.RecordMetrics(150m, 0.01m, 30m, 80m, 150m, 6000m, FixedLater);

        result.IsSuccess.Should().BeTrue();
        window.IsCollected.Should().BeTrue();
        window.RequestsPerMinute.Should().Be(150m);
        window.ErrorRate.Should().Be(0.01m);
    }

    [Fact]
    public void ObservationWindow_RecordMetrics_ShouldFailWhenAlreadyCollected()
    {
        var window = ObservationWindow.Create(NewReleaseId(), ObservationPhase.InitialObservation,
            FixedNow, FixedLater);
        window.RecordMetrics(150m, 0.01m, 30m, 80m, 150m, 6000m, FixedLater);
        var result = window.RecordMetrics(200m, 0.02m, 40m, 90m, 160m, 7000m, FixedLater);

        result.IsFailure.Should().BeTrue();
    }

    // ═══════════════ PostReleaseReview ═══════════════

    [Fact]
    public void PostReleaseReview_Start_ShouldBeInconclusiveInitially()
    {
        var review = PostReleaseReview.Start(NewReleaseId(), FixedNow);

        review.CurrentPhase.Should().Be(ObservationPhase.InitialObservation);
        review.Outcome.Should().Be(ReviewOutcome.Inconclusive);
        review.ConfidenceScore.Should().Be(0m);
        review.IsCompleted.Should().BeFalse();
    }

    [Fact]
    public void PostReleaseReview_Progress_ShouldAdvancePhase()
    {
        var review = PostReleaseReview.Start(NewReleaseId(), FixedNow);
        var result = review.Progress(ObservationPhase.PreliminaryReview,
            ReviewOutcome.Neutral, 0.5m, "Preliminary review shows stable metrics.");

        result.IsSuccess.Should().BeTrue();
        review.CurrentPhase.Should().Be(ObservationPhase.PreliminaryReview);
        review.Outcome.Should().Be(ReviewOutcome.Neutral);
        review.ConfidenceScore.Should().Be(0.5m);
    }

    [Fact]
    public void PostReleaseReview_Progress_ShouldCompleteAtFinalReview()
    {
        var review = PostReleaseReview.Start(NewReleaseId(), FixedNow);
        review.Progress(ObservationPhase.PreliminaryReview, ReviewOutcome.Neutral, 0.5m, "Stable");
        review.Progress(ObservationPhase.ConsolidatedReview, ReviewOutcome.Positive, 0.75m, "Improving");
        var result = review.Progress(ObservationPhase.FinalReview, ReviewOutcome.Positive, 0.85m, "Confirmed positive", FixedLater);

        result.IsSuccess.Should().BeTrue();
        review.IsCompleted.Should().BeTrue();
        review.CompletedAt.Should().Be(FixedLater);
    }

    [Fact]
    public void PostReleaseReview_Progress_ShouldCompleteWhenHighConfidence()
    {
        var review = PostReleaseReview.Start(NewReleaseId(), FixedNow);
        var result = review.Progress(ObservationPhase.PreliminaryReview,
            ReviewOutcome.Positive, 0.95m, "Very high confidence early", FixedLater);

        result.IsSuccess.Should().BeTrue();
        review.IsCompleted.Should().BeTrue();
    }

    [Fact]
    public void PostReleaseReview_Progress_ShouldRejectBackwardPhase()
    {
        var review = PostReleaseReview.Start(NewReleaseId(), FixedNow);
        review.Progress(ObservationPhase.PreliminaryReview, ReviewOutcome.Neutral, 0.5m, "Stable");
        var result = review.Progress(ObservationPhase.InitialObservation,
            ReviewOutcome.Neutral, 0.3m, "Backward");

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void PostReleaseReview_Progress_ShouldRejectInvalidConfidence()
    {
        var review = PostReleaseReview.Start(NewReleaseId(), FixedNow);
        var result = review.Progress(ObservationPhase.PreliminaryReview,
            ReviewOutcome.Neutral, 1.5m, "Too high");

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void PostReleaseReview_Progress_ShouldFailWhenCompleted()
    {
        var review = PostReleaseReview.Start(NewReleaseId(), FixedNow);
        review.Progress(ObservationPhase.FinalReview, ReviewOutcome.Positive, 0.85m, "Done", FixedLater);
        var result = review.Progress(ObservationPhase.FinalReview,
            ReviewOutcome.Positive, 0.99m, "Again");

        result.IsFailure.Should().BeTrue();
    }

    // ═══════════════ RollbackAssessment ═══════════════

    [Fact]
    public void RollbackAssessment_Create_ShouldSetAllProperties()
    {
        var releaseId = NewReleaseId();
        var assessment = RollbackAssessment.Create(releaseId, true, 0.8m, "1.0.0",
            true, 0, 5, null, "Rollback is safe", FixedNow);

        assessment.ReleaseId.Should().Be(releaseId);
        assessment.IsViable.Should().BeTrue();
        assessment.ReadinessScore.Should().Be(0.8m);
        assessment.PreviousVersion.Should().Be("1.0.0");
        assessment.HasReversibleMigrations.Should().BeTrue();
        assessment.ConsumersAlreadyMigrated.Should().Be(0);
        assessment.TotalConsumersImpacted.Should().Be(5);
        assessment.Recommendation.Should().Be("Rollback is safe");
    }

    [Fact]
    public void RollbackAssessment_Create_ShouldClampScoreToMax()
    {
        var assessment = RollbackAssessment.Create(NewReleaseId(), true, 1.5m, "1.0.0",
            true, 0, 0, null, "Safe", FixedNow);
        assessment.ReadinessScore.Should().Be(1m);
    }

    [Fact]
    public void RollbackAssessment_Create_ShouldClampScoreToMin()
    {
        var assessment = RollbackAssessment.Create(NewReleaseId(), false, -0.5m, null,
            false, 0, 0, "No previous version", "Not viable", FixedNow);
        assessment.ReadinessScore.Should().Be(0m);
    }

    [Fact]
    public void RollbackAssessment_Create_ShouldRejectEmptyRecommendation()
    {
        var act = () => RollbackAssessment.Create(NewReleaseId(), true, 0.5m, "1.0.0",
            true, 0, 0, null, "", FixedNow);
        act.Should().Throw<ArgumentException>();
    }
}
