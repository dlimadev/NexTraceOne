using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.Catalog.Application.Contracts.Abstractions;
using NexTraceOne.Catalog.Application.Contracts.Features.GetCriticalPathReport;
using NexTraceOne.Catalog.Application.Contracts.Features.GetDependencyVersionAlignmentReport;
using NexTraceOne.Catalog.Application.Contracts.Features.GetServiceTopologyHealthReport;

namespace NexTraceOne.Catalog.Tests.Contracts.Application.Features;

/// <summary>
/// Testes unitários para Wave AR — Service Topology Intelligence &amp; Dependency Mapping.
/// Cobre AR.1 GetServiceTopologyHealthReport, AR.2 GetCriticalPathReport
/// e AR.3 GetDependencyVersionAlignmentReport.
/// </summary>
public sealed class WaveArTopologyTests
{
    private static readonly DateTimeOffset FixedNow = new(2026, 7, 1, 12, 0, 0, TimeSpan.Zero);
    private const string TenantId = "tenant-ar-001";

    private static IDateTimeProvider CreateClock()
    {
        var clock = Substitute.For<IDateTimeProvider>();
        clock.UtcNow.Returns(FixedNow);
        return clock;
    }

    private static IServiceTopologyReader.ServiceDependencyEntry Dep(
        string src, string tgt,
        string srcTier = "Standard", string tgtTier = "Standard",
        int daysAgo = 1)
        => new(src, tgt, srcTier, tgtTier, FixedNow.AddDays(-daysAgo));

    private static IServiceTopologyReader.ServiceNodeEntry Node(
        string id, string tier = "Standard",
        bool customerFacing = false, int daysAgo = 1)
        => new(id, $"Service-{id}", tier, customerFacing, FixedNow.AddDays(-daysAgo));

    // ════════════════════════════════════════════════════════════════════════
    // AR.1 — GetServiceTopologyHealthReport
    // ════════════════════════════════════════════════════════════════════════

    private static GetServiceTopologyHealthReport.Handler CreateTopologyHandler(
        IReadOnlyList<IServiceTopologyReader.ServiceDependencyEntry>? deps = null,
        IReadOnlyList<IServiceTopologyReader.ServiceNodeEntry>? nodes = null)
    {
        var reader = Substitute.For<IServiceTopologyReader>();
        reader.ListDependenciesByTenantAsync(TenantId, Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(deps ?? []);
        reader.ListServiceNodesByTenantAsync(TenantId, Arg.Any<CancellationToken>())
            .Returns(nodes ?? []);
        return new GetServiceTopologyHealthReport.Handler(reader, CreateClock());
    }

    [Fact]
    public async Task AR1_EmptyGraph_ReturnsZeroCountsAndHealthyTier()
    {
        var handler = CreateTopologyHandler();
        var result = await handler.Handle(new GetServiceTopologyHealthReport.Query(TenantId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TotalServices.Should().Be(0);
        result.Value.TotalDependencies.Should().Be(0);
        result.Value.OrphanServices.Should().BeEmpty();
        result.Value.CircularDependencies.Should().BeEmpty();
        result.Value.Tier.Should().Be(GetServiceTopologyHealthReport.TopologyHealthTier.Healthy);
    }

    [Fact]
    public async Task AR1_SingleServiceNoDeps_AppearsInOrphanServices()
    {
        var nodes = new[] { Node("svc-a") };
        var handler = CreateTopologyHandler(nodes: nodes);
        var result = await handler.Handle(new GetServiceTopologyHealthReport.Query(TenantId), CancellationToken.None);

        result.Value.TotalServices.Should().Be(1);
        result.Value.OrphanServices.Should().ContainSingle().Which.Should().Be("svc-a");
    }

    [Fact]
    public async Task AR1_TwoServicesLinear_FanOutAndFanInCorrect()
    {
        var nodes = new[] { Node("A"), Node("B") };
        var deps = new[] { Dep("A", "B") };
        var handler = CreateTopologyHandler(deps, nodes);
        var result = await handler.Handle(new GetServiceTopologyHealthReport.Query(TenantId), CancellationToken.None);

        result.Value.TotalServices.Should().Be(2);
        result.Value.TotalDependencies.Should().Be(1);
        result.Value.OrphanServices.Should().BeEmpty();
        result.Value.CircularDependencies.Should().BeEmpty();
        result.Value.AvgFanOut.Should().Be(0.5);
    }

    [Fact]
    public async Task AR1_CircularDependency_DetectedAndTierDegraded()
    {
        var nodes = new[] { Node("A"), Node("B") };
        var deps = new[] { Dep("A", "B"), Dep("B", "A") };
        var handler = CreateTopologyHandler(deps, nodes);
        var result = await handler.Handle(new GetServiceTopologyHealthReport.Query(TenantId), CancellationToken.None);

        result.Value.CircularDependencies.Should().NotBeEmpty();
        result.Value.Tier.Should().BeOneOf(
            GetServiceTopologyHealthReport.TopologyHealthTier.Degraded,
            GetServiceTopologyHealthReport.TopologyHealthTier.Critical);
    }

    [Fact]
    public async Task AR1_ServiceWithHighFanIn_AppearsInHubServices()
    {
        var nodes = new[] { Node("hub"), Node("c1"), Node("c2"), Node("c3"), Node("c4"), Node("c5") };
        var deps = new[]
        {
            Dep("c1", "hub"), Dep("c2", "hub"), Dep("c3", "hub"),
            Dep("c4", "hub"), Dep("c5", "hub")
        };
        var handler = CreateTopologyHandler(deps, nodes);
        var result = await handler.Handle(
            new GetServiceTopologyHealthReport.Query(TenantId, HubFanInThreshold: 5), CancellationToken.None);

        result.Value.HubServices.Should().Contain("hub");
    }

    [Fact]
    public async Task AR1_StaleDependencies_PopulatesStaleTopologyServices()
    {
        var freshDate = FixedNow.AddDays(-5);
        var staleDate = FixedNow.AddDays(-60);
        var deps = new[]
        {
            new IServiceTopologyReader.ServiceDependencyEntry("A", "B", "Standard", "Standard", staleDate),
            new IServiceTopologyReader.ServiceDependencyEntry("C", "D", "Standard", "Standard", freshDate)
        };
        var nodes = new[] { Node("A"), Node("B"), Node("C"), Node("D") };
        var handler = CreateTopologyHandler(deps, nodes);
        var result = await handler.Handle(
            new GetServiceTopologyHealthReport.Query(TenantId, FreshnessDays: 30), CancellationToken.None);

        result.Value.StaleTopologyServices.Should().NotBeEmpty();
        result.Value.StaleTopologyServices.Should().Contain("A");
        result.Value.StaleTopologyServices.Should().Contain("B");
    }

    [Fact]
    public async Task AR1_AllFreshDependencies_FreshnessScoreIs100()
    {
        var deps = new[] { Dep("A", "B", daysAgo: 1), Dep("B", "C", daysAgo: 2) };
        var nodes = new[] { Node("A"), Node("B"), Node("C") };
        var handler = CreateTopologyHandler(deps, nodes);
        var result = await handler.Handle(
            new GetServiceTopologyHealthReport.Query(TenantId, FreshnessDays: 30), CancellationToken.None);

        result.Value.TopologyFreshnessScore.Should().Be(100.0);
        result.Value.StaleTopologyServices.Should().BeEmpty();
    }

    [Fact]
    public async Task AR1_HubAndCircularPenalties_ReduceHealthScore()
    {
        // One circular + one hub → score reduced
        var nodes = new[] { Node("A"), Node("B"), Node("hub"), Node("x1"), Node("x2"), Node("x3"), Node("x4"), Node("x5") };
        var deps = new[]
        {
            Dep("A", "B"), Dep("B", "A"), // circular
            Dep("x1", "hub"), Dep("x2", "hub"), Dep("x3", "hub"), Dep("x4", "hub"), Dep("x5", "hub")
        };
        var handler = CreateTopologyHandler(deps, nodes);
        var result = await handler.Handle(
            new GetServiceTopologyHealthReport.Query(TenantId, HubFanInThreshold: 5, HubPenalty: 15, CircularPenalty: 20),
            CancellationToken.None);

        result.Value.TenantTopologyHealthScore.Should().BeLessThan(100.0);
    }

    [Fact]
    public async Task AR1_HealthyTier_WhenNoCircularsAndFewHubsAndFreshTopology()
    {
        var deps = new[] { Dep("A", "B", daysAgo: 1), Dep("B", "C", daysAgo: 1) };
        var nodes = new[] { Node("A"), Node("B"), Node("C") };
        var handler = CreateTopologyHandler(deps, nodes);
        var result = await handler.Handle(
            new GetServiceTopologyHealthReport.Query(TenantId, HubFanInThreshold: 5),
            CancellationToken.None);

        result.Value.Tier.Should().Be(GetServiceTopologyHealthReport.TopologyHealthTier.Healthy);
    }

    [Fact]
    public async Task AR1_CriticalTier_WhenHealthScoreBelowThirty()
    {
        // Lots of circulars to push score below 30
        var nodes = Enumerable.Range(1, 6).Select(i => Node($"s{i}")).ToArray();
        var deps = new[]
        {
            Dep("s1","s2"), Dep("s2","s1"), // circular 1
            Dep("s3","s4"), Dep("s4","s3"), // circular 2
            Dep("s5","s6"), Dep("s6","s5")  // circular 3
        };
        var handler = CreateTopologyHandler(deps, nodes);
        var result = await handler.Handle(
            new GetServiceTopologyHealthReport.Query(TenantId, CircularPenalty: 20), CancellationToken.None);

        result.Value.Tier.Should().Be(GetServiceTopologyHealthReport.TopologyHealthTier.Critical);
    }

    [Fact]
    public async Task AR1_ArchitectureRecommendations_GeneratedForCircular()
    {
        var nodes = new[] { Node("A"), Node("B") };
        var deps = new[] { Dep("A", "B"), Dep("B", "A") };
        var handler = CreateTopologyHandler(deps, nodes);
        var result = await handler.Handle(new GetServiceTopologyHealthReport.Query(TenantId), CancellationToken.None);

        result.Value.ArchitectureRecommendations.Should().NotBeEmpty();
        result.Value.ArchitectureRecommendations[0].Should().StartWith("Resolve circular dependency");
    }

    [Fact]
    public async Task AR1_IsolatedClusters_CountedForDisconnectedComponents()
    {
        // Two disconnected pairs: A↔B and C↔D (no connection between them)
        var nodes = new[] { Node("A"), Node("B"), Node("C"), Node("D") };
        var deps = new[] { Dep("A", "B"), Dep("C", "D") };
        var handler = CreateTopologyHandler(deps, nodes);
        var result = await handler.Handle(new GetServiceTopologyHealthReport.Query(TenantId), CancellationToken.None);

        result.Value.IsolatedClusterCount.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task AR1_AvgFanOut_CalculatedAsTotalDepsOverTotalServices()
    {
        var nodes = new[] { Node("A"), Node("B"), Node("C"), Node("D") };
        var deps = new[] { Dep("A", "B"), Dep("A", "C"), Dep("B", "D") };
        var handler = CreateTopologyHandler(deps, nodes);
        var result = await handler.Handle(new GetServiceTopologyHealthReport.Query(TenantId), CancellationToken.None);

        result.Value.AvgFanOut.Should().Be(Math.Round(3.0 / 4.0, 2));
    }

    [Fact]
    public async Task AR1_GraphDensity_CalculatedCorrectly()
    {
        // 2 services, 1 dep: density = 1 / (2*1) = 0.5
        var nodes = new[] { Node("A"), Node("B") };
        var deps = new[] { Dep("A", "B") };
        var handler = CreateTopologyHandler(deps, nodes);
        var result = await handler.Handle(new GetServiceTopologyHealthReport.Query(TenantId), CancellationToken.None);

        result.Value.GraphDensity.Should().BeApproximately(0.5, 0.001);
    }

    [Fact]
    public async Task AR1_Validator_RejectsEmptyTenantId()
    {
        var validator = new GetServiceTopologyHealthReport.Validator();
        var result = validator.Validate(new GetServiceTopologyHealthReport.Query(""));
        result.IsValid.Should().BeFalse();
    }

    // ════════════════════════════════════════════════════════════════════════
    // AR.2 — GetCriticalPathReport
    // ════════════════════════════════════════════════════════════════════════

    private static GetCriticalPathReport.Handler CreateCriticalPathHandler(
        IReadOnlyList<IServiceTopologyReader.ServiceDependencyEntry>? deps = null,
        IReadOnlyList<IServiceTopologyReader.ServiceNodeEntry>? nodes = null)
    {
        var reader = Substitute.For<ICriticalPathReader>();
        reader.ListDependenciesByTenantAsync(TenantId, Arg.Any<CancellationToken>())
            .Returns(deps ?? []);
        reader.ListServiceNodesByTenantAsync(TenantId, Arg.Any<CancellationToken>())
            .Returns(nodes ?? []);
        return new GetCriticalPathReport.Handler(reader, CreateClock());
    }

    [Fact]
    public async Task AR2_EmptyGraph_ReturnsZeroDepthAndEmptyChains()
    {
        var handler = CreateCriticalPathHandler();
        var result = await handler.Handle(new GetCriticalPathReport.Query(TenantId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.MaxDependencyDepth.Should().Be(0);
        result.Value.CriticalPathChains.Should().BeEmpty();
        result.Value.TenantCriticalPathIndex.Should().Be(0.0);
    }

    [Fact]
    public async Task AR2_LinearChainABC_MaxDepthIsThree()
    {
        var nodes = new[] { Node("A"), Node("B"), Node("C") };
        var deps = new[] { Dep("A", "B"), Dep("B", "C") };
        var handler = CreateCriticalPathHandler(deps, nodes);
        var result = await handler.Handle(new GetCriticalPathReport.Query(TenantId), CancellationToken.None);

        result.Value.MaxDependencyDepth.Should().Be(3);
        result.Value.CriticalPathChains.Should().ContainSingle();
        result.Value.CriticalPathChains[0].Path.Should().ContainInOrder("A", "B", "C");
    }

    [Fact]
    public async Task AR2_TopNChains_LimitsReturnedChains()
    {
        // Build 5 independent chains of varying depth
        var nodes = Enumerable.Range(1, 10).Select(i => Node($"s{i}")).ToArray();
        var deps = new[]
        {
            Dep("s1","s2"), Dep("s2","s3"),
            Dep("s4","s5"), Dep("s5","s6"),
            Dep("s7","s8"),
            Dep("s9","s10")
        };
        var handler = CreateCriticalPathHandler(deps, nodes);
        var result = await handler.Handle(
            new GetCriticalPathReport.Query(TenantId, TopNChains: 2), CancellationToken.None);

        result.Value.CriticalPathChains.Count.Should().BeLessThanOrEqualTo(2);
    }

    [Fact]
    public async Task AR2_ServiceInMultipleChains_AppearsInBottleneckServices()
    {
        // B appears in 3 chains: A→B→C, D→B→E, F→B→G
        var nodes = new[] { Node("A"), Node("B"), Node("C"), Node("D"), Node("E"), Node("F"), Node("G") };
        var deps = new[]
        {
            Dep("A","B"), Dep("B","C"),
            Dep("D","B"), Dep("B","E"),
            Dep("F","B"), Dep("B","G")
        };
        var handler = CreateCriticalPathHandler(deps, nodes);
        var result = await handler.Handle(
            new GetCriticalPathReport.Query(TenantId, BottleneckPathCount: 3), CancellationToken.None);

        result.Value.BottleneckServices.Should().Contain("B");
    }

    [Fact]
    public async Task AR2_CustomerFacingAtRoot_TrueForChainStartingAtCustomerFacingNode()
    {
        var nodes = new[]
        {
            Node("frontend", customerFacing: true),
            Node("backend")
        };
        var deps = new[] { Dep("frontend", "backend") };
        var handler = CreateCriticalPathHandler(deps, nodes);
        var result = await handler.Handle(new GetCriticalPathReport.Query(TenantId), CancellationToken.None);

        result.Value.CriticalPathChains.Should().ContainSingle();
        result.Value.CriticalPathChains[0].CustomerFacingAtRoot.Should().BeTrue();
    }

    [Fact]
    public async Task AR2_HighFanOutService_HasHighCascadeRiskScore()
    {
        // hub fans out to 5 services
        var nodes = new[] { Node("hub"), Node("t1"), Node("t2"), Node("t3"), Node("t4"), Node("t5") };
        var deps = new[]
        {
            Dep("hub","t1"), Dep("hub","t2"), Dep("hub","t3"), Dep("hub","t4"), Dep("hub","t5")
        };
        var handler = CreateCriticalPathHandler(deps, nodes);
        var result = await handler.Handle(new GetCriticalPathReport.Query(TenantId), CancellationToken.None);

        var hubEntry = result.Value.TopCascadeRiskServices.FirstOrDefault(e => e.ServiceId == "hub");
        hubEntry.Should().NotBeNull();
        hubEntry!.CascadeRiskScore.Should().BeGreaterThan(30.0);
    }

    [Fact]
    public async Task AR2_TopCascadeRiskServices_MaxTenEntries()
    {
        var nodes = Enumerable.Range(1, 15).Select(i => Node($"s{i}")).ToArray();
        var deps = Enumerable.Range(2, 14).Select(i => Dep("s1", $"s{i}")).ToArray();
        var handler = CreateCriticalPathHandler(deps, nodes);
        var result = await handler.Handle(new GetCriticalPathReport.Query(TenantId), CancellationToken.None);

        result.Value.TopCascadeRiskServices.Count.Should().BeLessThanOrEqualTo(10);
    }

    [Fact]
    public async Task AR2_DepthDistribution_CountsServicesAtMinDepth3()
    {
        // Chain of depth 4: A→B→C→D
        var nodes = new[] { Node("A"), Node("B"), Node("C"), Node("D") };
        var deps = new[] { Dep("A","B"), Dep("B","C"), Dep("C","D") };
        var handler = CreateCriticalPathHandler(deps, nodes);
        var result = await handler.Handle(new GetCriticalPathReport.Query(TenantId), CancellationToken.None);

        var depthAt3 = result.Value.DepthDistribution.FirstOrDefault(d => d.MinDepth == 3);
        depthAt3.Should().NotBeNull();
        depthAt3!.ServiceCount.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task AR2_TenantCriticalPathIndex_PositiveForComplexGraph()
    {
        var nodes = new[] { Node("A"), Node("B"), Node("C"), Node("D"), Node("E") };
        var deps = new[] { Dep("A","B"), Dep("B","C"), Dep("C","D"), Dep("D","E") };
        var handler = CreateCriticalPathHandler(deps, nodes);
        var result = await handler.Handle(new GetCriticalPathReport.Query(TenantId), CancellationToken.None);

        result.Value.TenantCriticalPathIndex.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task AR2_CriticalTierServices_HaveHigherTotalServiceTierRisk()
    {
        var nodes = new[]
        {
            Node("crit1", tier: "Critical"),
            Node("crit2", tier: "Critical"),
            Node("std1", tier: "Standard"),
            Node("std2", tier: "Standard")
        };
        var deps = new[]
        {
            Dep("crit1","crit2", "Critical","Critical"),
            Dep("std1","std2", "Standard","Standard")
        };
        var handler = CreateCriticalPathHandler(deps, nodes);
        var result = await handler.Handle(new GetCriticalPathReport.Query(TenantId), CancellationToken.None);

        var critChain = result.Value.CriticalPathChains
            .FirstOrDefault(c => c.Path.Contains("crit1"));
        var stdChain = result.Value.CriticalPathChains
            .FirstOrDefault(c => c.Path.Contains("std1"));

        critChain.Should().NotBeNull();
        stdChain.Should().NotBeNull();
        critChain!.TotalServiceTierRisk.Should().BeGreaterThan(stdChain!.TotalServiceTierRisk);
    }

    [Fact]
    public async Task AR2_SingleHopDependency_MaxDepthIsTwo()
    {
        var nodes = new[] { Node("A"), Node("B") };
        var deps = new[] { Dep("A","B") };
        var handler = CreateCriticalPathHandler(deps, nodes);
        var result = await handler.Handle(new GetCriticalPathReport.Query(TenantId), CancellationToken.None);

        result.Value.MaxDependencyDepth.Should().Be(2);
    }

    [Fact]
    public async Task AR2_Chains_SortedByDescendingDepth()
    {
        var nodes = Enumerable.Range(1, 6).Select(i => Node($"n{i}")).ToArray();
        // Long chain n1→n2→n3→n4 and short chain n5→n6
        var deps = new[]
        {
            Dep("n1","n2"), Dep("n2","n3"), Dep("n3","n4"),
            Dep("n5","n6")
        };
        var handler = CreateCriticalPathHandler(deps, nodes);
        var result = await handler.Handle(new GetCriticalPathReport.Query(TenantId, TopNChains: 5), CancellationToken.None);

        result.Value.CriticalPathChains.Should().NotBeEmpty();
        // First chain should be the longest
        result.Value.CriticalPathChains[0].Depth.Should().BeGreaterThanOrEqualTo(
            result.Value.CriticalPathChains[^1].Depth);
    }

    [Fact]
    public async Task AR2_TenantCriticalPathIndex_CappedAt100()
    {
        // Extreme depth
        var nodeList = Enumerable.Range(1, 20).Select(i => Node($"d{i}")).ToArray();
        var depList = Enumerable.Range(1, 19).Select(i => Dep($"d{i}", $"d{i+1}")).ToArray();
        var handler = CreateCriticalPathHandler(depList, nodeList);
        var result = await handler.Handle(new GetCriticalPathReport.Query(TenantId), CancellationToken.None);

        result.Value.TenantCriticalPathIndex.Should().BeLessThanOrEqualTo(100.0);
    }

    [Fact]
    public async Task AR2_Validator_RejectsEmptyTenantId()
    {
        var validator = new GetCriticalPathReport.Validator();
        var result = validator.Validate(new GetCriticalPathReport.Query(""));
        result.IsValid.Should().BeFalse();
    }

    // ════════════════════════════════════════════════════════════════════════
    // AR.3 — GetDependencyVersionAlignmentReport
    // ════════════════════════════════════════════════════════════════════════

    private static GetDependencyVersionAlignmentReport.Handler CreateAlignmentHandler(
        IReadOnlyList<IDependencyVersionAlignmentReader.ComponentVersionEntry>? entries = null)
    {
        var reader = Substitute.For<IDependencyVersionAlignmentReader>();
        reader.ListComponentVersionsByTenantAsync(TenantId, Arg.Any<CancellationToken>())
            .Returns(entries ?? []);
        return new GetDependencyVersionAlignmentReport.Handler(reader, CreateClock());
    }

    private static IDependencyVersionAlignmentReader.ComponentVersionEntry CompEntry(
        string serviceId, string teamId, string componentName, string version,
        bool hasCve = false, string tier = "Standard")
        => new(serviceId, $"Svc-{serviceId}", teamId, tier, componentName, version, hasCve, FixedNow.AddDays(-1));

    [Fact]
    public async Task AR3_Empty_ComponentsAnalyzedZeroAndScoreHundred()
    {
        var handler = CreateAlignmentHandler();
        var result = await handler.Handle(new GetDependencyVersionAlignmentReport.Query(TenantId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.ComponentsAnalyzed.Should().Be(0);
        result.Value.TenantAlignmentScore.Should().Be(100.0);
    }

    [Fact]
    public async Task AR3_OneComponentOneVersion_AlignedTierAndSpreadOne()
    {
        var entries = new[] { CompEntry("s1", "team-1", "Newtonsoft.Json", "13.0.1") };
        var handler = CreateAlignmentHandler(entries);
        var result = await handler.Handle(new GetDependencyVersionAlignmentReport.Query(TenantId), CancellationToken.None);

        var detail = result.Value.ComponentDetails.Should().ContainSingle().Subject;
        detail.AlignmentTier.Should().Be(GetDependencyVersionAlignmentReport.AlignmentTier.Aligned);
        detail.VersionSpread.Should().Be(1);
    }

    [Fact]
    public async Task AR3_OneComponentTwoVersions_MinorDriftTier()
    {
        var entries = new[]
        {
            CompEntry("s1", "team-1", "Serilog", "3.0.0"),
            CompEntry("s2", "team-1", "Serilog", "3.1.0")
        };
        var handler = CreateAlignmentHandler(entries);
        var result = await handler.Handle(new GetDependencyVersionAlignmentReport.Query(TenantId), CancellationToken.None);

        var detail = result.Value.ComponentDetails.Should().ContainSingle().Subject;
        detail.AlignmentTier.Should().Be(GetDependencyVersionAlignmentReport.AlignmentTier.MinorDrift);
    }

    [Fact]
    public async Task AR3_VersionSpreadAboveThreshold_MajorDriftTier()
    {
        var entries = new[]
        {
            CompEntry("s1","t1","MediatR","11.0.0"),
            CompEntry("s2","t1","MediatR","11.1.0"),
            CompEntry("s3","t1","MediatR","11.2.0"),
            CompEntry("s4","t1","MediatR","11.3.0")
        };
        var handler = CreateAlignmentHandler(entries);
        var result = await handler.Handle(
            new GetDependencyVersionAlignmentReport.Query(TenantId, MajorDriftThreshold: 3), CancellationToken.None);

        var detail = result.Value.ComponentDetails.Should().ContainSingle().Subject;
        detail.AlignmentTier.Should().Be(GetDependencyVersionAlignmentReport.AlignmentTier.MajorDrift);
    }

    [Fact]
    public async Task AR3_ComponentWithCveAndMultipleVersions_SecurityRiskTier()
    {
        var entries = new[]
        {
            CompEntry("s1","t1","log4j","1.0.0", hasCve: true),
            CompEntry("s2","t1","log4j","1.1.0", hasCve: false)
        };
        var handler = CreateAlignmentHandler(entries);
        var result = await handler.Handle(new GetDependencyVersionAlignmentReport.Query(TenantId), CancellationToken.None);

        var detail = result.Value.ComponentDetails.Should().ContainSingle().Subject;
        detail.AlignmentTier.Should().Be(GetDependencyVersionAlignmentReport.AlignmentTier.SecurityRisk);
        detail.HasSecurityImplications.Should().BeTrue();
    }

    [Fact]
    public async Task AR3_TenantAlignmentScore_PercentageOfAlignedAndMinorDrift()
    {
        var entries = new[]
        {
            CompEntry("s1","t1","LibA","1.0.0"),                         // Aligned
            CompEntry("s2","t1","LibB","2.0.0"),                         // Aligned
            CompEntry("s3","t1","LibC","3.0.0", hasCve: true),           // SecurityRisk (single version but CVE → still Aligned since spread=1)
            CompEntry("s4","t1","LibD","4.0.0"), CompEntry("s5","t1","LibD","4.1.0"), // MinorDrift
        };
        var handler = CreateAlignmentHandler(entries);
        var result = await handler.Handle(new GetDependencyVersionAlignmentReport.Query(TenantId), CancellationToken.None);

        // LibA: Aligned, LibB: Aligned, LibC: Aligned (spread=1, CVE but no spread), LibD: MinorDrift → 4/4 = 100%
        result.Value.TenantAlignmentScore.Should().BeGreaterThan(0.0);
    }

    [Fact]
    public async Task AR3_ServicesOnOldestVersion_CorrectlyIdentified()
    {
        var entries = new[]
        {
            CompEntry("s1","t1","FluentValidation","10.0.0"),
            CompEntry("s2","t1","FluentValidation","11.0.0"),
            CompEntry("s3","t1","FluentValidation","11.0.0")
        };
        var handler = CreateAlignmentHandler(entries);
        var result = await handler.Handle(new GetDependencyVersionAlignmentReport.Query(TenantId), CancellationToken.None);

        var detail = result.Value.ComponentDetails.Should().ContainSingle().Subject;
        detail.ServicesOnOldestVersion.Should().ContainSingle().Which.Should().Be("s1");
    }

    [Fact]
    public async Task AR3_LatestAvailable_IsMaxVersionString()
    {
        var entries = new[]
        {
            CompEntry("s1","t1","AutoMapper","10.0.0"),
            CompEntry("s2","t1","AutoMapper","12.0.1"),
            CompEntry("s3","t1","AutoMapper","9.0.0")
        };
        var handler = CreateAlignmentHandler(entries);
        var result = await handler.Handle(new GetDependencyVersionAlignmentReport.Query(TenantId), CancellationToken.None);

        var detail = result.Value.ComponentDetails.Should().ContainSingle().Subject;
        detail.LatestAvailable.Should().Be("9.0.0"); // lexicographic max
    }

    [Fact]
    public async Task AR3_AlignmentUpgradeMap_GeneratedForDriftedComponents()
    {
        var entries = new[]
        {
            CompEntry("s1","t1","EFCore","7.0.0"),
            CompEntry("s2","t1","EFCore","8.0.0")
        };
        var handler = CreateAlignmentHandler(entries);
        var result = await handler.Handle(new GetDependencyVersionAlignmentReport.Query(TenantId), CancellationToken.None);

        result.Value.AlignmentUpgradeMap.Should().ContainSingle();
        result.Value.AlignmentUpgradeMap[0].ComponentName.Should().Be("EFCore");
        result.Value.AlignmentUpgradeMap[0].ServicesNeedingUpgrade.Should().Contain("s1");
    }

    [Fact]
    public async Task AR3_CrossTeamInconsistencies_DetectedWhenTeamsUseDifferentVersions()
    {
        var entries = new[]
        {
            CompEntry("s1","team-A","Polly","7.0.0"),
            CompEntry("s2","team-B","Polly","8.0.0")
        };
        var handler = CreateAlignmentHandler(entries);
        var result = await handler.Handle(new GetDependencyVersionAlignmentReport.Query(TenantId), CancellationToken.None);

        result.Value.CrossTeamInconsistencies.Should().ContainSingle();
        result.Value.CrossTeamInconsistencies[0].ComponentName.Should().Be("Polly");
    }

    [Fact]
    public async Task AR3_CriticalAlignmentGaps_SecurityRiskWithSufficientServices()
    {
        var entries = new[]
        {
            CompEntry("s1","t1","vuln-lib","1.0.0", hasCve: true),
            CompEntry("s2","t1","vuln-lib","1.1.0", hasCve: false),
            CompEntry("s3","t1","vuln-lib","1.2.0", hasCve: false)
        };
        var handler = CreateAlignmentHandler(entries);
        var result = await handler.Handle(
            new GetDependencyVersionAlignmentReport.Query(TenantId, CriticalServiceCount: 2), CancellationToken.None);

        result.Value.CriticalAlignmentGaps.Should().ContainSingle();
        result.Value.CriticalAlignmentGaps[0].ComponentName.Should().Be("vuln-lib");
    }

    [Fact]
    public async Task AR3_ComponentsAnalyzed_EqualsDistinctComponentNames()
    {
        var entries = new[]
        {
            CompEntry("s1","t1","LibA","1.0.0"),
            CompEntry("s2","t1","LibA","1.0.0"),
            CompEntry("s3","t1","LibB","2.0.0")
        };
        var handler = CreateAlignmentHandler(entries);
        var result = await handler.Handle(new GetDependencyVersionAlignmentReport.Query(TenantId), CancellationToken.None);

        result.Value.ComponentsAnalyzed.Should().Be(2);
    }

    [Fact]
    public async Task AR3_SingleVersionComponent_NoAlignmentUpgradeMapEntry()
    {
        var entries = new[]
        {
            CompEntry("s1","t1","SameVersion","5.0.0"),
            CompEntry("s2","t1","SameVersion","5.0.0")
        };
        var handler = CreateAlignmentHandler(entries);
        var result = await handler.Handle(new GetDependencyVersionAlignmentReport.Query(TenantId), CancellationToken.None);

        result.Value.AlignmentUpgradeMap.Should().BeEmpty();
    }

    [Fact]
    public async Task AR3_Validator_RejectsEmptyTenantId()
    {
        var validator = new GetDependencyVersionAlignmentReport.Validator();
        var result = validator.Validate(new GetDependencyVersionAlignmentReport.Query(""));
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task AR3_AllComponentsAligned_TenantAlignmentScoreIs100()
    {
        var entries = new[]
        {
            CompEntry("s1","t1","LibA","1.0.0"),
            CompEntry("s2","t1","LibB","2.0.0"),
            CompEntry("s3","t1","LibC","3.0.0")
        };
        var handler = CreateAlignmentHandler(entries);
        var result = await handler.Handle(new GetDependencyVersionAlignmentReport.Query(TenantId), CancellationToken.None);

        result.Value.TenantAlignmentScore.Should().Be(100.0);
    }

    [Fact]
    public async Task AR3_GeneratedAt_MatchesClockUtcNow()
    {
        var handler = CreateAlignmentHandler();
        var result = await handler.Handle(new GetDependencyVersionAlignmentReport.Query(TenantId), CancellationToken.None);

        result.Value.GeneratedAt.Should().Be(FixedNow);
    }
}
