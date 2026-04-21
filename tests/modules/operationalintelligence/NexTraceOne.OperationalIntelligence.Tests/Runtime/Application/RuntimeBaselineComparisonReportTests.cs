using System.Linq;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.OperationalIntelligence.Application.Runtime.Abstractions;
using NexTraceOne.OperationalIntelligence.Application.Runtime.Features.GetRuntimeBaselineComparisonReport;
using NexTraceOne.OperationalIntelligence.Domain.Runtime.Entities;
using DriftSeverity = NexTraceOne.OperationalIntelligence.Application.Runtime.Features.GetRuntimeBaselineComparisonReport.GetRuntimeBaselineComparisonReport.DriftSeverity;

namespace NexTraceOne.OperationalIntelligence.Tests.Runtime.Application;

/// <summary>
/// Testes unitários para Wave Q.1 — GetRuntimeBaselineComparisonReport.
/// Cobre: relatório vazio, classificação de severidade de drift (None/Minor/Moderate/Severe),
/// serviços sem baseline, desvio de latência e erro, top drifting services, médias tenant-level.
/// </summary>
public sealed class RuntimeBaselineComparisonReportTests
{
    private static readonly DateTimeOffset FixedNow = new(2026, 4, 21, 12, 0, 0, TimeSpan.Zero);

    private static IDateTimeProvider CreateClock()
    {
        var clock = Substitute.For<IDateTimeProvider>();
        clock.UtcNow.Returns(FixedNow);
        return clock;
    }

    private static RuntimeBaseline MakeBaseline(
        string serviceName,
        string env,
        decimal avgLatency = 100m,
        decimal p99Latency = 300m,
        decimal errorRate = 0.01m,
        decimal rps = 100m,
        decimal confidence = 0.9m)
        => RuntimeBaseline.Establish(serviceName, env, avgLatency, p99Latency, errorRate, rps,
            FixedNow.AddDays(-10), dataPointCount: 200, confidenceScore: confidence);

    private static RuntimeSnapshot MakeSnapshot(
        string serviceName,
        string env,
        decimal avgLatency = 100m,
        decimal p99Latency = 300m,
        decimal errorRate = 0.01m,
        decimal rps = 100m)
        => RuntimeSnapshot.Create(serviceName, env, avgLatency, p99Latency, errorRate, rps,
            cpuUsagePercent: 50m, memoryUsageMb: 512m, activeInstances: 2, capturedAt: FixedNow, source: "test");

    private static GetRuntimeBaselineComparisonReport.Handler CreateHandler(
        IReadOnlyList<(string ServiceName, string Environment)> pairs,
        IReadOnlyDictionary<(string, string), RuntimeBaseline?> baselines,
        IReadOnlyDictionary<(string, string), RuntimeSnapshot?> snapshots)
    {
        var snapshotRepo = Substitute.For<IRuntimeSnapshotRepository>();
        var baselineRepo = Substitute.For<IRuntimeBaselineRepository>();

        snapshotRepo.GetServicesWithRecentSnapshotsAsync(Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns(pairs);

        foreach (var kvp in baselines)
            baselineRepo.GetByServiceAndEnvironmentAsync(kvp.Key.Item1, kvp.Key.Item2, Arg.Any<CancellationToken>())
                .Returns(kvp.Value);

        foreach (var kvp in snapshots)
            snapshotRepo.GetLatestByServiceAsync(kvp.Key.Item1, kvp.Key.Item2, Arg.Any<CancellationToken>())
                .Returns(kvp.Value);

        return new GetRuntimeBaselineComparisonReport.Handler(snapshotRepo, baselineRepo, CreateClock());
    }

    // ── Empty report ──────────────────────────────────────────────────────

    [Fact]
    public async Task Report_Empty_When_No_Recent_Snapshots()
    {
        var handler = CreateHandler([], new Dictionary<(string, string), RuntimeBaseline?>(), new Dictionary<(string, string), RuntimeSnapshot?>());
        var result = await handler.Handle(new GetRuntimeBaselineComparisonReport.Query(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TotalServicesMonitored.Should().Be(0);
        result.Value.ServicesWithBaseline.Should().Be(0);
        result.Value.ServicesWithDrift.Should().Be(0);
        result.Value.TopDriftingServices.Should().BeEmpty();
    }

    // ── Baseline missing ──────────────────────────────────────────────────

    [Fact]
    public async Task Services_Without_Baseline_Counted_Correctly()
    {
        var pair = ("svc-a", "prod");
        var baselines = new Dictionary<(string, string), RuntimeBaseline?> { [pair] = null };
        var snapshots = new Dictionary<(string, string), RuntimeSnapshot?>();

        var handler = CreateHandler([pair], baselines, snapshots);
        var result = await handler.Handle(new GetRuntimeBaselineComparisonReport.Query(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TotalServicesMonitored.Should().Be(1);
        result.Value.ServicesWithBaseline.Should().Be(0);
        result.Value.ServicesWithoutBaseline.Should().Be(1);
        result.Value.ServicesWithDrift.Should().Be(0);
    }

    // ── None severity ─────────────────────────────────────────────────────

    [Fact]
    public async Task Severity_None_When_Snapshot_Matches_Baseline_Exactly()
    {
        var pair = ("svc-match", "prod");
        var baseline = MakeBaseline("svc-match", "prod");
        // Snapshot exactly matches baseline — 0% deviation
        var snapshot = MakeSnapshot("svc-match", "prod", 100m, 300m, 0.01m, 100m);

        var baselines = new Dictionary<(string, string), RuntimeBaseline?> { [pair] = baseline };
        var snapshots = new Dictionary<(string, string), RuntimeSnapshot?> { [pair] = snapshot };

        var handler = CreateHandler([pair], baselines, snapshots);
        var result = await handler.Handle(new GetRuntimeBaselineComparisonReport.Query(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.SeverityDistribution.NoneCount.Should().Be(1);
        result.Value.ServicesWithDrift.Should().Be(0);
        result.Value.TopDriftingServices.Single().Severity.Should().Be(DriftSeverity.None);
    }

    // ── Minor severity ────────────────────────────────────────────────────

    [Fact]
    public async Task Severity_Minor_When_Composite_Deviation_Between_5_And_15_Pct()
    {
        var pair = ("svc-minor", "prod");
        var baseline = MakeBaseline("svc-minor", "prod", avgLatency: 100m, errorRate: 0.01m, rps: 100m);
        // Avg latency 8% higher → composite approx 8%
        var snapshot = MakeSnapshot("svc-minor", "prod", avgLatency: 108m, errorRate: 0.01m, rps: 100m);

        var baselines = new Dictionary<(string, string), RuntimeBaseline?> { [pair] = baseline };
        var snapshots = new Dictionary<(string, string), RuntimeSnapshot?> { [pair] = snapshot };

        var handler = CreateHandler([pair], baselines, snapshots);
        var result = await handler.Handle(new GetRuntimeBaselineComparisonReport.Query(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.SeverityDistribution.MinorCount.Should().Be(1);
        result.Value.ServicesWithDrift.Should().Be(1);
        result.Value.TopDriftingServices.Single().Severity.Should().Be(DriftSeverity.Minor);
    }

    // ── Moderate severity ─────────────────────────────────────────────────

    [Fact]
    public async Task Severity_Moderate_When_Composite_Deviation_Between_15_And_30_Pct()
    {
        var pair = ("svc-mod", "staging");
        var baseline = MakeBaseline("svc-mod", "staging", avgLatency: 100m, errorRate: 0.01m, rps: 100m);
        // 20% higher latency → composite > 15%
        var snapshot = MakeSnapshot("svc-mod", "staging", avgLatency: 120m, errorRate: 0.01m, rps: 100m);

        var baselines = new Dictionary<(string, string), RuntimeBaseline?> { [pair] = baseline };
        var snapshots = new Dictionary<(string, string), RuntimeSnapshot?> { [pair] = snapshot };

        var handler = CreateHandler([pair], baselines, snapshots);
        var result = await handler.Handle(new GetRuntimeBaselineComparisonReport.Query(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.SeverityDistribution.ModerateCount.Should().Be(1);
        result.Value.TopDriftingServices.Single().Severity.Should().Be(DriftSeverity.Moderate);
    }

    // ── Severe severity ───────────────────────────────────────────────────

    [Fact]
    public async Task Severity_Severe_When_Composite_Deviation_Above_30_Pct()
    {
        var pair = ("svc-severe", "prod");
        var baseline = MakeBaseline("svc-severe", "prod", avgLatency: 100m, errorRate: 0.01m, rps: 100m);
        // 50% higher latency → composite > 30%
        var snapshot = MakeSnapshot("svc-severe", "prod", avgLatency: 150m, errorRate: 0.01m, rps: 100m);

        var baselines = new Dictionary<(string, string), RuntimeBaseline?> { [pair] = baseline };
        var snapshots = new Dictionary<(string, string), RuntimeSnapshot?> { [pair] = snapshot };

        var handler = CreateHandler([pair], baselines, snapshots);
        var result = await handler.Handle(new GetRuntimeBaselineComparisonReport.Query(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.SeverityDistribution.SevereCount.Should().Be(1);
        result.Value.SevereDriftCount.Should().Be(1);
        result.Value.TopDriftingServices.Single().Severity.Should().Be(DriftSeverity.Severe);
    }

    // ── Multiple services ─────────────────────────────────────────────────

    [Fact]
    public async Task Multiple_Services_Distribution_Correct()
    {
        var pairs = new[]
        {
            ("svc-1", "prod"),  // None (exact match)
            ("svc-2", "prod"),  // Severe (50% off)
            ("svc-3", "prod"),  // No baseline
        };

        var baselines = new Dictionary<(string, string), RuntimeBaseline?>
        {
            [("svc-1", "prod")] = MakeBaseline("svc-1", "prod"),
            [("svc-2", "prod")] = MakeBaseline("svc-2", "prod"),
            [("svc-3", "prod")] = null,
        };
        var snapshots = new Dictionary<(string, string), RuntimeSnapshot?>
        {
            [("svc-1", "prod")] = MakeSnapshot("svc-1", "prod"),
            [("svc-2", "prod")] = MakeSnapshot("svc-2", "prod", avgLatency: 150m),
        };

        var handler = CreateHandler(pairs, baselines, snapshots);
        var result = await handler.Handle(new GetRuntimeBaselineComparisonReport.Query(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TotalServicesMonitored.Should().Be(3);
        result.Value.ServicesWithBaseline.Should().Be(2);
        result.Value.ServicesWithoutBaseline.Should().Be(1);
        result.Value.SeverityDistribution.NoneCount.Should().Be(1);
        result.Value.SeverityDistribution.SevereCount.Should().Be(1);
        result.Value.ServicesWithDrift.Should().Be(1);
    }

    // ── Top ranking ───────────────────────────────────────────────────────

    [Fact]
    public async Task TopDriftingServices_Ordered_By_CompositeDeviation_Descending()
    {
        var pairs = new[]
        {
            ("svc-low", "prod"),
            ("svc-high", "prod"),
        };
        var baselines = new Dictionary<(string, string), RuntimeBaseline?>
        {
            [("svc-low", "prod")] = MakeBaseline("svc-low", "prod"),
            [("svc-high", "prod")] = MakeBaseline("svc-high", "prod"),
        };
        var snapshots = new Dictionary<(string, string), RuntimeSnapshot?>
        {
            [("svc-low", "prod")] = MakeSnapshot("svc-low", "prod", avgLatency: 105m),
            [("svc-high", "prod")] = MakeSnapshot("svc-high", "prod", avgLatency: 200m),
        };

        var handler = CreateHandler(pairs, baselines, snapshots);
        var result = await handler.Handle(new GetRuntimeBaselineComparisonReport.Query(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TopDriftingServices.First().ServiceName.Should().Be("svc-high");
        result.Value.TopDriftingServices.Last().ServiceName.Should().Be("svc-low");
    }

    // ── MaxTopServices cap ────────────────────────────────────────────────

    [Fact]
    public async Task TopDriftingServices_Capped_By_MaxTopServices()
    {
        var pairs = Enumerable.Range(1, 15).Select(i => ($"svc-{i}", "prod")).ToList();
        var baselines = pairs.ToDictionary(p => p, p => (RuntimeBaseline?)MakeBaseline(p.Item1, "prod"));
        var snapshots = pairs.ToDictionary(p => p, p => (RuntimeSnapshot?)MakeSnapshot(p.Item1, "prod", avgLatency: 150m));

        var handler = CreateHandler(pairs, baselines, snapshots);
        var result = await handler.Handle(new GetRuntimeBaselineComparisonReport.Query(MaxTopServices: 5), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TopDriftingServices.Count.Should().Be(5);
    }

    // ── Tenant averages ───────────────────────────────────────────────────

    [Fact]
    public async Task TenantAvg_Latency_Deviation_Computed_Correctly()
    {
        var pairs = new[] { ("svc-a", "prod"), ("svc-b", "prod") };
        var baselines = new Dictionary<(string, string), RuntimeBaseline?>
        {
            [("svc-a", "prod")] = MakeBaseline("svc-a", "prod", avgLatency: 100m),
            [("svc-b", "prod")] = MakeBaseline("svc-b", "prod", avgLatency: 100m),
        };
        var snapshots = new Dictionary<(string, string), RuntimeSnapshot?>
        {
            [("svc-a", "prod")] = MakeSnapshot("svc-a", "prod", avgLatency: 120m), // 20%
            [("svc-b", "prod")] = MakeSnapshot("svc-b", "prod", avgLatency: 140m), // 40%
        };

        var handler = CreateHandler(pairs, baselines, snapshots);
        var result = await handler.Handle(new GetRuntimeBaselineComparisonReport.Query(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TenantAvgLatencyDeviationPct.Should().BeApproximately(30m, 0.1m);
    }

    // ── Environment filter ────────────────────────────────────────────────

    [Fact]
    public async Task Environment_Filter_Excludes_Other_Environments()
    {
        var pairs = new[] { ("svc-a", "prod"), ("svc-b", "staging") };
        var baselines = new Dictionary<(string, string), RuntimeBaseline?>
        {
            [("svc-a", "prod")] = MakeBaseline("svc-a", "prod"),
            [("svc-b", "staging")] = MakeBaseline("svc-b", "staging"),
        };
        var snapshots = new Dictionary<(string, string), RuntimeSnapshot?>
        {
            [("svc-a", "prod")] = MakeSnapshot("svc-a", "prod", avgLatency: 150m),
            [("svc-b", "staging")] = MakeSnapshot("svc-b", "staging", avgLatency: 150m),
        };

        var handler = CreateHandler(pairs, baselines, snapshots);
        var result = await handler.Handle(
            new GetRuntimeBaselineComparisonReport.Query(Environment: "prod"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TotalServicesMonitored.Should().Be(1);
        result.Value.TopDriftingServices.All(e => e.Environment == "prod").Should().BeTrue();
    }

    // ── Validator ─────────────────────────────────────────────────────────

    [Fact]
    public void Validator_Rejects_LookbackHours_Zero()
    {
        var validator = new GetRuntimeBaselineComparisonReport.Validator();
        var result = validator.Validate(new GetRuntimeBaselineComparisonReport.Query(LookbackHours: 0));
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validator_Rejects_MaxTopServices_Zero()
    {
        var validator = new GetRuntimeBaselineComparisonReport.Validator();
        var result = validator.Validate(new GetRuntimeBaselineComparisonReport.Query(MaxTopServices: 0));
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validator_Accepts_Valid_Query()
    {
        var validator = new GetRuntimeBaselineComparisonReport.Validator();
        var result = validator.Validate(new GetRuntimeBaselineComparisonReport.Query(LookbackHours: 48, MaxTopServices: 10, MinorDriftThresholdPct: 5));
        result.IsValid.Should().BeTrue();
    }

    // ── Zero baseline expected values ──────────────────────────────────────

    [Fact]
    public async Task Zero_Expected_Rps_Does_Not_Divide_By_Zero()
    {
        var pair = ("svc-zero-rps", "prod");
        var baseline = RuntimeBaseline.Establish("svc-zero-rps", "prod",
            expectedAvgLatencyMs: 100m, expectedP99LatencyMs: 300m,
            expectedErrorRate: 0.01m, expectedRequestsPerSecond: 0m,
            establishedAt: FixedNow.AddDays(-1), dataPointCount: 10, confidenceScore: 0.5m);
        var snapshot = MakeSnapshot("svc-zero-rps", "prod");

        var baselines = new Dictionary<(string, string), RuntimeBaseline?> { [pair] = baseline };
        var snapshots = new Dictionary<(string, string), RuntimeSnapshot?> { [pair] = snapshot };

        var handler = CreateHandler([pair], baselines, snapshots);
        var result = await handler.Handle(new GetRuntimeBaselineComparisonReport.Query(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TopDriftingServices.Should().HaveCount(1);
    }

    // ── LookbackHours passed to repo ──────────────────────────────────────

    [Fact]
    public async Task LookbackHours_Controls_Since_Passed_To_Repo()
    {
        var snapshotRepo = Substitute.For<IRuntimeSnapshotRepository>();
        var baselineRepo = Substitute.For<IRuntimeBaselineRepository>();

        snapshotRepo.GetServicesWithRecentSnapshotsAsync(Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns([]);

        var handler = new GetRuntimeBaselineComparisonReport.Handler(snapshotRepo, baselineRepo, CreateClock());
        await handler.Handle(new GetRuntimeBaselineComparisonReport.Query(LookbackHours: 24), CancellationToken.None);

        await snapshotRepo.Received(1).GetServicesWithRecentSnapshotsAsync(
            Arg.Is<DateTimeOffset>(d => d == FixedNow.AddHours(-24)),
            Arg.Any<CancellationToken>());
    }

    // ── GeneratedAt ───────────────────────────────────────────────────────

    [Fact]
    public async Task Report_GeneratedAt_Matches_Clock()
    {
        var handler = CreateHandler([], new Dictionary<(string, string), RuntimeBaseline?>(), new Dictionary<(string, string), RuntimeSnapshot?>());
        var result = await handler.Handle(new GetRuntimeBaselineComparisonReport.Query(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.GeneratedAt.Should().Be(FixedNow);
    }

    // ── Confidence score propagated ───────────────────────────────────────

    [Fact]
    public async Task BaselineConfidenceScore_Propagated_To_DriftEntry()
    {
        var pair = ("svc-conf", "prod");
        var baseline = MakeBaseline("svc-conf", "prod", confidence: 0.75m);
        var snapshot = MakeSnapshot("svc-conf", "prod");

        var baselines = new Dictionary<(string, string), RuntimeBaseline?> { [pair] = baseline };
        var snapshots = new Dictionary<(string, string), RuntimeSnapshot?> { [pair] = snapshot };

        var handler = CreateHandler([pair], baselines, snapshots);
        var result = await handler.Handle(new GetRuntimeBaselineComparisonReport.Query(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TopDriftingServices.Single().BaselineConfidenceScore.Should().Be(0.75m);
    }
}
