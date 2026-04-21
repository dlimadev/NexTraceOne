using System.Linq;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.OperationalIntelligence.Application.Runtime.Abstractions;
using NexTraceOne.OperationalIntelligence.Application.Runtime.Features.GetEnvironmentStabilityReport;
using NexTraceOne.OperationalIntelligence.Domain.Runtime.Entities;
using NexTraceOne.OperationalIntelligence.Domain.Runtime.Enums;

namespace NexTraceOne.OperationalIntelligence.Tests.Runtime.Application;

/// <summary>
/// Testes unitários para Wave T.3 — GetEnvironmentStabilityReport.
/// Cobre: sem ambientes, ambiente único estável, ambiente crítico, multi-ambiente,
/// non-prod mais instável que prod (flag), distribuição de StabilityTier, validator.
/// </summary>
public sealed class EnvironmentStabilityReportTests
{
    private static readonly DateTimeOffset FixedNow = new(2026, 4, 21, 12, 0, 0, TimeSpan.Zero);
    private const string TenantId = "tenant-env-stability-001";
    private const string ProdEnv = "prod";
    private const string StagingEnv = "staging";

    private static IDateTimeProvider CreateClock()
    {
        var clock = Substitute.For<IDateTimeProvider>();
        clock.UtcNow.Returns(FixedNow);
        return clock;
    }

    private static SloObservation MakeSlo(
        string serviceName,
        string env,
        SloObservationStatus status,
        DateTimeOffset? observedAt = null)
    {
        var at = observedAt ?? FixedNow.AddDays(-5);
        decimal observed = status == SloObservationStatus.Met ? 99.9m : 70m;
        return SloObservation.Create(TenantId, serviceName, env,
            "availability", observed, 99.5m,
            at.AddHours(-1), at, at);
    }

    private static ChaosExperiment MakeChaos(
        string serviceName,
        string env,
        ExperimentStatus status)
    {
        var exp = ChaosExperiment.Create(
            TenantId, serviceName, env, "network-latency",
            null, "Low", 300, 10m, ["step1"], ["check1"],
            FixedNow.AddDays(-5), "ops@example.com");
        exp.Start(FixedNow.AddDays(-5));
        if (status == ExperimentStatus.Completed)
            exp.Complete(FixedNow.AddDays(-4));
        else if (status == ExperimentStatus.Failed)
            exp.Fail(FixedNow.AddDays(-4), "failure");
        return exp;
    }

    private static DriftFinding MakeDrift(string serviceName, string env)
        => DriftFinding.Detect(serviceName, env, "latency_p99", 80m, 200m, FixedNow.AddDays(-2));

    private static GetEnvironmentStabilityReport.Handler CreateHandler(
        IReadOnlyList<SloObservation> sloObs,
        IReadOnlyList<DriftFinding> driftFindings,
        IReadOnlyList<ChaosExperiment> chaosExperiments)
    {
        var sloRepo = Substitute.For<ISloObservationRepository>();
        sloRepo.ListByTenantAsync(
                Arg.Any<string>(), Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(),
                Arg.Any<SloObservationStatus?>(), Arg.Any<CancellationToken>())
            .Returns(sloObs);

        var driftRepo = Substitute.For<IDriftFindingRepository>();
        driftRepo.ListUnacknowledgedAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(driftFindings);

        var chaosRepo = Substitute.For<IChaosExperimentRepository>();
        chaosRepo.ListAsync(
                Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<string?>(),
                Arg.Any<ExperimentStatus?>(), Arg.Any<CancellationToken>())
            .Returns(chaosExperiments);

        return new GetEnvironmentStabilityReport.Handler(sloRepo, driftRepo, chaosRepo, CreateClock());
    }

    private static GetEnvironmentStabilityReport.Query DefaultQuery()
        => new(TenantId: TenantId, LookbackDays: 30);

    // ── Empty: no SLO observations, no chaos → empty report ───────────────

    [Fact]
    public async Task Handle_NoData_ReturnsEmptyReport()
    {
        var handler = CreateHandler([], [], []);
        var result = await handler.Handle(DefaultQuery(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var r = result.Value;
        Assert.Equal(0, r.TotalEnvironmentsAnalyzed);
        Assert.Empty(r.Environments);
        Assert.False(r.NonProdMoreUnstableThanProd);
    }

    // ── Single stable prod environment (all Met, no drift, chaos success) ──

    [Fact]
    public async Task Handle_ProdEnvironmentAllMet_StableTier()
    {
        var sloObs = new[]
        {
            MakeSlo("svc-a", ProdEnv, SloObservationStatus.Met),
            MakeSlo("svc-b", ProdEnv, SloObservationStatus.Met),
        };
        var chaos = new[] { MakeChaos("svc-a", ProdEnv, ExperimentStatus.Completed) };

        var handler = CreateHandler(sloObs, [], chaos);
        var result = await handler.Handle(DefaultQuery(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var r = result.Value;
        Assert.Equal(1, r.TotalEnvironmentsAnalyzed);

        var env = r.Environments.Single();
        Assert.Equal(ProdEnv, env.Environment);
        Assert.True(env.IsProduction);
        Assert.Equal(GetEnvironmentStabilityReport.StabilityTier.Stable, env.Tier);
        Assert.True(env.StabilityScore >= 80m, $"Expected score >= 80 but got {env.StabilityScore}");
    }

    // ── Critical environment: all SLO breached ───────────────────────────

    [Fact]
    public async Task Handle_AllBreachedSlos_CriticalTier()
    {
        var sloObs = new[]
        {
            MakeSlo("svc-critical", StagingEnv, SloObservationStatus.Breached),
            MakeSlo("svc-critical", StagingEnv, SloObservationStatus.Breached),
            MakeSlo("svc-critical", StagingEnv, SloObservationStatus.Breached),
        };

        var handler = CreateHandler(sloObs, [], []);
        var result = await handler.Handle(DefaultQuery(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var r = result.Value;
        var env = r.Environments.Single();
        Assert.Equal(StagingEnv, env.Environment);
        Assert.Equal(GetEnvironmentStabilityReport.StabilityTier.Critical, env.Tier);
        Assert.True(env.StabilityScore < 55m, $"Expected critical score < 55 but got {env.StabilityScore}");
    }

    // ── Multi-environment: prod stable, staging unstable ─────────────────

    [Fact]
    public async Task Handle_MultiEnvironment_ProdStableStagingCritical()
    {
        var sloObs = new[]
        {
            MakeSlo("svc-a", ProdEnv, SloObservationStatus.Met),
            MakeSlo("svc-a", ProdEnv, SloObservationStatus.Met),
            MakeSlo("svc-b", StagingEnv, SloObservationStatus.Breached),
            MakeSlo("svc-b", StagingEnv, SloObservationStatus.Breached),
            MakeSlo("svc-b", StagingEnv, SloObservationStatus.Breached),
        };

        var handler = CreateHandler(sloObs, [], []);
        var result = await handler.Handle(DefaultQuery(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var r = result.Value;
        Assert.Equal(2, r.TotalEnvironmentsAnalyzed);

        var prod = r.Environments.First(e => e.Environment == ProdEnv);
        var staging = r.Environments.First(e => e.Environment == StagingEnv);

        Assert.True(prod.IsProduction);
        Assert.False(staging.IsProduction);
        Assert.True(prod.StabilityScore > staging.StabilityScore);
    }

    // ── NonProdMoreUnstableThanProd flag ──────────────────────────────────

    [Fact]
    public async Task Handle_StagingWorseThanProd_FlagsNonProdMoreUnstable()
    {
        var sloObs = new[]
        {
            MakeSlo("svc-a", ProdEnv, SloObservationStatus.Met),
            MakeSlo("svc-a", ProdEnv, SloObservationStatus.Met),
            MakeSlo("svc-b", StagingEnv, SloObservationStatus.Breached),
            MakeSlo("svc-b", StagingEnv, SloObservationStatus.Breached),
            MakeSlo("svc-b", StagingEnv, SloObservationStatus.Breached),
        };

        var handler = CreateHandler(sloObs, [], []);
        var result = await handler.Handle(DefaultQuery(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.True(result.Value.NonProdMoreUnstableThanProd,
            "Expected NonProdMoreUnstableThanProd = true when staging is worse than prod");
    }

    // ── NonProdMoreUnstableThanProd NOT flagged when prod is stable and staging also stable ──

    [Fact]
    public async Task Handle_BothEnvStable_NoNonProdUnstableFlag()
    {
        var sloObs = new[]
        {
            MakeSlo("svc-a", ProdEnv, SloObservationStatus.Met),
            MakeSlo("svc-a", StagingEnv, SloObservationStatus.Met),
        };

        var handler = CreateHandler(sloObs, [], []);
        var result = await handler.Handle(DefaultQuery(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.False(result.Value.NonProdMoreUnstableThanProd);
    }

    // ── Drift findings reduce stability score ─────────────────────────────

    [Fact]
    public async Task Handle_DriftFindingInEnv_ReducesDriftScore()
    {
        var sloObs = new[] { MakeSlo("svc-a", StagingEnv, SloObservationStatus.Met) };
        var drift = new[] { MakeDrift("svc-a", StagingEnv) };

        var handler = CreateHandler(sloObs, drift, []);
        var result = await handler.Handle(DefaultQuery(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var env = result.Value.Environments.Single();
        // Drift finding is returned by the repo, so it should be counted
        Assert.Equal(1, env.Dimensions.UnacknowledgedDriftCount);
        // Drift score should be reduced from 100
        Assert.True(env.Dimensions.DriftScore < 100m,
            "Unacknowledged drift finding should reduce drift score");
    }

    // ── Chaos failed experiments reduce stability score ───────────────────

    [Fact]
    public async Task Handle_ChaosFailedExperiment_ReducesChaosScore()
    {
        var sloObs = new[] { MakeSlo("svc-a", ProdEnv, SloObservationStatus.Met) };
        var chaos = new[] { MakeChaos("svc-a", ProdEnv, ExperimentStatus.Failed) };

        var handler = CreateHandler(sloObs, [], chaos);
        var result = await handler.Handle(DefaultQuery(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var env = result.Value.Environments.Single();
        Assert.Equal(ProdEnv, env.Environment);
        Assert.True(env.Dimensions.ChaosSuccessRatePct < 100m,
            "Failed chaos experiment should reduce chaos success rate");
    }

    // ── StabilityTierDistribution counts match environments ───────────────

    [Fact]
    public async Task Handle_MixedTiers_DistributionCountsCorrect()
    {
        var sloObs = new[]
        {
            MakeSlo("svc-a", ProdEnv, SloObservationStatus.Met),       // → stable
            MakeSlo("svc-b", StagingEnv, SloObservationStatus.Breached),// → critical
            MakeSlo("svc-b", StagingEnv, SloObservationStatus.Breached),
            MakeSlo("svc-b", StagingEnv, SloObservationStatus.Breached),
        };

        var handler = CreateHandler(sloObs, [], []);
        var result = await handler.Handle(DefaultQuery(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var r = result.Value;
        Assert.Equal(1, r.TierDistribution.StableCount);
        Assert.Equal(0, r.TierDistribution.UnstableCount);
        Assert.Equal(1, r.TierDistribution.CriticalCount);
    }

    // ── Prod environments listed first in output ───────────────────────────

    [Fact]
    public async Task Handle_ProdAndNonProd_ProdListedFirst()
    {
        var sloObs = new[]
        {
            MakeSlo("svc-a", ProdEnv, SloObservationStatus.Met),
            MakeSlo("svc-b", StagingEnv, SloObservationStatus.Met),
        };

        var handler = CreateHandler(sloObs, [], []);
        var result = await handler.Handle(DefaultQuery(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var r = result.Value;
        Assert.Equal(ProdEnv, r.Environments.First().Environment);
    }

    // ── Validator: invalid lookback days ──────────────────────────────────

    [Fact]
    public void Validator_InvalidLookbackDays_ReturnsError()
    {
        var validator = new GetEnvironmentStabilityReport.Validator();
        var result = validator.Validate(new GetEnvironmentStabilityReport.Query(
            TenantId: TenantId, LookbackDays: 0));
        Assert.False(result.IsValid);
    }

    [Fact]
    public void Validator_EmptyTenantId_ReturnsError()
    {
        var validator = new GetEnvironmentStabilityReport.Validator();
        var result = validator.Validate(new GetEnvironmentStabilityReport.Query(TenantId: ""));
        Assert.False(result.IsValid);
    }

    [Fact]
    public void Validator_InvalidMaxDriftPenalty_ReturnsError()
    {
        var validator = new GetEnvironmentStabilityReport.Validator();
        var result = validator.Validate(new GetEnvironmentStabilityReport.Query(
            TenantId: TenantId, LookbackDays: 30, MaxDriftPenaltyPerService: 0));
        Assert.False(result.IsValid);
    }

    [Fact]
    public void Validator_ValidQuery_PassesValidation()
    {
        var validator = new GetEnvironmentStabilityReport.Validator();
        var result = validator.Validate(new GetEnvironmentStabilityReport.Query(
            TenantId: TenantId, LookbackDays: 30));
        Assert.True(result.IsValid);
    }
}
