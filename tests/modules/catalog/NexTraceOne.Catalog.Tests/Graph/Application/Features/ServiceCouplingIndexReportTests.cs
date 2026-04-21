using NSubstitute;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.Catalog.Application.Graph.Abstractions;
using NexTraceOne.Catalog.Application.Graph.Features.GetServiceCouplingIndexReport;
using NexTraceOne.Catalog.Domain.Graph.Entities;
using NexTraceOne.Catalog.Domain.Graph.Enums;

namespace NexTraceOne.Catalog.Tests.Graph.Application.Features;

/// <summary>
/// Testes unitários para Wave W.2 — GetServiceCouplingIndexReport.
/// Cobre: sem serviços, serviços isolados, hub services, fan-in/fan-out,
/// ArchitecturalRisk, IsolationRisk, CouplingIndex, distribuição, validator.
/// </summary>
public sealed class ServiceCouplingIndexReportTests
{
    private static readonly DateTimeOffset FixedNow = new(2026, 4, 22, 10, 0, 0, TimeSpan.Zero);

    private static IDateTimeProvider CreateClock()
    {
        var clock = Substitute.For<IDateTimeProvider>();
        clock.UtcNow.Returns(FixedNow);
        return clock;
    }

    private static ServiceAsset MakeService(string name, ServiceTierType tier = ServiceTierType.Standard)
    {
        var svc = ServiceAsset.Create(name, "domain", "team-a");
        svc.SetTier(tier);
        return svc;
    }

    private static ApiAsset MakeApi(ServiceAsset owner, params string[] consumerNames)
    {
        var api = ApiAsset.Register($"{owner.Name}-api", "/api", "1.0", "Internal", owner);
        var ds = DiscoverySource.Create("manual", Guid.NewGuid().ToString(), FixedNow, 1.0m);
        foreach (var name in consumerNames)
        {
            var consumer = ConsumerAsset.Create(name, "Service", "production");
            api.MapConsumerRelationship(consumer, ds, FixedNow);
        }
        return api;
    }

    private static GetServiceCouplingIndexReport.Handler CreateHandler(
        IReadOnlyList<ServiceAsset> services,
        IReadOnlyList<ApiAsset> apis)
    {
        var svcRepo = Substitute.For<IServiceAssetRepository>();
        svcRepo.ListAllAsync(Arg.Any<CancellationToken>()).Returns(services);

        var apiRepo = Substitute.For<IApiAssetRepository>();
        apiRepo.ListAllAsync(Arg.Any<CancellationToken>()).Returns(apis);

        return new GetServiceCouplingIndexReport.Handler(svcRepo, apiRepo, CreateClock());
    }

    // ── Empty: no services ────────────────────────────────────────────────

    [Fact]
    public async Task Handle_NoServices_ReturnsEmpty()
    {
        var handler = CreateHandler([], []);
        var result = await handler.Handle(new GetServiceCouplingIndexReport.Query(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(0, result.Value.TotalServicesAnalyzed);
        Assert.Empty(result.Value.AllServices);
    }

    // ── Single isolated service (no APIs) ─────────────────────────────────

    [Fact]
    public async Task Handle_SingleIsolatedService_IsIsolated()
    {
        var svc = MakeService("svc-a");
        var handler = CreateHandler([svc], []);

        var result = await handler.Handle(new GetServiceCouplingIndexReport.Query(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var entry = result.Value.AllServices.Single();
        Assert.Equal(GetServiceCouplingIndexReport.CouplingTier.Isolated, entry.CouplingTier);
        Assert.Equal(0, entry.FanIn);
        Assert.Equal(0, entry.FanOut);
        Assert.Equal(0m, entry.CouplingIndex);
    }

    // ── IsolationRisk: isolated Standard service ──────────────────────────

    [Fact]
    public async Task Handle_IsolatedStandardService_SetsIsolationRisk()
    {
        var svc = MakeService("svc-isolated", ServiceTierType.Standard);
        var handler = CreateHandler([svc], []);

        var result = await handler.Handle(new GetServiceCouplingIndexReport.Query(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var entry = result.Value.AllServices.Single();
        Assert.True(entry.IsolationRisk);
        Assert.Equal(1, result.Value.Distribution.IsolatedCount);
    }

    // ── IsolationRisk: Experimental isolated service does NOT flag ─────────

    [Fact]
    public async Task Handle_IsolatedExperimentalService_NoIsolationRisk()
    {
        var svc = MakeService("svc-exp", ServiceTierType.Experimental);
        var handler = CreateHandler([svc], []);

        var result = await handler.Handle(new GetServiceCouplingIndexReport.Query(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.False(result.Value.AllServices.Single().IsolationRisk);
    }

    // ── FanIn: service with consumers → LooselyCoupled ───────────────────

    [Fact]
    public async Task Handle_ServiceWithOneConsumer_HasFanIn1()
    {
        var svcA = MakeService("svc-a");
        var api = MakeApi(svcA, "svc-b");

        var handler = CreateHandler([svcA], [api]);
        var result = await handler.Handle(new GetServiceCouplingIndexReport.Query(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var entry = result.Value.AllServices.Single(e => e.ServiceName == "svc-a");
        Assert.Equal(1, entry.FanIn);
        Assert.Equal(0, entry.FanOut);
        Assert.True(entry.CouplingIndex > 0m);
    }

    // ── FanOut: consumer service depends on another ────────────────────────

    [Fact]
    public async Task Handle_ConsumerService_HasFanOut()
    {
        var svcProvider = MakeService("svc-provider");
        var svcConsumer = MakeService("svc-consumer");
        var api = MakeApi(svcProvider, "svc-consumer");

        var handler = CreateHandler([svcProvider, svcConsumer], [api]);
        var result = await handler.Handle(new GetServiceCouplingIndexReport.Query(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var consumer = result.Value.AllServices.Single(e => e.ServiceName == "svc-consumer");
        Assert.Equal(0, consumer.FanIn);
        Assert.Equal(1, consumer.FanOut);
    }

    // ── HubService: high fan-in + Critical tier → ArchitecturalRisk ───────

    [Fact]
    public async Task Handle_HubServiceCriticalTier_SetsArchitecturalRisk()
    {
        var hub = MakeService("hub-svc", ServiceTierType.Critical);

        // Create 5 distinct consumers to trigger high fan-in
        var consumers = Enumerable.Range(1, 8).Select(i => $"consumer-{i}").ToArray();
        var api = MakeApi(hub, consumers);

        var handler = CreateHandler([hub], [api]);
        var result = await handler.Handle(new GetServiceCouplingIndexReport.Query(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var entry = result.Value.AllServices.Single();
        Assert.Equal(8, entry.FanIn);
        Assert.True(entry.ArchitecturalRisk);
        Assert.Equal(GetServiceCouplingIndexReport.CouplingTier.HubService, entry.CouplingTier);
        Assert.NotEmpty(result.Value.TopHubServices);
    }

    // ── HubService Standard tier: no ArchitecturalRisk ────────────────────

    [Fact]
    public async Task Handle_HubServiceStandardTier_NoArchitecturalRisk()
    {
        var hub = MakeService("hub-std", ServiceTierType.Standard);
        var consumers = Enumerable.Range(1, 8).Select(i => $"consumer-{i}").ToArray();
        var api = MakeApi(hub, consumers);

        var handler = CreateHandler([hub], [api]);
        var result = await handler.Handle(new GetServiceCouplingIndexReport.Query(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        // No ArchitecturalRisk since tier is not Critical
        Assert.False(result.Value.AllServices.Single().ArchitecturalRisk);
    }

    // ── Distribution: multiple tiers ──────────────────────────────────────

    [Fact]
    public async Task Handle_MultipleServices_DistributionIsCorrect()
    {
        var isolated = MakeService("isolated");
        var provider = MakeService("provider");
        var api = MakeApi(provider, "consumer-1", "consumer-2");

        var handler = CreateHandler([isolated, provider], [api]);
        var result = await handler.Handle(new GetServiceCouplingIndexReport.Query(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value.TotalServicesAnalyzed);
        Assert.Equal(1, result.Value.Distribution.IsolatedCount);
    }

    // ── AvgCouplingIndex ──────────────────────────────────────────────────

    [Fact]
    public async Task Handle_MultipleServices_AvgCouplingIndexCalculated()
    {
        var svcA = MakeService("svc-a");
        var svcB = MakeService("svc-b");
        var api = MakeApi(svcA, "svc-b");

        var handler = CreateHandler([svcA, svcB], [api]);
        var result = await handler.Handle(new GetServiceCouplingIndexReport.Query(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.True(result.Value.AvgCouplingIndex >= 0m);
    }

    // ── IsolatedServicePct with all isolated ──────────────────────────────

    [Fact]
    public async Task Handle_AllServicesIsolated_IsolatedPct100()
    {
        var svcs = new[] { MakeService("a"), MakeService("b") };
        var handler = CreateHandler(svcs, []);

        var result = await handler.Handle(new GetServiceCouplingIndexReport.Query(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(100m, result.Value.IsolatedServicePct);
    }

    // ── Validator ─────────────────────────────────────────────────────────

    [Fact]
    public void Validator_MaxTopServicesOutOfRange_Fails()
    {
        var v = new GetServiceCouplingIndexReport.Validator();
        var result = v.Validate(new GetServiceCouplingIndexReport.Query(MaxTopServices: 0));
        Assert.False(result.IsValid);
    }

    [Fact]
    public void Validator_DefaultQuery_IsValid()
    {
        var v = new GetServiceCouplingIndexReport.Validator();
        var result = v.Validate(new GetServiceCouplingIndexReport.Query());
        Assert.True(result.IsValid);
    }

    // ── TopHighlyCoupled by fan-out ────────────────────────────────────────

    [Fact]
    public async Task Handle_ConsumerWithHighFanOut_AppearsInTopHighlyCoupled()
    {
        var pA = MakeService("provider-a");
        var pB = MakeService("provider-b");
        var consumer = MakeService("consumer");

        var apiA = MakeApi(pA, "consumer");
        var apiB = MakeApi(pB, "consumer");

        var handler = CreateHandler([pA, pB, consumer], [apiA, apiB]);
        var result = await handler.Handle(new GetServiceCouplingIndexReport.Query(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var consumerEntry = result.Value.AllServices.Single(e => e.ServiceName == "consumer");
        Assert.Equal(2, consumerEntry.FanOut);
        Assert.NotEmpty(result.Value.TopHighlyCoupledServices);
    }
}
