using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.OperationalIntelligence.Application.Cost.Abstractions;
using NexTraceOne.OperationalIntelligence.Application.Cost.Features.DetectWasteSignals;
using NexTraceOne.OperationalIntelligence.Application.Cost.Features.EvaluateCostAwareChangeGate;
using NexTraceOne.OperationalIntelligence.Application.Cost.Features.GetFocusExport;
using NexTraceOne.OperationalIntelligence.Application.Cost.Features.GetWasteReport;
using NexTraceOne.OperationalIntelligence.Domain.Cost.Entities;
using NexTraceOne.OperationalIntelligence.Domain.Cost.Enums;

namespace NexTraceOne.OperationalIntelligence.Tests.Cost.Application;

/// <summary>
/// Testes unitários — WAVE A.6: FinOps Contextual.
/// Cobre WasteSignal domain, DetectWasteSignals, GetWasteReport, GetFocusExport e EvaluateCostAwareChangeGate.
/// </summary>
public sealed class FinOpsA6Tests
{
    private static readonly DateTimeOffset FixedNow = new(2026, 4, 20, 12, 0, 0, TimeSpan.Zero);

    // ── WasteSignal.Create ────────────────────────────────────────────────

    [Fact]
    public void WasteSignal_Create_WithValidArgs_SetsProperties()
    {
        var signal = WasteSignal.Create(
            serviceName: "payment-service",
            environment: "production",
            signalType: WasteSignalType.Overprovisioned,
            estimatedMonthlySavings: 150.50m,
            description: "Over budget by 30%",
            detectedAt: FixedNow,
            teamName: "payments-team");

        signal.ServiceName.Should().Be("payment-service");
        signal.Environment.Should().Be("production");
        signal.SignalType.Should().Be(WasteSignalType.Overprovisioned);
        signal.EstimatedMonthlySavings.Should().Be(150.50m);
        signal.Description.Should().Be("Over budget by 30%");
        signal.TeamName.Should().Be("payments-team");
        signal.IsAcknowledged.Should().BeFalse();
        signal.AcknowledgedAt.Should().BeNull();
    }

    [Fact]
    public void WasteSignal_Create_WithNegativeSavings_Throws()
    {
        var act = () => WasteSignal.Create("svc", "env", WasteSignalType.IdleResources, -1m, "desc", FixedNow);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Theory]
    [InlineData("", "env", "desc")]
    [InlineData("svc", "", "desc")]
    [InlineData("svc", "env", "")]
    public void WasteSignal_Create_WithMissingRequiredStrings_Throws(string svc, string env, string desc)
    {
        var act = () => WasteSignal.Create(svc, env, WasteSignalType.IdleResources, 0m, desc, FixedNow);
        act.Should().Throw<Exception>();
    }

    // ── WasteSignal.Acknowledge ───────────────────────────────────────────

    [Fact]
    public void WasteSignal_Acknowledge_SetsFieldsCorrectly()
    {
        var signal = WasteSignal.Create("svc", "prod", WasteSignalType.IdleResources, 0m, "idle", FixedNow);

        signal.Acknowledge("john.doe", FixedNow.AddHours(1));

        signal.IsAcknowledged.Should().BeTrue();
        signal.AcknowledgedBy.Should().Be("john.doe");
        signal.AcknowledgedAt.Should().Be(FixedNow.AddHours(1));
    }

    // ── DetectWasteSignals ────────────────────────────────────────────────

    [Fact]
    public async Task DetectWasteSignals_OverBudgetProfile_GeneratesOverprovisionedSignal()
    {
        var profileRepo = Substitute.For<IServiceCostProfileRepository>();
        var costRepo = Substitute.For<ICostRecordRepository>();
        var wasteRepo = Substitute.For<IWasteSignalRepository>();
        var clock = Substitute.For<IDateTimeProvider>();
        clock.UtcNow.Returns(FixedNow);

        var profile = NexTraceOne.OperationalIntelligence.Domain.Cost.Entities.ServiceCostProfile.Create(
            "payment-service", "production", 80m, FixedNow, 200m);
        profile.UpdateCurrentCost(260m, FixedNow); // 30% over budget (> 20% threshold)

        profileRepo.GetByServiceAndEnvironmentAsync("payment-service", "production", Arg.Any<CancellationToken>())
            .Returns(profile);
        costRepo.ListByServiceAsync("payment-service", cancellationToken: Arg.Any<CancellationToken>())
            .Returns(new List<NexTraceOne.OperationalIntelligence.Domain.Cost.Entities.CostRecord>());

        var handler = new DetectWasteSignals.Handler(profileRepo, costRepo, wasteRepo, clock);
        var command = new DetectWasteSignals.Command("payment-service", "production");

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.DetectedCount.Should().Be(1);
        result.Value.Signals[0].Type.Should().Be("Overprovisioned");
    }

    [Fact]
    public async Task DetectWasteSignals_IdleService_GeneratesIdleResourcesSignal()
    {
        var profileRepo = Substitute.For<IServiceCostProfileRepository>();
        var costRepo = Substitute.For<ICostRecordRepository>();
        var wasteRepo = Substitute.For<IWasteSignalRepository>();
        var clock = Substitute.For<IDateTimeProvider>();
        clock.UtcNow.Returns(FixedNow);

        profileRepo.GetByServiceAndEnvironmentAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((NexTraceOne.OperationalIntelligence.Domain.Cost.Entities.ServiceCostProfile?)null);

        var idleRecords = new List<NexTraceOne.OperationalIntelligence.Domain.Cost.Entities.CostRecord>
        {
            CreateZeroCostRecord("dev", "2026-03"),
            CreateZeroCostRecord("dev", "2026-02"),
        };
        costRepo.ListByServiceAsync("idle-svc", cancellationToken: Arg.Any<CancellationToken>())
            .Returns(idleRecords);

        var handler = new DetectWasteSignals.Handler(profileRepo, costRepo, wasteRepo, clock);
        var command = new DetectWasteSignals.Command("idle-svc", "dev");

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.DetectedCount.Should().Be(1);
        result.Value.Signals[0].Type.Should().Be("IdleResources");
    }

    [Fact]
    public async Task DetectWasteSignals_NoProfile_ReturnsZeroSignals()
    {
        var profileRepo = Substitute.For<IServiceCostProfileRepository>();
        var costRepo = Substitute.For<ICostRecordRepository>();
        var wasteRepo = Substitute.For<IWasteSignalRepository>();
        var clock = Substitute.For<IDateTimeProvider>();
        clock.UtcNow.Returns(FixedNow);

        profileRepo.GetByServiceAndEnvironmentAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((NexTraceOne.OperationalIntelligence.Domain.Cost.Entities.ServiceCostProfile?)null);
        costRepo.ListByServiceAsync(Arg.Any<string>(), cancellationToken: Arg.Any<CancellationToken>())
            .Returns(new List<NexTraceOne.OperationalIntelligence.Domain.Cost.Entities.CostRecord>());

        var handler = new DetectWasteSignals.Handler(profileRepo, costRepo, wasteRepo, clock);
        var result = await handler.Handle(new DetectWasteSignals.Command("healthy-svc", "prod"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.DetectedCount.Should().Be(0);
    }

    // ── GetWasteReport ────────────────────────────────────────────────────

    [Fact]
    public async Task GetWasteReport_AggregatesByType()
    {
        var repo = Substitute.For<IWasteSignalRepository>();
        var signals = new List<WasteSignal>
        {
            WasteSignal.Create("svc-a", "prod", WasteSignalType.Overprovisioned, 100m, "over budget", FixedNow),
            WasteSignal.Create("svc-b", "prod", WasteSignalType.Overprovisioned, 50m, "over budget 2", FixedNow),
            WasteSignal.Create("svc-c", "dev", WasteSignalType.IdleResources, 0m, "idle", FixedNow),
        };
        repo.ListAllAsync(null, false, Arg.Any<CancellationToken>()).Returns(signals);

        var handler = new GetWasteReport.Handler(repo);
        var result = await handler.Handle(new GetWasteReport.Query(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TotalSignals.Should().Be(3);
        result.Value.TotalEstimatedMonthlySavings.Should().Be(150m);
        result.Value.ByType.Should().HaveCount(2);
        result.Value.ByType[0].Type.Should().Be("Overprovisioned");
    }

    [Fact]
    public async Task GetWasteReport_IncludeAcknowledgedFalse_FiltersAcknowledged()
    {
        var repo = Substitute.For<IWasteSignalRepository>();
        repo.ListAllAsync(null, false, Arg.Any<CancellationToken>()).Returns(new List<WasteSignal>());

        var handler = new GetWasteReport.Handler(repo);
        var result = await handler.Handle(new GetWasteReport.Query(IncludeAcknowledged: false), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await repo.Received(1).ListAllAsync(null, false, Arg.Any<CancellationToken>());
    }

    // ── GetFocusExport ────────────────────────────────────────────────────

    [Fact]
    public async Task GetFocusExport_ValidPeriod_ReturnsFocusSchema()
    {
        var repo = Substitute.For<ICostRecordRepository>();
        repo.ListByPeriodAsync("2026-04", Arg.Any<CancellationToken>())
            .Returns(new List<NexTraceOne.OperationalIntelligence.Domain.Cost.Entities.CostRecord>());

        var handler = new GetFocusExport.Handler(repo);
        var result = await handler.Handle(new GetFocusExport.Query("2026-04"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.SchemaVersion.Should().Be("FOCUS_1.0");
        result.Value.Period.Should().Be("2026-04");
    }

    // ── EvaluateCostAwareChangeGate ───────────────────────────────────────

    [Fact]
    public async Task EvaluateCostAwareChangeGate_NoProfile_ReturnsUnknown()
    {
        var profileRepo = Substitute.For<IServiceCostProfileRepository>();
        var wasteRepo = Substitute.For<IWasteSignalRepository>();
        profileRepo.GetByServiceAndEnvironmentAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((NexTraceOne.OperationalIntelligence.Domain.Cost.Entities.ServiceCostProfile?)null);
        wasteRepo.ListByServiceAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new List<WasteSignal>());

        var handler = new EvaluateCostAwareChangeGate.Handler(profileRepo, wasteRepo);
        var result = await handler.Handle(new EvaluateCostAwareChangeGate.Query("unknown-svc", "prod"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Decision.Should().Be("Unknown");
    }

    [Fact]
    public async Task EvaluateCostAwareChangeGate_OverBudget_ReturnsBlocked()
    {
        var profileRepo = Substitute.For<IServiceCostProfileRepository>();
        var wasteRepo = Substitute.For<IWasteSignalRepository>();

        var profile = NexTraceOne.OperationalIntelligence.Domain.Cost.Entities.ServiceCostProfile.Create(
            "critical-svc", "production", 80m, FixedNow, 200m);
        profile.UpdateCurrentCost(300m, FixedNow);

        profileRepo.GetByServiceAndEnvironmentAsync("critical-svc", "production", Arg.Any<CancellationToken>())
            .Returns(profile);
        wasteRepo.ListByServiceAsync("critical-svc", "production", Arg.Any<CancellationToken>())
            .Returns(new List<WasteSignal>());

        var handler = new EvaluateCostAwareChangeGate.Handler(profileRepo, wasteRepo);
        var result = await handler.Handle(new EvaluateCostAwareChangeGate.Query("critical-svc", "production"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Decision.Should().Be("Blocked");
    }

    [Fact]
    public async Task EvaluateCostAwareChangeGate_ThreeOrMoreWasteSignals_ReturnsReview()
    {
        var profileRepo = Substitute.For<IServiceCostProfileRepository>();
        var wasteRepo = Substitute.For<IWasteSignalRepository>();

        var profile = NexTraceOne.OperationalIntelligence.Domain.Cost.Entities.ServiceCostProfile.Create(
            "noisy-svc", "production", 80m, FixedNow, 500m);
        profile.UpdateCurrentCost(100m, FixedNow);

        profileRepo.GetByServiceAndEnvironmentAsync("noisy-svc", "production", Arg.Any<CancellationToken>())
            .Returns(profile);

        var signals = new List<WasteSignal>
        {
            WasteSignal.Create("noisy-svc", "production", WasteSignalType.IdleResources, 0m, "idle", FixedNow),
            WasteSignal.Create("noisy-svc", "production", WasteSignalType.Overprovisioned, 10m, "over", FixedNow),
            WasteSignal.Create("noisy-svc", "production", WasteSignalType.UnusedLicenses, 20m, "unused", FixedNow),
        };
        wasteRepo.ListByServiceAsync("noisy-svc", "production", Arg.Any<CancellationToken>())
            .Returns(signals);

        var handler = new EvaluateCostAwareChangeGate.Handler(profileRepo, wasteRepo);
        var result = await handler.Handle(new EvaluateCostAwareChangeGate.Query("noisy-svc", "production"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Decision.Should().Be("Review");
    }

    [Fact]
    public async Task EvaluateCostAwareChangeGate_HealthyBudget_ReturnsApproved()
    {
        var profileRepo = Substitute.For<IServiceCostProfileRepository>();
        var wasteRepo = Substitute.For<IWasteSignalRepository>();

        var profile = NexTraceOne.OperationalIntelligence.Domain.Cost.Entities.ServiceCostProfile.Create(
            "healthy-svc", "staging", 80m, FixedNow, 500m);
        profile.UpdateCurrentCost(50m, FixedNow);

        profileRepo.GetByServiceAndEnvironmentAsync("healthy-svc", "staging", Arg.Any<CancellationToken>())
            .Returns(profile);
        wasteRepo.ListByServiceAsync("healthy-svc", "staging", Arg.Any<CancellationToken>())
            .Returns(new List<WasteSignal>());

        var handler = new EvaluateCostAwareChangeGate.Handler(profileRepo, wasteRepo);
        var result = await handler.Handle(new EvaluateCostAwareChangeGate.Query("healthy-svc", "staging"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Decision.Should().Be("Approved");
    }

    // ── Helpers ───────────────────────────────────────────────────────────

    private static NexTraceOne.OperationalIntelligence.Domain.Cost.Entities.CostRecord CreateZeroCostRecord(string environment, string period)
    {
        return NexTraceOne.OperationalIntelligence.Domain.Cost.Entities.CostRecord.Create(
            batchId: Guid.NewGuid(),
            serviceId: "idle-svc-id",
            serviceName: "idle-svc",
            team: null,
            domain: null,
            environment: environment,
            period: period,
            totalCost: 0m,
            currency: "USD",
            source: "test",
            recordedAt: FixedNow).Value;
    }
}
