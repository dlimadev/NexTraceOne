using NexTraceOne.Catalog.Application.Contracts.Abstractions;
using NexTraceOne.Catalog.Application.Contracts.Features.GetContractAdoptionReport;
using NexTraceOne.Catalog.Domain.Contracts.Entities;
using NexTraceOne.Catalog.Domain.Contracts.Enums;

namespace NexTraceOne.Catalog.Tests.Contracts.Application.Features;

/// <summary>
/// Testes unitários para Wave S.2 — GetContractAdoptionReport.
/// Cobre: sem contratos, asset com única versão estável (Complete), asset com versões mixed (InProgress/Lagging),
/// asset apenas com draft (NoConsumers), global obsolete ratio, top lagging ordering, validator.
/// </summary>
public sealed class ContractAdoptionReportTests
{
    private static ContractVersion MakeVersion(
        Guid assetId,
        string semVer,
        ContractLifecycleState state)
    {
        var importResult = ContractVersion.Import(
            apiAssetId: assetId,
            semVer: semVer,
            specContent: "{\"openapi\":\"3.0.0\"}",
            format: "json",
            importedFrom: "test",
            protocol: ContractProtocol.OpenApi);

        importResult.IsSuccess.Should().BeTrue();
        var version = importResult.Value;

        if (state == ContractLifecycleState.Draft) return version;
        version.TransitionTo(ContractLifecycleState.InReview, DateTimeOffset.UtcNow).IsSuccess.Should().BeTrue();
        if (state == ContractLifecycleState.InReview) return version;
        version.TransitionTo(ContractLifecycleState.Approved, DateTimeOffset.UtcNow).IsSuccess.Should().BeTrue();
        if (state == ContractLifecycleState.Approved) return version;
        version.TransitionTo(ContractLifecycleState.Locked, DateTimeOffset.UtcNow).IsSuccess.Should().BeTrue();
        if (state == ContractLifecycleState.Locked) return version;
        version.TransitionTo(ContractLifecycleState.Deprecated, DateTimeOffset.UtcNow).IsSuccess.Should().BeTrue();
        if (state == ContractLifecycleState.Deprecated) return version;
        version.TransitionTo(ContractLifecycleState.Sunset, DateTimeOffset.UtcNow).IsSuccess.Should().BeTrue();
        if (state == ContractLifecycleState.Sunset) return version;
        version.TransitionTo(ContractLifecycleState.Retired, DateTimeOffset.UtcNow).IsSuccess.Should().BeTrue();
        return version;
    }

    private static IContractVersionRepository MakeRepo(IReadOnlyList<ContractVersion> versions)
    {
        var summary = new ContractSummaryData(
            TotalVersions: versions.Count,
            DistinctContracts: versions.Select(v => v.ApiAssetId).Distinct().Count(),
            DraftCount: versions.Count(v => v.LifecycleState == ContractLifecycleState.Draft),
            InReviewCount: 0, ApprovedCount: 0, LockedCount: 0, DeprecatedCount: 0,
            ByProtocol: []);

        var repo = Substitute.For<IContractVersionRepository>();
        repo.GetSummaryAsync(Arg.Any<CancellationToken>()).Returns(summary);
        repo.SearchAsync(null, null, null, null, 1, Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns((versions, versions.Count));

        return repo;
    }

    // ── Empty: no versions ────────────────────────────────────────────────

    [Fact]
    public async Task Handle_NoVersions_ReturnsEmptyReport()
    {
        var repo = MakeRepo([]);
        var handler = new GetContractAdoptionReport.Handler(repo);
        var result = await handler.Handle(new GetContractAdoptionReport.Query(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var r = result.Value;
        Assert.Equal(0, r.TotalDistinctAssets);
        Assert.Empty(r.AllContracts);
        Assert.Empty(r.TopLaggingContracts);
        Assert.Equal(0m, r.GlobalObsoleteRatioPct);
    }

    // ── Single stable version → Complete ─────────────────────────────────

    [Fact]
    public async Task Handle_SingleApprovedVersion_CompleteTier()
    {
        var assetId = Guid.NewGuid();
        var version = MakeVersion(assetId, "1.0.0", ContractLifecycleState.Approved);
        var repo = MakeRepo([version]);
        var handler = new GetContractAdoptionReport.Handler(repo);
        var result = await handler.Handle(new GetContractAdoptionReport.Query(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var r = result.Value;
        Assert.Equal(1, r.TotalDistinctAssets);
        Assert.Equal(1, r.AssetsWithStableVersion);
        Assert.Equal(0, r.AssetsWithoutStableVersion);
        Assert.Equal(GetContractAdoptionReport.MigrationTier.Complete, r.AllContracts.Single().MigrationTier);
        Assert.Equal(1, r.TierDistribution.CompleteCount);
        Assert.Equal(0m, r.GlobalObsoleteRatioPct);
    }

    // ── Locked version also counts as stable → Complete ───────────────────

    [Fact]
    public async Task Handle_LockedVersion_CountsAsStable()
    {
        var assetId = Guid.NewGuid();
        var version = MakeVersion(assetId, "2.0.0", ContractLifecycleState.Locked);
        var repo = MakeRepo([version]);
        var handler = new GetContractAdoptionReport.Handler(repo);
        var result = await handler.Handle(new GetContractAdoptionReport.Query(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(GetContractAdoptionReport.MigrationTier.Complete, result.Value.AllContracts.Single().MigrationTier);
    }

    // ── Draft-only version → NoConsumers ──────────────────────────────────

    [Fact]
    public async Task Handle_DraftOnlyVersion_NoConsumersTier()
    {
        var assetId = Guid.NewGuid();
        var version = MakeVersion(assetId, "0.1.0", ContractLifecycleState.Draft);
        var repo = MakeRepo([version]);
        var handler = new GetContractAdoptionReport.Handler(repo);
        var result = await handler.Handle(new GetContractAdoptionReport.Query(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var r = result.Value;
        Assert.Equal(GetContractAdoptionReport.MigrationTier.NoConsumers, r.AllContracts.Single().MigrationTier);
        Assert.Equal(1, r.TierDistribution.NoConsumersCount);
        Assert.Equal(0, r.AssetsWithStableVersion);
        Assert.Equal(1, r.AssetsWithoutStableVersion);
    }

    // ── InProgress: stable + few deprecated ───────────────────────────────

    [Fact]
    public async Task Handle_OneStableOnDeprecated_InProgressTier()
    {
        var assetId = Guid.NewGuid();
        var approved = MakeVersion(assetId, "2.0.0", ContractLifecycleState.Approved);
        var deprecated = MakeVersion(assetId, "1.0.0", ContractLifecycleState.Deprecated);
        var repo = MakeRepo([approved, deprecated]);
        var handler = new GetContractAdoptionReport.Handler(repo);
        var result = await handler.Handle(new GetContractAdoptionReport.Query(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var entry = result.Value.AllContracts.Single();
        Assert.Equal(GetContractAdoptionReport.MigrationTier.InProgress, entry.MigrationTier);
        Assert.Equal(1, entry.StableVersionCount);
        Assert.Equal(1, entry.ObsoleteVersionCount);
        Assert.Equal(1, result.Value.TierDistribution.InProgressCount);
    }

    // ── Lagging: majority deprecated ──────────────────────────────────────

    [Fact]
    public async Task Handle_MajorityObsolete_LaggingTier()
    {
        var assetId = Guid.NewGuid();
        var approved = MakeVersion(assetId, "5.0.0", ContractLifecycleState.Approved);
        var dep1 = MakeVersion(assetId, "1.0.0", ContractLifecycleState.Deprecated);
        var dep2 = MakeVersion(assetId, "2.0.0", ContractLifecycleState.Deprecated);
        var dep3 = MakeVersion(assetId, "3.0.0", ContractLifecycleState.Sunset);
        var repo = MakeRepo([approved, dep1, dep2, dep3]);
        var handler = new GetContractAdoptionReport.Handler(repo);
        var result = await handler.Handle(new GetContractAdoptionReport.Query(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var entry = result.Value.AllContracts.Single();
        // 3/4 = 75% obsolete → Lagging
        Assert.Equal(GetContractAdoptionReport.MigrationTier.Lagging, entry.MigrationTier);
        Assert.Equal(1, result.Value.TierDistribution.LaggingCount);
    }

    // ── Multiple assets: distribution ────────────────────────────────────

    [Fact]
    public async Task Handle_MultipleAssets_CorrectDistribution()
    {
        var asset1 = Guid.NewGuid();
        var asset2 = Guid.NewGuid();
        var asset3 = Guid.NewGuid();

        var versions = new List<ContractVersion>
        {
            MakeVersion(asset1, "1.0.0", ContractLifecycleState.Approved),                        // Complete
            MakeVersion(asset2, "2.0.0", ContractLifecycleState.Approved),                        // InProgress
            MakeVersion(asset2, "1.0.0", ContractLifecycleState.Deprecated),
            MakeVersion(asset3, "0.5.0", ContractLifecycleState.Draft),                           // NoConsumers
        };

        var repo = MakeRepo(versions);
        var handler = new GetContractAdoptionReport.Handler(repo);
        var result = await handler.Handle(new GetContractAdoptionReport.Query(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var r = result.Value;
        Assert.Equal(3, r.TotalDistinctAssets);
        Assert.Equal(1, r.TierDistribution.CompleteCount);
        Assert.Equal(1, r.TierDistribution.InProgressCount);
        Assert.Equal(1, r.TierDistribution.NoConsumersCount);
    }

    // ── GlobalObsoleteRatio calculated from all versions ──────────────────

    [Fact]
    public async Task Handle_GlobalObsoleteRatio_IsGlobal()
    {
        var assetId = Guid.NewGuid();
        var approved = MakeVersion(assetId, "2.0.0", ContractLifecycleState.Approved);
        var deprecated = MakeVersion(assetId, "1.0.0", ContractLifecycleState.Deprecated);
        var repo = MakeRepo([approved, deprecated]);
        var handler = new GetContractAdoptionReport.Handler(repo);
        var result = await handler.Handle(new GetContractAdoptionReport.Query(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        // 1/2 = 50% obsolete globally
        Assert.Equal(50m, result.Value.GlobalObsoleteRatioPct);
    }

    // ── TopLaggingContracts ordered by obsolete count ─────────────────────

    [Fact]
    public async Task Handle_TopLagging_OrderedByObsoleteCountDescending()
    {
        var asset1 = Guid.NewGuid();
        var asset2 = Guid.NewGuid();

        var versions = new List<ContractVersion>
        {
            // asset1: 1 stable + 1 deprecated (InProgress)
            MakeVersion(asset1, "2.0.0", ContractLifecycleState.Approved),
            MakeVersion(asset1, "1.0.0", ContractLifecycleState.Deprecated),
            // asset2: 1 stable + 3 deprecated (Lagging, more obsolete)
            MakeVersion(asset2, "4.0.0", ContractLifecycleState.Approved),
            MakeVersion(asset2, "1.0.0", ContractLifecycleState.Deprecated),
            MakeVersion(asset2, "2.0.0", ContractLifecycleState.Deprecated),
            MakeVersion(asset2, "3.0.0", ContractLifecycleState.Sunset),
        };

        var repo = MakeRepo(versions);
        var handler = new GetContractAdoptionReport.Handler(repo);
        var result = await handler.Handle(new GetContractAdoptionReport.Query(TopLaggingCount: 5), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var top = result.Value.TopLaggingContracts;
        Assert.True(top.Count >= 2);
        Assert.True(top[0].ObsoleteVersionCount >= top[1].ObsoleteVersionCount);
        Assert.Equal(asset2, top[0].ApiAssetId);
    }

    // ── Validator: invalid TopLaggingCount ───────────────────────────────

    [Theory]
    [InlineData(0)]
    [InlineData(51)]
    public void Validator_TopLaggingCount_OutOfRange_Fails(int count)
    {
        var validator = new GetContractAdoptionReport.Validator();
        var result = validator.Validate(new GetContractAdoptionReport.Query(TopLaggingCount: count));
        Assert.False(result.IsValid);
    }

    // ── Validator: valid query passes ─────────────────────────────────────

    [Fact]
    public void Validator_ValidQuery_Passes()
    {
        var validator = new GetContractAdoptionReport.Validator();
        var result = validator.Validate(new GetContractAdoptionReport.Query());
        Assert.True(result.IsValid);
    }
}
