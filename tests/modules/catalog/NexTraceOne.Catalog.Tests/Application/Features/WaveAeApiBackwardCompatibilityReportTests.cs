using System.Linq;
using FluentAssertions;
using NSubstitute;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.Catalog.Application.Contracts.Abstractions;
using NexTraceOne.Catalog.Application.Contracts.Features.GetApiBackwardCompatibilityReport;

namespace NexTraceOne.Catalog.Tests.Application.Features;

/// <summary>
/// Testes unitários para Wave AE.3 — GetApiBackwardCompatibilityReport.
/// Cobre CompatibilityTier, BackwardCompatibilityScore, StagnationFlag,
/// TenantCompatibilityIndex, TopStable/TopVolatile, AdoptionLag penalty e Validator.
/// </summary>
public sealed class WaveAeApiBackwardCompatibilityReportTests
{
    private static readonly DateTimeOffset FixedNow = new(2026, 4, 22, 10, 0, 0, TimeSpan.Zero);
    private const string TenantId = "tenant-ae3";

    private static IDateTimeProvider CreateClock()
    {
        var clock = Substitute.For<IDateTimeProvider>();
        clock.UtcNow.Returns(FixedNow);
        return clock;
    }

    private static GetApiBackwardCompatibilityReport.Handler CreateHandler(
        IReadOnlyList<ContractCompatibilityEntry> entries)
    {
        var reader = Substitute.For<IContractCompatibilityReader>();
        reader.ListByTenantAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(entries));
        return new GetApiBackwardCompatibilityReport.Handler(reader, CreateClock());
    }

    private static ContractCompatibilityEntry MakeEntry(
        string apiId,
        string svc,
        int totalChangelogs,
        int breakingCount,
        int majorVersions = 0,
        double lagDays = 0.0,
        int daysAgoLastChange = 30) =>
        new(apiId, svc, "1.0.0", totalChangelogs, breakingCount, majorVersions,
            lagDays, FixedNow.AddDays(-daysAgoLastChange));

    private static GetApiBackwardCompatibilityReport.Query DefaultQuery()
        => new(TenantId: TenantId);

    // ── Stable tier ───────────────────────────────────────────────────────

    [Fact]
    public async Task Returns_Stable_tier_when_no_breaking_changes_and_score_high()
    {
        var entries = new List<ContractCompatibilityEntry>
        {
            MakeEntry("api-1", "svc-a", totalChangelogs: 20, breakingCount: 0)
        };
        var handler = CreateHandler(entries);
        var result = await handler.Handle(DefaultQuery(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.AllContracts.Single().CompatibilityTier
            .Should().Be(GetApiBackwardCompatibilityReport.CompatibilityTier.Stable);
        result.Value.AllContracts.Single().BackwardCompatibilityScore.Should().Be(100.0);
    }

    // ── Unstable tier ─────────────────────────────────────────────────────

    [Fact]
    public async Task Returns_Unstable_tier_when_high_breaking_rate()
    {
        var entries = new List<ContractCompatibilityEntry>
        {
            MakeEntry("api-2", "svc-b", totalChangelogs: 10, breakingCount: 8) // 80% breaking
        };
        var handler = CreateHandler(entries);
        var result = await handler.Handle(DefaultQuery(), CancellationToken.None);

        result.Value.AllContracts.Single().CompatibilityTier
            .Should().Be(GetApiBackwardCompatibilityReport.CompatibilityTier.Unstable);
        result.Value.AllContracts.Single().BackwardCompatibilityScore.Should().BeLessThan(40.0);
    }

    // ── Volatile tier ─────────────────────────────────────────────────────

    [Fact]
    public async Task Returns_Volatile_tier_when_moderate_breaking_rate()
    {
        var entries = new List<ContractCompatibilityEntry>
        {
            MakeEntry("api-3", "svc-c", totalChangelogs: 10, breakingCount: 5) // 50% breaking → score=50
        };
        var handler = CreateHandler(entries);
        var result = await handler.Handle(DefaultQuery(), CancellationToken.None);

        result.Value.AllContracts.Single().CompatibilityTier
            .Should().Be(GetApiBackwardCompatibilityReport.CompatibilityTier.Volatile);
    }

    // ── Evolving tier ─────────────────────────────────────────────────────

    [Fact]
    public async Task Returns_Evolving_tier_when_low_breaking_rate()
    {
        var entries = new List<ContractCompatibilityEntry>
        {
            MakeEntry("api-4", "svc-d", totalChangelogs: 10, breakingCount: 2) // 20% breaking → score=80
        };
        var handler = CreateHandler(entries);
        var result = await handler.Handle(DefaultQuery(), CancellationToken.None);

        var tier = result.Value.AllContracts.Single().CompatibilityTier;
        // score=80 ≥65 but breakingRate=20% ≥10% → Evolving (not Stable)
        tier.Should().Be(GetApiBackwardCompatibilityReport.CompatibilityTier.Evolving);
    }

    // ── AdoptionLag penalty ───────────────────────────────────────────────

    [Fact]
    public async Task AdoptionLag_reduces_backward_compatibility_score()
    {
        var entries = new List<ContractCompatibilityEntry>
        {
            MakeEntry("api-5", "svc-e", totalChangelogs: 10, breakingCount: 0, lagDays: 107)
            // lagPenalty = (107-7) * 0.1 = 10 → score = 100 - 10 = 90
        };
        var handler = CreateHandler(entries);
        var result = await handler.Handle(DefaultQuery(), CancellationToken.None);

        result.Value.AllContracts.Single().BackwardCompatibilityScore.Should().Be(90.0);
    }

    // ── StagnationFlag ────────────────────────────────────────────────────

    [Fact]
    public async Task StagnationFlag_is_set_for_stable_contracts_with_no_change_in_stagnation_window()
    {
        var entries = new List<ContractCompatibilityEntry>
        {
            MakeEntry("api-6", "svc-f", totalChangelogs: 20, breakingCount: 0, daysAgoLastChange: 200)
        };
        var handler = CreateHandler(entries);
        var result = await handler.Handle(
            new GetApiBackwardCompatibilityReport.Query(TenantId: TenantId, StagnationDays: 180),
            CancellationToken.None);

        result.Value.AllContracts.Single().StagnationFlag.Should().BeTrue();
        result.Value.StagnationFlagCount.Should().Be(1);
    }

    [Fact]
    public async Task StagnationFlag_is_not_set_when_recent_change_exists()
    {
        var entries = new List<ContractCompatibilityEntry>
        {
            MakeEntry("api-7", "svc-g", totalChangelogs: 20, breakingCount: 0, daysAgoLastChange: 10)
        };
        var handler = CreateHandler(entries);
        var result = await handler.Handle(
            new GetApiBackwardCompatibilityReport.Query(TenantId: TenantId, StagnationDays: 180),
            CancellationToken.None);

        result.Value.AllContracts.Single().StagnationFlag.Should().BeFalse();
    }

    // ── TenantCompatibilityIndex ──────────────────────────────────────────

    [Fact]
    public async Task TenantCompatibilityIndex_is_average_of_all_scores()
    {
        var entries = new List<ContractCompatibilityEntry>
        {
            MakeEntry("api-8", "svc-h", totalChangelogs: 10, breakingCount: 0),  // score=100
            MakeEntry("api-9", "svc-i", totalChangelogs: 10, breakingCount: 10)  // score=0
        };
        var handler = CreateHandler(entries);
        var result = await handler.Handle(DefaultQuery(), CancellationToken.None);

        result.Value.TenantCompatibilityIndex.Should().Be(50.0);
    }

    // ── TopStable / TopVolatile ───────────────────────────────────────────

    [Fact]
    public async Task TopStableContracts_ordered_by_descending_score_without_stagnation()
    {
        var entries = new List<ContractCompatibilityEntry>
        {
            MakeEntry("api-10", "svc-j", totalChangelogs: 10, breakingCount: 0, daysAgoLastChange: 10),
            MakeEntry("api-11", "svc-k", totalChangelogs: 10, breakingCount: 1, daysAgoLastChange: 10)
        };
        var handler = CreateHandler(entries);
        var result = await handler.Handle(DefaultQuery(), CancellationToken.None);

        result.Value.TopStableContracts.Should().NotBeEmpty();
        result.Value.TopStableContracts.First().ApiAssetId.Should().Be("api-10");
    }

    [Fact]
    public async Task TopVolatileContracts_ordered_by_ascending_score()
    {
        var entries = new List<ContractCompatibilityEntry>
        {
            MakeEntry("api-12", "svc-l", totalChangelogs: 10, breakingCount: 0),  // score=100
            MakeEntry("api-13", "svc-m", totalChangelogs: 10, breakingCount: 8)   // score~20
        };
        var handler = CreateHandler(entries);
        var result = await handler.Handle(DefaultQuery(), CancellationToken.None);

        result.Value.TopVolatileContracts.First().ApiAssetId.Should().Be("api-13");
    }

    // ── Empty result ──────────────────────────────────────────────────────

    [Fact]
    public async Task Returns_empty_report_when_no_entries()
    {
        var handler = CreateHandler(new List<ContractCompatibilityEntry>());
        var result = await handler.Handle(DefaultQuery(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TotalContractsAnalyzed.Should().Be(0);
        result.Value.TenantCompatibilityIndex.Should().Be(0.0);
    }

    // ── TierDistribution ──────────────────────────────────────────────────

    [Fact]
    public async Task TierDistribution_sums_to_total_contracts()
    {
        var entries = new List<ContractCompatibilityEntry>
        {
            MakeEntry("api-14", "svc-n", totalChangelogs: 10, breakingCount: 0),
            MakeEntry("api-15", "svc-o", totalChangelogs: 10, breakingCount: 9)
        };
        var handler = CreateHandler(entries);
        var result = await handler.Handle(DefaultQuery(), CancellationToken.None);

        var dist = result.Value.TierDistribution;
        (dist.StableCount + dist.EvolvingCount + dist.VolatileCount + dist.UnstableCount)
            .Should().Be(result.Value.TotalContractsAnalyzed);
    }

    // ── Validator ─────────────────────────────────────────────────────────

    [Fact]
    public void Validator_rejects_empty_tenantId()
    {
        var validator = new GetApiBackwardCompatibilityReport.Validator();
        validator.Validate(new GetApiBackwardCompatibilityReport.Query(TenantId: ""))
            .IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validator_rejects_lookback_below_minimum()
    {
        var validator = new GetApiBackwardCompatibilityReport.Validator();
        validator.Validate(
            new GetApiBackwardCompatibilityReport.Query(TenantId: TenantId, LookbackDays: 5))
            .IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validator_accepts_valid_query()
    {
        var validator = new GetApiBackwardCompatibilityReport.Validator();
        validator.Validate(DefaultQuery()).IsValid.Should().BeTrue();
    }
}
