using NexTraceOne.Catalog.Application.Contracts.Abstractions;
using NexTraceOne.Catalog.Application.Graph.Abstractions;
using NexTraceOne.Catalog.Domain.Contracts.Entities;
using NexTraceOne.Catalog.Domain.Graph.Entities;

using AutoMapFeature = NexTraceOne.Catalog.Application.Contracts.Features.AutoMapDependenciesFromContracts.AutoMapDependenciesFromContracts;

namespace NexTraceOne.Catalog.Tests.Contracts.Application.Features;

/// <summary>
/// Testes da feature AutoMapDependenciesFromContracts — validação da descoberta
/// automática de dependências entre serviços a partir da análise de contratos.
/// </summary>
public sealed class AutoMapDependenciesTests
{
    private const string SpecWithCrossRef = """
        {
            "openapi": "3.1.0",
            "info": { "title": "Orders API", "version": "1.0.0" },
            "paths": {
                "/orders": {
                    "get": {
                        "responses": {
                            "200": { "description": "OK" }
                        }
                    }
                }
            },
            "components": {
                "schemas": {
                    "Order": {
                        "type": "object",
                        "properties": {
                            "customer": { "$ref": "#/components/schemas/PaymentService" }
                        }
                    }
                }
            }
        }
        """;

    private const string SpecNoRefs = """
        {
            "openapi": "3.1.0",
            "info": { "title": "Simple API", "version": "1.0.0" },
            "paths": {
                "/health": {
                    "get": {
                        "responses": { "200": { "description": "OK" } }
                    }
                }
            }
        }
        """;

    private static ServiceAsset CreateService(string name, string domain)
        => ServiceAsset.Create(name, domain, "team-default");

    [Fact]
    public async Task Should_Return_Zero_Dependencies_When_No_Contracts()
    {
        var service = CreateService("OrderService", "orders");

        var contractRepo = Substitute.For<IContractVersionRepository>();
        var serviceRepo = Substitute.For<IServiceAssetRepository>();
        var apiRepo = Substitute.For<IApiAssetRepository>();

        serviceRepo.ListAllAsync(Arg.Any<CancellationToken>())
            .Returns(new List<ServiceAsset> { service });
        apiRepo.ListByServiceIdAsync(service.Id, Arg.Any<CancellationToken>())
            .Returns(new List<ApiAsset>());

        var handler = new AutoMapFeature.Handler(contractRepo, serviceRepo, apiRepo);
        var result = await handler.Handle(new AutoMapFeature.Query(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TotalAnalyzed.Should().Be(0);
        result.Value.DependenciesDiscovered.Should().Be(0);
        result.Value.Dependencies.Should().BeEmpty();
    }

    [Fact]
    public async Task Should_Discover_Dependencies_From_Cross_Service_References()
    {
        var orderService = CreateService("OrderService", "orders");
        var paymentService = CreateService("PaymentService", "payments");

        var orderApi = ApiAsset.Register("OrdersApi", "/api/orders", "1.0.0", "Internal", orderService);
        var paymentApi = ApiAsset.Register("PaymentsApi", "/api/payments", "1.0.0", "Internal", paymentService);

        var contractResult = ContractVersion.Import(orderApi.Id.Value, "1.0.0", SpecWithCrossRef, "json", "upload");
        var contract = contractResult.Value;

        var contractRepo = Substitute.For<IContractVersionRepository>();
        var serviceRepo = Substitute.For<IServiceAssetRepository>();
        var apiRepo = Substitute.For<IApiAssetRepository>();

        serviceRepo.ListAllAsync(Arg.Any<CancellationToken>())
            .Returns(new List<ServiceAsset> { orderService, paymentService });
        apiRepo.ListByServiceIdAsync(orderService.Id, Arg.Any<CancellationToken>())
            .Returns(new List<ApiAsset> { orderApi });
        apiRepo.ListByServiceIdAsync(paymentService.Id, Arg.Any<CancellationToken>())
            .Returns(new List<ApiAsset> { paymentApi });
        contractRepo.ListByApiAssetIdsAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(new List<ContractVersion> { contract });

        var handler = new AutoMapFeature.Handler(contractRepo, serviceRepo, apiRepo);
        var result = await handler.Handle(new AutoMapFeature.Query(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TotalAnalyzed.Should().BeGreaterThan(0);
        result.Value.DependenciesDiscovered.Should().BeGreaterThan(0);
        result.Value.Dependencies.Should().Contain(d =>
            d.ProducerServiceName == "OrderService"
            && d.ConsumerServiceName == "PaymentService");
    }

    [Fact]
    public async Task Should_Filter_By_Single_Service_When_ServiceAssetId_Provided()
    {
        var orderService = CreateService("OrderService", "orders");
        var orderApi = ApiAsset.Register("OrdersApi", "/api/orders", "1.0.0", "Internal", orderService);

        var contractResult = ContractVersion.Import(orderApi.Id.Value, "1.0.0", SpecNoRefs, "json", "upload");
        var contract = contractResult.Value;

        var contractRepo = Substitute.For<IContractVersionRepository>();
        var serviceRepo = Substitute.For<IServiceAssetRepository>();
        var apiRepo = Substitute.For<IApiAssetRepository>();

        serviceRepo.GetByIdAsync(orderService.Id, Arg.Any<CancellationToken>())
            .Returns(orderService);
        apiRepo.ListByServiceIdAsync(orderService.Id, Arg.Any<CancellationToken>())
            .Returns(new List<ApiAsset> { orderApi });
        contractRepo.ListByApiAssetIdsAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(new List<ContractVersion> { contract });

        var handler = new AutoMapFeature.Handler(contractRepo, serviceRepo, apiRepo);
        var result = await handler.Handle(
            new AutoMapFeature.Query(orderService.Id.Value), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TotalAnalyzed.Should().Be(1);
        result.Value.DependenciesDiscovered.Should().Be(0);
    }
}
