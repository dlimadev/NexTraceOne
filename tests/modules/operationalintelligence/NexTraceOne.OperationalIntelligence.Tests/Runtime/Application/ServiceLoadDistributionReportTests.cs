using System.Linq;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.OperationalIntelligence.Application.FinOps.Abstractions;
using NexTraceOne.OperationalIntelligence.Application.Runtime.Abstractions;
using NexTraceOne.OperationalIntelligence.Application.Runtime.Features.GetServiceLoadDistributionReport;
using NexTraceOne.OperationalIntelligence.Domain.FinOps.Entities;
using NexTraceOne.OperationalIntelligence.Domain.FinOps.Enums;
using NexTraceOne.OperationalIntelligence.Domain.Runtime.Entities;

namespace NexTraceOne.OperationalIntelligence.Tests.Runtime.Application;

/// <summary>
/// Testes unitários para Wave U.3 — GetServiceLoadDistributionReport.
/// Cobre: sem snapshots (empty), serviço único (medium load), high/medium/low band,
/// waste candidate flag, high cost efficiency flag, median cálculo, validator.
/// </summary>
public sealed class ServiceLoadDistributionReportTests
{
    private static readonly DateTimeOffset FixedNow = new(2026, 4, 21, 12, 0, 0, TimeSpan.Zero);
    private const string TenantId = "tenant-load-dist-001";
    private const string Env = "prod";

    private static IDateTimeProvider CreateClock()
    {
        var clock = Substitute.For<IDateTimeProvider>();
        clock.UtcNow.Returns(FixedNow);
        return clock;
    }

    private static RuntimeSnapshot MakeSnapshot(
        string serviceName, string env, decimal rps, decimal avgLatency = 100m,
        decimal errorRate = 0.01m, DateTimeOffset? capturedAt = null)
        => RuntimeSnapshot.Create(
            serviceName, env, avgLatency, avgLatency * 2m, errorRate,
            rps, cpuUsagePercent: 50m, memoryUsageMb: 512m, activeInstances: 1,
            capturedAt: capturedAt ?? FixedNow.AddHours(-1),
            source: "test");

    private static ServiceCostAllocationRecord MakeCost(string tenantId, string serviceName, string env, decimal amount)
        => ServiceCostAllocationRecord.Create(
            tenantId, serviceName, env, CostCategory.Compute, amount,
            periodStart: FixedNow.AddDays(-30), periodEnd: FixedNow,
            createdAt: FixedNow);

    private static GetServiceLoadDistributionReport.Handler CreateHandler(
        IReadOnlyList<(string ServiceName, string Environment)> servicePairs,
        Func<string, string, IReadOnlyList<RuntimeSnapshot>> snapshotsByService,
        IReadOnlyList<ServiceCostAllocationRecord> costRecords)
    {
        var snapshotRepo = Substitute.For<IRuntimeSnapshotRepository>();
        var costRepo = Substitute.For<IServiceCostAllocationRepository>();

        snapshotRepo.GetServicesWithRecentSnapshotsAsync(
                Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns(servicePairs);

        foreach (var (svc, env) in servicePairs)
        {
            var key = (svc, env);
            snapshotRepo.ListByServiceAsync(
                    svc, env,
                    Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
                .Returns(snapshotsByService(svc, env));
        }

        costRepo.ListByTenantAsync(
                Arg.Any<string>(), Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(),
                Arg.Any<string?>(), Arg.Any<CostCategory?>(), Arg.Any<CancellationToken>())
            .Returns(costRecords);

        return new GetServiceLoadDistributionReport.Handler(snapshotRepo, costRepo, CreateClock());
    }

    private static GetServiceLoadDistributionReport.Query DefaultQuery()
        => new(TenantId: TenantId, LookbackDays: 30);

    // ── Empty: no service pairs → empty report ─────────────────────────────

    [Fact]
    public async Task Handle_NoServicePairs_ReturnsEmptyReport()
    {
        var handler = CreateHandler([], (_, _) => [], []);
        var result = await handler.Handle(DefaultQuery(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var r = result.Value;
        Assert.Equal(0, r.TotalServicesAnalyzed);
        Assert.Empty(r.AllServices);
    }

    // ── Single service: MediumLoad (only one → not top/bottom quartile) ────

    [Fact]
    public async Task Handle_SingleService_AnalyzesAndReturnsBand()
    {
        // With a single service, P75 == P25 == rps, so rps >= highThreshold → HighLoad
        var pairs = new[] { ("svc-a", Env) };
        var snapshot = MakeSnapshot("svc-a", Env, rps: 50m);

        var handler = CreateHandler(
            pairs,
            (_, _) => [snapshot],
            []);

        var result = await handler.Handle(DefaultQuery(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var r = result.Value;
        Assert.Equal(1, r.TotalServicesAnalyzed);
        Assert.Single(r.AllServices);
        // Single service: P75 == P25 == rps, so >= P75 → HighLoad (boundary condition)
        Assert.Equal(GetServiceLoadDistributionReport.LoadBand.HighLoad, r.AllServices[0].Band);
    }

    // ── High/Low load band assignment ─────────────────────────────────────

    [Fact]
    public async Task Handle_FourServices_CorrectBandDistribution()
    {
        // 4 services: RPS 10, 20, 30, 40
        // P25 = 15, P75 = 35
        // 10 → LowLoad, 20/30 → MediumLoad, 40 → HighLoad
        var svcA = ("svc-a", Env);
        var svcB = ("svc-b", Env);
        var svcC = ("svc-c", Env);
        var svcD = ("svc-d", Env);

        var pairs = new[] { svcA, svcB, svcC, svcD };

        var handler = CreateHandler(pairs, (svc, env) => svc switch
        {
            "svc-a" => [MakeSnapshot(svc, env, rps: 10m)],
            "svc-b" => [MakeSnapshot(svc, env, rps: 20m)],
            "svc-c" => [MakeSnapshot(svc, env, rps: 30m)],
            "svc-d" => [MakeSnapshot(svc, env, rps: 40m)],
            _ => []
        }, []);

        var result = await handler.Handle(DefaultQuery(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var r = result.Value;
        Assert.Equal(4, r.TotalServicesAnalyzed);

        var low = r.AllServices.Where(e => e.Band == GetServiceLoadDistributionReport.LoadBand.LowLoad).ToList();
        var high = r.AllServices.Where(e => e.Band == GetServiceLoadDistributionReport.LoadBand.HighLoad).ToList();

        Assert.True(low.Count >= 1, "Expected at least 1 LowLoad service");
        Assert.True(high.Count >= 1, "Expected at least 1 HighLoad service");
    }

    // ── WasteCandidate: LowLoad service with cost above median total cost ──

    [Fact]
    public async Task Handle_LowLoadServiceWithAboveMedianCost_FlagsWasteCandidate()
    {
        // 4 services: svc-waste (very low RPS, very high cost), others have medium/high RPS and low cost
        // svc-waste total cost = $10000 >> median total cost (others have $10)
        var pairs = new[] { ("svc-waste", Env), ("svc-m1", Env), ("svc-m2", Env), ("svc-m3", Env) };

        var handler = CreateHandler(pairs, (svc, env) => svc switch
        {
            "svc-waste" => [MakeSnapshot(svc, env, rps: 1m)],   // LowLoad
            "svc-m1" => [MakeSnapshot(svc, env, rps: 50m)],
            "svc-m2" => [MakeSnapshot(svc, env, rps: 60m)],
            "svc-m3" => [MakeSnapshot(svc, env, rps: 70m)],
            _ => []
        }, [
            MakeCost(TenantId, "svc-waste", Env, 10000m), // expensive waste candidate
            MakeCost(TenantId, "svc-m1", Env, 10m),
            MakeCost(TenantId, "svc-m2", Env, 10m),
            MakeCost(TenantId, "svc-m3", Env, 10m)
        ]);

        var result = await handler.Handle(DefaultQuery(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var wasteEntry = result.Value.AllServices.Single(e => e.ServiceName == "svc-waste");
        Assert.Equal(GetServiceLoadDistributionReport.LoadBand.LowLoad, wasteEntry.Band);
        Assert.True(wasteEntry.WasteCandidate);
    }

    // ── CostPerRequest calculation ─────────────────────────────────────────

    [Fact]
    public async Task Handle_ServiceWithCostAndRps_ComputesCostPerRequest()
    {
        var pairs = new[] { ("svc-cost", Env) };
        // 100 RPS over 30 days = 100 * 30 * 86400 = 259,200,000 requests
        // Cost = $259.2 → CostPerRequest = $0.000001 USD
        var snapshot = MakeSnapshot("svc-cost", Env, rps: 100m);
        var costRecord = MakeCost(TenantId, "svc-cost", Env, 259.2m);

        var handler = CreateHandler(pairs, (_, _) => [snapshot], [costRecord]);
        var result = await handler.Handle(DefaultQuery(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var entry = result.Value.AllServices.Single();
        Assert.True(entry.CostPerRequestUsd > 0m, "CostPerRequest should be > 0 when RPS and cost are set");
    }

    // ── No snapshots for pair in period → pair skipped ────────────────────

    [Fact]
    public async Task Handle_PairWithOldSnapshotsOnly_IsSkipped()
    {
        var pairs = new[] { ("svc-old", Env) };
        // Snapshot older than lookback window
        var oldSnapshot = MakeSnapshot("svc-old", Env, rps: 50m,
            capturedAt: FixedNow.AddDays(-60));

        var handler = CreateHandler(pairs, (_, _) => [oldSnapshot], []);
        var result = await handler.Handle(DefaultQuery(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(0, result.Value.TotalServicesAnalyzed);
    }

    // ── TopWasteServices ordered by highest CostPerRequest ────────────────

    [Fact]
    public async Task Handle_MultipleServices_TopWasteOrderedByHighestCostPerRequest()
    {
        var pairs = new[] { ("svc-a", Env), ("svc-b", Env), ("svc-c", Env), ("svc-d", Env) };

        var handler = CreateHandler(pairs, (svc, env) => svc switch
        {
            "svc-a" => [MakeSnapshot(svc, env, rps: 1m)],
            "svc-b" => [MakeSnapshot(svc, env, rps: 50m)],
            "svc-c" => [MakeSnapshot(svc, env, rps: 60m)],
            "svc-d" => [MakeSnapshot(svc, env, rps: 70m)],
            _ => []
        }, [
            MakeCost(TenantId, "svc-a", Env, 5000m), // expensive + low RPS = highest cost/req
            MakeCost(TenantId, "svc-b", Env, 10m),
            MakeCost(TenantId, "svc-c", Env, 5m),
            MakeCost(TenantId, "svc-d", Env, 2m)
        ]);

        var result = await handler.Handle(DefaultQuery(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var r = result.Value;
        Assert.True(r.TopWasteServices.Count > 0);
        Assert.Equal("svc-a", r.TopWasteServices.First().ServiceName);
    }

    // ── MedianRequestsPerSecond calculated correctly ───────────────────────

    [Fact]
    public async Task Handle_EvenNumberOfServices_MedianRpsIsAvgOfMiddleTwo()
    {
        // 4 services: 10, 20, 30, 40 → median = (20+30)/2 = 25
        var pairs = new[] { ("svc-a", Env), ("svc-b", Env), ("svc-c", Env), ("svc-d", Env) };

        var handler = CreateHandler(pairs, (svc, env) => svc switch
        {
            "svc-a" => [MakeSnapshot(svc, env, rps: 10m)],
            "svc-b" => [MakeSnapshot(svc, env, rps: 20m)],
            "svc-c" => [MakeSnapshot(svc, env, rps: 30m)],
            "svc-d" => [MakeSnapshot(svc, env, rps: 40m)],
            _ => []
        }, []);

        var result = await handler.Handle(DefaultQuery(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(25m, result.Value.MedianRequestsPerSecond);
    }

    // ── Validator ──────────────────────────────────────────────────────────

    [Fact]
    public void Validator_EmptyTenantId_ReturnsError()
    {
        var v = new GetServiceLoadDistributionReport.Validator();
        Assert.False(v.Validate(new GetServiceLoadDistributionReport.Query(TenantId: "")).IsValid);
    }

    [Fact]
    public void Validator_InvalidLookbackDays_ReturnsError()
    {
        var v = new GetServiceLoadDistributionReport.Validator();
        Assert.False(v.Validate(new GetServiceLoadDistributionReport.Query(TenantId: TenantId, LookbackDays: 3)).IsValid);
    }

    [Fact]
    public void Validator_ValidQuery_PassesValidation()
    {
        var v = new GetServiceLoadDistributionReport.Validator();
        Assert.True(v.Validate(new GetServiceLoadDistributionReport.Query(TenantId: TenantId)).IsValid);
    }

    [Fact]
    public void Validator_InvalidMaxTopServices_ReturnsError()
    {
        var v = new GetServiceLoadDistributionReport.Validator();
        Assert.False(v.Validate(new GetServiceLoadDistributionReport.Query(TenantId: TenantId, MaxTopServices: 0)).IsValid);
    }
}
