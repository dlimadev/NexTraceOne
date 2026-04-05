using System.Linq;
using FluentAssertions;
using NSubstitute;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.OperationalIntelligence.Application.Runtime.Abstractions;
using NexTraceOne.OperationalIntelligence.Application.Runtime.Features.CorrelateServiceMetrics;
using NexTraceOne.OperationalIntelligence.Application.Runtime.Features.GetTopologyAwareAlerts;
using NexTraceOne.OperationalIntelligence.Domain.Runtime.Entities;
using Xunit;

namespace NexTraceOne.OperationalIntelligence.Tests.Runtime.Application;

/// <summary>
/// Testes unitários para Phase 5.4 — Metric Correlation + Topology-Aware Alerting.
/// </summary>
public sealed class Phase54ObservabilityCorrelationTests
{
    private static readonly DateTimeOffset FixedNow = new(2026, 4, 5, 15, 0, 0, TimeSpan.Zero);

    private readonly IRuntimeSnapshotRepository _snapshotRepo = Substitute.For<IRuntimeSnapshotRepository>();
    private readonly IDateTimeProvider _clock = Substitute.For<IDateTimeProvider>();

    public Phase54ObservabilityCorrelationTests()
    {
        _clock.UtcNow.Returns(FixedNow);
    }

    private static RuntimeSnapshot MakeSnapshot(string service, string env, decimal latency, decimal errorRate, DateTimeOffset capturedAt)
        => RuntimeSnapshot.Create(service, env, latency, latency * 2, errorRate, 100m, 20m, 256m, 1, capturedAt, "test");

    // ── CorrelateServiceMetrics ───────────────────────────────────────────

    [Fact]
    public async Task CorrelateServiceMetrics_WithSimilarDegradation_ShouldDetectCorrelation()
    {
        var windowStart = FixedNow - TimeSpan.FromHours(1);
        var windowEnd = FixedNow;

        // Both services have similar latency (~200ms) → high correlation
        var snapshotsA = new List<RuntimeSnapshot>
        {
            MakeSnapshot("svc-a", "production", 200m, 0.01m, windowStart + TimeSpan.FromMinutes(10)),
            MakeSnapshot("svc-a", "production", 210m, 0.02m, windowStart + TimeSpan.FromMinutes(30))
        };
        var snapshotsB = new List<RuntimeSnapshot>
        {
            MakeSnapshot("svc-b", "production", 205m, 0.01m, windowStart + TimeSpan.FromMinutes(10)),
            MakeSnapshot("svc-b", "production", 215m, 0.02m, windowStart + TimeSpan.FromMinutes(30))
        };

        _snapshotRepo.ListByServiceAsync("svc-a", "production", 1, 200, Arg.Any<CancellationToken>())
            .Returns(snapshotsA);
        _snapshotRepo.ListByServiceAsync("svc-b", "production", 1, 200, Arg.Any<CancellationToken>())
            .Returns(snapshotsB);

        var handler = new CorrelateServiceMetrics.Handler(_snapshotRepo, _clock);

        var result = await handler.Handle(new CorrelateServiceMetrics.Query(
            ServiceIds: new[] { "svc-a", "svc-b" },
            Environment: "production",
            WindowStart: windowStart,
            WindowEnd: windowEnd,
            CorrelationThresholdPercent: 10m), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Correlations.Should().HaveCount(1);
        result.Value.Correlations[0].ServiceIdA.Should().Be("svc-a");
        result.Value.Correlations[0].ServiceIdB.Should().Be("svc-b");
        result.Value.Correlations[0].CorrelationStrengthPercent.Should().BeGreaterThan(0m);
    }

    [Fact]
    public async Task CorrelateServiceMetrics_WithDivergentLatency_ShouldNotDetectCorrelation()
    {
        var windowStart = FixedNow - TimeSpan.FromHours(1);
        var windowEnd = FixedNow;

        // svc-a: 50ms, svc-b: 2000ms → very different, no correlation
        var snapshotsA = new List<RuntimeSnapshot>
        {
            MakeSnapshot("svc-a", "production", 50m, 0.01m, windowStart + TimeSpan.FromMinutes(10))
        };
        var snapshotsB = new List<RuntimeSnapshot>
        {
            MakeSnapshot("svc-b", "production", 2000m, 0.01m, windowStart + TimeSpan.FromMinutes(10))
        };

        _snapshotRepo.ListByServiceAsync("svc-a", "production", 1, 200, Arg.Any<CancellationToken>())
            .Returns(snapshotsA);
        _snapshotRepo.ListByServiceAsync("svc-b", "production", 1, 200, Arg.Any<CancellationToken>())
            .Returns(snapshotsB);

        var handler = new CorrelateServiceMetrics.Handler(_snapshotRepo, _clock);

        var result = await handler.Handle(new CorrelateServiceMetrics.Query(
            new[] { "svc-a", "svc-b" }, "production", windowStart, windowEnd, 10m),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Correlations.Should().BeEmpty();
    }

    [Fact]
    public async Task CorrelateServiceMetrics_WithNoSnapshotsInWindow_ShouldReturnNoCorrelations()
    {
        // Snapshots exist but OUTSIDE the window
        var outsideWindow = FixedNow - TimeSpan.FromDays(2);
        var snapshotsA = new List<RuntimeSnapshot>
        {
            MakeSnapshot("svc-a", "production", 200m, 0.01m, outsideWindow)
        };

        _snapshotRepo.ListByServiceAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(snapshotsA);

        var handler = new CorrelateServiceMetrics.Handler(_snapshotRepo, _clock);

        var windowStart = FixedNow - TimeSpan.FromHours(1);
        var result = await handler.Handle(new CorrelateServiceMetrics.Query(
            new[] { "svc-a", "svc-b" }, "production", windowStart, FixedNow, 10m),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Correlations.Should().BeEmpty();
    }

    // ── GetTopologyAwareAlerts ────────────────────────────────────────────

    [Fact]
    public async Task GetTopologyAwareAlerts_WhenServiceHasHighLatency_ShouldGenerateAlerts()
    {
        var recentSnapshot = MakeSnapshot("api-gateway", "production",
            latency: 800m,  // exceeds 500ms threshold
            errorRate: 0.01m,
            capturedAt: FixedNow - TimeSpan.FromMinutes(10));

        _snapshotRepo.ListByServiceAsync("api-gateway", "production", 1, 60, Arg.Any<CancellationToken>())
            .Returns(new List<RuntimeSnapshot> { recentSnapshot });

        var handler = new GetTopologyAwareAlerts.Handler(_snapshotRepo, _clock);

        var result = await handler.Handle(new GetTopologyAwareAlerts.Query(
            ServiceId: "api-gateway",
            Environment: "production",
            DependentServiceIds: new[] { "checkout-service", "payment-service" },
            ErrorRateAlertThreshold: 5m,
            LatencyAlertThresholdMs: 500m), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Alerts.Should().NotBeEmpty();
        result.Value.Alerts.Any(a => a.AlertType == "HighLatency").Should().BeTrue();
        result.Value.Alerts.Any(a => a.AlertType == "PropagationRisk").Should().BeTrue();
        result.Value.DependentServiceCount.Should().Be(2);
    }

    [Fact]
    public async Task GetTopologyAwareAlerts_WhenServiceHasHighErrorRate_ShouldGenerateCriticalAlert()
    {
        var recentSnapshot = MakeSnapshot("payment-svc", "production",
            latency: 100m,
            errorRate: 0.15m, // 0.15 → 15% — well above 5% threshold
            capturedAt: FixedNow - TimeSpan.FromMinutes(5));

        _snapshotRepo.ListByServiceAsync("payment-svc", "production", 1, 60, Arg.Any<CancellationToken>())
            .Returns(new List<RuntimeSnapshot> { recentSnapshot });

        var handler = new GetTopologyAwareAlerts.Handler(_snapshotRepo, _clock);

        var result = await handler.Handle(new GetTopologyAwareAlerts.Query(
            "payment-svc", "production",
            new[] { "order-service" }, 5m, 500m), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Alerts.Any(a => a.AlertType == "HighErrorRate" && a.Severity == "Critical").Should().BeTrue();
    }

    [Fact]
    public async Task GetTopologyAwareAlerts_WhenServiceIsHealthy_ShouldReturnNoAlerts()
    {
        var healthy = MakeSnapshot("healthy-svc", "production",
            latency: 50m, errorRate: 0.001m,
            capturedAt: FixedNow - TimeSpan.FromMinutes(5));

        _snapshotRepo.ListByServiceAsync("healthy-svc", "production", 1, 60, Arg.Any<CancellationToken>())
            .Returns(new List<RuntimeSnapshot> { healthy });

        var handler = new GetTopologyAwareAlerts.Handler(_snapshotRepo, _clock);

        var result = await handler.Handle(new GetTopologyAwareAlerts.Query(
            "healthy-svc", "production",
            new[] { "svc-b" }, 5m, 500m), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Alerts.Should().BeEmpty();
    }

    [Fact]
    public async Task GetTopologyAwareAlerts_WhenNoDependents_ShouldNotGeneratePropagationAlert()
    {
        var degraded = MakeSnapshot("isolated-svc", "production",
            latency: 600m, errorRate: 0.01m,
            capturedAt: FixedNow - TimeSpan.FromMinutes(5));

        _snapshotRepo.ListByServiceAsync("isolated-svc", "production", 1, 60, Arg.Any<CancellationToken>())
            .Returns(new List<RuntimeSnapshot> { degraded });

        var handler = new GetTopologyAwareAlerts.Handler(_snapshotRepo, _clock);

        var result = await handler.Handle(new GetTopologyAwareAlerts.Query(
            "isolated-svc", "production",
            Array.Empty<string>(), 5m, 500m), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Alerts.Any(a => a.AlertType == "PropagationRisk").Should().BeFalse();
        result.Value.Alerts.Any(a => a.AlertType == "HighLatency").Should().BeTrue();
    }
}
