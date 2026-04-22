using System.Linq;
using FluentAssertions;
using NSubstitute;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.Catalog.Application.Services.Abstractions;
using NexTraceOne.Catalog.Application.Services.Features.GetServiceMigrationProgressReport;

namespace NexTraceOne.Catalog.Tests.Application.Features;

/// <summary>
/// Testes unitários para Wave AF.3 — GetServiceMigrationProgressReport.
/// Cobre MigrationTier, MigrationCompletionRate, StuckConsumers,
/// EstimatedCompletionDate, TierDistribution e Validator.
/// </summary>
public sealed class WaveAfServiceMigrationProgressReportTests
{
    private static readonly DateTimeOffset FixedNow = new(2026, 4, 22, 12, 0, 0, TimeSpan.Zero);
    private const string TenantId = "tenant-af3";

    private static IDateTimeProvider CreateClock()
    {
        var clock = Substitute.For<IDateTimeProvider>();
        clock.UtcNow.Returns(FixedNow);
        return clock;
    }

    private static GetServiceMigrationProgressReport.Handler CreateHandler(
        IReadOnlyList<MigrationProgressEntry> entries)
    {
        var reader = Substitute.For<IMigrationProgressReader>();
        reader.ListByTenantAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(entries));
        return new GetServiceMigrationProgressReport.Handler(reader, CreateClock());
    }

    private static MigrationProgressEntry MakeEntry(
        string id,
        string name,
        int totalConsumers,
        int migratedConsumers,
        int inProgress = 0,
        IReadOnlyList<StuckConsumerInfo>? stuckConsumers = null,
        int daysInState = 60) =>
        new(ServiceId: id,
            ServiceName: name,
            SuccessorServiceName: name + "-v2",
            TeamName: "team-a",
            CurrentLifecycleState: "Deprecated",
            StateEnteredAt: FixedNow.AddDays(-daysInState),
            TotalConsumers: totalConsumers,
            MigratedConsumers: migratedConsumers,
            InProgressConsumers: inProgress,
            StuckConsumerDetails: stuckConsumers ?? new List<StuckConsumerInfo>(),
            DailyTimeline: BuildTimeline(daysInState, migratedConsumers));

    private static IReadOnlyList<DailyMigrationPoint> BuildTimeline(int daysInState, int migratedTotal)
    {
        var points = new List<DailyMigrationPoint>();
        var today = DateOnly.FromDateTime(FixedNow.UtcDateTime);
        for (int i = daysInState; i >= 0; i--)
        {
            var ratio = daysInState > 0 ? (daysInState - i) / (double)daysInState : 1.0;
            points.Add(new DailyMigrationPoint(today.AddDays(-i), (int)(migratedTotal * ratio)));
        }
        return points;
    }

    private static GetServiceMigrationProgressReport.Query DefaultQuery()
        => new(TenantId: TenantId, StuckThresholdDays: 30);

    // ── MigrationTier ─────────────────────────────────────────────────────

    [Fact]
    public async Task MigrationTier_Complete_when_all_consumers_migrated()
    {
        var entries = new List<MigrationProgressEntry>
        {
            MakeEntry("s1", "svc-a", totalConsumers: 5, migratedConsumers: 5)
        };
        var result = await CreateHandler(entries).Handle(DefaultQuery(), CancellationToken.None);

        result.Value.Services.Single().Tier
            .Should().Be(GetServiceMigrationProgressReport.MigrationTier.Complete);
        result.Value.Services.Single().MigrationCompletionRate.Should().Be(100.0);
    }

    [Fact]
    public async Task MigrationTier_Advanced_when_75_percent_migrated()
    {
        var entries = new List<MigrationProgressEntry>
        {
            MakeEntry("s2", "svc-b", totalConsumers: 8, migratedConsumers: 6)
        };
        var result = await CreateHandler(entries).Handle(DefaultQuery(), CancellationToken.None);

        result.Value.Services.Single().Tier
            .Should().Be(GetServiceMigrationProgressReport.MigrationTier.Advanced);
    }

    [Fact]
    public async Task MigrationTier_InProgress_when_between_25_and_75()
    {
        var entries = new List<MigrationProgressEntry>
        {
            MakeEntry("s3", "svc-c", totalConsumers: 10, migratedConsumers: 4)
        };
        var result = await CreateHandler(entries).Handle(DefaultQuery(), CancellationToken.None);

        result.Value.Services.Single().Tier
            .Should().Be(GetServiceMigrationProgressReport.MigrationTier.InProgress);
    }

    [Fact]
    public async Task MigrationTier_Lagging_when_below_25_percent()
    {
        var entries = new List<MigrationProgressEntry>
        {
            MakeEntry("s4", "svc-d", totalConsumers: 10, migratedConsumers: 2)
        };
        var result = await CreateHandler(entries).Handle(DefaultQuery(), CancellationToken.None);

        result.Value.Services.Single().Tier
            .Should().Be(GetServiceMigrationProgressReport.MigrationTier.Lagging);
    }

    // ── StuckConsumers ────────────────────────────────────────────────────

    [Fact]
    public async Task StuckConsumers_filtered_by_threshold()
    {
        var stuck = new List<StuckConsumerInfo>
        {
            new("c1", "team-x", "Standard", DaysSinceLastActivity: 40),  // above threshold=30
            new("c2", "team-y", "Critical", DaysSinceLastActivity: 15)   // below threshold
        };
        var entries = new List<MigrationProgressEntry>
        {
            MakeEntry("s5", "svc-e", totalConsumers: 3, migratedConsumers: 1, stuckConsumers: stuck)
        };
        var result = await CreateHandler(entries).Handle(DefaultQuery(), CancellationToken.None);

        result.Value.Services.Single().StuckConsumerCount.Should().Be(1);
        result.Value.TotalStuckConsumers.Should().Be(1);
    }

    [Fact]
    public async Task TopStuckConsumers_ordered_by_days_descending()
    {
        var stuck = new List<StuckConsumerInfo>
        {
            new("c1", "team-x", "Standard", 31),
            new("c2", "team-y", "Critical", 60),
            new("c3", "team-z", "Experimental", 45)
        };
        var entries = new List<MigrationProgressEntry>
        {
            MakeEntry("s6", "svc-f", totalConsumers: 4, migratedConsumers: 0, stuckConsumers: stuck)
        };
        var result = await CreateHandler(entries).Handle(DefaultQuery(), CancellationToken.None);

        var topStuck = result.Value.Services.Single().TopStuckConsumers;
        topStuck.First().ConsumerServiceName.Should().Be("c2");
        topStuck.Last().ConsumerServiceName.Should().Be("c1");
    }

    // ── TierDistribution ──────────────────────────────────────────────────

    [Fact]
    public async Task TierDistribution_counts_correctly()
    {
        var entries = new List<MigrationProgressEntry>
        {
            MakeEntry("s7",  "svc-g", totalConsumers: 5, migratedConsumers: 5),  // Complete
            MakeEntry("s8",  "svc-h", totalConsumers: 10, migratedConsumers: 2), // Lagging (20%)
            MakeEntry("s9",  "svc-i", totalConsumers: 8, migratedConsumers: 6),  // Advanced
            MakeEntry("s10", "svc-j", totalConsumers: 10, migratedConsumers: 3)  // InProgress
        };
        var result = await CreateHandler(entries).Handle(DefaultQuery(), CancellationToken.None);

        var dist = result.Value.TierDistribution;
        dist.CompleteCount.Should().Be(1);
        dist.AdvancedCount.Should().Be(1);
        dist.InProgressCount.Should().Be(1);
        dist.LaggingCount.Should().Be(1);
    }

    // ── TenantMigrationCompletionRate ─────────────────────────────────────

    [Fact]
    public async Task TenantMigrationCompletionRate_is_average_of_services()
    {
        var entries = new List<MigrationProgressEntry>
        {
            MakeEntry("s11", "svc-k", totalConsumers: 10, migratedConsumers: 6),   // 60%
            MakeEntry("s12", "svc-l", totalConsumers: 10, migratedConsumers: 10)   // 100%
        };
        var result = await CreateHandler(entries).Handle(DefaultQuery(), CancellationToken.None);

        result.Value.TenantMigrationCompletionRate.Should().Be(80.0);
    }

    // ── TopLaggingServices ────────────────────────────────────────────────

    [Fact]
    public async Task TopLaggingServices_ordered_by_completion_rate_ascending()
    {
        var entries = new List<MigrationProgressEntry>
        {
            MakeEntry("s13", "svc-m", totalConsumers: 10, migratedConsumers: 2),  // 20% - Lagging
            MakeEntry("s14", "svc-n", totalConsumers: 10, migratedConsumers: 3),  // 30% - InProgress
            MakeEntry("s15", "svc-o", totalConsumers: 10, migratedConsumers: 10)  // 100% - Complete (excluded)
        };
        var result = await CreateHandler(entries).Handle(DefaultQuery(), CancellationToken.None);

        result.Value.TopLaggingServices.Should().HaveCount(2);
        result.Value.TopLaggingServices.First().ServiceName.Should().Be("svc-m");
    }

    // ── EstimatedCompletionDate ───────────────────────────────────────────

    [Fact]
    public async Task EstimatedCompletionDate_null_when_no_migration_progress()
    {
        var entries = new List<MigrationProgressEntry>
        {
            MakeEntry("s16", "svc-p", totalConsumers: 5, migratedConsumers: 0, daysInState: 30)
        };
        var result = await CreateHandler(entries).Handle(DefaultQuery(), CancellationToken.None);

        // No progress → cannot project
        result.Value.Services.Single().EstimatedCompletionDate.Should().BeNull();
    }

    // ── Empty result ──────────────────────────────────────────────────────

    [Fact]
    public async Task Returns_empty_report_when_no_deprecated_services()
    {
        var result = await CreateHandler(new List<MigrationProgressEntry>())
            .Handle(DefaultQuery(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TotalDeprecatedServices.Should().Be(0);
        result.Value.TenantMigrationCompletionRate.Should().Be(0.0);
    }

    // ── DailyMigrationTimeline ────────────────────────────────────────────

    [Fact]
    public async Task DailyMigrationTimeline_exposed_in_profile()
    {
        var entries = new List<MigrationProgressEntry>
        {
            MakeEntry("s17", "svc-q", totalConsumers: 4, migratedConsumers: 4, daysInState: 5)
        };
        var result = await CreateHandler(entries).Handle(DefaultQuery(), CancellationToken.None);

        result.Value.Services.Single().DailyMigrationTimeline.Should().NotBeEmpty();
    }

    // ── Validator ─────────────────────────────────────────────────────────

    [Fact]
    public void Validator_rejects_empty_tenantId()
    {
        var validator = new GetServiceMigrationProgressReport.Validator();
        validator.Validate(new GetServiceMigrationProgressReport.Query(TenantId: ""))
            .IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validator_rejects_stuck_threshold_out_of_range()
    {
        var validator = new GetServiceMigrationProgressReport.Validator();
        validator.Validate(new GetServiceMigrationProgressReport.Query(TenantId: TenantId, StuckThresholdDays: 0))
            .IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validator_accepts_valid_query()
    {
        var validator = new GetServiceMigrationProgressReport.Validator();
        validator.Validate(DefaultQuery()).IsValid.Should().BeTrue();
    }
}
