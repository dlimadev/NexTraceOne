using System.Linq;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.OperationalIntelligence.Application.FinOps.Features.GetCostPerReleaseReport;

namespace NexTraceOne.OperationalIntelligence.Tests.Cost;

/// <summary>
/// Testes unitários para Wave AG.2 — GetCostPerReleaseReport.
/// Cobre CostImpactTier, CostDeltaPct, WastedDeploymentCost, ReleaseCostSummary e Validator.
/// </summary>
public sealed class WaveAgCostPerReleaseReportTests
{
    private const string TenantId = "tenant-ag2";

    private static IDateTimeProvider CreateClock()
    {
        var clock = Substitute.For<IDateTimeProvider>();
        clock.UtcNow.Returns(new DateTimeOffset(2026, 4, 22, 0, 0, 0, TimeSpan.Zero));
        return clock;
    }

    private static GetCostPerReleaseReport.Handler CreateHandler(
        GetCostPerReleaseReport.ICostPerReleaseReader reader)
        => new(reader, CreateClock());

    private static GetCostPerReleaseReport.ICostPerReleaseReader EmptyReader()
    {
        var r = Substitute.For<GetCostPerReleaseReport.ICostPerReleaseReader>();
        r.ListByTenantAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<int>(),
            Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<GetCostPerReleaseReport.ReleaseCostData>>([]));
        return r;
    }

    private static GetCostPerReleaseReport.ICostPerReleaseReader ReaderWith(
        params GetCostPerReleaseReport.ReleaseCostData[] data)
    {
        var r = Substitute.For<GetCostPerReleaseReport.ICostPerReleaseReader>();
        r.ListByTenantAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<int>(),
            Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<GetCostPerReleaseReport.ReleaseCostData>>(data));
        return r;
    }

    private static GetCostPerReleaseReport.ReleaseCostData BuildRelease(
        string releaseId = "rel-1", decimal preCost = 100m, decimal postCost = 100m,
        bool failed = false)
        => new(releaseId, "svc-a", "production",
            new DateTimeOffset(2026, 4, 1, 0, 0, 0, TimeSpan.Zero),
            failed, preCost, postCost, postCost * 7);

    // ── 1. Tenant sem releases devolve relatório vazio ────────────────────

    [Fact]
    public async Task Handler_ReturnsEmptyReport_WhenNoReleases()
    {
        var result = await CreateHandler(EmptyReader())
            .Handle(new GetCostPerReleaseReport.Query(TenantId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TotalReleasesAnalyzed.Should().Be(0);
        result.Value.AllReleases.Should().BeEmpty();
        result.Value.Summary.TotalReleases.Should().Be(0);
    }

    // ── 2. Delta -20% → CostSaving ────────────────────────────────────────

    [Fact]
    public async Task Handler_CostSaving_WhenDeltaBelow_Neg10()
    {
        // pre=100, post=75 → delta=-25%
        var result = await CreateHandler(ReaderWith(BuildRelease(preCost: 100m, postCost: 75m)))
            .Handle(new GetCostPerReleaseReport.Query(TenantId), CancellationToken.None);

        result.Value.AllReleases[0].ImpactTier.Should().Be(GetCostPerReleaseReport.CostImpactTier.CostSaving);
        result.Value.AllReleases[0].CostDeltaPct.Should().BeLessThan(-10.0);
    }

    // ── 3. Delta 0% → Neutral ─────────────────────────────────────────────

    [Fact]
    public async Task Handler_Neutral_WhenDeltaIsZero()
    {
        var result = await CreateHandler(ReaderWith(BuildRelease(preCost: 100m, postCost: 100m)))
            .Handle(new GetCostPerReleaseReport.Query(TenantId), CancellationToken.None);

        result.Value.AllReleases[0].ImpactTier.Should().Be(GetCostPerReleaseReport.CostImpactTier.Neutral);
    }

    // ── 4. Delta +20% → MinorIncrease ─────────────────────────────────────

    [Fact]
    public async Task Handler_MinorIncrease_WhenDeltaBetween10And30()
    {
        // pre=100, post=120 → delta=20%
        var result = await CreateHandler(ReaderWith(BuildRelease(preCost: 100m, postCost: 120m)))
            .Handle(new GetCostPerReleaseReport.Query(TenantId), CancellationToken.None);

        result.Value.AllReleases[0].ImpactTier.Should().Be(GetCostPerReleaseReport.CostImpactTier.MinorIncrease);
    }

    // ── 5. Delta +60% → MajorIncrease ─────────────────────────────────────

    [Fact]
    public async Task Handler_MajorIncrease_WhenDeltaBetween30And100()
    {
        // pre=100, post=160 → delta=60%
        var result = await CreateHandler(ReaderWith(BuildRelease(preCost: 100m, postCost: 160m)))
            .Handle(new GetCostPerReleaseReport.Query(TenantId), CancellationToken.None);

        result.Value.AllReleases[0].ImpactTier.Should().Be(GetCostPerReleaseReport.CostImpactTier.MajorIncrease);
    }

    // ── 6. Delta +110% → CostSpike ────────────────────────────────────────

    [Fact]
    public async Task Handler_CostSpike_WhenDeltaAbove100()
    {
        // pre=100, post=210 → delta=110%
        var result = await CreateHandler(ReaderWith(BuildRelease(preCost: 100m, postCost: 210m)))
            .Handle(new GetCostPerReleaseReport.Query(TenantId), CancellationToken.None);

        result.Value.AllReleases[0].ImpactTier.Should().Be(GetCostPerReleaseReport.CostImpactTier.CostSpike);
    }

    // ── 7. Failed deploy + CostSpike → WastedDeploymentCost=true ─────────

    [Fact]
    public async Task Handler_WastedDeploymentCost_WhenFailedAndCostSpike()
    {
        var data = BuildRelease(preCost: 100m, postCost: 250m, failed: true);
        var result = await CreateHandler(ReaderWith(data))
            .Handle(new GetCostPerReleaseReport.Query(TenantId), CancellationToken.None);

        result.Value.AllReleases[0].WastedDeploymentCost.Should().BeTrue();
    }

    // ── 8. Failed deploy + Neutral → WastedDeploymentCost=false ──────────

    [Fact]
    public async Task Handler_WastedDeploymentCost_False_WhenFailedButNotSpike()
    {
        var data = BuildRelease(preCost: 100m, postCost: 100m, failed: true);
        var result = await CreateHandler(ReaderWith(data))
            .Handle(new GetCostPerReleaseReport.Query(TenantId), CancellationToken.None);

        result.Value.AllReleases[0].WastedDeploymentCost.Should().BeFalse();
    }

    // ── 9. Summary.CostSpikeRatePct é correcta ────────────────────────────

    [Fact]
    public async Task Handler_Summary_CostSpikeRatePct_IsCorrect()
    {
        var data = new[]
        {
            BuildRelease("r1", 100m, 210m),  // spike
            BuildRelease("r2", 100m, 100m),  // neutral
            BuildRelease("r3", 100m, 100m),  // neutral
            BuildRelease("r4", 100m, 100m)   // neutral
        };
        var result = await CreateHandler(ReaderWith(data))
            .Handle(new GetCostPerReleaseReport.Query(TenantId), CancellationToken.None);

        result.Value.Summary.CostSpikeCount.Should().Be(1);
        result.Value.Summary.CostSpikeRatePct.Should().Be(25.0);
    }

    // ── 10. TotalWastedDeploymentCostUsd conta só failed+spike ────────────

    [Fact]
    public async Task Handler_TotalWastedDeploymentCostUsd_CountsOnlyFailedSpikes()
    {
        var data = new[]
        {
            BuildRelease("r1", 100m, 250m, failed: true),   // wasted
            BuildRelease("r2", 100m, 250m, failed: false),  // spike but not failed
            BuildRelease("r3", 100m, 100m, failed: true)    // failed but not spike
        };
        var result = await CreateHandler(ReaderWith(data))
            .Handle(new GetCostPerReleaseReport.Query(TenantId), CancellationToken.None);

        result.Value.Summary.TotalWastedDeploymentCostUsd.Should().BeGreaterThan(0m);
        // Only r1 should contribute
        result.Value.AllReleases.Where(e => e.WastedDeploymentCost).Should().HaveCount(1);
    }

    // ── 11. TopCostSpikeReleases respeita TopReleasesCount ────────────────

    [Fact]
    public async Task Handler_TopCostSpikeReleases_RespectsCount()
    {
        var data = Enumerable.Range(1, 20)
            .Select(i => BuildRelease($"r{i}", 100m, 210m + i))
            .ToArray();

        var result = await CreateHandler(ReaderWith(data))
            .Handle(new GetCostPerReleaseReport.Query(TenantId, TopReleasesCount: 5), CancellationToken.None);

        result.Value.TopCostSpikeReleases.Count.Should().Be(5);
    }

    // ── 12. Validator: LookbackDays fora do range é inválido ──────────────

    [Theory]
    [InlineData(0)]
    [InlineData(6)]
    [InlineData(91)]
    public void Validator_Fails_WhenLookbackDaysOutOfRange(int days)
    {
        var validator = new GetCostPerReleaseReport.Validator();
        var result = validator.Validate(new GetCostPerReleaseReport.Query(TenantId, LookbackDays: days));
        result.IsValid.Should().BeFalse();
    }

    // ── 13. Null reader devolve relatório vazio sem erro ───────────────────

    [Fact]
    public async Task Handler_NullReader_ReturnsEmptyReport()
    {
        var handler = new GetCostPerReleaseReport.Handler(
            new GetCostPerReleaseReport.NullCostPerReleaseReader(), CreateClock());
        var result = await handler.Handle(
            new GetCostPerReleaseReport.Query(TenantId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TotalReleasesAnalyzed.Should().Be(0);
    }

    // ── 14. Pre-release avg = 0 → delta calculado para releases novas ─────

    [Fact]
    public async Task Handler_ZeroPreReleaseCost_ProducesCorrectBehavior()
    {
        var data = new GetCostPerReleaseReport.ReleaseCostData(
            "r-new", "svc-new", "production",
            new DateTimeOffset(2026, 4, 1, 0, 0, 0, TimeSpan.Zero),
            false, 0m, 100m, 700m);

        var result = await CreateHandler(ReaderWith(data))
            .Handle(new GetCostPerReleaseReport.Query(TenantId), CancellationToken.None);

        // With zero pre-cost and non-zero post, tier should be CostSpike
        result.Value.AllReleases[0].ImpactTier.Should().Be(GetCostPerReleaseReport.CostImpactTier.CostSpike);
    }

    // ── 15. Validator: TenantId vazio é inválido ───────────────────────────

    [Fact]
    public void Validator_Fails_WhenTenantIdEmpty()
    {
        var validator = new GetCostPerReleaseReport.Validator();
        var result = validator.Validate(new GetCostPerReleaseReport.Query(string.Empty));
        result.IsValid.Should().BeFalse();
    }
}
