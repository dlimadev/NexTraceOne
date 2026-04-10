using NexTraceOne.OperationalIntelligence.Domain.Runtime.Entities;
using NexTraceOne.OperationalIntelligence.Domain.Runtime.Enums;

namespace NexTraceOne.OperationalIntelligence.Tests.Runtime.Domain.Entities;

/// <summary>Testes unitários da entidade PlaybookExecution — ciclo de vida, invariantes e transições.</summary>
public sealed class PlaybookExecutionTests
{
    private static readonly DateTimeOffset FixedNow = new(2026, 3, 1, 10, 0, 0, TimeSpan.Zero);
    private static readonly Guid PlaybookId = Guid.NewGuid();

    // ── Start (valid) ───────────────────────────────────────────────────

    [Fact]
    public void Start_WithValidInputs_ShouldCreateExecution()
    {
        var execution = PlaybookExecution.Start(
            PlaybookId,
            "DB Failover Playbook",
            null,
            "user-1",
            "tenant-1",
            FixedNow);

        execution.PlaybookId.Should().Be(PlaybookId);
        execution.PlaybookName.Should().Be("DB Failover Playbook");
        execution.IncidentId.Should().BeNull();
        execution.ExecutedByUserId.Should().Be("user-1");
        execution.Status.Should().Be(PlaybookExecutionStatus.InProgress);
        execution.StartedAt.Should().Be(FixedNow);
        execution.CompletedAt.Should().BeNull();
        execution.TenantId.Should().Be("tenant-1");
        execution.Id.Value.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public void Start_WithIncidentId_ShouldSetIncidentId()
    {
        var incidentId = Guid.NewGuid();
        var execution = PlaybookExecution.Start(
            PlaybookId,
            "DB Failover Playbook",
            incidentId,
            "user-1",
            "tenant-1",
            FixedNow);

        execution.IncidentId.Should().Be(incidentId);
    }

    // ── Start (validation) ──────────────────────────────────────────────

    [Fact]
    public void Start_WithEmptyPlaybookId_ShouldThrow()
    {
        var act = () => PlaybookExecution.Start(
            Guid.Empty,
            "Test",
            null,
            "user-1",
            "tenant-1",
            FixedNow);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Start_WithEmptyPlaybookName_ShouldThrow()
    {
        var act = () => PlaybookExecution.Start(
            PlaybookId,
            "",
            null,
            "user-1",
            "tenant-1",
            FixedNow);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Start_WithEmptyExecutedByUserId_ShouldThrow()
    {
        var act = () => PlaybookExecution.Start(
            PlaybookId,
            "Test",
            null,
            "",
            "tenant-1",
            FixedNow);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Start_WithEmptyTenantId_ShouldThrow()
    {
        var act = () => PlaybookExecution.Start(
            PlaybookId,
            "Test",
            null,
            "user-1",
            "",
            FixedNow);

        act.Should().Throw<ArgumentException>();
    }

    // ── Complete ─────────────────────────────────────────────────────────

    [Fact]
    public void Complete_FromInProgress_ShouldTransitionToCompleted()
    {
        var execution = CreateInProgressExecution();
        var completedAt = FixedNow.AddHours(1);

        var result = execution.Complete(
            "{\"step1\":\"ok\"}",
            "{\"screenshot\":\"base64...\"}",
            "All steps completed successfully",
            completedAt);

        result.IsSuccess.Should().BeTrue();
        execution.Status.Should().Be(PlaybookExecutionStatus.Completed);
        execution.StepResults.Should().Be("{\"step1\":\"ok\"}");
        execution.Evidence.Should().Be("{\"screenshot\":\"base64...\"}");
        execution.Notes.Should().Be("All steps completed successfully");
        execution.CompletedAt.Should().Be(completedAt);
    }

    [Fact]
    public void Complete_FromCompleted_ShouldReturnError()
    {
        var execution = CreateInProgressExecution();
        execution.Complete(null, null, null, FixedNow.AddHours(1));

        var result = execution.Complete(null, null, null, FixedNow.AddHours(2));

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("InvalidTransition");
    }

    // ── Fail ─────────────────────────────────────────────────────────────

    [Fact]
    public void Fail_FromInProgress_ShouldTransitionToFailed()
    {
        var execution = CreateInProgressExecution();
        var failedAt = FixedNow.AddMinutes(30);

        var result = execution.Fail(
            "{\"step1\":\"ok\",\"step2\":\"error\"}",
            "{\"logs\":\"error trace...\"}",
            "Step 2 failed: connection timeout",
            failedAt);

        result.IsSuccess.Should().BeTrue();
        execution.Status.Should().Be(PlaybookExecutionStatus.Failed);
        execution.StepResults.Should().Be("{\"step1\":\"ok\",\"step2\":\"error\"}");
        execution.Evidence.Should().Be("{\"logs\":\"error trace...\"}");
        execution.Notes.Should().Be("Step 2 failed: connection timeout");
        execution.CompletedAt.Should().Be(failedAt);
    }

    [Fact]
    public void Fail_FromAborted_ShouldReturnError()
    {
        var execution = CreateInProgressExecution();
        execution.Abort("Cancelled", FixedNow.AddMinutes(10));

        var result = execution.Fail(null, null, "Error", FixedNow.AddMinutes(20));

        result.IsFailure.Should().BeTrue();
    }

    // ── Abort ────────────────────────────────────────────────────────────

    [Fact]
    public void Abort_FromInProgress_ShouldTransitionToAborted()
    {
        var execution = CreateInProgressExecution();
        var abortedAt = FixedNow.AddMinutes(15);

        var result = execution.Abort("Operator decided to stop", abortedAt);

        result.IsSuccess.Should().BeTrue();
        execution.Status.Should().Be(PlaybookExecutionStatus.Aborted);
        execution.Notes.Should().Be("Operator decided to stop");
        execution.CompletedAt.Should().Be(abortedAt);
    }

    [Fact]
    public void Abort_FromCompleted_ShouldReturnError()
    {
        var execution = CreateInProgressExecution();
        execution.Complete(null, null, null, FixedNow.AddHours(1));

        var result = execution.Abort("Try to abort", FixedNow.AddHours(2));

        result.IsFailure.Should().BeTrue();
    }

    // ── Helper ────────────────────────────────────────────────────────────

    private static PlaybookExecution CreateInProgressExecution()
        => PlaybookExecution.Start(
            PlaybookId,
            "DB Failover Playbook",
            null,
            "user-1",
            "tenant-1",
            FixedNow);
}
