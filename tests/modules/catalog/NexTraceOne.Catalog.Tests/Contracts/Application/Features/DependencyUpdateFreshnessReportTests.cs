using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.Catalog.Application.Contracts.Abstractions;
using NexTraceOne.Catalog.Application.Contracts.Features.GetDependencyUpdateFreshnessReport;
using NexTraceOne.Catalog.Application.DependencyGovernance.Ports;
using NexTraceOne.Catalog.Application.Graph.Abstractions;
using NexTraceOne.Catalog.Domain.Contracts.Entities;
using NexTraceOne.Catalog.Domain.Contracts.Enums;
using NexTraceOne.Catalog.Domain.DependencyGovernance.Enums;
using NexTraceOne.Catalog.Domain.Graph.Entities;

namespace NexTraceOne.Catalog.Tests.Contracts.Application.Features;

/// <summary>
/// Testes unitários para Wave U.2 — GetDependencyUpdateFreshnessReport.
/// Cobre: sem serviços, serviço fresh, serviço aging, serviço stale com vuln,
/// serviço critical, multi-serviço, top stale ordering, validator.
/// </summary>
public sealed class DependencyUpdateFreshnessReportTests
{
    private static readonly DateTimeOffset FixedNow = new(2026, 4, 21, 12, 0, 0, TimeSpan.Zero);
    private const string TenantId = "tenant-dep-fresh-001";

    private static IDateTimeProvider CreateClock()
    {
        var clock = Substitute.For<IDateTimeProvider>();
        clock.UtcNow.Returns(FixedNow);
        return clock;
    }

    private static ServiceAsset MakeSvc(string name)
        => ServiceAsset.Create(name, "test-domain", "test-team");

    private static ContractChangelog MakeChangelog(string tenantId, string serviceName, DateTimeOffset createdAt)
        => ContractChangelog.Create(
            tenantId, Guid.NewGuid().ToString(), serviceName,
            fromVersion: "1.0.0", toVersion: "1.1.0",
            contractVersionId: Guid.NewGuid(), verificationId: null,
            source: ChangelogSource.Verification,
            entries: "[]", summary: "Minor change",
            markdownContent: null, jsonContent: null, commitSha: null,
            createdAt: createdAt, createdBy: "system");

    private static GetDependencyUpdateFreshnessReport.Handler CreateHandler(
        IReadOnlyList<ServiceAsset> services,
        IReadOnlyList<ContractChangelog> changelogs,
        Func<Guid, int>? vulnCountByService = null)
    {
        var svcRepo = Substitute.For<IServiceAssetRepository>();
        var changelogRepo = Substitute.For<IContractChangelogRepository>();
        var vulnRepo = Substitute.For<IVulnerabilityAdvisoryRepository>();

        svcRepo.ListAllAsync(Arg.Any<CancellationToken>()).Returns(services);

        changelogRepo.ListByTenantInPeriodAsync(
                Arg.Any<string>(), Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns(changelogs);

        vulnRepo.CountByServiceAndSeverityAsync(
                Arg.Any<Guid>(), Arg.Any<VulnerabilitySeverity>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                var id = callInfo.Arg<Guid>();
                return vulnCountByService?.Invoke(id) ?? 0;
            });

        return new GetDependencyUpdateFreshnessReport.Handler(svcRepo, changelogRepo, vulnRepo, CreateClock());
    }

    private static GetDependencyUpdateFreshnessReport.Query DefaultQuery()
        => new(TenantId: TenantId, LookbackDays: 180);

    // ── Empty: no services ─────────────────────────────────────────────────

    [Fact]
    public async Task Handle_NoServices_ReturnsEmptyReport()
    {
        var handler = CreateHandler([], []);
        var result = await handler.Handle(DefaultQuery(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(0, result.Value.TotalServicesAnalyzed);
        Assert.Empty(result.Value.AllServices);
    }

    // ── Fresh: changelog within 30 days → Fresh ───────────────────────────

    [Fact]
    public async Task Handle_RecentChangelog_ClassifiesFresh()
    {
        var svc = MakeSvc("svc-fresh");
        var changelog = MakeChangelog(TenantId, "svc-fresh", FixedNow.AddDays(-10));

        var handler = CreateHandler([svc], [changelog]);
        var result = await handler.Handle(DefaultQuery(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var entry = result.Value.AllServices.Single();
        Assert.Equal(GetDependencyUpdateFreshnessReport.FreshnessTier.Fresh, entry.Tier);
        Assert.InRange(entry.DaysSinceLastDependencyChange, 0, 30);
    }

    // ── Aging: changelog between 31 and 90 days ago → Aging ──────────────

    [Fact]
    public async Task Handle_AgingChangelog_ClassifiesAging()
    {
        var svc = MakeSvc("svc-aging");
        var changelog = MakeChangelog(TenantId, "svc-aging", FixedNow.AddDays(-60));

        var handler = CreateHandler([svc], [changelog]);
        var result = await handler.Handle(DefaultQuery(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var entry = result.Value.AllServices.Single();
        Assert.Equal(GetDependencyUpdateFreshnessReport.FreshnessTier.Aging, entry.Tier);
    }

    // ── Stale: changelog between 91 and 180 days → Stale ─────────────────

    [Fact]
    public async Task Handle_StaleChangelog_ClassifiesStale()
    {
        var svc = MakeSvc("svc-stale");
        var changelog = MakeChangelog(TenantId, "svc-stale", FixedNow.AddDays(-150));

        var handler = CreateHandler([svc], [changelog]);
        var result = await handler.Handle(DefaultQuery(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var entry = result.Value.AllServices.Single();
        Assert.Equal(GetDependencyUpdateFreshnessReport.FreshnessTier.Stale, entry.Tier);
    }

    // ── Critical: no changelog at all → Critical ──────────────────────────

    [Fact]
    public async Task Handle_NoChangelog_ClassifiesCritical()
    {
        var svc = MakeSvc("svc-critical");
        var handler = CreateHandler([svc], []);
        var result = await handler.Handle(DefaultQuery(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var entry = result.Value.AllServices.Single();
        Assert.Equal(GetDependencyUpdateFreshnessReport.FreshnessTier.Critical, entry.Tier);
    }

    // ── VulnerabilityGap: Stale service with open vulns ───────────────────

    [Fact]
    public async Task Handle_StaleServiceWithVulns_FlagsVulnerabilityGap()
    {
        var svc = MakeSvc("svc-stale-vuln");
        var changelog = MakeChangelog(TenantId, "svc-stale-vuln", FixedNow.AddDays(-150));

        var handler = CreateHandler([svc], [changelog],
            vulnCountByService: _ => 3);

        var result = await handler.Handle(DefaultQuery(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var entry = result.Value.AllServices.Single();
        Assert.Equal(GetDependencyUpdateFreshnessReport.FreshnessTier.Stale, entry.Tier);
        Assert.True(entry.VulnerabilityGap);
        Assert.True(entry.OpenVulnerabilityCount > 0);
    }

    // ── No VulnerabilityGap for Fresh service ─────────────────────────────

    [Fact]
    public async Task Handle_FreshServiceWithVulns_NoVulnerabilityGapCheck()
    {
        // Vuln check only runs for Stale/Critical
        var svc = MakeSvc("svc-fresh-vuln");
        var changelog = MakeChangelog(TenantId, "svc-fresh-vuln", FixedNow.AddDays(-5));

        var handler = CreateHandler([svc], [changelog],
            vulnCountByService: _ => 10);

        var result = await handler.Handle(DefaultQuery(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var entry = result.Value.AllServices.Single();
        Assert.Equal(GetDependencyUpdateFreshnessReport.FreshnessTier.Fresh, entry.Tier);
        Assert.False(entry.VulnerabilityGap);
        Assert.Equal(0, entry.OpenVulnerabilityCount);
    }

    // ── Multi-service: correct distribution ───────────────────────────────

    [Fact]
    public async Task Handle_MultipleServices_CorrectTierDistribution()
    {
        var svcFresh = MakeSvc("svc-fresh");
        var svcCritical = MakeSvc("svc-critical");

        var changelog = MakeChangelog(TenantId, "svc-fresh", FixedNow.AddDays(-10));
        var handler = CreateHandler([svcFresh, svcCritical], [changelog]);

        var result = await handler.Handle(DefaultQuery(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var r = result.Value;
        Assert.Equal(2, r.TotalServicesAnalyzed);
        Assert.Equal(1, r.TierDistribution.FreshCount);
        Assert.Equal(1, r.TierDistribution.CriticalCount);
    }

    // ── TopStaleServices ordered by days descending ───────────────────────

    [Fact]
    public async Task Handle_MultipleStale_TopStaleOrderedByDaysDescending()
    {
        var svcA = MakeSvc("svc-a"); // no changelog → Critical
        var svcB = MakeSvc("svc-b"); // 150 days → Stale

        var changelog = MakeChangelog(TenantId, "svc-b", FixedNow.AddDays(-150));
        var handler = CreateHandler([svcA, svcB], [changelog]);

        var result = await handler.Handle(DefaultQuery(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var top = result.Value.TopStaleServices;
        Assert.Equal("svc-a", top.First().ServiceName); // no changelog = StaleThreshold+1 days
    }

    // ── Validator ──────────────────────────────────────────────────────────

    [Fact]
    public void Validator_EmptyTenantId_ReturnsError()
    {
        var v = new GetDependencyUpdateFreshnessReport.Validator();
        Assert.False(v.Validate(new GetDependencyUpdateFreshnessReport.Query(TenantId: "")).IsValid);
    }

    [Fact]
    public void Validator_InvalidLookbackDays_ReturnsError()
    {
        var v = new GetDependencyUpdateFreshnessReport.Validator();
        Assert.False(v.Validate(new GetDependencyUpdateFreshnessReport.Query(TenantId: TenantId, LookbackDays: 10)).IsValid);
    }

    [Fact]
    public void Validator_ValidQuery_PassesValidation()
    {
        var v = new GetDependencyUpdateFreshnessReport.Validator();
        Assert.True(v.Validate(new GetDependencyUpdateFreshnessReport.Query(TenantId: TenantId)).IsValid);
    }
}
