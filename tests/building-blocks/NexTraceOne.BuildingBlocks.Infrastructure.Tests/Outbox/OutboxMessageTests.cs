using FluentAssertions;
using NexTraceOne.BuildingBlocks.Infrastructure.Outbox;

namespace NexTraceOne.BuildingBlocks.Infrastructure.Tests.Outbox;

/// <summary>
/// Testes para OutboxMessage — foco na garantia de idempotência determinística.
/// </summary>
public sealed class OutboxMessageTests
{
    private sealed record SampleDomainEvent(string OrderId, decimal Amount, DateTimeOffset OccurredAt);

    [Fact]
    public void Create_SameEvent_ShouldProduceDeterministicIdempotencyKey()
    {
        var domainEvent = new SampleDomainEvent("order-123", 99.50m, new DateTimeOffset(2026, 3, 20, 12, 0, 0, TimeSpan.Zero));
        var tenantId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
        var createdAt = new DateTimeOffset(2026, 3, 20, 12, 0, 0, TimeSpan.Zero);

        var msg1 = OutboxMessage.Create(domainEvent, tenantId, createdAt);
        var msg2 = OutboxMessage.Create(domainEvent, tenantId, createdAt);

        msg1.IdempotencyKey.Should().Be(msg2.IdempotencyKey, "same logical event must produce the same idempotency key");
    }

    [Fact]
    public void Create_DifferentPayloads_ShouldProduceDifferentKeys()
    {
        var event1 = new SampleDomainEvent("order-123", 99.50m, DateTimeOffset.UtcNow);
        var event2 = new SampleDomainEvent("order-456", 150.00m, DateTimeOffset.UtcNow);
        var tenantId = Guid.NewGuid();
        var createdAt = DateTimeOffset.UtcNow;

        var msg1 = OutboxMessage.Create(event1, tenantId, createdAt);
        var msg2 = OutboxMessage.Create(event2, tenantId, createdAt);

        msg1.IdempotencyKey.Should().NotBe(msg2.IdempotencyKey, "different payloads must produce different keys");
    }

    [Fact]
    public void Create_ShouldSetEventType()
    {
        var domainEvent = new SampleDomainEvent("order-123", 50m, DateTimeOffset.UtcNow);
        var msg = OutboxMessage.Create(domainEvent, Guid.NewGuid(), DateTimeOffset.UtcNow);

        msg.EventType.Should().Contain(nameof(SampleDomainEvent));
    }

    [Fact]
    public void Create_ShouldSetTenantId()
    {
        var tenantId = Guid.NewGuid();
        var msg = OutboxMessage.Create(new SampleDomainEvent("o1", 1m, DateTimeOffset.UtcNow), tenantId, DateTimeOffset.UtcNow);

        msg.TenantId.Should().Be(tenantId);
    }

    [Fact]
    public void Create_ShouldSerializePayloadAsJson()
    {
        var msg = OutboxMessage.Create(new SampleDomainEvent("order-1", 10m, DateTimeOffset.UtcNow), Guid.NewGuid(), DateTimeOffset.UtcNow);

        msg.Payload.Should().Contain("order-1");
        msg.Payload.Should().Contain("10");
    }

    [Fact]
    public void Create_IdempotencyKeyShouldContainEventTypeAndHash()
    {
        var domainEvent = new SampleDomainEvent("order-1", 10m, DateTimeOffset.UtcNow);
        var msg = OutboxMessage.Create(domainEvent, Guid.NewGuid(), DateTimeOffset.UtcNow);

        var parts = msg.IdempotencyKey.Split(':');
        parts.Length.Should().BeGreaterThanOrEqualTo(3, "format: {EventType}:{Hash}:{Timestamp}");
        parts[0].Should().Contain(nameof(SampleDomainEvent));
        parts[1].Should().HaveLength(16, "content hash should be 16 hex characters");
    }

    [Fact]
    public void Create_NewMessage_ShouldHaveZeroRetryCountAndNoProcessedAt()
    {
        var msg = OutboxMessage.Create(new SampleDomainEvent("o1", 1m, DateTimeOffset.UtcNow), Guid.NewGuid(), DateTimeOffset.UtcNow);

        msg.RetryCount.Should().Be(0);
        msg.ProcessedAt.Should().BeNull();
        msg.LastError.Should().BeNull();
    }

    [Fact]
    public void Create_ShouldGenerateUniqueIds()
    {
        var msg1 = OutboxMessage.Create(new SampleDomainEvent("o1", 1m, DateTimeOffset.UtcNow), Guid.NewGuid(), DateTimeOffset.UtcNow);
        var msg2 = OutboxMessage.Create(new SampleDomainEvent("o2", 2m, DateTimeOffset.UtcNow), Guid.NewGuid(), DateTimeOffset.UtcNow);

        msg1.Id.Should().NotBe(msg2.Id);
    }
}
