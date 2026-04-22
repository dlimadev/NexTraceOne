using NexTraceOne.Integrations.Application.Abstractions;
using NexTraceOne.Integrations.Application.Features.GetEventConsumerStatus;
using NexTraceOne.Integrations.Application.Services;
using NexTraceOne.Integrations.Application.Services.NormalizationStrategies;
using NexTraceOne.Integrations.Domain.Entities;
using NexTraceOne.Integrations.Domain.Enums;

namespace NexTraceOne.Integrations.Tests.Application.Features;

/// <summary>
/// Testes de Wave Z: estratégias de normalização de eventos e handler GetEventConsumerStatus.
/// Cobre os quatro tipos de fonte (Kafka, ServiceBus, SQS, RabbitMQ) e o handler de estado.
/// </summary>
public sealed class WaveZTests
{
    // ── Helpers ──────────────────────────────────────────────────────────────

    private static RawConsumerEvent MakeRaw(string sourceType, string topic, string payload) =>
        new(sourceType, topic, null, payload, DateTimeOffset.UtcNow);

    // ═════════════════════════════════════════════════════════════════════════
    // KafkaChangeEventStrategy (5 testes)
    // ═════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task Kafka_ValidPayload_ReturnsNormalizedEvent()
    {
        var strategy = new KafkaChangeEventStrategy();
        var raw = MakeRaw("Kafka", "deployments", """
            {"service_name":"payments","release_id":"v1.2.0","environment":"production","event_type":"DeployCompleted"}
            """);

        var result = await strategy.NormalizeAsync(raw, CancellationToken.None);

        result.Should().NotBeNull();
        result!.ServiceName.Should().Be("payments");
        result.ReleaseId.Should().Be("v1.2.0");
        result.EnvironmentName.Should().Be("production");
        result.EventType.Should().Be("DeployCompleted");
    }

    [Fact]
    public async Task Kafka_InvalidJson_ReturnsNull()
    {
        var strategy = new KafkaChangeEventStrategy();
        var raw = MakeRaw("Kafka", "deployments", "NOT_JSON");

        var result = await strategy.NormalizeAsync(raw, CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public async Task Kafka_MissingServiceName_ReturnsNull()
    {
        var strategy = new KafkaChangeEventStrategy();
        var raw = MakeRaw("Kafka", "deployments", """{"release_id":"v1.0"}""");

        var result = await strategy.NormalizeAsync(raw, CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public async Task Kafka_MissingOptionalFields_ReturnsPartialNormalization()
    {
        var strategy = new KafkaChangeEventStrategy();
        var raw = MakeRaw("Kafka", "deployments", """{"service_name":"orders"}""");

        var result = await strategy.NormalizeAsync(raw, CancellationToken.None);

        result.Should().NotBeNull();
        result!.ServiceName.Should().Be("orders");
        result.ReleaseId.Should().BeNull();
        result.EnvironmentName.Should().BeNull();
    }

    [Fact]
    public void Kafka_CanHandle_ReturnsTrueForKafka()
    {
        var strategy = new KafkaChangeEventStrategy();

        strategy.CanHandle("Kafka").Should().BeTrue();
        strategy.CanHandle("kafka").Should().BeTrue();
        strategy.SourceType.Should().Be(EventSourceType.Kafka);
    }

    // ═════════════════════════════════════════════════════════════════════════
    // ServiceBusChangeEventStrategy (5 testes)
    // ═════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task ServiceBus_ValidPayload_ReturnsNormalizedEvent()
    {
        var strategy = new ServiceBusChangeEventStrategy();
        var raw = MakeRaw("ServiceBus", "changes", """
            {"serviceName":"inventory","version":"2.0.0","env":"staging","eventType":"ReleaseStarted"}
            """);

        var result = await strategy.NormalizeAsync(raw, CancellationToken.None);

        result.Should().NotBeNull();
        result!.ServiceName.Should().Be("inventory");
        result.ReleaseId.Should().Be("2.0.0");
        result.EnvironmentName.Should().Be("staging");
    }

    [Fact]
    public async Task ServiceBus_InvalidJson_ReturnsNull()
    {
        var strategy = new ServiceBusChangeEventStrategy();
        var raw = MakeRaw("ServiceBus", "changes", "{broken json");

        var result = await strategy.NormalizeAsync(raw, CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public async Task ServiceBus_MissingServiceName_ReturnsNull()
    {
        var strategy = new ServiceBusChangeEventStrategy();
        var raw = MakeRaw("ServiceBus", "changes", """{"version":"1.0","env":"prod"}""");

        var result = await strategy.NormalizeAsync(raw, CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public async Task ServiceBus_MissingOptionalFields_ReturnsPartialNormalization()
    {
        var strategy = new ServiceBusChangeEventStrategy();
        var raw = MakeRaw("ServiceBus", "changes", """{"serviceName":"catalog"}""");

        var result = await strategy.NormalizeAsync(raw, CancellationToken.None);

        result.Should().NotBeNull();
        result!.ServiceName.Should().Be("catalog");
        result.ReleaseId.Should().BeNull();
    }

    [Fact]
    public void ServiceBus_CanHandle_ReturnsTrueForServiceBus()
    {
        var strategy = new ServiceBusChangeEventStrategy();

        strategy.CanHandle("ServiceBus").Should().BeTrue();
        strategy.CanHandle("servicebus").Should().BeTrue();
        strategy.SourceType.Should().Be(EventSourceType.ServiceBus);
    }

    // ═════════════════════════════════════════════════════════════════════════
    // SqsChangeEventStrategy (5 testes)
    // ═════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task Sqs_DirectPayload_ReturnsNormalizedEvent()
    {
        var strategy = new SqsChangeEventStrategy();
        var raw = MakeRaw("SQS", "deploy-events", """
            {"service_name":"auth","release_id":"rc-1","environment":"pre-production"}
            """);

        var result = await strategy.NormalizeAsync(raw, CancellationToken.None);

        result.Should().NotBeNull();
        result!.ServiceName.Should().Be("auth");
        result.ReleaseId.Should().Be("rc-1");
    }

    [Fact]
    public async Task Sqs_SnsEnvelope_ReturnsNormalizedEvent()
    {
        var strategy = new SqsChangeEventStrategy();
        var innerJson = """{"service_name":"gateway","environment":"production"}""";
        var raw = MakeRaw("SQS", "notifications", $$"""{"Type":"Notification","Message":"{{innerJson.Replace("\"", "\\\"")}}"}""");

        var result = await strategy.NormalizeAsync(raw, CancellationToken.None);

        result.Should().NotBeNull();
        result!.ServiceName.Should().Be("gateway");
    }

    [Fact]
    public async Task Sqs_InvalidJson_ReturnsNull()
    {
        var strategy = new SqsChangeEventStrategy();
        var raw = MakeRaw("SQS", "deploy-events", "not-json");

        var result = await strategy.NormalizeAsync(raw, CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public async Task Sqs_MissingServiceName_ReturnsNull()
    {
        var strategy = new SqsChangeEventStrategy();
        var raw = MakeRaw("SQS", "deploy-events", """{"environment":"prod"}""");

        var result = await strategy.NormalizeAsync(raw, CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public void Sqs_CanHandle_ReturnsTrueForSqs()
    {
        var strategy = new SqsChangeEventStrategy();

        strategy.CanHandle("SQS").Should().BeTrue();
        strategy.CanHandle("Sqs").Should().BeTrue();
        strategy.SourceType.Should().Be(EventSourceType.Sqs);
    }

    // ═════════════════════════════════════════════════════════════════════════
    // RabbitMqChangeEventStrategy (5 testes)
    // ═════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task RabbitMq_WithAmqpHeaders_ReturnsNormalizedEvent()
    {
        var strategy = new RabbitMqChangeEventStrategy();
        var raw = MakeRaw("RabbitMQ", "domain.changes", """
            {"headers":{"x-service":"billing","x-release":"1.5.0","x-env":"production","x-event-type":"Deploy"}}
            """);

        var result = await strategy.NormalizeAsync(raw, CancellationToken.None);

        result.Should().NotBeNull();
        result!.ServiceName.Should().Be("billing");
        result.ReleaseId.Should().Be("1.5.0");
        result.EnvironmentName.Should().Be("production");
    }

    [Fact]
    public async Task RabbitMq_DirectPayload_ReturnsNormalizedEvent()
    {
        var strategy = new RabbitMqChangeEventStrategy();
        var raw = MakeRaw("RabbitMq", "domain.changes", """
            {"service_name":"shipping","environment":"staging"}
            """);

        var result = await strategy.NormalizeAsync(raw, CancellationToken.None);

        result.Should().NotBeNull();
        result!.ServiceName.Should().Be("shipping");
    }

    [Fact]
    public async Task RabbitMq_InvalidJson_ReturnsNull()
    {
        var strategy = new RabbitMqChangeEventStrategy();
        var raw = MakeRaw("RabbitMQ", "changes", "{{invalid");

        var result = await strategy.NormalizeAsync(raw, CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public async Task RabbitMq_MissingServiceName_ReturnsNull()
    {
        var strategy = new RabbitMqChangeEventStrategy();
        var raw = MakeRaw("RabbitMQ", "changes", """{"headers":{"x-env":"prod"}}""");

        var result = await strategy.NormalizeAsync(raw, CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public void RabbitMq_CanHandle_ReturnsTrueForRabbitMq()
    {
        var strategy = new RabbitMqChangeEventStrategy();

        strategy.CanHandle("RabbitMQ").Should().BeTrue();
        strategy.CanHandle("RabbitMq").Should().BeTrue();
        strategy.SourceType.Should().Be(EventSourceType.RabbitMq);
    }

    // ═════════════════════════════════════════════════════════════════════════
    // GetEventConsumerStatus Handler (5 testes)
    // ═════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task GetEventConsumerStatus_EmptyStatus_ReturnsEmptyConsumersAndHealthy()
    {
        var statusReader = new NullEventConsumerStatusReader();
        var deadLetterRepo = new NullEventConsumerDeadLetterRepository();
        var handler = new GetEventConsumerStatus.Handler(statusReader, deadLetterRepo);

        var result = await handler.Handle(new GetEventConsumerStatus.Query(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Consumers.Should().BeEmpty();
        result.Value.TotalDeadLetterCount.Should().Be(0);
        result.Value.IsHealthy.Should().BeTrue();
    }

    [Fact]
    public async Task GetEventConsumerStatus_WithDeadLetters_ReturnsUnhealthy()
    {
        var statusReader = new NullEventConsumerStatusReader();
        var deadLetterRepo = new NullEventConsumerDeadLetterRepository();

        var record = EventConsumerDeadLetterRecord.Record(
            Guid.NewGuid(), "Kafka", "deploy.events", null,
            """{"service_name":"test"}""", "Parse error");
        await deadLetterRepo.AddAsync(record, CancellationToken.None);

        var handler = new GetEventConsumerStatus.Handler(statusReader, deadLetterRepo);
        var result = await handler.Handle(new GetEventConsumerStatus.Query(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TotalDeadLetterCount.Should().Be(1);
        result.Value.IsHealthy.Should().BeFalse();
    }

    [Fact]
    public async Task GetEventConsumerStatus_DeadLetterAfterResolve_CountsCorrectly()
    {
        var statusReader = new NullEventConsumerStatusReader();
        var deadLetterRepo = new NullEventConsumerDeadLetterRepository();

        var record = EventConsumerDeadLetterRecord.Record(
            Guid.NewGuid(), "Kafka", "topic.a", null, "{}", "error");
        await deadLetterRepo.AddAsync(record, CancellationToken.None);
        await deadLetterRepo.ResolveAsync(record.Id, CancellationToken.None);

        var handler = new GetEventConsumerStatus.Handler(statusReader, deadLetterRepo);
        var result = await handler.Handle(new GetEventConsumerStatus.Query(), CancellationToken.None);

        result.Value.TotalDeadLetterCount.Should().Be(0);
        result.Value.IsHealthy.Should().BeTrue();
    }

    [Fact]
    public async Task GetEventConsumerStatus_CheckedAt_IsRecent()
    {
        var before = DateTimeOffset.UtcNow.AddSeconds(-1);
        var handler = new GetEventConsumerStatus.Handler(
            new NullEventConsumerStatusReader(),
            new NullEventConsumerDeadLetterRepository());

        var result = await handler.Handle(new GetEventConsumerStatus.Query(), CancellationToken.None);

        result.Value.CheckedAt.Should().BeAfter(before);
    }

    [Fact]
    public async Task GetEventConsumerStatus_MultipleDeadLetters_CountsAll()
    {
        var repo = new NullEventConsumerDeadLetterRepository();
        var tenantId = Guid.NewGuid();

        for (var i = 0; i < 3; i++)
        {
            var record = EventConsumerDeadLetterRecord.Record(
                tenantId, "ServiceBus", $"topic-{i}", null, "{}", $"error-{i}");
            await repo.AddAsync(record, CancellationToken.None);
        }

        var handler = new GetEventConsumerStatus.Handler(
            new NullEventConsumerStatusReader(), repo);
        var result = await handler.Handle(new GetEventConsumerStatus.Query(), CancellationToken.None);

        result.Value.TotalDeadLetterCount.Should().Be(3);
    }
}
