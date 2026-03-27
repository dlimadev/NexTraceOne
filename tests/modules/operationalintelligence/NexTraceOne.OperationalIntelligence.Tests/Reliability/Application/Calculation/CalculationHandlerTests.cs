using FluentAssertions;
using NSubstitute;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.OperationalIntelligence.Application.Reliability.Abstractions;
using NexTraceOne.OperationalIntelligence.Application.Reliability.Features.ComputeBurnRate;
using NexTraceOne.OperationalIntelligence.Application.Reliability.Features.ComputeErrorBudget;
using NexTraceOne.OperationalIntelligence.Application.Reliability.Features.ListServiceSlos;
using NexTraceOne.OperationalIntelligence.Application.Reliability.Features.ListSloSlas;
using NexTraceOne.OperationalIntelligence.Application.Reliability.Services;
using NexTraceOne.OperationalIntelligence.Domain.Reliability.Entities;
using NexTraceOne.OperationalIntelligence.Domain.Reliability.Enums;
using NexTraceOne.OperationalIntelligence.Infrastructure.Reliability;
using Xunit;

namespace NexTraceOne.OperationalIntelligence.Tests.Reliability.Application.Calculation;

/// <summary>
/// Testes unitários para os handlers de ComputeErrorBudget, ComputeBurnRate,
/// ListServiceSlos e ListSloSlas introduzidos na P6.2.
/// </summary>
public sealed class CalculationHandlerTests
{
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly ErrorBudgetCalculator Calculator = new();

    private static ICurrentTenant MockTenant()
    {
        var t = Substitute.For<ICurrentTenant>();
        t.Id.Returns(TenantId);
        return t;
    }

    private static IDateTimeProvider MockClock()
    {
        var c = Substitute.For<IDateTimeProvider>();
        c.UtcNow.Returns(DateTimeOffset.UtcNow);
        return c;
    }

    // ── ComputeErrorBudget ───────────────────────────────────────────────────

    [Fact]
    public async Task ComputeErrorBudget_WithRuntimeSignal_ShouldPersistAndReturnSnapshot()
    {
        var slo = SloDefinition.Create(TenantId, "svc-api", "production", "Availability SLO", SloType.Availability, 99.9m, 30);

        var sloRepository = Substitute.For<ISloDefinitionRepository>();
        sloRepository.GetByIdAsync(Arg.Any<SloDefinitionId>(), TenantId, Arg.Any<CancellationToken>())
            .Returns(slo);

        var budgetRepository = Substitute.For<IErrorBudgetSnapshotRepository>();

        var runtimeSurface = Substitute.For<IReliabilityRuntimeSurface>();
        runtimeSurface.GetLatestSignalAsync("svc-api", "production", Arg.Any<CancellationToken>())
            .Returns(new RuntimeServiceSignal("svc-api", "production", "Healthy", 0.001m, 50m, 100m, DateTimeOffset.UtcNow));

        var handler = new ComputeErrorBudget.Handler(
            sloRepository, budgetRepository, runtimeSurface, Calculator, MockTenant(), MockClock());

        var result = await handler.Handle(new ComputeErrorBudget.Command(slo.Id.Value), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.ObservedErrorRate.Should().Be(0.001m);
        result.Value.TotalBudgetMinutes.Should().BeApproximately(43.2m, 0.001m);
        result.Value.ConsumedBudgetMinutes.Should().BeApproximately(43.2m, 0.001m);  // observed == tolerated
        result.Value.ConsumedPercent.Should().BeApproximately(100m, 1m);
        result.Value.Status.Should().Be(SloStatus.Violated);

        await budgetRepository.Received(1).AddAsync(Arg.Any<ErrorBudgetSnapshot>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ComputeErrorBudget_WithNoRuntimeSignal_ShouldPersistWithZeroConsumed()
    {
        var slo = SloDefinition.Create(TenantId, "svc-api", "production", "Availability SLO", SloType.Availability, 99.9m, 30);

        var sloRepository = Substitute.For<ISloDefinitionRepository>();
        sloRepository.GetByIdAsync(Arg.Any<SloDefinitionId>(), TenantId, Arg.Any<CancellationToken>())
            .Returns(slo);

        var budgetRepository = Substitute.For<IErrorBudgetSnapshotRepository>();
        var runtimeSurface = Substitute.For<IReliabilityRuntimeSurface>();
        runtimeSurface.GetLatestSignalAsync("svc-api", "production", Arg.Any<CancellationToken>())
            .Returns((RuntimeServiceSignal?)null);

        var handler = new ComputeErrorBudget.Handler(
            sloRepository, budgetRepository, runtimeSurface, Calculator, MockTenant(), MockClock());

        var result = await handler.Handle(new ComputeErrorBudget.Command(slo.Id.Value), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.ObservedErrorRate.Should().Be(0m);
        result.Value.ConsumedBudgetMinutes.Should().Be(0m);
        result.Value.Status.Should().Be(SloStatus.Healthy);
        await budgetRepository.Received(1).AddAsync(Arg.Any<ErrorBudgetSnapshot>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ComputeErrorBudget_WithHighErrorRate_ShouldReturnViolatedStatus()
    {
        var slo = SloDefinition.Create(TenantId, "svc-api", "production", "Availability SLO", SloType.Availability, 99.9m, 30);

        var sloRepository = Substitute.For<ISloDefinitionRepository>();
        sloRepository.GetByIdAsync(Arg.Any<SloDefinitionId>(), TenantId, Arg.Any<CancellationToken>())
            .Returns(slo);

        var budgetRepository = Substitute.For<IErrorBudgetSnapshotRepository>();
        var runtimeSurface = Substitute.For<IReliabilityRuntimeSurface>();
        // 5% error rate vs 0.1% tolerated → massive violation
        runtimeSurface.GetLatestSignalAsync("svc-api", "production", Arg.Any<CancellationToken>())
            .Returns(new RuntimeServiceSignal("svc-api", "production", "Unhealthy", 0.05m, 500m, 50m, DateTimeOffset.UtcNow));

        var handler = new ComputeErrorBudget.Handler(
            sloRepository, budgetRepository, runtimeSurface, Calculator, MockTenant(), MockClock());

        var result = await handler.Handle(new ComputeErrorBudget.Command(slo.Id.Value), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Status.Should().Be(SloStatus.Violated);
        result.Value.ConsumedPercent.Should().Be(100m);
    }

    [Fact]
    public async Task ComputeErrorBudget_WhenSloNotFound_ShouldReturnNotFound()
    {
        var sloRepository = Substitute.For<ISloDefinitionRepository>();
        sloRepository.GetByIdAsync(Arg.Any<SloDefinitionId>(), TenantId, Arg.Any<CancellationToken>())
            .Returns((SloDefinition?)null);

        var handler = new ComputeErrorBudget.Handler(
            sloRepository,
            Substitute.For<IErrorBudgetSnapshotRepository>(),
            Substitute.For<IReliabilityRuntimeSurface>(),
            Calculator, MockTenant(), MockClock());

        var result = await handler.Handle(new ComputeErrorBudget.Command(Guid.NewGuid()), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
    }

    // ── ComputeBurnRate ──────────────────────────────────────────────────────

    [Fact]
    public async Task ComputeBurnRate_WithSpecificWindow_ShouldPersistOnlyThatWindow()
    {
        var slo = SloDefinition.Create(TenantId, "svc-api", "production", "Error Rate SLO", SloType.ErrorRate, 99.9m, 30);

        var sloRepository = Substitute.For<ISloDefinitionRepository>();
        sloRepository.GetByIdAsync(Arg.Any<SloDefinitionId>(), TenantId, Arg.Any<CancellationToken>())
            .Returns(slo);

        var burnRateRepository = Substitute.For<IBurnRateSnapshotRepository>();
        var runtimeSurface = Substitute.For<IReliabilityRuntimeSurface>();
        runtimeSurface.GetLatestSignalAsync("svc-api", "production", Arg.Any<CancellationToken>())
            .Returns(new RuntimeServiceSignal("svc-api", "production", "Healthy", 0.005m, 100m, 200m, DateTimeOffset.UtcNow));
        var handler = new ComputeBurnRate.Handler(
            sloRepository, burnRateRepository, runtimeSurface, Calculator, MockTenant(), MockClock());

        var result = await handler.Handle(
            new ComputeBurnRate.Command(slo.Id.Value, BurnRateWindow.OneHour), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.ObservedErrorRate.Should().Be(0.005m);
        result.Value.ToleratedErrorRate.Should().BeApproximately(0.001m, 0.00001m);
        result.Value.Snapshots.Should().HaveCount(1);
        result.Value.Snapshots[0].Window.Should().Be(BurnRateWindow.OneHour);
        result.Value.Snapshots[0].BurnRate.Should().BeApproximately(5m, 0.001m);
        result.Value.Snapshots[0].Status.Should().Be(SloStatus.Healthy);  // burn rate 5 < 6 threshold

        await burnRateRepository.Received(1).AddAsync(Arg.Any<BurnRateSnapshot>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ComputeBurnRate_WithAllWindows_ShouldPersistFourSnapshots()
    {
        var slo = SloDefinition.Create(TenantId, "svc-api", "production", "Error Rate SLO", SloType.ErrorRate, 99.9m, 30);

        var sloRepository = Substitute.For<ISloDefinitionRepository>();
        sloRepository.GetByIdAsync(Arg.Any<SloDefinitionId>(), TenantId, Arg.Any<CancellationToken>())
            .Returns(slo);

        var burnRateRepository = Substitute.For<IBurnRateSnapshotRepository>();
        var runtimeSurface = Substitute.For<IReliabilityRuntimeSurface>();
        runtimeSurface.GetLatestSignalAsync("svc-api", "production", Arg.Any<CancellationToken>())
            .Returns(new RuntimeServiceSignal("svc-api", "production", "Healthy", 0.001m, 100m, 200m, DateTimeOffset.UtcNow));

        var handler = new ComputeBurnRate.Handler(
            sloRepository, burnRateRepository, runtimeSurface, Calculator, MockTenant(), MockClock());

        // Window = null → all 4 windows
        var result = await handler.Handle(
            new ComputeBurnRate.Command(slo.Id.Value, null), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Snapshots.Should().HaveCount(4);

        await burnRateRepository.Received(4).AddAsync(Arg.Any<BurnRateSnapshot>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ComputeBurnRate_WhenSloNotFound_ShouldReturnNotFound()
    {
        var sloRepository = Substitute.For<ISloDefinitionRepository>();
        sloRepository.GetByIdAsync(Arg.Any<SloDefinitionId>(), TenantId, Arg.Any<CancellationToken>())
            .Returns((SloDefinition?)null);

        var handler = new ComputeBurnRate.Handler(
            sloRepository,
            Substitute.For<IBurnRateSnapshotRepository>(),
            Substitute.For<IReliabilityRuntimeSurface>(),
            Calculator, MockTenant(), MockClock());

        var result = await handler.Handle(
            new ComputeBurnRate.Command(Guid.NewGuid(), BurnRateWindow.OneHour), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
    }

    // ── ListServiceSlos ──────────────────────────────────────────────────────

    [Fact]
    public async Task ListServiceSlos_ShouldReturnAllSlosForService()
    {
        var slo1 = SloDefinition.Create(TenantId, "svc-api", "production", "Availability SLO", SloType.Availability, 99.9m, 30);
        var slo2 = SloDefinition.Create(TenantId, "svc-api", "staging", "Latency SLO", SloType.Latency, 99m, 7);

        var sloRepository = Substitute.For<ISloDefinitionRepository>();
        sloRepository.GetByServiceAsync("svc-api", TenantId, Arg.Any<CancellationToken>())
            .Returns(new List<SloDefinition> { slo1, slo2 });

        var handler = new ListServiceSlos.Handler(sloRepository, MockTenant());
        var result = await handler.Handle(new ListServiceSlos.Query("svc-api"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(2);
        result.Value.ServiceId.Should().Be("svc-api");
    }

    [Fact]
    public async Task ListServiceSlos_WithNoSlos_ShouldReturnEmptyList()
    {
        var sloRepository = Substitute.For<ISloDefinitionRepository>();
        sloRepository.GetByServiceAsync("svc-unknown", TenantId, Arg.Any<CancellationToken>())
            .Returns(new List<SloDefinition>());

        var handler = new ListServiceSlos.Handler(sloRepository, MockTenant());
        var result = await handler.Handle(new ListServiceSlos.Query("svc-unknown"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().BeEmpty();
    }

    // ── ListSloSlas ──────────────────────────────────────────────────────────

    [Fact]
    public async Task ListSloSlas_WhenSloExists_ShouldReturnSlas()
    {
        var sloId = SloDefinitionId.New();
        var slo = SloDefinition.Create(TenantId, "svc-api", "production", "Availability SLO", SloType.Availability, 99.9m, 30);
        var sla = SlaDefinition.Create(TenantId, sloId, "SLA Tier-1", 99.5m, DateTimeOffset.UtcNow);

        var sloRepository = Substitute.For<ISloDefinitionRepository>();
        sloRepository.GetByIdAsync(Arg.Any<SloDefinitionId>(), TenantId, Arg.Any<CancellationToken>())
            .Returns(slo);

        var slaRepository = Substitute.For<ISlaDefinitionRepository>();
        slaRepository.GetBySloAsync(Arg.Any<SloDefinitionId>(), TenantId, Arg.Any<CancellationToken>())
            .Returns(new List<SlaDefinition> { sla });

        var handler = new ListSloSlas.Handler(sloRepository, slaRepository, MockTenant());
        var result = await handler.Handle(new ListSloSlas.Query(sloId.Value), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(1);
        result.Value.Items[0].Name.Should().Be("SLA Tier-1");
        result.Value.Items[0].ContractualTargetPercent.Should().Be(99.5m);
        result.Value.Items[0].Status.Should().Be(SlaStatus.Active);
    }

    [Fact]
    public async Task ListSloSlas_WhenSloNotFound_ShouldReturnNotFound()
    {
        var sloRepository = Substitute.For<ISloDefinitionRepository>();
        sloRepository.GetByIdAsync(Arg.Any<SloDefinitionId>(), TenantId, Arg.Any<CancellationToken>())
            .Returns((SloDefinition?)null);

        var handler = new ListSloSlas.Handler(
            sloRepository, Substitute.For<ISlaDefinitionRepository>(), MockTenant());

        var result = await handler.Handle(new ListSloSlas.Query(Guid.NewGuid()), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
    }
}
