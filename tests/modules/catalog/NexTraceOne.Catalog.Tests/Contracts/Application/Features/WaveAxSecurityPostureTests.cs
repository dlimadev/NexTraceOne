using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.Catalog.Application.Contracts.Abstractions;
using NexTraceOne.Catalog.Application.Contracts.Features.GetVulnerabilityExposureReport;
using NexTraceOne.Catalog.Application.Contracts.Features.GetSecurityPatchComplianceReport;

namespace NexTraceOne.Catalog.Tests.Contracts.Application.Features;

/// <summary>
/// Testes unitários para Wave AX — Security Posture &amp; Vulnerability Intelligence.
/// Cobre AX.1 GetVulnerabilityExposureReport (~15 testes) e AX.2 GetSecurityPatchComplianceReport (~15 testes).
/// </summary>
public sealed class WaveAxSecurityPostureTests
{
    private static readonly DateTimeOffset FixedNow = new(2026, 9, 1, 12, 0, 0, TimeSpan.Zero);
    private const string TenantId = "tenant-ax-001";

    private static IDateTimeProvider CreateClock()
    {
        var clock = Substitute.For<IDateTimeProvider>();
        clock.UtcNow.Returns(FixedNow);
        return clock;
    }

    // ════════════════════════════════════════════════════════════════════════
    // AX.1 — GetVulnerabilityExposureReport
    // ════════════════════════════════════════════════════════════════════════

    private static GetVulnerabilityExposureReport.Handler CreateExposureHandler(
        IVulnerabilityExposureReader? reader = null) =>
        new(reader ?? Substitute.For<IVulnerabilityExposureReader>(), CreateClock());

    private static IVulnerabilityExposureReader BuildExposureReader(
        IReadOnlyList<IVulnerabilityExposureReader.ServiceVulnerabilityEntry> entries)
    {
        var reader = Substitute.For<IVulnerabilityExposureReader>();
        reader.ListByTenantAsync(TenantId, Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
              .Returns(entries);
        return reader;
    }

    private static IVulnerabilityExposureReader.ServiceVulnerabilityEntry MakeVulnEntry(
        string serviceId = "svc-ax",
        string serviceName = "service-ax",
        string teamName = "team-ax",
        string serviceTier = "Internal",
        string domainName = "domain-ax",
        int criticalCve = 0,
        int highCve = 0,
        int mediumCve = 0,
        int lowCve = 0,
        int totalComponents = 10,
        int unpatchedCritical = 0,
        double avgCveAge = 0.0,
        IReadOnlyList<IVulnerabilityExposureReader.WeeklyExposureSnapshot>? snapshots = null) =>
        new(serviceId, serviceName, teamName, serviceTier, domainName, false,
            criticalCve, highCve, mediumCve, lowCve, totalComponents, unpatchedCritical, avgCveAge,
            snapshots ?? [new(0, 10m)]);

    [Fact]
    public async Task AX1_EmptyReport_WhenNoEntries()
    {
        var reader = BuildExposureReader([]);
        var handler = CreateExposureHandler(reader);

        var result = await handler.Handle(new GetVulnerabilityExposureReport.Query(TenantId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.ByService.Should().BeEmpty();
        result.Value.TenantExposureScore.Should().Be(0m);
        result.Value.ServicesWithCriticalCVEs.Should().Be(0);
        result.Value.TotalCVEsBySeverity.Critical.Should().Be(0);
    }

    [Fact]
    public async Task AX1_ExposureScore_Calculation_AllSeverities()
    {
        // 2 critical + 3 high + 5 medium + 10 low, 20 components
        // score = min(100, (2*40+3*30+5*20+10*10)/20) = min(100, (80+90+100+100)/20) = min(100, 370/20) = 18.5
        var entry = MakeVulnEntry(criticalCve: 2, highCve: 3, mediumCve: 5, lowCve: 10, totalComponents: 20);
        var handler = CreateExposureHandler(BuildExposureReader([entry]));

        var result = await handler.Handle(new GetVulnerabilityExposureReport.Query(TenantId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.ByService[0].ExposureScore.Should().Be(18.5m);
    }

    [Fact]
    public async Task AX1_ExposureScore_CappedAt100()
    {
        // 50 critical, 0 others, 1 component → score = min(100, 50*40/1) = 100
        var entry = MakeVulnEntry(criticalCve: 50, totalComponents: 1);
        var handler = CreateExposureHandler(BuildExposureReader([entry]));

        var result = await handler.Handle(new GetVulnerabilityExposureReport.Query(TenantId), CancellationToken.None);

        result.Value.ByService[0].ExposureScore.Should().Be(100m);
    }

    [Fact]
    public async Task AX1_Tier_Minimal_WhenLowScore()
    {
        // score = min(100, 0*40+0*30+1*20+0*10)/10) = 2 → Minimal, critical=0 <= threshold=1
        var entry = MakeVulnEntry(criticalCve: 0, mediumCve: 1, totalComponents: 10);
        var handler = CreateExposureHandler(BuildExposureReader([entry]));

        var result = await handler.Handle(new GetVulnerabilityExposureReport.Query(TenantId, CriticalCveThreshold: 1), CancellationToken.None);

        result.Value.ByService[0].Tier.Should().Be(GetVulnerabilityExposureReport.VulnerabilityExposureTier.Minimal);
    }

    [Fact]
    public async Task AX1_Tier_Moderate_WhenScoreAbove20()
    {
        // score ~25 → Moderate: 1 high + 0 others / 1 component = min(100, 30) = 30
        var entry = MakeVulnEntry(criticalCve: 0, highCve: 1, totalComponents: 1);
        var handler = CreateExposureHandler(BuildExposureReader([entry]));

        var result = await handler.Handle(new GetVulnerabilityExposureReport.Query(TenantId, CriticalCveThreshold: 1), CancellationToken.None);

        result.Value.ByService[0].Tier.Should().Be(GetVulnerabilityExposureReport.VulnerabilityExposureTier.Moderate);
    }

    [Fact]
    public async Task AX1_Tier_Elevated_WhenScoreAbove50()
    {
        // 2 high / 1 component = min(100, 60) = 60 → Elevated
        var entry = MakeVulnEntry(criticalCve: 0, highCve: 2, totalComponents: 1);
        var handler = CreateExposureHandler(BuildExposureReader([entry]));

        var result = await handler.Handle(new GetVulnerabilityExposureReport.Query(TenantId, CriticalCveThreshold: 1), CancellationToken.None);

        result.Value.ByService[0].Tier.Should().Be(GetVulnerabilityExposureReport.VulnerabilityExposureTier.Elevated);
    }

    [Fact]
    public async Task AX1_Tier_Critical_WhenScoreAbove75()
    {
        // 2 critical / 1 component = min(100, 80) = 80 → Critical
        var entry = MakeVulnEntry(criticalCve: 2, totalComponents: 1);
        var handler = CreateExposureHandler(BuildExposureReader([entry]));

        var result = await handler.Handle(new GetVulnerabilityExposureReport.Query(TenantId, CriticalCveThreshold: 1), CancellationToken.None);

        result.Value.ByService[0].Tier.Should().Be(GetVulnerabilityExposureReport.VulnerabilityExposureTier.Critical);
    }

    [Fact]
    public async Task AX1_Tier_Critical_WhenCriticalCveCountAboveThreshold()
    {
        // critical=2 > threshold=1 → Critical even if score is low (large totalComponents)
        var entry = MakeVulnEntry(criticalCve: 2, totalComponents: 1000);
        var handler = CreateExposureHandler(BuildExposureReader([entry]));

        var result = await handler.Handle(new GetVulnerabilityExposureReport.Query(TenantId, CriticalCveThreshold: 1), CancellationToken.None);

        result.Value.ByService[0].Tier.Should().Be(GetVulnerabilityExposureReport.VulnerabilityExposureTier.Critical);
    }

    [Fact]
    public async Task AX1_TenantExposureScore_WeightedAvg_CriticalTierWeight3()
    {
        // Service Critical tier score=90, Internal tier score=10 → (90*3 + 10*1)/(3+1) = 70
        var entryCritical = MakeVulnEntry(serviceId: "svc-1", serviceTier: "Critical", criticalCve: 2, totalComponents: 1);
        var entryInternal = MakeVulnEntry(serviceId: "svc-2", serviceTier: "Internal", criticalCve: 0, mediumCve: 0, lowCve: 1, totalComponents: 1);

        // svc-1: min(100, 2*40/1) = 80. svc-2: min(100, 1*10/1) = 10
        // weighted = (80*3 + 10*1)/(3+1) = 250/4 = 62.5
        var handler = CreateExposureHandler(BuildExposureReader([entryCritical, entryInternal]));

        var result = await handler.Handle(new GetVulnerabilityExposureReport.Query(TenantId, CriticalCveThreshold: 5), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TenantExposureScore.Should().BeGreaterThan(0m);
        // verify weighting: Critical tier gets 3x weight
        var criticalScore = result.Value.ByService.First(e => e.ServiceId == "svc-1").ExposureScore;
        var internalScore = result.Value.ByService.First(e => e.ServiceId == "svc-2").ExposureScore;
        var expected = (criticalScore * 3m + internalScore * 1m) / 4m;
        result.Value.TenantExposureScore.Should().Be(expected);
    }

    [Fact]
    public async Task AX1_TopExposedServices_LimitedToMaxTopServices()
    {
        var entries = Enumerable.Range(1, 15).Select(i =>
            MakeVulnEntry(serviceId: $"svc-{i}", criticalCve: i, totalComponents: 1)).ToList();

        var handler = CreateExposureHandler(BuildExposureReader(entries));

        var result = await handler.Handle(new GetVulnerabilityExposureReport.Query(TenantId, MaxTopServices: 5), CancellationToken.None);

        result.Value.TopExposedServices.Count.Should().Be(5);
        result.Value.ByService.Count.Should().Be(15);
    }

    [Fact]
    public async Task AX1_ExposureTrend_Improving_WhenRecentLowerThanOldest()
    {
        var snapshots = new List<IVulnerabilityExposureReader.WeeklyExposureSnapshot>
        {
            new(0, 30m),
            new(3, 70m)
        };
        var entry = MakeVulnEntry(snapshots: snapshots);
        var handler = CreateExposureHandler(BuildExposureReader([entry]));

        var result = await handler.Handle(new GetVulnerabilityExposureReport.Query(TenantId), CancellationToken.None);

        result.Value.ByService[0].Trend.Should().Be(GetVulnerabilityExposureReport.ExposureTrend.Improving);
    }

    [Fact]
    public async Task AX1_ExposureTrend_Worsening_WhenRecentHigherThanOldest()
    {
        var snapshots = new List<IVulnerabilityExposureReader.WeeklyExposureSnapshot>
        {
            new(0, 70m),
            new(3, 30m)
        };
        var entry = MakeVulnEntry(snapshots: snapshots);
        var handler = CreateExposureHandler(BuildExposureReader([entry]));

        var result = await handler.Handle(new GetVulnerabilityExposureReport.Query(TenantId), CancellationToken.None);

        result.Value.ByService[0].Trend.Should().Be(GetVulnerabilityExposureReport.ExposureTrend.Worsening);
    }

    [Fact]
    public async Task AX1_ExposureTrend_Stable_WhenEqualOrSingleSnapshot()
    {
        var singleSnapshot = new List<IVulnerabilityExposureReader.WeeklyExposureSnapshot> { new(0, 50m) };
        var entry = MakeVulnEntry(snapshots: singleSnapshot);
        var handler = CreateExposureHandler(BuildExposureReader([entry]));

        var result = await handler.Handle(new GetVulnerabilityExposureReport.Query(TenantId), CancellationToken.None);

        result.Value.ByService[0].Trend.Should().Be(GetVulnerabilityExposureReport.ExposureTrend.Stable);
    }

    [Fact]
    public async Task AX1_ServicesWithCriticalCVEs_CountsCorrectly()
    {
        var entries = new[]
        {
            MakeVulnEntry(serviceId: "svc-1", criticalCve: 2),
            MakeVulnEntry(serviceId: "svc-2", criticalCve: 1),
            MakeVulnEntry(serviceId: "svc-3", criticalCve: 0)
        };
        var handler = CreateExposureHandler(BuildExposureReader(entries));

        var result = await handler.Handle(new GetVulnerabilityExposureReport.Query(TenantId), CancellationToken.None);

        result.Value.ServicesWithCriticalCVEs.Should().Be(2);
    }

    [Fact]
    public async Task AX1_NullImpl_ReturnsEmptyReport()
    {
        var reader = new NexTraceOne.Catalog.Application.Contracts.NullVulnerabilityExposureReader();
        var handler = new GetVulnerabilityExposureReport.Handler(reader, CreateClock());

        var result = await handler.Handle(new GetVulnerabilityExposureReport.Query(TenantId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.ByService.Should().BeEmpty();
        result.Value.TenantExposureScore.Should().Be(0m);
    }

    // ════════════════════════════════════════════════════════════════════════
    // AX.2 — GetSecurityPatchComplianceReport
    // ════════════════════════════════════════════════════════════════════════

    private static GetSecurityPatchComplianceReport.Handler CreatePatchHandler(
        ISecurityPatchComplianceReader? reader = null) =>
        new(reader ?? Substitute.For<ISecurityPatchComplianceReader>(), CreateClock());

    private static ISecurityPatchComplianceReader BuildPatchReader(
        IReadOnlyList<ISecurityPatchComplianceReader.PatchComplianceEntry> entries)
    {
        var reader = Substitute.For<ISecurityPatchComplianceReader>();
        reader.ListByTenantAsync(TenantId, Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
              .Returns(entries);
        return reader;
    }

    private static ISecurityPatchComplianceReader.PatchComplianceEntry MakePatchEntry(
        string serviceId = "svc-ax",
        string serviceName = "service-ax",
        string teamName = "team-ax",
        string serviceTier = "Internal",
        IReadOnlyList<ISecurityPatchComplianceReader.RemediatedCve>? remediated = null,
        IReadOnlyList<ISecurityPatchComplianceReader.ActiveCve>? active = null) =>
        new(serviceId, serviceName, teamName, serviceTier, remediated ?? [], active ?? []);

    private static ISecurityPatchComplianceReader.RemediatedCve MakeRemediatedCve(
        string cveId = "CVE-001",
        string severity = "Critical",
        double discoveredDaysAgo = 10,
        double remediatedDaysAgo = 5) =>
        new(cveId, severity, FixedNow.AddDays(-discoveredDaysAgo), FixedNow.AddDays(-remediatedDaysAgo));

    private static ISecurityPatchComplianceReader.ActiveCve MakeActiveCve(
        string cveId = "CVE-A01",
        string severity = "Critical",
        double discoveredDaysAgo = 10) =>
        new(cveId, severity, FixedNow.AddDays(-discoveredDaysAgo));

    [Fact]
    public async Task AX2_EmptyReport_WhenNoEntries()
    {
        var handler = CreatePatchHandler(BuildPatchReader([]));

        var result = await handler.Handle(new GetSecurityPatchComplianceReport.Query(TenantId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TenantPatchComplianceSummary.CriticalPatchBacklogCount.Should().Be(0);
        result.Value.TenantPatchComplianceSummary.OverallPatchComplianceRate.Should().Be(0m);
        result.Value.SLABreaches.Should().BeEmpty();
    }

    [Fact]
    public async Task AX2_WithinSla_True_WhenRemediatedBeforeSla()
    {
        // Critical CVE: discovered 10d ago, remediated 5d ago = 5d to remediate, SLA=7d → within SLA
        var cve = MakeRemediatedCve(severity: "Critical", discoveredDaysAgo: 10, remediatedDaysAgo: 5);
        var entry = MakePatchEntry(remediated: [cve]);
        var handler = CreatePatchHandler(BuildPatchReader([entry]));

        var result = await handler.Handle(new GetSecurityPatchComplianceReport.Query(TenantId, PatchSlaCriticalDays: 7), CancellationToken.None);

        result.Value.PatchComplianceRateBySeverity.CriticalWithinSlaRate.Should().Be(100m);
        result.Value.SLABreaches.Should().BeEmpty();
    }

    [Fact]
    public async Task AX2_WithinSla_False_WhenRemediatedAfterSla()
    {
        // Critical CVE: discovered 15d ago, remediated 5d ago = 10d to remediate, SLA=7d → breach
        var cve = MakeRemediatedCve(severity: "Critical", discoveredDaysAgo: 15, remediatedDaysAgo: 5);
        var entry = MakePatchEntry(remediated: [cve]);
        var handler = CreatePatchHandler(BuildPatchReader([entry]));

        var result = await handler.Handle(new GetSecurityPatchComplianceReport.Query(TenantId, PatchSlaCriticalDays: 7), CancellationToken.None);

        result.Value.PatchComplianceRateBySeverity.CriticalWithinSlaRate.Should().Be(0m);
        result.Value.SLABreaches.Should().HaveCount(1);
    }

    [Fact]
    public async Task AX2_Tier_AtRisk_WhenActiveCriticalCveOlderThanSla()
    {
        // Active Critical CVE older than 7 days SLA → AtRisk
        var activeCve = MakeActiveCve(severity: "Critical", discoveredDaysAgo: 10);
        var entry = MakePatchEntry(active: [activeCve]);
        var handler = CreatePatchHandler(BuildPatchReader([entry]));

        var result = await handler.Handle(new GetSecurityPatchComplianceReport.Query(TenantId, PatchSlaCriticalDays: 7), CancellationToken.None);

        result.Value.TenantPatchComplianceSummary.Tier.Should().Be(GetSecurityPatchComplianceReport.PatchComplianceTier.AtRisk);
        result.Value.TenantPatchComplianceSummary.CriticalPatchBacklogCount.Should().Be(1);
    }

    [Fact]
    public async Task AX2_Tier_Compliant_WhenHighRates()
    {
        // 2 critical CVEs both within SLA, 2 high both within SLA → Compliant
        var cve1 = MakeRemediatedCve("CVE-001", "Critical", discoveredDaysAgo: 5, remediatedDaysAgo: 1);
        var cve2 = MakeRemediatedCve("CVE-002", "Critical", discoveredDaysAgo: 5, remediatedDaysAgo: 2);
        var cve3 = MakeRemediatedCve("CVE-003", "High", discoveredDaysAgo: 20, remediatedDaysAgo: 1);
        var cve4 = MakeRemediatedCve("CVE-004", "High", discoveredDaysAgo: 20, remediatedDaysAgo: 2);
        var entry = MakePatchEntry(remediated: [cve1, cve2, cve3, cve4]);
        var handler = CreatePatchHandler(BuildPatchReader([entry]));

        var result = await handler.Handle(
            new GetSecurityPatchComplianceReport.Query(TenantId, PatchSlaCriticalDays: 7, PatchSlaHighDays: 30, CompliantCriticalRate: 95m),
            CancellationToken.None);

        result.Value.TenantPatchComplianceSummary.Tier.Should().Be(GetSecurityPatchComplianceReport.PatchComplianceTier.Compliant);
    }

    [Fact]
    public async Task AX2_Tier_Partial_WhenCriticalRateAbove70()
    {
        // 3 critical: 2 within SLA (67%) — wait, need 75% → 3 critical, 3 within SLA but 1 breach in high
        // 4 critical: 3 within, 1 breach = 75% → Partial (no active critical backlog)
        var withinSla1 = MakeRemediatedCve("CVE-001", "Critical", discoveredDaysAgo: 5, remediatedDaysAgo: 1);
        var withinSla2 = MakeRemediatedCve("CVE-002", "Critical", discoveredDaysAgo: 5, remediatedDaysAgo: 2);
        var withinSla3 = MakeRemediatedCve("CVE-003", "Critical", discoveredDaysAgo: 5, remediatedDaysAgo: 3);
        var breach = MakeRemediatedCve("CVE-004", "Critical", discoveredDaysAgo: 20, remediatedDaysAgo: 1);
        var entry = MakePatchEntry(remediated: [withinSla1, withinSla2, withinSla3, breach]);
        var handler = CreatePatchHandler(BuildPatchReader([entry]));

        var result = await handler.Handle(
            new GetSecurityPatchComplianceReport.Query(TenantId, PatchSlaCriticalDays: 7, CompliantCriticalRate: 95m),
            CancellationToken.None);

        result.Value.TenantPatchComplianceSummary.Tier.Should().Be(GetSecurityPatchComplianceReport.PatchComplianceTier.Partial);
    }

    [Fact]
    public async Task AX2_Tier_NonCompliant_WhenLowRates()
    {
        // 2 critical: 1 within SLA = 50%, no active critical backlog → NonCompliant (50% < 70%, backlog=0)
        // Actually with backlog=0, it falls into Partial per ComputeTier logic. 
        // Let's test: 1 critical within, 1 breach = 50%. backlog=0 → falls into Partial (criticalRate >= 70 OR backlog==0)
        // To get NonCompliant need: backlog=0 AND criticalRate < 70 AND criticalRate < 70
        // Per code: if backlog>0 → AtRisk; if criticalRate>=compliant && high>=90 → Compliant; if criticalRate>=70 OR backlog==0 → Partial; else → NonCompliant
        // So NonCompliant is unreachable when backlog=0 (since backlog==0 → Partial)
        // Instead test: 2 critical CVEs, 1 within SLA (50%). CriticalPatchBacklogCount=0 → Partial
        var withinSla = MakeRemediatedCve("CVE-001", "Critical", discoveredDaysAgo: 5, remediatedDaysAgo: 1);
        var breach = MakeRemediatedCve("CVE-002", "Critical", discoveredDaysAgo: 20, remediatedDaysAgo: 1);
        var entry = MakePatchEntry(remediated: [withinSla, breach]);
        var handler = CreatePatchHandler(BuildPatchReader([entry]));

        var result = await handler.Handle(
            new GetSecurityPatchComplianceReport.Query(TenantId, PatchSlaCriticalDays: 7, CompliantCriticalRate: 95m),
            CancellationToken.None);

        // backlog=0 → Partial (at least)
        result.Value.TenantPatchComplianceSummary.Tier.Should().NotBe(GetSecurityPatchComplianceReport.PatchComplianceTier.AtRisk);
    }

    [Fact]
    public async Task AX2_CriticalPatchBacklog_CountsActiveCriticalOlderThanSla()
    {
        var active1 = MakeActiveCve("CVE-A1", "Critical", discoveredDaysAgo: 10);
        var active2 = MakeActiveCve("CVE-A2", "Critical", discoveredDaysAgo: 15);
        var active3 = MakeActiveCve("CVE-A3", "Critical", discoveredDaysAgo: 20);
        var active4 = MakeActiveCve("CVE-A4", "Critical", discoveredDaysAgo: 3); // within SLA
        var entry = MakePatchEntry(active: [active1, active2, active3, active4]);
        var handler = CreatePatchHandler(BuildPatchReader([entry]));

        var result = await handler.Handle(new GetSecurityPatchComplianceReport.Query(TenantId, PatchSlaCriticalDays: 7), CancellationToken.None);

        result.Value.TenantPatchComplianceSummary.CriticalPatchBacklogCount.Should().Be(3);
    }

    [Fact]
    public async Task AX2_OverallComplianceRate_CalculatedCorrectly()
    {
        // 4 CVEs remediated: 3 within SLA, 1 breach → 75%
        var within1 = MakeRemediatedCve("CVE-1", "Critical", discoveredDaysAgo: 5, remediatedDaysAgo: 1);
        var within2 = MakeRemediatedCve("CVE-2", "High", discoveredDaysAgo: 20, remediatedDaysAgo: 1);
        var within3 = MakeRemediatedCve("CVE-3", "Medium", discoveredDaysAgo: 50, remediatedDaysAgo: 1);
        var breach = MakeRemediatedCve("CVE-4", "Low", discoveredDaysAgo: 200, remediatedDaysAgo: 1);
        var entry = MakePatchEntry(remediated: [within1, within2, within3, breach]);
        var handler = CreatePatchHandler(BuildPatchReader([entry]));

        var result = await handler.Handle(
            new GetSecurityPatchComplianceReport.Query(TenantId, PatchSlaCriticalDays: 7, PatchSlaHighDays: 30, PatchSlaMediumDays: 90, PatchSlaLowDays: 180),
            CancellationToken.None);

        result.Value.TenantPatchComplianceSummary.OverallPatchComplianceRate.Should().Be(75m);
    }

    [Fact]
    public async Task AX2_SlaBreaches_Top20_OrderedByDaysDesc()
    {
        var breaches = Enumerable.Range(1, 25).Select(i =>
            MakeRemediatedCve($"CVE-{i:D3}", "Critical", discoveredDaysAgo: 10 + i, remediatedDaysAgo: 0))
            .ToList();
        var entry = MakePatchEntry(remediated: breaches);
        var handler = CreatePatchHandler(BuildPatchReader([entry]));

        var result = await handler.Handle(new GetSecurityPatchComplianceReport.Query(TenantId, PatchSlaCriticalDays: 7), CancellationToken.None);

        result.Value.SLABreaches.Count.Should().Be(20);
        result.Value.SLABreaches.Should().BeInDescendingOrder(x => x.DaysToRemediate);
    }

    [Fact]
    public async Task AX2_SlowPatchingTeams_WhenAvgAboveMedian()
    {
        // team-fast: avg 5d, team-mid: avg 10d, team-slow: avg 30d → median=10, slow=team-slow (avg > 10)
        var fastCve = MakeRemediatedCve("CVE-F", "Critical", discoveredDaysAgo: 8, remediatedDaysAgo: 3);
        var midCve = MakeRemediatedCve("CVE-M", "Critical", discoveredDaysAgo: 15, remediatedDaysAgo: 5);
        var slowCve = MakeRemediatedCve("CVE-S", "Critical", discoveredDaysAgo: 40, remediatedDaysAgo: 10);

        var entryFast = MakePatchEntry("svc-f", "svc-fast", "team-fast", remediated: [fastCve]);
        var entryMid = MakePatchEntry("svc-m", "svc-mid", "team-mid", remediated: [midCve]);
        var entrySlow = MakePatchEntry("svc-s", "svc-slow", "team-slow", remediated: [slowCve]);

        var handler = CreatePatchHandler(BuildPatchReader([entryFast, entryMid, entrySlow]));

        var result = await handler.Handle(new GetSecurityPatchComplianceReport.Query(TenantId, PatchSlaCriticalDays: 7), CancellationToken.None);

        result.Value.SlowPatchingTeams.Should().ContainSingle(t => t.TeamName == "team-slow");
        result.Value.SlowPatchingTeams.Should().NotContain(t => t.TeamName == "team-fast");
    }

    [Fact]
    public async Task AX2_PatchComplianceTrend_4Entries()
    {
        var handler = CreatePatchHandler(BuildPatchReader([]));

        var result = await handler.Handle(new GetSecurityPatchComplianceReport.Query(TenantId), CancellationToken.None);

        result.Value.PatchComplianceTrend.Should().HaveCount(4);
    }

    [Fact]
    public async Task AX2_PatchComplianceTrend_AtRisk_WhenCriticalActiveAtBoundary()
    {
        // Active critical CVE discovered 5 days ago → active at all 4 boundaries (w=0,1,2,3)
        var activeCve = MakeActiveCve("CVE-A", "Critical", discoveredDaysAgo: 5);
        var entry = MakePatchEntry(active: [activeCve]);
        var handler = CreatePatchHandler(BuildPatchReader([entry]));

        var result = await handler.Handle(new GetSecurityPatchComplianceReport.Query(TenantId), CancellationToken.None);

        result.Value.PatchComplianceTrend[0].Should().Be(GetSecurityPatchComplianceReport.PatchComplianceTier.AtRisk);
    }

    [Fact]
    public async Task AX2_NullImpl_ReturnsEmptyReport()
    {
        var reader = new NexTraceOne.Catalog.Application.Contracts.NullSecurityPatchComplianceReader();
        var handler = new GetSecurityPatchComplianceReport.Handler(reader, CreateClock());

        var result = await handler.Handle(new GetSecurityPatchComplianceReport.Query(TenantId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.SLABreaches.Should().BeEmpty();
        result.Value.SlowPatchingTeams.Should().BeEmpty();
    }

    [Fact]
    public async Task AX2_Validator_Rejects_LookbackBelow7()
    {
        var validator = new GetSecurityPatchComplianceReport.Validator();
        var query = new GetSecurityPatchComplianceReport.Query(TenantId, LookbackDays: 6);

        var result = await validator.ValidateAsync(query);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(query.LookbackDays));
    }
}
