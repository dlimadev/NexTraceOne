using System.Linq;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.OperationalIntelligence.Application.Runtime.Abstractions;
using NexTraceOne.OperationalIntelligence.Application.Runtime.Features.GetChaosCoverageGapReport;
using NexTraceOne.OperationalIntelligence.Domain.Runtime.Entities;
using NexTraceOne.OperationalIntelligence.Domain.Runtime.Enums;

namespace NexTraceOne.OperationalIntelligence.Tests.Runtime.Application;

/// <summary>
/// Testes unitários para Wave V.2 — GetChaosCoverageGapReport.
/// Cobre: sem serviços/experimentos, NoCoverage, ProductionGap, FailedCoverage,
/// PartialCoverage (Running), FullCoverage, CriticalGap, multi-serviço, CoverageRate, validator.
/// </summary>
public sealed class ChaosCoverageGapReportTests
{
    private static readonly DateTimeOffset FixedNow = new(2026, 4, 21, 12, 0, 0, TimeSpan.Zero);
    private const string TenantId = "tenant-chaos-gap-v02";
    private const string ProdEnv = "production";
    private const string StagingEnv = "staging";

    private static IDateTimeProvider CreateClock()
    {
        var clock = Substitute.For<IDateTimeProvider>();
        clock.UtcNow.Returns(FixedNow);
        return clock;
    }

    private static ChaosExperiment MakeExperiment(
        string serviceName,
        string environment,
        ExperimentStatus status,
        DateTimeOffset? createdAt = null)
    {
        var at = createdAt ?? FixedNow.AddDays(-5);
        var exp = ChaosExperiment.Create(
            tenantId: TenantId,
            serviceName: serviceName,
            environment: environment,
            experimentType: "latency-injection",
            description: null,
            riskLevel: "Low",
            durationSeconds: 60,
            targetPercentage: 10m,
            steps: ["step1"],
            safetyChecks: ["check1"],
            createdAt: at,
            createdBy: "system");

        if (status >= ExperimentStatus.Running)
            exp.Start(at.AddMinutes(1));
        if (status == ExperimentStatus.Completed)
            exp.Complete(at.AddMinutes(2));
        else if (status == ExperimentStatus.Failed)
            exp.Fail(at.AddMinutes(2));
        else if (status == ExperimentStatus.Cancelled)
            exp.Cancel(at.AddMinutes(1));

        return exp;
    }

    private static GetChaosCoverageGapReport.Handler CreateHandler(
        IReadOnlyList<ChaosExperiment> experiments,
        IReadOnlyList<string> knownServices)
    {
        var experimentRepo = Substitute.For<IChaosExperimentRepository>();
        var serviceNamesReader = Substitute.For<IActiveServiceNamesReader>();

        experimentRepo.ListAsync(
                Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<string?>(),
                Arg.Any<ExperimentStatus?>(), Arg.Any<CancellationToken>())
            .Returns(experiments);

        serviceNamesReader.ListActiveServiceNamesAsync(
                Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(knownServices);

        return new GetChaosCoverageGapReport.Handler(experimentRepo, serviceNamesReader, CreateClock());
    }

    private static GetChaosCoverageGapReport.Query DefaultQuery()
        => new(TenantId: TenantId, LookbackDays: 90, MaxTopServices: 10, ProductionEnvironmentName: ProdEnv);

    // ── Empty: no experiments, no known services ───────────────────────────

    [Fact]
    public async Task Handle_NoExperimentsAndNoKnownServices_ReturnsEmptyReport()
    {
        var handler = CreateHandler([], []);
        var result = await handler.Handle(DefaultQuery(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var r = result.Value;
        Assert.Equal(0, r.TotalServicesAnalyzed);
        Assert.Empty(r.AllServices);
        Assert.Equal(0m, r.CoverageRatePct);
    }

    // ── NoCoverage: known service but no experiments ──────────────────────

    [Fact]
    public async Task Handle_KnownServiceWithNoExperiments_ClassifiesAsNoCoverage()
    {
        var handler = CreateHandler([], ["svc-no-coverage"]);
        var result = await handler.Handle(DefaultQuery(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var entry = result.Value.AllServices.Single(e => e.ServiceName == "svc-no-coverage");
        Assert.Equal(GetChaosCoverageGapReport.GapLevel.NoCoverage, entry.Gap);
        Assert.True(entry.CriticalGap);
    }

    // ── ProductionGap: experiments only in non-production ─────────────────

    [Fact]
    public async Task Handle_OnlyStagingExperiments_ClassifiesAsProductionGap()
    {
        var exp = MakeExperiment("svc-staging-only", StagingEnv, ExperimentStatus.Completed);
        var handler = CreateHandler([exp], ["svc-staging-only"]);
        var result = await handler.Handle(DefaultQuery(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var entry = result.Value.AllServices.Single(e => e.ServiceName == "svc-staging-only");
        Assert.Equal(GetChaosCoverageGapReport.GapLevel.ProductionGap, entry.Gap);
    }

    // ── FailedCoverage: all prod experiments failed/cancelled ─────────────

    [Fact]
    public async Task Handle_AllProdExperimentsFailed_ClassifiesAsFailedCoverage()
    {
        var failedExp = MakeExperiment("svc-failed", ProdEnv, ExperimentStatus.Failed);
        var cancelledExp = MakeExperiment("svc-failed", ProdEnv, ExperimentStatus.Cancelled);

        var handler = CreateHandler([failedExp, cancelledExp], ["svc-failed"]);
        var result = await handler.Handle(DefaultQuery(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var entry = result.Value.AllServices.Single(e => e.ServiceName == "svc-failed");
        Assert.Equal(GetChaosCoverageGapReport.GapLevel.FailedCoverage, entry.Gap);
        Assert.Equal(2, entry.ProductionExperimentsInPeriod);
        Assert.Equal(0, entry.CompletedProductionExperiments);
    }

    // ── PartialCoverage: prod experiment Planned (no terminal status) ─────

    [Fact]
    public async Task Handle_ProdExperimentsOnlyPlanned_ClassifiesAsPartialCoverage()
    {
        var plannedExp = MakeExperiment("svc-partial", ProdEnv, ExperimentStatus.Planned);

        var handler = CreateHandler([plannedExp], ["svc-partial"]);
        var result = await handler.Handle(DefaultQuery(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var entry = result.Value.AllServices.Single(e => e.ServiceName == "svc-partial");
        Assert.Equal(GetChaosCoverageGapReport.GapLevel.PartialCoverage, entry.Gap);
    }

    // ── FullCoverage: at least 1 Completed prod experiment ────────────────

    [Fact]
    public async Task Handle_CompletedProdExperiment_ClassifiesAsFullCoverage()
    {
        var exp = MakeExperiment("svc-covered", ProdEnv, ExperimentStatus.Completed);
        var handler = CreateHandler([exp], ["svc-covered"]);
        var result = await handler.Handle(DefaultQuery(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var entry = result.Value.AllServices.Single(e => e.ServiceName == "svc-covered");
        Assert.Equal(GetChaosCoverageGapReport.GapLevel.FullCoverage, entry.Gap);
        Assert.False(entry.CriticalGap);
        Assert.Equal(1, entry.CompletedProductionExperiments);
    }

    // ── CoverageRate calculation ───────────────────────────────────────────

    [Fact]
    public async Task Handle_MultipleServices_CorrectCoverageRate()
    {
        // 2 services: 1 FullCoverage, 1 NoCoverage → 50%
        var exp = MakeExperiment("svc-full", ProdEnv, ExperimentStatus.Completed);
        var handler = CreateHandler([exp], ["svc-full", "svc-none"]);
        var result = await handler.Handle(DefaultQuery(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(50.0m, result.Value.CoverageRatePct);
        Assert.Equal(1, result.Value.GapDistribution.FullCoverageCount);
        Assert.Equal(1, result.Value.GapDistribution.NoCoverageCount);
    }

    // ── Fallback: no known services → derive from experiments ─────────────

    [Fact]
    public async Task Handle_NoKnownServices_DerivesFromExperiments()
    {
        var exp1 = MakeExperiment("svc-derived-a", ProdEnv, ExperimentStatus.Completed);
        var exp2 = MakeExperiment("svc-derived-b", StagingEnv, ExperimentStatus.Completed);

        var handler = CreateHandler([exp1, exp2], []);
        var result = await handler.Handle(DefaultQuery(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value.TotalServicesAnalyzed);
        Assert.Contains(result.Value.AllServices, e => e.ServiceName == "svc-derived-a");
        Assert.Contains(result.Value.AllServices, e => e.ServiceName == "svc-derived-b");
    }

    // ── TopCriticalGap ordering ───────────────────────────────────────────

    [Fact]
    public async Task Handle_MultipleCriticalGaps_OrderedByWorstFirst()
    {
        var expProdGap = MakeExperiment("svc-b", StagingEnv, ExperimentStatus.Completed);

        var handler = CreateHandler([expProdGap], ["svc-a", "svc-b"]);
        var result = await handler.Handle(DefaultQuery(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        // svc-a has NoCoverage (gap=0 = worse), svc-b has ProductionGap (gap=1)
        var top = result.Value.TopCriticalGapServices;
        Assert.Equal("svc-a", top.First().ServiceName);
    }

    // ── Experiments outside lookback period are ignored ───────────────────

    [Fact]
    public async Task Handle_ExperimentOutsidePeriod_IsIgnored()
    {
        // Experiment created 100 days ago — outside default 90-day lookback
        var oldExp = MakeExperiment("svc-old", ProdEnv, ExperimentStatus.Completed,
            createdAt: FixedNow.AddDays(-100));

        var handler = CreateHandler([oldExp], ["svc-old"]);
        var result = await handler.Handle(DefaultQuery(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var entry = result.Value.AllServices.Single(e => e.ServiceName == "svc-old");
        // Despite having a completed experiment, it was outside the period → NoCoverage
        Assert.Equal(GetChaosCoverageGapReport.GapLevel.NoCoverage, entry.Gap);
    }

    // ── Validator ─────────────────────────────────────────────────────────

    [Fact]
    public async Task Validator_EmptyTenantId_Fails()
    {
        var v = new GetChaosCoverageGapReport.Validator();
        var r = await v.ValidateAsync(new GetChaosCoverageGapReport.Query(TenantId: ""));
        Assert.False(r.IsValid);
    }

    [Fact]
    public async Task Validator_ValidQuery_Passes()
    {
        var v = new GetChaosCoverageGapReport.Validator();
        var r = await v.ValidateAsync(DefaultQuery());
        Assert.True(r.IsValid);
    }

    [Fact]
    public async Task Validator_LookbackDaysTooLow_Fails()
    {
        var v = new GetChaosCoverageGapReport.Validator();
        var r = await v.ValidateAsync(new GetChaosCoverageGapReport.Query(TenantId: TenantId, LookbackDays: 5));
        Assert.False(r.IsValid);
    }
}
