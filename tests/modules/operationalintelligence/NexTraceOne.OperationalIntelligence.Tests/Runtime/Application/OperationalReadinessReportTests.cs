using System.Linq;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.OperationalIntelligence.Application.Runtime.Abstractions;
using NexTraceOne.OperationalIntelligence.Application.Runtime.Features.GetOperationalReadinessReport;
using NexTraceOne.OperationalIntelligence.Domain.Runtime.Entities;
using NexTraceOne.OperationalIntelligence.Domain.Runtime.Enums;

namespace NexTraceOne.OperationalIntelligence.Tests.Runtime.Application;

/// <summary>
/// Testes unitários para Wave L.3 — Operational Readiness Report.
/// Cobre scoring composto, classificação de readiness e detecção de bloqueadores.
/// </summary>
public sealed class OperationalReadinessReportTests
{
    private static readonly DateTimeOffset FixedNow = new(2026, 4, 21, 12, 0, 0, TimeSpan.Zero);
    private const string TenantId = "tenant-readiness";
    private const string ServiceName = "payment-svc";
    private const string SourceEnv = "staging";
    private const string TargetEnv = "production";

    private static IDateTimeProvider CreateClock()
    {
        var clock = Substitute.For<IDateTimeProvider>();
        clock.UtcNow.Returns(FixedNow);
        return clock;
    }

    private static SloObservation MakeSlo(SloObservationStatus status = SloObservationStatus.Met)
        => SloObservation.Create(
            TenantId, ServiceName, SourceEnv,
            "availability",
            status == SloObservationStatus.Met ? 99.9m : status == SloObservationStatus.Warning ? 95m : 70m,
            99.5m,
            FixedNow.AddHours(-1), FixedNow, FixedNow);

    private static ChaosExperiment MakeChaos(ExperimentStatus experimentStatus)
    {
        var exp = ChaosExperiment.Create(
            TenantId, ServiceName, SourceEnv, "network-latency",
            null, "Low", 300, 10m, ["step1"], ["check1"], FixedNow.AddHours(-2), "ops@example.com");
        exp.Start(FixedNow.AddHours(-2));
        if (experimentStatus == ExperimentStatus.Completed)
            exp.Complete(FixedNow.AddHours(-1));
        else if (experimentStatus == ExperimentStatus.Failed)
            exp.Fail(FixedNow.AddHours(-1), "injected error");
        return exp;
    }

    private static ProfilingSession MakeProfiling(DateTimeOffset windowStart)
        => ProfilingSession.Start(
            TenantId, ServiceName, SourceEnv,
            ProfilingFrameType.DotNetTrace,
            windowStart, windowStart.AddMinutes(30),
            5000, 512m, 32, FixedNow.AddMinutes(-15));

    private static RuntimeSnapshot MakeSnapshot(DateTimeOffset capturedAt)
        => RuntimeSnapshot.Create(ServiceName, SourceEnv, 50m, 80m, 0.01m, 100m, 20m, 512m, 2, capturedAt, "otel");

    private static DriftFinding MakeDrift()
        => DriftFinding.Detect(ServiceName, SourceEnv, "latency_p99", 80m, 200m, FixedNow.AddHours(-1));

    private static GetOperationalReadinessReport.Handler CreateHandler(
        IReadOnlyList<SloObservation>? slos = null,
        IReadOnlyList<ChaosExperiment>? experiments = null,
        ProfilingSession? profiling = null,
        IReadOnlyList<DriftFinding>? drifts = null,
        RuntimeSnapshot? snapshot = null)
    {
        var sloRepo = Substitute.For<ISloObservationRepository>();
        sloRepo.ListByServiceAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<SloObservation>)(slos ?? []));

        var chaosRepo = Substitute.For<IChaosExperimentRepository>();
        chaosRepo.ListAsync(Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<ExperimentStatus?>(), Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<ChaosExperiment>)(experiments ?? []));

        var profilingRepo = Substitute.For<IProfilingSessionRepository>();
        profilingRepo.GetLatestByServiceAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(profiling);

        var driftRepo = Substitute.For<IDriftFindingRepository>();
        driftRepo.ListUnacknowledgedAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<DriftFinding>)(drifts ?? []));

        var snapshotRepo = Substitute.For<IRuntimeSnapshotRepository>();
        snapshotRepo.GetLatestByServiceAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(snapshot);

        return new GetOperationalReadinessReport.Handler(
            sloRepo, chaosRepo, profilingRepo, driftRepo, snapshotRepo, CreateClock());
    }

    // ── Handler tests ────────────────────────────────────────────────────

    [Fact]
    public async Task GetOperationalReadinessReport_NoData_Returns_ConditionallyReady_WithBlockers()
    {
        var handler = CreateHandler();
        var result = await handler.Handle(
            new GetOperationalReadinessReport.Query(TenantId, ServiceName, SourceEnv, TargetEnv),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Classification.Should().Be(GetOperationalReadinessReport.ReadinessClassification.ConditionallyReady);
        result.Value.Blockers.Should().NotBeEmpty();
        result.Value.Dimensions.TotalSloObservations.Should().Be(0);
    }

    [Fact]
    public async Task GetOperationalReadinessReport_AllGreen_Returns_ReadyForProduction()
    {
        var slos = new[] { MakeSlo(SloObservationStatus.Met), MakeSlo(SloObservationStatus.Met) }.ToList();
        var chaos = new[] { MakeChaos(ExperimentStatus.Completed) }.ToList();
        var profiling = MakeProfiling(FixedNow.AddDays(-5));
        var snapshot = MakeSnapshot(FixedNow.AddDays(-5));

        var handler = CreateHandler(slos, chaos, profiling, [], snapshot);
        var result = await handler.Handle(
            new GetOperationalReadinessReport.Query(TenantId, ServiceName, SourceEnv, TargetEnv),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Classification.Should().Be(GetOperationalReadinessReport.ReadinessClassification.ReadyForProduction);
        result.Value.Blockers.Should().BeEmpty();
        result.Value.CompositeScore.Should().BeGreaterThanOrEqualTo(80m);
    }

    [Fact]
    public async Task GetOperationalReadinessReport_SloBreached_Adds_Blocker()
    {
        var slos = new[] { MakeSlo(SloObservationStatus.Breached) }.ToList();
        var handler = CreateHandler(slos: slos);

        var result = await handler.Handle(
            new GetOperationalReadinessReport.Query(TenantId, ServiceName, SourceEnv, TargetEnv),
            CancellationToken.None);

        result.Value.Blockers.Should().Contain(b => b.Contains("SLO breach"));
        result.Value.Dimensions.SloBreachedCount.Should().Be(1);
    }

    [Fact]
    public async Task GetOperationalReadinessReport_ChaosFailed_Adds_Blocker()
    {
        var slos = new[] { MakeSlo(SloObservationStatus.Met) }.ToList();
        var chaos = new[] { MakeChaos(ExperimentStatus.Failed) }.ToList();
        var profiling = MakeProfiling(FixedNow.AddDays(-5));
        var snapshot = MakeSnapshot(FixedNow.AddDays(-5));

        var handler = CreateHandler(slos, chaos, profiling, [], snapshot);
        var result = await handler.Handle(
            new GetOperationalReadinessReport.Query(TenantId, ServiceName, SourceEnv, TargetEnv),
            CancellationToken.None);

        result.Value.Blockers.Should().Contain(b => b.Contains("chaos experiment"));
        result.Value.Dimensions.ChaosFailed.Should().Be(1);
    }

    [Fact]
    public async Task GetOperationalReadinessReport_UnacknowledgedDrift_Adds_Blocker()
    {
        var slos = new[] { MakeSlo(SloObservationStatus.Met) }.ToList();
        var profiling = MakeProfiling(FixedNow.AddDays(-5));
        var snapshot = MakeSnapshot(FixedNow.AddDays(-5));
        var drifts = new[] { MakeDrift() }.ToList();

        var handler = CreateHandler(slos, [], profiling, drifts, snapshot);
        var result = await handler.Handle(
            new GetOperationalReadinessReport.Query(TenantId, ServiceName, SourceEnv, TargetEnv),
            CancellationToken.None);

        result.Value.Blockers.Should().Contain(b => b.Contains("drift"));
        result.Value.Dimensions.IsDriftFree.Should().BeFalse();
        result.Value.Dimensions.UnacknowledgedDriftCount.Should().Be(1);
    }

    [Fact]
    public async Task GetOperationalReadinessReport_NoProfiling_Adds_Blocker()
    {
        var slos = new[] { MakeSlo(SloObservationStatus.Met) }.ToList();
        var snapshot = MakeSnapshot(FixedNow.AddDays(-5));

        var handler = CreateHandler(slos, [], null, [], snapshot);
        var result = await handler.Handle(
            new GetOperationalReadinessReport.Query(TenantId, ServiceName, SourceEnv, TargetEnv),
            CancellationToken.None);

        result.Value.Blockers.Should().Contain(b => b.Contains("profiling"));
        result.Value.Dimensions.HasRecentProfiling.Should().BeFalse();
    }

    [Fact]
    public async Task GetOperationalReadinessReport_NoBaseline_Adds_Blocker()
    {
        var slos = new[] { MakeSlo(SloObservationStatus.Met) }.ToList();
        var profiling = MakeProfiling(FixedNow.AddDays(-5));

        var handler = CreateHandler(slos, [], profiling, [], null);
        var result = await handler.Handle(
            new GetOperationalReadinessReport.Query(TenantId, ServiceName, SourceEnv, TargetEnv),
            CancellationToken.None);

        result.Value.Blockers.Should().Contain(b => b.Contains("baseline"));
        result.Value.Dimensions.HasRecentBaseline.Should().BeFalse();
    }

    [Fact]
    public async Task GetOperationalReadinessReport_SloComplianceRate_100_When_All_Met()
    {
        var slos = Enumerable.Range(0, 5).Select(_ => MakeSlo(SloObservationStatus.Met)).ToList();
        var profiling = MakeProfiling(FixedNow.AddDays(-5));
        var snapshot = MakeSnapshot(FixedNow.AddDays(-5));

        var handler = CreateHandler(slos, [], profiling, [], snapshot);
        var result = await handler.Handle(
            new GetOperationalReadinessReport.Query(TenantId, ServiceName, SourceEnv, TargetEnv),
            CancellationToken.None);

        result.Value.Dimensions.SloComplianceRate.Should().Be(100m);
    }

    [Fact]
    public async Task GetOperationalReadinessReport_ChaosSuccessRate_100_When_All_Completed()
    {
        var slos = new[] { MakeSlo(SloObservationStatus.Met) }.ToList();
        var chaos = new[] { MakeChaos(ExperimentStatus.Completed), MakeChaos(ExperimentStatus.Completed) }.ToList();
        var profiling = MakeProfiling(FixedNow.AddDays(-5));
        var snapshot = MakeSnapshot(FixedNow.AddDays(-5));

        var handler = CreateHandler(slos, chaos, profiling, [], snapshot);
        var result = await handler.Handle(
            new GetOperationalReadinessReport.Query(TenantId, ServiceName, SourceEnv, TargetEnv),
            CancellationToken.None);

        result.Value.Dimensions.ChaosSuccessRate.Should().Be(100m);
        result.Value.Dimensions.TotalChaosExperiments.Should().Be(2);
    }

    [Fact]
    public async Task GetOperationalReadinessReport_NotReady_When_Score_Below_60()
    {
        // Force all dimensions to fail: SLO breach, chaos fail, drift, no profiling, no baseline
        var slos = Enumerable.Range(0, 5).Select(_ => MakeSlo(SloObservationStatus.Breached)).ToList();
        var chaos = new[] { MakeChaos(ExperimentStatus.Failed) }.ToList();
        var drifts = Enumerable.Range(0, 4).Select(_ => MakeDrift()).ToList();

        var handler = CreateHandler(slos, chaos, null, drifts, null);
        var result = await handler.Handle(
            new GetOperationalReadinessReport.Query(TenantId, ServiceName, SourceEnv, TargetEnv),
            CancellationToken.None);

        result.Value.Classification.Should().Be(GetOperationalReadinessReport.ReadinessClassification.NotReady);
        result.Value.CompositeScore.Should().BeLessThan(60m);
    }

    [Fact]
    public async Task GetOperationalReadinessReport_Has_Correct_ServiceName_And_Environments()
    {
        var handler = CreateHandler();
        var result = await handler.Handle(
            new GetOperationalReadinessReport.Query(TenantId, ServiceName, SourceEnv, TargetEnv, 14),
            CancellationToken.None);

        result.Value.ServiceName.Should().Be(ServiceName);
        result.Value.SourceEnvironment.Should().Be(SourceEnv);
        result.Value.TargetEnvironment.Should().Be(TargetEnv);
        result.Value.LookbackDays.Should().Be(14);
    }

    // ── Validator tests ──────────────────────────────────────────────────

    [Fact]
    public void GetOperationalReadinessReport_Validator_Rejects_InvalidDays()
    {
        var validator = new GetOperationalReadinessReport.Validator();
        validator.Validate(new GetOperationalReadinessReport.Query(TenantId, ServiceName, SourceEnv, TargetEnv, 0))
            .IsValid.Should().BeFalse();
        validator.Validate(new GetOperationalReadinessReport.Query(TenantId, ServiceName, SourceEnv, TargetEnv, 91))
            .IsValid.Should().BeFalse();
    }

    [Fact]
    public void GetOperationalReadinessReport_Validator_Rejects_Empty_ServiceName()
    {
        var validator = new GetOperationalReadinessReport.Validator();
        validator.Validate(new GetOperationalReadinessReport.Query(TenantId, "", SourceEnv, TargetEnv))
            .IsValid.Should().BeFalse();
    }

    [Fact]
    public void GetOperationalReadinessReport_Validator_Accepts_Valid_Query()
    {
        var validator = new GetOperationalReadinessReport.Validator();
        validator.Validate(new GetOperationalReadinessReport.Query(TenantId, ServiceName, SourceEnv, TargetEnv, 30))
            .IsValid.Should().BeTrue();
    }
}
