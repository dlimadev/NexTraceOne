using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.Catalog.Application.Contracts.Abstractions;
using NexTraceOne.Catalog.Application.Contracts.Features.GetApiSchemaStabilityReport;
using NexTraceOne.Catalog.Domain.Contracts.Entities;
using NexTraceOne.Catalog.Domain.Contracts.Enums;
using SchemaStabilityTier = NexTraceOne.Catalog.Application.Contracts.Features.GetApiSchemaStabilityReport.GetApiSchemaStabilityReport.SchemaStabilityTier;

namespace NexTraceOne.Catalog.Tests.Contracts.Application.Features;

/// <summary>
/// Testes unitários para Wave R.2 — GetApiSchemaStabilityReport.
/// Cobre: relatório vazio, classificação de tier (Stable/Volatile/Unstable/Critical),
/// agrupamento por ApiAssetId, avg changelogs, top instáveis, top estáveis.
/// </summary>
public sealed class ApiSchemaStabilityReportTests
{
    private static readonly DateTimeOffset FixedNow = new(2026, 4, 21, 12, 0, 0, TimeSpan.Zero);
    private static readonly string TenantId = Guid.NewGuid().ToString();

    private static IDateTimeProvider CreateClock()
    {
        var clock = Substitute.For<IDateTimeProvider>();
        clock.UtcNow.Returns(FixedNow);
        return clock;
    }

    private static ContractChangelog MakeChangelog(
        string apiAssetId,
        string serviceName,
        DateTimeOffset? createdAt = null)
        => ContractChangelog.Create(
            tenantId: TenantId,
            apiAssetId: apiAssetId,
            serviceName: serviceName,
            fromVersion: "1.0.0",
            toVersion: "1.1.0",
            contractVersionId: Guid.NewGuid(),
            verificationId: null,
            source: ChangelogSource.Manual,
            entries: "[]",
            summary: "Test changelog",
            markdownContent: null,
            jsonContent: null,
            commitSha: null,
            createdAt: createdAt ?? FixedNow.AddDays(-1),
            createdBy: "user-1");

    private static GetApiSchemaStabilityReport.Handler CreateHandler(
        IReadOnlyList<ContractChangelog> changelogs)
    {
        var changelogRepo = Substitute.For<IContractChangelogRepository>();
        changelogRepo.ListByTenantInPeriodAsync(
                Arg.Any<string>(), Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(),
                Arg.Any<CancellationToken>())
            .Returns(changelogs);
        return new GetApiSchemaStabilityReport.Handler(changelogRepo, CreateClock());
    }

    private static GetApiSchemaStabilityReport.Query DefaultQuery()
        => new(TenantId: TenantId, LookbackDays: 90, TopUnstableCount: 10, VolatileThreshold: 1);

    // ── Empty: no changelogs ───────────────────────────────────────────────

    [Fact]
    public async Task Handle_NoChangelogs_ReturnsZeroTotals()
    {
        var handler = CreateHandler([]);
        var result = await handler.Handle(DefaultQuery(), CancellationToken.None);
        Assert.True(result.IsSuccess);
        var r = result.Value;
        Assert.Equal(0, r.TotalContractsAnalyzed);
        Assert.Equal(0, r.ContractsWithChanges);
        Assert.Equal(0m, r.AvgChangelogsPerContract);
        Assert.Equal(0, r.MaxChangelogsInPeriod);
        Assert.Empty(r.TopUnstableContracts);
        Assert.Empty(r.TopStableContracts);
    }

    // ── Single contract with 1 changelog → Volatile ───────────────────────

    [Fact]
    public async Task Handle_OneChangelog_ClassifiesAsVolatile()
    {
        var assetId = Guid.NewGuid().ToString();
        var handler = CreateHandler([MakeChangelog(assetId, "svc-a")]);
        var result = await handler.Handle(DefaultQuery(), CancellationToken.None);
        Assert.True(result.IsSuccess);
        var r = result.Value;
        Assert.Equal(1, r.TotalContractsAnalyzed);
        Assert.Equal(1, r.ContractsWithChanges);
        Assert.Equal(1, r.MaxChangelogsInPeriod);
        var entry = r.TopUnstableContracts.First();
        Assert.Equal(SchemaStabilityTier.Volatile, entry.StabilityTier);
    }

    // ── 3 changelogs for same contract → Unstable ─────────────────────────

    [Fact]
    public async Task Handle_ThreeChangelogs_ClassifiesAsUnstable()
    {
        var assetId = Guid.NewGuid().ToString();
        var changelogs = Enumerable.Range(1, 3)
            .Select(_ => MakeChangelog(assetId, "svc-b"))
            .ToList();
        var handler = CreateHandler(changelogs);
        var result = await handler.Handle(DefaultQuery(), CancellationToken.None);
        Assert.True(result.IsSuccess);
        var entry = result.Value.TopUnstableContracts.First();
        Assert.Equal(SchemaStabilityTier.Unstable, entry.StabilityTier);
        Assert.Equal(1, result.Value.TierDistribution.UnstableCount);
    }

    // ── 6+ changelogs → Critical ──────────────────────────────────────────

    [Fact]
    public async Task Handle_SixChangelogs_ClassifiesAsCritical()
    {
        var assetId = Guid.NewGuid().ToString();
        var changelogs = Enumerable.Range(1, 6)
            .Select(_ => MakeChangelog(assetId, "svc-c"))
            .ToList();
        var handler = CreateHandler(changelogs);
        var result = await handler.Handle(DefaultQuery(), CancellationToken.None);
        Assert.True(result.IsSuccess);
        var entry = result.Value.TopUnstableContracts.First();
        Assert.Equal(SchemaStabilityTier.Critical, entry.StabilityTier);
        Assert.Equal(1, result.Value.TierDistribution.CriticalCount);
    }

    // ── Multiple contracts grouped by ApiAssetId ──────────────────────────

    [Fact]
    public async Task Handle_MultipleContracts_GroupsByApiAssetId()
    {
        var assetA = Guid.NewGuid().ToString();
        var assetB = Guid.NewGuid().ToString();

        var changelogs = new List<ContractChangelog>
        {
            MakeChangelog(assetA, "svc-a"),
            MakeChangelog(assetA, "svc-a"),
            MakeChangelog(assetB, "svc-b")
        };

        var handler = CreateHandler(changelogs);
        var result = await handler.Handle(DefaultQuery(), CancellationToken.None);
        Assert.True(result.IsSuccess);
        var r = result.Value;
        Assert.Equal(2, r.TotalContractsAnalyzed);
        Assert.Equal(2, r.ContractsWithChanges);
        Assert.Equal(2, r.MaxChangelogsInPeriod);
        // assetA has 2 changelogs → Volatile; assetB has 1 → Volatile
        Assert.Equal(2, r.TierDistribution.VolatileCount);
    }

    // ── AvgChangelogsPerContract calculation ──────────────────────────────

    [Fact]
    public async Task Handle_AvgChangelogs_CalculatedCorrectly()
    {
        // 3 changelogs for assetA + 1 for assetB = 4 changelogs / 2 contracts = 2.0 avg
        var assetA = Guid.NewGuid().ToString();
        var assetB = Guid.NewGuid().ToString();

        var changelogs = new List<ContractChangelog>
        {
            MakeChangelog(assetA, "svc-a"),
            MakeChangelog(assetA, "svc-a"),
            MakeChangelog(assetA, "svc-a"),
            MakeChangelog(assetB, "svc-b")
        };

        var handler = CreateHandler(changelogs);
        var result = await handler.Handle(DefaultQuery(), CancellationToken.None);
        Assert.True(result.IsSuccess);
        Assert.Equal(2.00m, result.Value.AvgChangelogsPerContract);
    }

    // ── TopUnstableContracts ordered by changelog count descending ─────────

    [Fact]
    public async Task Handle_TopUnstable_OrderedByChangelogCountDescending()
    {
        var assetA = Guid.NewGuid().ToString(); // 5 changelogs
        var assetB = Guid.NewGuid().ToString(); // 2 changelogs

        var changelogs = Enumerable.Range(1, 5).Select(_ => MakeChangelog(assetA, "svc-a"))
            .Concat(Enumerable.Range(1, 2).Select(_ => MakeChangelog(assetB, "svc-b")))
            .ToList();

        var handler = CreateHandler(changelogs);
        var result = await handler.Handle(DefaultQuery(), CancellationToken.None);
        Assert.True(result.IsSuccess);
        var top = result.Value.TopUnstableContracts;
        Assert.True(top.Count >= 2);
        Assert.True(top[0].ChangelogCount >= top[1].ChangelogCount);
        Assert.Equal(assetA, top[0].ApiAssetId);
    }

    // ── TopStableContracts ordered by changelog count ascending ───────────

    [Fact]
    public async Task Handle_TopStable_OrderedByChangelogCountAscending()
    {
        var assetA = Guid.NewGuid().ToString(); // 5 changelogs
        var assetB = Guid.NewGuid().ToString(); // 1 changelog

        var changelogs = Enumerable.Range(1, 5).Select(_ => MakeChangelog(assetA, "svc-a"))
            .Concat([MakeChangelog(assetB, "svc-b")])
            .ToList();

        var handler = CreateHandler(changelogs);
        var result = await handler.Handle(DefaultQuery(), CancellationToken.None);
        Assert.True(result.IsSuccess);
        var stable = result.Value.TopStableContracts;
        Assert.True(stable[0].ChangelogCount <= stable[^1].ChangelogCount);
        Assert.Equal(assetB, stable[0].ApiAssetId);
    }

    // ── VolatileThreshold boundary: threshold=2, count=1 → Stable ────────

    [Fact]
    public async Task Handle_VolatileThreshold2_OneChangelog_ClassifiesAsStable()
    {
        var assetId = Guid.NewGuid().ToString();
        var handler = CreateHandler([MakeChangelog(assetId, "svc-x")]);
        var query = DefaultQuery() with { VolatileThreshold = 2 };
        var result = await handler.Handle(query, CancellationToken.None);
        Assert.True(result.IsSuccess);
        var entry = result.Value.TopUnstableContracts.First();
        Assert.Equal(SchemaStabilityTier.Stable, entry.StabilityTier);
    }

    // ── GeneratedAt matches clock ─────────────────────────────────────────

    [Fact]
    public async Task Handle_GeneratedAt_MatchesClock()
    {
        var handler = CreateHandler([]);
        var result = await handler.Handle(DefaultQuery(), CancellationToken.None);
        Assert.True(result.IsSuccess);
        Assert.Equal(FixedNow, result.Value.GeneratedAt);
    }

    // ── Validator: TenantId required ──────────────────────────────────────

    [Fact]
    public void Validator_EmptyTenantId_Fails()
    {
        var validator = new GetApiSchemaStabilityReport.Validator();
        var result = validator.Validate(new GetApiSchemaStabilityReport.Query(TenantId: ""));
        Assert.False(result.IsValid);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(366)]
    public void Validator_LookbackDaysOutOfRange_Fails(int days)
    {
        var validator = new GetApiSchemaStabilityReport.Validator();
        var result = validator.Validate(new GetApiSchemaStabilityReport.Query(
            TenantId: TenantId, LookbackDays: days));
        Assert.False(result.IsValid);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(11)]
    public void Validator_VolatileThresholdOutOfRange_Fails(int threshold)
    {
        var validator = new GetApiSchemaStabilityReport.Validator();
        var result = validator.Validate(new GetApiSchemaStabilityReport.Query(
            TenantId: TenantId, VolatileThreshold: threshold));
        Assert.False(result.IsValid);
    }

    [Fact]
    public void Validator_ValidQuery_Passes()
    {
        var validator = new GetApiSchemaStabilityReport.Validator();
        var result = validator.Validate(DefaultQuery());
        Assert.True(result.IsValid);
    }
}
