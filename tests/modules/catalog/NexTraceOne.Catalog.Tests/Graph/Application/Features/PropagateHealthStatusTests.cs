using System.Linq;
using NexTraceOne.Catalog.Application.Graph.Abstractions;
using NexTraceOne.Catalog.Domain.Graph.Entities;
using NexTraceOne.Catalog.Domain.Graph.Enums;

using PropagateHealthStatusFeature = NexTraceOne.Catalog.Application.Graph.Features.PropagateHealthStatus.PropagateHealthStatus;

namespace NexTraceOne.Catalog.Tests.Graph.Application.Features;

/// <summary>
/// Testes do handler PropagateHealthStatus.
/// Cenários: propagação direta, transitiva, com MaxDepth, serviço não encontrado,
/// isolado sem consumidores, e status Unknown quando sem health records.
/// </summary>
public sealed class PropagateHealthStatusTests
{
    private static readonly DateTimeOffset FixedNow =
        new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    private static (ApiAsset Api, ServiceAsset Owner) CreateApiWithOwner(
        string serviceName, string domain = "Domain")
    {
        var owner = ServiceAsset.Create(serviceName, domain, "Team", Guid.NewGuid());
        var api = ApiAsset.Register(
            $"{serviceName}-api", $"/api/{serviceName}", "1.0.0", "Internal", owner);
        return (api, owner);
    }

    private static void AddConsumer(ApiAsset api, string consumerName)
    {
        var consumer = ConsumerAsset.Create(consumerName, "Service", "Production");
        var source = DiscoverySource.Create(
            "CatalogImport", $"catalog/{consumerName}.csv", FixedNow, 0.95m);
        api.MapConsumerRelationship(consumer, source, FixedNow);
    }

    private readonly IApiAssetRepository _apiRepo = Substitute.For<IApiAssetRepository>();
    private readonly INodeHealthRepository _healthRepo = Substitute.For<INodeHealthRepository>();

    [Fact]
    public async Task Handle_WhenServiceNotFound_ShouldReturnFailure()
    {
        _apiRepo.ListAllAsync(Arg.Any<CancellationToken>()).Returns(new List<ApiAsset>());
        _healthRepo.GetLatestByOverlayAsync(Arg.Any<OverlayMode>(), Arg.Any<CancellationToken>())
            .Returns(new List<NodeHealthRecord>());

        var sut = new PropagateHealthStatusFeature.Handler(_apiRepo, _healthRepo);
        var result = await sut.Handle(
            new PropagateHealthStatusFeature.Query("non-existent-service"),
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_WhenServiceHasDirectConsumers_ShouldPropagateAtDepth1()
    {
        // svc-payments (root) is consumed by svc-billing and svc-reports
        var (apiPayments, ownerPayments) = CreateApiWithOwner("svc-payments");
        AddConsumer(apiPayments, "svc-billing");
        AddConsumer(apiPayments, "svc-reports");

        _apiRepo.ListAllAsync(Arg.Any<CancellationToken>())
            .Returns(new List<ApiAsset> { apiPayments });

        _healthRepo.GetLatestByOverlayAsync(Arg.Any<OverlayMode>(), Arg.Any<CancellationToken>())
            .Returns(new List<NodeHealthRecord>());

        var sut = new PropagateHealthStatusFeature.Handler(_apiRepo, _healthRepo);
        var result = await sut.Handle(
            new PropagateHealthStatusFeature.Query("svc-payments"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.RootServiceName.Should().Be("svc-payments");
        result.Value.AffectedServicesCount.Should().Be(2);
        result.Value.AffectedNodes.Should().Contain(n =>
            n.ServiceName == "svc-billing" && n.PropagationDepth == 1);
        result.Value.AffectedNodes.Should().Contain(n =>
            n.ServiceName == "svc-reports" && n.PropagationDepth == 1);
        result.Value.RootHealthStatus.Should().Be("Unknown",
            "no health records exist for the service");
    }

    [Fact]
    public async Task Handle_WhenTransitiveDependencies_ShouldPropagateAtDepth2()
    {
        // svc-auth (root) → svc-api-gateway → svc-frontend
        var (apiAuth, _) = CreateApiWithOwner("svc-auth");
        var (apiGateway, _) = CreateApiWithOwner("svc-api-gateway");

        AddConsumer(apiAuth, "svc-api-gateway");    // depth 1
        AddConsumer(apiGateway, "svc-frontend");    // depth 2

        _apiRepo.ListAllAsync(Arg.Any<CancellationToken>())
            .Returns(new List<ApiAsset> { apiAuth, apiGateway });

        _healthRepo.GetLatestByOverlayAsync(Arg.Any<OverlayMode>(), Arg.Any<CancellationToken>())
            .Returns(new List<NodeHealthRecord>());

        var sut = new PropagateHealthStatusFeature.Handler(_apiRepo, _healthRepo);
        var result = await sut.Handle(
            new PropagateHealthStatusFeature.Query("svc-auth"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.AffectedServicesCount.Should().Be(2);

        var gatewayNode = result.Value.AffectedNodes
            .SingleOrDefault(n => n.ServiceName == "svc-api-gateway");
        gatewayNode.Should().NotBeNull();
        gatewayNode!.PropagationDepth.Should().Be(1);

        var frontendNode = result.Value.AffectedNodes
            .SingleOrDefault(n => n.ServiceName == "svc-frontend");
        frontendNode.Should().NotBeNull();
        frontendNode!.PropagationDepth.Should().Be(2);
        frontendNode.PropagationPath.Should().ContainInOrder(
            "svc-auth", "svc-api-gateway", "svc-frontend");
    }

    [Fact]
    public async Task Handle_WhenMaxDepthIsOne_ShouldOnlyReturnDirectConsumers()
    {
        // depth: svc-auth → svc-api-gateway(1) → svc-frontend(2)
        var (apiAuth, _) = CreateApiWithOwner("svc-auth");
        var (apiGateway, _) = CreateApiWithOwner("svc-api-gateway");

        AddConsumer(apiAuth, "svc-api-gateway");    // depth 1
        AddConsumer(apiGateway, "svc-frontend");    // depth 2 (should be excluded)

        _apiRepo.ListAllAsync(Arg.Any<CancellationToken>())
            .Returns(new List<ApiAsset> { apiAuth, apiGateway });

        _healthRepo.GetLatestByOverlayAsync(Arg.Any<OverlayMode>(), Arg.Any<CancellationToken>())
            .Returns(new List<NodeHealthRecord>());

        var sut = new PropagateHealthStatusFeature.Handler(_apiRepo, _healthRepo);
        var result = await sut.Handle(
            new PropagateHealthStatusFeature.Query("svc-auth", MaxDepth: 1),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.AffectedServicesCount.Should().Be(1,
            "MaxDepth=1 should only return direct consumers");
        result.Value.AffectedNodes.Should().ContainSingle(n => n.ServiceName == "svc-api-gateway");
        result.Value.AffectedNodes.Should().NotContain(n => n.ServiceName == "svc-frontend");
    }

    [Fact]
    public async Task Handle_WhenRootServiceHasNoConsumers_ShouldReturnEmptyAffected()
    {
        var (apiIsolated, _) = CreateApiWithOwner("svc-isolated");

        _apiRepo.ListAllAsync(Arg.Any<CancellationToken>())
            .Returns(new List<ApiAsset> { apiIsolated });

        _healthRepo.GetLatestByOverlayAsync(Arg.Any<OverlayMode>(), Arg.Any<CancellationToken>())
            .Returns(new List<NodeHealthRecord>());

        var sut = new PropagateHealthStatusFeature.Handler(_apiRepo, _healthRepo);
        var result = await sut.Handle(
            new PropagateHealthStatusFeature.Query("svc-isolated"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.AffectedServicesCount.Should().Be(0);
        result.Value.AffectedNodes.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_WhenHealthRecordExists_ShouldReflectRootStatus()
    {
        var (apiPayments, ownerPayments) = CreateApiWithOwner("svc-payments");
        AddConsumer(apiPayments, "svc-billing");

        var healthRecord = NodeHealthRecord.Create(
            ownerPayments.Id.Value,
            NodeType.Service,
            OverlayMode.Health,
            HealthStatus.Unhealthy,
            score: 0.1m,
            factorsJson: "{\"reason\":\"high error rate\"}",
            calculatedAt: FixedNow,
            sourceSystem: "AlertManager");

        _apiRepo.ListAllAsync(Arg.Any<CancellationToken>())
            .Returns(new List<ApiAsset> { apiPayments });

        _healthRepo.GetLatestByOverlayAsync(Arg.Any<OverlayMode>(), Arg.Any<CancellationToken>())
            .Returns(new List<NodeHealthRecord> { healthRecord });

        var sut = new PropagateHealthStatusFeature.Handler(_apiRepo, _healthRepo);
        var result = await sut.Handle(
            new PropagateHealthStatusFeature.Query("svc-payments"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.RootHealthStatus.Should().Be("Unhealthy",
            "health record indicates Unhealthy for svc-payments");
        result.Value.AffectedServicesCount.Should().Be(1);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public async Task Validator_WhenRootServiceNameEmpty_ShouldFail(string emptyName)
    {
        var validator = new PropagateHealthStatusFeature.Validator();
        var result = validator.Validate(new PropagateHealthStatusFeature.Query(emptyName));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "RootServiceName");
    }

    [Fact]
    public async Task Validator_WhenMaxDepthOutOfRange_ShouldFail()
    {
        var validator = new PropagateHealthStatusFeature.Validator();
        var result = validator.Validate(
            new PropagateHealthStatusFeature.Query("svc-valid", MaxDepth: 11));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "MaxDepth");
    }

    [Fact]
    public async Task Validator_WhenValidQuery_ShouldPass()
    {
        var validator = new PropagateHealthStatusFeature.Validator();
        var result = validator.Validate(
            new PropagateHealthStatusFeature.Query("svc-payments", MaxDepth: 3));

        result.IsValid.Should().BeTrue();
    }
}
