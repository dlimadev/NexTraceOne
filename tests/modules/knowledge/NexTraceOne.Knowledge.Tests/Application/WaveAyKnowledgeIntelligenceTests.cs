using System.Linq;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.Knowledge.Application.Abstractions;
using NexTraceOne.Knowledge.Application.Features.GetKnowledgeBaseUtilizationReport;
using NexTraceOne.Knowledge.Application.Features.GetTeamKnowledgeSharingReport;

namespace NexTraceOne.Knowledge.Tests.Application;

/// <summary>
/// Testes unitários para Wave AY — Organizational Knowledge &amp; Documentation Intelligence.
/// Cobre AY.2 GetKnowledgeBaseUtilizationReport (~14 testes) e AY.3 GetTeamKnowledgeSharingReport (~15 testes).
/// </summary>
public sealed class WaveAyKnowledgeIntelligenceTests
{
    private static readonly DateTimeOffset FixedNow = new(2026, 9, 1, 12, 0, 0, TimeSpan.Zero);
    private const string TenantId = "tenant-ay-001";

    private static IDateTimeProvider CreateClock()
    {
        var clock = Substitute.For<IDateTimeProvider>();
        clock.UtcNow.Returns(FixedNow);
        return clock;
    }

    // ════════════════════════════════════════════════════════════════════════
    // AY.2 — GetKnowledgeBaseUtilizationReport
    // ════════════════════════════════════════════════════════════════════════

    private static GetKnowledgeBaseUtilizationReport.Handler CreateUtilizationHandler(
        IKnowledgeBaseUtilizationReader? reader = null) =>
        new(reader ?? Substitute.For<IKnowledgeBaseUtilizationReader>(), CreateClock());

    private static IKnowledgeBaseUtilizationReader BuildUtilizationReader(
        IKnowledgeBaseUtilizationReader.KnowledgeBaseUtilizationData data)
    {
        var reader = Substitute.For<IKnowledgeBaseUtilizationReader>();
        reader.ReadByTenantAsync(TenantId, Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
              .Returns(data);
        return reader;
    }

    private static IKnowledgeBaseUtilizationReader.KnowledgeBaseUtilizationData EmptyData() =>
        new(SearchTerms: [], AccessedDocuments: [], AccessedRunbooks: [],
            DailyActiveKnowledgeUsers: 0, TotalSearchSessions: 0, SessionsWithResultClick: 0);

    private static IKnowledgeBaseUtilizationReader.SearchTermEntry MakeTerm(
        string term, int searchCount, int resultCount, int clickCount) =>
        new(term, searchCount, resultCount, clickCount);

    // ────────────────────────────────────────────────────────────────────────
    // Empty report
    // ────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task AY2_EmptyReport_WhenNoData()
    {
        var handler = CreateUtilizationHandler(BuildUtilizationReader(EmptyData()));
        var result = await handler.Handle(new GetKnowledgeBaseUtilizationReport.Query(TenantId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TopSearchTerms.Should().BeEmpty();
        result.Value.SearchTermsWithNoResults.Should().BeEmpty();
        result.Value.TopKnowledgeGaps.Should().BeEmpty();
        result.Value.Summary.DailyActiveKnowledgeUsers.Should().Be(0);
        result.Value.Summary.KnowledgeResolutionRate.Should().Be(0m);
    }

    // ────────────────────────────────────────────────────────────────────────
    // KnowledgeResolutionRate
    // ────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task AY2_KnowledgeResolutionRate_CalculatedCorrectly()
    {
        // 80 clicks out of 100 sessions = 80%
        var data = EmptyData() with { TotalSearchSessions = 100, SessionsWithResultClick = 80 };
        var handler = CreateUtilizationHandler(BuildUtilizationReader(data));
        var result = await handler.Handle(new GetKnowledgeBaseUtilizationReport.Query(TenantId), CancellationToken.None);

        result.Value.Summary.KnowledgeResolutionRate.Should().Be(80m);
    }

    [Fact]
    public async Task AY2_KnowledgeResolutionRate_ZeroWhenNoSessions()
    {
        var data = EmptyData() with { TotalSearchSessions = 0, SessionsWithResultClick = 0 };
        var handler = CreateUtilizationHandler(BuildUtilizationReader(data));
        var result = await handler.Handle(new GetKnowledgeBaseUtilizationReport.Query(TenantId), CancellationToken.None);

        result.Value.Summary.KnowledgeResolutionRate.Should().Be(0m);
    }

    // ────────────────────────────────────────────────────────────────────────
    // SearchTermsWithNoResults — gap detection
    // ────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task AY2_SearchTermsWithNoResults_DetectsGaps()
    {
        var terms = new[]
        {
            MakeTerm("runbook deploy", 10, 0, 0),  // gap
            MakeTerm("incident response", 5, 3, 2), // has results
            MakeTerm("kafka consumer", 8, 0, 0),   // gap
        };
        var data = EmptyData() with { SearchTerms = terms };
        var handler = CreateUtilizationHandler(BuildUtilizationReader(data));
        var result = await handler.Handle(new GetKnowledgeBaseUtilizationReport.Query(TenantId), CancellationToken.None);

        result.Value.SearchTermsWithNoResults.Should().HaveCount(2);
        result.Value.TopKnowledgeGaps.Should().HaveCount(2);
        result.Value.TopKnowledgeGaps.Should().Contain("runbook deploy");
        result.Value.TopKnowledgeGaps.Should().Contain("kafka consumer");
    }

    // ────────────────────────────────────────────────────────────────────────
    // KnowledgeHubHealthTier
    // ────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task AY2_Tier_Thriving_WhenHighResolutionAndFewGaps()
    {
        var query = new GetKnowledgeBaseUtilizationReport.Query(
            TenantId, ThriveResolutionRatePct: 70m, ThriveGapCountMax: 10);
        var tier = GetKnowledgeBaseUtilizationReport.Handler.ComputeTier(
            resolutionRate: 85m, gapCount: 3, dailyActiveUsers: 50, request: query);

        tier.Should().Be(GetKnowledgeBaseUtilizationReport.KnowledgeHubHealthTier.Thriving);
    }

    [Fact]
    public async Task AY2_Tier_GapHeavy_WhenManyGaps()
    {
        var query = new GetKnowledgeBaseUtilizationReport.Query(
            TenantId, ThriveResolutionRatePct: 70m, ThriveGapCountMax: 10);
        var tier = GetKnowledgeBaseUtilizationReport.Handler.ComputeTier(
            resolutionRate: 60m, gapCount: 25, dailyActiveUsers: 30, request: query);

        tier.Should().Be(GetKnowledgeBaseUtilizationReport.KnowledgeHubHealthTier.GapHeavy);
    }

    [Fact]
    public async Task AY2_Tier_Underused_WhenNoActiveUsers()
    {
        var query = new GetKnowledgeBaseUtilizationReport.Query(TenantId);
        var tier = GetKnowledgeBaseUtilizationReport.Handler.ComputeTier(
            resolutionRate: 0m, gapCount: 5, dailyActiveUsers: 0, request: query);

        tier.Should().Be(GetKnowledgeBaseUtilizationReport.KnowledgeHubHealthTier.Underused);
    }

    [Fact]
    public async Task AY2_Tier_Active_WhenModerateResolution()
    {
        var query = new GetKnowledgeBaseUtilizationReport.Query(
            TenantId, ThriveResolutionRatePct: 70m, ThriveGapCountMax: 10);
        var tier = GetKnowledgeBaseUtilizationReport.Handler.ComputeTier(
            resolutionRate: 55m, gapCount: 5, dailyActiveUsers: 20, request: query);

        tier.Should().Be(GetKnowledgeBaseUtilizationReport.KnowledgeHubHealthTier.Active);
    }

    // ────────────────────────────────────────────────────────────────────────
    // TopSearchTerms ordering
    // ────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task AY2_TopSearchTerms_OrderedByFrequency()
    {
        var terms = new[]
        {
            MakeTerm("term-a", 5, 3, 2),
            MakeTerm("term-b", 20, 10, 8),
            MakeTerm("term-c", 12, 6, 4),
        };
        var data = EmptyData() with { SearchTerms = terms };
        var handler = CreateUtilizationHandler(BuildUtilizationReader(data));
        var result = await handler.Handle(
            new GetKnowledgeBaseUtilizationReport.Query(TenantId, TopSearchTermsCount: 3), CancellationToken.None);

        result.Value.TopSearchTerms[0].Term.Should().Be("term-b");
        result.Value.TopSearchTerms[1].Term.Should().Be("term-c");
        result.Value.TopSearchTerms[2].Term.Should().Be("term-a");
    }

    // ────────────────────────────────────────────────────────────────────────
    // Validator
    // ────────────────────────────────────────────────────────────────────────

    [Fact]
    public void AY2_Validator_RejectsEmptyTenantId()
    {
        var validator = new GetKnowledgeBaseUtilizationReport.Validator();
        validator.Validate(new GetKnowledgeBaseUtilizationReport.Query("")).IsValid.Should().BeFalse();
    }

    [Fact]
    public void AY2_Validator_RejectsInvalidLookbackDays()
    {
        var validator = new GetKnowledgeBaseUtilizationReport.Validator();
        validator.Validate(new GetKnowledgeBaseUtilizationReport.Query("t", LookbackDays: 1)).IsValid.Should().BeFalse();
    }

    // ════════════════════════════════════════════════════════════════════════
    // AY.3 — GetTeamKnowledgeSharingReport
    // ════════════════════════════════════════════════════════════════════════

    private static GetTeamKnowledgeSharingReport.Handler CreateSharingHandler(
        ITeamKnowledgeSharingReader? reader = null) =>
        new(reader ?? Substitute.For<ITeamKnowledgeSharingReader>(), CreateClock());

    private static ITeamKnowledgeSharingReader BuildSharingReader(
        IReadOnlyList<ITeamKnowledgeSharingReader.TeamKnowledgeEntry> teams,
        IReadOnlyList<ITeamKnowledgeSharingReader.WeeklyKnowledgeSharingSnapshot>? trend = null,
        IReadOnlyList<ITeamKnowledgeSharingReader.ServiceKnowledgeEntry>? services = null)
    {
        var reader = Substitute.For<ITeamKnowledgeSharingReader>();
        reader.ListByTenantAsync(TenantId, Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
              .Returns(teams);
        reader.GetTenantTrendAsync(TenantId, Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
              .Returns(trend ?? []);
        reader.ListServiceContributionsAsync(TenantId, Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
              .Returns(services ?? []);
        return reader;
    }

    private static ITeamKnowledgeSharingReader.TeamKnowledgeEntry MakeTeamEntry(
        string teamId = "team-1",
        string teamName = "Team Alpha",
        int docContribs = 10,
        int crossTeamContribs = 3,
        int docConsumption = 5,
        int runbookContribs = 2,
        IReadOnlyList<string>? targetTeams = null) =>
        new(teamId, teamName, docContribs, crossTeamContribs, docConsumption, runbookContribs,
            targetTeams ?? ["team-2"]);

    // ────────────────────────────────────────────────────────────────────────
    // Empty report
    // ────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task AY3_EmptyReport_WhenNoTeams()
    {
        var handler = CreateSharingHandler(BuildSharingReader([]));
        var result = await handler.Handle(new GetTeamKnowledgeSharingReport.Query(TenantId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.ByTeam.Should().BeEmpty();
        result.Value.Summary.TenantKnowledgeSharingScore.Should().Be(100m);
        result.Value.BusFactor1Services.Should().BeEmpty();
    }

    // ────────────────────────────────────────────────────────────────────────
    // KnowledgeSharingRatio
    // ────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task AY3_KnowledgeSharingRatio_CalculatedCorrectly()
    {
        // 3 cross-team out of 10 total = 0.300
        var team = MakeTeamEntry(docContribs: 10, crossTeamContribs: 3);
        var handler = CreateSharingHandler(BuildSharingReader([team]));
        var result = await handler.Handle(new GetTeamKnowledgeSharingReport.Query(TenantId), CancellationToken.None);

        result.Value.ByTeam[0].KnowledgeSharingRatio.Should().Be(0.3m);
    }

    [Fact]
    public async Task AY3_KnowledgeSharingRatio_ZeroWhenNoContributions()
    {
        var team = MakeTeamEntry(docContribs: 0, crossTeamContribs: 0);
        var handler = CreateSharingHandler(BuildSharingReader([team]));
        var result = await handler.Handle(new GetTeamKnowledgeSharingReport.Query(TenantId), CancellationToken.None);

        result.Value.ByTeam[0].KnowledgeSharingRatio.Should().Be(0m);
    }

    // ────────────────────────────────────────────────────────────────────────
    // KnowledgeSiloRisk
    // ────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task AY3_SiloRisk_True_WhenRatioBelowThreshold()
    {
        // ratio = 0.05 < silo threshold 0.15
        var team = MakeTeamEntry(docContribs: 20, crossTeamContribs: 1);
        var handler = CreateSharingHandler(BuildSharingReader([team]));
        var result = await handler.Handle(
            new GetTeamKnowledgeSharingReport.Query(TenantId, SiloThreshold: 0.15m), CancellationToken.None);

        result.Value.ByTeam[0].KnowledgeSiloRisk.Should().BeTrue();
    }

    [Fact]
    public async Task AY3_SiloRisk_False_WhenRatioAboveThreshold()
    {
        // ratio = 0.5 > silo threshold 0.15
        var team = MakeTeamEntry(docContribs: 10, crossTeamContribs: 5);
        var handler = CreateSharingHandler(BuildSharingReader([team]));
        var result = await handler.Handle(
            new GetTeamKnowledgeSharingReport.Query(TenantId, SiloThreshold: 0.15m), CancellationToken.None);

        result.Value.ByTeam[0].KnowledgeSiloRisk.Should().BeFalse();
    }

    // ────────────────────────────────────────────────────────────────────────
    // TenantKnowledgeSharingScore
    // ────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task AY3_TenantScore_50_WhenHalfTeamsAreSilos()
    {
        var silo = MakeTeamEntry("t1", "Team-Silo", docContribs: 20, crossTeamContribs: 0, targetTeams: []);
        var healthy = MakeTeamEntry("t2", "Team-Healthy", docContribs: 10, crossTeamContribs: 5);
        var handler = CreateSharingHandler(BuildSharingReader([silo, healthy]));
        var result = await handler.Handle(
            new GetTeamKnowledgeSharingReport.Query(TenantId, SiloThreshold: 0.15m), CancellationToken.None);

        result.Value.Summary.TenantKnowledgeSharingScore.Should().Be(50m);
        result.Value.Summary.TeamsWithSiloRisk.Should().HaveCount(1);
    }

    // ────────────────────────────────────────────────────────────────────────
    // BusFactor1Services
    // ────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task AY3_BusFactor1Services_DetectsUnique()
    {
        var services = new[]
        {
            new ITeamKnowledgeSharingReader.ServiceKnowledgeEntry("svc-1", "Service A", ["user-1"]),
            new ITeamKnowledgeSharingReader.ServiceKnowledgeEntry("svc-2", "Service B", ["user-1", "user-2"]),
        };
        var handler = CreateSharingHandler(BuildSharingReader([], services: services));
        var result = await handler.Handle(
            new GetTeamKnowledgeSharingReport.Query(TenantId, BusFactorMaxContributors: 1), CancellationToken.None);

        result.Value.BusFactor1Services.Should().HaveCount(1);
        result.Value.BusFactor1Services.Should().Contain("Service A");
    }

    // ────────────────────────────────────────────────────────────────────────
    // CollaborationTrend
    // ────────────────────────────────────────────────────────────────────────

    [Fact]
    public void AY3_CollaborationTrend_Growing_WhenRatioIncreases()
    {
        var points = new[]
        {
            new GetTeamKnowledgeSharingReport.WeeklyTrendPoint(0, 0.10m),
            new GetTeamKnowledgeSharingReport.WeeklyTrendPoint(1, 0.20m),
        };
        var direction = GetTeamKnowledgeSharingReport.Handler.ComputeTrendDirection(points);
        direction.Should().Be(GetTeamKnowledgeSharingReport.CollaborationTrendDirection.Growing);
    }

    [Fact]
    public void AY3_CollaborationTrend_Declining_WhenRatioDecreases()
    {
        var points = new[]
        {
            new GetTeamKnowledgeSharingReport.WeeklyTrendPoint(0, 0.30m),
            new GetTeamKnowledgeSharingReport.WeeklyTrendPoint(1, 0.05m),
        };
        var direction = GetTeamKnowledgeSharingReport.Handler.ComputeTrendDirection(points);
        direction.Should().Be(GetTeamKnowledgeSharingReport.CollaborationTrendDirection.Declining);
    }

    [Fact]
    public void AY3_CollaborationTrend_Stable_WhenSinglePoint()
    {
        var points = new[] { new GetTeamKnowledgeSharingReport.WeeklyTrendPoint(0, 0.20m) };
        var direction = GetTeamKnowledgeSharingReport.Handler.ComputeTrendDirection(points);
        direction.Should().Be(GetTeamKnowledgeSharingReport.CollaborationTrendDirection.Stable);
    }

    // ────────────────────────────────────────────────────────────────────────
    // TopKnowledgeContributors
    // ────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task AY3_TopKnowledgeContributors_OrderedByCrossTeamContribs()
    {
        var teams = new[]
        {
            MakeTeamEntry("t1", "Low-Team", docContribs: 10, crossTeamContribs: 1),
            MakeTeamEntry("t2", "High-Team", docContribs: 10, crossTeamContribs: 8),
            MakeTeamEntry("t3", "Mid-Team", docContribs: 10, crossTeamContribs: 4),
        };
        var handler = CreateSharingHandler(BuildSharingReader(teams));
        var result = await handler.Handle(
            new GetTeamKnowledgeSharingReport.Query(TenantId, TopContributorsCount: 2), CancellationToken.None);

        result.Value.Summary.TopKnowledgeContributors.Should().HaveCount(2);
        result.Value.Summary.TopKnowledgeContributors[0].TeamId.Should().Be("t2");
    }

    // ────────────────────────────────────────────────────────────────────────
    // Validator
    // ────────────────────────────────────────────────────────────────────────

    [Fact]
    public void AY3_Validator_RejectsEmptyTenantId()
    {
        var validator = new GetTeamKnowledgeSharingReport.Validator();
        validator.Validate(new GetTeamKnowledgeSharingReport.Query("")).IsValid.Should().BeFalse();
    }

    [Fact]
    public void AY3_Validator_RejectsInvalidSiloThreshold()
    {
        var validator = new GetTeamKnowledgeSharingReport.Validator();
        validator.Validate(new GetTeamKnowledgeSharingReport.Query("t", SiloThreshold: -0.1m)).IsValid.Should().BeFalse();
    }
}
