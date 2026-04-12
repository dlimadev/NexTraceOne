using NexTraceOne.ChangeGovernance.Domain.ChangeIntelligence.Entities;
using NexTraceOne.ChangeGovernance.Domain.ChangeIntelligence.Enums;

namespace NexTraceOne.ChangeGovernance.Tests.ChangeIntelligence.Domain.Entities;

/// <summary>Testes unitários da entidade ChangeConfidenceEvent.</summary>
public sealed class ChangeConfidenceEventTests
{
    private static readonly DateTimeOffset FixedNow = new(2025, 6, 1, 10, 0, 0, TimeSpan.Zero);
    private static readonly ReleaseId TestReleaseId = ReleaseId.New();

    // ── Create com valores válidos ─────────────────────────────────────────

    [Fact]
    public void Create_ShouldReturnEvent_WithValidValues()
    {
        var evt = ChangeConfidenceEvent.Create(
            TestReleaseId,
            ConfidenceEventType.Created,
            50, 60,
            "Initial confidence based on risk score",
            """{"riskScore": 0.3}""",
            "system",
            FixedNow);

        evt.Should().NotBeNull();
        evt.Id.Value.Should().NotBeEmpty();
        evt.ReleaseId.Should().Be(TestReleaseId);
        evt.EventType.Should().Be(ConfidenceEventType.Created);
        evt.ConfidenceBefore.Should().Be(50);
        evt.ConfidenceAfter.Should().Be(60);
        evt.Reason.Should().Be("Initial confidence based on risk score");
        evt.Details.Should().Be("""{"riskScore": 0.3}""");
        evt.Source.Should().Be("system");
        evt.OccurredAt.Should().Be(FixedNow);
    }

    [Fact]
    public void Create_ShouldAllowNullDetails()
    {
        var evt = ChangeConfidenceEvent.Create(
            TestReleaseId,
            ConfidenceEventType.Deployed,
            70, 80,
            "Deploy successful",
            null,
            "ci-pipeline",
            FixedNow);

        evt.Details.Should().BeNull();
    }

    // ── Boundary values (0, 100) ───────────────────────────────────────────

    [Fact]
    public void Create_ShouldAcceptBoundaryScore_Zero()
    {
        var evt = ChangeConfidenceEvent.Create(
            TestReleaseId,
            ConfidenceEventType.RolledBack,
            0, 0,
            "Rollback executed",
            null,
            "operator",
            FixedNow);

        evt.ConfidenceBefore.Should().Be(0);
        evt.ConfidenceAfter.Should().Be(0);
    }

    [Fact]
    public void Create_ShouldAcceptBoundaryScore_Hundred()
    {
        var evt = ChangeConfidenceEvent.Create(
            TestReleaseId,
            ConfidenceEventType.PostDeployValidated,
            100, 100,
            "All validations passed",
            null,
            "system",
            FixedNow);

        evt.ConfidenceBefore.Should().Be(100);
        evt.ConfidenceAfter.Should().Be(100);
    }

    // ── Create com valores inválidos ───────────────────────────────────────

    [Fact]
    public void Create_ShouldThrow_WhenReleaseIdIsNull()
    {
        var act = () => ChangeConfidenceEvent.Create(
            null!, ConfidenceEventType.Created, 50, 60, "reason", null, "src", FixedNow);

        act.Should().Throw<ArgumentNullException>();
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(101)]
    public void Create_ShouldThrow_WhenConfidenceBeforeOutOfRange(int value)
    {
        var act = () => ChangeConfidenceEvent.Create(
            TestReleaseId, ConfidenceEventType.Created, value, 50, "reason", null, "src", FixedNow);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(101)]
    public void Create_ShouldThrow_WhenConfidenceAfterOutOfRange(int value)
    {
        var act = () => ChangeConfidenceEvent.Create(
            TestReleaseId, ConfidenceEventType.Created, 50, value, "reason", null, "src", FixedNow);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_ShouldThrow_WhenReasonIsNullOrWhitespace(string? reason)
    {
        var act = () => ChangeConfidenceEvent.Create(
            TestReleaseId, ConfidenceEventType.Created, 50, 60, reason!, null, "src", FixedNow);

        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_ShouldThrow_WhenSourceIsNullOrWhitespace(string? source)
    {
        var act = () => ChangeConfidenceEvent.Create(
            TestReleaseId, ConfidenceEventType.Created, 50, 60, "reason", null, source!, FixedNow);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_ShouldThrow_WhenReasonExceedsMaxLength()
    {
        var longReason = new string('x', 2001);

        var act = () => ChangeConfidenceEvent.Create(
            TestReleaseId, ConfidenceEventType.Created, 50, 60, longReason, null, "src", FixedNow);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_ShouldThrow_WhenSourceExceedsMaxLength()
    {
        var longSource = new string('x', 501);

        var act = () => ChangeConfidenceEvent.Create(
            TestReleaseId, ConfidenceEventType.Created, 50, 60, "reason", null, longSource, FixedNow);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_ShouldThrow_WhenDetailsExceedsMaxLength()
    {
        var longDetails = new string('x', 8001);

        var act = () => ChangeConfidenceEvent.Create(
            TestReleaseId, ConfidenceEventType.Created, 50, 60, "reason", longDetails, "src", FixedNow);

        act.Should().Throw<ArgumentException>();
    }

    // ── Strongly Typed Id ──────────────────────────────────────────────────

    [Fact]
    public void ChangeConfidenceEventId_New_ShouldGenerateUniqueIds()
    {
        var id1 = ChangeConfidenceEventId.New();
        var id2 = ChangeConfidenceEventId.New();

        id1.Should().NotBe(id2);
    }

    [Fact]
    public void ChangeConfidenceEventId_From_ShouldPreserveGuid()
    {
        var guid = Guid.NewGuid();
        var id = ChangeConfidenceEventId.From(guid);

        id.Value.Should().Be(guid);
    }
}
