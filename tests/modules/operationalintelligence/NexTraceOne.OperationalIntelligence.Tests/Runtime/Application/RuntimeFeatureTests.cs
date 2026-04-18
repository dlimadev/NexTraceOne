using System.Linq;
using FluentAssertions;
using NSubstitute;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.OperationalIntelligence.Application.Runtime.Abstractions;
using NexTraceOne.OperationalIntelligence.Application.Runtime.Features.CompareReleaseRuntime;
using NexTraceOne.OperationalIntelligence.Application.Runtime.Features.DetectRuntimeDrift;
using NexTraceOne.OperationalIntelligence.Application.Runtime.Features.GetDriftFindings;
using NexTraceOne.OperationalIntelligence.Domain.Runtime.Entities;
using Xunit;

namespace NexTraceOne.OperationalIntelligence.Tests.Runtime.Application;

/// <summary>
/// Testes unitários para as features do subdomínio RuntimeIntelligence.
/// Cobrem: DetectRuntimeDrift, GetDriftFindings, CompareReleaseRuntime.
/// </summary>
public sealed class RuntimeFeatureTests
{
    private static readonly DateTimeOffset FixedNow = new(2025, 10, 1, 12, 0, 0, TimeSpan.Zero);

    // ── DetectRuntimeDrift ────────────────────────────────────────────────

    [Fact]
    public async Task DetectRuntimeDrift_WhenDriftExists_ShouldReturnFindings()
    {
        var snapshotRepo = Substitute.For<IRuntimeSnapshotRepository>();
        var baselineRepo = Substitute.For<IRuntimeBaselineRepository>();
        var findingRepo = Substitute.For<IDriftFindingRepository>();
        var unitOfWork = Substitute.For<IRuntimeIntelligenceUnitOfWork>();
        var dateTimeProvider = Substitute.For<IDateTimeProvider>();

        dateTimeProvider.UtcNow.Returns(FixedNow);

        var baseline = RuntimeBaseline.Establish(
            "order-service", "production",
            100m, 200m, 0.01m, 500m,
            FixedNow - TimeSpan.FromDays(30), 100, 0.9m);

        var snapshot = RuntimeSnapshot.Create(
            "order-service", "production",
            180m, 300m, 0.02m, 500m, 30m, 512m, 1, FixedNow, "test");

        baselineRepo.GetByServiceAndEnvironmentAsync("order-service", "production", Arg.Any<CancellationToken>())
            .Returns(baseline);
        snapshotRepo.GetLatestByServiceAsync("order-service", "production", Arg.Any<CancellationToken>())
            .Returns(snapshot);

        var handler = new DetectRuntimeDrift.Handler(
            snapshotRepo, baselineRepo, findingRepo, unitOfWork, dateTimeProvider);

        var result = await handler.Handle(
            new DetectRuntimeDrift.Command("order-service", "production", 10m),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.HasDrift.Should().BeTrue();
        result.Value.Findings.Should().NotBeEmpty();
        result.Value.ServiceName.Should().Be("order-service");
        result.Value.Environment.Should().Be("production");
    }

    [Fact]
    public async Task DetectRuntimeDrift_WhenNoBaseline_ShouldReturnError()
    {
        var snapshotRepo = Substitute.For<IRuntimeSnapshotRepository>();
        var baselineRepo = Substitute.For<IRuntimeBaselineRepository>();
        var findingRepo = Substitute.For<IDriftFindingRepository>();
        var unitOfWork = Substitute.For<IRuntimeIntelligenceUnitOfWork>();
        var dateTimeProvider = Substitute.For<IDateTimeProvider>();

        dateTimeProvider.UtcNow.Returns(FixedNow);
        baselineRepo.GetByServiceAndEnvironmentAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((RuntimeBaseline?)null);

        var handler = new DetectRuntimeDrift.Handler(
            snapshotRepo, baselineRepo, findingRepo, unitOfWork, dateTimeProvider);

        var result = await handler.Handle(
            new DetectRuntimeDrift.Command("unknown-service", "production", 10m),
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task DetectRuntimeDrift_WhenWithinTolerance_ShouldReturnNoDrift()
    {
        var snapshotRepo = Substitute.For<IRuntimeSnapshotRepository>();
        var baselineRepo = Substitute.For<IRuntimeBaselineRepository>();
        var findingRepo = Substitute.For<IDriftFindingRepository>();
        var unitOfWork = Substitute.For<IRuntimeIntelligenceUnitOfWork>();
        var dateTimeProvider = Substitute.For<IDateTimeProvider>();

        dateTimeProvider.UtcNow.Returns(FixedNow);

        var baseline = RuntimeBaseline.Establish(
            "stable-service", "staging",
            100m, 200m, 0.01m, 500m,
            FixedNow - TimeSpan.FromDays(7), 50, 0.85m);

        // Snapshot within 10% tolerance
        var snapshot = RuntimeSnapshot.Create(
            "stable-service", "staging",
            105m, 205m, 0.0105m, 495m, 30m, 512m, 1, FixedNow, "test");

        baselineRepo.GetByServiceAndEnvironmentAsync("stable-service", "staging", Arg.Any<CancellationToken>())
            .Returns(baseline);
        snapshotRepo.GetLatestByServiceAsync("stable-service", "staging", Arg.Any<CancellationToken>())
            .Returns(snapshot);

        var handler = new DetectRuntimeDrift.Handler(
            snapshotRepo, baselineRepo, findingRepo, unitOfWork, dateTimeProvider);

        var result = await handler.Handle(
            new DetectRuntimeDrift.Command("stable-service", "staging", 10m),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.HasDrift.Should().BeFalse();
        result.Value.Findings.Should().BeEmpty();
    }

    // ── GetDriftFindings ──────────────────────────────────────────────────

    [Fact]
    public async Task GetDriftFindings_WhenFindingsExist_ShouldReturnPagedList()
    {
        var repo = Substitute.For<IDriftFindingRepository>();

        var findings = new List<DriftFinding>
        {
            DriftFinding.Detect("payment-service", "production", "AvgLatencyMs", 100m, 180m, FixedNow),
            DriftFinding.Detect("payment-service", "production", "ErrorRate", 0.01m, 0.025m, FixedNow)
        };

        repo.ListByServiceAsync("payment-service", "production", 1, 20, Arg.Any<CancellationToken>())
            .Returns(findings);

        var handler = new GetDriftFindings.Handler(repo);
        var result = await handler.Handle(
            new GetDriftFindings.Query("payment-service", "production"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Findings.Should().HaveCount(2);
        result.Value.Findings.Should().AllSatisfy(f => f.ServiceName.Should().Be("payment-service"));
    }

    [Fact]
    public async Task GetDriftFindings_UnacknowledgedOnly_ShouldCallCorrectRepository()
    {
        var repo = Substitute.For<IDriftFindingRepository>();

        repo.ListUnacknowledgedAsync(1, 10, Arg.Any<CancellationToken>())
            .Returns(new List<DriftFinding>
            {
                DriftFinding.Detect("api-gateway", "staging", "ErrorRate", 0.01m, 0.05m, FixedNow)
            });

        var handler = new GetDriftFindings.Handler(repo);
        var result = await handler.Handle(
            new GetDriftFindings.Query("api-gateway", "staging", UnacknowledgedOnly: true, PageSize: 10),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Findings.Should().HaveCount(1);
        await repo.Received(1).ListUnacknowledgedAsync(1, 10, Arg.Any<CancellationToken>());
    }

    // ── CompareReleaseRuntime ─────────────────────────────────────────────

    [Fact]
    public async Task CompareReleaseRuntime_WithTwoPeriodsOfData_ShouldReturnDelta()
    {
        var repo = Substitute.For<IRuntimeSnapshotRepository>();

        var now = FixedNow;
        var beforeStart = now - TimeSpan.FromDays(14);
        var beforeEnd = now - TimeSpan.FromDays(8); // Non-overlapping with afterStart
        var afterStart = now - TimeSpan.FromDays(7);
        var afterEnd = now;

        // Before period: lower latency, lower error rate
        var beforeSnapshots = Enumerable.Range(0, 5)
            .Select(i => RuntimeSnapshot.Create(
                "checkout-service", "production",
                100m, 200m, 0.01m, 500m, 30m, 512m, 1,
                beforeStart + TimeSpan.FromDays(i), "test"))
            .ToList<RuntimeSnapshot>();

        // After period: higher latency (20% regression), higher error rate
        var afterSnapshots = Enumerable.Range(0, 5)
            .Select(i => RuntimeSnapshot.Create(
                "checkout-service", "production",
                120m, 240m, 0.02m, 480m, 35m, 600m, 1,
                afterStart + TimeSpan.FromDays(i), "test"))
            .ToList<RuntimeSnapshot>();

        var allSnapshots = beforeSnapshots.Concat(afterSnapshots).ToList();

        repo.ListByServiceAsync("checkout-service", "production", 1, 1000, Arg.Any<CancellationToken>())
            .Returns(allSnapshots);

        var handler = new CompareReleaseRuntime.Handler(repo);
        var result = await handler.Handle(
            new CompareReleaseRuntime.Query(
                "checkout-service", "production",
                beforeStart, beforeEnd,
                afterStart, afterEnd),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.BeforeDataPoints.Should().Be(5);
        result.Value.AfterDataPoints.Should().Be(5);
        result.Value.LatencyDeltaPercent.Should().Be(20m);
        result.Value.ErrorRateDeltaPercent.Should().Be(100m);
    }

    [Fact]
    public async Task CompareReleaseRuntime_WhenNoPeriodData_ShouldReturnZeroMetrics()
    {
        var repo = Substitute.For<IRuntimeSnapshotRepository>();

        repo.ListByServiceAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(new List<RuntimeSnapshot>());

        var handler = new CompareReleaseRuntime.Handler(repo);
        var now = FixedNow;

        var result = await handler.Handle(
            new CompareReleaseRuntime.Query(
                "unknown-service", "production",
                now - TimeSpan.FromDays(14), now - TimeSpan.FromDays(7),
                now - TimeSpan.FromDays(7), now),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.BeforeDataPoints.Should().Be(0);
        result.Value.AfterDataPoints.Should().Be(0);
        result.Value.BeforeMetrics.AvgLatencyMs.Should().Be(0);
        result.Value.AfterMetrics.AvgLatencyMs.Should().Be(0);
    }
}

