using System.Linq;
using FluentAssertions;
using NSubstitute;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.OperationalIntelligence.Application.Runtime.Abstractions;
using NexTraceOne.OperationalIntelligence.Application.Runtime.Features.GetErrorBudgetReport;
using NexTraceOne.OperationalIntelligence.Application.Runtime.Features.GetIncidentImpactScorecardReport;
using NexTraceOne.OperationalIntelligence.Application.Runtime.Features.GetSreMaturityIndexReport;

namespace NexTraceOne.OperationalIntelligence.Tests.Runtime.Application;

/// <summary>
/// Testes unitários para Wave AN — SRE Intelligence &amp; Error Budget Management.
/// Cobre: AN.1 GetErrorBudgetReport, AN.2 GetIncidentImpactScorecardReport, AN.3 GetSreMaturityIndexReport.
/// </summary>
public sealed class WaveAnSreIntelligenceTests
{
    private static readonly DateTimeOffset FixedNow = new(2026, 5, 1, 12, 0, 0, TimeSpan.Zero);
    private const string TenantId = "tenant-an-sre";

    private static IDateTimeProvider CreateClock()
    {
        var clock = Substitute.For<IDateTimeProvider>();
        clock.UtcNow.Returns(FixedNow);
        return clock;
    }

    // ═══════════════════════════════════════════════════════════════════════
    // AN.1 — GetErrorBudgetReport
    // ═══════════════════════════════════════════════════════════════════════

    private static GetErrorBudgetReport.Handler CreateErrorBudgetHandler(
        IReadOnlyList<IErrorBudgetReader.ServiceSloEntry> entries)
    {
        var reader = Substitute.For<IErrorBudgetReader>();
        reader.ListByTenantAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(entries);
        return new GetErrorBudgetReport.Handler(reader, CreateClock());
    }

    private static IErrorBudgetReader.ServiceSloEntry MakeSloEntry(
        string serviceId, string serviceName, string teamName,
        decimal sloTargetPct, decimal actualCompliancePct,
        IReadOnlyList<IErrorBudgetReader.DailyBudgetSample>? dailySamples = null)
        => new(serviceId, serviceName, teamName, "tier-1",
            sloTargetPct, actualCompliancePct,
            FixedNow.AddDays(-30), FixedNow,
            dailySamples ?? []);

    [Fact]
    public async Task AN1_EmptyReader_ReturnsReportWithZeroServices()
    {
        var handler = CreateErrorBudgetHandler([]);
        var result = await handler.Handle(new GetErrorBudgetReport.Query(TenantId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TotalServicesAnalyzed.Should().Be(0);
        result.Value.Summary.HealthyServices.Should().Be(0);
        result.Value.Summary.WarningServices.Should().Be(0);
        result.Value.Summary.ExhaustedServices.Should().Be(0);
        result.Value.Summary.BurnedServices.Should().Be(0);
        result.Value.FreezeRecommendations.Should().BeEmpty();
        result.Value.ByService.Should().BeEmpty();
    }

    [Fact]
    public async Task AN1_ServiceWithComplianceBelowTarget_ReturnsBurnedTier()
    {
        // SLO 99%, actual 98% → BudgetConsumedPct = 200%, Burned
        var entry = MakeSloEntry("svc-1", "ServiceA", "TeamA", 99m, 98m);
        var handler = CreateErrorBudgetHandler([entry]);
        var result = await handler.Handle(new GetErrorBudgetReport.Query(TenantId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var row = result.Value.ByService.Single();
        row.BudgetRemainingPct.Should().BeLessThan(0m);
        row.Tier.Should().Be(GetErrorBudgetReport.ErrorBudgetTier.Burned);
    }

    [Fact]
    public async Task AN1_ServiceWithExactSloMatch_ReturnsExhaustedTier()
    {
        // SLO 99%, actual 99% → BudgetConsumedPct = 100%, Exhausted
        var entry = MakeSloEntry("svc-2", "ServiceB", "TeamB", 99m, 99m);
        var handler = CreateErrorBudgetHandler([entry]);
        var result = await handler.Handle(new GetErrorBudgetReport.Query(TenantId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var row = result.Value.ByService.Single();
        row.BudgetConsumedPct.Should().Be(100m);
        row.BudgetRemainingPct.Should().Be(0m);
        row.Tier.Should().Be(GetErrorBudgetReport.ErrorBudgetTier.Exhausted);
    }

    [Fact]
    public async Task AN1_ServiceWithHighCompliance_ReturnsHealthyTier()
    {
        // SLO 99.9%, actual 99.98% → remaining > 70% → Healthy
        var entry = MakeSloEntry("svc-3", "ServiceC", "TeamC", 99.9m, 99.98m);
        var handler = CreateErrorBudgetHandler([entry]);
        var result = await handler.Handle(new GetErrorBudgetReport.Query(TenantId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var row = result.Value.ByService.Single();
        row.BudgetRemainingPct.Should().BeGreaterThan(70m);
        row.Tier.Should().Be(GetErrorBudgetReport.ErrorBudgetTier.Healthy);
    }

    [Fact]
    public async Task AN1_ServiceWithMidRangeCompliance_ReturnsWarningTier()
    {
        // SLO 99.9%, actual 99.96% → consumed = 40%, remaining = 60% → Warning (≥30 but <70)
        var entry = MakeSloEntry("svc-4", "ServiceD", "TeamD", 99.9m, 99.96m);
        var handler = CreateErrorBudgetHandler([entry]);
        var result = await handler.Handle(new GetErrorBudgetReport.Query(TenantId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var row = result.Value.ByService.Single();
        row.BudgetRemainingPct.Should().BeGreaterThanOrEqualTo(30m).And.BeLessThan(70m);
        row.Tier.Should().Be(GetErrorBudgetReport.ErrorBudgetTier.Warning);
    }

    [Fact]
    public async Task AN1_ServiceWithFailures_DaysToExhaustionNotNull()
    {
        var entry = MakeSloEntry("svc-5", "ServiceE", "TeamE", 99.9m, 99.95m);
        var handler = CreateErrorBudgetHandler([entry]);
        var result = await handler.Handle(new GetErrorBudgetReport.Query(TenantId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var row = result.Value.ByService.Single();
        row.DaysToExhaustion.Should().NotBeNull();
        row.DaysToExhaustion.Should().BePositive();
    }

    [Fact]
    public async Task AN1_ServiceWithPerfectCompliance_DaysToExhaustionNull()
    {
        // actual 100% → actualFailurePct = 0 → burnRate = 0 → no exhaustion projection
        var entry = MakeSloEntry("svc-6", "ServiceF", "TeamF", 99m, 100m);
        var handler = CreateErrorBudgetHandler([entry]);
        var result = await handler.Handle(new GetErrorBudgetReport.Query(TenantId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var row = result.Value.ByService.Single();
        row.DaysToExhaustion.Should().BeNull();
    }

    [Fact]
    public async Task AN1_FreezeRecommendations_ContainsOnlyExhaustedAndBurned()
    {
        var entries = new[]
        {
            MakeSloEntry("svc-a", "A", "T", 99m, 100m),   // Healthy
            MakeSloEntry("svc-b", "B", "T", 99m, 99m),    // Exhausted
            MakeSloEntry("svc-c", "C", "T", 99m, 98m),    // Burned
        };
        var handler = CreateErrorBudgetHandler(entries);
        var result = await handler.Handle(new GetErrorBudgetReport.Query(TenantId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.FreezeRecommendations.Should().HaveCount(2);
        result.Value.FreezeRecommendations
            .All(r => r.Tier is GetErrorBudgetReport.ErrorBudgetTier.Exhausted
                              or GetErrorBudgetReport.ErrorBudgetTier.Burned)
            .Should().BeTrue();
    }

    [Fact]
    public async Task AN1_TopBurningServices_CappedAt5()
    {
        var entries = Enumerable.Range(1, 8)
            .Select(i => MakeSloEntry($"svc-{i}", $"Svc{i}", "T", 99m, 99m - i * 0.1m))
            .ToArray();
        var handler = CreateErrorBudgetHandler(entries);
        var result = await handler.Handle(new GetErrorBudgetReport.Query(TenantId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Summary.TopBurningServices.Should().HaveCount(5);
    }

    [Fact]
    public async Task AN1_SummaryCountsCorrect()
    {
        var entries = new[]
        {
            MakeSloEntry("s1", "A", "T", 99m, 100m),   // Healthy
            MakeSloEntry("s2", "B", "T", 99.9m, 99.96m), // Warning
            MakeSloEntry("s3", "C", "T", 99m, 99m),    // Exhausted
            MakeSloEntry("s4", "D", "T", 99m, 98m),    // Burned
        };
        var handler = CreateErrorBudgetHandler(entries);
        var result = await handler.Handle(new GetErrorBudgetReport.Query(TenantId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Summary.HealthyServices.Should().Be(1);
        result.Value.Summary.WarningServices.Should().Be(1);
        result.Value.Summary.ExhaustedServices.Should().Be(1);
        result.Value.Summary.BurnedServices.Should().Be(1);
    }

    [Fact]
    public async Task AN1_Validator_EmptyTenantId_Invalid()
    {
        var validator = new GetErrorBudgetReport.Validator();
        var result = validator.Validate(new GetErrorBudgetReport.Query(""));
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task AN1_Validator_PeriodDaysZero_Invalid()
    {
        var validator = new GetErrorBudgetReport.Validator();
        var result = validator.Validate(new GetErrorBudgetReport.Query(TenantId, PeriodDays: 0));
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task AN1_DailyTimeline_BuiltFromSamples()
    {
        var samples = new[]
        {
            new IErrorBudgetReader.DailyBudgetSample(FixedNow.AddDays(-2), 99.95m),
            new IErrorBudgetReader.DailyBudgetSample(FixedNow.AddDays(-1), 99.92m),
        };
        var entry = MakeSloEntry("svc-t", "SvcT", "T", 99.9m, 99.94m, samples);
        var handler = CreateErrorBudgetHandler([entry]);
        var result = await handler.Handle(new GetErrorBudgetReport.Query(TenantId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.ByService.Single().Timeline.Should().HaveCount(2);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // AN.2 — GetIncidentImpactScorecardReport
    // ═══════════════════════════════════════════════════════════════════════

    private static GetIncidentImpactScorecardReport.Handler CreateScorecardHandler(
        IReadOnlyList<IIncidentImpactScorecardReader.IncidentEntry> entries)
    {
        var reader = Substitute.For<IIncidentImpactScorecardReader>();
        reader.ListByTenantAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(entries);
        return new GetIncidentImpactScorecardReport.Handler(reader, CreateClock());
    }

    private static IIncidentImpactScorecardReader.IncidentEntry MakeIncident(
        string id, string serviceId, string serviceName, string teamId, string teamName,
        int durationMinutes, int blastRadius, decimal sloImpactPct, bool customerFacing)
        => new(id, serviceId, serviceName, teamId, teamName,
            durationMinutes, blastRadius, sloImpactPct, customerFacing, FixedNow.AddDays(-1));

    [Fact]
    public async Task AN2_EmptyReader_ReturnsZeroIncidentsAndFullHealthIndex()
    {
        var handler = CreateScorecardHandler([]);
        var result = await handler.Handle(new GetIncidentImpactScorecardReport.Query(TenantId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TotalIncidentsAnalyzed.Should().Be(0);
        result.Value.TenantIncidentHealthIndex.Should().Be(100m);
        result.Value.ByTeam.Should().BeEmpty();
        result.Value.TopImpactfulIncidents.Should().BeEmpty();
        result.Value.RepeatOffenderServices.Should().BeEmpty();
    }

    [Fact]
    public async Task AN2_MaxImpactIncident_ReturnsCriticalTier()
    {
        // Duration=480 (max), BlastRadius=20, SloImpact=100%, CustomerFacing=true → score=100
        var entry = MakeIncident("i1", "svc1", "Svc1", "t1", "T1", 480, 20, 100m, true);
        var handler = CreateScorecardHandler([entry]);
        var result = await handler.Handle(new GetIncidentImpactScorecardReport.Query(TenantId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var row = result.Value.TopImpactfulIncidents.Single();
        row.IncidentImpactScore.Should().Be(100m);
        row.Tier.Should().Be(GetIncidentImpactScorecardReport.ImpactTier.Critical);
    }

    [Fact]
    public async Task AN2_ZeroImpactIncident_ReturnsMinorTier()
    {
        var entry = MakeIncident("i2", "svc2", "Svc2", "t2", "T2", 0, 0, 0m, false);
        var handler = CreateScorecardHandler([entry]);
        var result = await handler.Handle(new GetIncidentImpactScorecardReport.Query(TenantId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var row = result.Value.TopImpactfulIncidents.Single();
        row.IncidentImpactScore.Should().Be(0m);
        row.Tier.Should().Be(GetIncidentImpactScorecardReport.ImpactTier.Minor);
    }

    [Fact]
    public async Task AN2_ImpactTierModerate_ScoreBetween25And55()
    {
        // Score that lands in Moderate range (26-55): duration 120 min (30% of 480 = 25% weight → 7.5),
        // blast 5 (25% of max 20 = 25% weight → 6.25), slo 50% weight → 12.5, cf false → 0 → total ~26.25
        var entry = MakeIncident("i3", "svc3", "Svc3", "t3", "T3", 120, 5, 50m, false);
        var handler = CreateScorecardHandler([entry]);
        var result = await handler.Handle(new GetIncidentImpactScorecardReport.Query(TenantId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var row = result.Value.TopImpactfulIncidents.Single();
        row.Tier.Should().Be(GetIncidentImpactScorecardReport.ImpactTier.Moderate);
        row.IncidentImpactScore.Should().BeGreaterThan(25m).And.BeLessThanOrEqualTo(55m);
    }

    [Fact]
    public async Task AN2_ImpactTierSevere_ScoreBetween55And80()
    {
        // Duration 360 (75% of 480), blast 10 (50% of 20), slo 80%, cf false
        // → (75*0.3 + 50*0.25 + 80*0.25 + 0*0.2) = 22.5 + 12.5 + 20 + 0 = 55 → Moderate threshold is >55 for Severe
        // Use: Duration 480 (100), blast 10 (50), slo 80, cf false → 30+12.5+20+0=62.5 → Severe
        var entry = MakeIncident("i4", "svc4", "Svc4", "t4", "T4", 480, 10, 80m, false);
        var handler = CreateScorecardHandler([entry]);
        var result = await handler.Handle(new GetIncidentImpactScorecardReport.Query(TenantId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var row = result.Value.TopImpactfulIncidents.Single();
        row.Tier.Should().Be(GetIncidentImpactScorecardReport.ImpactTier.Severe);
        row.IncidentImpactScore.Should().BeGreaterThan(55m).And.BeLessThanOrEqualTo(80m);
    }

    [Fact]
    public async Task AN2_TeamExcellentReliability_WhenLowAvgAndFewSevere()
    {
        // 1 incident, low score → Excellent
        var entry = MakeIncident("i5", "svc5", "Svc5", "t5", "T5", 10, 1, 5m, false);
        var handler = CreateScorecardHandler([entry]);
        var result = await handler.Handle(new GetIncidentImpactScorecardReport.Query(TenantId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.ByTeam.Single().ReliabilityTier.Should().Be(GetIncidentImpactScorecardReport.TeamReliabilityTier.Excellent);
    }

    [Fact]
    public async Task AN2_TeamStrugglingReliability_WhenHighAvgScore()
    {
        // High scoring incidents in one team → Struggling
        var entries = Enumerable.Range(1, 3)
            .Select(i => MakeIncident($"i-s{i}", "svc6", "Svc6", "t6", "T6", 480, 20, 100m, true))
            .ToArray();
        var handler = CreateScorecardHandler(entries);
        var result = await handler.Handle(new GetIncidentImpactScorecardReport.Query(TenantId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.ByTeam.Single().ReliabilityTier.Should().Be(GetIncidentImpactScorecardReport.TeamReliabilityTier.Struggling);
    }

    [Fact]
    public async Task AN2_RepeatOffenderServices_WhenThresholdReached()
    {
        var entries = Enumerable.Range(1, 4)
            .Select(i => MakeIncident($"i-r{i}", "svc-r", "SvcRepeat", "t7", "T7", 60, 2, 10m, false))
            .ToArray();
        var handler = CreateScorecardHandler(entries);
        var result = await handler.Handle(
            new GetIncidentImpactScorecardReport.Query(TenantId, RepeatIncidentThreshold: 3),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.RepeatOffenderServices.Should().HaveCount(1);
        result.Value.RepeatOffenderServices.Single().IncidentCount.Should().Be(4);
    }

    [Fact]
    public async Task AN2_TenantHealthIndex_FullWhenAllExcellentOrGood()
    {
        // 1 low-score incident → Excellent team → health = 100%
        var entry = MakeIncident("i6", "svc8", "Svc8", "t8", "T8", 5, 0, 0m, false);
        var handler = CreateScorecardHandler([entry]);
        var result = await handler.Handle(new GetIncidentImpactScorecardReport.Query(TenantId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TenantIncidentHealthIndex.Should().Be(100m);
    }

    [Fact]
    public async Task AN2_TopImpactfulIncidents_CappedAt10()
    {
        var entries = Enumerable.Range(1, 15)
            .Select(i => MakeIncident($"i-top{i}", $"svc{i}", $"Svc{i}", "tAll", "TeamAll",
                i * 10, i, i * 5m, false))
            .ToArray();
        var handler = CreateScorecardHandler(entries);
        var result = await handler.Handle(new GetIncidentImpactScorecardReport.Query(TenantId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TopImpactfulIncidents.Should().HaveCount(10);
    }

    [Fact]
    public async Task AN2_BlastRadiusCapped_AtMaxBlastRadius()
    {
        // BlastRadius=100 should be capped at max (20) for scoring purposes
        var entry = MakeIncident("i7", "svc9", "Svc9", "t9", "T9", 0, 100, 0m, false);
        var handler = CreateScorecardHandler([entry]);
        var result = await handler.Handle(new GetIncidentImpactScorecardReport.Query(TenantId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var row = result.Value.TopImpactfulIncidents.Single();
        // BlastRadius capped at 100%, CustomerFacing=0, Duration=0, Slo=0 → score = 25%*100 = 25
        row.IncidentImpactScore.Should().Be(25m);
    }

    [Fact]
    public async Task AN2_Validator_EmptyTenantId_Invalid()
    {
        var validator = new GetIncidentImpactScorecardReport.Validator();
        var result = validator.Validate(new GetIncidentImpactScorecardReport.Query(""));
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task AN2_Validator_LookbackDaysZero_Invalid()
    {
        var validator = new GetIncidentImpactScorecardReport.Validator();
        var result = validator.Validate(new GetIncidentImpactScorecardReport.Query(TenantId, LookbackDays: 0));
        result.IsValid.Should().BeFalse();
    }

    // ═══════════════════════════════════════════════════════════════════════
    // AN.3 — GetSreMaturityIndexReport
    // ═══════════════════════════════════════════════════════════════════════

    private static GetSreMaturityIndexReport.Handler CreateMaturityHandler(
        IReadOnlyList<ISreMaturityReader.TeamSreDataEntry> entries)
    {
        var reader = Substitute.For<ISreMaturityReader>();
        reader.ListByTenantAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(entries);
        return new GetSreMaturityIndexReport.Handler(reader, CreateClock());
    }

    private static ISreMaturityReader.TeamSreDataEntry MakeTeamEntry(
        string teamId, string teamName, int totalServices,
        int withSlo, int withEb, int withChaos, bool hasAutomation,
        int sevCrit, int pirCount, int totalIncidents, int withRunbook,
        decimal? prevScore = null)
        => new(teamId, teamName, totalServices, withSlo, withEb, withChaos,
            hasAutomation, sevCrit, pirCount, totalIncidents, withRunbook, prevScore);

    [Fact]
    public async Task AN3_EmptyReader_ReturnsTenantIndexOf100AndEliteTier()
    {
        var handler = CreateMaturityHandler([]);
        var result = await handler.Handle(new GetSreMaturityIndexReport.Query(TenantId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TotalTeamsAnalyzed.Should().Be(0);
        result.Value.TenantSreMaturityIndex.Should().Be(100m);
        result.Value.TenantTier.Should().Be(GetSreMaturityIndexReport.SreMaturityTier.Elite);
    }

    [Fact]
    public async Task AN3_PerfectTeam_ReturnsScoreOf100AndEliteTier()
    {
        var entry = MakeTeamEntry("t1", "T1", 5, 5, 5, 5, true, 2, 2, 5, 5);
        var handler = CreateMaturityHandler([entry]);
        var result = await handler.Handle(new GetSreMaturityIndexReport.Query(TenantId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var row = result.Value.ByTeam.Single();
        row.SreMaturityScore.Should().Be(100m);
        row.Tier.Should().Be(GetSreMaturityIndexReport.SreMaturityTier.Elite);
    }

    [Fact]
    public async Task AN3_TeamWithZeroSlos_LoweredScore()
    {
        // totalServices=5, withSlo=0 → SloDefinitionScore=0 → score reduced by 20%
        var entry = MakeTeamEntry("t2", "T2", 5, 0, 5, 5, true, 0, 0, 0, 0);
        var handler = CreateMaturityHandler([entry]);
        var result = await handler.Handle(new GetSreMaturityIndexReport.Query(TenantId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var row = result.Value.ByTeam.Single();
        row.DimensionScores.SloDefinitionScore.Should().Be(0m);
        row.SreMaturityScore.Should().BeLessThan(100m);
    }

    [Fact]
    public async Task AN3_EliteTier_WhenScoreAtLeast85()
    {
        var entry = MakeTeamEntry("t3", "T3", 10, 10, 10, 10, true, 0, 0, 0, 0);
        var handler = CreateMaturityHandler([entry]);
        var result = await handler.Handle(new GetSreMaturityIndexReport.Query(TenantId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.ByTeam.Single().Tier.Should().Be(GetSreMaturityIndexReport.SreMaturityTier.Elite);
    }

    [Fact]
    public async Task AN3_AdvancedTier_WhenScoreBetween65And84()
    {
        // SLO=50% (2/4), EB=0, Chaos=100% (4/4), Toil=true, PIR=100% (no severe), RK=100% (no incidents)
        // → 0.20*50 + 0 + 0.15*100 + 0.15*100 + 0.15*100 + 0.15*100 = 10+15+15+15+15 = 70 → Advanced
        var entry = MakeTeamEntry("t4", "T4", 4, 2, 0, 4, true, 0, 0, 0, 0);
        var handler = CreateMaturityHandler([entry]);
        var result = await handler.Handle(new GetSreMaturityIndexReport.Query(TenantId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var row = result.Value.ByTeam.Single();
        row.SreMaturityScore.Should().BeGreaterThanOrEqualTo(65m).And.BeLessThan(85m);
        row.Tier.Should().Be(GetSreMaturityIndexReport.SreMaturityTier.Advanced);
    }

    [Fact]
    public async Task AN3_PracticingTier_WhenScoreBetween40And64()
    {
        // Only toil + some pir + some runbook: slo=0, eb=0, chaos=0, toil=true (15), pir=50% (7.5), rk=50% (7.5) → 30 → Foundational
        // Let's try: slo=50%, eb=0, chaos=50%, toil=false, pir=50%, rk=50%
        // → 0.20*50 + 0.20*0 + 0.15*50 + 0 + 0.15*50 + 0.15*50 = 10+0+7.5+0+7.5+7.5 = 32.5 → Foundational
        // Try slo=100, eb=50, chaos=0, toil=false, pir=0, rk=0 → 20+10+0+0+0+0 = 30 → Foundational
        // Try slo=100, eb=100, chaos=50, toil=true, pir=0, rk=0 → 20+20+7.5+15+0+0 = 62.5 → Practicing
        var entry = MakeTeamEntry("t5", "T5", 4, 4, 4, 2, true, 5, 0, 10, 0);
        var handler = CreateMaturityHandler([entry]);
        var result = await handler.Handle(new GetSreMaturityIndexReport.Query(TenantId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var row = result.Value.ByTeam.Single();
        row.SreMaturityScore.Should().BeGreaterThanOrEqualTo(40m).And.BeLessThan(65m);
        row.Tier.Should().Be(GetSreMaturityIndexReport.SreMaturityTier.Practicing);
    }

    [Fact]
    public async Task AN3_FoundationalTier_WhenScoreBelow40()
    {
        // All zeros except toil: SLO=0, EB=0, Chaos=0, Toil=false, PIR=0 (but 5 sev incidents, 0 reviewed), RK=0
        var entry = MakeTeamEntry("t6", "T6", 5, 0, 0, 0, false, 5, 0, 5, 0);
        var handler = CreateMaturityHandler([entry]);
        var result = await handler.Handle(new GetSreMaturityIndexReport.Query(TenantId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var row = result.Value.ByTeam.Single();
        row.SreMaturityScore.Should().BeLessThan(40m);
        row.Tier.Should().Be(GetSreMaturityIndexReport.SreMaturityTier.Foundational);
    }

    [Fact]
    public async Task AN3_WeakestPractices_Contains2LowestDimensions()
    {
        // SLO=0, EB=0 → those should be the weakest
        var entry = MakeTeamEntry("t7", "T7", 5, 0, 0, 5, true, 0, 0, 5, 5);
        var handler = CreateMaturityHandler([entry]);
        var result = await handler.Handle(new GetSreMaturityIndexReport.Query(TenantId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var row = result.Value.ByTeam.Single();
        row.WeakestPractices.Should().HaveCount(2);
        row.WeakestPractices.Select(w => w.Score).All(s => s == 0m).Should().BeTrue();
    }

    [Fact]
    public async Task AN3_TenantIndexWeightedByServiceCount()
    {
        // 2 teams: one with 10 services (score 100), one with 10 services (score 0 — all zeros)
        var elite = MakeTeamEntry("tE", "Elite", 10, 10, 10, 10, true, 0, 0, 0, 0);
        var foundational = MakeTeamEntry("tF", "Foundational", 10, 0, 0, 0, false, 10, 0, 10, 0);
        var handler = CreateMaturityHandler([elite, foundational]);
        var result = await handler.Handle(new GetSreMaturityIndexReport.Query(TenantId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        // Weighted avg should be between the two extremes
        result.Value.TenantSreMaturityIndex.Should().BeGreaterThan(0m).And.BeLessThan(100m);
    }

    [Fact]
    public async Task AN3_PostIncidentReviewRate_100WhenNoSevereCriticalIncidents()
    {
        // TotalSevereOrCriticalIncidents=0 → PIR score = 100%
        var entry = MakeTeamEntry("t8", "T8", 5, 5, 5, 5, true, 0, 0, 5, 5);
        var handler = CreateMaturityHandler([entry]);
        var result = await handler.Handle(new GetSreMaturityIndexReport.Query(TenantId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.ByTeam.Single().DimensionScores.PostIncidentReviewScore.Should().Be(100m);
    }

    [Fact]
    public async Task AN3_Validator_EmptyTenantId_Invalid()
    {
        var validator = new GetSreMaturityIndexReport.Validator();
        var result = validator.Validate(new GetSreMaturityIndexReport.Query(""));
        result.IsValid.Should().BeFalse();
    }
}
