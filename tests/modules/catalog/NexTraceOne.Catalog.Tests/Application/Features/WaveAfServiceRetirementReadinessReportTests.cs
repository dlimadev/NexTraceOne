using System.Linq;
using FluentAssertions;
using NSubstitute;
using NexTraceOne.Catalog.Application.Services.Abstractions;
using NexTraceOne.Catalog.Application.Services.Features.GetServiceRetirementReadinessReport;

namespace NexTraceOne.Catalog.Tests.Application.Features;

/// <summary>
/// Testes unitários para Wave AF.2 — GetServiceRetirementReadinessReport.
/// Cobre RetirementReadinessScore, RetirementReadinessTier, DimensionScores,
/// BlockerList, ConsumerMigrationProgress e Validator.
/// </summary>
public sealed class WaveAfServiceRetirementReadinessReportTests
{
    private const string TenantId = "tenant-af2";
    private const string ServiceId = "svc-retire-1";

    private static GetServiceRetirementReadinessReport.Handler CreateHandler(
        RetirementReadinessData? data)
    {
        var reader = Substitute.For<IRetirementReadinessReader>();
        reader.GetByServiceAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(data));
        return new GetServiceRetirementReadinessReport.Handler(reader);
    }

    private static RetirementReadinessData MakeData(
        int totalConsumers,
        int migratedConsumers,
        int totalContracts,
        int deprecatedContracts,
        bool hasRunbook,
        int totalConsumerTeams,
        int notifiedTeams,
        IReadOnlyList<BlockerConsumerInfo>? unmigratedConsumers = null) =>
        new(ServiceId: ServiceId,
            ServiceName: "svc-retire",
            TeamName: "team-a",
            CurrentLifecycleState: "Deprecated",
            TotalConsumers: totalConsumers,
            MigratedConsumers: migratedConsumers,
            TotalContracts: totalContracts,
            DeprecatedContracts: deprecatedContracts,
            HasApprovedDecommissionRunbook: hasRunbook,
            TotalConsumerTeams: totalConsumerTeams,
            NotifiedConsumerTeams: notifiedTeams,
            UnmigratedConsumers: unmigratedConsumers ?? new List<BlockerConsumerInfo>());

    private static GetServiceRetirementReadinessReport.Query DefaultQuery()
        => new(TenantId: TenantId, ServiceId: ServiceId);

    // ── Ready tier ────────────────────────────────────────────────────────

    [Fact]
    public async Task Returns_Ready_when_all_dimensions_satisfied()
    {
        // ConsumerMigrated=40, ContractsDeprecated=25, Runbook=15, Notified=20 → 100
        var data = MakeData(10, 10, 4, 4, hasRunbook: true, 3, 3);
        var result = await CreateHandler(data).Handle(DefaultQuery(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.RetirementReadinessScore.Should().Be(100.0);
        result.Value.Tier.Should().Be(GetServiceRetirementReadinessReport.RetirementReadinessTier.Ready);
        result.Value.BlockerList.Should().BeEmpty();
    }

    // ── NotReady tier ─────────────────────────────────────────────────────

    [Fact]
    public async Task Returns_NotReady_when_no_dimensions_satisfied()
    {
        // 0 consumers migrated, 0 contracts deprecated, no runbook, 0 notified → 0
        var data = MakeData(10, 0, 4, 0, hasRunbook: false, 3, 0,
            new[] { new BlockerConsumerInfo("c1", "team-b", "Critical", false) });
        var result = await CreateHandler(data).Handle(DefaultQuery(), CancellationToken.None);

        result.Value.RetirementReadinessScore.Should().Be(0.0);
        result.Value.Tier.Should().Be(GetServiceRetirementReadinessReport.RetirementReadinessTier.NotReady);
    }

    // ── Blocked tier ──────────────────────────────────────────────────────

    [Fact]
    public async Task Returns_Blocked_when_score_between_40_and_65()
    {
        // ConsumerMigrated=50%→20, ContractsDeprecated=0, Runbook=15, Notified=50%→10 → 45
        var data = MakeData(10, 5, 4, 0, hasRunbook: true, 2, 1);
        var result = await CreateHandler(data).Handle(DefaultQuery(), CancellationToken.None);

        result.Value.Tier.Should().Be(GetServiceRetirementReadinessReport.RetirementReadinessTier.Blocked);
    }

    // ── NearReady tier ────────────────────────────────────────────────────

    [Fact]
    public async Task Returns_NearReady_when_score_between_65_and_85()
    {
        // ConsumerMigrated=80%→32, ContractsDeprecated=100%→25, Runbook=0, Notified=100%→20 → 77
        var data = MakeData(10, 8, 4, 4, hasRunbook: false, 2, 2);
        var result = await CreateHandler(data).Handle(DefaultQuery(), CancellationToken.None);

        result.Value.Tier.Should().Be(GetServiceRetirementReadinessReport.RetirementReadinessTier.NearReady);
    }

    // ── BlockerList ───────────────────────────────────────────────────────

    [Fact]
    public async Task BlockerList_includes_active_consumers_when_unmigrated()
    {
        var data = MakeData(5, 2, 2, 2, hasRunbook: true, 2, 2,
            new[] { new BlockerConsumerInfo("c1", "t1", "Standard", true),
                    new BlockerConsumerInfo("c2", "t2", "Critical", false) });
        var result = await CreateHandler(data).Handle(DefaultQuery(), CancellationToken.None);

        result.Value.BlockerList.Should().Contain(b => b.BlockerType == "active-consumers");
    }

    [Fact]
    public async Task BlockerList_includes_missing_runbook()
    {
        var data = MakeData(0, 0, 0, 0, hasRunbook: false, 0, 0);
        var result = await CreateHandler(data).Handle(DefaultQuery(), CancellationToken.None);

        result.Value.BlockerList.Should().Contain(b => b.BlockerType == "missing-runbook");
    }

    [Fact]
    public async Task BlockerList_includes_active_contracts_when_not_deprecated()
    {
        var data = MakeData(0, 0, 4, 2, hasRunbook: true, 0, 0);
        var result = await CreateHandler(data).Handle(DefaultQuery(), CancellationToken.None);

        result.Value.BlockerList.Should().Contain(b => b.BlockerType == "active-contracts");
    }

    [Fact]
    public async Task BlockerList_includes_unnotified_teams()
    {
        var data = MakeData(0, 0, 4, 4, hasRunbook: true, 4, 2);
        var result = await CreateHandler(data).Handle(DefaultQuery(), CancellationToken.None);

        result.Value.BlockerList.Should().Contain(b => b.BlockerType == "unnotified-teams" && b.AffectedCount == 2);
    }

    // ── No consumers edge case ────────────────────────────────────────────

    [Fact]
    public async Task No_consumers_contributes_full_consumer_score()
    {
        // No consumers → ConsumerMigrated gets full 40 pts
        var data = MakeData(0, 0, 4, 4, hasRunbook: true, 0, 0);
        var result = await CreateHandler(data).Handle(DefaultQuery(), CancellationToken.None);

        result.Value.Dimensions.ConsumerMigratedScore.Should().Be(40.0);
        result.Value.RetirementReadinessScore.Should().Be(100.0);
    }

    // ── Service not found ─────────────────────────────────────────────────

    [Fact]
    public async Task Returns_failure_when_service_not_found()
    {
        var result = await CreateHandler(null).Handle(DefaultQuery(), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Type.Should().Be(NexTraceOne.BuildingBlocks.Core.Results.ErrorType.NotFound);
    }

    // ── DimensionScores ───────────────────────────────────────────────────

    [Fact]
    public async Task DimensionScores_sum_equals_total_score()
    {
        var data = MakeData(10, 6, 4, 3, hasRunbook: true, 2, 1);
        var result = await CreateHandler(data).Handle(DefaultQuery(), CancellationToken.None);

        var dims = result.Value.Dimensions;
        var sum = dims.ConsumerMigratedScore + dims.ContractsDeprecatedScore +
                  dims.RunbookDocumentedScore + dims.DependantsNotifiedScore;
        Math.Round(sum, 0).Should().Be(Math.Round(result.Value.RetirementReadinessScore, 0));
    }

    // ── ConsumerMigrationProgress ─────────────────────────────────────────

    [Fact]
    public async Task ConsumerMigrationProgress_lists_unmigrated_consumers()
    {
        var data = MakeData(2, 0, 0, 0, hasRunbook: true, 0, 0,
            new[]
            {
                new BlockerConsumerInfo("svc-consumer-1", "team-x", "Standard", true),
                new BlockerConsumerInfo("svc-consumer-2", "team-y", "Critical", false)
            });
        var result = await CreateHandler(data).Handle(DefaultQuery(), CancellationToken.None);

        result.Value.ConsumerMigrationProgress.Should().HaveCount(2);
        result.Value.ConsumerMigrationProgress.All(c => !c.IsMigrated).Should().BeTrue();
        result.Value.ConsumerMigrationProgress.Should()
            .Contain(c => c.ConsumerServiceName == "svc-consumer-1" && c.IsNotified);
    }

    // ── Validator ─────────────────────────────────────────────────────────

    [Fact]
    public void Validator_rejects_empty_tenantId()
    {
        var validator = new GetServiceRetirementReadinessReport.Validator();
        validator.Validate(new GetServiceRetirementReadinessReport.Query(TenantId: "", ServiceId: ServiceId))
            .IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validator_rejects_empty_serviceId()
    {
        var validator = new GetServiceRetirementReadinessReport.Validator();
        validator.Validate(new GetServiceRetirementReadinessReport.Query(TenantId: TenantId, ServiceId: ""))
            .IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validator_rejects_near_ready_threshold_higher_than_ready()
    {
        var validator = new GetServiceRetirementReadinessReport.Validator();
        validator.Validate(new GetServiceRetirementReadinessReport.Query(
            TenantId: TenantId, ServiceId: ServiceId,
            ReadyThreshold: 70, NearReadyThreshold: 75))
            .IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validator_accepts_valid_query()
    {
        var validator = new GetServiceRetirementReadinessReport.Validator();
        validator.Validate(DefaultQuery()).IsValid.Should().BeTrue();
    }
}
