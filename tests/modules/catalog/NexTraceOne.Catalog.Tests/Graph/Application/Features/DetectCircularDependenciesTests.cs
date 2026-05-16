using System.Linq;
using NexTraceOne.Catalog.Application.Graph.Abstractions;
using NexTraceOne.Catalog.Domain.Graph.Entities;
using NexTraceOne.Catalog.Domain.Graph.Enums;

using DetectCircularDependenciesFeature = NexTraceOne.Catalog.Application.Graph.Features.DetectCircularDependencies.DetectCircularDependencies;

namespace NexTraceOne.Catalog.Tests.Graph.Application.Features;

/// <summary>
/// Testes do handler DetectCircularDependencies.
/// Cenários: grafo sem ciclos, ciclo simples A→B→A, ciclo transitivo A→B→C→A,
/// múltiplos ciclos independentes, grafo vazio, e filtro por serviço.
/// </summary>
public sealed class DetectCircularDependenciesTests
{
    private static readonly DateTimeOffset FixedNow =
        new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    private static (ApiAsset Api, ServiceAsset Owner) CreateApiWithOwner(string serviceName, string domain = "Domain")
    {
        var owner = ServiceAsset.Create(serviceName, domain, "Team", Guid.NewGuid());
        var api = ApiAsset.Register($"{serviceName}-api", $"/api/{serviceName}", "1.0.0", "Internal", owner);
        return (api, owner);
    }

    private static void AddConsumer(ApiAsset api, string consumerName)
    {
        var consumer = ConsumerAsset.Create(consumerName, "Service", "Production");
        var source = DiscoverySource.Create("CatalogImport", $"catalog/{consumerName}.csv", FixedNow, 0.95m);
        api.MapConsumerRelationship(consumer, source, FixedNow);
    }

    private readonly IApiAssetRepository _apiRepo = Substitute.For<IApiAssetRepository>();

    [Fact]
    public async Task Handle_WhenNoCycles_ShouldReturnFalse()
    {
        // svc-a → svc-b → svc-c (linear, sem ciclo)
        var (apiA, _) = CreateApiWithOwner("svc-a");
        var (apiB, _) = CreateApiWithOwner("svc-b");
        var (apiC, _) = CreateApiWithOwner("svc-c");

        // svc-b consumes svc-a's api
        AddConsumer(apiA, "svc-b");
        // svc-c consumes svc-b's api
        AddConsumer(apiB, "svc-c");

        _apiRepo.ListAllAsync(Arg.Any<CancellationToken>())
            .Returns(new List<ApiAsset> { apiA, apiB, apiC });

        var sut = new DetectCircularDependenciesFeature.Handler(_apiRepo);
        var result = await sut.Handle(new DetectCircularDependenciesFeature.Query(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.CircularDependenciesFound.Should().BeFalse();
        result.Value.Cycles.Should().BeEmpty();
        result.Value.TotalServicesAnalyzed.Should().Be(3);
    }

    [Fact]
    public async Task Handle_WhenSimpleDirectCycle_ShouldDetectIt()
    {
        // svc-a publishes api, svc-b consumes it
        // svc-b publishes api, svc-a consumes it  → svc-a ↔ svc-b (cycle)
        var (apiA, _) = CreateApiWithOwner("svc-a");
        var (apiB, _) = CreateApiWithOwner("svc-b");

        AddConsumer(apiA, "svc-b"); // svc-b depends on svc-a
        AddConsumer(apiB, "svc-a"); // svc-a depends on svc-b → CYCLE

        _apiRepo.ListAllAsync(Arg.Any<CancellationToken>())
            .Returns(new List<ApiAsset> { apiA, apiB });

        var sut = new DetectCircularDependenciesFeature.Handler(_apiRepo);
        var result = await sut.Handle(new DetectCircularDependenciesFeature.Query(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.CircularDependenciesFound.Should().BeTrue();
        result.Value.Cycles.Should().NotBeEmpty();

        var cycle = result.Value.Cycles.First();
        cycle.CycleLength.Should().Be(2);
        cycle.Participants.Should().Contain("svc-a");
        cycle.Participants.Should().Contain("svc-b");
        cycle.Description.Should().Contain("circular");
    }

    [Fact]
    public async Task Handle_WhenTransitiveCycle_ShouldDetectAllParticipants()
    {
        // svc-a → svc-b → svc-c → svc-a (triangle cycle)
        var (apiA, _) = CreateApiWithOwner("svc-a");
        var (apiB, _) = CreateApiWithOwner("svc-b");
        var (apiC, _) = CreateApiWithOwner("svc-c");

        AddConsumer(apiA, "svc-b"); // svc-b depends on svc-a
        AddConsumer(apiB, "svc-c"); // svc-c depends on svc-b
        AddConsumer(apiC, "svc-a"); // svc-a depends on svc-c → CYCLE

        _apiRepo.ListAllAsync(Arg.Any<CancellationToken>())
            .Returns(new List<ApiAsset> { apiA, apiB, apiC });

        var sut = new DetectCircularDependenciesFeature.Handler(_apiRepo);
        var result = await sut.Handle(new DetectCircularDependenciesFeature.Query(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.CircularDependenciesFound.Should().BeTrue();
        result.Value.Cycles.Should().HaveCount(1);

        var cycle = result.Value.Cycles.First();
        cycle.CycleLength.Should().Be(3);
        cycle.Participants.Should().Contain("svc-a");
        cycle.Participants.Should().Contain("svc-b");
        cycle.Participants.Should().Contain("svc-c");
    }

    [Fact]
    public async Task Handle_WhenEmptyGraph_ShouldReturnNoCycles()
    {
        _apiRepo.ListAllAsync(Arg.Any<CancellationToken>())
            .Returns(new List<ApiAsset>());

        var sut = new DetectCircularDependenciesFeature.Handler(_apiRepo);
        var result = await sut.Handle(new DetectCircularDependenciesFeature.Query(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.CircularDependenciesFound.Should().BeFalse();
        result.Value.TotalServicesAnalyzed.Should().Be(0);
    }

    [Fact]
    public async Task Handle_WhenSingleServiceNoConsumers_ShouldReturnNoCycles()
    {
        var (apiA, _) = CreateApiWithOwner("svc-a");

        _apiRepo.ListAllAsync(Arg.Any<CancellationToken>())
            .Returns(new List<ApiAsset> { apiA });

        var sut = new DetectCircularDependenciesFeature.Handler(_apiRepo);
        var result = await sut.Handle(new DetectCircularDependenciesFeature.Query(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.CircularDependenciesFound.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_WithServiceNameFilter_ShouldLimitAnalysisToSubgraph()
    {
        // Two independent components: svc-a↔svc-b (cycle) and svc-c↔svc-d (cycle)
        var (apiA, _) = CreateApiWithOwner("svc-a");
        var (apiB, _) = CreateApiWithOwner("svc-b");
        var (apiC, _) = CreateApiWithOwner("svc-c");
        var (apiD, _) = CreateApiWithOwner("svc-d");

        AddConsumer(apiA, "svc-b");
        AddConsumer(apiB, "svc-a"); // cycle in svc-a subgraph
        AddConsumer(apiC, "svc-d");
        AddConsumer(apiD, "svc-c"); // cycle in svc-c subgraph

        _apiRepo.ListAllAsync(Arg.Any<CancellationToken>())
            .Returns(new List<ApiAsset> { apiA, apiB, apiC, apiD });

        var sut = new DetectCircularDependenciesFeature.Handler(_apiRepo);

        // Filter to svc-a only — should only see the apis owned by svc-a
        var result = await sut.Handle(
            new DetectCircularDependenciesFeature.Query(ServiceName: "svc-a"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        // Only svc-a's APIs are loaded — svc-b is a consumer of svc-a but svc-b's API is excluded
        // Without svc-b's API, there's no edge svc-a→svc-b in the filtered subgraph
        result.Value.TotalServicesAnalyzed.Should().Be(2, "svc-a (publisher) and svc-b (consumer)");
    }

    [Fact]
    public async Task Validator_WhenServiceNameTooLong_ShouldFail()
    {
        var validator = new DetectCircularDependenciesFeature.Validator();
        var query = new DetectCircularDependenciesFeature.Query(ServiceName: new string('x', 201));

        var result = validator.Validate(query);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "ServiceName");
    }

    [Fact]
    public async Task Validator_WhenServiceNameIsNull_ShouldPass()
    {
        var validator = new DetectCircularDependenciesFeature.Validator();
        var result = validator.Validate(new DetectCircularDependenciesFeature.Query(null));

        result.IsValid.Should().BeTrue();
    }
}
