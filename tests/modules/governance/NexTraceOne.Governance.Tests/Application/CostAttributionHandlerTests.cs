using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.Governance.Application.Abstractions;
using NexTraceOne.Governance.Application.Features.ComputeCostAttribution;
using NexTraceOne.Governance.Application.Features.GetCostAttribution;
using NexTraceOne.Governance.Application.Features.ListCostAttributions;
using NexTraceOne.Governance.Domain.Entities;
using NexTraceOne.Governance.Domain.Enums;

namespace NexTraceOne.Governance.Tests.Application;

/// <summary>
/// Testes dos handlers de atribuição de custo operacional.
/// Cobre ComputeCostAttribution, GetCostAttribution e ListCostAttributions.
/// </summary>
public sealed class CostAttributionHandlerTests
{
    private static readonly DateTimeOffset FixedNow = new(2026, 7, 1, 10, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset PeriodStart = new(2026, 6, 1, 0, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset PeriodEnd = new(2026, 7, 1, 0, 0, 0, TimeSpan.Zero);

    private readonly ICostAttributionRepository _repository =
        Substitute.For<ICostAttributionRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IDateTimeProvider _clock = Substitute.For<IDateTimeProvider>();

    public CostAttributionHandlerTests()
    {
        _clock.UtcNow.Returns(FixedNow);
        _unitOfWork.CommitAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult(1));
    }

    // ── ComputeCostAttribution ──

    [Fact]
    public async Task Compute_ValidCommand_ShouldCreateAttribution()
    {
        var handler = new ComputeCostAttribution.Handler(_repository, _unitOfWork, _clock);
        var command = new ComputeCostAttribution.Command(
            Dimension: CostAttributionDimension.Service,
            DimensionKey: "payment-service",
            DimensionLabel: "Payment Service",
            PeriodStart: PeriodStart,
            PeriodEnd: PeriodEnd,
            TotalCost: 1000m,
            ComputeCost: 500m,
            StorageCost: 200m,
            NetworkCost: 100m,
            OtherCost: 200m,
            Currency: "USD",
            AttributionMethod: "telemetry-based",
            TenantId: "tenant1");

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Dimension.Should().Be(CostAttributionDimension.Service);
        result.Value.DimensionKey.Should().Be("payment-service");
        result.Value.DimensionLabel.Should().Be("Payment Service");
        result.Value.TotalCost.Should().Be(1000m);
        result.Value.Currency.Should().Be("USD");
        result.Value.ComputedAt.Should().Be(FixedNow);
        result.Value.AttributionId.Should().NotBe(Guid.Empty);

        await _repository.Received(1).AddAsync(Arg.Any<CostAttribution>(), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Compute_TeamDimension_ShouldSucceed()
    {
        var handler = new ComputeCostAttribution.Handler(_repository, _unitOfWork, _clock);
        var command = new ComputeCostAttribution.Command(
            Dimension: CostAttributionDimension.Team,
            DimensionKey: "platform-team",
            DimensionLabel: "Platform Team",
            PeriodStart: PeriodStart,
            PeriodEnd: PeriodEnd,
            TotalCost: 5000m,
            ComputeCost: 3000m,
            StorageCost: 1000m,
            NetworkCost: 500m,
            OtherCost: 500m);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Dimension.Should().Be(CostAttributionDimension.Team);
        result.Value.TotalCost.Should().Be(5000m);
    }

    [Fact]
    public async Task Compute_ZeroCost_ShouldSucceed()
    {
        var handler = new ComputeCostAttribution.Handler(_repository, _unitOfWork, _clock);
        var command = new ComputeCostAttribution.Command(
            Dimension: CostAttributionDimension.Domain,
            DimensionKey: "dormant-domain",
            DimensionLabel: null,
            PeriodStart: PeriodStart,
            PeriodEnd: PeriodEnd,
            TotalCost: 0m,
            ComputeCost: 0m,
            StorageCost: 0m,
            NetworkCost: 0m,
            OtherCost: 0m);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TotalCost.Should().Be(0m);
    }

    // ── GetCostAttribution ──

    [Fact]
    public async Task Get_ExistingAttribution_ShouldReturnAllDetails()
    {
        var attribution = CreateSampleAttribution();

        _repository.GetByIdAsync(attribution.Id, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<CostAttribution?>(attribution));

        var handler = new GetCostAttribution.Handler(_repository);
        var query = new GetCostAttribution.Query(attribution.Id.Value);

        var result = await handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.DimensionKey.Should().Be("payment-service");
        result.Value.TotalCost.Should().Be(1000m);
        result.Value.ComputeCost.Should().Be(500m);
        result.Value.StorageCost.Should().Be(200m);
        result.Value.NetworkCost.Should().Be(100m);
        result.Value.OtherCost.Should().Be(200m);
        result.Value.Currency.Should().Be("USD");
        result.Value.AttributionMethod.Should().Be("telemetry-based");
    }

    [Fact]
    public async Task Get_NonExistent_ShouldReturnNotFoundError()
    {
        var id = Guid.NewGuid();
        _repository.GetByIdAsync(new CostAttributionId(id), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<CostAttribution?>(null));

        var handler = new GetCostAttribution.Handler(_repository);
        var result = await handler.Handle(new GetCostAttribution.Query(id), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Governance.CostAttribution.NotFound");
    }

    // ── ListCostAttributions ──

    [Fact]
    public async Task List_ByDimension_ShouldReturnFiltered()
    {
        var attributions = new List<CostAttribution>
        {
            CreateSampleAttribution(),
            CostAttribution.Compute(
                CostAttributionDimension.Service, "order-service", "Order Service",
                PeriodStart, PeriodEnd,
                500m, 300m, 100m, 50m, 50m,
                "USD", null, "proportional", null, null, FixedNow)
        };

        _repository.ListByDimensionAsync(
                CostAttributionDimension.Service, null, null, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<CostAttribution>>(attributions));

        var handler = new ListCostAttributions.Handler(_repository);
        var result = await handler.Handle(
            new ListCostAttributions.Query(CostAttributionDimension.Service), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TotalCount.Should().Be(2);
        result.Value.FilteredDimension.Should().Be(CostAttributionDimension.Service);
    }

    [Fact]
    public async Task List_WithPeriodFilters_ShouldPassToRepository()
    {
        _repository.ListByDimensionAsync(
                CostAttributionDimension.Team, PeriodStart, PeriodEnd, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<CostAttribution>>([]));

        var handler = new ListCostAttributions.Handler(_repository);
        var result = await handler.Handle(
            new ListCostAttributions.Query(CostAttributionDimension.Team, PeriodStart, PeriodEnd),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TotalCount.Should().Be(0);

        await _repository.Received(1).ListByDimensionAsync(
            CostAttributionDimension.Team, PeriodStart, PeriodEnd, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task List_EmptyResult_ShouldReturnEmptyList()
    {
        _repository.ListByDimensionAsync(
                CostAttributionDimension.Contract, null, null, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<CostAttribution>>([]));

        var handler = new ListCostAttributions.Handler(_repository);
        var result = await handler.Handle(
            new ListCostAttributions.Query(CostAttributionDimension.Contract), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().BeEmpty();
        result.Value.TotalCount.Should().Be(0);
    }

    [Fact]
    public async Task List_ShouldMapDtoFieldsCorrectly()
    {
        var attribution = CreateSampleAttribution();
        _repository.ListByDimensionAsync(
                CostAttributionDimension.Service, null, null, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<CostAttribution>>(new[] { attribution }));

        var handler = new ListCostAttributions.Handler(_repository);
        var result = await handler.Handle(
            new ListCostAttributions.Query(CostAttributionDimension.Service), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var item = result.Value.Items[0];
        item.DimensionKey.Should().Be("payment-service");
        item.DimensionLabel.Should().Be("Payment Service");
        item.TotalCost.Should().Be(1000m);
        item.ComputeCost.Should().Be(500m);
        item.StorageCost.Should().Be(200m);
        item.NetworkCost.Should().Be(100m);
        item.OtherCost.Should().Be(200m);
        item.Currency.Should().Be("USD");
        item.AttributionMethod.Should().Be("telemetry-based");
    }

    // ── Helper ──

    private static CostAttribution CreateSampleAttribution() => CostAttribution.Compute(
        dimension: CostAttributionDimension.Service,
        dimensionKey: "payment-service",
        dimensionLabel: "Payment Service",
        periodStart: PeriodStart,
        periodEnd: PeriodEnd,
        totalCost: 1000m,
        computeCost: 500m,
        storageCost: 200m,
        networkCost: 100m,
        otherCost: 200m,
        currency: "USD",
        costBreakdown: """{"compute":"2 vCPU"}""",
        attributionMethod: "telemetry-based",
        dataSources: """["otel"]""",
        tenantId: "tenant1",
        now: FixedNow);
}
