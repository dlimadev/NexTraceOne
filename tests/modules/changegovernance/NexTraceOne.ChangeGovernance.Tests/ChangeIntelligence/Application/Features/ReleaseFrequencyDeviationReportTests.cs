using System.Linq;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Abstractions;
using NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Features.GetReleaseFrequencyDeviationReport;
using NexTraceOne.ChangeGovernance.Domain.ChangeIntelligence.Entities;

namespace NexTraceOne.ChangeGovernance.Tests.ChangeIntelligence.Application.Features;

/// <summary>
/// Testes unitários para Wave V.3 — GetReleaseFrequencyDeviationReport.
/// Cobre: sem releases, serviço novo (New), stalled, accelerating, stable, decelerating,
/// RiskFlag, multi-serviço, distribuição, validator.
/// </summary>
public sealed class ReleaseFrequencyDeviationReportTests
{
    private static readonly DateTimeOffset FixedNow = new(2026, 4, 21, 12, 0, 0, TimeSpan.Zero);
    private static readonly Guid TenantId = Guid.Parse("44444444-4444-4444-4444-444444444444");
    private static readonly string TenantIdStr = TenantId.ToString();

    private static IDateTimeProvider CreateClock()
    {
        var clock = Substitute.For<IDateTimeProvider>();
        clock.UtcNow.Returns(FixedNow);
        return clock;
    }

    private static Release MakeRelease(
        string serviceName,
        DateTimeOffset createdAt,
        DeploymentStatus status = DeploymentStatus.Succeeded,
        string environment = "production")
    {
        var r = Release.Create(TenantId, Guid.NewGuid(), serviceName, "1.0.0",
            environment, "jenkins", "abc", createdAt);

        if (status is DeploymentStatus.Running or DeploymentStatus.Succeeded
            or DeploymentStatus.Failed or DeploymentStatus.RolledBack)
            r.UpdateStatus(DeploymentStatus.Running);

        if (status == DeploymentStatus.Succeeded)
            r.UpdateStatus(DeploymentStatus.Succeeded);
        else if (status == DeploymentStatus.Failed)
            r.UpdateStatus(DeploymentStatus.Failed);
        else if (status == DeploymentStatus.RolledBack)
        {
            r.UpdateStatus(DeploymentStatus.Succeeded);
            r.UpdateStatus(DeploymentStatus.RolledBack);
        }
        return r;
    }

    private static GetReleaseFrequencyDeviationReport.Handler CreateHandler(
        IReadOnlyList<Release> recentReleases,
        IReadOnlyList<Release> historicalReleases)
    {
        var repo = Substitute.For<IReleaseRepository>();
        // First call = recent window, second call = historical window
        repo.ListInRangeAsync(
                Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(),
                Arg.Any<string?>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(recentReleases, historicalReleases);

        return new GetReleaseFrequencyDeviationReport.Handler(repo, CreateClock());
    }

    private static GetReleaseFrequencyDeviationReport.Query DefaultQuery()
        => new(TenantId: TenantIdStr, RecentDays: 30, HistoricalDays: 90);

    // ── Empty: no releases in either window ───────────────────────────────

    [Fact]
    public async Task Handle_NoReleasesAtAll_ReturnsEmptyReport()
    {
        var handler = CreateHandler([], []);
        var result = await handler.Handle(DefaultQuery(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var r = result.Value;
        Assert.Equal(0, r.TotalServicesAnalyzed);
        Assert.Empty(r.AllServices);
    }

    // ── New: releases only in recent window (no historical) ───────────────

    [Fact]
    public async Task Handle_ReleasesOnlyRecent_ClassifiesAsNew()
    {
        var recent = new[]
        {
            MakeRelease("svc-new", FixedNow.AddDays(-5)),
            MakeRelease("svc-new", FixedNow.AddDays(-10))
        };
        var handler = CreateHandler(recent, []);
        var result = await handler.Handle(DefaultQuery(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var entry = result.Value.AllServices.Single(e => e.ServiceName == "svc-new");
        Assert.Equal(GetReleaseFrequencyDeviationReport.FrequencyDeviation.New, entry.Deviation);
        Assert.Equal(1, result.Value.DeviationDistribution.NewCount);
    }

    // ── Stalled: releases only in historical window (none recent) ─────────

    [Fact]
    public async Task Handle_ReleasesOnlyHistorical_ClassifiesAsStalled()
    {
        var historical = new[]
        {
            MakeRelease("svc-stalled", FixedNow.AddDays(-50)),
            MakeRelease("svc-stalled", FixedNow.AddDays(-70))
        };
        var handler = CreateHandler([], historical);
        var result = await handler.Handle(DefaultQuery(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var entry = result.Value.AllServices.Single(e => e.ServiceName == "svc-stalled");
        Assert.Equal(GetReleaseFrequencyDeviationReport.FrequencyDeviation.Stalled, entry.Deviation);
        Assert.Equal(1, result.Value.DeviationDistribution.StalledCount);
    }

    // ── Accelerating: >+50% deviation ────────────────────────────────────

    [Fact]
    public async Task Handle_DeployRateMuchHigherRecent_ClassifiesAsAccelerating()
    {
        // Historical: 3 in 90 days → 0.033/day
        // Recent: 6 in 30 days → 0.2/day → deviation = (0.2-0.033)/0.033 ≈ 506%
        var recent = Enumerable.Range(0, 6)
            .Select(i => MakeRelease("svc-accel", FixedNow.AddDays(-i - 1)))
            .ToList();
        var historical = Enumerable.Range(0, 3)
            .Select(i => MakeRelease("svc-accel", FixedNow.AddDays(-40 - i)))
            .ToList();

        var handler = CreateHandler(recent, historical);
        var result = await handler.Handle(DefaultQuery(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var entry = result.Value.AllServices.Single(e => e.ServiceName == "svc-accel");
        Assert.Equal(GetReleaseFrequencyDeviationReport.FrequencyDeviation.Accelerating, entry.Deviation);
        Assert.True(entry.DeviationPct > 50m);
    }

    // ── Stable: deviation within -50% to +50% ────────────────────────────

    [Fact]
    public async Task Handle_SimilarDeployRate_ClassifiesAsStable()
    {
        // Same rate: 3 in 30 days vs 9 in 90 days → 0.1/day each → 0% deviation
        var recent = Enumerable.Range(0, 3)
            .Select(i => MakeRelease("svc-stable", FixedNow.AddDays(-i * 7 - 1)))
            .ToList();
        var historical = Enumerable.Range(0, 9)
            .Select(i => MakeRelease("svc-stable", FixedNow.AddDays(-40 - i * 8)))
            .ToList();

        var handler = CreateHandler(recent, historical);
        var result = await handler.Handle(DefaultQuery(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var entry = result.Value.AllServices.Single(e => e.ServiceName == "svc-stable");
        Assert.Equal(GetReleaseFrequencyDeviationReport.FrequencyDeviation.Stable, entry.Deviation);
    }

    // ── Decelerating: < -50% deviation ───────────────────────────────────

    [Fact]
    public async Task Handle_MuchLowerRecentRate_ClassifiesAsDecelerating()
    {
        // Historical: 9 in 90 days → 0.1/day
        // Recent: 1 in 30 days → 0.033/day → deviation ≈ -67%
        var recent = new[] { MakeRelease("svc-decel", FixedNow.AddDays(-25)) };
        var historical = Enumerable.Range(0, 9)
            .Select(i => MakeRelease("svc-decel", FixedNow.AddDays(-40 - i * 5)))
            .ToList();

        var handler = CreateHandler(recent, historical);
        var result = await handler.Handle(DefaultQuery(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var entry = result.Value.AllServices.Single(e => e.ServiceName == "svc-decel");
        Assert.Equal(GetReleaseFrequencyDeviationReport.FrequencyDeviation.Decelerating, entry.Deviation);
    }

    // ── RiskFlag: Accelerating with low success rate ──────────────────────

    [Fact]
    public async Task Handle_AcceleratingWithLowSuccessRate_SetsRiskFlag()
    {
        // High deploy rate + failures
        var recent = new List<Release>
        {
            MakeRelease("svc-risky", FixedNow.AddDays(-1), DeploymentStatus.Succeeded),
            MakeRelease("svc-risky", FixedNow.AddDays(-2), DeploymentStatus.Failed),
            MakeRelease("svc-risky", FixedNow.AddDays(-3), DeploymentStatus.Failed),
            MakeRelease("svc-risky", FixedNow.AddDays(-4), DeploymentStatus.Failed),
            MakeRelease("svc-risky", FixedNow.AddDays(-5), DeploymentStatus.Failed),
            MakeRelease("svc-risky", FixedNow.AddDays(-6), DeploymentStatus.Failed),
        };
        var historical = new[] { MakeRelease("svc-risky", FixedNow.AddDays(-50)) };

        var handler = CreateHandler(recent, historical);
        var result = await handler.Handle(DefaultQuery(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var entry = result.Value.AllServices.Single(e => e.ServiceName == "svc-risky");
        Assert.Equal(GetReleaseFrequencyDeviationReport.FrequencyDeviation.Accelerating, entry.Deviation);
        Assert.True(entry.RecentSuccessRatePct < 80m);
        Assert.True(entry.RiskFlag);
    }

    // ── RiskFlag: Stalled always sets RiskFlag ───────────────────────────

    [Fact]
    public async Task Handle_StalledService_SetsRiskFlag()
    {
        var historical = new[] { MakeRelease("svc-stalled-risk", FixedNow.AddDays(-60)) };
        var handler = CreateHandler([], historical);
        var result = await handler.Handle(DefaultQuery(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var entry = result.Value.AllServices.Single(e => e.ServiceName == "svc-stalled-risk");
        Assert.True(entry.RiskFlag);
    }

    // ── Multi-service distribution ────────────────────────────────────────

    [Fact]
    public async Task Handle_MultipleServices_CorrectDistribution()
    {
        // svc-a: New (only recent)
        // svc-b: Stalled (only historical)
        var recentReleases = new[] { MakeRelease("svc-a", FixedNow.AddDays(-5)) };
        var historicalReleases = new[] { MakeRelease("svc-b", FixedNow.AddDays(-50)) };

        var handler = CreateHandler(recentReleases, historicalReleases);
        var result = await handler.Handle(DefaultQuery(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var r = result.Value;
        Assert.Equal(2, r.TotalServicesAnalyzed);
        Assert.Equal(1, r.DeviationDistribution.NewCount);
        Assert.Equal(1, r.DeviationDistribution.StalledCount);
    }

    // ── Validator ─────────────────────────────────────────────────────────

    [Fact]
    public async Task Validator_EmptyTenantId_Fails()
    {
        var v = new GetReleaseFrequencyDeviationReport.Validator();
        var r = await v.ValidateAsync(new GetReleaseFrequencyDeviationReport.Query(TenantId: ""));
        Assert.False(r.IsValid);
    }

    [Fact]
    public async Task Validator_ValidQuery_Passes()
    {
        var v = new GetReleaseFrequencyDeviationReport.Validator();
        var r = await v.ValidateAsync(DefaultQuery());
        Assert.True(r.IsValid);
    }

    [Fact]
    public async Task Validator_RecentDaysGreaterThanHistorical_Fails()
    {
        var v = new GetReleaseFrequencyDeviationReport.Validator();
        // Historical must be > Recent
        var r = await v.ValidateAsync(
            new GetReleaseFrequencyDeviationReport.Query(TenantId: TenantIdStr, RecentDays: 60, HistoricalDays: 30));
        Assert.False(r.IsValid);
    }
}
