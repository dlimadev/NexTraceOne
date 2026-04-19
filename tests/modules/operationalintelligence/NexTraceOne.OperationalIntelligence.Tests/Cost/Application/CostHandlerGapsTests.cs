using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.OperationalIntelligence.Application.Cost.Abstractions;
using NexTraceOne.OperationalIntelligence.Application.Cost.Features.AlertCostAnomaly;
using NexTraceOne.OperationalIntelligence.Application.Cost.Features.GetCostByRoute;
using NexTraceOne.OperationalIntelligence.Application.Cost.Features.GetCostDelta;
using NexTraceOne.OperationalIntelligence.Application.Cost.Features.GetCostReport;
using NexTraceOne.OperationalIntelligence.Domain.Cost.Entities;

namespace NexTraceOne.OperationalIntelligence.Tests.Cost.Application;

/// <summary>
/// Testes de unidade para os handlers de custo sem cobertura prévia:
/// AlertCostAnomaly, GetCostByRoute, GetCostDelta, GetCostReport.
/// </summary>
public sealed class CostHandlerGapsTests
{
    private static readonly DateTimeOffset FixedNow = new(2026, 4, 19, 12, 0, 0, TimeSpan.Zero);

    private static IDateTimeProvider CreateClock()
    {
        var c = Substitute.For<IDateTimeProvider>();
        c.UtcNow.Returns(FixedNow);
        return c;
    }

    private static CostAttribution MakeAttribution(string service = "OrderService", string env = "production")
    {
        var period = FixedNow.AddDays(-30);
        var result = CostAttribution.Attribute(Guid.NewGuid(), service, period, FixedNow, 500m, 5000, env);
        result.IsSuccess.Should().BeTrue();
        return result.Value;
    }

    private static CostSnapshot MakeSnapshot(string service = "OrderService", string env = "production", decimal cost = 300m, DateTimeOffset? capturedAt = null)
    {
        var at = capturedAt ?? FixedNow;
        var result = CostSnapshot.Create(service, env, cost, cost * 0.4m, cost * 0.2m, cost * 0.2m, cost * 0.1m, at, "CloudProvider", "2026-04");
        result.IsSuccess.Should().BeTrue();
        return result.Value;
    }

    // ═══════════════════════════════════════════════════════════════════
    // AlertCostAnomaly
    // ═══════════════════════════════════════════════════════════════════

    [Fact]
    public async Task AlertCostAnomaly_Should_PublishEvent_When_BudgetExceeded()
    {
        var profile = ServiceCostProfile.Create("OrderService", "production", 80m, FixedNow, monthlyBudget: 1000m);

        var profileRepo = Substitute.For<IServiceCostProfileRepository>();
        profileRepo.GetByServiceAndEnvironmentAsync("OrderService", "production", Arg.Any<CancellationToken>()).Returns(profile);

        var uow = Substitute.For<ICostIntelligenceUnitOfWork>();
        var eventBus = Substitute.For<IEventBus>();

        var handler = new AlertCostAnomaly.Handler(profileRepo, uow, CreateClock(), eventBus);
        var result = await handler.Handle(
            new AlertCostAnomaly.Command("OrderService", "production", 850m),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.ServiceName.Should().Be("OrderService");
        result.Value.CurrentCost.Should().Be(850m);
        result.Value.IsAnomalyDetected.Should().BeTrue();
        result.Value.MonthlyBudget.Should().Be(1000m);

        await eventBus.Received(1).PublishAsync(Arg.Any<object>(), Arg.Any<CancellationToken>());
        await uow.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AlertCostAnomaly_Should_NotPublishEvent_When_BudgetNotExceeded()
    {
        var profile = ServiceCostProfile.Create("OrderService", "production", 80m, FixedNow, monthlyBudget: 1000m);

        var profileRepo = Substitute.For<IServiceCostProfileRepository>();
        profileRepo.GetByServiceAndEnvironmentAsync("OrderService", "production", Arg.Any<CancellationToken>()).Returns(profile);

        var uow = Substitute.For<ICostIntelligenceUnitOfWork>();
        var eventBus = Substitute.For<IEventBus>();

        var handler = new AlertCostAnomaly.Handler(profileRepo, uow, CreateClock(), eventBus);
        var result = await handler.Handle(
            new AlertCostAnomaly.Command("OrderService", "production", 400m),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.IsAnomalyDetected.Should().BeFalse();

        await eventBus.DidNotReceive().PublishAsync(Arg.Any<object>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AlertCostAnomaly_Should_ReturnError_When_ProfileNotFound()
    {
        var profileRepo = Substitute.For<IServiceCostProfileRepository>();
        profileRepo.GetByServiceAndEnvironmentAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((ServiceCostProfile?)null);

        var uow = Substitute.For<ICostIntelligenceUnitOfWork>();
        var eventBus = Substitute.For<IEventBus>();

        var handler = new AlertCostAnomaly.Handler(profileRepo, uow, CreateClock(), eventBus);
        var result = await handler.Handle(
            new AlertCostAnomaly.Command("UnknownSvc", "production", 100m),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("NotFound");
    }

    [Fact]
    public void AlertCostAnomaly_Validator_Should_Reject_NegativeCost()
    {
        var validator = new AlertCostAnomaly.Validator();
        var result = validator.Validate(new AlertCostAnomaly.Command("Svc", "prod", -1m));
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void AlertCostAnomaly_Validator_Should_Reject_EmptyServiceName()
    {
        var validator = new AlertCostAnomaly.Validator();
        var result = validator.Validate(new AlertCostAnomaly.Command("", "prod", 100m));
        result.IsValid.Should().BeFalse();
    }

    // ═══════════════════════════════════════════════════════════════════
    // GetCostByRoute
    // ═══════════════════════════════════════════════════════════════════

    [Fact]
    public async Task GetCostByRoute_Should_ReturnAttributions_When_DataExists()
    {
        var attributions = new List<CostAttribution> { MakeAttribution(), MakeAttribution() };

        var repo = Substitute.For<ICostAttributionRepository>();
        repo.ListByServiceAsync("OrderService", "production", 1, 20, Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<CostAttribution>)attributions);

        var handler = new GetCostByRoute.Handler(repo);
        var result = await handler.Handle(
            new GetCostByRoute.Query("OrderService", "production"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.ServiceName.Should().Be("OrderService");
        result.Value.Environment.Should().Be("production");
        result.Value.Attributions.Should().HaveCount(2);
        result.Value.TotalCost.Should().Be(1000m); // 2 × 500
    }

    [Fact]
    public async Task GetCostByRoute_Should_ReturnEmpty_When_NoDataExists()
    {
        var repo = Substitute.For<ICostAttributionRepository>();
        repo.ListByServiceAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<CostAttribution>)new List<CostAttribution>());

        var handler = new GetCostByRoute.Handler(repo);
        var result = await handler.Handle(
            new GetCostByRoute.Query("NoSvc", "staging", 1, 20),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Attributions.Should().BeEmpty();
        result.Value.TotalCost.Should().Be(0m);
    }

    [Fact]
    public void GetCostByRoute_Validator_Should_Reject_InvalidPageSize()
    {
        var validator = new GetCostByRoute.Validator();
        var result = validator.Validate(new GetCostByRoute.Query("Svc", "prod", 1, 200));
        result.IsValid.Should().BeFalse();
    }

    // ═══════════════════════════════════════════════════════════════════
    // GetCostDelta
    // ═══════════════════════════════════════════════════════════════════

    [Fact]
    public async Task GetCostDelta_Should_ReturnPositiveDelta_When_CostIncreased()
    {
        var currentSnapshot = MakeSnapshot(cost: 500m, capturedAt: FixedNow.AddDays(-1));
        var previousSnapshot = MakeSnapshot(cost: 300m, capturedAt: FixedNow.AddDays(-31));

        var previousStart = FixedNow.AddDays(-40);
        var previousEnd = FixedNow.AddDays(-20);
        var currentStart = FixedNow.AddDays(-10);
        var currentEnd = FixedNow;

        var repo = Substitute.For<ICostSnapshotRepository>();
        repo.ListByServiceAsync("OrderService", "production", 1, 1000, Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<CostSnapshot>)new List<CostSnapshot> { currentSnapshot, previousSnapshot });

        var handler = new GetCostDelta.Handler(repo);
        var result = await handler.Handle(
            new GetCostDelta.Query("OrderService", "production", currentStart, currentEnd, previousStart, previousEnd),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.ServiceName.Should().Be("OrderService");
        result.Value.CurrentPeriodCost.Should().Be(500m);
        result.Value.PreviousPeriodCost.Should().Be(300m);
        result.Value.AbsoluteDelta.Should().Be(200m);
        result.Value.PercentageDelta.Should().BeApproximately(66.67m, 0.1m);
    }

    [Fact]
    public async Task GetCostDelta_Should_ReturnZeroDelta_When_NoSnapshots()
    {
        var repo = Substitute.For<ICostSnapshotRepository>();
        repo.ListByServiceAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<CostSnapshot>)new List<CostSnapshot>());

        var currentStart = FixedNow.AddDays(-10);
        var currentEnd = FixedNow;
        var previousStart = FixedNow.AddDays(-40);
        var previousEnd = FixedNow.AddDays(-20);

        var handler = new GetCostDelta.Handler(repo);
        var result = await handler.Handle(
            new GetCostDelta.Query("NewSvc", "production", currentStart, currentEnd, previousStart, previousEnd),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.AbsoluteDelta.Should().Be(0m);
        result.Value.PercentageDelta.Should().Be(0m);
    }

    [Fact]
    public void GetCostDelta_Validator_Should_Reject_WhenCurrentEndBeforeStart()
    {
        var validator = new GetCostDelta.Validator();
        var result = validator.Validate(new GetCostDelta.Query(
            "Svc", "prod",
            FixedNow, FixedNow.AddDays(-1),          // current: end before start — invalid
            FixedNow.AddDays(-30), FixedNow.AddDays(-20)));
        result.IsValid.Should().BeFalse();
    }

    // ═══════════════════════════════════════════════════════════════════
    // GetCostReport
    // ═══════════════════════════════════════════════════════════════════

    [Fact]
    public async Task GetCostReport_Should_ReturnSnapshots_When_DataExists()
    {
        var snapshots = new List<CostSnapshot>
        {
            MakeSnapshot(cost: 100m, capturedAt: FixedNow.AddDays(-2)),
            MakeSnapshot(cost: 200m, capturedAt: FixedNow.AddDays(-1)),
        };

        var repo = Substitute.For<ICostSnapshotRepository>();
        repo.ListByServiceAsync("OrderService", "production", 1, 20, Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<CostSnapshot>)snapshots);

        var handler = new GetCostReport.Handler(repo);
        var result = await handler.Handle(
            new GetCostReport.Query("OrderService", "production"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.ServiceName.Should().Be("OrderService");
        result.Value.Environment.Should().Be("production");
        result.Value.Snapshots.Should().HaveCount(2);
        result.Value.Page.Should().Be(1);
        result.Value.PageSize.Should().Be(20);
    }

    [Fact]
    public async Task GetCostReport_Should_ReturnEmpty_When_NoSnapshots()
    {
        var repo = Substitute.For<ICostSnapshotRepository>();
        repo.ListByServiceAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<CostSnapshot>)new List<CostSnapshot>());

        var handler = new GetCostReport.Handler(repo);
        var result = await handler.Handle(
            new GetCostReport.Query("NewSvc", "dev"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Snapshots.Should().BeEmpty();
    }

    [Fact]
    public void GetCostReport_Validator_Should_Reject_PageZero()
    {
        var validator = new GetCostReport.Validator();
        var result = validator.Validate(new GetCostReport.Query("Svc", "prod", Page: 0));
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void GetCostReport_Validator_Should_Accept_ValidQuery()
    {
        var validator = new GetCostReport.Validator();
        var result = validator.Validate(new GetCostReport.Query("Svc", "prod", 1, 50));
        result.IsValid.Should().BeTrue();
    }
}
