using System.Linq;
using NSubstitute;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.OperationalIntelligence.Application.Cost.Abstractions;
using NexTraceOne.OperationalIntelligence.Application.Runtime.Abstractions;
using NexTraceOne.OperationalIntelligence.Application.Runtime.Features.GetAnomalyDetectionSummaryReport;
using NexTraceOne.OperationalIntelligence.Domain.Cost.Entities;
using NexTraceOne.OperationalIntelligence.Domain.Cost.Enums;
using NexTraceOne.OperationalIntelligence.Domain.Runtime.Entities;
using NexTraceOne.OperationalIntelligence.Domain.Runtime.Enums;

namespace NexTraceOne.OperationalIntelligence.Tests.Runtime.Application;

/// <summary>
/// Testes unitários para Wave W.3 — GetAnomalyDetectionSummaryReport.
/// Cobre: sem anomalias, Clean/Moderate/Dense/Critical density, multi-anomaly services,
/// timeline 30 pontos, distribuição por tipo, validator.
/// </summary>
public sealed class AnomalyDetectionSummaryReportTests
{
    private static readonly DateTimeOffset FixedNow = new(2026, 4, 22, 10, 0, 0, TimeSpan.Zero);
    private const string TenantId = "tenant-anomaly-w3";

    private static IDateTimeProvider CreateClock()
    {
        var clock = Substitute.For<IDateTimeProvider>();
        clock.UtcNow.Returns(FixedNow);
        return clock;
    }

    private static WasteSignal MakeWaste(string serviceName, DateTimeOffset? at = null)
        => WasteSignal.Create(serviceName, "prod", WasteSignalType.IdleResources, 100m, "idle", at ?? FixedNow.AddDays(-5));

    private static DriftFinding MakeDrift(string serviceName, DateTimeOffset? at = null)
        => DriftFinding.Detect(serviceName, "prod", "AvgLatencyMs", 100m, 150m, at ?? FixedNow.AddDays(-5));

    /// <summary>Creates a Breached SloObservation (observedValue below sloTarget).</summary>
    private static SloObservation MakeSloBreached(string serviceName)
        => SloObservation.Create(TenantId, serviceName, "prod", "availability",
            observedValue: 0.9m,   // below target → Breached
            sloTarget: 0.99m,
            periodStart: FixedNow.AddDays(-10),
            periodEnd: FixedNow.AddDays(-5),
            observedAt: FixedNow.AddDays(-5));

    private static ChaosExperiment MakeChaos(string serviceName, ExperimentStatus status = ExperimentStatus.Failed, DateTimeOffset? at = null)
    {
        var createdAt = at ?? FixedNow.AddDays(-5);
        var exp = ChaosExperiment.Create(
            tenantId: TenantId,
            serviceName: serviceName,
            environment: "prod",
            experimentType: "latency-injection",
            description: null,
            riskLevel: "Low",
            durationSeconds: 60,
            targetPercentage: 10m,
            steps: ["step1"],
            safetyChecks: ["check1"],
            createdAt: createdAt,
            createdBy: "system");
        exp.Start(createdAt.AddMinutes(1));
        if (status == ExperimentStatus.Failed)
            exp.Fail(createdAt.AddMinutes(2));
        return exp;
    }

    private static GetAnomalyDetectionSummaryReport.Handler CreateHandler(
        IReadOnlyList<WasteSignal> waste,
        IReadOnlyList<DriftFinding> drifts,
        IReadOnlyList<SloObservation> sloBreaches,
        IReadOnlyList<ChaosExperiment> chaos,
        IReadOnlyList<string> vulnServiceNames)
    {
        var wasteRepo = Substitute.For<IWasteSignalRepository>();
        wasteRepo.ListAllAsync(Arg.Any<string?>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(waste);

        var driftRepo = Substitute.For<IDriftFindingRepository>();
        driftRepo.ListByTenantInPeriodAsync(
                Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns(drifts);

        var sloRepo = Substitute.For<ISloObservationRepository>();
        sloRepo.ListByTenantAsync(
                Arg.Any<string>(), Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(),
                Arg.Any<SloObservationStatus?>(), Arg.Any<CancellationToken>())
            .Returns(sloBreaches);

        var chaosRepo = Substitute.For<IChaosExperimentRepository>();
        chaosRepo.ListAsync(
                Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<string?>(),
                Arg.Any<ExperimentStatus?>(), Arg.Any<CancellationToken>())
            .Returns(chaos);

        var vulnReader = Substitute.For<IVulnerabilityAdvisoryReader>();
        vulnReader.ListCriticalOrHighServiceNamesInPeriodAsync(
                Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns(vulnServiceNames);

        return new GetAnomalyDetectionSummaryReport.Handler(
            wasteRepo, driftRepo, sloRepo, chaosRepo, vulnReader, CreateClock());
    }

    private static GetAnomalyDetectionSummaryReport.Query DefaultQuery()
        => new(TenantId: TenantId, LookbackDays: 30, MaxTopServices: 10);

    // ── Empty: no anomalies ───────────────────────────────────────────────

    [Fact]
    public async Task Handle_NoAnomalies_ReturnsEmptyReport()
    {
        var handler = CreateHandler([], [], [], [], []);
        var result = await handler.Handle(DefaultQuery(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(0, result.Value.TotalServicesWithAnomalies);
        Assert.Empty(result.Value.MultiAnomalyServices);
        Assert.Empty(result.Value.AllServices);
    }

    // ── Moderate: 1 anomaly type ─────────────────────────────────────────

    [Fact]
    public async Task Handle_OneAnomalyType_ModerateAndNotMultiAnomaly()
    {
        var handler = CreateHandler(
            [MakeWaste("svc-one")], [], [], [], []);
        var result = await handler.Handle(DefaultQuery(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var entry = result.Value.AllServices.Single();
        Assert.Equal(GetAnomalyDetectionSummaryReport.AnomalyDensity.Moderate, entry.Density);
        Assert.Equal(1, entry.AnomalyCount);
        Assert.Empty(result.Value.MultiAnomalyServices);
    }

    // ── Dense: 3 anomaly types ────────────────────────────────────────────

    [Fact]
    public async Task Handle_ThreeAnomalyTypes_DenseAndIsMultiAnomaly()
    {
        const string svc = "svc-dense";
        var handler = CreateHandler(
            [MakeWaste(svc)],
            [MakeDrift(svc)],
            [MakeSloBreached(svc)],
            [], []);

        var result = await handler.Handle(DefaultQuery(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var entry = result.Value.AllServices.Single();
        Assert.Equal(GetAnomalyDetectionSummaryReport.AnomalyDensity.Dense, entry.Density);
        Assert.Equal(3, entry.AnomalyCount);
        Assert.Single(result.Value.MultiAnomalyServices);
    }

    // ── Critical: 5 anomaly types ─────────────────────────────────────────

    [Fact]
    public async Task Handle_FiveAnomalyTypes_CriticalDensity()
    {
        const string svc = "svc-critical";
        var handler = CreateHandler(
            [MakeWaste(svc)],
            [MakeDrift(svc)],
            [MakeSloBreached(svc)],
            [MakeChaos(svc)],
            [svc]);

        var result = await handler.Handle(DefaultQuery(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var entry = result.Value.AllServices.Single();
        Assert.Equal(GetAnomalyDetectionSummaryReport.AnomalyDensity.Critical, entry.Density);
        Assert.Equal(5, entry.AnomalyCount);
        Assert.Single(result.Value.MultiAnomalyServices);
    }

    // ── AnomalyCount = distinct types (not total instances) ───────────────

    [Fact]
    public async Task Handle_MultipleWasteSignalsSameService_CountsAsOneType()
    {
        const string svc = "svc-multi-waste";
        var handler = CreateHandler(
            [MakeWaste(svc), MakeWaste(svc), MakeWaste(svc)],
            [], [], [], []);

        var result = await handler.Handle(DefaultQuery(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var entry = result.Value.AllServices.Single();
        Assert.Equal(1, entry.AnomalyCount);   // 1 distinct type
        Assert.Equal(3, entry.WasteSignals);    // 3 instances
    }

    // ── TypeDistribution totals ───────────────────────────────────────────

    [Fact]
    public async Task Handle_MixedAnomalies_TypeDistributionCorrect()
    {
        var handler = CreateHandler(
            [MakeWaste("a"), MakeWaste("b")],
            [MakeDrift("c")],
            [],
            [MakeChaos("d")],
            ["e", "e"]);

        var result = await handler.Handle(DefaultQuery(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var dist = result.Value.TypeDistribution;
        Assert.Equal(2, dist.WasteSignalCount);
        Assert.Equal(1, dist.DriftFindingCount);
        Assert.Equal(0, dist.SloBreachCount);
        Assert.Equal(1, dist.ChaosFailureCount);
        Assert.Equal(2, dist.VulnerabilityCount);
    }

    // ── Timeline: 30 daily points ─────────────────────────────────────────

    [Fact]
    public async Task Handle_WithAnomalies_TimelineHas30Points()
    {
        var handler = CreateHandler([MakeWaste("svc-x")], [], [], [], []);
        var result = await handler.Handle(DefaultQuery(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(30, result.Value.AnomalyTimeline.Count);
    }

    // ── TopByAnomalyCount ordering ────────────────────────────────────────

    [Fact]
    public async Task Handle_MultipleServices_TopByCountOrdered()
    {
        const string svcMany = "svc-many";
        const string svcFew = "svc-few";

        var handler = CreateHandler(
            [MakeWaste(svcMany), MakeWaste(svcFew)],
            [MakeDrift(svcMany)],
            [MakeSloBreached(svcMany)],
            [], []);

        var result = await handler.Handle(DefaultQuery(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(svcMany, result.Value.TopByAnomalyCount.First().ServiceName);
    }

    // ── Period filter: old anomalies excluded ─────────────────────────────

    [Fact]
    public async Task Handle_WasteSignalOlderThanLookback_Excluded()
    {
        // Signal 40 days ago, lookback = 30 → excluded
        var oldWaste = MakeWaste("svc-old", FixedNow.AddDays(-40));
        var handler = CreateHandler([oldWaste], [], [], [], []);

        var result = await handler.Handle(DefaultQuery(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(0, result.Value.TotalServicesWithAnomalies);
        Assert.Empty(result.Value.AllServices);
    }

    // ── Validator ─────────────────────────────────────────────────────────

    [Fact]
    public void Validator_EmptyTenantId_Fails()
    {
        var v = new GetAnomalyDetectionSummaryReport.Validator();
        Assert.False(v.Validate(new GetAnomalyDetectionSummaryReport.Query("")).IsValid);
    }

    [Fact]
    public void Validator_LookbackDaysOutOfRange_Fails()
    {
        var v = new GetAnomalyDetectionSummaryReport.Validator();
        Assert.False(v.Validate(new GetAnomalyDetectionSummaryReport.Query(TenantId, LookbackDays: 5)).IsValid);
        Assert.False(v.Validate(new GetAnomalyDetectionSummaryReport.Query(TenantId, LookbackDays: 100)).IsValid);
    }

    [Fact]
    public void Validator_DefaultQuery_IsValid()
    {
        var v = new GetAnomalyDetectionSummaryReport.Validator();
        Assert.True(v.Validate(new GetAnomalyDetectionSummaryReport.Query(TenantId)).IsValid);
    }

    // ── MultiAnomalyServices threshold = 3 ───────────────────────────────

    [Fact]
    public async Task Handle_TwoAnomalyTypes_NotInMultiAnomalyList()
    {
        const string svc = "svc-two";
        var handler = CreateHandler(
            [MakeWaste(svc)],
            [MakeDrift(svc)],
            [], [], []);

        var result = await handler.Handle(DefaultQuery(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value.MultiAnomalyServices);
        Assert.Equal(GetAnomalyDetectionSummaryReport.AnomalyDensity.Moderate,
            result.Value.AllServices.Single().Density);
    }

    // ── TotalServicesWithAnomalies ────────────────────────────────────────

    [Fact]
    public async Task Handle_OneServiceWithAnomaly_TotalServicesWithAnomaliesIsOne()
    {
        var handler = CreateHandler([MakeWaste("svc-has")], [], [], [], []);
        var result = await handler.Handle(DefaultQuery(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(1, result.Value.TotalServicesWithAnomalies);
    }
}
