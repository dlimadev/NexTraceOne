using FluentAssertions;
using NexTraceOne.BuildingBlocks.Observability.Observability.Models;
using Xunit;

namespace NexTraceOne.BuildingBlocks.Observability.Tests.Telemetry;

/// <summary>
/// Testes unitários para ClickHouseServiceKindFilter.
/// Valida que cada ServiceKind é traduzido corretamente para condições SQL ClickHouse.
/// </summary>
public sealed class ClickHouseServiceKindFilterTests
{
    // ── REST ──────────────────────────────────────────────────────────────────

    [Fact]
    public void Build_WhenRest_ReturnsHttpMethodCondition()
    {
        var clause = ClickHouseServiceKindFilter.Build(ServiceKindValues.Rest);

        clause.Should().NotBeNullOrWhiteSpace();
        clause.Should().Contain("http.method");
        clause.Should().Contain("http.request.method");
    }

    [Fact]
    public void Build_WhenRest_ConditionCoversLegacyAndNewAttribute()
    {
        var clause = ClickHouseServiceKindFilter.Build(ServiceKindValues.Rest)!;

        // Must cover both legacy (http.method) and OTel v2 (http.request.method)
        clause.Should().Contain("SpanAttributes['http.method'] != ''");
        clause.Should().Contain("SpanAttributes['http.request.method'] != ''");
        // Combined with OR
        clause.Should().Contain("OR");
    }

    // ── Kafka ─────────────────────────────────────────────────────────────────

    [Fact]
    public void Build_WhenKafka_ReturnsMessagingSystemKafkaCondition()
    {
        var clause = ClickHouseServiceKindFilter.Build(ServiceKindValues.Kafka);

        clause.Should().Be("SpanAttributes['messaging.system'] = 'kafka'");
    }

    // ── SOAP ──────────────────────────────────────────────────────────────────

    [Fact]
    public void Build_WhenSoap_ReturnsRpcSystemSoapCondition()
    {
        var clause = ClickHouseServiceKindFilter.Build(ServiceKindValues.Soap);

        clause.Should().Be("SpanAttributes['rpc.system'] = 'soap'");
    }

    // ── gRPC ──────────────────────────────────────────────────────────────────

    [Fact]
    public void Build_WhenGrpc_ReturnsRpcSystemGrpcCondition()
    {
        var clause = ClickHouseServiceKindFilter.Build(ServiceKindValues.GRpc);

        clause.Should().Be("SpanAttributes['rpc.system'] = 'grpc'");
    }

    // ── DB ────────────────────────────────────────────────────────────────────

    [Fact]
    public void Build_WhenDb_ReturnsDbSystemExistsCondition()
    {
        var clause = ClickHouseServiceKindFilter.Build(ServiceKindValues.Db);

        clause.Should().NotBeNullOrWhiteSpace();
        clause.Should().Contain("SpanAttributes['db.system'] != ''");
    }

    // ── Messaging ─────────────────────────────────────────────────────────────

    [Fact]
    public void Build_WhenMessaging_ReturnsConditionExcludingKafka()
    {
        var clause = ClickHouseServiceKindFilter.Build(ServiceKindValues.Messaging)!;

        clause.Should().Contain("SpanAttributes['messaging.system'] != ''");
        clause.Should().Contain("SpanAttributes['messaging.system'] != 'kafka'");
    }

    // ── Background ───────────────────────────────────────────────────────────

    [Fact]
    public void Build_WhenBackground_ReturnsInternalSpanKindWithNoNetworkAttributes()
    {
        var clause = ClickHouseServiceKindFilter.Build(ServiceKindValues.Background)!;

        clause.Should().Contain("SpanKind = 'Internal'");
        clause.Should().Contain("SpanAttributes['http.method'] = ''");
        clause.Should().Contain("SpanAttributes['messaging.system'] = ''");
        clause.Should().Contain("SpanAttributes['db.system'] = ''");
        clause.Should().Contain("SpanAttributes['rpc.system'] = ''");
    }

    // ── Unknown / null ────────────────────────────────────────────────────────

    [Fact]
    public void Build_WhenUnknown_ReturnsNull()
    {
        var clause = ClickHouseServiceKindFilter.Build(ServiceKindValues.Unknown);

        clause.Should().BeNull();
    }

    [Fact]
    public void Build_WhenArbitraryString_ReturnsNull()
    {
        var clause = ClickHouseServiceKindFilter.Build("SomethingElse");

        clause.Should().BeNull();
    }

    [Fact]
    public void Build_WhenEmptyString_ReturnsNull()
    {
        var clause = ClickHouseServiceKindFilter.Build(string.Empty);

        clause.Should().BeNull();
    }
}
