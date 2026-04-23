using System.Linq;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.Catalog.Application.Contracts.Abstractions;
using NexTraceOne.Catalog.Application.Contracts.Features.GetDocumentationHealthReport;

namespace NexTraceOne.Catalog.Tests.Contracts.Application.Features;

/// <summary>
/// Testes unitários para Wave AY.1 — GetDocumentationHealthReport.
/// Cobre scoring, cobertura de runbook, cobertura de API docs, frescura e tier do tenant (~15 testes).
/// </summary>
public sealed class WaveAyDocumentationHealthTests
{
    private static readonly DateTimeOffset FixedNow = new(2026, 9, 1, 12, 0, 0, TimeSpan.Zero);
    private const string TenantId = "tenant-ay-001";

    private static IDateTimeProvider CreateClock()
    {
        var clock = Substitute.For<IDateTimeProvider>();
        clock.UtcNow.Returns(FixedNow);
        return clock;
    }

    private static GetDocumentationHealthReport.Handler CreateHandler(
        IDocumentationHealthReader? reader = null) =>
        new(reader ?? Substitute.For<IDocumentationHealthReader>(), CreateClock());

    private static IDocumentationHealthReader BuildReader(
        IReadOnlyList<IDocumentationHealthReader.ServiceDocumentationEntry> entries)
    {
        var reader = Substitute.For<IDocumentationHealthReader>();
        reader.ListByTenantAsync(TenantId, Arg.Any<CancellationToken>())
              .Returns(entries);
        return reader;
    }

    private static IDocumentationHealthReader.ServiceDocumentationEntry MakeEntry(
        string serviceId = "svc-ay",
        string serviceName = "service-ay",
        string teamName = "team-ay",
        string serviceTier = "Internal",
        string domainName = "domain-ay",
        bool hasRunbookUrl = true,
        DateTimeOffset? runbookLastUpdated = null,
        int contractCount = 1,
        int contractsWithDescription = 1,
        int contractsWithExamples = 1,
        int contractsWithErrorCodes = 1,
        bool hasArchDoc = true,
        bool hasOnboardingDoc = true,
        DateTimeOffset? docLastUpdated = null,
        IReadOnlyList<string>? contributorIds = null) =>
        new(serviceId, serviceName, teamName, serviceTier, domainName,
            hasRunbookUrl, runbookLastUpdated ?? FixedNow.AddDays(-30),
            contractCount, contractsWithDescription, contractsWithExamples, contractsWithErrorCodes,
            hasArchDoc, hasOnboardingDoc, docLastUpdated ?? FixedNow.AddDays(-30),
            contributorIds ?? ["user-1"]);

    // ────────────────────────────────────────────────────────────────────────
    // Empty report
    // ────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task AY1_EmptyReport_WhenNoEntries()
    {
        var handler = CreateHandler(BuildReader([]));
        var result = await handler.Handle(new GetDocumentationHealthReport.Query(TenantId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.ByService.Should().BeEmpty();
        result.Value.Summary.TenantDocHealthScore.Should().Be(0m);
        result.Value.DocDebt.Should().Be(0);
        result.Value.CriticalServicesWithoutRunbook.Should().BeEmpty();
    }

    // ────────────────────────────────────────────────────────────────────────
    // DocHealthScore calculation
    // ────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task AY1_DocHealthScore_PerfectService_Is100()
    {
        // Perfect: Covered runbook (35%) + Full apiDoc (30%) + arch (15%) + Fresh freshness (20%) = 100
        var entry = MakeEntry(
            hasRunbookUrl: true,
            runbookLastUpdated: FixedNow.AddDays(-30),
            contractCount: 2, contractsWithDescription: 2, contractsWithExamples: 2, contractsWithErrorCodes: 2,
            hasArchDoc: true,
            docLastUpdated: FixedNow.AddDays(-30));

        var handler = CreateHandler(BuildReader([entry]));
        var result = await handler.Handle(new GetDocumentationHealthReport.Query(TenantId), CancellationToken.None);

        result.Value.ByService[0].DocHealthScore.Should().Be(100m);
    }

    [Fact]
    public async Task AY1_DocHealthScore_NoRunbook_PartialApiDoc_NoArch_AgingFreshness()
    {
        // Missing runbook (0% × 35) + Partial apiDoc (50% × 30) + no arch (0% × 15) + Aging freshness (70% × 20)
        // = 0 + 15 + 0 + 14 = 29
        var entry = MakeEntry(
            hasRunbookUrl: false,
            contractCount: 2, contractsWithDescription: 2, contractsWithExamples: 1, contractsWithErrorCodes: 1,
            hasArchDoc: false,
            docLastUpdated: FixedNow.AddDays(-120)); // 120 days old, freshness = 180 days → 120 > 90 (50%) → Aging

        var handler = CreateHandler(BuildReader([entry]));
        var result = await handler.Handle(
            new GetDocumentationHealthReport.Query(TenantId, FreshnessDays: 180), CancellationToken.None);

        result.Value.ByService[0].RunbookCoverage.Should().Be(GetDocumentationHealthReport.RunbookCoverageStatus.Missing);
        result.Value.ByService[0].ApiDocCoverage.Should().Be(GetDocumentationHealthReport.ApiDocCoverageStatus.Partial);
        result.Value.ByService[0].ArchitectureDocPresence.Should().BeFalse();
        result.Value.ByService[0].DocFreshnessTier.Should().Be(GetDocumentationHealthReport.DocFreshnessTier.Aging);
        result.Value.ByService[0].DocHealthScore.Should().Be(29m);
    }

    [Fact]
    public async Task AY1_DocHealthScore_ZeroScore_AllMissing()
    {
        // All missing: no runbook, absent api doc, no arch doc, stale freshness
        var entry = MakeEntry(
            hasRunbookUrl: false,
            contractCount: 2, contractsWithDescription: 0, contractsWithExamples: 0, contractsWithErrorCodes: 0,
            hasArchDoc: false,
            docLastUpdated: FixedNow.AddDays(-200)); // Stale (>180 days, non-critical tier)

        var handler = CreateHandler(BuildReader([entry]));
        var result = await handler.Handle(
            new GetDocumentationHealthReport.Query(TenantId, FreshnessDays: 180), CancellationToken.None);

        result.Value.ByService[0].DocHealthScore.Should().Be(6m); // Stale freshness = 30% × 20 = 6
        result.Value.ByService[0].RunbookCoverage.Should().Be(GetDocumentationHealthReport.RunbookCoverageStatus.Missing);
        result.Value.ByService[0].ApiDocCoverage.Should().Be(GetDocumentationHealthReport.ApiDocCoverageStatus.Absent);
        result.Value.ByService[0].DocFreshnessTier.Should().Be(GetDocumentationHealthReport.DocFreshnessTier.Stale);
    }

    // ────────────────────────────────────────────────────────────────────────
    // RunbookCoverage logic
    // ────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task AY1_RunbookCoverage_Missing_WhenNoRunbookUrl()
    {
        var entry = MakeEntry(hasRunbookUrl: false);
        var handler = CreateHandler(BuildReader([entry]));
        var result = await handler.Handle(new GetDocumentationHealthReport.Query(TenantId), CancellationToken.None);

        result.Value.ByService[0].RunbookCoverage.Should().Be(GetDocumentationHealthReport.RunbookCoverageStatus.Missing);
    }

    [Fact]
    public async Task AY1_RunbookCoverage_Stale_WhenOlderThanFreshnessLimit()
    {
        var entry = MakeEntry(hasRunbookUrl: true, runbookLastUpdated: FixedNow.AddDays(-100));
        var handler = CreateHandler(BuildReader([entry]));
        var result = await handler.Handle(
            new GetDocumentationHealthReport.Query(TenantId, RunbookFreshnessDays: 90), CancellationToken.None);

        result.Value.ByService[0].RunbookCoverage.Should().Be(GetDocumentationHealthReport.RunbookCoverageStatus.Stale);
    }

    [Fact]
    public async Task AY1_RunbookCoverage_Covered_WhenFreshAndPresent()
    {
        var entry = MakeEntry(hasRunbookUrl: true, runbookLastUpdated: FixedNow.AddDays(-30));
        var handler = CreateHandler(BuildReader([entry]));
        var result = await handler.Handle(
            new GetDocumentationHealthReport.Query(TenantId, RunbookFreshnessDays: 90), CancellationToken.None);

        result.Value.ByService[0].RunbookCoverage.Should().Be(GetDocumentationHealthReport.RunbookCoverageStatus.Covered);
    }

    // ────────────────────────────────────────────────────────────────────────
    // ApiDocCoverage logic
    // ────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task AY1_ApiDocCoverage_Absent_WhenNoContracts()
    {
        var entry = MakeEntry(contractCount: 0);
        var handler = CreateHandler(BuildReader([entry]));
        var result = await handler.Handle(new GetDocumentationHealthReport.Query(TenantId), CancellationToken.None);

        result.Value.ByService[0].ApiDocCoverage.Should().Be(GetDocumentationHealthReport.ApiDocCoverageStatus.Absent);
    }

    [Fact]
    public async Task AY1_ApiDocCoverage_Full_WhenAllContractsDocumented()
    {
        var entry = MakeEntry(contractCount: 3, contractsWithDescription: 3, contractsWithExamples: 3, contractsWithErrorCodes: 3);
        var handler = CreateHandler(BuildReader([entry]));
        var result = await handler.Handle(new GetDocumentationHealthReport.Query(TenantId), CancellationToken.None);

        result.Value.ByService[0].ApiDocCoverage.Should().Be(GetDocumentationHealthReport.ApiDocCoverageStatus.Full);
    }

    // ────────────────────────────────────────────────────────────────────────
    // CriticalServicesWithoutRunbook
    // ────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task AY1_CriticalServicesWithoutRunbook_IncludesCriticalTierOnly()
    {
        var criticalSvc = MakeEntry("svc-crit", "critical-svc", serviceTier: "Critical", hasRunbookUrl: false);
        var internalSvc = MakeEntry("svc-int", "internal-svc", serviceTier: "Internal", hasRunbookUrl: false);

        var handler = CreateHandler(BuildReader([criticalSvc, internalSvc]));
        var result = await handler.Handle(
            new GetDocumentationHealthReport.Query(TenantId, CriticalWithoutRunbookTiers: "Critical,High"),
            CancellationToken.None);

        result.Value.CriticalServicesWithoutRunbook.Should().HaveCount(1);
        result.Value.CriticalServicesWithoutRunbook[0].ServiceId.Should().Be("svc-crit");
    }

    [Fact]
    public async Task AY1_TenantTier_Critical_WhenCriticalServicesWithoutRunbook()
    {
        var entry = MakeEntry(serviceTier: "Critical", hasRunbookUrl: false);
        var handler = CreateHandler(BuildReader([entry]));
        var result = await handler.Handle(new GetDocumentationHealthReport.Query(TenantId), CancellationToken.None);

        result.Value.Summary.TenantDocHealthTier.Should().Be(GetDocumentationHealthReport.TenantDocHealthTier.Critical);
    }

    // ────────────────────────────────────────────────────────────────────────
    // Tenant scoring and tiers
    // ────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task AY1_TenantTier_Excellent_WhenHighScore()
    {
        // Perfect service with Internal tier (weight=1), score=100 → tenant=100
        var entry = MakeEntry();
        var handler = CreateHandler(BuildReader([entry]));
        var result = await handler.Handle(new GetDocumentationHealthReport.Query(TenantId), CancellationToken.None);

        result.Value.Summary.TenantDocHealthTier.Should().Be(GetDocumentationHealthReport.TenantDocHealthTier.Excellent);
        result.Value.Summary.TenantDocHealthScore.Should().Be(100m);
    }

    [Fact]
    public async Task AY1_WeightedScore_CriticalTierHasHigherWeight()
    {
        // Critical svc (score=100, weight=3) + Internal svc (score=0, weight=1)
        // weighted = (100*3 + 0*1) / 4 = 75 → Good
        var critSvc = MakeEntry("svc-crit", "svc-crit", serviceTier: "Critical",
            hasRunbookUrl: true, runbookLastUpdated: FixedNow.AddDays(-30),
            contractCount: 1, contractsWithDescription: 1, contractsWithExamples: 1, contractsWithErrorCodes: 1,
            hasArchDoc: true, docLastUpdated: FixedNow.AddDays(-30));
        var intSvc = MakeEntry("svc-int", "svc-int", serviceTier: "Internal",
            hasRunbookUrl: false, contractCount: 0, hasArchDoc: false,
            docLastUpdated: FixedNow.AddDays(-400));

        var handler = CreateHandler(BuildReader([critSvc, intSvc]));
        var result = await handler.Handle(new GetDocumentationHealthReport.Query(TenantId), CancellationToken.None);

        result.Value.Summary.TenantDocHealthScore.Should().BeGreaterThan(60m); // Should be 75 → Good
        result.Value.Summary.TenantDocHealthTier.Should().Be(GetDocumentationHealthReport.TenantDocHealthTier.Good);
    }

    // ────────────────────────────────────────────────────────────────────────
    // BestDocumentedServices
    // ────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task AY1_BestDocumentedServices_LimitedToMaxCount()
    {
        var entries = Enumerable.Range(1, 10)
            .Select(i => MakeEntry($"svc-{i}", $"service-{i}",
                docLastUpdated: FixedNow.AddDays(-i)))
            .ToList();

        var handler = CreateHandler(BuildReader(entries));
        var result = await handler.Handle(
            new GetDocumentationHealthReport.Query(TenantId, MaxBestDocumentedServices: 3),
            CancellationToken.None);

        result.Value.BestDocumentedServices.Should().HaveCount(3);
    }

    // ────────────────────────────────────────────────────────────────────────
    // StaleDocsByTeam
    // ────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task AY1_StaleDocsByTeam_GroupsByTeamName()
    {
        var stale1 = MakeEntry("svc-1", "svc-1", "team-alpha", docLastUpdated: FixedNow.AddDays(-400), hasRunbookUrl: false);
        var stale2 = MakeEntry("svc-2", "svc-2", "team-alpha", docLastUpdated: FixedNow.AddDays(-400), hasRunbookUrl: false);
        var fresh = MakeEntry("svc-3", "svc-3", "team-beta", docLastUpdated: FixedNow.AddDays(-10));

        var handler = CreateHandler(BuildReader([stale1, stale2, fresh]));
        var result = await handler.Handle(
            new GetDocumentationHealthReport.Query(TenantId, FreshnessDays: 180),
            CancellationToken.None);

        result.Value.StaleDocsByTeam.Should().HaveCount(1);
        result.Value.StaleDocsByTeam[0].TeamName.Should().Be("team-alpha");
        result.Value.StaleDocsByTeam[0].MissingRunbookCount.Should().Be(2);
    }

    // ────────────────────────────────────────────────────────────────────────
    // Validator
    // ────────────────────────────────────────────────────────────────────────

    [Fact]
    public void AY1_Validator_RejectsEmptyTenantId()
    {
        var validator = new GetDocumentationHealthReport.Validator();
        var result = validator.Validate(new GetDocumentationHealthReport.Query(""));
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void AY1_Validator_RejectsInvalidFreshnessDays()
    {
        var validator = new GetDocumentationHealthReport.Validator();
        var result = validator.Validate(new GetDocumentationHealthReport.Query("t", FreshnessDays: 1));
        result.IsValid.Should().BeFalse();
    }
}
