using FluentAssertions;
using NexTraceOne.BuildingBlocks.Observability.Observability.Models;
using Xunit;

namespace NexTraceOne.BuildingBlocks.Observability.Tests.Telemetry;

/// <summary>
/// Testes unitários para SpanKindResolver.
/// Valida todos os casos de inferência baseados nas convenções semânticas OTel.
/// </summary>
public sealed class SpanKindResolverTests
{
    // ── Kafka ─────────────────────────────────────────────────────────────────

    [Fact]
    public void Resolve_WhenMessagingSystemIsKafka_ReturnsKafka()
    {
        var attrs = new Dictionary<string, string> { ["messaging.system"] = "kafka" };
        SpanKindResolver.Resolve(attrs, "Producer").Should().Be(ServiceKindValues.Kafka);
    }

    [Fact]
    public void Resolve_WhenMessagingSystemIsKafkaCaseInsensitive_ReturnsKafka()
    {
        var attrs = new Dictionary<string, string> { ["messaging.system"] = "Kafka" };
        SpanKindResolver.Resolve(attrs, "Consumer").Should().Be(ServiceKindValues.Kafka);
    }

    // ── Messaging genérica ────────────────────────────────────────────────────

    [Fact]
    public void Resolve_WhenMessagingSystemIsRabbitMQ_ReturnsMessaging()
    {
        var attrs = new Dictionary<string, string> { ["messaging.system"] = "rabbitmq" };
        SpanKindResolver.Resolve(attrs, "Producer").Should().Be(ServiceKindValues.Messaging);
    }

    [Fact]
    public void Resolve_WhenMessagingSystemIsAzureServiceBus_ReturnsMessaging()
    {
        var attrs = new Dictionary<string, string> { ["messaging.system"] = "servicebus" };
        SpanKindResolver.Resolve(attrs, "Consumer").Should().Be(ServiceKindValues.Messaging);
    }

    // ── SOAP ──────────────────────────────────────────────────────────────────

    [Fact]
    public void Resolve_WhenRpcSystemIsSoap_ReturnsSoap()
    {
        var attrs = new Dictionary<string, string> { ["rpc.system"] = "soap" };
        SpanKindResolver.Resolve(attrs, "Client").Should().Be(ServiceKindValues.Soap);
    }

    [Fact]
    public void Resolve_WhenRpcSystemIsSoapCaseInsensitive_ReturnsSoap()
    {
        var attrs = new Dictionary<string, string> { ["rpc.system"] = "SOAP" };
        SpanKindResolver.Resolve(attrs, "Client").Should().Be(ServiceKindValues.Soap);
    }

    // ── gRPC ──────────────────────────────────────────────────────────────────

    [Fact]
    public void Resolve_WhenRpcSystemIsGrpc_ReturnsGRpc()
    {
        var attrs = new Dictionary<string, string> { ["rpc.system"] = "grpc" };
        SpanKindResolver.Resolve(attrs, "Client").Should().Be(ServiceKindValues.GRpc);
    }

    // ── Database ──────────────────────────────────────────────────────────────

    [Fact]
    public void Resolve_WhenDbSystemPresent_ReturnsDb()
    {
        var attrs = new Dictionary<string, string> { ["db.system"] = "postgresql" };
        SpanKindResolver.Resolve(attrs, "Client").Should().Be(ServiceKindValues.Db);
    }

    [Fact]
    public void Resolve_WhenDbSystemIsRedis_ReturnsDb()
    {
        var attrs = new Dictionary<string, string> { ["db.system"] = "redis" };
        SpanKindResolver.Resolve(attrs, "Client").Should().Be(ServiceKindValues.Db);
    }

    // ── REST / HTTP ───────────────────────────────────────────────────────────

    [Fact]
    public void Resolve_WhenHttpMethodPresent_ReturnsRest()
    {
        var attrs = new Dictionary<string, string> { ["http.method"] = "POST" };
        SpanKindResolver.Resolve(attrs, "Server").Should().Be(ServiceKindValues.Rest);
    }

    [Fact]
    public void Resolve_WhenHttpRequestMethodPresent_ReturnsRest()
    {
        var attrs = new Dictionary<string, string> { ["http.request.method"] = "GET" };
        SpanKindResolver.Resolve(attrs, "Client").Should().Be(ServiceKindValues.Rest);
    }

    // ── Background ────────────────────────────────────────────────────────────

    [Fact]
    public void Resolve_WhenSpanKindIsInternal_ReturnsBackground()
    {
        SpanKindResolver.Resolve(null, "Internal").Should().Be(ServiceKindValues.Background);
    }

    [Fact]
    public void Resolve_WhenSpanKindIsInternalAndNoAttributes_ReturnsBackground()
    {
        SpanKindResolver.Resolve(new Dictionary<string, string>(), "Internal").Should().Be(ServiceKindValues.Background);
    }

    // ── Unknown ───────────────────────────────────────────────────────────────

    [Fact]
    public void Resolve_WhenNoAttributesAndNullSpanKind_ReturnsUnknown()
    {
        SpanKindResolver.Resolve(null, null).Should().Be(ServiceKindValues.Unknown);
    }

    [Fact]
    public void Resolve_WhenNoAttributesAndEmptyAttributes_ReturnsUnknown()
    {
        SpanKindResolver.Resolve(new Dictionary<string, string>(), null).Should().Be(ServiceKindValues.Unknown);
    }

    [Fact]
    public void Resolve_WhenSpanKindIsServerWithoutHttpAttributes_ReturnsUnknown()
    {
        SpanKindResolver.Resolve(null, "Server").Should().Be(ServiceKindValues.Unknown);
    }

    [Fact]
    public void Resolve_WhenSpanKindIsClientWithoutAttributes_ReturnsUnknown()
    {
        SpanKindResolver.Resolve(null, "Client").Should().Be(ServiceKindValues.Unknown);
    }

    // ── Producer/Consumer without messaging.system ────────────────────────────

    [Fact]
    public void Resolve_WhenSpanKindIsProducerWithoutMessagingSystem_ReturnsMessaging()
    {
        SpanKindResolver.Resolve(null, "Producer").Should().Be(ServiceKindValues.Messaging);
    }

    [Fact]
    public void Resolve_WhenSpanKindIsConsumerWithoutMessagingSystem_ReturnsMessaging()
    {
        SpanKindResolver.Resolve(null, "Consumer").Should().Be(ServiceKindValues.Messaging);
    }

    // ── Priority: messaging.system beats other attributes ────────────────────

    [Fact]
    public void Resolve_WhenKafkaAndHttpAttributesBothPresent_PrioritizesKafka()
    {
        var attrs = new Dictionary<string, string>
        {
            ["messaging.system"] = "kafka",
            ["http.method"] = "POST"
        };
        SpanKindResolver.Resolve(attrs, "Producer").Should().Be(ServiceKindValues.Kafka);
    }

    // ── Priority: rpc.system beats db.system ─────────────────────────────────

    [Fact]
    public void Resolve_WhenRpcAndDbAttributesBothPresent_PrioritizesRpc()
    {
        var attrs = new Dictionary<string, string>
        {
            ["rpc.system"] = "grpc",
            ["db.system"] = "postgresql"
        };
        SpanKindResolver.Resolve(attrs, "Client").Should().Be(ServiceKindValues.GRpc);
    }
}
