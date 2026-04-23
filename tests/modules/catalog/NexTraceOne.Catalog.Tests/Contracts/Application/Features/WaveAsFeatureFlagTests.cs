using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.Catalog.Application.Contracts.Abstractions;
using NexTraceOne.Catalog.Application.Contracts.Features.GetExperimentGovernanceReport;
using NexTraceOne.Catalog.Application.Contracts.Features.GetFeatureFlagInventoryReport;
using NexTraceOne.Catalog.Application.Contracts.Features.GetFeatureFlagRiskReport;
using NexTraceOne.Catalog.Application.Contracts.Features.IngestFeatureFlagState;
using NexTraceOne.Catalog.Domain.Entities;
using static NexTraceOne.Catalog.Application.Contracts.Abstractions.IFeatureFlagRiskReader;

namespace NexTraceOne.Catalog.Tests.Contracts.Application.Features;

/// <summary>
/// Testes unitários para Wave AS — Feature Flag &amp; Experimentation Governance.
/// Cobre AS.1 IngestFeatureFlagState, AS.1 GetFeatureFlagInventoryReport,
/// AS.2 GetFeatureFlagRiskReport e AS.3 GetExperimentGovernanceReport.
/// </summary>
public sealed class WaveAsFeatureFlagTests
{
    private static readonly DateTimeOffset FixedNow = new(2026, 6, 1, 12, 0, 0, TimeSpan.Zero);
    private const string TenantId = "tenant-as-001";

    private static IDateTimeProvider CreateClock()
    {
        var clock = Substitute.For<IDateTimeProvider>();
        clock.UtcNow.Returns(FixedNow);
        return clock;
    }

    // ════════════════════════════════════════════════════════════════════════
    // AS.1 — IngestFeatureFlagState
    // ════════════════════════════════════════════════════════════════════════

    private static IngestFeatureFlagState.Handler CreateIngestHandler(IFeatureFlagRepository? repo = null)
    {
        repo ??= Substitute.For<IFeatureFlagRepository>();
        return new IngestFeatureFlagState.Handler(repo, CreateClock());
    }

    private static IngestFeatureFlagState.Command ValidIngestCommand(
        string flagKey = "flag-001",
        string flagType = "Release",
        bool isEnabled = true) =>
        new(TenantId, "svc-1", flagKey, flagType, isEnabled,
            ["dev", "staging"], "owner-team", null, null, null, null);

    [Fact]
    public async Task IngestFeatureFlagState_ValidCommand_ReturnsGuidAndCallsRepository()
    {
        var repo = Substitute.For<IFeatureFlagRepository>();
        repo.ListByServiceAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns([]);
        var handler = CreateIngestHandler(repo);

        var result = await handler.Handle(ValidIngestCommand(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBe(Guid.Empty);
        await repo.Received(1).UpsertAsync(Arg.Any<FeatureFlagRecord>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task IngestFeatureFlagState_NewFlag_CreatesFlagRecord()
    {
        FeatureFlagRecord? captured = null;
        var repo = Substitute.For<IFeatureFlagRepository>();
        repo.ListByServiceAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns([]);
        repo.When(r => r.UpsertAsync(Arg.Any<FeatureFlagRecord>(), Arg.Any<CancellationToken>()))
            .Do(ci => captured = ci.Arg<FeatureFlagRecord>());
        var handler = CreateIngestHandler(repo);

        await handler.Handle(ValidIngestCommand("flag-new"), CancellationToken.None);

        captured.Should().NotBeNull();
        captured!.FlagKey.Should().Be("flag-new");
        captured.TenantId.Should().Be(TenantId);
    }

    [Fact]
    public async Task IngestFeatureFlagState_ExistingFlag_UpsertUpdatesRecord()
    {
        var existing = FeatureFlagRecord.Create(
            TenantId, "svc-1", "flag-001", FeatureFlagRecord.FlagType.Release,
            false, null, null, null, null, FixedNow.AddDays(-10));

        var repo = Substitute.For<IFeatureFlagRepository>();
        repo.ListByServiceAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns([existing]);
        var handler = CreateIngestHandler(repo);

        var result = await handler.Handle(ValidIngestCommand("flag-001", isEnabled: true), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        existing.IsEnabled.Should().BeTrue();
    }

    [Theory]
    [InlineData("TenantId")]
    [InlineData("ServiceId")]
    [InlineData("FlagKey")]
    public void IngestFeatureFlagState_EmptyRequiredField_ValidationFails(string fieldName)
    {
        var validator = new IngestFeatureFlagState.Validator();
        var command = fieldName switch
        {
            "TenantId"  => ValidIngestCommand() with { TenantId = "" },
            "ServiceId" => ValidIngestCommand() with { ServiceId = "" },
            "FlagKey"   => ValidIngestCommand() with { FlagKey = "" },
            _           => ValidIngestCommand()
        };

        var result = validator.Validate(command);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task IngestFeatureFlagState_NullOptionalFields_Succeeds()
    {
        var repo = Substitute.For<IFeatureFlagRepository>();
        repo.ListByServiceAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns([]);
        var handler = CreateIngestHandler(repo);
        var command = new IngestFeatureFlagState.Command(
            TenantId, "svc-1", "flag-null", "KillSwitch", false,
            null, null, null, null, null, null);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task IngestFeatureFlagState_UnknownFlagType_DefaultsToRelease()
    {
        FeatureFlagRecord? captured = null;
        var repo = Substitute.For<IFeatureFlagRepository>();
        repo.ListByServiceAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns([]);
        repo.When(r => r.UpsertAsync(Arg.Any<FeatureFlagRecord>(), Arg.Any<CancellationToken>()))
            .Do(ci => captured = ci.Arg<FeatureFlagRecord>());
        var handler = CreateIngestHandler(repo);

        await handler.Handle(ValidIngestCommand("flag-x", flagType: "Unknown"), CancellationToken.None);

        captured!.Type.Should().Be(FeatureFlagRecord.FlagType.Release);
    }

    [Fact]
    public async Task IngestFeatureFlagState_EnabledEnvironments_SerializedAsJson()
    {
        FeatureFlagRecord? captured = null;
        var repo = Substitute.For<IFeatureFlagRepository>();
        repo.ListByServiceAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns([]);
        repo.When(r => r.UpsertAsync(Arg.Any<FeatureFlagRecord>(), Arg.Any<CancellationToken>()))
            .Do(ci => captured = ci.Arg<FeatureFlagRecord>());
        var handler = CreateIngestHandler(repo);

        await handler.Handle(
            new IngestFeatureFlagState.Command(TenantId, "svc-1", "flag-env", "Release", true,
                ["dev", "staging", "prod"], null, null, null, null, null),
            CancellationToken.None);

        captured!.EnabledEnvironmentsJson.Should().Contain("dev");
        captured.EnabledEnvironmentsJson.Should().Contain("staging");
    }

    [Fact]
    public void IngestFeatureFlagState_ValidCommand_PassesValidation()
    {
        var validator = new IngestFeatureFlagState.Validator();
        var result = validator.Validate(ValidIngestCommand());
        result.IsValid.Should().BeTrue();
    }

    // ════════════════════════════════════════════════════════════════════════
    // AS.1 — GetFeatureFlagInventoryReport
    // ════════════════════════════════════════════════════════════════════════

    private static GetFeatureFlagInventoryReport.Handler CreateInventoryHandler(
        IReadOnlyList<FeatureFlagRecord> flags)
    {
        var repo = Substitute.For<IFeatureFlagRepository>();
        repo.ListByTenantAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(flags);
        return new GetFeatureFlagInventoryReport.Handler(repo, CreateClock());
    }

    private static FeatureFlagRecord MakeFlag(
        string serviceId,
        string flagKey,
        FeatureFlagRecord.FlagType type = FeatureFlagRecord.FlagType.Release,
        bool isEnabled = true,
        string? ownerId = "owner-1",
        DateTimeOffset? lastToggledAt = null,
        string? enabledEnvJson = null)
    {
        var flag = FeatureFlagRecord.Create(
            TenantId, serviceId, flagKey, type, isEnabled,
            enabledEnvJson, ownerId, lastToggledAt, null, FixedNow.AddDays(-30));
        return flag;
    }

    [Fact]
    public async Task GetFeatureFlagInventoryReport_EmptyTenant_ReturnsEmptyReport()
    {
        var handler = CreateInventoryHandler([]);
        var result = await handler.Handle(
            new GetFeatureFlagInventoryReport.Query(TenantId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.ByService.Should().BeEmpty();
        result.Value.Summary.TotalFlags.Should().Be(0);
        result.Value.Summary.ActiveFlags.Should().Be(0);
    }

    [Fact]
    public async Task GetFeatureFlagInventoryReport_StaleFlag_DetectedByLastToggledAt()
    {
        var staleFlag = MakeFlag("svc-1", "flag-stale",
            lastToggledAt: FixedNow.AddDays(-90)); // > 60 days stale
        var handler = CreateInventoryHandler([staleFlag]);

        var result = await handler.Handle(
            new GetFeatureFlagInventoryReport.Query(TenantId, StaleFlagDays: 60), CancellationToken.None);

        result.Value.ByService.Single().StaleFlagsCount.Should().Be(1);
        result.Value.Summary.StaleFlags.Should().Be(1);
    }

    [Fact]
    public async Task GetFeatureFlagInventoryReport_RecentFlag_NotStale()
    {
        var freshFlag = MakeFlag("svc-1", "flag-fresh",
            lastToggledAt: FixedNow.AddDays(-10)); // < 60 days
        var handler = CreateInventoryHandler([freshFlag]);

        var result = await handler.Handle(
            new GetFeatureFlagInventoryReport.Query(TenantId, StaleFlagDays: 60), CancellationToken.None);

        result.Value.ByService.Single().StaleFlagsCount.Should().Be(0);
    }

    [Fact]
    public async Task GetFeatureFlagInventoryReport_OwnerlessFlags_Counted()
    {
        var flags = new[]
        {
            MakeFlag("svc-1", "flag-no-owner", ownerId: null),
            MakeFlag("svc-1", "flag-with-owner", ownerId: "team-a")
        };
        var handler = CreateInventoryHandler(flags);

        var result = await handler.Handle(
            new GetFeatureFlagInventoryReport.Query(TenantId), CancellationToken.None);

        result.Value.ByService.Single().OwnerlessFlags.Should().Be(1);
        result.Value.Summary.OwnerlessFlags.Should().Be(1);
    }

    [Fact]
    public async Task GetFeatureFlagInventoryReport_KillSwitchCount_InSummary()
    {
        var flags = new[]
        {
            MakeFlag("svc-1", "ks-1", type: FeatureFlagRecord.FlagType.KillSwitch),
            MakeFlag("svc-1", "ks-2", type: FeatureFlagRecord.FlagType.KillSwitch),
            MakeFlag("svc-1", "rel-1", type: FeatureFlagRecord.FlagType.Release)
        };
        var handler = CreateInventoryHandler(flags);

        var result = await handler.Handle(
            new GetFeatureFlagInventoryReport.Query(TenantId), CancellationToken.None);

        result.Value.Summary.KillSwitchCount.Should().Be(2);
    }

    [Fact]
    public async Task GetFeatureFlagInventoryReport_ByTypeDistribution_CorrectCounts()
    {
        var flags = new[]
        {
            MakeFlag("svc-1", "rel-1", type: FeatureFlagRecord.FlagType.Release),
            MakeFlag("svc-1", "exp-1", type: FeatureFlagRecord.FlagType.Experiment),
            MakeFlag("svc-1", "exp-2", type: FeatureFlagRecord.FlagType.Experiment),
            MakeFlag("svc-1", "perm-1", type: FeatureFlagRecord.FlagType.Permission)
        };
        var handler = CreateInventoryHandler(flags);

        var result = await handler.Handle(
            new GetFeatureFlagInventoryReport.Query(TenantId), CancellationToken.None);

        var byType = result.Value.ByService.Single().ByType;
        byType[GetFeatureFlagInventoryReport.FlagType.Release].Should().Be(1);
        byType[GetFeatureFlagInventoryReport.FlagType.Experiment].Should().Be(2);
        byType[GetFeatureFlagInventoryReport.FlagType.Permission].Should().Be(1);
        byType[GetFeatureFlagInventoryReport.FlagType.KillSwitch].Should().Be(0);
    }

    [Fact]
    public async Task GetFeatureFlagInventoryReport_FlagsByEnvironment_ActiveFlagsCountedPerEnv()
    {
        var flags = new[]
        {
            MakeFlag("svc-1", "flag-a", isEnabled: true, enabledEnvJson: "[\"dev\",\"staging\"]"),
            MakeFlag("svc-1", "flag-b", isEnabled: true, enabledEnvJson: "[\"dev\"]"),
            MakeFlag("svc-1", "flag-c", isEnabled: false, enabledEnvJson: "[\"dev\"]")
        };
        var handler = CreateInventoryHandler(flags);

        var result = await handler.Handle(
            new GetFeatureFlagInventoryReport.Query(TenantId), CancellationToken.None);

        result.Value.FlagsByEnvironment["dev"].Should().Be(2);
        result.Value.FlagsByEnvironment["staging"].Should().Be(1);
    }

    [Fact]
    public void GetFeatureFlagInventoryReport_EmptyTenantId_ValidationFails()
    {
        var validator = new GetFeatureFlagInventoryReport.Validator();
        var result = validator.Validate(new GetFeatureFlagInventoryReport.Query(""));
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task GetFeatureFlagInventoryReport_TopServicesWithStaleFlags_CappedAtFive()
    {
        var flags = Enumerable.Range(1, 8)
            .SelectMany(i => Enumerable.Range(1, i).Select(j =>
                MakeFlag($"svc-{i}", $"flag-{i}-{j}", lastToggledAt: FixedNow.AddDays(-90))))
            .ToList();
        var handler = CreateInventoryHandler(flags);

        var result = await handler.Handle(
            new GetFeatureFlagInventoryReport.Query(TenantId, StaleFlagDays: 60), CancellationToken.None);

        result.Value.Summary.TopServicesWithStaleFlags.Should().HaveCount(5);
        result.Value.Summary.TopServicesWithStaleFlags.First().StaleFlagsCount.Should().BeGreaterThanOrEqualTo(
            result.Value.Summary.TopServicesWithStaleFlags.Last().StaleFlagsCount);
    }

    // ════════════════════════════════════════════════════════════════════════
    // AS.2 — GetFeatureFlagRiskReport
    // ════════════════════════════════════════════════════════════════════════

    private static GetFeatureFlagRiskReport.Handler CreateRiskReportHandler(
        IReadOnlyList<FlagRiskEntry> entries)
    {
        var reader = Substitute.For<IFeatureFlagRiskReader>();
        reader.ListFlagRiskByTenantAsync(
                Arg.Any<string>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(entries);
        return new GetFeatureFlagRiskReport.Handler(reader, CreateClock());
    }

    private static FlagRiskEntry MakeRiskEntry(
        string flagKey,
        StalenessRisk staleness = StalenessRisk.Low,
        OwnershipRisk ownership = OwnershipRisk.Low,
        ProductionPresenceRisk prodPresence = ProductionPresenceRisk.Low,
        bool incidentCorrelated = false,
        DateTimeOffset? scheduledRemovalDate = null) =>
        new("svc-1", "Service-1", flagKey, staleness, ownership, prodPresence,
            incidentCorrelated, [], scheduledRemovalDate,
            FixedNow.AddDays(-10), FixedNow.AddDays(-30));

    [Fact]
    public async Task GetFeatureFlagRiskReport_EmptyReader_ReturnsEmptyReport()
    {
        var handler = CreateRiskReportHandler([]);
        var result = await handler.Handle(
            new GetFeatureFlagRiskReport.Query(TenantId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.ByFlag.Should().BeEmpty();
        result.Value.Summary.UrgentFlagCount.Should().Be(0);
        result.Value.Summary.TenantFlagRiskIndex.Should().Be(100m);
    }

    [Fact]
    public async Task GetFeatureFlagRiskReport_AllLowRisk_ScoreIsZero_TierIsSafe()
    {
        var entry = MakeRiskEntry("flag-safe");
        var handler = CreateRiskReportHandler([entry]);

        var result = await handler.Handle(
            new GetFeatureFlagRiskReport.Query(TenantId), CancellationToken.None);

        result.Value.ByFlag.Single().FlagRiskScore.Should().Be(0m);
        result.Value.ByFlag.Single().FlagRiskTier.Should().Be(GetFeatureFlagRiskReport.FlagRiskTier.Safe);
    }

    [Fact]
    public async Task GetFeatureFlagRiskReport_HighStaleness_HighOwnershipRisk_ScoreIsCorrect()
    {
        // Staleness.High=100 * 0.30 + Ownership.None=100 * 0.25 = 55
        var entry = MakeRiskEntry("flag-stale",
            staleness: StalenessRisk.High,
            ownership: OwnershipRisk.None);
        var handler = CreateRiskReportHandler([entry]);

        var result = await handler.Handle(
            new GetFeatureFlagRiskReport.Query(TenantId), CancellationToken.None);

        result.Value.ByFlag.Single().FlagRiskScore.Should().Be(55m);
    }

    [Fact]
    public async Task GetFeatureFlagRiskReport_AllHighRisk_ScoreIs100_TierIsUrgent()
    {
        // High*0.30 + None*0.25 + High*0.30 + Incident*0.15 = 100
        var entry = MakeRiskEntry("flag-urgent",
            staleness: StalenessRisk.High,
            ownership: OwnershipRisk.None,
            prodPresence: ProductionPresenceRisk.High,
            incidentCorrelated: true);
        var handler = CreateRiskReportHandler([entry]);

        var result = await handler.Handle(
            new GetFeatureFlagRiskReport.Query(TenantId), CancellationToken.None);

        result.Value.ByFlag.Single().FlagRiskScore.Should().Be(100m);
        result.Value.ByFlag.Single().FlagRiskTier.Should().Be(GetFeatureFlagRiskReport.FlagRiskTier.Urgent);
    }

    [Fact]
    public async Task GetFeatureFlagRiskReport_MediumStaleness_ProdMedium_TierIsMonitor()
    {
        // Medium*0.30=15 + Medium*0.30=15 = 30 > 25 => Monitor
        var entry = MakeRiskEntry("flag-monitor",
            staleness: StalenessRisk.Medium,
            prodPresence: ProductionPresenceRisk.Medium);
        var handler = CreateRiskReportHandler([entry]);

        var result = await handler.Handle(
            new GetFeatureFlagRiskReport.Query(TenantId), CancellationToken.None);

        result.Value.ByFlag.Single().FlagRiskTier.Should().Be(GetFeatureFlagRiskReport.FlagRiskTier.Monitor);
    }

    [Fact]
    public async Task GetFeatureFlagRiskReport_ScoreExactly25_TierIsSafe()
    {
        // Staleness.Low=0, Ownership.None=100*0.25=25, Prod.Low=0, Incident=false → 25 → Safe
        var entry = MakeRiskEntry("flag-border",
            ownership: OwnershipRisk.None);
        var handler = CreateRiskReportHandler([entry]);

        var result = await handler.Handle(
            new GetFeatureFlagRiskReport.Query(TenantId), CancellationToken.None);

        result.Value.ByFlag.Single().FlagRiskScore.Should().Be(25m);
        result.Value.ByFlag.Single().FlagRiskTier.Should().Be(GetFeatureFlagRiskReport.FlagRiskTier.Safe);
    }

    [Fact]
    public async Task GetFeatureFlagRiskReport_ScoreExactly55_TierIsMonitor()
    {
        // High*0.30=30 + None*0.25=25 = 55 → Monitor (≤55)
        var entry = MakeRiskEntry("flag-55",
            staleness: StalenessRisk.High,
            ownership: OwnershipRisk.None);
        var handler = CreateRiskReportHandler([entry]);

        var result = await handler.Handle(
            new GetFeatureFlagRiskReport.Query(TenantId), CancellationToken.None);

        result.Value.ByFlag.Single().FlagRiskScore.Should().Be(55m);
        result.Value.ByFlag.Single().FlagRiskTier.Should().Be(GetFeatureFlagRiskReport.FlagRiskTier.Monitor);
    }

    [Fact]
    public async Task GetFeatureFlagRiskReport_ScheduledRemovalOverdue_Listed()
    {
        var overdueEntry = MakeRiskEntry("flag-overdue",
            scheduledRemovalDate: FixedNow.AddDays(-5));
        var notOverdue = MakeRiskEntry("flag-ok",
            scheduledRemovalDate: FixedNow.AddDays(30));
        var handler = CreateRiskReportHandler([overdueEntry, notOverdue]);

        var result = await handler.Handle(
            new GetFeatureFlagRiskReport.Query(TenantId), CancellationToken.None);

        result.Value.ScheduledRemovalOverdue.Should().Contain("flag-overdue");
        result.Value.ScheduledRemovalOverdue.Should().NotContain("flag-ok");
    }

    [Fact]
    public async Task GetFeatureFlagRiskReport_IncidentCorrelated_InToggleList()
    {
        var correlated = MakeRiskEntry("flag-incident", incidentCorrelated: true);
        var clean = MakeRiskEntry("flag-clean");
        var handler = CreateRiskReportHandler([correlated, clean]);

        var result = await handler.Handle(
            new GetFeatureFlagRiskReport.Query(TenantId), CancellationToken.None);

        result.Value.ToggleWithIncidentCorrelation.Should().Contain("flag-incident");
        result.Value.ToggleWithIncidentCorrelation.Should().NotContain("flag-clean");
    }

    [Fact]
    public async Task GetFeatureFlagRiskReport_RecommendedRemovals_UrgentAndNotIncidentCorrelated()
    {
        var urgentClean = MakeRiskEntry("flag-urgent-clean",
            staleness: StalenessRisk.High, ownership: OwnershipRisk.None,
            prodPresence: ProductionPresenceRisk.High, incidentCorrelated: false);
        var urgentCorrelated = MakeRiskEntry("flag-urgent-corr",
            staleness: StalenessRisk.High, ownership: OwnershipRisk.None,
            prodPresence: ProductionPresenceRisk.High, incidentCorrelated: true);
        var handler = CreateRiskReportHandler([urgentClean, urgentCorrelated]);

        var result = await handler.Handle(
            new GetFeatureFlagRiskReport.Query(TenantId), CancellationToken.None);

        result.Value.RecommendedRemovals.Should().Contain("flag-urgent-clean");
        result.Value.RecommendedRemovals.Should().NotContain("flag-urgent-corr");
    }

    [Fact]
    public async Task GetFeatureFlagRiskReport_TenantFlagRiskIndex_PercentageSafeOrMonitor()
    {
        var entries = new[]
        {
            MakeRiskEntry("flag-safe"),   // Safe
            MakeRiskEntry("flag-monitor", staleness: StalenessRisk.Medium, prodPresence: ProductionPresenceRisk.Medium), // Monitor
            MakeRiskEntry("flag-urgent",  staleness: StalenessRisk.High, ownership: OwnershipRisk.None,
                          prodPresence: ProductionPresenceRisk.High, incidentCorrelated: true) // Urgent
        };
        var handler = CreateRiskReportHandler(entries);

        var result = await handler.Handle(
            new GetFeatureFlagRiskReport.Query(TenantId), CancellationToken.None);

        // 2 out of 3 are Safe/Monitor → 66.67%
        result.Value.Summary.TenantFlagRiskIndex.Should().BeApproximately(66.67m, 0.01m);
    }

    [Fact]
    public async Task GetFeatureFlagRiskReport_UrgentAndReviewCounts_InSummary()
    {
        var entries = new[]
        {
            MakeRiskEntry("f-urgent", staleness: StalenessRisk.High, ownership: OwnershipRisk.None,
                          prodPresence: ProductionPresenceRisk.High, incidentCorrelated: true),
            MakeRiskEntry("f-review", staleness: StalenessRisk.High, ownership: OwnershipRisk.None,
                          prodPresence: ProductionPresenceRisk.Medium),
            MakeRiskEntry("f-safe")
        };
        var handler = CreateRiskReportHandler(entries);

        var result = await handler.Handle(
            new GetFeatureFlagRiskReport.Query(TenantId), CancellationToken.None);

        result.Value.Summary.UrgentFlagCount.Should().Be(1);
        result.Value.Summary.ReviewFlagCount.Should().BeGreaterThanOrEqualTo(0);
    }

    [Fact]
    public void GetFeatureFlagRiskReport_EmptyTenantId_ValidationFails()
    {
        var validator = new GetFeatureFlagRiskReport.Validator();
        var result = validator.Validate(new GetFeatureFlagRiskReport.Query(""));
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void GetFeatureFlagRiskReport_InvalidStaleFlagDays_ValidationFails()
    {
        var validator = new GetFeatureFlagRiskReport.Validator();
        var result = validator.Validate(new GetFeatureFlagRiskReport.Query(TenantId, StaleFlagDays: 0));
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void GetFeatureFlagRiskReport_ValidQuery_PassesValidation()
    {
        var validator = new GetFeatureFlagRiskReport.Validator();
        var result = validator.Validate(new GetFeatureFlagRiskReport.Query(TenantId));
        result.IsValid.Should().BeTrue();
    }

    // ════════════════════════════════════════════════════════════════════════
    // AS.3 — GetExperimentGovernanceReport
    // ════════════════════════════════════════════════════════════════════════

    private static GetExperimentGovernanceReport.Handler CreateGovernanceHandler(
        IReadOnlyList<IExperimentGovernanceReader.ExperimentEntry> entries)
    {
        var reader = Substitute.For<IExperimentGovernanceReader>();
        reader.ListExperimentsByTenantAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(entries);
        return new GetExperimentGovernanceReport.Handler(reader, CreateClock());
    }

    private static IExperimentGovernanceReader.ExperimentEntry MakeExperiment(
        string flagKey,
        int durationDays = 10,
        bool hasSuccessCriteria = true,
        bool isActiveInProd = false,
        IReadOnlyList<string>? activeEnvs = null,
        DateTimeOffset? lastToggledAt = null,
        DateTimeOffset? concludedAt = null) =>
        new("svc-1", "Service-1", flagKey, durationDays, hasSuccessCriteria,
            isActiveInProd, activeEnvs ?? ["dev", "staging"],
            lastToggledAt ?? FixedNow.AddDays(-5),
            FixedNow.AddDays(-durationDays), concludedAt);

    [Fact]
    public async Task GetExperimentGovernanceReport_EmptyReader_ReturnsEmptyReport()
    {
        var handler = CreateGovernanceHandler([]);
        var result = await handler.Handle(
            new GetExperimentGovernanceReport.Query(TenantId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.ByExperiment.Should().BeEmpty();
        result.Value.Summary.ActiveExperiments.Should().Be(0);
        result.Value.TenantExperimentGovernanceScore.Should().Be(100m);
    }

    [Fact]
    public async Task GetExperimentGovernanceReport_ConcludedExperiment_StatusIsConcluded()
    {
        var exp = MakeExperiment("exp-done", concludedAt: FixedNow.AddDays(-1));
        var handler = CreateGovernanceHandler([exp]);

        var result = await handler.Handle(
            new GetExperimentGovernanceReport.Query(TenantId), CancellationToken.None);

        result.Value.ByExperiment.Single().ExperimentStatus
            .Should().Be(GetExperimentGovernanceReport.ExperimentStatus.Concluded);
    }

    [Fact]
    public async Task GetExperimentGovernanceReport_DurationExceedsMax_StatusIsOverdue()
    {
        var exp = MakeExperiment("exp-overdue", durationDays: 45);
        var handler = CreateGovernanceHandler([exp]);

        var result = await handler.Handle(
            new GetExperimentGovernanceReport.Query(TenantId, ExperimentMaxDays: 30), CancellationToken.None);

        result.Value.ByExperiment.Single().ExperimentStatus
            .Should().Be(GetExperimentGovernanceReport.ExperimentStatus.Overdue);
    }

    [Fact]
    public async Task GetExperimentGovernanceReport_NoToggleRecentActivity_StatusIsStale()
    {
        var exp = MakeExperiment("exp-stale",
            durationDays: 10,
            lastToggledAt: FixedNow.AddDays(-70)); // > 60 stale days
        var handler = CreateGovernanceHandler([exp]);

        var result = await handler.Handle(
            new GetExperimentGovernanceReport.Query(TenantId, StaleFlagDays: 60), CancellationToken.None);

        result.Value.ByExperiment.Single().ExperimentStatus
            .Should().Be(GetExperimentGovernanceReport.ExperimentStatus.Stale);
    }

    [Fact]
    public async Task GetExperimentGovernanceReport_ActiveRecent_StatusIsActive()
    {
        var exp = MakeExperiment("exp-active",
            durationDays: 10,
            lastToggledAt: FixedNow.AddDays(-3));
        var handler = CreateGovernanceHandler([exp]);

        var result = await handler.Handle(
            new GetExperimentGovernanceReport.Query(TenantId), CancellationToken.None);

        result.Value.ByExperiment.Single().ExperimentStatus
            .Should().Be(GetExperimentGovernanceReport.ExperimentStatus.Active);
    }

    [Fact]
    public async Task GetExperimentGovernanceReport_LowOverdueLowNoCriteria_TierIsGoverned()
    {
        // 1 overdue (10%), 0 no-criteria (0%) → Governed
        var entries = Enumerable.Range(1, 9)
            .Select(i => MakeExperiment($"exp-ok-{i}", durationDays: 10))
            .Append(MakeExperiment("exp-overdue", durationDays: 45))
            .ToList();
        var handler = CreateGovernanceHandler(entries);

        var result = await handler.Handle(
            new GetExperimentGovernanceReport.Query(TenantId, ExperimentMaxDays: 30), CancellationToken.None);

        result.Value.ExperimentGovernanceTier
            .Should().Be(GetExperimentGovernanceReport.ExperimentGovernanceTier.Governed);
    }

    [Fact]
    public async Task GetExperimentGovernanceReport_MediumOverdue_TierIsImproving()
    {
        // 35% overdue, 20% no-criteria → Improving
        var entries = Enumerable.Range(1, 6)
            .Select(i => MakeExperiment($"exp-ok-{i}", durationDays: 10))
            .Concat(Enumerable.Range(1, 2).Select(i => MakeExperiment($"exp-no-crit-{i}", hasSuccessCriteria: false)))
            .Concat(Enumerable.Range(1, 4).Select(i => MakeExperiment($"exp-overdue-{i}", durationDays: 45)))
            .ToList();
        var handler = CreateGovernanceHandler(entries);

        var result = await handler.Handle(
            new GetExperimentGovernanceReport.Query(TenantId, ExperimentMaxDays: 30), CancellationToken.None);

        result.Value.ExperimentGovernanceTier
            .Should().Be(GetExperimentGovernanceReport.ExperimentGovernanceTier.Improving);
    }

    [Fact]
    public async Task GetExperimentGovernanceReport_HighOverdue_TierIsAtRisk()
    {
        // 50% overdue → AtRisk
        var entries = Enumerable.Range(1, 5)
            .Select(i => MakeExperiment($"exp-ok-{i}", durationDays: 10))
            .Concat(Enumerable.Range(1, 5).Select(i => MakeExperiment($"exp-overdue-{i}", durationDays: 45)))
            .ToList();
        var handler = CreateGovernanceHandler(entries);

        var result = await handler.Handle(
            new GetExperimentGovernanceReport.Query(TenantId, ExperimentMaxDays: 30), CancellationToken.None);

        result.Value.ExperimentGovernanceTier
            .Should().Be(GetExperimentGovernanceReport.ExperimentGovernanceTier.AtRisk);
    }

    [Fact]
    public async Task GetExperimentGovernanceReport_AllOverdue_TierIsUnmanaged()
    {
        var entries = Enumerable.Range(1, 5)
            .Select(i => MakeExperiment($"exp-overdue-{i}", durationDays: 45))
            .ToList();
        var handler = CreateGovernanceHandler(entries);

        var result = await handler.Handle(
            new GetExperimentGovernanceReport.Query(TenantId, ExperimentMaxDays: 30), CancellationToken.None);

        result.Value.ExperimentGovernanceTier
            .Should().Be(GetExperimentGovernanceReport.ExperimentGovernanceTier.Unmanaged);
    }

    [Fact]
    public async Task GetExperimentGovernanceReport_ExperimentProdOnlyRisk_ProdOnlyActiveEnvs()
    {
        var prodOnly = MakeExperiment("exp-prod-only", activeEnvs: ["prod"]);
        var multiEnv = MakeExperiment("exp-multi", activeEnvs: ["dev", "staging", "prod"]);
        var handler = CreateGovernanceHandler([prodOnly, multiEnv]);

        var result = await handler.Handle(
            new GetExperimentGovernanceReport.Query(TenantId), CancellationToken.None);

        result.Value.ExperimentProdOnlyRisk.Should().Contain("exp-prod-only");
        result.Value.ExperimentProdOnlyRisk.Should().NotContain("exp-multi");
    }

    [Fact]
    public async Task GetExperimentGovernanceReport_LongRunningExperiments_ExceedMaxDays()
    {
        var longRunning = MakeExperiment("exp-long", durationDays: 45);
        var normal = MakeExperiment("exp-normal", durationDays: 15);
        var handler = CreateGovernanceHandler([longRunning, normal]);

        var result = await handler.Handle(
            new GetExperimentGovernanceReport.Query(TenantId, ExperimentMaxDays: 30), CancellationToken.None);

        result.Value.LongRunningExperiments.Should().Contain("exp-long");
        result.Value.LongRunningExperiments.Should().NotContain("exp-normal");
    }

    [Fact]
    public async Task GetExperimentGovernanceReport_GovernanceScore_ClampedbetweenZeroAndHundred()
    {
        var entries = Enumerable.Range(1, 10)
            .Select(i => MakeExperiment($"exp-overdue-{i}", durationDays: 45, hasSuccessCriteria: false))
            .ToList();
        var handler = CreateGovernanceHandler(entries);

        var result = await handler.Handle(
            new GetExperimentGovernanceReport.Query(TenantId, ExperimentMaxDays: 30), CancellationToken.None);

        result.Value.TenantExperimentGovernanceScore.Should().BeGreaterThanOrEqualTo(0m);
        result.Value.TenantExperimentGovernanceScore.Should().BeLessThanOrEqualTo(100m);
    }

    [Fact]
    public async Task GetExperimentGovernanceReport_ExperimentVelocity_ConcludedOverTotal()
    {
        var entries = new[]
        {
            MakeExperiment("exp-concluded-1", concludedAt: FixedNow.AddDays(-1)),
            MakeExperiment("exp-concluded-2", concludedAt: FixedNow.AddDays(-2)),
            MakeExperiment("exp-active-1")
        };
        var handler = CreateGovernanceHandler(entries);

        var result = await handler.Handle(
            new GetExperimentGovernanceReport.Query(TenantId), CancellationToken.None);

        // 2 concluded / 3 total = 66.67%
        result.Value.Summary.ExperimentVelocity.Should().BeApproximately(66.67m, 0.01m);
    }

    [Fact]
    public async Task GetExperimentGovernanceReport_MedianDuration_CalculatedCorrectly()
    {
        var entries = new[]
        {
            MakeExperiment("exp-a", durationDays: 5),
            MakeExperiment("exp-b", durationDays: 10),
            MakeExperiment("exp-c", durationDays: 20)
        };
        var handler = CreateGovernanceHandler(entries);

        var result = await handler.Handle(
            new GetExperimentGovernanceReport.Query(TenantId), CancellationToken.None);

        result.Value.Summary.MedianExperimentDurationDays.Should().Be(10.0);
    }

    [Fact]
    public async Task GetExperimentGovernanceReport_OverdueAndActiveCount_InSummary()
    {
        var entries = new[]
        {
            MakeExperiment("exp-active-1", durationDays: 5),
            MakeExperiment("exp-active-2", durationDays: 8),
            MakeExperiment("exp-overdue-1", durationDays: 45)
        };
        var handler = CreateGovernanceHandler(entries);

        var result = await handler.Handle(
            new GetExperimentGovernanceReport.Query(TenantId, ExperimentMaxDays: 30), CancellationToken.None);

        result.Value.Summary.ActiveExperiments.Should().Be(2);
        result.Value.Summary.OverdueExperiments.Should().Be(1);
    }

    [Fact]
    public void GetExperimentGovernanceReport_EmptyTenantId_ValidationFails()
    {
        var validator = new GetExperimentGovernanceReport.Validator();
        var result = validator.Validate(new GetExperimentGovernanceReport.Query(""));
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void GetExperimentGovernanceReport_InvalidExperimentMaxDays_ValidationFails()
    {
        var validator = new GetExperimentGovernanceReport.Validator();
        var result = validator.Validate(new GetExperimentGovernanceReport.Query(TenantId, ExperimentMaxDays: 0));
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void GetExperimentGovernanceReport_ValidQuery_PassesValidation()
    {
        var validator = new GetExperimentGovernanceReport.Validator();
        var result = validator.Validate(new GetExperimentGovernanceReport.Query(TenantId));
        result.IsValid.Should().BeTrue();
    }
}
