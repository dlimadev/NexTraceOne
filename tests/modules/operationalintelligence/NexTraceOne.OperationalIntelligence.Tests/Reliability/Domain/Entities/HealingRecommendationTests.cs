using NexTraceOne.OperationalIntelligence.Domain.Reliability.Entities;
using NexTraceOne.OperationalIntelligence.Domain.Reliability.Enums;

namespace NexTraceOne.OperationalIntelligence.Tests.Reliability.Domain.Entities;

/// <summary>Testes unitários da entidade HealingRecommendation — ciclo de vida, invariantes e transições de estado.</summary>
public sealed class HealingRecommendationTests
{
    private static readonly DateTimeOffset FixedNow = new(2026, 3, 1, 10, 0, 0, TimeSpan.Zero);
    private static readonly Guid TenantId = Guid.NewGuid();

    // ── Generate (valid) ──────────────────────────────────────────────────

    [Fact]
    public void Generate_WithValidInputs_ShouldCreateRecommendation()
    {
        var recommendation = CreateRecommendation();

        recommendation.ServiceName.Should().Be("order-service");
        recommendation.Environment.Should().Be("production");
        recommendation.IncidentId.Should().NotBeNull();
        recommendation.RootCauseDescription.Should().Be("Memory leak detected after deploy v2.3.1");
        recommendation.ActionType.Should().Be(HealingActionType.Restart);
        recommendation.ActionDetails.Should().Be("{\"target\":\"pod-order-service-abc\"}");
        recommendation.ConfidenceScore.Should().Be(85);
        recommendation.HistoricalSuccessRate.Should().Be(92.5m);
        recommendation.Status.Should().Be(HealingRecommendationStatus.Proposed);
        recommendation.GeneratedAt.Should().Be(FixedNow);
        recommendation.TenantId.Should().Be(TenantId);
        recommendation.Id.Value.Should().NotBe(Guid.Empty);
    }

    // ── Generate (validation) ─────────────────────────────────────────────

    [Fact]
    public void Generate_WithEmptyServiceName_ShouldThrow()
    {
        var act = () => HealingRecommendation.Generate(
            "", "production", null, "Root cause",
            HealingActionType.Restart, "{}", 50, null, null, null, TenantId, FixedNow);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Generate_WithEmptyEnvironment_ShouldThrow()
    {
        var act = () => HealingRecommendation.Generate(
            "svc", "", null, "Root cause",
            HealingActionType.Restart, "{}", 50, null, null, null, TenantId, FixedNow);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Generate_WithEmptyRootCause_ShouldThrow()
    {
        var act = () => HealingRecommendation.Generate(
            "svc", "production", null, "",
            HealingActionType.Restart, "{}", 50, null, null, null, TenantId, FixedNow);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Generate_WithEmptyActionDetails_ShouldThrow()
    {
        var act = () => HealingRecommendation.Generate(
            "svc", "production", null, "Root cause",
            HealingActionType.Restart, "", 50, null, null, null, TenantId, FixedNow);

        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(101)]
    public void Generate_WithInvalidConfidenceScore_ShouldThrow(int score)
    {
        var act = () => HealingRecommendation.Generate(
            "svc", "production", null, "Root cause",
            HealingActionType.Restart, "{}", score, null, null, null, TenantId, FixedNow);

        act.Should().Throw<ArgumentException>()
            .WithMessage("*Confidence score must be between 0 and 100*");
    }

    [Theory]
    [InlineData(-0.1)]
    [InlineData(100.1)]
    public void Generate_WithInvalidHistoricalSuccessRate_ShouldThrow(double rate)
    {
        var act = () => HealingRecommendation.Generate(
            "svc", "production", null, "Root cause",
            HealingActionType.Restart, "{}", 50, null, null, (decimal)rate, TenantId, FixedNow);

        act.Should().Throw<ArgumentException>()
            .WithMessage("*Historical success rate must be between 0 and 100*");
    }

    // ── Approve ───────────────────────────────────────────────────────────

    [Fact]
    public void Approve_FromProposed_ShouldTransitionToApproved()
    {
        var recommendation = CreateRecommendation();
        var approvedAt = FixedNow.AddMinutes(30);

        var result = recommendation.Approve("user-123", approvedAt);

        result.IsSuccess.Should().BeTrue();
        recommendation.Status.Should().Be(HealingRecommendationStatus.Approved);
        recommendation.ApprovedByUserId.Should().Be("user-123");
        recommendation.ApprovedAt.Should().Be(approvedAt);
    }

    [Fact]
    public void Approve_FromNonProposed_ShouldReturnError()
    {
        var recommendation = CreateRecommendation();
        recommendation.Approve("user-123", FixedNow.AddMinutes(10));

        var result = recommendation.Approve("user-456", FixedNow.AddMinutes(20));

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("InvalidTransition");
    }

    // ── Reject ────────────────────────────────────────────────────────────

    [Fact]
    public void Reject_FromProposed_ShouldTransitionToRejected()
    {
        var recommendation = CreateRecommendation();
        var rejectedAt = FixedNow.AddMinutes(30);

        var result = recommendation.Reject("user-123", rejectedAt);

        result.IsSuccess.Should().BeTrue();
        recommendation.Status.Should().Be(HealingRecommendationStatus.Rejected);
        recommendation.ApprovedByUserId.Should().Be("user-123");
        recommendation.ApprovedAt.Should().Be(rejectedAt);
    }

    [Fact]
    public void Reject_FromNonProposed_ShouldReturnError()
    {
        var recommendation = CreateRecommendation();
        recommendation.Approve("user-123", FixedNow.AddMinutes(10));

        var result = recommendation.Reject("user-456", FixedNow.AddMinutes(20));

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("InvalidTransition");
    }

    // ── StartExecution ────────────────────────────────────────────────────

    [Fact]
    public void StartExecution_FromApproved_ShouldTransitionToExecuting()
    {
        var recommendation = CreateApprovedRecommendation();
        var startedAt = FixedNow.AddHours(1);

        var result = recommendation.StartExecution(startedAt);

        result.IsSuccess.Should().BeTrue();
        recommendation.Status.Should().Be(HealingRecommendationStatus.Executing);
        recommendation.ExecutionStartedAt.Should().Be(startedAt);
    }

    [Fact]
    public void StartExecution_FromProposed_ShouldReturnError()
    {
        var recommendation = CreateRecommendation();

        var result = recommendation.StartExecution(FixedNow.AddHours(1));

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("InvalidTransition");
    }

    // ── CompleteExecution ──────────────────────────────────────────────────

    [Fact]
    public void CompleteExecution_FromExecuting_ShouldTransitionToCompleted()
    {
        var recommendation = CreateExecutingRecommendation();
        var completedAt = FixedNow.AddHours(2);

        var result = recommendation.CompleteExecution(
            "{\"restarted\":true}", "{\"logs\":[\"Pod restarted successfully\"]}", completedAt);

        result.IsSuccess.Should().BeTrue();
        recommendation.Status.Should().Be(HealingRecommendationStatus.Completed);
        recommendation.ExecutionResult.Should().Be("{\"restarted\":true}");
        recommendation.EvidenceTrail.Should().Be("{\"logs\":[\"Pod restarted successfully\"]}");
        recommendation.ExecutionCompletedAt.Should().Be(completedAt);
    }

    [Fact]
    public void CompleteExecution_FromApproved_ShouldReturnError()
    {
        var recommendation = CreateApprovedRecommendation();

        var result = recommendation.CompleteExecution("{}", "{}", FixedNow.AddHours(2));

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("InvalidTransition");
    }

    // ── FailExecution ─────────────────────────────────────────────────────

    [Fact]
    public void FailExecution_FromExecuting_ShouldTransitionToFailed()
    {
        var recommendation = CreateExecutingRecommendation();
        var completedAt = FixedNow.AddHours(2);

        var result = recommendation.FailExecution("Timeout waiting for pod restart", completedAt);

        result.IsSuccess.Should().BeTrue();
        recommendation.Status.Should().Be(HealingRecommendationStatus.Failed);
        recommendation.ErrorMessage.Should().Be("Timeout waiting for pod restart");
        recommendation.ExecutionCompletedAt.Should().Be(completedAt);
    }

    [Fact]
    public void FailExecution_FromProposed_ShouldReturnError()
    {
        var recommendation = CreateRecommendation();

        var result = recommendation.FailExecution("Error", FixedNow.AddHours(2));

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("InvalidTransition");
    }

    // ── Full lifecycle ────────────────────────────────────────────────────

    [Fact]
    public void FullLifecycle_Proposed_Approved_Executing_Completed()
    {
        var recommendation = CreateRecommendation();

        recommendation.Approve("user-1", FixedNow.AddMinutes(10)).IsSuccess.Should().BeTrue();
        recommendation.StartExecution(FixedNow.AddMinutes(20)).IsSuccess.Should().BeTrue();
        recommendation.CompleteExecution("{}", "{}", FixedNow.AddMinutes(30)).IsSuccess.Should().BeTrue();

        recommendation.Status.Should().Be(HealingRecommendationStatus.Completed);
    }

    [Fact]
    public void FullLifecycle_Proposed_Approved_Executing_Failed()
    {
        var recommendation = CreateRecommendation();

        recommendation.Approve("user-1", FixedNow.AddMinutes(10)).IsSuccess.Should().BeTrue();
        recommendation.StartExecution(FixedNow.AddMinutes(20)).IsSuccess.Should().BeTrue();
        recommendation.FailExecution("Connection refused", FixedNow.AddMinutes(30)).IsSuccess.Should().BeTrue();

        recommendation.Status.Should().Be(HealingRecommendationStatus.Failed);
    }

    // ── Helpers ────────────────────────────────────────────────────────────

    private static HealingRecommendation CreateRecommendation()
        => HealingRecommendation.Generate(
            "order-service",
            "production",
            Guid.NewGuid(),
            "Memory leak detected after deploy v2.3.1",
            HealingActionType.Restart,
            "{\"target\":\"pod-order-service-abc\"}",
            85,
            "{\"downtime\":\"~30s\"}",
            "[\"runbook-001\",\"runbook-002\"]",
            92.5m,
            TenantId,
            FixedNow);

    private static HealingRecommendation CreateApprovedRecommendation()
    {
        var r = CreateRecommendation();
        r.Approve("user-123", FixedNow.AddMinutes(10));
        return r;
    }

    private static HealingRecommendation CreateExecutingRecommendation()
    {
        var r = CreateApprovedRecommendation();
        r.StartExecution(FixedNow.AddMinutes(20));
        return r;
    }
}
