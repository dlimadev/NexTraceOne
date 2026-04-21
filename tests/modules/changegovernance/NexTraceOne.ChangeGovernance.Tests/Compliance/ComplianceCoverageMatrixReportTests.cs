using System.Linq;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Abstractions;
using NexTraceOne.ChangeGovernance.Application.Compliance.Abstractions;
using NexTraceOne.ChangeGovernance.Application.Compliance.Features.GetComplianceCoverageMatrixReport;
using NexTraceOne.ChangeGovernance.Domain.ChangeIntelligence.Entities;

namespace NexTraceOne.ChangeGovernance.Tests.Compliance;

/// <summary>
/// Testes unitários para Wave U.1 — GetComplianceCoverageMatrixReport.
/// Cobre: sem releases (empty), sem cobertura (None), cobertura parcial (Partial/Minimal),
/// cobertura total (Full), multi-serviço, distribuição por standard, top gap, validator.
/// </summary>
public sealed class ComplianceCoverageMatrixReportTests
{
    private static readonly DateTimeOffset FixedNow = new(2026, 4, 21, 12, 0, 0, TimeSpan.Zero);
    private static readonly Guid TenantId = Guid.Parse("33333333-3333-3333-3333-333333333333");
    private const string TenantIdStr = "33333333-3333-3333-3333-333333333333";

    private static IDateTimeProvider CreateClock()
    {
        var clock = Substitute.For<IDateTimeProvider>();
        clock.UtcNow.Returns(FixedNow);
        return clock;
    }

    private static Release MakeRelease(string serviceName)
        => Release.Create(TenantId, Guid.NewGuid(), serviceName, "1.0.0", "prod",
            "pipeline-ci", "abc123", FixedNow.AddDays(-1));

    private static GetComplianceCoverageMatrixReport.Handler CreateHandler(
        IReadOnlyList<Release> releases,
        IReadOnlyList<IComplianceServiceCoverageReader.ServiceStandardCoverage> coverage)
    {
        var releaseRepo = Substitute.For<IReleaseRepository>();
        var coverageReader = Substitute.For<IComplianceServiceCoverageReader>();

        releaseRepo.ListInRangeAsync(
                Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(),
                Arg.Any<string?>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(releases);

        coverageReader.ListCoverageAsync(
                Arg.Any<string>(), Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns(coverage);

        return new GetComplianceCoverageMatrixReport.Handler(releaseRepo, coverageReader, CreateClock());
    }

    private static GetComplianceCoverageMatrixReport.Query DefaultQuery()
        => new(TenantId: TenantIdStr, LookbackDays: 30, TopGapCount: 10);

    // ── Empty: no releases ─────────────────────────────────────────────────

    [Fact]
    public async Task Handle_NoReleases_ReturnsEmptyReport()
    {
        var handler = CreateHandler([], []);
        var result = await handler.Handle(DefaultQuery(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var r = result.Value;
        Assert.Equal(0, r.TotalServicesAnalyzed);
        Assert.Equal(0m, r.TenantAvgCoverageScorePct);
        Assert.Empty(r.AllServices);
        Assert.Equal(0, r.LevelDistribution.FullCount);
        Assert.Equal(8, r.TotalStandardsActive);
    }

    // ── None: releases but no coverage assessments → all NotAssessed → None ──

    [Fact]
    public async Task Handle_ServicesWithNoCoverage_AllClassifiedAsNone()
    {
        var rel = MakeRelease("svc-a");
        var handler = CreateHandler([rel], []);
        var result = await handler.Handle(DefaultQuery(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var r = result.Value;
        Assert.Equal(1, r.TotalServicesAnalyzed);
        Assert.Equal(0m, r.TenantAvgCoverageScorePct);
        Assert.Equal(1, r.LevelDistribution.NoneCount);
        Assert.Equal(0, r.LevelDistribution.FullCount);

        var entry = r.AllServices.Single();
        Assert.Equal("svc-a", entry.ServiceName);
        Assert.Equal(GetComplianceCoverageMatrixReport.CoverageLevel.None, entry.Level);
        Assert.Equal(0m, entry.CoverageScorePct);
    }

    // ── Full: all 8 standards assessed → Full ────────────────────────────

    [Fact]
    public async Task Handle_AllStandardsAssessed_ClassifiesFull()
    {
        var rel = MakeRelease("svc-full");
        var coverage = GetComplianceCoverageMatrixReport.DefaultEnabledStandards
            .Select(std => new IComplianceServiceCoverageReader.ServiceStandardCoverage(
                "svc-full", std, ComplianceCoverageStatus.Compliant))
            .ToList();

        var handler = CreateHandler([rel], coverage);
        var result = await handler.Handle(DefaultQuery(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var r = result.Value;
        Assert.Equal(1, r.TotalServicesAnalyzed);
        Assert.Equal(100m, r.TenantAvgCoverageScorePct);
        Assert.Equal(1, r.LevelDistribution.FullCount);

        var entry = r.AllServices.Single();
        Assert.Equal(GetComplianceCoverageMatrixReport.CoverageLevel.Full, entry.Level);
        Assert.Equal(100m, entry.CoverageScorePct);
    }

    // ── Partial: 4 of 8 standards assessed → Partial (50% = default threshold) ─

    [Fact]
    public async Task Handle_HalfStandardsAssessed_ClassifiesPartial()
    {
        var rel = MakeRelease("svc-partial");
        var coverage = GetComplianceCoverageMatrixReport.DefaultEnabledStandards
            .Take(4)
            .Select(std => new IComplianceServiceCoverageReader.ServiceStandardCoverage(
                "svc-partial", std, ComplianceCoverageStatus.Compliant))
            .ToList();

        var handler = CreateHandler([rel], coverage);
        var result = await handler.Handle(DefaultQuery(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var r = result.Value;
        var entry = r.AllServices.Single();
        // 4/8 = 50%, exactly at partial threshold → Partial
        Assert.Equal(GetComplianceCoverageMatrixReport.CoverageLevel.Partial, entry.Level);
        Assert.Equal(50m, entry.CoverageScorePct);
    }

    // ── Minimal: 1 of 8 standards assessed (12.5% < 50% threshold) → Minimal ─

    [Fact]
    public async Task Handle_OneStandardAssessed_ClassifiesMinimal()
    {
        var rel = MakeRelease("svc-minimal");
        var coverage = new List<IComplianceServiceCoverageReader.ServiceStandardCoverage>
        {
            new("svc-minimal", "GDPR", ComplianceCoverageStatus.Compliant)
        };

        var handler = CreateHandler([rel], coverage);
        var result = await handler.Handle(DefaultQuery(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var r = result.Value;
        var entry = r.AllServices.Single();
        Assert.Equal(GetComplianceCoverageMatrixReport.CoverageLevel.Minimal, entry.Level);
        Assert.True(entry.CoverageScorePct > 0m && entry.CoverageScorePct < 50m);
    }

    // ── Multi-service: correct distribution ───────────────────────────────

    [Fact]
    public async Task Handle_MultipleServices_CorrectLevelDistribution()
    {
        var relA = MakeRelease("svc-a"); // Full (all 8)
        var relB = MakeRelease("svc-b"); // None (no coverage)

        var coverage = GetComplianceCoverageMatrixReport.DefaultEnabledStandards
            .Select(std => new IComplianceServiceCoverageReader.ServiceStandardCoverage(
                "svc-a", std, ComplianceCoverageStatus.Compliant))
            .ToList();

        var handler = CreateHandler([relA, relB], coverage);
        var result = await handler.Handle(DefaultQuery(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var r = result.Value;
        Assert.Equal(2, r.TotalServicesAnalyzed);
        Assert.Equal(1, r.LevelDistribution.FullCount);
        Assert.Equal(1, r.LevelDistribution.NoneCount);
        Assert.Equal(50m, r.TenantAvgCoverageScorePct); // (100 + 0) / 2
    }

    // ── TopGapServices ordered by lowest coverage first ───────────────────

    [Fact]
    public async Task Handle_MultipleServices_TopGapOrderedByLowestCoverage()
    {
        var relA = MakeRelease("svc-best");  // Full
        var relB = MakeRelease("svc-worst"); // None

        var coverage = GetComplianceCoverageMatrixReport.DefaultEnabledStandards
            .Select(std => new IComplianceServiceCoverageReader.ServiceStandardCoverage(
                "svc-best", std, ComplianceCoverageStatus.Compliant))
            .ToList();

        var handler = CreateHandler([relA, relB], coverage);
        var result = await handler.Handle(DefaultQuery(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var r = result.Value;
        Assert.Equal("svc-worst", r.TopGapServices.First().ServiceName);
        Assert.Equal("svc-best", r.TopGapServices.Last().ServiceName);
    }

    // ── ByStandard: per-standard assessment count ─────────────────────────

    [Fact]
    public async Task Handle_OneServiceFullyCovered_ByStandardShowsAllAssessed()
    {
        var rel = MakeRelease("svc-a");
        var coverage = GetComplianceCoverageMatrixReport.DefaultEnabledStandards
            .Select(std => new IComplianceServiceCoverageReader.ServiceStandardCoverage(
                "svc-a", std, ComplianceCoverageStatus.Compliant))
            .ToList();

        var handler = CreateHandler([rel], coverage);
        var result = await handler.Handle(DefaultQuery(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var r = result.Value;
        Assert.Equal(8, r.ByStandard.Count);
        Assert.All(r.ByStandard, e =>
        {
            Assert.Equal(1, e.AssessedServiceCount);
            Assert.Equal(100m, e.CoveredPct);
        });
    }

    // ── CustomEnabledStandards: only 2 standards active ──────────────────

    [Fact]
    public async Task Handle_CustomEnabledStandards_OnlyThoseStandardsConsidered()
    {
        var rel = MakeRelease("svc-a");
        var coverage = new List<IComplianceServiceCoverageReader.ServiceStandardCoverage>
        {
            new("svc-a", "GDPR", ComplianceCoverageStatus.Compliant),
            new("svc-a", "SOC2", ComplianceCoverageStatus.Compliant)
        };

        var handler = CreateHandler([rel], coverage);
        var query = new GetComplianceCoverageMatrixReport.Query(
            TenantId: TenantIdStr,
            LookbackDays: 30,
            EnabledStandards: ["GDPR", "SOC2"]);

        var result = await handler.Handle(query, CancellationToken.None);

        Assert.True(result.IsSuccess);
        var r = result.Value;
        Assert.Equal(2, r.TotalStandardsActive);
        Assert.Equal(2, r.ByStandard.Count);
        var entry = r.AllServices.Single();
        Assert.Equal(GetComplianceCoverageMatrixReport.CoverageLevel.Full, entry.Level);
    }

    // ── NonCompliant status still counts as assessed ───────────────────────

    [Fact]
    public async Task Handle_NonCompliantStatus_CountsAsAssessed()
    {
        var rel = MakeRelease("svc-nc");
        var coverage = new List<IComplianceServiceCoverageReader.ServiceStandardCoverage>
        {
            new("svc-nc", "GDPR", ComplianceCoverageStatus.NonCompliant)
        };

        var handler = CreateHandler([rel], coverage);
        var result = await handler.Handle(DefaultQuery(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var r = result.Value;
        var entry = r.AllServices.Single();
        Assert.True(entry.CoverageScorePct > 0m, "NonCompliant should still count as assessed");
        Assert.Equal(ComplianceCoverageStatus.NonCompliant,
            entry.ByStandard["GDPR"]);
    }

    // ── DuplicateServiceName across releases: deduplication ───────────────

    [Fact]
    public async Task Handle_DuplicateServiceNameInReleases_DeduplicatesServices()
    {
        var rel1 = MakeRelease("svc-dup");
        var rel2 = MakeRelease("svc-dup");
        var handler = CreateHandler([rel1, rel2], []);
        var result = await handler.Handle(DefaultQuery(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(1, result.Value.TotalServicesAnalyzed);
    }

    // ── Validator ──────────────────────────────────────────────────────────

    [Fact]
    public void Validator_EmptyTenantId_ReturnsError()
    {
        var v = new GetComplianceCoverageMatrixReport.Validator();
        Assert.False(v.Validate(new GetComplianceCoverageMatrixReport.Query(TenantId: "")).IsValid);
    }

    [Fact]
    public void Validator_InvalidLookbackDays_ReturnsError()
    {
        var v = new GetComplianceCoverageMatrixReport.Validator();
        Assert.False(v.Validate(new GetComplianceCoverageMatrixReport.Query(TenantId: TenantIdStr, LookbackDays: 3)).IsValid);
    }

    [Fact]
    public void Validator_InvalidPartialThreshold_ReturnsError()
    {
        var v = new GetComplianceCoverageMatrixReport.Validator();
        Assert.False(v.Validate(new GetComplianceCoverageMatrixReport.Query(TenantId: TenantIdStr, PartialThresholdPct: 0m)).IsValid);
    }

    [Fact]
    public void Validator_ValidQuery_PassesValidation()
    {
        var v = new GetComplianceCoverageMatrixReport.Validator();
        Assert.True(v.Validate(new GetComplianceCoverageMatrixReport.Query(TenantId: TenantIdStr)).IsValid);
    }
}
