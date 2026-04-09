using NexTraceOne.OperationalIntelligence.Domain.Runtime.Entities;
using NexTraceOne.OperationalIntelligence.Domain.Runtime.Enums;

namespace NexTraceOne.OperationalIntelligence.Tests.Runtime.Domain.Entities;

/// <summary>Testes unitários da entidade OperationalPlaybook — ciclo de vida, invariantes e transições.</summary>
public sealed class OperationalPlaybookTests
{
    private static readonly DateTimeOffset FixedNow = new(2026, 3, 1, 10, 0, 0, TimeSpan.Zero);

    // ── Create (valid) ──────────────────────────────────────────────────

    [Fact]
    public void Create_WithValidInputs_ShouldCreatePlaybook()
    {
        var playbook = OperationalPlaybook.Create(
            "Incident Response - DB Failover",
            "Steps for database failover procedure",
            "[{\"step\":1,\"action\":\"Verify replica status\"}]",
            "[\"svc-1\",\"svc-2\"]",
            "[\"rb-1\"]",
            "[\"database\",\"failover\"]",
            "tenant-1",
            FixedNow,
            "user-1");

        playbook.Name.Should().Be("Incident Response - DB Failover");
        playbook.Description.Should().Be("Steps for database failover procedure");
        playbook.Version.Should().Be(1);
        playbook.Status.Should().Be(PlaybookStatus.Draft);
        playbook.ExecutionCount.Should().Be(0);
        playbook.TenantId.Should().Be("tenant-1");
        playbook.Id.Value.Should().NotBe(Guid.Empty);
        playbook.ApprovedByUserId.Should().BeNull();
        playbook.ApprovedAt.Should().BeNull();
        playbook.DeprecatedAt.Should().BeNull();
        playbook.LastExecutedAt.Should().BeNull();
    }

    // ── Create (validation) ─────────────────────────────────────────────

    [Fact]
    public void Create_WithEmptyName_ShouldThrow()
    {
        var act = () => OperationalPlaybook.Create(
            "", null, "[{}]", null, null, null, "tenant-1", FixedNow, "user-1");

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_WithEmptySteps_ShouldThrow()
    {
        var act = () => OperationalPlaybook.Create(
            "Test Playbook", null, "", null, null, null, "tenant-1", FixedNow, "user-1");

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_WithEmptyTenantId_ShouldThrow()
    {
        var act = () => OperationalPlaybook.Create(
            "Test Playbook", null, "[{}]", null, null, null, "", FixedNow, "user-1");

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_WithTooLongName_ShouldThrow()
    {
        var longName = new string('A', 201);
        var act = () => OperationalPlaybook.Create(
            longName, null, "[{}]", null, null, null, "tenant-1", FixedNow, "user-1");

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_WithTooLongDescription_ShouldThrow()
    {
        var longDesc = new string('D', 2001);
        var act = () => OperationalPlaybook.Create(
            "Test Playbook", longDesc, "[{}]", null, null, null, "tenant-1", FixedNow, "user-1");

        act.Should().Throw<ArgumentException>()
            .WithMessage("*Description must not exceed 2000 characters*");
    }

    // ── Activate ────────────────────────────────────────────────────────

    [Fact]
    public void Activate_FromDraft_ShouldTransitionToActive()
    {
        var playbook = CreateDraftPlaybook();
        var approvedAt = FixedNow.AddHours(1);

        var result = playbook.Activate("approver-1", approvedAt);

        result.IsSuccess.Should().BeTrue();
        playbook.Status.Should().Be(PlaybookStatus.Active);
        playbook.ApprovedByUserId.Should().Be("approver-1");
        playbook.ApprovedAt.Should().Be(approvedAt);
    }

    [Fact]
    public void Activate_FromActive_ShouldReturnError()
    {
        var playbook = CreateDraftPlaybook();
        playbook.Activate("approver-1", FixedNow.AddHours(1));

        var result = playbook.Activate("approver-2", FixedNow.AddHours(2));

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("InvalidTransition");
    }

    [Fact]
    public void Activate_FromDeprecated_ShouldReturnError()
    {
        var playbook = CreateDraftPlaybook();
        playbook.Activate("approver-1", FixedNow.AddHours(1));
        playbook.Deprecate(FixedNow.AddHours(2));

        var result = playbook.Activate("approver-2", FixedNow.AddHours(3));

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("InvalidTransition");
    }

    // ── Deprecate ───────────────────────────────────────────────────────

    [Fact]
    public void Deprecate_FromActive_ShouldTransitionToDeprecated()
    {
        var playbook = CreateDraftPlaybook();
        playbook.Activate("approver-1", FixedNow.AddHours(1));
        var deprecatedAt = FixedNow.AddDays(30);

        var result = playbook.Deprecate(deprecatedAt);

        result.IsSuccess.Should().BeTrue();
        playbook.Status.Should().Be(PlaybookStatus.Deprecated);
        playbook.DeprecatedAt.Should().Be(deprecatedAt);
    }

    [Fact]
    public void Deprecate_FromDraft_ShouldReturnError()
    {
        var playbook = CreateDraftPlaybook();

        var result = playbook.Deprecate(FixedNow.AddHours(1));

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("InvalidTransition");
    }

    // ── UpdateSteps ─────────────────────────────────────────────────────

    [Fact]
    public void UpdateSteps_InDraft_ShouldUpdateAndIncrementVersion()
    {
        var playbook = CreateDraftPlaybook();
        var newSteps = "[{\"step\":1,\"action\":\"Updated action\"}]";

        var result = playbook.UpdateSteps(newSteps, FixedNow.AddHours(1));

        result.IsSuccess.Should().BeTrue();
        playbook.Steps.Should().Be(newSteps);
        playbook.Version.Should().Be(2);
    }

    [Fact]
    public void UpdateSteps_InActive_ShouldReturnError()
    {
        var playbook = CreateDraftPlaybook();
        playbook.Activate("approver-1", FixedNow.AddHours(1));

        var result = playbook.UpdateSteps("[{\"new\":true}]", FixedNow.AddHours(2));

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("InvalidTransition");
    }

    [Fact]
    public void UpdateSteps_WithEmptySteps_ShouldThrow()
    {
        var playbook = CreateDraftPlaybook();

        var act = () => playbook.UpdateSteps("", FixedNow.AddHours(1));

        act.Should().Throw<ArgumentException>();
    }

    // ── IncrementExecutionCount ─────────────────────────────────────────

    [Fact]
    public void IncrementExecutionCount_ShouldIncrementAndSetLastExecutedAt()
    {
        var playbook = CreateDraftPlaybook();
        playbook.Activate("approver-1", FixedNow.AddHours(1));
        var executedAt = FixedNow.AddHours(2);

        playbook.IncrementExecutionCount(executedAt);

        playbook.ExecutionCount.Should().Be(1);
        playbook.LastExecutedAt.Should().Be(executedAt);
    }

    [Fact]
    public void IncrementExecutionCount_MultipleTimes_ShouldAccumulate()
    {
        var playbook = CreateDraftPlaybook();
        playbook.Activate("approver-1", FixedNow.AddHours(1));

        playbook.IncrementExecutionCount(FixedNow.AddHours(2));
        playbook.IncrementExecutionCount(FixedNow.AddHours(3));
        playbook.IncrementExecutionCount(FixedNow.AddHours(4));

        playbook.ExecutionCount.Should().Be(3);
        playbook.LastExecutedAt.Should().Be(FixedNow.AddHours(4));
    }

    // ── Helper ────────────────────────────────────────────────────────────

    private static OperationalPlaybook CreateDraftPlaybook()
        => OperationalPlaybook.Create(
            "DB Failover Playbook",
            "Standard failover procedure",
            "[{\"step\":1,\"action\":\"Check status\"}]",
            "[\"svc-1\"]",
            "[\"rb-1\"]",
            "[\"database\"]",
            "tenant-1",
            FixedNow,
            "user-1");
}
