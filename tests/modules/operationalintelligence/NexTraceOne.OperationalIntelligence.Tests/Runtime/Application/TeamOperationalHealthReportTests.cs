using System.Linq;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.OperationalIntelligence.Application.Runtime.Abstractions;
using NexTraceOne.OperationalIntelligence.Application.Runtime.Features.GetTeamOperationalHealthReport;
using OperationalHealthTier = NexTraceOne.OperationalIntelligence.Application.Runtime.Features.GetTeamOperationalHealthReport.GetTeamOperationalHealthReport.OperationalHealthTier;

namespace NexTraceOne.OperationalIntelligence.Tests.Runtime.Application;

/// <summary>
/// Testes unitários para Wave R.3 — GetTeamOperationalHealthReport.
/// Cobre: relatório vazio, score composto (SLO 40%/Drift 30%/Chaos 20%/Profiling 10%),
/// classificação por tier (Excellent/Good/Fair/Poor), distribuição, top healthy/at-risk, avg tenant.
/// </summary>
public sealed class TeamOperationalHealthReportTests
{
    private static readonly DateTimeOffset FixedNow = new(2026, 4, 21, 12, 0, 0, TimeSpan.Zero);

    private static IDateTimeProvider CreateClock()
    {
        var clock = Substitute.For<IDateTimeProvider>();
        clock.UtcNow.Returns(FixedNow);
        return clock;
    }

    private static GetTeamOperationalHealthReport.Handler CreateHandler(
        IReadOnlyList<TeamOperationalMetrics> metrics)
    {
        var reader = Substitute.For<ITeamOperationalMetricsReader>();
        reader.ListTeamMetricsAsync(
                Arg.Any<string>(), Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(),
                Arg.Any<CancellationToken>())
            .Returns(metrics);
        return new GetTeamOperationalHealthReport.Handler(reader, CreateClock());
    }

    private static GetTeamOperationalHealthReport.Query DefaultQuery()
        => new(TenantId: "tenant-1", LookbackDays: 30, TopTeamsCount: 10, MaxDriftPenaltyPerService: 5);

    // ── Empty: no teams ───────────────────────────────────────────────────

    [Fact]
    public async Task Handle_NoTeams_ReturnsZeroTotals()
    {
        var handler = CreateHandler([]);
        var result = await handler.Handle(DefaultQuery(), CancellationToken.None);
        Assert.True(result.IsSuccess);
        var r = result.Value;
        Assert.Equal(0, r.TotalTeamsAnalyzed);
        Assert.Equal(0m, r.TenantAvgHealthScore);
        Assert.Empty(r.AllTeams);
        Assert.Empty(r.TopHealthyTeams);
        Assert.Empty(r.TopAtRiskTeams);
    }

    // ── Excellent tier: SLO=100, no drift, chaos=100, profiling=100% ──────

    [Fact]
    public async Task Handle_PerfectMetrics_ClassifiesAsExcellent()
    {
        var metrics = new TeamOperationalMetrics(
            TeamName: "team-a",
            ServiceCount: 5,
            SloComplianceRatePct: 100m,
            UnacknowledgedDriftCount: 0,
            ChaosSuccessRatePct: 100m,
            ServicesWithProfilingCount: 5,
            PostDeployIncidentCount: 0);

        var handler = CreateHandler([metrics]);
        var result = await handler.Handle(DefaultQuery(), CancellationToken.None);
        Assert.True(result.IsSuccess);
        var entry = result.Value.AllTeams.First();
        Assert.Equal(OperationalHealthTier.Excellent, entry.HealthTier);
        Assert.Equal(100m, entry.CompositeHealthScore);
        Assert.Equal(1, result.Value.TierDistribution.ExcellentCount);
    }

    // ── Poor tier: zero SLO compliance, maximum drift, zero chaos ─────────

    [Fact]
    public async Task Handle_ZeroMetrics_ClassifiesAsPoor()
    {
        var metrics = new TeamOperationalMetrics(
            TeamName: "team-bad",
            ServiceCount: 5,
            SloComplianceRatePct: 0m,
            UnacknowledgedDriftCount: 25, // 5 per service → max penalty
            ChaosSuccessRatePct: 0m,
            ServicesWithProfilingCount: 0,
            PostDeployIncidentCount: 10);

        var handler = CreateHandler([metrics]);
        var result = await handler.Handle(DefaultQuery(), CancellationToken.None);
        Assert.True(result.IsSuccess);
        var entry = result.Value.AllTeams.First();
        Assert.Equal(OperationalHealthTier.Poor, entry.HealthTier);
        Assert.Equal(0m, entry.CompositeHealthScore);
        Assert.Equal(1, result.Value.TierDistribution.PoorCount);
    }

    // ── Score formula: SLO=80, drift=0/2svc→100, chaos=60, profiling=50% ──

    [Fact]
    public async Task Handle_MixedMetrics_ScoreIsWeightedComposite()
    {
        // SLO=80 * 0.40 + drift=100 * 0.30 + chaos=60 * 0.20 + profiling=50 * 0.10
        // = 32 + 30 + 12 + 5 = 79
        var metrics = new TeamOperationalMetrics(
            TeamName: "team-mix",
            ServiceCount: 2,
            SloComplianceRatePct: 80m,
            UnacknowledgedDriftCount: 0,
            ChaosSuccessRatePct: 60m,
            ServicesWithProfilingCount: 1,
            PostDeployIncidentCount: 1);

        var handler = CreateHandler([metrics]);
        var result = await handler.Handle(DefaultQuery(), CancellationToken.None);
        Assert.True(result.IsSuccess);
        var entry = result.Value.AllTeams.First();
        Assert.Equal(79m, entry.CompositeHealthScore);
        Assert.Equal(OperationalHealthTier.Good, entry.HealthTier);
    }

    // ── Drift penalty capped at 100 penalty ───────────────────────────────

    [Fact]
    public async Task Handle_ExcessiveDrift_DriftScoreClampedAtZero()
    {
        // 100 drifts for 1 service → drift penalty per service = 100 > maxPenalty(5) → driftScore = 0
        var metrics = new TeamOperationalMetrics(
            TeamName: "team-drift",
            ServiceCount: 1,
            SloComplianceRatePct: 100m,
            UnacknowledgedDriftCount: 100,
            ChaosSuccessRatePct: 100m,
            ServicesWithProfilingCount: 1,
            PostDeployIncidentCount: 0);

        var handler = CreateHandler([metrics]);
        var result = await handler.Handle(DefaultQuery(), CancellationToken.None);
        Assert.True(result.IsSuccess);
        var entry = result.Value.AllTeams.First();
        // SLO=100*0.4 + Drift=0*0.3 + Chaos=100*0.2 + Profiling=100*0.1 = 40+0+20+10 = 70
        Assert.Equal(70m, entry.CompositeHealthScore);
        Assert.Equal(OperationalHealthTier.Good, entry.HealthTier);
    }

    // ── Good tier boundary (score = 70) ───────────────────────────────────

    [Fact]
    public async Task Handle_Score70_ClassifiesAsGood()
    {
        // Craft: SLO=80*0.4 + Drift=80*0.3 + Chaos=60*0.2 + Profiling=0*0.1 = 32+24+12+0 = 68 → Fair
        // SLO=90*0.4 + Drift=80*0.3 + Chaos=60*0.2 + Prof=0*0.1 = 36+24+12+0 = 72 → Good
        var metrics = new TeamOperationalMetrics(
            TeamName: "team-good",
            ServiceCount: 5,
            SloComplianceRatePct: 90m,
            UnacknowledgedDriftCount: 5,   // 1 per service → 80% drift score
            ChaosSuccessRatePct: 60m,
            ServicesWithProfilingCount: 0,
            PostDeployIncidentCount: 0);

        var handler = CreateHandler([metrics]);
        var result = await handler.Handle(DefaultQuery(), CancellationToken.None);
        Assert.True(result.IsSuccess);
        var entry = result.Value.AllTeams.First();
        Assert.Equal(OperationalHealthTier.Good, entry.HealthTier);
    }

    // ── Fair tier boundary ────────────────────────────────────────────────

    [Fact]
    public async Task Handle_Score50to69_ClassifiesAsFair()
    {
        // SLO=60*0.4 + Drift=100*0.3 + Chaos=0*0.2 + Prof=0*0.1 = 24+30+0+0 = 54 → Fair
        var metrics = new TeamOperationalMetrics(
            TeamName: "team-fair",
            ServiceCount: 2,
            SloComplianceRatePct: 60m,
            UnacknowledgedDriftCount: 0,
            ChaosSuccessRatePct: 0m,
            ServicesWithProfilingCount: 0,
            PostDeployIncidentCount: 2);

        var handler = CreateHandler([metrics]);
        var result = await handler.Handle(DefaultQuery(), CancellationToken.None);
        Assert.True(result.IsSuccess);
        var entry = result.Value.AllTeams.First();
        Assert.Equal(OperationalHealthTier.Fair, entry.HealthTier);
        Assert.Equal(1, result.Value.TierDistribution.FairCount);
    }

    // ── Multiple teams: tier distribution ────────────────────────────────

    [Fact]
    public async Task Handle_MultipleTeams_DistributionCoversTiers()
    {
        var metrics = new List<TeamOperationalMetrics>
        {
            new("team-excellent", 2, 100m, 0, 100m, 2, 0), // Excellent
            new("team-good", 2, 80m, 0, 80m, 2, 0),        // 32+30+16+10 = 88 → Good? Let me check: 80*0.4+100*0.3+80*0.2+100*0.1 = 32+30+16+10 = 88 → Excellent
            new("team-fair", 2, 60m, 0, 0m, 0, 0),          // 60*0.4+100*0.3+0*0.2+0*0.1 = 24+30 = 54 → Fair
            new("team-poor", 2, 0m, 10, 0m, 0, 5),          // 0+drift_score*0.3+0+0, drift=max penalty → 0
        };

        var handler = CreateHandler(metrics);
        var result = await handler.Handle(DefaultQuery(), CancellationToken.None);
        Assert.True(result.IsSuccess);
        Assert.Equal(4, result.Value.TotalTeamsAnalyzed);
        var dist = result.Value.TierDistribution;
        Assert.True(dist.ExcellentCount + dist.GoodCount + dist.FairCount + dist.PoorCount == 4);
        Assert.Equal(1, dist.FairCount);
        Assert.Equal(1, dist.PoorCount);
    }

    // ── TenantAvgHealthScore ──────────────────────────────────────────────

    [Fact]
    public async Task Handle_TenantAvgHealthScore_IsMeanOfTeamScores()
    {
        var metrics = new List<TeamOperationalMetrics>
        {
            new("team-a", 5, 100m, 0, 100m, 5, 0),  // score = 100
            new("team-b", 5, 0m, 25, 0m, 0, 0)       // score = 0
        };

        var handler = CreateHandler(metrics);
        var result = await handler.Handle(DefaultQuery(), CancellationToken.None);
        Assert.True(result.IsSuccess);
        Assert.Equal(50m, result.Value.TenantAvgHealthScore);
    }

    // ── TopHealthyTeams ordered by score descending ───────────────────────

    [Fact]
    public async Task Handle_TopHealthyTeams_OrderedByScoreDescending()
    {
        var metrics = new List<TeamOperationalMetrics>
        {
            new("team-low", 2, 50m, 0, 50m, 0, 0),
            new("team-high", 2, 100m, 0, 100m, 2, 0),
            new("team-mid", 2, 75m, 0, 75m, 1, 0),
        };

        var handler = CreateHandler(metrics);
        var result = await handler.Handle(DefaultQuery(), CancellationToken.None);
        Assert.True(result.IsSuccess);
        var top = result.Value.TopHealthyTeams;
        Assert.True(top.Count >= 2);
        Assert.True(top[0].CompositeHealthScore >= top[1].CompositeHealthScore);
        Assert.Equal("team-high", top[0].TeamName);
    }

    // ── TopAtRiskTeams ordered by score ascending ─────────────────────────

    [Fact]
    public async Task Handle_TopAtRiskTeams_OrderedByScoreAscending()
    {
        var metrics = new List<TeamOperationalMetrics>
        {
            new("team-low", 2, 50m, 0, 50m, 0, 0),
            new("team-high", 2, 100m, 0, 100m, 2, 0),
            new("team-zero", 2, 0m, 10, 0m, 0, 5),
        };

        var handler = CreateHandler(metrics);
        var result = await handler.Handle(DefaultQuery(), CancellationToken.None);
        Assert.True(result.IsSuccess);
        var atRisk = result.Value.TopAtRiskTeams;
        Assert.True(atRisk[0].CompositeHealthScore <= atRisk[1].CompositeHealthScore);
        Assert.Equal("team-zero", atRisk[0].TeamName);
    }

    // ── AllTeams ordered alphabetically by TeamName ───────────────────────

    [Fact]
    public async Task Handle_AllTeams_OrderedAlphabetically()
    {
        var metrics = new List<TeamOperationalMetrics>
        {
            new("team-z", 1, 80m, 0, 80m, 1, 0),
            new("team-a", 1, 80m, 0, 80m, 1, 0),
            new("team-m", 1, 80m, 0, 80m, 1, 0),
        };

        var handler = CreateHandler(metrics);
        var result = await handler.Handle(DefaultQuery(), CancellationToken.None);
        Assert.True(result.IsSuccess);
        var names = result.Value.AllTeams.Select(t => t.TeamName).ToList();
        Assert.Equal("team-a", names[0]);
        Assert.Equal("team-m", names[1]);
        Assert.Equal("team-z", names[2]);
    }

    // ── Zero service count: profiling score is 0 ─────────────────────────

    [Fact]
    public async Task Handle_ZeroServiceCount_ProfilingScoreIsZero()
    {
        var metrics = new TeamOperationalMetrics(
            TeamName: "team-empty",
            ServiceCount: 0,
            SloComplianceRatePct: 100m,
            UnacknowledgedDriftCount: 0,
            ChaosSuccessRatePct: 100m,
            ServicesWithProfilingCount: 0,
            PostDeployIncidentCount: 0);

        var handler = CreateHandler([metrics]);
        var result = await handler.Handle(DefaultQuery(), CancellationToken.None);
        Assert.True(result.IsSuccess);
        var entry = result.Value.AllTeams.First();
        // SLO=100*0.4 + Drift=100*0.3 + Chaos=100*0.2 + Prof=0*0.1 = 40+30+20+0 = 90
        Assert.Equal(90m, entry.CompositeHealthScore);
        Assert.Equal(OperationalHealthTier.Excellent, entry.HealthTier);
    }

    // ── GeneratedAt matches clock ─────────────────────────────────────────

    [Fact]
    public async Task Handle_GeneratedAt_MatchesClock()
    {
        var handler = CreateHandler([]);
        var result = await handler.Handle(DefaultQuery(), CancellationToken.None);
        Assert.True(result.IsSuccess);
        Assert.Equal(FixedNow, result.Value.GeneratedAt);
    }

    // ── LookbackDays echoed in report ─────────────────────────────────────

    [Fact]
    public async Task Handle_LookbackDays_EchoedInReport()
    {
        var handler = CreateHandler([]);
        var query = DefaultQuery() with { LookbackDays = 14 };
        var result = await handler.Handle(query, CancellationToken.None);
        Assert.True(result.IsSuccess);
        Assert.Equal(14, result.Value.LookbackDays);
    }

    // ── Validator: TenantId required ──────────────────────────────────────

    [Fact]
    public void Validator_EmptyTenantId_Fails()
    {
        var validator = new GetTeamOperationalHealthReport.Validator();
        var result = validator.Validate(new GetTeamOperationalHealthReport.Query(TenantId: ""));
        Assert.False(result.IsValid);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(91)]
    public void Validator_LookbackDaysOutOfRange_Fails(int days)
    {
        var validator = new GetTeamOperationalHealthReport.Validator();
        var result = validator.Validate(new GetTeamOperationalHealthReport.Query(
            TenantId: "tenant-1", LookbackDays: days));
        Assert.False(result.IsValid);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(21)]
    public void Validator_MaxDriftPenaltyOutOfRange_Fails(int penalty)
    {
        var validator = new GetTeamOperationalHealthReport.Validator();
        var result = validator.Validate(new GetTeamOperationalHealthReport.Query(
            TenantId: "tenant-1", MaxDriftPenaltyPerService: penalty));
        Assert.False(result.IsValid);
    }

    [Fact]
    public void Validator_ValidQuery_Passes()
    {
        var validator = new GetTeamOperationalHealthReport.Validator();
        var result = validator.Validate(DefaultQuery());
        Assert.True(result.IsValid);
    }

    // ── PostDeployIncidentCount is stored in entry ────────────────────────

    [Fact]
    public async Task Handle_PostDeployIncidentCount_StoredInEntry()
    {
        var metrics = new TeamOperationalMetrics(
            TeamName: "team-x",
            ServiceCount: 3,
            SloComplianceRatePct: 90m,
            UnacknowledgedDriftCount: 1,
            ChaosSuccessRatePct: 80m,
            ServicesWithProfilingCount: 2,
            PostDeployIncidentCount: 7);

        var handler = CreateHandler([metrics]);
        var result = await handler.Handle(DefaultQuery(), CancellationToken.None);
        Assert.True(result.IsSuccess);
        Assert.Equal(7, result.Value.AllTeams.First().PostDeployIncidentCount);
    }

    // ── TopTeamsCount limits result sets ──────────────────────────────────

    [Fact]
    public async Task Handle_TopTeamsCount_LimitsResults()
    {
        var metrics = Enumerable.Range(1, 15)
            .Select(i => new TeamOperationalMetrics(
                $"team-{i}", 1, i * 5m, 0, i * 5m, 1, 0))
            .ToList();

        var handler = CreateHandler(metrics);
        var query = DefaultQuery() with { TopTeamsCount = 5 };
        var result = await handler.Handle(query, CancellationToken.None);
        Assert.True(result.IsSuccess);
        Assert.True(result.Value.TopHealthyTeams.Count <= 5);
        Assert.True(result.Value.TopAtRiskTeams.Count <= 5);
        Assert.Equal(15, result.Value.AllTeams.Count);
    }
}
