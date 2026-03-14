using FluentAssertions;
using NSubstitute;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.Catalog.Application.Graph.Abstractions;
using NexTraceOne.Catalog.Domain.Graph.Entities;
using GetImpactPropagationFeature = NexTraceOne.Catalog.Application.Graph.Features.GetImpactPropagation.GetImpactPropagation;

namespace NexTraceOne.Catalog.Tests.Graph.Application.Features;

/// <summary>
/// Testes do handler GetImpactPropagation que calcula a propagação de impacto
/// a partir de um nó raiz, identificando consumidores diretos e transitivos.
/// Cenários cobertos: consumidores diretos, transitivos, nó inexistente e maxDepth.
/// </summary>
public sealed class GetImpactPropagationTests
{
    private readonly IApiAssetRepository _apiAssetRepository = Substitute.For<IApiAssetRepository>();
    private readonly IServiceAssetRepository _serviceAssetRepository = Substitute.For<IServiceAssetRepository>();
    private readonly GetImpactPropagationFeature.Handler _sut;

    public GetImpactPropagationTests()
    {
        _sut = new GetImpactPropagationFeature.Handler(_apiAssetRepository, _serviceAssetRepository);
    }

    [Fact]
    public async Task Handle_Should_ReturnImpactedNodes_When_ApiHasDirectConsumers()
    {
        // Arrange — API com um consumidor direto mapeado
        var ownerService = ServiceAsset.Create("orders-service", "Commerce", "Orders Team");
        var api = ApiAsset.Register("Orders API", "/api/orders", "1.0.0", "Internal", ownerService);
        var consumer = ConsumerAsset.Create("billing-service", "Service", "Production");
        var source = DiscoverySource.Create("CatalogImport", "catalog/billing.csv",
            new DateTimeOffset(2025, 3, 1, 0, 0, 0, TimeSpan.Zero), 0.85m);
        api.MapConsumerRelationship(consumer, source, new DateTimeOffset(2025, 3, 1, 10, 0, 0, TimeSpan.Zero));

        _apiAssetRepository.ListAllAsync(Arg.Any<CancellationToken>())
            .Returns(new List<ApiAsset> { api });
        _serviceAssetRepository.ListAllAsync(Arg.Any<CancellationToken>())
            .Returns(new List<ServiceAsset> { ownerService });

        // Act
        var result = await _sut.Handle(
            new GetImpactPropagationFeature.Query(api.Id.Value), CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.RootNodeName.Should().Be("Orders API");
        result.Value.DirectConsumers.Should().Be(1);
        result.Value.TransitiveConsumers.Should().Be(0);
        result.Value.ImpactedNodes.Should().ContainSingle(n => n.Name == "billing-service" && n.Depth == 1);
    }

    [Fact]
    public async Task Handle_Should_ReturnTransitiveConsumers_When_ConsumersAlsoExposeApis()
    {
        // Arrange — cadeia transitiva: ApiA → ServiceB → ApiB → ServiceC
        var serviceA = ServiceAsset.Create("service-a", "DomainA", "TeamA");
        var serviceB = ServiceAsset.Create("service-b", "DomainB", "TeamB");

        var apiA = ApiAsset.Register("API-A", "/api/a", "1.0.0", "Internal", serviceA);
        var apiB = ApiAsset.Register("API-B", "/api/b", "1.0.0", "Internal", serviceB);

        // ApiA consumida por service-b
        var consumerB = ConsumerAsset.Create("service-b", "Service", "Production");
        var sourceB = DiscoverySource.Create("CatalogImport", "catalog/b.csv",
            new DateTimeOffset(2025, 3, 1, 0, 0, 0, TimeSpan.Zero), 0.90m);
        apiA.MapConsumerRelationship(consumerB, sourceB, new DateTimeOffset(2025, 3, 1, 10, 0, 0, TimeSpan.Zero));

        // ApiB consumida por service-c (transitivo)
        var consumerC = ConsumerAsset.Create("service-c", "Service", "Production");
        var sourceC = DiscoverySource.Create("OpenTelemetry", "otel:trace:456",
            new DateTimeOffset(2025, 3, 2, 0, 0, 0, TimeSpan.Zero), 0.75m);
        apiB.MapConsumerRelationship(consumerC, sourceC, new DateTimeOffset(2025, 3, 2, 10, 0, 0, TimeSpan.Zero));

        _apiAssetRepository.ListAllAsync(Arg.Any<CancellationToken>())
            .Returns(new List<ApiAsset> { apiA, apiB });
        _serviceAssetRepository.ListAllAsync(Arg.Any<CancellationToken>())
            .Returns(new List<ServiceAsset> { serviceA, serviceB });

        // Act
        var result = await _sut.Handle(
            new GetImpactPropagationFeature.Query(apiA.Id.Value, MaxDepth: 3), CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.DirectConsumers.Should().Be(1);
        result.Value.TransitiveConsumers.Should().BeGreaterThanOrEqualTo(1);
        result.Value.TotalImpacted.Should().BeGreaterThanOrEqualTo(2);
        result.Value.ImpactedNodes.Should().Contain(n => n.Name == "service-b" && n.Depth == 1);
        result.Value.ImpactedNodes.Should().Contain(n => n.Name == "service-c" && n.Depth == 2);
    }

    [Fact]
    public async Task Handle_Should_ReturnNotFound_When_RootNodeDoesNotExist()
    {
        // Arrange — nenhum ativo no grafo
        _apiAssetRepository.ListAllAsync(Arg.Any<CancellationToken>())
            .Returns(new List<ApiAsset>());
        _serviceAssetRepository.ListAllAsync(Arg.Any<CancellationToken>())
            .Returns(new List<ServiceAsset>());

        var nonExistentId = Guid.NewGuid();

        // Act
        var result = await _sut.Handle(
            new GetImpactPropagationFeature.Query(nonExistentId), CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("CatalogGraph.Impact.RootNotFound");
    }

    [Fact]
    public async Task Handle_Should_RespectMaxDepth_When_TransitiveChainsExceedLimit()
    {
        // Arrange — mesma cadeia transitiva, mas maxDepth=1 deve limitar a profundidade
        var serviceA = ServiceAsset.Create("service-a", "DomainA", "TeamA");
        var serviceB = ServiceAsset.Create("service-b", "DomainB", "TeamB");

        var apiA = ApiAsset.Register("API-A", "/api/a", "1.0.0", "Internal", serviceA);
        var apiB = ApiAsset.Register("API-B", "/api/b", "1.0.0", "Internal", serviceB);

        var consumerB = ConsumerAsset.Create("service-b", "Service", "Production");
        var sourceB = DiscoverySource.Create("CatalogImport", "catalog/b.csv",
            new DateTimeOffset(2025, 3, 1, 0, 0, 0, TimeSpan.Zero), 0.90m);
        apiA.MapConsumerRelationship(consumerB, sourceB, new DateTimeOffset(2025, 3, 1, 10, 0, 0, TimeSpan.Zero));

        var consumerC = ConsumerAsset.Create("service-c", "Service", "Production");
        var sourceC = DiscoverySource.Create("OpenTelemetry", "otel:trace:789",
            new DateTimeOffset(2025, 3, 2, 0, 0, 0, TimeSpan.Zero), 0.75m);
        apiB.MapConsumerRelationship(consumerC, sourceC, new DateTimeOffset(2025, 3, 2, 10, 0, 0, TimeSpan.Zero));

        _apiAssetRepository.ListAllAsync(Arg.Any<CancellationToken>())
            .Returns(new List<ApiAsset> { apiA, apiB });
        _serviceAssetRepository.ListAllAsync(Arg.Any<CancellationToken>())
            .Returns(new List<ServiceAsset> { serviceA, serviceB });

        // Act — maxDepth=1 impede propagação transitiva
        var result = await _sut.Handle(
            new GetImpactPropagationFeature.Query(apiA.Id.Value, MaxDepth: 1), CancellationToken.None);

        // Assert — apenas consumidores diretos (depth 1) devem aparecer
        result.IsSuccess.Should().BeTrue();
        result.Value.DirectConsumers.Should().Be(1);
        result.Value.TransitiveConsumers.Should().Be(0);
        result.Value.ImpactedNodes.Should().OnlyContain(n => n.Depth == 1);
    }
}
