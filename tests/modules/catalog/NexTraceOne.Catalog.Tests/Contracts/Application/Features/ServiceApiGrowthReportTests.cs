using System.Linq;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.Catalog.Application.Contracts.Abstractions;
using NexTraceOne.Catalog.Application.Contracts.Features.GetServiceApiGrowthReport;
using NexTraceOne.Catalog.Application.Graph.Abstractions;
using NexTraceOne.Catalog.Domain.Contracts.Entities;
using NexTraceOne.Catalog.Domain.Contracts.Enums;
using NexTraceOne.Catalog.Domain.Graph.Entities;

namespace NexTraceOne.Catalog.Tests.Contracts.Application.Features;

/// <summary>
/// Testes unitários para Wave V.1 — GetServiceApiGrowthReport.
/// Cobre: sem serviços, sem changelogs, serviço stable, growing, rapid/exploding,
/// shrinking, governanceRisk, novo serviço (previous=0), multi-serviço, validator.
/// </summary>
public sealed class ServiceApiGrowthReportTests
{
    private static readonly DateTimeOffset FixedNow = new(2026, 4, 21, 12, 0, 0, TimeSpan.Zero);
    private const string TenantId = "tenant-api-growth-v01";

    private static IDateTimeProvider CreateClock()
    {
        var clock = Substitute.For<IDateTimeProvider>();
        clock.UtcNow.Returns(FixedNow);
        return clock;
    }

    private static ServiceAsset MakeSvc(string name)
        => ServiceAsset.Create(name, "test-domain", "test-team");

    private static ContractChangelog MakeChangelog(
        string tenantId, string serviceName, string apiAssetId, DateTimeOffset createdAt)
        => ContractChangelog.Create(
            tenantId, apiAssetId, serviceName,
            fromVersion: "1.0.0", toVersion: "1.1.0",
            contractVersionId: Guid.NewGuid(), verificationId: null,
            source: ChangelogSource.Verification,
            entries: "[]", summary: "Minor change",
            markdownContent: null, jsonContent: null, commitSha: null,
            createdAt: createdAt, createdBy: "system");

    private static ContractHealthScore MakeHealthScore(Guid apiAssetId, int score)
        => ContractHealthScore.Create(
            apiAssetId, score, score, score, score, score, score,
            degradationThreshold: 10,
            calculatedAt: FixedNow.AddHours(-1));

    private static GetServiceApiGrowthReport.Handler CreateHandler(
        IReadOnlyList<ServiceAsset> services,
        IReadOnlyList<ContractChangelog> currentChangelogs,
        IReadOnlyList<ContractChangelog> compChangelogs,
        IReadOnlyList<ContractHealthScore> healthScores)
    {
        var svcRepo = Substitute.For<IServiceAssetRepository>();
        var changelogRepo = Substitute.For<IContractChangelogRepository>();
        var healthRepo = Substitute.For<IContractHealthScoreRepository>();

        svcRepo.ListAllAsync(Arg.Any<CancellationToken>()).Returns(services);

        // First call → current window, second call → comparison window
        changelogRepo.ListByTenantInPeriodAsync(
                Arg.Any<string>(), Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns(currentChangelogs, compChangelogs);

        healthRepo.ListBelowThresholdAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(healthScores);

        return new GetServiceApiGrowthReport.Handler(svcRepo, changelogRepo, healthRepo, CreateClock());
    }

    private static GetServiceApiGrowthReport.Query DefaultQuery()
        => new(TenantId: TenantId, LookbackDays: 30, ComparisonPeriodDays: 90);

    // ── Empty: no services ─────────────────────────────────────────────────

    [Fact]
    public async Task Handle_NoServices_ReturnsEmptyReport()
    {
        var handler = CreateHandler([], [], [], []);
        var result = await handler.Handle(DefaultQuery(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var r = result.Value;
        Assert.Equal(0, r.TotalServicesAnalyzed);
        Assert.Empty(r.AllServices);
    }

    // ── Empty: services exist but no changelogs ────────────────────────────

    [Fact]
    public async Task Handle_ServicesExistButNoChangelogs_ReturnsEmptyReport()
    {
        var svc = MakeSvc("svc-no-activity");
        var handler = CreateHandler([svc], [], [], []);
        var result = await handler.Handle(DefaultQuery(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(0, result.Value.TotalServicesAnalyzed);
    }

    // ── Stable: same number of contracts in both periods ──────────────────

    [Fact]
    public async Task Handle_SameContractsBothPeriods_ClassifiesAsStable()
    {
        var svc = MakeSvc("svc-stable");
        var apiId = Guid.NewGuid().ToString();

        // 1 distinct ApiAsset in both periods
        var current = new[] { MakeChangelog(TenantId, "svc-stable", apiId, FixedNow.AddDays(-5)) };
        var comp = new[] { MakeChangelog(TenantId, "svc-stable", apiId, FixedNow.AddDays(-50)) };

        var handler = CreateHandler([svc], current, comp, []);
        var result = await handler.Handle(DefaultQuery(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var entry = result.Value.AllServices.Single(e => e.ServiceName == "svc-stable");
        Assert.Equal(GetServiceApiGrowthReport.GrowthTier.Stable, entry.Tier);
        Assert.Equal(0m, entry.GrowthRatePct);
    }

    // ── Growing: 10–49% growth ────────────────────────────────────────────

    [Fact]
    public async Task Handle_ModerateGrowth_ClassifiesAsGrowing()
    {
        var svc = MakeSvc("svc-growing");
        // 2 current, 1 previous → GrowthRatePct = 100% → that's RapidGrowth
        // To get Growing (10–49%), use 3 current vs 2 previous → 50% but we need <50
        // Use 12 current vs 10 previous → 20% growth
        var currentLogs = Enumerable.Range(0, 12)
            .Select(i => MakeChangelog(TenantId, "svc-growing", $"api-{i}", FixedNow.AddDays(-5)))
            .ToList();
        var compLogs = Enumerable.Range(0, 10)
            .Select(i => MakeChangelog(TenantId, "svc-growing", $"api-{i}", FixedNow.AddDays(-50)))
            .ToList();

        var handler = CreateHandler([svc], currentLogs, compLogs, []);
        var result = await handler.Handle(DefaultQuery(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var entry = result.Value.AllServices.Single(e => e.ServiceName == "svc-growing");
        Assert.Equal(GetServiceApiGrowthReport.GrowthTier.Growing, entry.Tier);
        Assert.Equal(20m, entry.GrowthRatePct);
    }

    // ── RapidGrowth: 50–100% growth ──────────────────────────────────────

    [Fact]
    public async Task Handle_HighGrowth_ClassifiesAsRapidGrowth()
    {
        var svc = MakeSvc("svc-rapid");
        // 3 current vs 2 previous → 50% growth → exactly at RapidGrowth boundary
        var current = Enumerable.Range(0, 3)
            .Select(i => MakeChangelog(TenantId, "svc-rapid", $"api-{i}", FixedNow.AddDays(-5)))
            .ToList();
        var comp = Enumerable.Range(0, 2)
            .Select(i => MakeChangelog(TenantId, "svc-rapid", $"api-{i}", FixedNow.AddDays(-50)))
            .ToList();

        var handler = CreateHandler([svc], current, comp, []);
        var result = await handler.Handle(DefaultQuery(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var entry = result.Value.AllServices.Single(e => e.ServiceName == "svc-rapid");
        Assert.Equal(GetServiceApiGrowthReport.GrowthTier.RapidGrowth, entry.Tier);
        Assert.Equal(50m, entry.GrowthRatePct);
    }

    // ── Exploding: >100% growth ───────────────────────────────────────────

    [Fact]
    public async Task Handle_ExtremeGrowth_ClassifiesAsExploding()
    {
        var svc = MakeSvc("svc-exploding");
        // 5 current vs 2 previous → 150% → Exploding
        var current = Enumerable.Range(0, 5)
            .Select(i => MakeChangelog(TenantId, "svc-exploding", $"api-{i}", FixedNow.AddDays(-5)))
            .ToList();
        var comp = Enumerable.Range(0, 2)
            .Select(i => MakeChangelog(TenantId, "svc-exploding", $"api-{i}", FixedNow.AddDays(-50)))
            .ToList();

        var handler = CreateHandler([svc], current, comp, []);
        var result = await handler.Handle(DefaultQuery(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var entry = result.Value.AllServices.Single(e => e.ServiceName == "svc-exploding");
        Assert.Equal(GetServiceApiGrowthReport.GrowthTier.Exploding, entry.Tier);
        Assert.Equal(150m, entry.GrowthRatePct);
    }

    // ── Shrinking: negative growth ────────────────────────────────────────

    [Fact]
    public async Task Handle_NegativeGrowth_ClassifiesAsShrinking()
    {
        var svc = MakeSvc("svc-shrinking");
        // 2 current vs 4 previous → -50%
        var current = Enumerable.Range(0, 2)
            .Select(i => MakeChangelog(TenantId, "svc-shrinking", $"api-{i}", FixedNow.AddDays(-5)))
            .ToList();
        var comp = Enumerable.Range(0, 4)
            .Select(i => MakeChangelog(TenantId, "svc-shrinking", $"api-{i}", FixedNow.AddDays(-50)))
            .ToList();

        var handler = CreateHandler([svc], current, comp, []);
        var result = await handler.Handle(DefaultQuery(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var entry = result.Value.AllServices.Single(e => e.ServiceName == "svc-shrinking");
        Assert.Equal(GetServiceApiGrowthReport.GrowthTier.Shrinking, entry.Tier);
        Assert.Equal(-50m, entry.GrowthRatePct);
    }

    // ── New service: no previous contracts ───────────────────────────────

    [Fact]
    public async Task Handle_NewServiceNoPreviousPeriod_Returns100PctGrowth()
    {
        var svc = MakeSvc("svc-new");
        var current = new[] { MakeChangelog(TenantId, "svc-new", "api-1", FixedNow.AddDays(-5)) };

        var handler = CreateHandler([svc], current, [], []);
        var result = await handler.Handle(DefaultQuery(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var entry = result.Value.AllServices.Single(e => e.ServiceName == "svc-new");
        Assert.Equal(100m, entry.GrowthRatePct);
    }

    // ── GovernanceRisk: RapidGrowth + low health score ────────────────────

    [Fact]
    public async Task Handle_RapidGrowthWithLowHealthScore_FlagsGovernanceRisk()
    {
        var svc = MakeSvc("svc-risky");
        var apiId = Guid.NewGuid();

        // 3 current vs 2 previous → 50% RapidGrowth
        var current = new[]
        {
            MakeChangelog(TenantId, "svc-risky", apiId.ToString(), FixedNow.AddDays(-5)),
            MakeChangelog(TenantId, "svc-risky", Guid.NewGuid().ToString(), FixedNow.AddDays(-5)),
            MakeChangelog(TenantId, "svc-risky", Guid.NewGuid().ToString(), FixedNow.AddDays(-5))
        };
        var comp = new[]
        {
            MakeChangelog(TenantId, "svc-risky", apiId.ToString(), FixedNow.AddDays(-60)),
            MakeChangelog(TenantId, "svc-risky", Guid.NewGuid().ToString(), FixedNow.AddDays(-60))
        };

        // Low health score for the first apiId (below threshold 60)
        var lowScore = MakeHealthScore(apiId, 40);

        var handler = CreateHandler([svc], current, comp, [lowScore]);
        var result = await handler.Handle(DefaultQuery(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var entry = result.Value.AllServices.Single(e => e.ServiceName == "svc-risky");
        Assert.Equal(GetServiceApiGrowthReport.GrowthTier.RapidGrowth, entry.Tier);
        Assert.True(entry.GovernanceRisk);
        Assert.NotEmpty(result.Value.TopGovernanceRiskServices);
    }

    // ── GovernanceRisk: NOT flagged when growth is stable ────────────────

    [Fact]
    public async Task Handle_StableGrowthWithLowHealthScore_DoesNotFlagGovernanceRisk()
    {
        var svc = MakeSvc("svc-stable-ok");
        var apiId = Guid.NewGuid();

        var current = new[] { MakeChangelog(TenantId, "svc-stable-ok", apiId.ToString(), FixedNow.AddDays(-5)) };
        var comp = new[] { MakeChangelog(TenantId, "svc-stable-ok", apiId.ToString(), FixedNow.AddDays(-60)) };

        var lowScore = MakeHealthScore(apiId, 30);

        var handler = CreateHandler([svc], current, comp, [lowScore]);
        var result = await handler.Handle(DefaultQuery(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var entry = result.Value.AllServices.Single();
        Assert.Equal(GetServiceApiGrowthReport.GrowthTier.Stable, entry.Tier);
        Assert.False(entry.GovernanceRisk);
    }

    // ── Multi-service: distribution ───────────────────────────────────────

    [Fact]
    public async Task Handle_MultipleServices_CorrectTierDistribution()
    {
        var services = new[]
        {
            MakeSvc("svc-a"),
            MakeSvc("svc-b"),
            MakeSvc("svc-c")
        };

        // svc-a: stable (1 vs 1)
        // svc-b: growing (12 vs 10)
        // svc-c: new service (2 vs 0)
        var current = new List<ContractChangelog>
        {
            MakeChangelog(TenantId, "svc-a", "a-1", FixedNow.AddDays(-5)),
            MakeChangelog(TenantId, "svc-b", "b-1", FixedNow.AddDays(-5)),
            MakeChangelog(TenantId, "svc-b", "b-2", FixedNow.AddDays(-5)),
            MakeChangelog(TenantId, "svc-c", "c-1", FixedNow.AddDays(-5)),
            MakeChangelog(TenantId, "svc-c", "c-2", FixedNow.AddDays(-5)),
        };
        var comp = new List<ContractChangelog>
        {
            MakeChangelog(TenantId, "svc-a", "a-1", FixedNow.AddDays(-60)),
            MakeChangelog(TenantId, "svc-b", "b-1", FixedNow.AddDays(-60)),
        };

        var handler = CreateHandler(services, current, comp, []);
        var result = await handler.Handle(DefaultQuery(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var r = result.Value;
        Assert.Equal(3, r.TotalServicesAnalyzed);
        Assert.Equal(1, r.TierDistribution.StableCount);
        Assert.Equal(0, r.TierDistribution.GrowingCount);
        // svc-b: 2 current vs 1 previous → 100% = RapidGrowth
        // svc-c: 2 current vs 0 previous → 100% (new service formula) = RapidGrowth
        Assert.Equal(2, r.TierDistribution.RapidGrowthCount);
    }

    // ── Validator ─────────────────────────────────────────────────────────

    [Fact]
    public async Task Validator_InvalidTenantId_Fails()
    {
        var v = new GetServiceApiGrowthReport.Validator();
        var result = await v.ValidateAsync(new GetServiceApiGrowthReport.Query(TenantId: ""));
        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task Validator_LookbackDaysTooLow_Fails()
    {
        var v = new GetServiceApiGrowthReport.Validator();
        var result = await v.ValidateAsync(
            new GetServiceApiGrowthReport.Query(TenantId: TenantId, LookbackDays: 3));
        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task Validator_ValidQuery_Passes()
    {
        var v = new GetServiceApiGrowthReport.Validator();
        var result = await v.ValidateAsync(DefaultQuery());
        Assert.True(result.IsValid);
    }
}
