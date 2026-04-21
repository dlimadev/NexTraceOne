using System.Linq;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Abstractions;
using NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Features.GetBlastRadiusDistributionReport;
using NexTraceOne.ChangeGovernance.Domain.ChangeIntelligence.Entities;

namespace NexTraceOne.ChangeGovernance.Tests.ChangeIntelligence.Application.Features;

/// <summary>
/// Testes unitários para Wave Q.3 — GetBlastRadiusDistributionReport.
/// Cobre: relatório vazio, releases sem blast radius, distribuição por bucket (Zero/Small/Medium/Large),
/// top releases por total de consumidores, top serviços por avg blast radius, médias tenant-level.
/// </summary>
public sealed class BlastRadiusDistributionReportTests
{
    private static readonly DateTimeOffset FixedNow = new(2026, 4, 21, 12, 0, 0, TimeSpan.Zero);
    private static readonly Guid TenantId = Guid.NewGuid();

    private static IDateTimeProvider CreateClock()
    {
        var clock = Substitute.For<IDateTimeProvider>();
        clock.UtcNow.Returns(FixedNow);
        return clock;
    }

    private static Release MakeRelease(string serviceName, string version = "1.0.0", string env = "prod")
        => Release.Create(TenantId, Guid.NewGuid(), serviceName, version, env,
            "pipeline-ci", "abc123", FixedNow.AddDays(-1));

    private static BlastRadiusReport MakeBlastRadius(Release release, IReadOnlyList<string> direct, IReadOnlyList<string> transitive)
        => BlastRadiusReport.Calculate(release.Id, release.ApiAssetId, direct, transitive, FixedNow);

    private static GetBlastRadiusDistributionReport.Handler CreateHandler(
        IReadOnlyList<Release> releases,
        IReadOnlyDictionary<ReleaseId, BlastRadiusReport?> blastMap)
    {
        var releaseRepo = Substitute.For<IReleaseRepository>();
        var blastRepo = Substitute.For<IBlastRadiusRepository>();

        releaseRepo.ListInRangeAsync(
                Arg.Any<DateTimeOffset>(),
                Arg.Any<DateTimeOffset>(),
                Arg.Any<string?>(),
                Arg.Any<Guid>(),
                Arg.Any<CancellationToken>())
            .Returns(releases);

        foreach (var kvp in blastMap)
            blastRepo.GetByReleaseIdAsync(Arg.Is<ReleaseId>(id => id == kvp.Key), Arg.Any<CancellationToken>())
                .Returns(kvp.Value);

        return new GetBlastRadiusDistributionReport.Handler(releaseRepo, blastRepo, CreateClock());
    }

    private static GetBlastRadiusDistributionReport.Query DefaultQuery()
        => new(TenantId: TenantId.ToString(), LookbackDays: 90, MaxTopReleases: 10);

    // ── Empty report ──────────────────────────────────────────────────────

    [Fact]
    public async Task Report_Empty_When_No_Releases_In_Period()
    {
        var handler = CreateHandler([], new Dictionary<ReleaseId, BlastRadiusReport?>());
        var result = await handler.Handle(DefaultQuery(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TotalReleasesInPeriod.Should().Be(0);
        result.Value.ReleasesWithBlastRadius.Should().Be(0);
        result.Value.MaxTotalConsumers.Should().Be(0);
        result.Value.TopReleasesByBlastRadius.Should().BeEmpty();
    }

    // ── Releases without blast radius ─────────────────────────────────────

    [Fact]
    public async Task Releases_Without_BlastRadius_Counted_Correctly()
    {
        var release = MakeRelease("svc-a");
        var handler = CreateHandler(
            [release],
            new Dictionary<ReleaseId, BlastRadiusReport?> { [release.Id] = null });

        var result = await handler.Handle(DefaultQuery(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TotalReleasesInPeriod.Should().Be(1);
        result.Value.ReleasesWithBlastRadius.Should().Be(0);
        result.Value.ReleasesWithoutBlastRadius.Should().Be(1);
        result.Value.AvgTotalConsumers.Should().Be(0m);
    }

    // ── Zero bucket ───────────────────────────────────────────────────────

    [Fact]
    public async Task Release_With_No_Consumers_Goes_To_Zero_Bucket()
    {
        var release = MakeRelease("svc-isolated");
        var blast = MakeBlastRadius(release, [], []);

        var handler = CreateHandler(
            [release],
            new Dictionary<ReleaseId, BlastRadiusReport?> { [release.Id] = blast });

        var result = await handler.Handle(DefaultQuery(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.BucketDistribution.ZeroCount.Should().Be(1);
        result.Value.BucketDistribution.SmallCount.Should().Be(0);
        result.Value.AvgTotalConsumers.Should().Be(0m);
    }

    // ── Small bucket (1-5) ────────────────────────────────────────────────

    [Fact]
    public async Task Release_With_3_Total_Consumers_Goes_To_Small_Bucket()
    {
        var release = MakeRelease("svc-small");
        var blast = MakeBlastRadius(release, ["c1", "c2"], ["c3"]);

        var handler = CreateHandler(
            [release],
            new Dictionary<ReleaseId, BlastRadiusReport?> { [release.Id] = blast });

        var result = await handler.Handle(DefaultQuery(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.BucketDistribution.SmallCount.Should().Be(1);
        result.Value.BucketDistribution.ZeroCount.Should().Be(0);
        result.Value.AvgTotalConsumers.Should().Be(3m);
    }

    // ── Medium bucket (6-20) ──────────────────────────────────────────────

    [Fact]
    public async Task Release_With_10_Total_Consumers_Goes_To_Medium_Bucket()
    {
        var release = MakeRelease("svc-medium");
        var direct = Enumerable.Range(1, 5).Select(i => $"direct-{i}").ToList();
        var transitive = Enumerable.Range(1, 5).Select(i => $"transitive-{i}").ToList();
        var blast = MakeBlastRadius(release, direct, transitive);

        var handler = CreateHandler(
            [release],
            new Dictionary<ReleaseId, BlastRadiusReport?> { [release.Id] = blast });

        var result = await handler.Handle(DefaultQuery(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.BucketDistribution.MediumCount.Should().Be(1);
        result.Value.MaxTotalConsumers.Should().Be(10);
    }

    // ── Large bucket (>20) ────────────────────────────────────────────────

    [Fact]
    public async Task Release_With_25_Total_Consumers_Goes_To_Large_Bucket()
    {
        var release = MakeRelease("svc-large");
        var direct = Enumerable.Range(1, 15).Select(i => $"d-{i}").ToList();
        var transitive = Enumerable.Range(1, 10).Select(i => $"t-{i}").ToList();
        var blast = MakeBlastRadius(release, direct, transitive);

        var handler = CreateHandler(
            [release],
            new Dictionary<ReleaseId, BlastRadiusReport?> { [release.Id] = blast });

        var result = await handler.Handle(DefaultQuery(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.BucketDistribution.LargeCount.Should().Be(1);
        result.Value.MaxTotalConsumers.Should().Be(25);
    }

    // ── Multiple releases distribution ────────────────────────────────────

    [Fact]
    public async Task Multiple_Releases_Distributed_To_Correct_Buckets()
    {
        var r1 = MakeRelease("svc-1"); // Zero
        var r2 = MakeRelease("svc-2"); // Small (3)
        var r3 = MakeRelease("svc-3"); // Large (21)

        var b1 = MakeBlastRadius(r1, [], []);
        var b2 = MakeBlastRadius(r2, ["a", "b"], ["c"]);
        var b3 = MakeBlastRadius(r3,
            Enumerable.Range(1, 11).Select(i => $"d-{i}").ToList(),
            Enumerable.Range(1, 10).Select(i => $"t-{i}").ToList());

        var handler = CreateHandler(
            [r1, r2, r3],
            new Dictionary<ReleaseId, BlastRadiusReport?>
            {
                [r1.Id] = b1, [r2.Id] = b2, [r3.Id] = b3
            });

        var result = await handler.Handle(DefaultQuery(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TotalReleasesInPeriod.Should().Be(3);
        result.Value.ReleasesWithBlastRadius.Should().Be(3);
        result.Value.BucketDistribution.ZeroCount.Should().Be(1);
        result.Value.BucketDistribution.SmallCount.Should().Be(1);
        result.Value.BucketDistribution.LargeCount.Should().Be(1);
    }

    // ── TopReleases ordered ───────────────────────────────────────────────

    [Fact]
    public async Task TopReleases_Ordered_By_TotalConsumers_Descending()
    {
        var r1 = MakeRelease("svc-low");
        var r2 = MakeRelease("svc-high");

        var b1 = MakeBlastRadius(r1, ["a"], []);
        var b2 = MakeBlastRadius(r2, ["a", "b", "c"], ["d", "e"]);

        var handler = CreateHandler(
            [r1, r2],
            new Dictionary<ReleaseId, BlastRadiusReport?> { [r1.Id] = b1, [r2.Id] = b2 });

        var result = await handler.Handle(DefaultQuery(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TopReleasesByBlastRadius.First().ServiceName.Should().Be("svc-high");
        result.Value.TopReleasesByBlastRadius.First().TotalConsumers.Should().Be(5);
    }

    // ── MaxTopReleases cap ────────────────────────────────────────────────

    [Fact]
    public async Task TopReleases_Capped_By_MaxTopReleases()
    {
        var releases = Enumerable.Range(1, 15).Select(i => MakeRelease($"svc-{i}")).ToList();
        var blastMap = releases.ToDictionary(
            r => r.Id,
            r => (BlastRadiusReport?)MakeBlastRadius(r, ["c1"], ["c2"]));

        var handler = CreateHandler(releases, blastMap);
        var result = await handler.Handle(
            new GetBlastRadiusDistributionReport.Query(TenantId: TenantId.ToString(), MaxTopReleases: 5),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TopReleasesByBlastRadius.Count.Should().BeLessThanOrEqualTo(5);
    }

    // ── Avg consumers ─────────────────────────────────────────────────────

    [Fact]
    public async Task AvgTotalConsumers_Computed_Correctly()
    {
        var r1 = MakeRelease("svc-a");
        var r2 = MakeRelease("svc-b");

        var b1 = MakeBlastRadius(r1, ["a", "b"], []); // 2
        var b2 = MakeBlastRadius(r2, ["c"], ["d", "e", "f"]); // 4

        var handler = CreateHandler(
            [r1, r2],
            new Dictionary<ReleaseId, BlastRadiusReport?> { [r1.Id] = b1, [r2.Id] = b2 });

        var result = await handler.Handle(DefaultQuery(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.AvgTotalConsumers.Should().BeApproximately(3m, 0.1m);
    }

    // ── TopServicesByAvgBlastRadius ───────────────────────────────────────

    [Fact]
    public async Task TopServicesByAvgBlastRadius_Aggregates_Same_Service_Across_Releases()
    {
        var r1 = MakeRelease("svc-shared", "1.0.0");
        var r2 = MakeRelease("svc-shared", "2.0.0");
        var r3 = MakeRelease("svc-other");

        var b1 = MakeBlastRadius(r1, ["a", "b", "c", "d"], []); // 4
        var b2 = MakeBlastRadius(r2, ["a", "b", "c", "d", "e", "f"], []); // 6
        var b3 = MakeBlastRadius(r3, ["x"], []); // 1

        var handler = CreateHandler(
            [r1, r2, r3],
            new Dictionary<ReleaseId, BlastRadiusReport?> { [r1.Id] = b1, [r2.Id] = b2, [r3.Id] = b3 });

        var result = await handler.Handle(DefaultQuery(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TopServicesByAvgBlastRadius.First().ServiceName.Should().Be("svc-shared");
        result.Value.TopServicesByAvgBlastRadius.First().ReleaseCount.Should().Be(2);
        result.Value.TopServicesByAvgBlastRadius.First().MaxTotalConsumers.Should().Be(6);
    }

    // ── Validator ─────────────────────────────────────────────────────────

    [Fact]
    public void Validator_Rejects_Empty_TenantId()
    {
        var validator = new GetBlastRadiusDistributionReport.Validator();
        var r = validator.Validate(new GetBlastRadiusDistributionReport.Query(TenantId: "", LookbackDays: 90));
        r.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validator_Rejects_LookbackDays_Zero()
    {
        var validator = new GetBlastRadiusDistributionReport.Validator();
        var r = validator.Validate(new GetBlastRadiusDistributionReport.Query(TenantId: TenantId.ToString(), LookbackDays: 0));
        r.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validator_Accepts_Valid_Query()
    {
        var validator = new GetBlastRadiusDistributionReport.Validator();
        var r = validator.Validate(DefaultQuery());
        r.IsValid.Should().BeTrue();
    }

    // ── GeneratedAt ───────────────────────────────────────────────────────

    [Fact]
    public async Task Report_GeneratedAt_Matches_Clock()
    {
        var handler = CreateHandler([], new Dictionary<ReleaseId, BlastRadiusReport?>());
        var result = await handler.Handle(DefaultQuery(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.GeneratedAt.Should().Be(FixedNow);
    }

    // ── LookbackDays ─────────────────────────────────────────────────────

    [Fact]
    public async Task LookbackDays_Set_On_Report()
    {
        var handler = CreateHandler([], new Dictionary<ReleaseId, BlastRadiusReport?>());
        var result = await handler.Handle(
            new GetBlastRadiusDistributionReport.Query(TenantId: TenantId.ToString(), LookbackDays: 30),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.LookbackDays.Should().Be(30);
    }

    // ── DirectConsumers vs TotalConsumers ────────────────────────────────

    [Fact]
    public async Task Direct_And_Transitive_Consumers_Recorded_Separately()
    {
        var release = MakeRelease("svc-check");
        var blast = MakeBlastRadius(release, ["a", "b"], ["c", "d", "e"]);

        var handler = CreateHandler(
            [release],
            new Dictionary<ReleaseId, BlastRadiusReport?> { [release.Id] = blast });

        var result = await handler.Handle(DefaultQuery(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var entry = result.Value.TopReleasesByBlastRadius.Single();
        entry.DirectConsumers.Should().Be(2);
        entry.TransitiveConsumers.Should().Be(3);
        entry.TotalConsumers.Should().Be(5);
    }
}
