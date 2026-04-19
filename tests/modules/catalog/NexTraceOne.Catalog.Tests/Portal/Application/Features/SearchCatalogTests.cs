using NexTraceOne.Catalog.Application.Contracts.Abstractions;
using NexTraceOne.Catalog.Application.Graph.Abstractions;
using NexTraceOne.Catalog.Domain.Contracts.Entities;
using NexTraceOne.Catalog.Domain.Contracts.Enums;
using NexTraceOne.Catalog.Domain.Graph.Entities;

using SearchCatalogFeature = NexTraceOne.Catalog.Application.Portal.Features.SearchCatalog.SearchCatalog;

namespace NexTraceOne.Catalog.Tests.Portal.Application.Features;

/// <summary>
/// Testes unitários do handler SearchCatalog.
/// Valida combinação de resultados de contratos e serviços, paginação, filtros e facetas.
/// </summary>
public sealed class SearchCatalogTests
{
    private readonly IContractVersionRepository _contractRepo = Substitute.For<IContractVersionRepository>();
    private readonly IServiceAssetRepository _serviceRepo = Substitute.For<IServiceAssetRepository>();
    private readonly IApiAssetRepository _apiAssetRepo = Substitute.For<IApiAssetRepository>();
    private readonly SearchCatalogFeature.Handler _sut;

    public SearchCatalogTests()
    {
        _sut = new SearchCatalogFeature.Handler(_contractRepo, _serviceRepo, _apiAssetRepo);
    }

    [Fact]
    public async Task Handle_Should_ReturnContractsAndServices_When_BothRepositoriesHaveMatches()
    {
        var apiAssetId = Guid.NewGuid();
        var cv = ContractVersion.Import(apiAssetId, "1.0.0", "{}", "json", "test").Value!;
        _contractRepo
            .SearchAsync(null, null, null, "payment", 1, 20, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<(IReadOnlyList<ContractVersion>, int)>(([cv], 1)));

        var svc = ServiceAsset.Create("PaymentService", "billing", "payments-team");
        _serviceRepo
            .SearchAsync("payment", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<ServiceAsset>>([svc]));

        _apiAssetRepo
            .ListByApiAssetIdsAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyDictionary<Guid, ApiAsset>>(new Dictionary<Guid, ApiAsset>()));

        var query = new SearchCatalogFeature.Query("payment");
        var result = await _sut.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Items.Should().HaveCount(2);
        result.Value.Items.Should().Contain(i => i.EntityType == "Contract");
        result.Value.Items.Should().Contain(i => i.EntityType == "Service" && i.Name == "PaymentService");
    }

    [Fact]
    public async Task Handle_Should_ReturnEmptyList_When_NeitherRepositoryHasMatches()
    {
        _contractRepo
            .SearchAsync(null, null, null, "xyz-nothing", 1, 20, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<(IReadOnlyList<ContractVersion>, int)>(([], 0)));

        _serviceRepo
            .SearchAsync("xyz-nothing", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<ServiceAsset>>([]));

        _apiAssetRepo
            .ListByApiAssetIdsAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyDictionary<Guid, ApiAsset>>(new Dictionary<Guid, ApiAsset>()));

        var query = new SearchCatalogFeature.Query("xyz-nothing");
        var result = await _sut.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Items.Should().BeEmpty();
        result.Value.TotalCount.Should().Be(0);
    }

    [Fact]
    public async Task Handle_Should_BuildFacets_From_ContractResults()
    {
        var apiAssetId = Guid.NewGuid();
        var cv1 = ContractVersion.Import(apiAssetId, "1.0.0", "{}", "json", "test", ContractProtocol.OpenApi).Value!;
        var cv2 = ContractVersion.Import(Guid.NewGuid(), "2.0.0", "{}", "json", "test", ContractProtocol.AsyncApi).Value!;

        _contractRepo
            .SearchAsync(null, null, null, "api", 1, 20, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<(IReadOnlyList<ContractVersion>, int)>(([cv1, cv2], 2)));

        _serviceRepo
            .SearchAsync("api", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<ServiceAsset>>([]));

        _apiAssetRepo
            .ListByApiAssetIdsAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyDictionary<Guid, ApiAsset>>(new Dictionary<Guid, ApiAsset>()));

        var query = new SearchCatalogFeature.Query("api");
        var result = await _sut.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Facets.TypeCounts.Should().ContainKey("OpenApi");
        result.Value.Facets.TypeCounts.Should().ContainKey("AsyncApi");
        result.Value.Facets.TypeCounts["OpenApi"].Should().Be(1);
        result.Value.Facets.TypeCounts["AsyncApi"].Should().Be(1);
    }

    [Fact]
    public async Task Handle_Should_EnrichContractOwner_From_ApiAsset()
    {
        var apiAssetId = Guid.NewGuid();
        var cv = ContractVersion.Import(apiAssetId, "1.0.0", "{}", "json", "test").Value!;
        var ownerSvc = ServiceAsset.Create("PaymentService", "billing", "payments-team");
        var apiAsset = ApiAsset.Register("payments-api", "/payments/v1", "1.0", "Public", ownerSvc);

        _contractRepo
            .SearchAsync(null, null, null, "payment", 1, 20, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<(IReadOnlyList<ContractVersion>, int)>(([cv], 1)));

        _serviceRepo
            .SearchAsync("payment", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<ServiceAsset>>([]));

        var apiAssets = new Dictionary<Guid, ApiAsset> { [apiAssetId] = apiAsset };
        _apiAssetRepo
            .ListByApiAssetIdsAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyDictionary<Guid, ApiAsset>>(apiAssets));

        var query = new SearchCatalogFeature.Query("payment");
        var result = await _sut.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var contractItem = result.Value!.Items.Single(i => i.EntityType == "Contract");
        contractItem.Owner.Should().Be("payments-team");
    }

    [Fact]
    public async Task Handle_Should_LimitServiceResults_To_RemainingPageCapacity()
    {
        // Page size = 5, and contracts fill 3 slots → services are limited to 2
        var contracts = Enumerable.Range(0, 3)
            .Select(_ => ContractVersion.Import(Guid.NewGuid(), "1.0.0", "{}", "json", "test").Value!)
            .ToList();

        var services = Enumerable.Range(0, 5)
            .Select(i => ServiceAsset.Create($"Service{i}", "domain", "team"))
            .ToList();

        _contractRepo
            .SearchAsync(null, null, null, "service", 1, 5, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<(IReadOnlyList<ContractVersion>, int)>((contracts, 3)));

        _serviceRepo
            .SearchAsync("service", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<ServiceAsset>>(services));

        _apiAssetRepo
            .ListByApiAssetIdsAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyDictionary<Guid, ApiAsset>>(new Dictionary<Guid, ApiAsset>()));

        var query = new SearchCatalogFeature.Query("service", PageSize: 5);
        var result = await _sut.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        // 3 contracts + 2 services (capped to remaining page capacity)
        result.Value!.Items.Should().HaveCount(5);
        result.Value.Items.Count(i => i.EntityType == "Service").Should().Be(2);
    }

    [Fact]
    public async Task Handle_Should_FilterByProtocol_When_TypeFilterProvided()
    {
        var cv = ContractVersion.Import(Guid.NewGuid(), "1.0.0", "{}", "json", "test", ContractProtocol.AsyncApi).Value!;

        _contractRepo
            .SearchAsync(ContractProtocol.AsyncApi, null, null, "events", 1, 20, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<(IReadOnlyList<ContractVersion>, int)>(([cv], 1)));

        _serviceRepo
            .SearchAsync("events", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<ServiceAsset>>([]));

        _apiAssetRepo
            .ListByApiAssetIdsAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyDictionary<Guid, ApiAsset>>(new Dictionary<Guid, ApiAsset>()));

        var query = new SearchCatalogFeature.Query("events", TypeFilter: "AsyncApi");
        var result = await _sut.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _contractRepo.Received(1).SearchAsync(
            ContractProtocol.AsyncApi, null, null, "events", 1, 20, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_SetRelevanceScore_Correctly()
    {
        var cv = ContractVersion.Import(Guid.NewGuid(), "1.0.0", "{}", "json", "test").Value!;
        var svc = ServiceAsset.Create("OrderService", "commerce", "orders-team");

        _contractRepo
            .SearchAsync(null, null, null, "order", 1, 20, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<(IReadOnlyList<ContractVersion>, int)>(([cv], 1)));

        _serviceRepo
            .SearchAsync("order", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<ServiceAsset>>([svc]));

        _apiAssetRepo
            .ListByApiAssetIdsAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyDictionary<Guid, ApiAsset>>(new Dictionary<Guid, ApiAsset>()));

        var query = new SearchCatalogFeature.Query("order");
        var result = await _sut.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Items.Single(i => i.EntityType == "Contract").RelevanceScore.Should().Be(1.0);
        result.Value.Items.Single(i => i.EntityType == "Service").RelevanceScore.Should().Be(0.9);
    }
}
