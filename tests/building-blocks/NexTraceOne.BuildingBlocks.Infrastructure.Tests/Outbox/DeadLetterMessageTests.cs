using NexTraceOne.BuildingBlocks.Infrastructure.Outbox;

namespace NexTraceOne.BuildingBlocks.Infrastructure.Tests.Outbox;

/// <summary>
/// Testes unitários para DeadLetterMessage — entity, factory e métodos de estado.
/// Cobre: criação via From(), transições de estado, imutabilidade dos campos init-only,
/// validação de conteúdo e casos-limite.
/// </summary>
public sealed class DeadLetterMessageTests
{
    private static readonly DateTimeOffset FixedNow = new(2026, 04, 23, 10, 0, 0, TimeSpan.Zero);

    // ── From() factory ────────────────────────────────────────────────────────

    [Fact]
    public void From_ShouldCopyTenantIdFromOutboxMessage()
    {
        var outbox = BuildExhaustedOutboxMessage(tenantId: Guid.Parse("aaaaaaaa-0000-0000-0000-000000000001"));
        var dlq = DeadLetterMessage.From(outbox, new Exception("boom"), FixedNow);
        dlq.TenantId.Should().Be(outbox.TenantId);
    }

    [Fact]
    public void From_ShouldCopyMessageTypeFromOutboxMessage()
    {
        var outbox = BuildExhaustedOutboxMessage(eventType: "MyModule.Events.OrderPlaced");
        var dlq = DeadLetterMessage.From(outbox, new Exception("boom"), FixedNow);
        dlq.MessageType.Should().Be("MyModule.Events.OrderPlaced");
    }

    [Fact]
    public void From_ShouldCopyPayloadFromOutboxMessage()
    {
        var outbox = BuildExhaustedOutboxMessage(payload: """{"orderId":"123"}""");
        var dlq = DeadLetterMessage.From(outbox, new Exception("boom"), FixedNow);
        dlq.Payload.Should().Be("""{"orderId":"123"}""");
    }

    [Fact]
    public void From_ShouldCopyRetryCountAsAttemptCount()
    {
        var outbox = BuildExhaustedOutboxMessage(retryCount: 5);
        var dlq = DeadLetterMessage.From(outbox, new Exception("boom"), FixedNow);
        dlq.AttemptCount.Should().Be(5);
    }

    [Fact]
    public void From_ShouldSetExhaustedAtFromProvidedTimestamp()
    {
        var outbox = BuildExhaustedOutboxMessage();
        var dlq = DeadLetterMessage.From(outbox, new Exception("boom"), FixedNow);
        dlq.ExhaustedAt.Should().Be(FixedNow);
    }

    [Fact]
    public void From_ShouldSetLastExceptionFromExceptionMessage()
    {
        var outbox = BuildExhaustedOutboxMessage();
        var dlq = DeadLetterMessage.From(outbox, new InvalidOperationException("handler crashed"), FixedNow);
        dlq.LastException.Should().Be("handler crashed");
    }

    [Fact]
    public void From_ShouldUseLastErrorAsFailureReason_WhenPresent()
    {
        var outbox = BuildExhaustedOutboxMessage(lastError: "Processing failed at attempt 5.");
        var dlq = DeadLetterMessage.From(outbox, new Exception("boom"), FixedNow);
        dlq.FailureReason.Should().Be("Processing failed at attempt 5.");
    }

    [Fact]
    public void From_ShouldUseFallbackFailureReason_WhenLastErrorIsNull()
    {
        var outbox = BuildExhaustedOutboxMessage(lastError: null);
        var dlq = DeadLetterMessage.From(outbox, new Exception("boom"), FixedNow);
        dlq.FailureReason.Should().Contain("Exhausted");
    }

    [Fact]
    public void From_ShouldGenerateNewUniqueIdOnEachCall()
    {
        var outbox = BuildExhaustedOutboxMessage();
        var dlq1 = DeadLetterMessage.From(outbox, new Exception("e"), FixedNow);
        var dlq2 = DeadLetterMessage.From(outbox, new Exception("e"), FixedNow);
        dlq1.Id.Should().NotBe(dlq2.Id);
    }

    [Fact]
    public void From_ShouldSetStatusToPending()
    {
        var outbox = BuildExhaustedOutboxMessage();
        var dlq = DeadLetterMessage.From(outbox, new Exception("boom"), FixedNow);
        dlq.Status.Should().Be(DlqMessageStatus.Pending);
    }

    [Fact]
    public void From_ShouldLeaveReprocessedAtNull()
    {
        var outbox = BuildExhaustedOutboxMessage();
        var dlq = DeadLetterMessage.From(outbox, new Exception("boom"), FixedNow);
        dlq.ReprocessedAt.Should().BeNull();
    }

    // ── MarkReprocessing() ────────────────────────────────────────────────────

    [Fact]
    public void MarkReprocessing_ShouldSetStatusToReprocessing()
    {
        var dlq = BuildPendingDlqMessage();
        dlq.MarkReprocessing(FixedNow);
        dlq.Status.Should().Be(DlqMessageStatus.Reprocessing);
    }

    [Fact]
    public void MarkReprocessing_ShouldRecordReprocessedAt()
    {
        var dlq = BuildPendingDlqMessage();
        dlq.MarkReprocessing(FixedNow);
        dlq.ReprocessedAt.Should().Be(FixedNow);
    }

    // ── MarkResolved() ────────────────────────────────────────────────────────

    [Fact]
    public void MarkResolved_ShouldSetStatusToResolved()
    {
        var dlq = BuildPendingDlqMessage();
        dlq.MarkResolved();
        dlq.Status.Should().Be(DlqMessageStatus.Resolved);
    }

    // ── MarkDiscarded() ───────────────────────────────────────────────────────

    [Fact]
    public void MarkDiscarded_ShouldSetStatusToDiscarded()
    {
        var dlq = BuildPendingDlqMessage();
        dlq.MarkDiscarded("No handler available.");
        dlq.Status.Should().Be(DlqMessageStatus.Discarded);
    }

    [Fact]
    public void MarkDiscarded_ShouldOverwriteFailureReason()
    {
        var dlq = BuildPendingDlqMessage();
        dlq.MarkDiscarded("Manually discarded by admin.");
        dlq.FailureReason.Should().Be("Manually discarded by admin.");
    }

    // ── DlqMessageStatus enum ─────────────────────────────────────────────────

    [Fact]
    public void DlqMessageStatus_ShouldHaveFourValues()
    {
        var values = Enum.GetValues<DlqMessageStatus>();
        values.Should().HaveCount(4);
        values.Should().Contain([
            DlqMessageStatus.Pending,
            DlqMessageStatus.Reprocessing,
            DlqMessageStatus.Resolved,
            DlqMessageStatus.Discarded
        ]);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static OutboxMessage BuildExhaustedOutboxMessage(
        Guid? tenantId = null,
        string eventType = "MyModule.Events.SomethingHappened",
        string payload = """{"data":"value"}""",
        int retryCount = 5,
        string? lastError = "Processing failed at attempt 5.")
    {
        var msg = new OutboxMessage
        {
            TenantId = tenantId ?? Guid.NewGuid(),
            EventType = eventType,
            Payload = payload,
            CreatedAt = FixedNow.AddMinutes(-10),
            RetryCount = retryCount,
            LastError = lastError
        };
        return msg;
    }

    private static DeadLetterMessage BuildPendingDlqMessage() =>
        DeadLetterMessage.From(BuildExhaustedOutboxMessage(), new Exception("boom"), FixedNow);
}
