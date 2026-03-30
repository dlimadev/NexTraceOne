using NexTraceOne.Catalog.Application.Contracts.Abstractions;
using NexTraceOne.Catalog.Application.Graph.Abstractions;
using NexTraceOne.Catalog.Domain.Contracts.Entities;
using NexTraceOne.Catalog.Domain.Contracts.Enums;
using NexTraceOne.Catalog.Domain.Graph.Entities;
using NexTraceOne.Catalog.Domain.Graph.Enums;
using NexTraceOne.Catalog.Infrastructure.Graph.Services;

namespace NexTraceOne.Catalog.Tests.Graph.Infrastructure.Services;

/// <summary>
/// Testes unitários para CatalogGraphModuleService — contrato público cross-module
/// usado por Governance, ChangeGovernance e outros módulos para obter dados de serviços,
/// contratos e dependências por equipa.
/// </summary>
public sealed class CatalogGraphModuleServiceTests
{
    private readonly IApiAssetRepository _apiAssetRepository = Substitute.For<IApiAssetRepository>();
    private readonly IServiceAssetRepository _serviceAssetRepository = Substitute.For<IServiceAssetRepository>();
    private readonly IContractVersionRepository _contractVersionRepository = Substitute.For<IContractVersionRepository>();

    private CatalogGraphModuleService CreateSut() =>
        new(_apiAssetRepository, _serviceAssetRepository, _contractVersionRepository);

    // ── ApiAssetExistsAsync ──────────────────────────────────────────

    [Fact]
    public async Task ApiAssetExistsAsync_KnownId_ShouldReturnTrue()
    {
        var apiAssetId = Guid.NewGuid();
        var service = ServiceAsset.Create("svc-test", "Payments", "team-alpha");
        var apiAsset = ApiAsset.Register("test-api", "/api/test", "1.0.0", "Internal", service);
        _apiAssetRepository.GetByIdAsync(Arg.Any<ApiAssetId>(), Arg.Any<CancellationToken>())
            .Returns(apiAsset);

        var result = await CreateSut().ApiAssetExistsAsync(apiAssetId, CancellationToken.None);

        result.Should().BeTrue();
    }

    [Fact]
    public async Task ApiAssetExistsAsync_UnknownId_ShouldReturnFalse()
    {
        _apiAssetRepository.GetByIdAsync(Arg.Any<ApiAssetId>(), Arg.Any<CancellationToken>())
            .Returns((ApiAsset?)null);

        var result = await CreateSut().ApiAssetExistsAsync(Guid.NewGuid(), CancellationToken.None);

        result.Should().BeFalse();
    }

    // ── ServiceAssetExistsAsync ──────────────────────────────────────

    [Fact]
    public async Task ServiceAssetExistsAsync_KnownName_ShouldReturnTrue()
    {
        var service = ServiceAsset.Create("svc-payments", "Payments", "team-alpha");
        _serviceAssetRepository.GetByNameAsync("svc-payments", Arg.Any<CancellationToken>())
            .Returns(service);

        var result = await CreateSut().ServiceAssetExistsAsync("svc-payments", CancellationToken.None);

        result.Should().BeTrue();
    }

    // ── CountServicesByTeamAsync ─────────────────────────────────────

    [Fact]
    public async Task CountServicesByTeamAsync_ShouldDelegateToRepository()
    {
        _serviceAssetRepository.CountByTeamAsync("team-alpha", Arg.Any<CancellationToken>())
            .Returns(5);

        var result = await CreateSut().CountServicesByTeamAsync("team-alpha", CancellationToken.None);

        result.Should().Be(5);
    }

    // ── ListServicesByTeamAsync ──────────────────────────────────────

    [Fact]
    public async Task ListServicesByTeamAsync_ShouldReturnMappedServices()
    {
        var svcPayments = ServiceAsset.Create("svc-payments", "Payments", "team-alpha");
        var svcOrders = ServiceAsset.Create("svc-orders", "Orders", "team-alpha");
        svcOrders.UpdateDetails("svc-orders", "", ServiceType.RestApi, "", Criticality.Medium, LifecycleStatus.Active, ExposureType.External, "", "");
        var services = new List<ServiceAsset> { svcPayments, svcOrders };
        _serviceAssetRepository.ListByTeamAsync("team-alpha", Arg.Any<CancellationToken>())
            .Returns(services);

        var result = await CreateSut().ListServicesByTeamAsync("team-alpha", CancellationToken.None);

        result.Should().HaveCount(2);
        result[0].Name.Should().Be("svc-payments");
        result[0].Domain.Should().Be("Payments");
        result[0].Criticality.Should().Be("Medium");
        result[1].Name.Should().Be("svc-orders");
        result[1].OwnershipType.Should().Be("External");
    }

    [Fact]
    public async Task ListServicesByTeamAsync_NoServices_ShouldReturnEmptyList()
    {
        _serviceAssetRepository.ListByTeamAsync("team-unknown", Arg.Any<CancellationToken>())
            .Returns(new List<ServiceAsset>());

        var result = await CreateSut().ListServicesByTeamAsync("team-unknown", CancellationToken.None);

        result.Should().BeEmpty();
    }

    // ── ListContractsByTeamAsync ─────────────────────────────────────

    [Fact]
    public async Task ListContractsByTeamAsync_TeamWithContracts_ShouldReturnMappedContracts()
    {
        var service = ServiceAsset.Create("svc-payments", "Payments", "team-alpha");
        _serviceAssetRepository.ListByTeamAsync("team-alpha", Arg.Any<CancellationToken>())
            .Returns(new List<ServiceAsset> { service });

        var apiAsset = ApiAsset.Register("payments-api", "/api/payments", "1.0.0", "Internal", service);
        _apiAssetRepository.ListByServiceIdAsync(service.Id, Arg.Any<CancellationToken>())
            .Returns(new List<ApiAsset> { apiAsset });

        var contractVersionResult = ContractVersion.Import(
            apiAsset.Id.Value, "1.0.0", "{}", "json", "upload", ContractProtocol.OpenApi);
        _contractVersionRepository.ListByApiAssetIdsAsync(
            Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(new List<ContractVersion> { contractVersionResult.Value });

        var result = await CreateSut().ListContractsByTeamAsync("team-alpha", CancellationToken.None);

        result.Should().HaveCount(1);
        result[0].Type.Should().Be("OpenApi");
        result[0].Version.Should().Be("1.0.0");
        result[0].Status.Should().Be("Draft");
    }

    [Fact]
    public async Task ListContractsByTeamAsync_NoServices_ShouldReturnEmptyList()
    {
        _serviceAssetRepository.ListByTeamAsync("team-unknown", Arg.Any<CancellationToken>())
            .Returns(new List<ServiceAsset>());

        var result = await CreateSut().ListContractsByTeamAsync("team-unknown", CancellationToken.None);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task ListContractsByTeamAsync_NoApis_ShouldReturnEmptyList()
    {
        var service = ServiceAsset.Create("svc-empty", "Infra", "team-beta");
        _serviceAssetRepository.ListByTeamAsync("team-beta", Arg.Any<CancellationToken>())
            .Returns(new List<ServiceAsset> { service });
        _apiAssetRepository.ListByServiceIdAsync(service.Id, Arg.Any<CancellationToken>())
            .Returns(new List<ApiAsset>());

        var result = await CreateSut().ListContractsByTeamAsync("team-beta", CancellationToken.None);

        result.Should().BeEmpty();
    }

    // ── ListCrossTeamDependenciesAsync ───────────────────────────────

    [Fact]
    public async Task ListCrossTeamDependenciesAsync_NoServices_ShouldReturnEmpty()
    {
        _serviceAssetRepository.ListByTeamAsync("team-alpha", Arg.Any<CancellationToken>())
            .Returns(new List<ServiceAsset>());

        var result = await CreateSut().ListCrossTeamDependenciesAsync("team-alpha", CancellationToken.None);

        result.Should().BeEmpty();
    }

    // ── CountServicesByDomainAsync ───────────────────────────────────

    [Fact]
    public async Task CountServicesByDomainAsync_ShouldDelegateToRepository()
    {
        _serviceAssetRepository.CountByDomainAsync("Payments", Arg.Any<CancellationToken>())
            .Returns(3);

        var result = await CreateSut().CountServicesByDomainAsync("Payments", CancellationToken.None);

        result.Should().Be(3);
    }
}
