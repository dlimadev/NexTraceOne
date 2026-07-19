using NexTraceOne.Catalog.Application.Graph.Abstractions;
using NexTraceOne.Catalog.Application.Graph.Maturity;
using NexTraceOne.Catalog.Domain.Graph.Entities;
using NexTraceOne.Catalog.Domain.Graph.Enums;

using ListServicesFeature = NexTraceOne.Catalog.Application.Graph.Features.ListServices.ListServices;

namespace NexTraceOne.Catalog.Tests.Graph.Features;

/// <summary>
/// Testes unitários para o caminho de maturidade do handler ListServices.
/// Verifica: ordenação por score ascendente/descendente, filtragem por nível,
/// paginação in-memory e que o caminho rápido não chama o calculator.
/// </summary>
public sealed class ListServicesMaturityTests
{
    private static ServiceAsset BuildService(string name = "svc", string team = "team")
        => ServiceAsset.Create(name, "platform", team, Guid.NewGuid());

    private static ServiceMaturityResult BuildMaturity(string level, decimal score)
        => new(level, score,
            HasOwnership: true,
            HasContracts: false,
            HasDocumentation: false,
            HasRunbook: false,
            HasMonitoring: false,
            HasRepository: false,
            ApiCount: 0,
            ContractCount: 0,
            LinkCount: 0);

    // ─── Ordenação por maturidade ascendente ─────────────────────────────

    [Fact]
    public async Task SortByMaturityAscending_Returns_Items_Ordered_By_Score_Ascending()
    {
        // Arrange
        var svcA = BuildService("svc-a");
        var svcB = BuildService("svc-b");
        var svcC = BuildService("svc-c");
        var all = new List<ServiceAsset> { svcA, svcB, svcC };

        var matResults = new Dictionary<Guid, ServiceMaturityResult>
        {
            [svcA.Id.Value] = BuildMaturity("Managed",    0.75m),
            [svcB.Id.Value] = BuildMaturity("Defined",    0.50m),
            [svcC.Id.Value] = BuildMaturity("Optimizing", 0.95m),
        };

        var repo = Substitute.For<IServiceAssetRepository>();
        repo.ListFilteredAsync(
                Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<ServiceType?>(),
                Arg.Any<Criticality?>(), Arg.Any<LifecycleStatus?>(), Arg.Any<ExposureType?>(),
                Arg.Any<string?>(), 1, 10_000, Arg.Any<CancellationToken>())
            .Returns(((IReadOnlyList<ServiceAsset>)all, all.Count));

        var calculator = Substitute.For<IServiceMaturityCalculator>();
        calculator.ComputeForServicesAsync(Arg.Any<IReadOnlyList<ServiceAsset>>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyDictionary<Guid, ServiceMaturityResult>>(matResults));

        var handler = new ListServicesFeature.Handler(repo, calculator);
        var query = new ListServicesFeature.Query(
            null, null, null, null, null, null, null, 1, 50,
            SortBy: "maturity", SortDescending: false);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var items = result.Value!.Items;
        items.Should().HaveCount(3);
        items[0].Name.Should().Be("svc-b"); // score 0.50
        items[1].Name.Should().Be("svc-a"); // score 0.75
        items[2].Name.Should().Be("svc-c"); // score 0.95
    }

    // ─── Ordenação por maturidade descendente ────────────────────────────

    [Fact]
    public async Task SortByMaturityDescending_Returns_Items_Ordered_By_Score_Descending()
    {
        // Arrange
        var svcA = BuildService("svc-a");
        var svcB = BuildService("svc-b");
        var all = new List<ServiceAsset> { svcA, svcB };

        var matResults = new Dictionary<Guid, ServiceMaturityResult>
        {
            [svcA.Id.Value] = BuildMaturity("Managed", 0.75m),
            [svcB.Id.Value] = BuildMaturity("Defined", 0.50m),
        };

        var repo = Substitute.For<IServiceAssetRepository>();
        repo.ListFilteredAsync(
                Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<ServiceType?>(),
                Arg.Any<Criticality?>(), Arg.Any<LifecycleStatus?>(), Arg.Any<ExposureType?>(),
                Arg.Any<string?>(), 1, 10_000, Arg.Any<CancellationToken>())
            .Returns(((IReadOnlyList<ServiceAsset>)all, all.Count));

        var calculator = Substitute.For<IServiceMaturityCalculator>();
        calculator.ComputeForServicesAsync(Arg.Any<IReadOnlyList<ServiceAsset>>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyDictionary<Guid, ServiceMaturityResult>>(matResults));

        var handler = new ListServicesFeature.Handler(repo, calculator);
        var query = new ListServicesFeature.Query(
            null, null, null, null, null, null, null, 1, 50,
            SortBy: "maturity", SortDescending: true);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var items = result.Value!.Items;
        items[0].Name.Should().Be("svc-a"); // score 0.75
        items[1].Name.Should().Be("svc-b"); // score 0.50
    }

    // ─── Filtragem por nível de maturidade ───────────────────────────────

    [Fact]
    public async Task MaturityLevel_Filter_Returns_Only_Matching_Services_And_Correct_TotalCount()
    {
        // Arrange
        var svcA = BuildService("svc-a");
        var svcB = BuildService("svc-b");
        var svcC = BuildService("svc-c");
        var all = new List<ServiceAsset> { svcA, svcB, svcC };

        var matResults = new Dictionary<Guid, ServiceMaturityResult>
        {
            [svcA.Id.Value] = BuildMaturity("Managed", 0.75m),
            [svcB.Id.Value] = BuildMaturity("Managed", 0.72m),
            [svcC.Id.Value] = BuildMaturity("Initial", 0.10m),
        };

        var repo = Substitute.For<IServiceAssetRepository>();
        repo.ListFilteredAsync(
                Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<ServiceType?>(),
                Arg.Any<Criticality?>(), Arg.Any<LifecycleStatus?>(), Arg.Any<ExposureType?>(),
                Arg.Any<string?>(), 1, 10_000, Arg.Any<CancellationToken>())
            .Returns(((IReadOnlyList<ServiceAsset>)all, all.Count));

        var calculator = Substitute.For<IServiceMaturityCalculator>();
        calculator.ComputeForServicesAsync(Arg.Any<IReadOnlyList<ServiceAsset>>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyDictionary<Guid, ServiceMaturityResult>>(matResults));

        var handler = new ListServicesFeature.Handler(repo, calculator);
        var query = new ListServicesFeature.Query(
            null, null, null, null, null, null, null, 1, 50,
            MaturityLevel: "Managed");

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.TotalCount.Should().Be(2);
        result.Value.Items.Should().HaveCount(2);
        result.Value.Items.Select(i => i.Name)
            .Should().BeEquivalentTo(["svc-a", "svc-b"]);
    }

    // ─── Paginação in-memory no caminho maturity ──────────────────────────

    [Fact]
    public async Task MaturityPath_Applies_In_Memory_Pagination_Correctly()
    {
        // Arrange: 5 serviços; page=2, pageSize=2 → 2 itens; total=5
        var services = Enumerable.Range(1, 5)
            .Select(i => BuildService($"svc-{i}"))
            .ToList();

        var matResults = services.ToDictionary(
            s => s.Id.Value,
            s => BuildMaturity("Managed", 0.70m + (decimal)services.IndexOf(s) * 0.01m));

        var repo = Substitute.For<IServiceAssetRepository>();
        repo.ListFilteredAsync(
                Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<ServiceType?>(),
                Arg.Any<Criticality?>(), Arg.Any<LifecycleStatus?>(), Arg.Any<ExposureType?>(),
                Arg.Any<string?>(), 1, 10_000, Arg.Any<CancellationToken>())
            .Returns(((IReadOnlyList<ServiceAsset>)services, services.Count));

        var calculator = Substitute.For<IServiceMaturityCalculator>();
        calculator.ComputeForServicesAsync(Arg.Any<IReadOnlyList<ServiceAsset>>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyDictionary<Guid, ServiceMaturityResult>>(matResults));

        var handler = new ListServicesFeature.Handler(repo, calculator);
        var query = new ListServicesFeature.Query(
            null, null, null, null, null, null, null, 2, 2,
            SortBy: "maturity");

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.TotalCount.Should().Be(5);
        result.Value.Items.Should().HaveCount(2);
    }

    // ─── Caminho rápido não chama calculator ──────────────────────────────

    [Fact]
    public async Task FastPath_NullMaturityAndNullSortBy_Does_Not_Call_ComputeForServicesAsync()
    {
        // Arrange: sem MaturityLevel e sem SortBy → caminho rápido
        var svcA = BuildService("svc-a");
        var all = new List<ServiceAsset> { svcA };

        var repo = Substitute.For<IServiceAssetRepository>();
        repo.ListFilteredAsync(
                Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<ServiceType?>(),
                Arg.Any<Criticality?>(), Arg.Any<LifecycleStatus?>(), Arg.Any<ExposureType?>(),
                Arg.Any<string?>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(((IReadOnlyList<ServiceAsset>)all, 1));

        var calculator = Substitute.For<IServiceMaturityCalculator>();

        var handler = new ListServicesFeature.Handler(repo, calculator);
        var query = new ListServicesFeature.Query(null, null, null, null, null, null, null, 1, 50);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        await calculator.DidNotReceive()
            .ComputeForServicesAsync(Arg.Any<IReadOnlyList<ServiceAsset>>(), Arg.Any<CancellationToken>());
    }

    // ─── Caminho rápido com SortBy=name não chama calculator ─────────────

    [Fact]
    public async Task FastPath_SortByName_Does_Not_Call_ComputeForServicesAsync()
    {
        // Arrange: SortBy=name (não maturity) → caminho rápido
        var svcA = BuildService("svc-a");
        var all = new List<ServiceAsset> { svcA };

        var repo = Substitute.For<IServiceAssetRepository>();
        repo.ListFilteredAsync(
                Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<ServiceType?>(),
                Arg.Any<Criticality?>(), Arg.Any<LifecycleStatus?>(), Arg.Any<ExposureType?>(),
                Arg.Any<string?>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(((IReadOnlyList<ServiceAsset>)all, 1));

        var calculator = Substitute.For<IServiceMaturityCalculator>();

        var handler = new ListServicesFeature.Handler(repo, calculator);
        var query = new ListServicesFeature.Query(
            null, null, null, null, null, null, null, 1, 50,
            SortBy: "name");

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        await calculator.DidNotReceive()
            .ComputeForServicesAsync(Arg.Any<IReadOnlyList<ServiceAsset>>(), Arg.Any<CancellationToken>());
    }
}
