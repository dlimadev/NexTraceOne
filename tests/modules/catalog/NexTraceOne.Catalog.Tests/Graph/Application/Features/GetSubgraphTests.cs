using FluentAssertions;
using NSubstitute;
using NexTraceOne.Catalog.Application.Graph.Abstractions;
using NexTraceOne.Catalog.Domain.Graph.Entities;
using GetSubgraphFeature = NexTraceOne.Catalog.Application.Graph.Features.GetSubgraph.GetSubgraph;

namespace NexTraceOne.Catalog.Tests.Graph.Application.Features;

/// <summary>
/// Testes do handler GetSubgraph que constrói mini-grafos contextuais
/// centrados em um nó raiz (API ou serviço) com profundidade e limite de nós configuráveis.
/// Cenários cobertos: nó API, nó serviço, nó inexistente e truncamento por maxNodes.
/// </summary>
public sealed class GetSubgraphTests
{
    private readonly IApiAssetRepository _apiAssetRepository = Substitute.For<IApiAssetRepository>();
    private readonly IServiceAssetRepository _serviceAssetRepository = Substitute.For<IServiceAssetRepository>();
    private readonly GetSubgraphFeature.Handler _sut;

    public GetSubgraphTests()
    {
        _sut = new GetSubgraphFeature.Handler(_apiAssetRepository, _serviceAssetRepository);
    }

    [Fact]
    public async Task Handle_Should_ReturnSubgraph_When_RootIsValidApiNode()
    {
        // Arrange — API com serviço proprietário
        var ownerService = ServiceAsset.Create("payments-service", "Finance", "Payments Team");
        var api = ApiAsset.Register("Payments API", "/api/payments", "2.0.0", "Internal", ownerService);

        _apiAssetRepository.ListAllAsync(Arg.Any<CancellationToken>())
            .Returns(new List<ApiAsset> { api });
        _serviceAssetRepository.ListAllAsync(Arg.Any<CancellationToken>())
            .Returns(new List<ServiceAsset> { ownerService });

        // Act
        var result = await _sut.Handle(
            new GetSubgraphFeature.Query(api.Id.Value), CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.RootNodeId.Should().Be(api.Id.Value);
        result.Value.Apis.Should().ContainSingle(a => a.Name == "Payments API");
        result.Value.Services.Should().ContainSingle(s => s.Name == "payments-service");
        result.Value.Edges.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Handle_Should_ReturnSubgraph_When_RootIsValidServiceNode()
    {
        // Arrange — serviço com API associada
        var service = ServiceAsset.Create("orders-service", "Commerce", "Orders Team");
        var api = ApiAsset.Register("Orders API", "/api/orders", "1.0.0", "Public", service);

        _apiAssetRepository.ListAllAsync(Arg.Any<CancellationToken>())
            .Returns(new List<ApiAsset> { api });
        _serviceAssetRepository.ListAllAsync(Arg.Any<CancellationToken>())
            .Returns(new List<ServiceAsset> { service });

        // Act — usa o Id do serviço como raiz
        var result = await _sut.Handle(
            new GetSubgraphFeature.Query(service.Id.Value), CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.RootNodeId.Should().Be(service.Id.Value);
        result.Value.Services.Should().ContainSingle(s => s.Name == "orders-service");
        result.Value.Apis.Should().ContainSingle(a => a.Name == "Orders API");
    }

    [Fact]
    public async Task Handle_Should_ReturnNotFound_When_NodeDoesNotExist()
    {
        // Arrange — repositórios vazios
        _apiAssetRepository.ListAllAsync(Arg.Any<CancellationToken>())
            .Returns(new List<ApiAsset>());
        _serviceAssetRepository.ListAllAsync(Arg.Any<CancellationToken>())
            .Returns(new List<ServiceAsset>());

        var nonExistentId = Guid.NewGuid();

        // Act
        var result = await _sut.Handle(
            new GetSubgraphFeature.Query(nonExistentId), CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("CatalogGraph.Impact.RootNotFound");
    }

    [Fact]
    public async Task Handle_Should_TruncateAndSetFlag_When_MaxNodesExceeded()
    {
        // Arrange — grafo com nós suficientes para exceder maxNodes=2
        var service = ServiceAsset.Create("platform-service", "Platform", "Platform Team");
        var api1 = ApiAsset.Register("API-1", "/api/1", "1.0.0", "Internal", service);
        var api2 = ApiAsset.Register("API-2", "/api/2", "1.0.0", "Internal", service);
        var api3 = ApiAsset.Register("API-3", "/api/3", "1.0.0", "Internal", service);

        _apiAssetRepository.ListAllAsync(Arg.Any<CancellationToken>())
            .Returns(new List<ApiAsset> { api1, api2, api3 });
        _serviceAssetRepository.ListAllAsync(Arg.Any<CancellationToken>())
            .Returns(new List<ServiceAsset> { service });

        // Act — maxNodes=2 força truncamento (serviço + 1 API já atinge o limite)
        var result = await _sut.Handle(
            new GetSubgraphFeature.Query(service.Id.Value, MaxDepth: 2, MaxNodes: 2),
            CancellationToken.None);

        // Assert — flag de truncamento deve estar ativo
        result.IsSuccess.Should().BeTrue();
        result.Value.IsTruncated.Should().BeTrue();
        var totalNodes = result.Value.Services.Count + result.Value.Apis.Count;
        totalNodes.Should().BeLessThanOrEqualTo(2);
    }
}
