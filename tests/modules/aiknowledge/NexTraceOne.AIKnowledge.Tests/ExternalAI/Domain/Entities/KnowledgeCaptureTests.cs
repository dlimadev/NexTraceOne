using NexTraceOne.AIKnowledge.Domain.ExternalAI.Entities;
using NexTraceOne.AIKnowledge.Domain.ExternalAI.Enums;

namespace NexTraceOne.AIKnowledge.Tests.ExternalAI.Domain.Entities;

/// <summary>Testes unitários da entidade KnowledgeCapture — ciclo de revisão e reutilização.</summary>
public sealed class KnowledgeCaptureTests
{
    private static readonly DateTimeOffset FixedNow = new(2025, 6, 1, 10, 0, 0, TimeSpan.Zero);

    private static KnowledgeCapture CreateCapture() =>
        KnowledgeCapture.Capture(
            ExternalAiConsultationId.New(),
            "Breaking change detection for REST field removal",
            "When a required field is removed from a REST API, all active consumers must be notified.",
            "error-resolution",
            "breaking-change,rest,field-removal",
            FixedNow);

    // ── Capture ───────────────────────────────────────────────────────────

    [Fact]
    public void Capture_ShouldInitializeWithPendingStatus()
    {
        var capture = CreateCapture();

        capture.Status.Should().Be(KnowledgeStatus.Pending);
        capture.ReuseCount.Should().Be(0);
        capture.ReviewedBy.Should().BeNull();
        capture.ReviewedAt.Should().BeNull();
    }

    // ── Approve ───────────────────────────────────────────────────────────

    [Fact]
    public void Approve_ShouldTransitionToApproved()
    {
        var capture = CreateCapture();

        var result = capture.Approve("tech-lead@company.com", FixedNow.AddHours(1));

        result.IsSuccess.Should().BeTrue();
        capture.Status.Should().Be(KnowledgeStatus.Approved);
        capture.ReviewedBy.Should().Be("tech-lead@company.com");
    }

    [Fact]
    public void Approve_WhenAlreadyApproved_ShouldFail()
    {
        var capture = CreateCapture();
        capture.Approve("lead@co.com", FixedNow.AddHours(1));

        var result = capture.Approve("another@co.com", FixedNow.AddHours(2));

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("AlreadyReviewed");
    }

    // ── Reject ────────────────────────────────────────────────────────────

    [Fact]
    public void Reject_ShouldTransitionToRejected()
    {
        var capture = CreateCapture();

        var result = capture.Reject("lead@co.com", "Too generic, needs more specific details", FixedNow.AddHours(1));

        result.IsSuccess.Should().BeTrue();
        capture.Status.Should().Be(KnowledgeStatus.Rejected);
        capture.RejectionReason.Should().Contain("generic");
    }

    [Fact]
    public void Reject_WhenAlreadyRejected_ShouldFail()
    {
        var capture = CreateCapture();
        capture.Reject("lead@co.com", "Reason", FixedNow.AddHours(1));

        var result = capture.Reject("another@co.com", "Other reason", FixedNow.AddHours(2));

        result.IsFailure.Should().BeTrue();
    }

    // ── IncrementReuse ────────────────────────────────────────────────────

    [Fact]
    public void IncrementReuse_WhenApproved_ShouldIncrement()
    {
        var capture = CreateCapture();
        capture.Approve("lead@co.com", FixedNow.AddHours(1));

        var result = capture.IncrementReuse();

        result.IsSuccess.Should().BeTrue();
        capture.ReuseCount.Should().Be(1);
    }

    [Fact]
    public void IncrementReuse_WhenPending_ShouldFail()
    {
        var capture = CreateCapture();

        var result = capture.IncrementReuse();

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void IncrementReuse_MultipleTimesWhenApproved_ShouldAccumulate()
    {
        var capture = CreateCapture();
        capture.Approve("lead@co.com", FixedNow.AddHours(1));

        capture.IncrementReuse();
        capture.IncrementReuse();
        capture.IncrementReuse();

        capture.ReuseCount.Should().Be(3);
    }
}
