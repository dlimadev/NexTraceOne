using System.Text.Json;
using FluentAssertions;
using NexTraceOne.BuildingBlocks.Observability.Observability.Models;
using Xunit;

namespace NexTraceOne.BuildingBlocks.Observability.Tests.Telemetry;

/// <summary>
/// Testes unitários para ElasticServiceKindFilter.
/// Valida que cada ServiceKind é traduzido para condições Query DSL corretas para Elasticsearch.
/// </summary>
public sealed class ElasticServiceKindFilterTests
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
    };

    private static string Serialize(object? obj) =>
        JsonSerializer.Serialize(obj, JsonOpts);

    // ── REST ──────────────────────────────────────────────────────────────────

    [Fact]
    public void Build_WhenRest_ReturnsNonNull()
    {
        ElasticServiceKindFilter.Build(ServiceKindValues.Rest).Should().NotBeNull();
    }

    [Fact]
    public void Build_WhenRest_JsonContainsHttpMethodFields()
    {
        var json = Serialize(ElasticServiceKindFilter.Build(ServiceKindValues.Rest));

        json.Should().Contain("attributes.http.method");
        json.Should().Contain("attributes.http.request.method");
        json.Should().Contain("should");
        json.Should().Contain("minimum_should_match");
    }

    // ── Kafka ─────────────────────────────────────────────────────────────────

    [Fact]
    public void Build_WhenKafka_ReturnsNonNull()
    {
        ElasticServiceKindFilter.Build(ServiceKindValues.Kafka).Should().NotBeNull();
    }

    [Fact]
    public void Build_WhenKafka_JsonContainsMessagingSystemKafka()
    {
        var json = Serialize(ElasticServiceKindFilter.Build(ServiceKindValues.Kafka));

        json.Should().Contain("attributes.messaging.system");
        json.Should().Contain("kafka");
        json.Should().Contain("term");
    }

    // ── SOAP ──────────────────────────────────────────────────────────────────

    [Fact]
    public void Build_WhenSoap_JsonContainsRpcSystemSoap()
    {
        var json = Serialize(ElasticServiceKindFilter.Build(ServiceKindValues.Soap));

        json.Should().Contain("attributes.rpc.system");
        json.Should().Contain("soap");
    }

    // ── gRPC ──────────────────────────────────────────────────────────────────

    [Fact]
    public void Build_WhenGrpc_JsonContainsRpcSystemGrpc()
    {
        var json = Serialize(ElasticServiceKindFilter.Build(ServiceKindValues.GRpc));

        json.Should().Contain("attributes.rpc.system");
        json.Should().Contain("grpc");
    }

    // ── DB ────────────────────────────────────────────────────────────────────

    [Fact]
    public void Build_WhenDb_JsonContainsDbSystemExists()
    {
        var json = Serialize(ElasticServiceKindFilter.Build(ServiceKindValues.Db));

        json.Should().Contain("attributes.db.system");
        json.Should().Contain("exists");
    }

    // ── Messaging ─────────────────────────────────────────────────────────────

    [Fact]
    public void Build_WhenMessaging_JsonContainsMessagingSystemExistsAndExcludesKafka()
    {
        var json = Serialize(ElasticServiceKindFilter.Build(ServiceKindValues.Messaging));

        json.Should().Contain("attributes.messaging.system");
        json.Should().Contain("must_not");
        json.Should().Contain("kafka");
    }

    // ── Background ───────────────────────────────────────────────────────────

    [Fact]
    public void Build_WhenBackground_JsonContainsInternalSpanKindAndMustNots()
    {
        var json = Serialize(ElasticServiceKindFilter.Build(ServiceKindValues.Background));

        json.Should().Contain("span_kind");
        json.Should().Contain("Internal");
        json.Should().Contain("must_not");
        json.Should().Contain("attributes.http.method");
        json.Should().Contain("attributes.messaging.system");
        json.Should().Contain("attributes.db.system");
        json.Should().Contain("attributes.rpc.system");
    }

    // ── Unknown / null ────────────────────────────────────────────────────────

    [Fact]
    public void Build_WhenUnknown_ReturnsNull()
    {
        ElasticServiceKindFilter.Build(ServiceKindValues.Unknown).Should().BeNull();
    }

    [Fact]
    public void Build_WhenArbitraryString_ReturnsNull()
    {
        ElasticServiceKindFilter.Build("SomethingElse").Should().BeNull();
    }

    [Fact]
    public void Build_WhenEmptyString_ReturnsNull()
    {
        ElasticServiceKindFilter.Build(string.Empty).Should().BeNull();
    }

    // ── Serializabilidade JSON ────────────────────────────────────────────────

    [Theory]
    [InlineData(ServiceKindValues.Rest)]
    [InlineData(ServiceKindValues.Kafka)]
    [InlineData(ServiceKindValues.Soap)]
    [InlineData(ServiceKindValues.GRpc)]
    [InlineData(ServiceKindValues.Db)]
    [InlineData(ServiceKindValues.Messaging)]
    [InlineData(ServiceKindValues.Background)]
    public void Build_AllKnownKinds_ReturnSerializableObjects(string serviceKind)
    {
        var condition = ElasticServiceKindFilter.Build(serviceKind);
        condition.Should().NotBeNull();

        // Must not throw on serialization (required for Elasticsearch query builder)
        var act = () => Serialize(condition);
        act.Should().NotThrow();
    }
}
