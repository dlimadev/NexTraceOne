using System.Linq;
using FluentAssertions;
using NSubstitute;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.Catalog.Application.Services.Abstractions;
using NexTraceOne.Catalog.Application.Services.Features.GetServiceLifecycleTransitionReport;

namespace NexTraceOne.Catalog.Tests.Application.Features;

/// <summary>
/// Testes unitários para Wave AF.1 — GetServiceLifecycleTransitionReport.
/// Cobre StagnationFlag, AcceleratedRetirementFlag, BlockedTransitionFlag,
/// LifecycleDistribution, TopStagnated/TopBlocked e Validator.
/// </summary>
public sealed class WaveAfServiceLifecycleTransitionReportTests
{
    private static readonly DateTimeOffset FixedNow = new(2026, 4, 22, 12, 0, 0, TimeSpan.Zero);
    private const string TenantId = "tenant-af1";

    private static IDateTimeProvider CreateClock()
    {
        var clock = Substitute.For<IDateTimeProvider>();
        clock.UtcNow.Returns(FixedNow);
        return clock;
    }

    private static GetServiceLifecycleTransitionReport.Handler CreateHandler(
        IReadOnlyList<ServiceLifecycleEntry> entries)
    {
        var reader = Substitute.For<IServiceLifecycleReader>();
        reader.ListByTenantAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(entries));
        return new GetServiceLifecycleTransitionReport.Handler(reader, CreateClock());
    }

    private static ServiceLifecycleEntry MakeEntry(
        string id,
        string name,
        ServiceLifecycleState state,
        int daysInState,
        int activeCriticalConsumers = 0,
        int migratingConsumers = 0,
        int transitionCount = 1,
        string tier = "Standard") =>
        new(id, name, "team-a", tier, state,
            FixedNow.AddDays(-daysInState), transitionCount, activeCriticalConsumers, migratingConsumers);

    private static GetServiceLifecycleTransitionReport.Query DefaultQuery()
        => new(TenantId: TenantId, StagnationDays: 90, MinDeprecationDays: 30);

    // ── StagnationFlag ─────────────────────────────────────────────────────

    [Fact]
    public async Task StagnationFlag_set_when_deprecated_with_no_migration_over_stagnation_days()
    {
        var entries = new List<ServiceLifecycleEntry>
        {
            MakeEntry("s1", "svc-a", ServiceLifecycleState.Deprecated, daysInState: 100, migratingConsumers: 0)
        };
        var result = await CreateHandler(entries).Handle(DefaultQuery(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.AllServices.Single().StagnationFlag.Should().BeTrue();
        result.Value.StagnationFlagCount.Should().Be(1);
    }

    [Fact]
    public async Task StagnationFlag_not_set_when_migrating_consumers_present()
    {
        var entries = new List<ServiceLifecycleEntry>
        {
            MakeEntry("s2", "svc-b", ServiceLifecycleState.Deprecated, daysInState: 120, migratingConsumers: 3)
        };
        var result = await CreateHandler(entries).Handle(DefaultQuery(), CancellationToken.None);

        result.Value.AllServices.Single().StagnationFlag.Should().BeFalse();
    }

    [Fact]
    public async Task StagnationFlag_not_set_when_deprecated_less_than_stagnation_days()
    {
        var entries = new List<ServiceLifecycleEntry>
        {
            MakeEntry("s3", "svc-c", ServiceLifecycleState.Deprecated, daysInState: 50, migratingConsumers: 0)
        };
        var result = await CreateHandler(entries).Handle(DefaultQuery(), CancellationToken.None);

        result.Value.AllServices.Single().StagnationFlag.Should().BeFalse();
    }

    // ── AcceleratedRetirementFlag ──────────────────────────────────────────

    [Fact]
    public async Task AcceleratedRetirementFlag_set_when_retired_within_min_days()
    {
        var entries = new List<ServiceLifecycleEntry>
        {
            MakeEntry("s4", "svc-d", ServiceLifecycleState.Retired, daysInState: 10)
            // 10 days in Retired < MinDeprecationDays=30 → AcceleratedRetirementFlag
        };
        var result = await CreateHandler(entries).Handle(DefaultQuery(), CancellationToken.None);

        result.Value.AllServices.Single().AcceleratedRetirementFlag.Should().BeTrue();
        result.Value.AcceleratedRetirementFlagCount.Should().Be(1);
    }

    [Fact]
    public async Task AcceleratedRetirementFlag_not_set_for_active_services()
    {
        var entries = new List<ServiceLifecycleEntry>
        {
            MakeEntry("s5", "svc-e", ServiceLifecycleState.Active, daysInState: 5)
        };
        var result = await CreateHandler(entries).Handle(DefaultQuery(), CancellationToken.None);

        result.Value.AllServices.Single().AcceleratedRetirementFlag.Should().BeFalse();
    }

    // ── BlockedTransitionFlag ─────────────────────────────────────────────

    [Fact]
    public async Task BlockedTransitionFlag_set_when_deprecated_with_critical_consumers()
    {
        var entries = new List<ServiceLifecycleEntry>
        {
            MakeEntry("s6", "svc-f", ServiceLifecycleState.Deprecated, daysInState: 20, activeCriticalConsumers: 2)
        };
        var result = await CreateHandler(entries).Handle(DefaultQuery(), CancellationToken.None);

        result.Value.AllServices.Single().BlockedTransitionFlag.Should().BeTrue();
        result.Value.BlockedTransitionFlagCount.Should().Be(1);
    }

    [Fact]
    public async Task BlockedTransitionFlag_not_set_when_no_active_consumers()
    {
        var entries = new List<ServiceLifecycleEntry>
        {
            MakeEntry("s7", "svc-g", ServiceLifecycleState.Deprecated, daysInState: 20, activeCriticalConsumers: 0)
        };
        var result = await CreateHandler(entries).Handle(DefaultQuery(), CancellationToken.None);

        result.Value.AllServices.Single().BlockedTransitionFlag.Should().BeFalse();
    }

    // ── LifecycleDistribution ─────────────────────────────────────────────

    [Fact]
    public async Task LifecycleDistribution_counts_by_state()
    {
        var entries = new List<ServiceLifecycleEntry>
        {
            MakeEntry("s8",  "svc-h", ServiceLifecycleState.Active, daysInState: 100),
            MakeEntry("s9",  "svc-i", ServiceLifecycleState.Active, daysInState: 200),
            MakeEntry("s10", "svc-j", ServiceLifecycleState.Deprecated, daysInState: 50),
            MakeEntry("s11", "svc-k", ServiceLifecycleState.Retired, daysInState: 10)
        };
        var result = await CreateHandler(entries).Handle(DefaultQuery(), CancellationToken.None);

        var dist = result.Value.LifecycleDistribution;
        dist.ActiveCount.Should().Be(2);
        dist.DeprecatedCount.Should().Be(1);
        dist.RetiredCount.Should().Be(1);
        dist.PreProductionCount.Should().Be(0);
    }

    // ── TopStagnated / TopBlocked ─────────────────────────────────────────

    [Fact]
    public async Task TopStagnatedServices_ordered_by_descending_days()
    {
        var entries = new List<ServiceLifecycleEntry>
        {
            MakeEntry("s12", "svc-l", ServiceLifecycleState.Deprecated, daysInState: 200, migratingConsumers: 0),
            MakeEntry("s13", "svc-m", ServiceLifecycleState.Deprecated, daysInState: 120, migratingConsumers: 0)
        };
        var result = await CreateHandler(entries).Handle(DefaultQuery(), CancellationToken.None);

        result.Value.TopStagnatedServices.Should().HaveCount(2);
        result.Value.TopStagnatedServices.First().ServiceName.Should().Be("svc-l");
    }

    [Fact]
    public async Task TopBlockedServices_contains_only_blocked_transition_services()
    {
        var entries = new List<ServiceLifecycleEntry>
        {
            MakeEntry("s14", "svc-n", ServiceLifecycleState.Deprecated, daysInState: 30, activeCriticalConsumers: 1),
            MakeEntry("s15", "svc-o", ServiceLifecycleState.Deprecated, daysInState: 30, activeCriticalConsumers: 0)
        };
        var result = await CreateHandler(entries).Handle(DefaultQuery(), CancellationToken.None);

        result.Value.TopBlockedServices.Should().ContainSingle(s => s.ServiceName == "svc-n");
        result.Value.TopBlockedServices.Should().NotContain(s => s.ServiceName == "svc-o");
    }

    // ── DaysInCurrentState ────────────────────────────────────────────────

    [Fact]
    public async Task DaysInCurrentState_calculated_from_state_entered_at()
    {
        var entries = new List<ServiceLifecycleEntry>
        {
            MakeEntry("s16", "svc-p", ServiceLifecycleState.Active, daysInState: 45)
        };
        var result = await CreateHandler(entries).Handle(DefaultQuery(), CancellationToken.None);

        result.Value.AllServices.Single().DaysInCurrentState.Should().Be(45);
    }

    // ── Empty result ──────────────────────────────────────────────────────

    [Fact]
    public async Task Returns_empty_report_when_no_services()
    {
        var result = await CreateHandler(new List<ServiceLifecycleEntry>())
            .Handle(DefaultQuery(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TotalServicesAnalyzed.Should().Be(0);
    }

    // ── Validator ─────────────────────────────────────────────────────────

    [Fact]
    public void Validator_rejects_empty_tenantId()
    {
        var validator = new GetServiceLifecycleTransitionReport.Validator();
        validator.Validate(new GetServiceLifecycleTransitionReport.Query(TenantId: ""))
            .IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validator_rejects_stagnation_days_out_of_range()
    {
        var validator = new GetServiceLifecycleTransitionReport.Validator();
        validator.Validate(new GetServiceLifecycleTransitionReport.Query(TenantId: TenantId, StagnationDays: 0))
            .IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validator_accepts_valid_query()
    {
        var validator = new GetServiceLifecycleTransitionReport.Validator();
        validator.Validate(DefaultQuery()).IsValid.Should().BeTrue();
    }
}
