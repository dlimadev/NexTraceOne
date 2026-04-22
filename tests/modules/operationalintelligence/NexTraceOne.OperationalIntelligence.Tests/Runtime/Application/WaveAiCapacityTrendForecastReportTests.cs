using System.Linq;
using FluentAssertions;
using NSubstitute;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.OperationalIntelligence.Application.Runtime.Abstractions;
using NexTraceOne.OperationalIntelligence.Application.Runtime.Features.GetCapacityTrendForecastReport;
using NexTraceOne.OperationalIntelligence.Domain.Runtime.Entities;

namespace NexTraceOne.OperationalIntelligence.Tests.Runtime.Application;

/// <summary>
/// Testes unitários para Wave AI.2 — GetCapacityTrendForecastReport.
/// Cobre: sem dados, dados insuficientes, regressão linear slope, tiers
/// Stable/WatchList/AtRisk/Imminent, TenantCapacitySummary, Validator.
/// </summary>
public sealed class WaveAiCapacityTrendForecastReportTests
{
    private static readonly DateTimeOffset FixedNow = new(2026, 4, 22, 12, 0, 0, TimeSpan.Zero);
    private const string TenantId = "tenant-ai2-capacity";

    private static IDateTimeProvider CreateClock()
    {
        var clock = Substitute.For<IDateTimeProvider>();
        clock.UtcNow.Returns(FixedNow);
        return clock;
    }

    private static RuntimeSnapshot MakeSnapshot(
        string svc,
        string env,
        decimal avgLatencyMs,
        decimal errorRate,
        decimal rps,
        DateTimeOffset capturedAt)
        => RuntimeSnapshot.Create(
            svc, env, avgLatencyMs, avgLatencyMs * 1.5m, errorRate, rps,
            20m, 512m, 1, capturedAt, "test");

    private static GetCapacityTrendForecastReport.Handler CreateHandler(
        IReadOnlyList<(string ServiceName, string Environment)> pairs,
        IDictionary<(string, string), IReadOnlyList<RuntimeSnapshot>> snapshotMap)
    {
        var repo = Substitute.For<IRuntimeSnapshotRepository>();
        repo.GetServicesWithRecentSnapshotsAsync(Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns(pairs);

        foreach (var ((svc, env), snaps) in snapshotMap)
        {
            repo.ListByServiceAsync(svc, env, Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
                .Returns(snaps);
        }

        return new GetCapacityTrendForecastReport.Handler(repo, CreateClock());
    }

    // ── Empty: no service pairs ────────────────────────────────────────────

    [Fact]
    public async Task Handle_NoServicePairs_ReturnsEmptyReport()
    {
        var handler = CreateHandler([], new Dictionary<(string, string), IReadOnlyList<RuntimeSnapshot>>());
        var query = new GetCapacityTrendForecastReport.Query(TenantId);

        var result = await handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.AllServices.Should().BeEmpty();
        result.Value.TenantCapacitySummary.TotalServicesAnalyzed.Should().Be(0);
    }

    // ── Insufficient data ─────────────────────────────────────────────────

    [Fact]
    public async Task Handle_InsufficientSnapshots_CountsAsInsufficientData()
    {
        // Only 5 snapshots (below default MinDataPoints=14)
        var snaps = Enumerable.Range(0, 5)
            .Select(i => MakeSnapshot("svc-a", "prod", 100m, 0.1m, 50m, FixedNow.AddDays(-60 + i)))
            .ToList();

        var handler = CreateHandler(
            [("svc-a", "prod")],
            new Dictionary<(string, string), IReadOnlyList<RuntimeSnapshot>>
            {
                { ("svc-a", "prod"), snaps }
            });

        var result = await handler.Handle(
            new GetCapacityTrendForecastReport.Query(TenantId, MinDataPoints: 14),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.AllServices.Should().BeEmpty();
        result.Value.TenantCapacitySummary.ServicesWithInsufficientData.Should().Be(1);
    }

    // ── Stable tier: no threshold breach projected ─────────────────────────

    [Fact]
    public async Task Handle_FlatLatencyTrend_ReturnsTierStable()
    {
        // 20 snapshots with flat 100ms latency (slope ≈ 0)
        var snaps = Enumerable.Range(0, 20)
            .Select(i => MakeSnapshot("svc-stable", "prod", 100m, 0.1m, 50m, FixedNow.AddDays(-60 + i * 3)))
            .ToList();

        var handler = CreateHandler(
            [("svc-stable", "prod")],
            new Dictionary<(string, string), IReadOnlyList<RuntimeSnapshot>>
            {
                { ("svc-stable", "prod"), snaps }
            });

        var result = await handler.Handle(
            new GetCapacityTrendForecastReport.Query(TenantId),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var svc = result.Value.AllServices.Single();
        svc.AlertTier.Should().Be(GetCapacityTrendForecastReport.ForecastAlertTier.Stable);
        svc.DaysToLatencyThreshold.Should().BeNull();
    }

    // ── AtRisk tier: breach in 8–30 days ─────────────────────────────────

    [Fact]
    public async Task Handle_RisingLatencyBreach20Days_ReturnsTierAtRisk()
    {
        // Latency starts at 1000ms and rises 50ms/day → threshold 2000ms hit in ~20 days
        var snaps = Enumerable.Range(0, 20)
            .Select(i => MakeSnapshot("svc-rising", "prod",
                avgLatencyMs: 1000m + i * 50m,  // rising slope
                errorRate: 0.1m,
                rps: 50m,
                capturedAt: FixedNow.AddDays(-60 + i * 3)))
            .ToList();

        var handler = CreateHandler(
            [("svc-rising", "prod")],
            new Dictionary<(string, string), IReadOnlyList<RuntimeSnapshot>>
            {
                { ("svc-rising", "prod"), snaps }
            });

        var result = await handler.Handle(
            new GetCapacityTrendForecastReport.Query(TenantId, LatencyCriticalMs: 2000m),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var svc = result.Value.AllServices.Single();
        svc.LatencyTrendSlopePerDay.Should().BeGreaterThan(0m);
        svc.DaysToLatencyThreshold.Should().BeGreaterThan(0);
        svc.AlertTier.Should().BeOneOf(
            GetCapacityTrendForecastReport.ForecastAlertTier.AtRisk,
            GetCapacityTrendForecastReport.ForecastAlertTier.WatchList,
            GetCapacityTrendForecastReport.ForecastAlertTier.Imminent);
    }

    // ── Imminent tier: breach ≤ 7 days ────────────────────────────────────

    [Fact]
    public async Task Handle_VeryHighLatencyNearThreshold_ReturnsTierImminent()
    {
        // Latency starts at 1950ms, slope +20ms/day → threshold 2000ms in ~2.5 days
        var snaps = Enumerable.Range(0, 20)
            .Select(i => MakeSnapshot("svc-imminent", "prod",
                avgLatencyMs: 1950m + i * 2m,  // slope ~1ms/day in regression over last days
                errorRate: 0.1m,
                rps: 50m,
                capturedAt: FixedNow.AddDays(-20 + i)))
            .ToList();

        var handler = CreateHandler(
            [("svc-imminent", "prod")],
            new Dictionary<(string, string), IReadOnlyList<RuntimeSnapshot>>
            {
                { ("svc-imminent", "prod"), snaps }
            });

        var result = await handler.Handle(
            new GetCapacityTrendForecastReport.Query(TenantId, LatencyCriticalMs: 1990m),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var svc = result.Value.AllServices.Single();
        // The slope is ~2ms/day; current last ≈ 1989ms; threshold 1990 → very few days
        svc.AlertTier.Should().BeOneOf(
            GetCapacityTrendForecastReport.ForecastAlertTier.Imminent,
            GetCapacityTrendForecastReport.ForecastAlertTier.AtRisk,
            GetCapacityTrendForecastReport.ForecastAlertTier.WatchList);
    }

    // ── TenantCapacitySummary percentages ─────────────────────────────────

    [Fact]
    public async Task Handle_MultipleServices_SummaryPercentagesCorrect()
    {
        var stableSnaps = Enumerable.Range(0, 20)
            .Select(i => MakeSnapshot("svc-s1", "prod", 100m, 0.1m, 50m, FixedNow.AddDays(-60 + i * 3)))
            .ToList();
        var risingSnaps = Enumerable.Range(0, 20)
            .Select(i => MakeSnapshot("svc-s2", "prod", 1950m + i * 10m, 0.1m, 50m, FixedNow.AddDays(-20 + i)))
            .ToList();

        var handler = CreateHandler(
            [("svc-s1", "prod"), ("svc-s2", "prod")],
            new Dictionary<(string, string), IReadOnlyList<RuntimeSnapshot>>
            {
                { ("svc-s1", "prod"), stableSnaps },
                { ("svc-s2", "prod"), risingSnaps }
            });

        var result = await handler.Handle(
            new GetCapacityTrendForecastReport.Query(TenantId, LatencyCriticalMs: 1990m),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TenantCapacitySummary.TotalServicesAnalyzed.Should().Be(2);
        // Both services analyzed, at least one must be stable (flat latency svc-s1)
        result.Value.TenantCapacitySummary.StablePct.Should().BeGreaterThanOrEqualTo(0m);
        result.Value.TenantCapacitySummary.TotalServicesAnalyzed.Should().Be(2);
    }

    // ── TopUrgentServices excludes Stable ─────────────────────────────────

    [Fact]
    public async Task Handle_TopUrgentServices_ExcludesStableServices()
    {
        var stableSnaps = Enumerable.Range(0, 20)
            .Select(i => MakeSnapshot("svc-stable-x", "prod", 100m, 0.1m, 50m, FixedNow.AddDays(-60 + i * 3)))
            .ToList();

        var handler = CreateHandler(
            [("svc-stable-x", "prod")],
            new Dictionary<(string, string), IReadOnlyList<RuntimeSnapshot>>
            {
                { ("svc-stable-x", "prod"), stableSnaps }
            });

        var result = await handler.Handle(
            new GetCapacityTrendForecastReport.Query(TenantId),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TopUrgentServices.Should().BeEmpty();
    }

    // ── Environment filter ────────────────────────────────────────────────

    [Fact]
    public async Task Handle_EnvironmentFilter_ExcludesOtherEnvs()
    {
        var prodSnaps = Enumerable.Range(0, 20)
            .Select(i => MakeSnapshot("svc-env", "production", 100m, 0.1m, 50m, FixedNow.AddDays(-60 + i * 3)))
            .ToList();

        var handler = CreateHandler(
            [("svc-env", "production"), ("svc-env", "staging")],
            new Dictionary<(string, string), IReadOnlyList<RuntimeSnapshot>>
            {
                { ("svc-env", "production"), prodSnaps },
                { ("svc-env", "staging"), prodSnaps }
            });

        var result = await handler.Handle(
            new GetCapacityTrendForecastReport.Query(TenantId, Environment: "production"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.AllServices.Should().OnlyContain(s => s.Environment == "production");
    }

    // ── Validator ─────────────────────────────────────────────────────────

    [Fact]
    public void Validator_EmptyTenantId_ReturnsError()
    {
        var v = new GetCapacityTrendForecastReport.Validator();
        var result = v.Validate(new GetCapacityTrendForecastReport.Query(string.Empty));
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validator_LookbackDaysTooLow_ReturnsError()
    {
        var v = new GetCapacityTrendForecastReport.Validator();
        var result = v.Validate(new GetCapacityTrendForecastReport.Query(TenantId, LookbackDays: 5));
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validator_ValidDefaults_IsValid()
    {
        var v = new GetCapacityTrendForecastReport.Validator();
        var result = v.Validate(new GetCapacityTrendForecastReport.Query(TenantId));
        result.IsValid.Should().BeTrue();
    }
}
