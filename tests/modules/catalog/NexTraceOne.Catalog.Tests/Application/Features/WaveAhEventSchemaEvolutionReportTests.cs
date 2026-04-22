using FluentAssertions;
using NSubstitute;
using NexTraceOne.Catalog.Application.Contracts.Abstractions;
using NexTraceOne.Catalog.Application.Contracts.Features.GetEventSchemaEvolutionReport;

namespace NexTraceOne.Catalog.Tests.Application.Features;

/// <summary>
/// Testes unitários para Wave AH.1 — GetEventSchemaEvolutionReport.
/// Cobre EventSchemaStabilityTier, MigrationLag flag, HealthSummary,
/// TopUnstableContracts, empty tenant e Validator.
/// </summary>
public sealed class WaveAhEventSchemaEvolutionReportTests
{
    private const string TenantId = "tenant-ah1";

    private static GetEventSchemaEvolutionReport.Handler CreateHandler(
        IReadOnlyList<EventSchemaEvolutionEntry> entries)
    {
        var reader = Substitute.For<IEventSchemaEvolutionReader>();
        reader.ListByTenantAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(entries));
        return new GetEventSchemaEvolutionReport.Handler(reader);
    }

    private static EventSchemaEvolutionEntry MakeEntry(
        string id,
        string name,
        string producer,
        int totalChanges,
        int breakingChanges,
        int consumersOnOldVersion = 0,
        double schemaLagDays = 0.0) =>
        new(id, name, producer, "2.0.0", totalChanges, breakingChanges,
            consumersOnOldVersion, schemaLagDays,
            new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero));

    private static GetEventSchemaEvolutionReport.Query DefaultQuery()
        => new(TenantId: TenantId);

    // ── Stable tier ───────────────────────────────────────────────────────

    [Fact]
    public async Task Returns_Stable_tier_when_no_breaking_changes()
    {
        var entries = new List<EventSchemaEvolutionEntry>
        {
            MakeEntry("c-1", "order.created", "svc-orders", totalChanges: 20, breakingChanges: 0)
        };
        var handler = CreateHandler(entries);
        var result = await handler.Handle(DefaultQuery(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.AllContracts.Single().StabilityTier
            .Should().Be(GetEventSchemaEvolutionReport.EventSchemaStabilityTier.Stable);
        result.Value.AllContracts.Single().BreakingChangeRate.Should().Be(0.0);
    }

    // ── Evolving tier ─────────────────────────────────────────────────────

    [Fact]
    public async Task Returns_Evolving_tier_when_breaking_rate_between_5_and_20()
    {
        var entries = new List<EventSchemaEvolutionEntry>
        {
            MakeEntry("c-2", "payment.processed", "svc-payments", totalChanges: 20, breakingChanges: 2) // 10%
        };
        var handler = CreateHandler(entries);
        var result = await handler.Handle(DefaultQuery(), CancellationToken.None);

        result.Value.AllContracts.Single().StabilityTier
            .Should().Be(GetEventSchemaEvolutionReport.EventSchemaStabilityTier.Evolving);
        result.Value.AllContracts.Single().BreakingChangeRate.Should().Be(10.0);
    }

    // ── Volatile tier ─────────────────────────────────────────────────────

    [Fact]
    public async Task Returns_Volatile_tier_when_breaking_rate_between_20_and_50()
    {
        var entries = new List<EventSchemaEvolutionEntry>
        {
            MakeEntry("c-3", "inventory.updated", "svc-inventory", totalChanges: 10, breakingChanges: 3) // 30%
        };
        var handler = CreateHandler(entries);
        var result = await handler.Handle(DefaultQuery(), CancellationToken.None);

        result.Value.AllContracts.Single().StabilityTier
            .Should().Be(GetEventSchemaEvolutionReport.EventSchemaStabilityTier.Volatile);
    }

    // ── Unstable tier ─────────────────────────────────────────────────────

    [Fact]
    public async Task Returns_Unstable_tier_when_breaking_rate_above_50()
    {
        var entries = new List<EventSchemaEvolutionEntry>
        {
            MakeEntry("c-4", "notification.sent", "svc-notify", totalChanges: 10, breakingChanges: 7) // 70%
        };
        var handler = CreateHandler(entries);
        var result = await handler.Handle(DefaultQuery(), CancellationToken.None);

        result.Value.AllContracts.Single().StabilityTier
            .Should().Be(GetEventSchemaEvolutionReport.EventSchemaStabilityTier.Unstable);
    }

    // ── MigrationLag flag ─────────────────────────────────────────────────

    [Fact]
    public async Task Sets_MigrationLag_when_consumers_on_old_version_and_lag_exceeds_alert()
    {
        var entries = new List<EventSchemaEvolutionEntry>
        {
            MakeEntry("c-5", "user.registered", "svc-users",
                totalChanges: 5, breakingChanges: 0,
                consumersOnOldVersion: 2, schemaLagDays: 45.0) // > 30 days default
        };
        var handler = CreateHandler(entries);
        var result = await handler.Handle(DefaultQuery(), CancellationToken.None);

        result.Value.AllContracts.Single().MigrationLag.Should().BeTrue();
        result.Value.MigrationLagContracts.Should().HaveCount(1);
    }

    [Fact]
    public async Task Does_not_set_MigrationLag_when_lag_below_alert_threshold()
    {
        var entries = new List<EventSchemaEvolutionEntry>
        {
            MakeEntry("c-6", "user.deleted", "svc-users",
                totalChanges: 5, breakingChanges: 0,
                consumersOnOldVersion: 2, schemaLagDays: 10.0) // < 30 days default
        };
        var handler = CreateHandler(entries);
        var result = await handler.Handle(DefaultQuery(), CancellationToken.None);

        result.Value.AllContracts.Single().MigrationLag.Should().BeFalse();
    }

    [Fact]
    public async Task Does_not_set_MigrationLag_when_no_consumers_on_old_version()
    {
        var entries = new List<EventSchemaEvolutionEntry>
        {
            MakeEntry("c-7", "catalog.synced", "svc-catalog",
                totalChanges: 5, breakingChanges: 0,
                consumersOnOldVersion: 0, schemaLagDays: 60.0)
        };
        var handler = CreateHandler(entries);
        var result = await handler.Handle(DefaultQuery(), CancellationToken.None);

        result.Value.AllContracts.Single().MigrationLag.Should().BeFalse();
    }

    // ── TopUnstableContracts ──────────────────────────────────────────────

    [Fact]
    public async Task TopUnstableContracts_ordered_by_breaking_rate_desc()
    {
        var entries = new List<EventSchemaEvolutionEntry>
        {
            MakeEntry("c-8", "evt-a", "svc-a", 10, 1), // 10%
            MakeEntry("c-9", "evt-b", "svc-b", 10, 6), // 60%
            MakeEntry("c-10", "evt-c", "svc-c", 10, 3) // 30%
        };
        var handler = CreateHandler(entries);
        var result = await handler.Handle(DefaultQuery() with { TopUnstableCount = 2 }, CancellationToken.None);

        result.Value.TopUnstableContracts.Should().HaveCount(2);
        result.Value.TopUnstableContracts[0].ContractId.Should().Be("c-9"); // 60%
        result.Value.TopUnstableContracts[1].ContractId.Should().Be("c-10"); // 30%
    }

    // ── Health Summary ────────────────────────────────────────────────────

    [Fact]
    public async Task HealthSummary_counts_tiers_correctly()
    {
        var entries = new List<EventSchemaEvolutionEntry>
        {
            MakeEntry("c-11", "evt-stable", "svc-a", 20, 0),      // Stable
            MakeEntry("c-12", "evt-evolving", "svc-b", 10, 1),    // 10% Evolving
            MakeEntry("c-13", "evt-volatile", "svc-c", 10, 3),    // 30% Volatile
            MakeEntry("c-14", "evt-unstable", "svc-d", 10, 7),    // 70% Unstable
        };
        var handler = CreateHandler(entries);
        var result = await handler.Handle(DefaultQuery(), CancellationToken.None);

        var summary = result.Value.HealthSummary;
        summary.TotalContracts.Should().Be(4);
        summary.StableCount.Should().Be(1);
        summary.EvolvingCount.Should().Be(1);
        summary.VolatileCount.Should().Be(1);
        summary.UnstableCount.Should().Be(1);
    }

    [Fact]
    public async Task HealthSummary_counts_migration_lag_correctly()
    {
        var entries = new List<EventSchemaEvolutionEntry>
        {
            MakeEntry("c-15", "evt-lag", "svc-x", 5, 0, consumersOnOldVersion: 3, schemaLagDays: 50.0),
            MakeEntry("c-16", "evt-ok", "svc-y", 5, 0, consumersOnOldVersion: 0, schemaLagDays: 0.0),
        };
        var handler = CreateHandler(entries);
        var result = await handler.Handle(DefaultQuery(), CancellationToken.None);

        result.Value.HealthSummary.MigrationLagCount.Should().Be(1);
    }

    // ── Empty tenant ──────────────────────────────────────────────────────

    [Fact]
    public async Task Returns_empty_report_when_no_contracts()
    {
        var handler = CreateHandler(Array.Empty<EventSchemaEvolutionEntry>());
        var result = await handler.Handle(DefaultQuery(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.AllContracts.Should().BeEmpty();
        result.Value.TopUnstableContracts.Should().BeEmpty();
        result.Value.MigrationLagContracts.Should().BeEmpty();
        result.Value.HealthSummary.TotalContracts.Should().Be(0);
    }

    // ── Zero changes ─────────────────────────────────────────────────────

    [Fact]
    public async Task Returns_zero_breaking_rate_when_total_changes_is_zero()
    {
        var entries = new List<EventSchemaEvolutionEntry>
        {
            MakeEntry("c-17", "evt-new", "svc-new", totalChanges: 0, breakingChanges: 0)
        };
        var handler = CreateHandler(entries);
        var result = await handler.Handle(DefaultQuery(), CancellationToken.None);

        result.Value.AllContracts.Single().BreakingChangeRate.Should().Be(0.0);
        result.Value.AllContracts.Single().StabilityTier
            .Should().Be(GetEventSchemaEvolutionReport.EventSchemaStabilityTier.Stable);
    }

    // ── Null reader ───────────────────────────────────────────────────────

    [Fact]
    public void Constructor_throws_when_reader_is_null()
    {
        var act = () => new GetEventSchemaEvolutionReport.Handler(null!);
        act.Should().Throw<Exception>();
    }
}
