using Microsoft.Extensions.Logging.Abstractions;
using NexTraceOne.Integrations.Domain;
using NexTraceOne.Integrations.Infrastructure.Kafka;

namespace NexTraceOne.Integrations.Tests.Application.Features;

/// <summary>
/// Testes unitários para <see cref="NullKafkaEventProducer"/>.
/// Valida que a implementação nula descarta silenciosamente os eventos,
/// reporta IsConfigured = false e não lança exceções.
/// </summary>
public sealed class NullKafkaEventProducerTests
{
    private readonly NullKafkaEventProducer _sut = new(NullLogger<NullKafkaEventProducer>.Instance);

    [Fact]
    public void IsConfigured_Should_BeFalse()
    {
        _sut.IsConfigured.Should().BeFalse();
    }

    [Fact]
    public async Task ProduceAsync_Should_CompleteSuccessfully_Without_Throwing()
    {
        // Arrange
        var act = async () => await _sut.ProduceAsync(
            "integrations.events",
            "evt-001",
            "IngestionPayloadProcessed",
            """{"id":"evt-001","serviceId":"svc-001"}""",
            CancellationToken.None);

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task ProduceAsync_Should_CompleteSuccessfully_With_EmptyPayload()
    {
        var act = async () => await _sut.ProduceAsync(
            "topic", "key", "EventType", string.Empty, CancellationToken.None);

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task ProduceBatchAsync_Should_CompleteSuccessfully_With_MultipleMessages()
    {
        var messages = new List<KafkaMessage>
        {
            new("key-1", "Event.Created", """{"id":"1"}"""),
            new("key-2", "Event.Updated", """{"id":"2"}"""),
            new("key-3", "Event.Deleted", """{"id":"3"}""")
        };

        var act = async () => await _sut.ProduceBatchAsync(
            "integrations.events", messages, CancellationToken.None);

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task ProduceBatchAsync_Should_CompleteSuccessfully_With_EmptyBatch()
    {
        var act = async () => await _sut.ProduceBatchAsync(
            "topic", Array.Empty<KafkaMessage>(), CancellationToken.None);

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public void KafkaMessage_Record_Should_Hold_Properties()
    {
        var msg = new KafkaMessage("my-key", "MyEvent", """{"data":"value"}""");

        msg.Key.Should().Be("my-key");
        msg.EventType.Should().Be("MyEvent");
        msg.PayloadJson.Should().Be("""{"data":"value"}""");
    }
}
