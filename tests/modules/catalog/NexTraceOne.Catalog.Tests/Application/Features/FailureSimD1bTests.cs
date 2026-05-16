using NexTraceOne.Catalog.Application.Graph.Abstractions;
using NexTraceOne.Catalog.Domain.Graph.Entities;
using NexTraceOne.Catalog.Domain.Graph.Enums;
using SimulateServiceFailureImpactFeature = NexTraceOne.Catalog.Application.Graph.Features.SimulateServiceFailureImpact.SimulateServiceFailureImpact;

namespace NexTraceOne.Catalog.Tests.Application.Features;

/// <summary>
/// Testes unitários para Wave D.1.b — SimulateServiceFailureImpact.
/// Verifica propagação de impacto transitivo, detecção de ciclos e cálculo de cascade risk.
/// </summary>
public sealed class FailureSimD1bTests
{
    private static readonly DateTimeOffset FixedNow = new(2025, 6, 1, 10, 0, 0, TimeSpan.Zero);

    private static ServiceAsset MakeService(string name = "order-service", string team = "TeamA", ServiceTierType tier = ServiceTierType.Standard)
    {
        var s = ServiceAsset.Create(name, "Core", team, Guid.NewGuid());
        s.SetTier(tier);
        return s;
    }

    private static ApiAsset MakeApi(ServiceAsset owner, string name = "orders-api")
        => ApiAsset.Register(name, "/api/v1/orders", "1.0.0", "Internal", owner);

    private static ConsumerAsset MakeConsumerAsset(string name = "billing-service")
        => ConsumerAsset.Create(name, "Service", "production");

    private static DiscoverySource MakeDiscoverySource(decimal confidence = 0.9m)
        => DiscoverySource.Create("OpenTelemetry", "ref-1", FixedNow, confidence);

    // ── Tests ────────────────────────────────────────────────────────────

    [Fact]
    public async Task SimulateServiceFailureImpact_Returns_NotFound_For_Unknown_Service()
    {
        var serviceRepo = Substitute.For<IServiceAssetRepository>();
        var apiRepo = Substitute.For<IApiAssetRepository>();
        serviceRepo.GetByIdAsync(Arg.Any<ServiceAssetId>(), Arg.Any<CancellationToken>()).Returns((ServiceAsset?)null);

        var handler = new SimulateServiceFailureImpactFeature.Handler(serviceRepo, apiRepo);
        var result = await handler.Handle(new SimulateServiceFailureImpactFeature.Query(Guid.NewGuid()), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("ServiceAsset");
    }

    [Fact]
    public async Task SimulateServiceFailureImpact_Returns_Empty_Impact_When_No_Apis()
    {
        var service = MakeService();
        var serviceRepo = Substitute.For<IServiceAssetRepository>();
        var apiRepo = Substitute.For<IApiAssetRepository>();
        serviceRepo.GetByIdAsync(Arg.Any<ServiceAssetId>(), Arg.Any<CancellationToken>()).Returns(service);
        serviceRepo.ListAllAsync(Arg.Any<CancellationToken>()).Returns((IReadOnlyList<ServiceAsset>)[service]);
        apiRepo.ListAllAsync(Arg.Any<CancellationToken>()).Returns((IReadOnlyList<ApiAsset>)[]);

        var handler = new SimulateServiceFailureImpactFeature.Handler(serviceRepo, apiRepo);
        var result = await handler.Handle(new SimulateServiceFailureImpactFeature.Query(service.Id.Value), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TotalImpacted.Should().Be(0);
        result.Value.ExposedApisCount.Should().Be(0);
    }

    [Fact]
    public async Task SimulateServiceFailureImpact_Returns_Direct_Consumers()
    {
        var service = MakeService();
        var api = MakeApi(service);
        var consumer = MakeConsumerAsset("billing-service");
        api.MapConsumerRelationship(consumer, MakeDiscoverySource(), FixedNow);

        var serviceRepo = Substitute.For<IServiceAssetRepository>();
        var apiRepo = Substitute.For<IApiAssetRepository>();
        serviceRepo.GetByIdAsync(Arg.Any<ServiceAssetId>(), Arg.Any<CancellationToken>()).Returns(service);
        serviceRepo.ListAllAsync(Arg.Any<CancellationToken>()).Returns((IReadOnlyList<ServiceAsset>)[service]);
        apiRepo.ListAllAsync(Arg.Any<CancellationToken>()).Returns((IReadOnlyList<ApiAsset>)[api]);

        var handler = new SimulateServiceFailureImpactFeature.Handler(serviceRepo, apiRepo);
        var result = await handler.Handle(new SimulateServiceFailureImpactFeature.Query(service.Id.Value), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TotalImpacted.Should().Be(1);
        result.Value.DirectImpactCount.Should().Be(1);
        result.Value.ImpactedNodes.Should().ContainSingle(n => n.ConsumerName == "billing-service");
    }

    [Fact]
    public async Task SimulateServiceFailureImpact_Returns_Correct_CascadeRisk_For_Critical_Tier()
    {
        var service = MakeService(tier: ServiceTierType.Critical);
        var apis = Enumerable.Range(0, 5).Select(i =>
        {
            var api = MakeApi(service, $"api-{i}");
            api.MapConsumerRelationship(MakeConsumerAsset($"consumer-{i}"), MakeDiscoverySource(), FixedNow);
            return api;
        }).ToList();

        var serviceRepo = Substitute.For<IServiceAssetRepository>();
        var apiRepo = Substitute.For<IApiAssetRepository>();
        serviceRepo.GetByIdAsync(Arg.Any<ServiceAssetId>(), Arg.Any<CancellationToken>()).Returns(service);
        serviceRepo.ListAllAsync(Arg.Any<CancellationToken>()).Returns((IReadOnlyList<ServiceAsset>)[service]);
        apiRepo.ListAllAsync(Arg.Any<CancellationToken>()).Returns((IReadOnlyList<ApiAsset>)apis);

        var handler = new SimulateServiceFailureImpactFeature.Handler(serviceRepo, apiRepo);
        var result = await handler.Handle(new SimulateServiceFailureImpactFeature.Query(service.Id.Value), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.CascadeRisk.Should().Be("critical");
    }

    [Fact]
    public async Task SimulateServiceFailureImpact_Returns_Correct_CascadeRisk_For_Standard_Tier_Many()
    {
        var service = MakeService(tier: ServiceTierType.Standard);
        var apis = Enumerable.Range(0, 10).Select(i =>
        {
            var api = MakeApi(service, $"api-std-{i}");
            api.MapConsumerRelationship(MakeConsumerAsset($"consumer-std-{i}"), MakeDiscoverySource(), FixedNow);
            return api;
        }).ToList();

        var serviceRepo = Substitute.For<IServiceAssetRepository>();
        var apiRepo = Substitute.For<IApiAssetRepository>();
        serviceRepo.GetByIdAsync(Arg.Any<ServiceAssetId>(), Arg.Any<CancellationToken>()).Returns(service);
        serviceRepo.ListAllAsync(Arg.Any<CancellationToken>()).Returns((IReadOnlyList<ServiceAsset>)[service]);
        apiRepo.ListAllAsync(Arg.Any<CancellationToken>()).Returns((IReadOnlyList<ApiAsset>)apis);

        var handler = new SimulateServiceFailureImpactFeature.Handler(serviceRepo, apiRepo);
        var result = await handler.Handle(new SimulateServiceFailureImpactFeature.Query(service.Id.Value), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.CascadeRisk.Should().Be("high");
    }

    [Fact]
    public async Task SimulateServiceFailureImpact_Returns_Low_Risk_For_Experimental()
    {
        var service = MakeService(tier: ServiceTierType.Experimental);
        var api = MakeApi(service);
        api.MapConsumerRelationship(MakeConsumerAsset("only-consumer"), MakeDiscoverySource(), FixedNow);

        var serviceRepo = Substitute.For<IServiceAssetRepository>();
        var apiRepo = Substitute.For<IApiAssetRepository>();
        serviceRepo.GetByIdAsync(Arg.Any<ServiceAssetId>(), Arg.Any<CancellationToken>()).Returns(service);
        serviceRepo.ListAllAsync(Arg.Any<CancellationToken>()).Returns((IReadOnlyList<ServiceAsset>)[service]);
        apiRepo.ListAllAsync(Arg.Any<CancellationToken>()).Returns((IReadOnlyList<ApiAsset>)[api]);

        var handler = new SimulateServiceFailureImpactFeature.Handler(serviceRepo, apiRepo);
        var result = await handler.Handle(new SimulateServiceFailureImpactFeature.Query(service.Id.Value), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.CascadeRisk.Should().Be("low");
    }

    [Fact]
    public async Task SimulateServiceFailureImpact_MaxDepth_One_Returns_Only_Direct()
    {
        var service = MakeService();
        var api = MakeApi(service);
        var consumerA = MakeConsumerAsset("consumer-a");
        api.MapConsumerRelationship(consumerA, MakeDiscoverySource(), FixedNow);

        // consumer-a also has an API with its own consumer
        var consumerService = MakeService("consumer-a", "TeamB");
        var consumerApi = MakeApi(consumerService, "consumer-a-api");
        var consumerB = MakeConsumerAsset("consumer-b");
        consumerApi.MapConsumerRelationship(consumerB, MakeDiscoverySource(), FixedNow);

        var serviceRepo = Substitute.For<IServiceAssetRepository>();
        var apiRepo = Substitute.For<IApiAssetRepository>();
        serviceRepo.GetByIdAsync(Arg.Any<ServiceAssetId>(), Arg.Any<CancellationToken>()).Returns(service);
        serviceRepo.ListAllAsync(Arg.Any<CancellationToken>()).Returns((IReadOnlyList<ServiceAsset>)[service, consumerService]);
        apiRepo.ListAllAsync(Arg.Any<CancellationToken>()).Returns((IReadOnlyList<ApiAsset>)[api, consumerApi]);

        var handler = new SimulateServiceFailureImpactFeature.Handler(serviceRepo, apiRepo);
        var result = await handler.Handle(new SimulateServiceFailureImpactFeature.Query(service.Id.Value, MaxDepth: 1), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TotalImpacted.Should().Be(1);
        result.Value.DirectImpactCount.Should().Be(1);
        result.Value.TransitiveImpactCount.Should().Be(0);
    }

    [Fact]
    public async Task SimulateServiceFailureImpact_Avoids_Circular_Dependency()
    {
        var serviceA = MakeService("service-a", "TeamA");
        var serviceB = MakeService("service-b", "TeamB");

        var apiA = MakeApi(serviceA, "api-a");
        var consumerB = MakeConsumerAsset("service-b");
        apiA.MapConsumerRelationship(consumerB, MakeDiscoverySource(), FixedNow);

        var apiB = MakeApi(serviceB, "api-b");
        var consumerA = MakeConsumerAsset("service-a");
        apiB.MapConsumerRelationship(consumerA, MakeDiscoverySource(), FixedNow);

        var serviceRepo = Substitute.For<IServiceAssetRepository>();
        var apiRepo = Substitute.For<IApiAssetRepository>();
        serviceRepo.GetByIdAsync(Arg.Any<ServiceAssetId>(), Arg.Any<CancellationToken>()).Returns(serviceA);
        serviceRepo.ListAllAsync(Arg.Any<CancellationToken>()).Returns((IReadOnlyList<ServiceAsset>)[serviceA, serviceB]);
        apiRepo.ListAllAsync(Arg.Any<CancellationToken>()).Returns((IReadOnlyList<ApiAsset>)[apiA, apiB]);

        var handler = new SimulateServiceFailureImpactFeature.Handler(serviceRepo, apiRepo);

        // Should not throw (no infinite recursion) and should terminate
        var result = await handler.Handle(new SimulateServiceFailureImpactFeature.Query(serviceA.Id.Value, MaxDepth: 5), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task SimulateServiceFailureImpact_Direct_Count_Correct()
    {
        var service = MakeService();
        var api1 = MakeApi(service, "api-1");
        var api2 = MakeApi(service, "api-2");
        api1.MapConsumerRelationship(MakeConsumerAsset("consumer-1"), MakeDiscoverySource(), FixedNow);
        api2.MapConsumerRelationship(MakeConsumerAsset("consumer-2"), MakeDiscoverySource(), FixedNow);

        var serviceRepo = Substitute.For<IServiceAssetRepository>();
        var apiRepo = Substitute.For<IApiAssetRepository>();
        serviceRepo.GetByIdAsync(Arg.Any<ServiceAssetId>(), Arg.Any<CancellationToken>()).Returns(service);
        serviceRepo.ListAllAsync(Arg.Any<CancellationToken>()).Returns((IReadOnlyList<ServiceAsset>)[service]);
        apiRepo.ListAllAsync(Arg.Any<CancellationToken>()).Returns((IReadOnlyList<ApiAsset>)[api1, api2]);

        var handler = new SimulateServiceFailureImpactFeature.Handler(serviceRepo, apiRepo);
        var result = await handler.Handle(new SimulateServiceFailureImpactFeature.Query(service.Id.Value), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.DirectImpactCount.Should().Be(2);
        result.Value.TransitiveImpactCount.Should().Be(0);
    }

    [Fact]
    public async Task SimulateServiceFailureImpact_Transitive_Count_Correct()
    {
        var service = MakeService();
        var api = MakeApi(service);
        var consumerSvc = MakeService("consumer-a", "TeamB");
        api.MapConsumerRelationship(MakeConsumerAsset("consumer-a"), MakeDiscoverySource(), FixedNow);

        var consumerApi = MakeApi(consumerSvc, "consumer-api");
        consumerApi.MapConsumerRelationship(MakeConsumerAsset("consumer-b"), MakeDiscoverySource(), FixedNow);

        var serviceRepo = Substitute.For<IServiceAssetRepository>();
        var apiRepo = Substitute.For<IApiAssetRepository>();
        serviceRepo.GetByIdAsync(Arg.Any<ServiceAssetId>(), Arg.Any<CancellationToken>()).Returns(service);
        serviceRepo.ListAllAsync(Arg.Any<CancellationToken>()).Returns((IReadOnlyList<ServiceAsset>)[service, consumerSvc]);
        apiRepo.ListAllAsync(Arg.Any<CancellationToken>()).Returns((IReadOnlyList<ApiAsset>)[api, consumerApi]);

        var handler = new SimulateServiceFailureImpactFeature.Handler(serviceRepo, apiRepo);
        var result = await handler.Handle(new SimulateServiceFailureImpactFeature.Query(service.Id.Value, MaxDepth: 3), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.DirectImpactCount.Should().Be(1);
        result.Value.TransitiveImpactCount.Should().Be(1);
        result.Value.TotalImpacted.Should().Be(2);
    }

    [Fact]
    public async Task SimulateServiceFailureImpact_Service_With_No_Consumers_Has_Zero_Impact()
    {
        var service = MakeService();
        var api = MakeApi(service);
        // no consumer relationships mapped

        var serviceRepo = Substitute.For<IServiceAssetRepository>();
        var apiRepo = Substitute.For<IApiAssetRepository>();
        serviceRepo.GetByIdAsync(Arg.Any<ServiceAssetId>(), Arg.Any<CancellationToken>()).Returns(service);
        serviceRepo.ListAllAsync(Arg.Any<CancellationToken>()).Returns((IReadOnlyList<ServiceAsset>)[service]);
        apiRepo.ListAllAsync(Arg.Any<CancellationToken>()).Returns((IReadOnlyList<ApiAsset>)[api]);

        var handler = new SimulateServiceFailureImpactFeature.Handler(serviceRepo, apiRepo);
        var result = await handler.Handle(new SimulateServiceFailureImpactFeature.Query(service.Id.Value), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TotalImpacted.Should().Be(0);
        result.Value.ExposedApisCount.Should().Be(1);
        result.Value.CascadeRisk.Should().Be("low");
    }

    [Fact]
    public void FailureImpactNode_CascadeRisk_Labels_Correct()
    {
        var directNode = new SimulateServiceFailureImpactFeature.FailureImpactNode(
            "svc-a", null, "TeamA", 1, "OpenTelemetry", 0.9m, "direct");
        var transitiveNode = new SimulateServiceFailureImpactFeature.FailureImpactNode(
            "svc-b", null, "TeamB", 2, "OpenTelemetry", 0.8m, "transitive");

        directNode.CascadeRisk.Should().Be("direct");
        transitiveNode.CascadeRisk.Should().Be("transitive");
        directNode.Depth.Should().Be(1);
        transitiveNode.Depth.Should().Be(2);
    }
}
