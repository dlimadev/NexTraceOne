using System.Linq;
using FluentAssertions;
using NSubstitute;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.OperationalIntelligence.Application.Runtime.Abstractions;
using NexTraceOne.OperationalIntelligence.Application.Runtime.Features.GetIncidentProbabilityReport;
using NexTraceOne.OperationalIntelligence.Domain.Runtime.Entities;
using NexTraceOne.OperationalIntelligence.Domain.Runtime.Enums;

namespace NexTraceOne.OperationalIntelligence.Tests.Runtime.Application;

/// <summary>
/// Testes unitários para Wave AI.3 — GetIncidentProbabilityReport.
/// Cobre: sem serviços, tiers Unlikely/Possible/Probable/Imminent,
/// sinais OpenDriftSignals/SloBreachTrend/ChaosGap/HighRiskRelease/OpenVulnerabilities,
/// AlertServicesList, TenantRiskHeatmap, ProbabilityExplanation, Validator.
/// </summary>
public sealed class WaveAiIncidentProbabilityReportTests
{
    private static readonly DateTimeOffset FixedNow = new(2026, 4, 22, 12, 0, 0, TimeSpan.Zero);
    private const string TenantId = "tenant-ai3-incident";

    private static IDateTimeProvider CreateClock()
    {
        var clock = Substitute.For<IDateTimeProvider>();
        clock.UtcNow.Returns(FixedNow);
        return clock;
    }

    private static DriftFinding MakeOpenHighDrift(string svc, string env = "production")
    {
        var d = DriftFinding.Detect(svc, env, "AvgLatencyMs", 100m, 180m, FixedNow.AddDays(-1));
        // High severity: deviation > 50% which is Critical
        return d;
    }

    private static SloObservation MakeSloBreached(string svc)
        => SloObservation.Create(TenantId, svc, "production", "availability",
            observedValue: 0.90m, sloTarget: 0.99m,
            periodStart: FixedNow.AddHours(-72), periodEnd: FixedNow.AddHours(-1),
            observedAt: FixedNow.AddHours(-1));

    private static ChaosExperiment MakeChaosCompleted(string svc)
    {
        var exp = ChaosExperiment.Create(
            TenantId, svc, "production", "latency-injection", null, "Low", 60, 10m,
            ["step1"], ["check1"], FixedNow.AddDays(-10), "test-user");
        exp.Start(FixedNow.AddDays(-10));
        exp.Complete(FixedNow.AddDays(-9));
        return exp;
    }

    private static GetIncidentProbabilityReport.Handler CreateHandler(
        IReadOnlyList<string>? serviceNames = null,
        IReadOnlyList<DriftFinding>? driftFindings = null,
        IReadOnlyList<SloObservation>? sloObs = null,
        IReadOnlyList<ChaosExperiment>? chaosExps = null,
        IReadOnlyList<string>? vulnServiceNames = null,
        decimal forecastRiskScore = 0m)
    {
        var driftRepo = Substitute.For<IDriftFindingRepository>();
        driftRepo
            .ListByTenantInPeriodAsync(Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns(driftFindings ?? []);

        var sloRepo = Substitute.For<ISloObservationRepository>();
        sloRepo
            .ListByTenantAsync(Arg.Any<string>(), Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(),
                Arg.Any<SloObservationStatus?>(), Arg.Any<CancellationToken>())
            .Returns(sloObs ?? []);

        var chaosRepo = Substitute.For<IChaosExperimentRepository>();
        chaosRepo
            .ListAsync(Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<string?>(),
                Arg.Any<ExperimentStatus?>(), Arg.Any<CancellationToken>())
            .Returns(chaosExps ?? []);

        var vulnReader = Substitute.For<IVulnerabilityAdvisoryReader>();
        vulnReader
            .ListCriticalOrHighServiceNamesInPeriodAsync(Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns(vulnServiceNames ?? []);

        var forecastReader = Substitute.For<IDeploymentRiskForecastReader>();
        forecastReader
            .GetMaxRecentForecastRiskScoreAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(forecastRiskScore);

        var activeNamesReader = Substitute.For<IActiveServiceNamesReader>();
        activeNamesReader
            .ListActiveServiceNamesAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(serviceNames ?? []);

        return new GetIncidentProbabilityReport.Handler(
            driftRepo, sloRepo, chaosRepo, vulnReader, forecastReader, activeNamesReader, CreateClock());
    }

    // ── Empty: no services ────────────────────────────────────────────────

    [Fact]
    public async Task Handle_NoServices_ReturnsEmptyReport()
    {
        var handler = CreateHandler();
        var result = await handler.Handle(
            new GetIncidentProbabilityReport.Query(TenantId),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TotalServicesAnalyzed.Should().Be(0);
        result.Value.AllServices.Should().BeEmpty();
        result.Value.AlertServicesList.Should().BeEmpty();
    }

    // ── Tier Unlikely: no signals ─────────────────────────────────────────

    [Fact]
    public async Task Handle_NoSignals_ReturnsTierUnlikely()
    {
        // Service with chaos coverage (no gap) and no other signals
        var chaos = new List<ChaosExperiment> { MakeChaosCompleted("svc-clean") };
        var handler = CreateHandler(
            serviceNames: ["svc-clean"],
            chaosExps: chaos);

        var result = await handler.Handle(
            new GetIncidentProbabilityReport.Query(TenantId),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var svc = result.Value.AllServices.Single();
        svc.ServiceName.Should().Be("svc-clean");
        svc.Tier.Should().Be(GetIncidentProbabilityReport.IncidentProbabilityTier.Unlikely);
    }

    // ── ChaosGap signal (20%) ─────────────────────────────────────────────

    [Fact]
    public async Task Handle_ChaosGapNoExperiment_SignalContributes20Pct()
    {
        var handler = CreateHandler(serviceNames: ["svc-nochaos"]);
        var result = await handler.Handle(
            new GetIncidentProbabilityReport.Query(TenantId),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var svc = result.Value.AllServices.Single();
        var chaosSignal = svc.Signals.Single(s => s.SignalName == "ChaosGap");
        chaosSignal.RawScore.Should().Be(100m);
        chaosSignal.WeightedScore.Should().Be(20m);  // 100 * 0.20
    }

    // ── OpenDriftSignals signal ───────────────────────────────────────────

    [Fact]
    public async Task Handle_OpenDriftFindings_ContributeToScore()
    {
        // 5 high/critical drift findings (cap = 5 = 100%)
        var drifts = Enumerable.Range(0, 5)
            .Select(_ => MakeOpenHighDrift("svc-drift"))
            .ToList();

        var chaos = new List<ChaosExperiment> { MakeChaosCompleted("svc-drift") };

        var handler = CreateHandler(
            serviceNames: ["svc-drift"],
            driftFindings: drifts,
            chaosExps: chaos);

        var result = await handler.Handle(
            new GetIncidentProbabilityReport.Query(TenantId),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var svc = result.Value.AllServices.Single();
        var driftSignal = svc.Signals.Single(s => s.SignalName == "OpenDriftSignals");
        driftSignal.RawScore.Should().Be(100m);
        driftSignal.WeightedScore.Should().Be(25m);  // 100 * 0.25
    }

    // ── SloBreachTrend signal ─────────────────────────────────────────────

    [Fact]
    public async Task Handle_AllSloObsBreached_BreachSignalIs100()
    {
        var sloObs = new List<SloObservation>
        {
            MakeSloBreached("svc-slo"),
            MakeSloBreached("svc-slo")
        };

        var chaos = new List<ChaosExperiment> { MakeChaosCompleted("svc-slo") };

        var handler = CreateHandler(
            serviceNames: ["svc-slo"],
            sloObs: sloObs,
            chaosExps: chaos);

        var result = await handler.Handle(
            new GetIncidentProbabilityReport.Query(TenantId),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var svc = result.Value.AllServices.Single();
        var sloSignal = svc.Signals.Single(s => s.SignalName == "SloBreachTrend");
        sloSignal.RawScore.Should().Be(100m);
        sloSignal.WeightedScore.Should().Be(25m);  // 100 * 0.25
    }

    // ── RecentHighRiskRelease signal ──────────────────────────────────────

    [Fact]
    public async Task Handle_HighForecastRiskScore_SignalContributes20Pct()
    {
        var handler = CreateHandler(
            serviceNames: ["svc-release"],
            forecastRiskScore: 80m);  // above threshold 75

        var result = await handler.Handle(
            new GetIncidentProbabilityReport.Query(TenantId),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var svc = result.Value.AllServices.Single();
        var releaseSignal = svc.Signals.Single(s => s.SignalName == "RecentHighRiskRelease");
        releaseSignal.RawScore.Should().Be(100m);
        releaseSignal.WeightedScore.Should().Be(20m);  // 100 * 0.20
    }

    // ── Tier Imminent (all signals converge) ──────────────────────────────

    [Fact]
    public async Task Handle_AllSignalsMaxed_ReturnsTierImminent()
    {
        // Max chaos gap (20) + max drift (25) + max SLO breach (25) + max release (20) + vuln (10) = 100
        var drifts = Enumerable.Range(0, 5).Select(_ => MakeOpenHighDrift("svc-max")).ToList();
        var sloObs = Enumerable.Range(0, 4).Select(_ => MakeSloBreached("svc-max")).ToList();

        var handler = CreateHandler(
            serviceNames: ["svc-max"],
            driftFindings: drifts,
            sloObs: sloObs,
            vulnServiceNames: ["svc-max", "svc-max", "svc-max", "svc-max", "svc-max"],
            forecastRiskScore: 80m);

        var result = await handler.Handle(
            new GetIncidentProbabilityReport.Query(TenantId),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var svc = result.Value.AllServices.Single();
        svc.Tier.Should().Be(GetIncidentProbabilityReport.IncidentProbabilityTier.Imminent);
        result.Value.AlertServicesList.Should().ContainSingle()
            .Which.ServiceName.Should().Be("svc-max");
    }

    // ── AlertServicesList only contains Imminent ──────────────────────────

    [Fact]
    public async Task Handle_AlertServicesList_OnlyContainsImminent()
    {
        var drifts = Enumerable.Range(0, 5).Select(_ => MakeOpenHighDrift("svc-alert")).ToList();
        var sloObs = Enumerable.Range(0, 4).Select(_ => MakeSloBreached("svc-alert")).ToList();

        var handler = CreateHandler(
            serviceNames: ["svc-alert", "svc-clean-x"],
            driftFindings: drifts,
            sloObs: sloObs,
            forecastRiskScore: 80m);

        var result = await handler.Handle(
            new GetIncidentProbabilityReport.Query(TenantId),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        // Only Imminent tier services appear in AlertServicesList
        result.Value.AlertServicesList.Should().OnlyContain(
            s => s.Tier == GetIncidentProbabilityReport.IncidentProbabilityTier.Imminent);
    }

    // ── ProbabilityExplanation ────────────────────────────────────────────

    [Fact]
    public async Task Handle_ProbabilityExplanation_ContainsAtMostThreeFactors()
    {
        var handler = CreateHandler(serviceNames: ["svc-explain-x"]);
        var result = await handler.Handle(
            new GetIncidentProbabilityReport.Query(TenantId),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.AllServices.Single().ProbabilityExplanation.Count
            .Should().BeLessThanOrEqualTo(3);
    }

    // ── TenantRiskHeatmap ─────────────────────────────────────────────────

    [Fact]
    public async Task Handle_TwoServices_HeatmapDistributionIsCorrect()
    {
        var chaos = new List<ChaosExperiment>
        {
            MakeChaosCompleted("svc-1"),
            MakeChaosCompleted("svc-2")
        };

        var handler = CreateHandler(
            serviceNames: ["svc-1", "svc-2"],
            chaosExps: chaos);

        var result = await handler.Handle(
            new GetIncidentProbabilityReport.Query(TenantId),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var dist = result.Value.RiskHeatmap.Distribution;
        (dist.UnlikelyCount + dist.PossibleCount + dist.ProbableCount + dist.ImminentCount)
            .Should().Be(2);
        result.Value.RiskHeatmap.Top10RiskiestServices.Should().HaveCountLessThanOrEqualTo(10);
    }

    // ── Validator ─────────────────────────────────────────────────────────

    [Fact]
    public void Validator_EmptyTenantId_ReturnsError()
    {
        var v = new GetIncidentProbabilityReport.Validator();
        var result = v.Validate(new GetIncidentProbabilityReport.Query(string.Empty));
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validator_MaxTopServicesZero_ReturnsError()
    {
        var v = new GetIncidentProbabilityReport.Validator();
        var result = v.Validate(new GetIncidentProbabilityReport.Query(TenantId, MaxTopServices: 0));
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validator_ImminentThresholdOutOfRange_ReturnsError()
    {
        var v = new GetIncidentProbabilityReport.Validator();
        var result = v.Validate(new GetIncidentProbabilityReport.Query(TenantId,
            ImminentThresholdOverride: 95m));  // above max 90
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validator_ValidQuery_IsValid()
    {
        var v = new GetIncidentProbabilityReport.Validator();
        var result = v.Validate(new GetIncidentProbabilityReport.Query(TenantId));
        result.IsValid.Should().BeTrue();
    }
}
