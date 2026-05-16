using FluentAssertions;
using NexTraceOne.Catalog.Domain.Graph.Entities;
using ApiAssetAggregate = NexTraceOne.Catalog.Domain.Graph.Entities.ApiAsset;

namespace NexTraceOne.Catalog.Tests.Graph.Domain.Entities;

/// <summary>
/// Testes de domínio para o aggregate ApiAsset.
/// </summary>
public sealed class ApiAssetTests
{
    [Fact]
    public void Register_Should_CreateApiAsset_When_InputIsValid()
    {
        var apiAsset = CreateApiAsset();

        apiAsset.Name.Should().Be("Orders API");
        apiAsset.OwnerService.Name.Should().Be("orders-service");
    }

    [Fact]
    public void AddDiscoverySource_Should_ReturnFailure_When_SourceIsDuplicated()
    {
        var apiAsset = CreateApiAsset();
        var source = DiscoverySource.Create("Manual", "orders/openapi.yaml", new DateTimeOffset(2025, 02, 01, 0, 0, 0, TimeSpan.Zero), 0.90m);

        var firstResult = apiAsset.AddDiscoverySource(source);
        var secondResult = apiAsset.AddDiscoverySource(DiscoverySource.Create("Manual", "orders/openapi.yaml", new DateTimeOffset(2025, 02, 02, 0, 0, 0, TimeSpan.Zero), 0.95m));

        firstResult.IsSuccess.Should().BeTrue();
        secondResult.IsFailure.Should().BeTrue();
        secondResult.Error.Code.Should().Be("CatalogGraph.DiscoverySource.Duplicate");
    }

    [Fact]
    public void MapConsumerRelationship_Should_CreateRelationship_When_ConsumerIsNew()
    {
        var apiAsset = CreateApiAsset();
        var consumer = ConsumerAsset.Create("billing-service", "Service", "Production");
        var source = DiscoverySource.Create("CatalogImport", "imports/orders.csv", new DateTimeOffset(2025, 02, 01, 0, 0, 0, TimeSpan.Zero), 0.80m);

        var result = apiAsset.MapConsumerRelationship(consumer, source, new DateTimeOffset(2025, 02, 01, 10, 0, 0, TimeSpan.Zero));

        result.IsSuccess.Should().BeTrue();
        apiAsset.ConsumerRelationships.Should().ContainSingle();
    }

    [Fact]
    public void InferDependencyFromOtel_Should_CreateRelationship_FromTelemetry()
    {
        var apiAsset = CreateApiAsset();

        var result = apiAsset.InferDependencyFromOtel(
            "checkout-service",
            "Production",
            "otel:trace:123",
            new DateTimeOffset(2025, 02, 01, 10, 0, 0, TimeSpan.Zero),
            0.92m);

        result.IsSuccess.Should().BeTrue();
        result.Value.SourceType.Should().Be("OpenTelemetry");
        apiAsset.ConsumerRelationships.Should().ContainSingle();
    }

    private static ApiAssetAggregate CreateApiAsset()
        => ApiAssetAggregate.Register(
            "Orders API",
            "/api/orders",
            "1.0.0",
            "Internal",
            ServiceAsset.Create("orders-service", "Commerce", "Core APIs", Guid.NewGuid()));
}
