using System.Linq;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.Catalog.Application.Contracts.Abstractions;
using NexTraceOne.Catalog.Application.Contracts.Features.GetHighTrafficEndpointRiskReport;
using NexTraceOne.Catalog.Application.Contracts.Features.GetRuntimeTrafficContractDeviationReport;

namespace NexTraceOne.Catalog.Tests.Contracts.Application.Features;

/// <summary>
/// Testes unitários para Wave AZ.1 GetRuntimeTrafficContractDeviationReport (~17 testes)
/// e Wave AZ.2 GetHighTrafficEndpointRiskReport (~15 testes).
/// </summary>
public sealed class WaveAzTrafficIntelligenceTests
{
    private static readonly DateTimeOffset FixedNow = new(2026, 10, 1, 0, 0, 0, TimeSpan.Zero);
    private const string TenantId = "tenant-az-001";

    private static IDateTimeProvider Clock()
    {
        var c = Substitute.For<IDateTimeProvider>();
        c.UtcNow.Returns(FixedNow);
        return c;
    }

    // ════════════════════════════════════════════════════════════════════════
    // AZ.1 — GetRuntimeTrafficContractDeviationReport
    // ════════════════════════════════════════════════════════════════════════

    private static GetRuntimeTrafficContractDeviationReport.Handler DeviationHandler(
        ITrafficObservationReader? reader = null) =>
        new(reader ?? Substitute.For<ITrafficObservationReader>(), Clock());

    private static ITrafficObservationReader BuildDeviationReader(
        IReadOnlyList<ITrafficObservationReader.ServiceTrafficObservationEntry> entries,
        IReadOnlyList<ITrafficObservationReader.DailyDeviationSnapshot>? trend = null)
    {
        var r = Substitute.For<ITrafficObservationReader>();
        r.ListByTenantAsync(TenantId, Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
         .Returns(entries);
        r.GetDeviationTrendAsync(TenantId, Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
         .Returns(trend ?? []);
        return r;
    }

    private static ITrafficObservationReader.ServiceTrafficObservationEntry MakeSvc(
        string svcId = "svc-1",
        string svcName = "service-1",
        string teamName = "team-1",
        string tier = "Internal",
        IReadOnlyList<ITrafficObservationReader.ObservedEndpoint>? observed = null,
        IReadOnlyList<string>? observedConsumers = null,
        IReadOnlyList<string>? registeredConsumers = null,
        IReadOnlyList<string>? contractedEndpoints = null,
        IReadOnlyList<string>? contractedStatusCodes = null,
        IReadOnlyList<ITrafficObservationReader.StatusCodeObservation>? observedStatusCodes = null,
        int totalPayloadEvents = 100,
        int payloadDeviations = 0) =>
        new(svcId, svcName, teamName, tier,
            observed ?? [new("GET", "/api/v1/users", 1000)],
            observedConsumers ?? ["svc-consumer-a"],
            registeredConsumers ?? ["svc-consumer-a"],
            contractedEndpoints ?? ["GET:/api/v1/users"],
            contractedStatusCodes ?? ["200", "400"],
            observedStatusCodes ?? [new(200, 900), new(400, 100)],
            totalPayloadEvents, payloadDeviations);

    // ────────────────────────────────────────────────────────────────────────
    // Empty report
    // ────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task AZ1_EmptyReport_WhenNoEntries()
    {
        var h = DeviationHandler(BuildDeviationReader([]));
        var r = await h.Handle(new GetRuntimeTrafficContractDeviationReport.Query(TenantId), CancellationToken.None);

        r.IsSuccess.Should().BeTrue();
        r.Value.ByService.Should().BeEmpty();
        r.Value.Summary.TenantDeviationHealthScore.Should().Be(100m);
        r.Value.UndocumentedEndpointHotspots.Should().BeEmpty();
        r.Value.ContractGapOpportunities.Should().BeEmpty();
    }

    // ────────────────────────────────────────────────────────────────────────
    // Tier logic
    // ────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task AZ1_Tier_Aligned_WhenNoDeviations()
    {
        var svc = MakeSvc();
        var h = DeviationHandler(BuildDeviationReader([svc]));
        var r = await h.Handle(new GetRuntimeTrafficContractDeviationReport.Query(TenantId), CancellationToken.None);

        r.Value.ByService[0].DeviationTier.Should()
            .Be(GetRuntimeTrafficContractDeviationReport.TrafficContractDeviationTier.Aligned);
    }

    [Fact]
    public async Task AZ1_Tier_MinorDrift_WhenFewUndocumentedEndpoints()
    {
        // 2 undocumented endpoints ≤ minor drift threshold (3)
        var svc = MakeSvc(
            observed: [new("GET", "/api/v1/users", 100), new("POST", "/api/v1/users", 50), new("DELETE", "/api/v1/users/1", 10)],
            contractedEndpoints: ["GET:/api/v1/users"]);

        var h = DeviationHandler(BuildDeviationReader([svc]));
        var r = await h.Handle(
            new GetRuntimeTrafficContractDeviationReport.Query(TenantId, MinorDriftThreshold: 3),
            CancellationToken.None);

        r.Value.ByService[0].DeviationTier.Should()
            .Be(GetRuntimeTrafficContractDeviationReport.TrafficContractDeviationTier.MinorDrift);
    }

    [Fact]
    public async Task AZ1_Tier_Significant_WhenManyUndocumentedEndpoints()
    {
        // 10 undocumented > 3*2=6 → Significant
        var endpoints = Enumerable.Range(1, 10)
            .Select(i => new ITrafficObservationReader.ObservedEndpoint("GET", $"/api/v1/ep{i}", 50))
            .ToList<ITrafficObservationReader.ObservedEndpoint>();

        var svc = MakeSvc(observed: endpoints, contractedEndpoints: []);
        var h = DeviationHandler(BuildDeviationReader([svc]));
        var r = await h.Handle(
            new GetRuntimeTrafficContractDeviationReport.Query(TenantId, MinorDriftThreshold: 3),
            CancellationToken.None);

        r.Value.ByService[0].DeviationTier.Should()
            .Be(GetRuntimeTrafficContractDeviationReport.TrafficContractDeviationTier.Significant);
    }

    [Fact]
    public async Task AZ1_Tier_Critical_WhenUndeclaredConsumerOnCriticalService()
    {
        var svc = MakeSvc(
            tier: "Critical",
            observedConsumers: ["svc-a", "svc-unknown"],
            registeredConsumers: ["svc-a"]);

        var h = DeviationHandler(BuildDeviationReader([svc]));
        var r = await h.Handle(
            new GetRuntimeTrafficContractDeviationReport.Query(TenantId, CriticalUndeclaredConsumers: 1),
            CancellationToken.None);

        r.Value.ByService[0].DeviationTier.Should()
            .Be(GetRuntimeTrafficContractDeviationReport.TrafficContractDeviationTier.Critical);
    }

    // ────────────────────────────────────────────────────────────────────────
    // PayloadDeviationRate
    // ────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task AZ1_PayloadDeviationRate_CalculatedCorrectly()
    {
        // 20 deviations in 100 events = 20%
        var svc = MakeSvc(totalPayloadEvents: 100, payloadDeviations: 20);
        var h = DeviationHandler(BuildDeviationReader([svc]));
        var r = await h.Handle(new GetRuntimeTrafficContractDeviationReport.Query(TenantId), CancellationToken.None);

        r.Value.ByService[0].PayloadDeviationRate.Should().Be(20m);
    }

    [Fact]
    public async Task AZ1_PayloadDeviationRate_ZeroWhenNoValidationEvents()
    {
        var svc = MakeSvc(totalPayloadEvents: 0, payloadDeviations: 0);
        var h = DeviationHandler(BuildDeviationReader([svc]));
        var r = await h.Handle(new GetRuntimeTrafficContractDeviationReport.Query(TenantId), CancellationToken.None);

        r.Value.ByService[0].PayloadDeviationRate.Should().Be(0m);
    }

    // ────────────────────────────────────────────────────────────────────────
    // TenantDeviationHealthScore
    // ────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task AZ1_TenantHealthScore_100_WhenAllAligned()
    {
        var svcs = Enumerable.Range(1, 5)
            .Select(i => MakeSvc($"s{i}", $"svc-{i}"))
            .ToList();

        var h = DeviationHandler(BuildDeviationReader(svcs));
        var r = await h.Handle(new GetRuntimeTrafficContractDeviationReport.Query(TenantId), CancellationToken.None);

        r.Value.Summary.TenantDeviationHealthScore.Should().Be(100m);
    }

    [Fact]
    public async Task AZ1_TenantHealthScore_50_WhenHalfAligned()
    {
        var aligned = MakeSvc("s1", "svc-1");
        var deviating = MakeSvc("s2", "svc-2",
            observed: Enumerable.Range(1, 10).Select(i =>
                new ITrafficObservationReader.ObservedEndpoint("GET", $"/undoc/{i}", 100)).ToList(),
            contractedEndpoints: []);

        var h = DeviationHandler(BuildDeviationReader([aligned, deviating]));
        var r = await h.Handle(
            new GetRuntimeTrafficContractDeviationReport.Query(TenantId, MinorDriftThreshold: 3),
            CancellationToken.None);

        r.Value.Summary.TenantDeviationHealthScore.Should().Be(50m);
    }

    // ────────────────────────────────────────────────────────────────────────
    // UndocumentedEndpointHotspots
    // ────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task AZ1_Hotspots_OrderedByCallCount()
    {
        var svc = MakeSvc(
            observed: [
                new("GET", "/hot", 5000),
                new("POST", "/cold", 100),
                new("GET", "/warm", 1000)],
            contractedEndpoints: []);

        var h = DeviationHandler(BuildDeviationReader([svc]));
        var r = await h.Handle(
            new GetRuntimeTrafficContractDeviationReport.Query(TenantId, TopHotspotsCount: 2),
            CancellationToken.None);

        r.Value.UndocumentedEndpointHotspots.Should().HaveCount(2);
        r.Value.UndocumentedEndpointHotspots[0].Path.Should().Be("/hot");
        r.Value.UndocumentedEndpointHotspots[1].Path.Should().Be("/warm");
    }

    // ────────────────────────────────────────────────────────────────────────
    // HistoricalDeviationTrend
    // ────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task AZ1_Trend_Improving_WhenDeviationsDecreasing()
    {
        var snapshots = new[]
        {
            new ITrafficObservationReader.DailyDeviationSnapshot(0, 20),
            new ITrafficObservationReader.DailyDeviationSnapshot(1, 10),
        };
        var h = DeviationHandler(BuildDeviationReader([], snapshots));
        var r = await h.Handle(new GetRuntimeTrafficContractDeviationReport.Query(TenantId), CancellationToken.None);

        r.Value.HistoricalDeviationTrend.Should()
            .Be(GetRuntimeTrafficContractDeviationReport.DeviationTrendDirection.Improving);
    }

    [Fact]
    public async Task AZ1_Trend_Worsening_WhenDeviationsIncreasing()
    {
        var snapshots = new[]
        {
            new ITrafficObservationReader.DailyDeviationSnapshot(0, 5),
            new ITrafficObservationReader.DailyDeviationSnapshot(1, 15),
        };
        var h = DeviationHandler(BuildDeviationReader([], snapshots));
        var r = await h.Handle(new GetRuntimeTrafficContractDeviationReport.Query(TenantId), CancellationToken.None);

        r.Value.HistoricalDeviationTrend.Should()
            .Be(GetRuntimeTrafficContractDeviationReport.DeviationTrendDirection.Worsening);
    }

    // ────────────────────────────────────────────────────────────────────────
    // Validator
    // ────────────────────────────────────────────────────────────────────────

    [Fact]
    public void AZ1_Validator_RejectsEmptyTenantId()
    {
        var v = new GetRuntimeTrafficContractDeviationReport.Validator();
        v.Validate(new GetRuntimeTrafficContractDeviationReport.Query("")).IsValid.Should().BeFalse();
    }

    [Fact]
    public void AZ1_Validator_RejectsInvalidLookbackDays()
    {
        var v = new GetRuntimeTrafficContractDeviationReport.Validator();
        v.Validate(new GetRuntimeTrafficContractDeviationReport.Query("t", LookbackDays: 0)).IsValid.Should().BeFalse();
    }

    // ════════════════════════════════════════════════════════════════════════
    // AZ.2 — GetHighTrafficEndpointRiskReport
    // ════════════════════════════════════════════════════════════════════════

    private static GetHighTrafficEndpointRiskReport.Handler RiskHandler(
        IHighTrafficEndpointReader? reader = null) =>
        new(reader ?? Substitute.For<IHighTrafficEndpointReader>(), Clock());

    private static IHighTrafficEndpointReader BuildRiskReader(
        IReadOnlyList<IHighTrafficEndpointReader.EndpointTrafficEntry> entries)
    {
        var r = Substitute.For<IHighTrafficEndpointReader>();
        r.ListByTenantAsync(TenantId, Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
         .Returns(entries);
        return r;
    }

    private static IHighTrafficEndpointReader.EndpointTrafficEntry MakeEndpoint(
        string svcId = "svc-1",
        string svcName = "svc-1",
        string teamName = "team-1",
        string tier = "Internal",
        string path = "/api/v1/resource",
        string method = "GET",
        long callVolume = 10000,
        double rps = 150.0,
        double latencyP50 = 50.0,
        double latencyP95 = 200.0,
        double latencyP99 = 500.0,
        double errorRate = 1.0,
        bool isDocumented = true,
        bool isDeprecated = false,
        bool hasSlo = true,
        bool chaosTested = true,
        bool criticalTier = false) =>
        new(svcId, svcName, teamName, tier, path, method,
            callVolume, rps, latencyP50, latencyP95, latencyP99,
            errorRate, isDocumented, isDeprecated, hasSlo, chaosTested, criticalTier);

    // ────────────────────────────────────────────────────────────────────────
    // Empty report
    // ────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task AZ2_EmptyReport_WhenNoHighTrafficEndpoints()
    {
        // All endpoints below threshold
        var ep = MakeEndpoint(rps: 50.0); // below default 100 rps
        var h = RiskHandler(BuildRiskReader([ep]));
        var r = await h.Handle(
            new GetHighTrafficEndpointRiskReport.Query(TenantId, RpsThreshold: 100.0),
            CancellationToken.None);

        r.IsSuccess.Should().BeTrue();
        r.Value.TopEndpoints.Should().BeEmpty();
        r.Value.Summary.CriticalUncoveredEndpointCount.Should().Be(0);
    }

    // ────────────────────────────────────────────────────────────────────────
    // EndpointRiskScore
    // ────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task AZ2_RiskScore_Zero_WhenFullyCovered()
    {
        // Documented + Chaos tested + low error rate + P99 < threshold → score = 0
        var ep = MakeEndpoint(
            isDocumented: true, chaosTested: true,
            errorRate: 0.0, latencyP99: 500.0);

        var h = RiskHandler(BuildRiskReader([ep]));
        var r = await h.Handle(
            new GetHighTrafficEndpointRiskReport.Query(TenantId, RpsThreshold: 100.0, LatencyP99ThresholdMs: 1000.0),
            CancellationToken.None);

        r.Value.TopEndpoints[0].EndpointRiskScore.Should().Be(0m);
        r.Value.TopEndpoints[0].RiskTier.Should().Be(GetHighTrafficEndpointRiskReport.EndpointRiskTier.Safe);
    }

    [Fact]
    public async Task AZ2_RiskScore_Critical_WhenUndocumented_NoChaos_HighError_HighLatency()
    {
        // Undocumented (30) + No chaos (25) + 100% error → error contribution = min(100/20, 1)*25 = 25
        // + P99 > threshold (20) = 100
        var ep = MakeEndpoint(
            isDocumented: false, chaosTested: false,
            errorRate: 100.0, latencyP99: 5000.0);

        var h = RiskHandler(BuildRiskReader([ep]));
        var r = await h.Handle(
            new GetHighTrafficEndpointRiskReport.Query(TenantId, RpsThreshold: 100.0, LatencyP99ThresholdMs: 1000.0),
            CancellationToken.None);

        r.Value.TopEndpoints[0].EndpointRiskScore.Should().Be(100m);
        r.Value.TopEndpoints[0].RiskTier.Should().Be(GetHighTrafficEndpointRiskReport.EndpointRiskTier.Critical);
    }

    [Fact]
    public async Task AZ2_ContractCoverage_Deprecated_WhenDeprecatedInContract()
    {
        var ep = MakeEndpoint(isDocumented: false, isDeprecated: true);
        var h = RiskHandler(BuildRiskReader([ep]));
        var r = await h.Handle(
            new GetHighTrafficEndpointRiskReport.Query(TenantId, RpsThreshold: 100.0),
            CancellationToken.None);

        r.Value.TopEndpoints[0].ContractCoverage
            .Should().Be(GetHighTrafficEndpointRiskReport.EndpointContractCoverage.Deprecated);
    }

    // ────────────────────────────────────────────────────────────────────────
    // DocumentationOpportunity
    // ────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task AZ2_DocumentationOpportunity_IncludesUndocumentedAboveThreshold()
    {
        var docOpp = MakeEndpoint("s1", path: "/undoc", isDocumented: false, callVolume: 5000);
        var smallUndoc = MakeEndpoint("s2", path: "/undoc-small", isDocumented: false, callVolume: 50);
        var documented = MakeEndpoint("s3", path: "/doc", isDocumented: true, callVolume: 2000);

        var h = RiskHandler(BuildRiskReader([docOpp, smallUndoc, documented]));
        var r = await h.Handle(
            new GetHighTrafficEndpointRiskReport.Query(TenantId, RpsThreshold: 100.0, DocumentationPriorityThreshold: 1000),
            CancellationToken.None);

        r.Value.DocumentationOpportunity.Should().HaveCount(1);
        r.Value.DocumentationOpportunity[0].EndpointPath.Should().Be("/undoc");
    }

    // ────────────────────────────────────────────────────────────────────────
    // ChaosGap and SloGap
    // ────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task AZ2_ChaosGapByTrafficVolume_IncludesUntested()
    {
        var untested = MakeEndpoint("s1", path: "/untested", chaosTested: false, callVolume: 3000);
        var tested = MakeEndpoint("s2", path: "/tested", chaosTested: true, callVolume: 2000);

        var h = RiskHandler(BuildRiskReader([untested, tested]));
        var r = await h.Handle(
            new GetHighTrafficEndpointRiskReport.Query(TenantId, RpsThreshold: 100.0),
            CancellationToken.None);

        r.Value.ChaosGapByTrafficVolume.Should().HaveCount(1);
        r.Value.ChaosGapByTrafficVolume[0].EndpointPath.Should().Be("/untested");
    }

    [Fact]
    public async Task AZ2_SloGapForHighTraffic_IncludesNoSloEndpoints()
    {
        var noSlo = MakeEndpoint("s1", hasSlo: false);
        var hasSlo = MakeEndpoint("s2", path: "/with-slo", hasSlo: true);

        var h = RiskHandler(BuildRiskReader([noSlo, hasSlo]));
        var r = await h.Handle(
            new GetHighTrafficEndpointRiskReport.Query(TenantId, RpsThreshold: 100.0),
            CancellationToken.None);

        r.Value.SloGapForHighTraffic.Should().HaveCount(1);
    }

    // ────────────────────────────────────────────────────────────────────────
    // TenantEndpointRiskScore (weighted by volume)
    // ────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task AZ2_TenantRiskScore_VolumeWeighted()
    {
        // ep1: score=0, volume=8000 | ep2: score=100, volume=2000
        // Weighted = (0*8000 + 100*2000) / 10000 = 20
        var ep1 = MakeEndpoint("s1", callVolume: 8000, isDocumented: true, chaosTested: true, errorRate: 0);
        var ep2 = MakeEndpoint("s2", path: "/risky", callVolume: 2000,
            isDocumented: false, chaosTested: false, errorRate: 100, latencyP99: 5000);

        var h = RiskHandler(BuildRiskReader([ep1, ep2]));
        var r = await h.Handle(
            new GetHighTrafficEndpointRiskReport.Query(TenantId, RpsThreshold: 100.0, LatencyP99ThresholdMs: 1000.0),
            CancellationToken.None);

        r.Value.Summary.TenantEndpointRiskScore.Should().Be(20m);
    }

    // ────────────────────────────────────────────────────────────────────────
    // Validator
    // ────────────────────────────────────────────────────────────────────────

    [Fact]
    public void AZ2_Validator_RejectsEmptyTenantId()
    {
        var v = new GetHighTrafficEndpointRiskReport.Validator();
        v.Validate(new GetHighTrafficEndpointRiskReport.Query("")).IsValid.Should().BeFalse();
    }

    [Fact]
    public void AZ2_Validator_RejectsZeroRpsThreshold()
    {
        var v = new GetHighTrafficEndpointRiskReport.Validator();
        v.Validate(new GetHighTrafficEndpointRiskReport.Query("t", RpsThreshold: 0)).IsValid.Should().BeFalse();
    }
}
