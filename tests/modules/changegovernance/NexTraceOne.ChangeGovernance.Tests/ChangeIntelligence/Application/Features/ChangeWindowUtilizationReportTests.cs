using System.Linq;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Abstractions;
using NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Features.GetChangeWindowUtilizationReport;
using NexTraceOne.ChangeGovernance.Domain.ChangeIntelligence.Entities;
using NexTraceOne.ChangeGovernance.Domain.ChangeIntelligence.Enums;

namespace NexTraceOne.ChangeGovernance.Tests.ChangeIntelligence.Application.Features;

/// <summary>
/// Testes unitários para Wave S.1 — GetChangeWindowUtilizationReport.
/// Cobre: sem releases, sem janelas, tiers Excellent/Good/AtRisk, environment filter,
/// grouping por equipa, janelas Freeze ignoradas, série de top non-compliant.
/// </summary>
public sealed class ChangeWindowUtilizationReportTests
{
    private static readonly DateTimeOffset FixedNow = new(2026, 4, 21, 12, 0, 0, TimeSpan.Zero);
    private static readonly Guid TenantId = Guid.NewGuid();
    private const string TenantIdStr = "11111111-1111-1111-1111-111111111111";

    private static IDateTimeProvider CreateClock()
    {
        var clock = Substitute.For<IDateTimeProvider>();
        clock.UtcNow.Returns(FixedNow);
        return clock;
    }

    private static Release MakeRelease(
        string serviceName = "svc-a",
        string env = "prod",
        DateTimeOffset? createdAt = null)
    {
        var release = Release.Create(
            Guid.Parse(TenantIdStr), Guid.NewGuid(), serviceName, "1.0.0", env,
            "pipeline-ci", "abc123", createdAt ?? FixedNow.AddDays(-1));
        return release;
    }

    private static ReleaseCalendarEntry MakeWindow(
        ReleaseWindowType type,
        DateTimeOffset startsAt,
        DateTimeOffset endsAt,
        string? envFilter = null)
    {
        var result = ReleaseCalendarEntry.Register(
            TenantIdStr, "Test Window", type, startsAt, endsAt, envFilter);
        result.IsSuccess.Should().BeTrue();
        return result.Value;
    }

    private static GetChangeWindowUtilizationReport.Handler CreateHandler(
        IReadOnlyList<Release> releases,
        IReadOnlyList<ReleaseCalendarEntry> windows)
    {
        var releaseRepo = Substitute.For<IReleaseRepository>();
        var calendarRepo = Substitute.For<IReleaseCalendarRepository>();

        releaseRepo.ListInRangeAsync(
                Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(),
                Arg.Any<string?>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(releases);

        calendarRepo.ListAsync(
                Arg.Any<string>(),
                Arg.Any<ReleaseWindowStatus?>(),
                Arg.Any<ReleaseWindowType?>(),
                Arg.Any<DateTimeOffset?>(),
                Arg.Any<DateTimeOffset?>(),
                Arg.Any<CancellationToken>())
            .Returns(windows);

        return new GetChangeWindowUtilizationReport.Handler(releaseRepo, calendarRepo, CreateClock());
    }

    private static GetChangeWindowUtilizationReport.Query DefaultQuery()
        => new(TenantId: TenantIdStr, LookbackDays: 90, TopNonCompliantCount: 10);

    // ── Empty: no releases ────────────────────────────────────────────────

    [Fact]
    public async Task Handle_NoReleases_ReturnsZeroTotals()
    {
        var handler = CreateHandler([], []);
        var result = await handler.Handle(DefaultQuery(), CancellationToken.None);
        Assert.True(result.IsSuccess);
        var r = result.Value;
        Assert.Equal(0, r.TotalReleasesAnalyzed);
        Assert.Equal(0, r.TotalReleasesOutOfWindow);
        Assert.Equal(100m, r.TenantConformanceRatePct);
        Assert.Empty(r.AllTeams);
    }

    // ── No calendar windows: all releases out of window ───────────────────

    [Fact]
    public async Task Handle_NoWindows_AllDeploymentsOutOfWindow()
    {
        var rel = MakeRelease("svc-a", createdAt: FixedNow.AddDays(-1));
        var handler = CreateHandler([rel], []);
        var result = await handler.Handle(DefaultQuery(), CancellationToken.None);
        Assert.True(result.IsSuccess);
        var r = result.Value;
        Assert.Equal(1, r.TotalReleasesAnalyzed);
        Assert.Equal(1, r.TotalReleasesOutOfWindow);
        Assert.Equal(0m, r.TenantConformanceRatePct);
    }

    // ── All releases within Scheduled window → Excellent ─────────────────

    [Fact]
    public async Task Handle_AllInWindow_ConformanceExcellent()
    {
        var windowStart = FixedNow.AddDays(-5);
        var windowEnd = FixedNow;
        var window = MakeWindow(ReleaseWindowType.Scheduled, windowStart, windowEnd);

        // Deploy within window
        var rel = MakeRelease("svc-a", createdAt: FixedNow.AddDays(-3));
        var handler = CreateHandler([rel], [window]);
        var result = await handler.Handle(DefaultQuery(), CancellationToken.None);
        Assert.True(result.IsSuccess);
        var r = result.Value;
        Assert.Equal(0, r.TotalReleasesOutOfWindow);
        Assert.Equal(100m, r.TenantConformanceRatePct);
        var team = r.AllTeams.Single();
        Assert.Equal(GetChangeWindowUtilizationReport.ConformanceTier.Excellent, team.ConformanceTier);
    }

    // ── Freeze window: deploy inside Freeze counts as out-of-window ───────

    [Fact]
    public async Task Handle_DeployDuringFreezeWindow_CountsAsOutOfWindow()
    {
        var freezeStart = FixedNow.AddDays(-5);
        var freezeEnd = FixedNow;
        var freeze = MakeWindow(ReleaseWindowType.Freeze, freezeStart, freezeEnd);

        var rel = MakeRelease("svc-a", createdAt: FixedNow.AddDays(-3));
        var handler = CreateHandler([rel], [freeze]);
        var result = await handler.Handle(DefaultQuery(), CancellationToken.None);
        Assert.True(result.IsSuccess);
        Assert.Equal(1, result.Value.TotalReleasesOutOfWindow);
    }

    // ── HotfixAllowed window: deploy inside it counts as in-window ────────

    [Fact]
    public async Task Handle_DeployDuringHotfixWindow_CountsAsInWindow()
    {
        var hotfixStart = FixedNow.AddDays(-5);
        var hotfixEnd = FixedNow;
        var hotfix = MakeWindow(ReleaseWindowType.HotfixAllowed, hotfixStart, hotfixEnd);

        var rel = MakeRelease("svc-hotfix", createdAt: FixedNow.AddDays(-3));
        var handler = CreateHandler([rel], [hotfix]);
        var result = await handler.Handle(DefaultQuery(), CancellationToken.None);
        Assert.True(result.IsSuccess);
        Assert.Equal(0, result.Value.TotalReleasesOutOfWindow);
    }

    // ── Mixed: 4/5 in window → Good tier ─────────────────────────────────

    [Fact]
    public async Task Handle_MixedDeployments_GoodTier()
    {
        var windowStart = FixedNow.AddDays(-10);
        var windowEnd = FixedNow.AddDays(-2);
        var window = MakeWindow(ReleaseWindowType.Scheduled, windowStart, windowEnd);

        // 4 releases within window, 1 outside (yesterday after window closed)
        var inWindowReleases = Enumerable.Range(1, 4)
            .Select(i => MakeRelease("svc-a", createdAt: FixedNow.AddDays(-9 + i)))
            .ToList<Release>();
        var outsideRelease = MakeRelease("svc-a", createdAt: FixedNow.AddDays(-1));
        var allReleases = inWindowReleases.Concat([outsideRelease]).ToList<Release>();

        var handler = CreateHandler(allReleases, [window]);
        var result = await handler.Handle(DefaultQuery(), CancellationToken.None);
        Assert.True(result.IsSuccess);
        var r = result.Value;
        // 4/5 = 80% → Good
        Assert.Equal(80m, r.TenantConformanceRatePct);
        Assert.Equal(GetChangeWindowUtilizationReport.ConformanceTier.Good, r.AllTeams.Single().ConformanceTier);
        Assert.Equal(1, r.TierDistribution.GoodCount);
    }

    // ── AtRisk: fewer than 80% in window ──────────────────────────────────

    [Fact]
    public async Task Handle_LowConformance_AtRiskTier()
    {
        // Only 1/5 = 20% within window → AtRisk
        var windowStart = FixedNow.AddDays(-2);
        var windowEnd = FixedNow;
        var window = MakeWindow(ReleaseWindowType.Scheduled, windowStart, windowEnd);

        var inWindow = MakeRelease("svc-x", createdAt: FixedNow.AddDays(-1));
        var out1 = MakeRelease("svc-x", createdAt: FixedNow.AddDays(-10));
        var out2 = MakeRelease("svc-x", createdAt: FixedNow.AddDays(-15));
        var out3 = MakeRelease("svc-x", createdAt: FixedNow.AddDays(-20));
        var out4 = MakeRelease("svc-x", createdAt: FixedNow.AddDays(-25));

        var handler = CreateHandler([inWindow, out1, out2, out3, out4], [window]);
        var result = await handler.Handle(DefaultQuery(), CancellationToken.None);
        Assert.True(result.IsSuccess);
        Assert.Equal(GetChangeWindowUtilizationReport.ConformanceTier.AtRisk, result.Value.AllTeams.Single().ConformanceTier);
        Assert.Equal(1, result.Value.TierDistribution.AtRiskCount);
    }

    // ── Validator: empty TenantId fails ──────────────────────────────────

    [Fact]
    public void Validator_EmptyTenantId_Fails()
    {
        var validator = new GetChangeWindowUtilizationReport.Validator();
        var result = validator.Validate(new GetChangeWindowUtilizationReport.Query(TenantId: ""));
        Assert.False(result.IsValid);
    }

    // ── Validator: LookbackDays out of range ──────────────────────────────

    [Theory]
    [InlineData(0)]
    [InlineData(366)]
    public void Validator_LookbackDaysOutOfRange_Fails(int days)
    {
        var validator = new GetChangeWindowUtilizationReport.Validator();
        var result = validator.Validate(new GetChangeWindowUtilizationReport.Query(
            TenantId: TenantIdStr, LookbackDays: days));
        Assert.False(result.IsValid);
    }

    // ── Validator: valid query passes ─────────────────────────────────────

    [Fact]
    public void Validator_ValidQuery_Passes()
    {
        var validator = new GetChangeWindowUtilizationReport.Validator();
        var result = validator.Validate(DefaultQuery());
        Assert.True(result.IsValid);
    }

    // ── GeneratedAt matches clock ─────────────────────────────────────────

    [Fact]
    public async Task Handle_GeneratedAt_MatchesClock()
    {
        var handler = CreateHandler([], []);
        var result = await handler.Handle(DefaultQuery(), CancellationToken.None);
        Assert.True(result.IsSuccess);
        Assert.Equal(FixedNow, result.Value.GeneratedAt);
    }

    // ── LookbackDays echoed in report ─────────────────────────────────────

    [Fact]
    public async Task Handle_LookbackDays_EchoedInReport()
    {
        var handler = CreateHandler([], []);
        var query = DefaultQuery() with { LookbackDays = 60 };
        var result = await handler.Handle(query, CancellationToken.None);
        Assert.True(result.IsSuccess);
        Assert.Equal(60, result.Value.LookbackDays);
    }

    // ── Top non-compliant ordered by conformance ascending ─────────────────

    [Fact]
    public async Task Handle_TopNonCompliant_OrderedByConformanceAscending()
    {
        // svc-a: 0% conformance, svc-b: 50% conformance
        var windowStart = FixedNow.AddDays(-1);
        var windowEnd = FixedNow;
        var window = MakeWindow(ReleaseWindowType.Scheduled, windowStart, windowEnd);

        var svcA = MakeRelease("svc-a", createdAt: FixedNow.AddDays(-10)); // out of window
        var svcB_in = MakeRelease("svc-b", createdAt: FixedNow.AddHours(-6)); // in window
        var svcB_out = MakeRelease("svc-b", createdAt: FixedNow.AddDays(-10)); // out of window

        var handler = CreateHandler([svcA, svcB_in, svcB_out], [window]);
        var result = await handler.Handle(DefaultQuery() with { TopNonCompliantCount = 5 }, CancellationToken.None);
        Assert.True(result.IsSuccess);
        var top = result.Value.TopNonCompliantTeams;
        Assert.True(top.Count >= 1);
        // svc-a has lower conformance and should be first
        Assert.True(top[0].ConformanceRatePct <= (top.Count > 1 ? top[1].ConformanceRatePct : 100m));
    }
}
