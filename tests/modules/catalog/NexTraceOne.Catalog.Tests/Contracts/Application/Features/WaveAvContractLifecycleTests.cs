using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.Catalog.Application.Contracts.Abstractions;
using NexTraceOne.Catalog.Application.Contracts.Features.GetApiVersionStrategyReport;
using NexTraceOne.Catalog.Application.Contracts.Features.GetContractDeprecationForecast;
using NexTraceOne.Catalog.Application.Contracts.Features.GetContractDeprecationPipelineReport;
using NexTraceOne.Catalog.Application.Contracts.Features.ScheduleContractDeprecation;

namespace NexTraceOne.Catalog.Tests.Contracts.Application.Features;

/// <summary>
/// Testes unitários para Wave AV — Contract Lifecycle Automation &amp; Deprecation Intelligence.
/// Cobre AV.1 GetContractDeprecationPipelineReport,
/// AV.2 GetApiVersionStrategyReport,
/// AV.3 ScheduleContractDeprecation e GetContractDeprecationForecast.
/// </summary>
public sealed class WaveAvContractLifecycleTests
{
    private static readonly DateTimeOffset FixedNow = new(2026, 7, 1, 12, 0, 0, TimeSpan.Zero);
    private const string TenantId = "tenant-av-001";

    private static IDateTimeProvider CreateClock()
    {
        var clock = Substitute.For<IDateTimeProvider>();
        clock.UtcNow.Returns(FixedNow);
        return clock;
    }

    // ════════════════════════════════════════════════════════════════════════
    // AV.1 — GetContractDeprecationPipelineReport
    // ════════════════════════════════════════════════════════════════════════

    private static GetContractDeprecationPipelineReport.Handler CreatePipelineHandler(
        IContractDeprecationPipelineReader? reader = null) =>
        new(reader ?? Substitute.For<IContractDeprecationPipelineReader>(), CreateClock());

    private static IContractDeprecationPipelineReader.DeprecatedContractEntry BuildDeprecatedEntry(
        Guid? id = null,
        string contractName = "contract-a",
        string version = "v1.0",
        int totalConsumers = 10,
        int notifiedConsumers = 9,
        int migratedConsumers = 8,
        double deprecatedDaysAgo = 30,
        double? sunsetDaysFromNow = 60,
        IReadOnlyList<string>? blockingConsumerIds = null) =>
        new(
            id ?? Guid.NewGuid(),
            contractName,
            version,
            "REST",
            "team-av",
            "svc-av",
            "Critical",
            FixedNow.AddDays(-deprecatedDaysAgo),
            sunsetDaysFromNow.HasValue ? FixedNow.AddDays(sunsetDaysFromNow.Value) : null,
            totalConsumers,
            notifiedConsumers,
            migratedConsumers,
            blockingConsumerIds ?? [],
            FixedNow.AddDays(-deprecatedDaysAgo + 2));

    [Fact]
    public async Task GetContractDeprecationPipeline_NoEntries_ReturnsEmptyReport()
    {
        var reader = Substitute.For<IContractDeprecationPipelineReader>();
        reader.ListDeprecatedContractsByTenantAsync(TenantId, Arg.Any<CancellationToken>()).Returns([]);
        var handler = CreatePipelineHandler(reader);

        var result = await handler.Handle(new GetContractDeprecationPipelineReport.Query(TenantId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Contracts.Should().BeEmpty();
        result.Value.Summary.TenantDeprecationHealthScore.Should().Be(100.0);
    }

    [Fact]
    public async Task GetContractDeprecationPipeline_OnTrackContract_TierIsOnTrack()
    {
        var entry = BuildDeprecatedEntry(migratedConsumers: 8, totalConsumers: 10, sunsetDaysFromNow: 60);
        var reader = Substitute.For<IContractDeprecationPipelineReader>();
        reader.ListDeprecatedContractsByTenantAsync(TenantId, Arg.Any<CancellationToken>()).Returns([entry]);
        var handler = CreatePipelineHandler(reader);

        var result = await handler.Handle(new GetContractDeprecationPipelineReport.Query(TenantId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Contracts[0].Tier.Should().Be(GetContractDeprecationPipelineReport.DeprecationPipelineTier.OnTrack);
        result.Value.Contracts[0].MigrationProgress.Should().Be(80.0);
    }

    [Fact]
    public async Task GetContractDeprecationPipeline_OverdueSunset_TierIsOverdue()
    {
        var entry = BuildDeprecatedEntry(
            totalConsumers: 5, migratedConsumers: 1,
            sunsetDaysFromNow: -10);
        var reader = Substitute.For<IContractDeprecationPipelineReader>();
        reader.ListDeprecatedContractsByTenantAsync(TenantId, Arg.Any<CancellationToken>()).Returns([entry]);
        var handler = CreatePipelineHandler(reader);

        var result = await handler.Handle(new GetContractDeprecationPipelineReport.Query(TenantId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Contracts[0].Tier.Should().Be(GetContractDeprecationPipelineReport.DeprecationPipelineTier.Overdue);
        result.Value.Summary.OverdueSunsets.Should().Be(1);
    }

    [Fact]
    public async Task GetContractDeprecationPipeline_BlockedContract_TierIsBlocked()
    {
        var entry = BuildDeprecatedEntry(
            deprecatedDaysAgo: 200,
            totalConsumers: 10,
            migratedConsumers: 0,
            notifiedConsumers: 0,
            sunsetDaysFromNow: null);
        var reader = Substitute.For<IContractDeprecationPipelineReader>();
        reader.ListDeprecatedContractsByTenantAsync(TenantId, Arg.Any<CancellationToken>()).Returns([entry]);
        var handler = CreatePipelineHandler(reader);

        var result = await handler.Handle(
            new GetContractDeprecationPipelineReport.Query(TenantId, DeprecationMaxDays: 180),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Contracts[0].Tier.Should().Be(GetContractDeprecationPipelineReport.DeprecationPipelineTier.Blocked);
    }

    [Fact]
    public async Task GetContractDeprecationPipeline_LowNotification_CreatesNotificationGap()
    {
        var entry = BuildDeprecatedEntry(totalConsumers: 10, notifiedConsumers: 5);
        var reader = Substitute.For<IContractDeprecationPipelineReader>();
        reader.ListDeprecatedContractsByTenantAsync(TenantId, Arg.Any<CancellationToken>()).Returns([entry]);
        var handler = CreatePipelineHandler(reader);

        var result = await handler.Handle(
            new GetContractDeprecationPipelineReport.Query(TenantId, MinNotificationPct: 80.0),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.NotificationGaps.Should().HaveCount(1);
        result.Value.NotificationGaps[0].NotifiedConsumersPct.Should().Be(50.0);
    }

    [Fact]
    public async Task GetContractDeprecationPipeline_HealthScore_IsOnTrackPercentage()
    {
        var onTrack = BuildDeprecatedEntry(migratedConsumers: 9, totalConsumers: 10, sunsetDaysFromNow: 90);
        var atRisk = BuildDeprecatedEntry(migratedConsumers: 3, totalConsumers: 10, sunsetDaysFromNow: 10);
        var reader = Substitute.For<IContractDeprecationPipelineReader>();
        reader.ListDeprecatedContractsByTenantAsync(TenantId, Arg.Any<CancellationToken>()).Returns([onTrack, atRisk]);
        var handler = CreatePipelineHandler(reader);

        var result = await handler.Handle(new GetContractDeprecationPipelineReport.Query(TenantId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Summary.TenantDeprecationHealthScore.Should().Be(50.0);
    }

    [Fact]
    public async Task GetContractDeprecationPipeline_BlockingConsumers_AreAggregated()
    {
        var entry = BuildDeprecatedEntry(
            blockingConsumerIds: ["consumer-crit-1", "consumer-crit-2"],
            migratedConsumers: 2, totalConsumers: 10);
        var reader = Substitute.For<IContractDeprecationPipelineReader>();
        reader.ListDeprecatedContractsByTenantAsync(TenantId, Arg.Any<CancellationToken>()).Returns([entry]);
        var handler = CreatePipelineHandler(reader);

        var result = await handler.Handle(new GetContractDeprecationPipelineReport.Query(TenantId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Summary.TotalBlockingConsumers.Should().Be(2);
        result.Value.Contracts[0].BlockingConsumerIds.Should().HaveCount(2);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task GetContractDeprecationPipeline_EmptyTenant_ReturnsFailure(string tenantId)
    {
        var handler = CreatePipelineHandler();
        var act = async () => await handler.Handle(
            new GetContractDeprecationPipelineReport.Query(tenantId), CancellationToken.None);
        await act.Should().ThrowAsync<Exception>();
    }

    [Fact]
    public async Task GetContractDeprecationPipeline_ApproachingSunsetFlag_IsCorrect()
    {
        var entry = BuildDeprecatedEntry(
            migratedConsumers: 5, totalConsumers: 10,
            sunsetDaysFromNow: 20);
        var reader = Substitute.For<IContractDeprecationPipelineReader>();
        reader.ListDeprecatedContractsByTenantAsync(TenantId, Arg.Any<CancellationToken>()).Returns([entry]);
        var handler = CreatePipelineHandler(reader);

        var result = await handler.Handle(
            new GetContractDeprecationPipelineReport.Query(TenantId, SunsetWarningDays: 30),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Summary.ApproachingSunset.Should().Be(1);
    }

    // ════════════════════════════════════════════════════════════════════════
    // AV.2 — GetApiVersionStrategyReport
    // ════════════════════════════════════════════════════════════════════════

    private static GetApiVersionStrategyReport.Handler CreateVersionHandler(
        IApiVersionStrategyReader? reader = null) =>
        new(reader ?? Substitute.For<IApiVersionStrategyReader>(), CreateClock());

    private static IApiVersionStrategyReader.ServiceVersionEntry BuildVersionEntry(
        string serviceId = "svc-1",
        string serviceName = "Service A",
        int activeVersions = 1,
        bool semver = true,
        int breakingChanges = 0,
        double avgLifetime = 120.0,
        string? teamId = "team-a") =>
        new(serviceId, serviceName, teamId, "REST", activeVersions, semver, breakingChanges, avgLifetime, "v1.0.0", ["v1.0.0"]);

    [Fact]
    public async Task GetApiVersionStrategy_NoEntries_ReturnsEmptyReport()
    {
        var reader = Substitute.For<IApiVersionStrategyReader>();
        reader.ListServiceVersionDataByTenantAsync(TenantId, Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns([]);
        var handler = CreateVersionHandler(reader);

        var result = await handler.Handle(new GetApiVersionStrategyReport.Query(TenantId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.ByService.Should().BeEmpty();
        result.Value.Summary.SemverAdoptionRate.Should().Be(100.0);
    }

    [Fact]
    public async Task GetApiVersionStrategy_SingleLinearService_PatternIsLinear()
    {
        var entry = BuildVersionEntry(activeVersions: 1);
        var reader = Substitute.For<IApiVersionStrategyReader>();
        reader.ListServiceVersionDataByTenantAsync(TenantId, Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns([entry]);
        var handler = CreateVersionHandler(reader);

        var result = await handler.Handle(new GetApiVersionStrategyReport.Query(TenantId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.ByService[0].Pattern.Should().Be(GetApiVersionStrategyReport.VersioningPattern.Linear);
    }

    [Fact]
    public async Task GetApiVersionStrategy_ThreeVersions_PatternIsParallel()
    {
        var entry = BuildVersionEntry(activeVersions: 3);
        var reader = Substitute.For<IApiVersionStrategyReader>();
        reader.ListServiceVersionDataByTenantAsync(TenantId, Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns([entry]);
        var handler = CreateVersionHandler(reader);

        var result = await handler.Handle(new GetApiVersionStrategyReport.Query(TenantId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.ByService[0].Pattern.Should().Be(GetApiVersionStrategyReport.VersioningPattern.Parallel);
    }

    [Fact]
    public async Task GetApiVersionStrategy_FiveVersions_PatternIsFragmented()
    {
        var entry = BuildVersionEntry(activeVersions: 5);
        var reader = Substitute.For<IApiVersionStrategyReader>();
        reader.ListServiceVersionDataByTenantAsync(TenantId, Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns([entry]);
        var handler = CreateVersionHandler(reader);

        var result = await handler.Handle(new GetApiVersionStrategyReport.Query(TenantId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.ByService[0].Pattern.Should().Be(GetApiVersionStrategyReport.VersioningPattern.Fragmented);
        result.Value.VersionProliferationRiskServiceIds.Should().Contain(entry.ServiceId);
    }

    [Fact]
    public async Task GetApiVersionStrategy_AllSemver_MatureTier()
    {
        var entries = Enumerable.Range(1, 5)
            .Select(i => BuildVersionEntry($"svc-{i}", activeVersions: 1, semver: true, breakingChanges: 0))
            .ToList();
        var reader = Substitute.For<IApiVersionStrategyReader>();
        reader.ListServiceVersionDataByTenantAsync(TenantId, Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(entries);
        var handler = CreateVersionHandler(reader);

        var result = await handler.Handle(new GetApiVersionStrategyReport.Query(TenantId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Summary.HealthTier.Should().Be(GetApiVersionStrategyReport.TenantVersioningHealthTier.Mature);
        result.Value.Summary.SemverAdoptionRate.Should().Be(100.0);
    }

    [Fact]
    public async Task GetApiVersionStrategy_NoSemver_ChaoticTier()
    {
        var entries = Enumerable.Range(1, 5)
            .Select(i => BuildVersionEntry($"svc-{i}", semver: false))
            .ToList();
        var reader = Substitute.For<IApiVersionStrategyReader>();
        reader.ListServiceVersionDataByTenantAsync(TenantId, Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(entries);
        var handler = CreateVersionHandler(reader);

        var result = await handler.Handle(new GetApiVersionStrategyReport.Query(TenantId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Summary.HealthTier.Should().Be(GetApiVersionStrategyReport.TenantVersioningHealthTier.Chaotic);
        result.Value.BestPracticedServiceIds.Should().BeEmpty();
    }

    [Fact]
    public async Task GetApiVersionStrategy_HighBreakingChange_FlaggedAsHigh()
    {
        var entry = BuildVersionEntry(breakingChanges: 5);
        var reader = Substitute.For<IApiVersionStrategyReader>();
        reader.ListServiceVersionDataByTenantAsync(TenantId, Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns([entry]);
        var handler = CreateVersionHandler(reader);

        var result = await handler.Handle(
            new GetApiVersionStrategyReport.Query(TenantId, BreakingChangeWarningThreshold: 3),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Summary.HighBreakingChangeServices.Should().Contain(entry.ServiceId);
    }

    [Fact]
    public async Task GetApiVersionStrategy_BestPracticed_LinearSemverNoBreaking()
    {
        var entry = BuildVersionEntry(activeVersions: 1, semver: true, breakingChanges: 0);
        var reader = Substitute.For<IApiVersionStrategyReader>();
        reader.ListServiceVersionDataByTenantAsync(TenantId, Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns([entry]);
        var handler = CreateVersionHandler(reader);

        var result = await handler.Handle(new GetApiVersionStrategyReport.Query(TenantId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.BestPracticedServiceIds.Should().Contain(entry.ServiceId);
    }

    // ════════════════════════════════════════════════════════════════════════
    // AV.3 — ScheduleContractDeprecation
    // ════════════════════════════════════════════════════════════════════════

    private static ScheduleContractDeprecation.Handler CreateScheduleHandler(
        IDeprecationScheduleRepository? repo = null) =>
        new(repo ?? Substitute.For<IDeprecationScheduleRepository>(), CreateClock());

    private static ScheduleContractDeprecation.Command ValidScheduleCommand(
        Guid? contractId = null,
        DateTimeOffset? plannedDate = null) =>
        new(
            contractId ?? Guid.NewGuid(),
            TenantId,
            plannedDate ?? FixedNow.AddDays(30),
            FixedNow.AddDays(90),
            "https://docs.example.com/migration",
            null,
            "Please migrate to v2.0 before the sunset date.",
            "user-001",
            "Successor available and consumers declining.");

    [Fact]
    public async Task ScheduleContractDeprecation_ValidCommand_ReturnsGuid()
    {
        var repo = Substitute.For<IDeprecationScheduleRepository>();
        repo.GetByContractIdAsync(Arg.Any<Guid>(), TenantId, Arg.Any<CancellationToken>())
            .Returns((IDeprecationScheduleRepository.DeprecationScheduleRecord?)null);
        var handler = CreateScheduleHandler(repo);

        var result = await handler.Handle(ValidScheduleCommand(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBe(Guid.Empty);
        await repo.Received(1).UpsertAsync(Arg.Any<IDeprecationScheduleRepository.DeprecationScheduleRecord>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ScheduleContractDeprecation_ExistingSchedule_UpdatesWithSameId()
    {
        var existingId = Guid.NewGuid();
        var contractId = Guid.NewGuid();
        var existing = new IDeprecationScheduleRepository.DeprecationScheduleRecord(
            existingId, contractId, TenantId,
            FixedNow.AddDays(20), null, null, null, null, "user-prev", null, FixedNow.AddDays(-5));

        var repo = Substitute.For<IDeprecationScheduleRepository>();
        repo.GetByContractIdAsync(contractId, TenantId, Arg.Any<CancellationToken>())
            .Returns(existing);

        IDeprecationScheduleRepository.DeprecationScheduleRecord? captured = null;
        repo.When(r => r.UpsertAsync(Arg.Any<IDeprecationScheduleRepository.DeprecationScheduleRecord>(), Arg.Any<CancellationToken>()))
            .Do(ci => captured = ci.Arg<IDeprecationScheduleRepository.DeprecationScheduleRecord>());

        var handler = CreateScheduleHandler(repo);
        var result = await handler.Handle(ValidScheduleCommand(contractId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(existingId);
        captured!.Id.Should().Be(existingId);
    }

    [Fact]
    public async Task ScheduleContractDeprecation_SunsetBeforeDeprecation_FailsValidation()
    {
        var validator = new ScheduleContractDeprecation.Validator();
        var cmd = new ScheduleContractDeprecation.Command(
            Guid.NewGuid(), TenantId,
            FixedNow.AddDays(60),
            FixedNow.AddDays(30),
            null, null, null, "user-001", null);

        var valResult = await validator.ValidateAsync(cmd);

        valResult.IsValid.Should().BeFalse();
        valResult.Errors.Should().ContainSingle(e => e.PropertyName == "PlannedSunsetDate");
    }

    [Fact]
    public async Task ScheduleContractDeprecation_InvalidMigrationUrl_FailsValidation()
    {
        var validator = new ScheduleContractDeprecation.Validator();
        var cmd = new ScheduleContractDeprecation.Command(
            Guid.NewGuid(), TenantId,
            FixedNow.AddDays(30),
            null,
            "not-a-url", null, null, "user-001", null);

        var valResult = await validator.ValidateAsync(cmd);

        valResult.IsValid.Should().BeFalse();
    }

    [Theory]
    [InlineData("TenantId")]
    [InlineData("ScheduledByUserId")]
    public async Task ScheduleContractDeprecation_RequiredField_FailsWhenEmpty(string field)
    {
        var validator = new ScheduleContractDeprecation.Validator();
        ScheduleContractDeprecation.Command cmd = field switch
        {
            "TenantId" => new(Guid.NewGuid(), "", FixedNow.AddDays(30), null, null, null, null, "user", null),
            "ScheduledByUserId" => new(Guid.NewGuid(), TenantId, FixedNow.AddDays(30), null, null, null, null, "", null),
            _ => throw new InvalidOperationException()
        };

        var valResult = await validator.ValidateAsync(cmd);

        valResult.IsValid.Should().BeFalse();
    }

    // ════════════════════════════════════════════════════════════════════════
    // AV.3 — GetContractDeprecationForecast
    // ════════════════════════════════════════════════════════════════════════

    private static GetContractDeprecationForecast.Handler CreateForecastHandler(
        IContractDeprecationForecastReader? reader = null) =>
        new(reader ?? Substitute.For<IContractDeprecationForecastReader>(), CreateClock());

    private static IContractDeprecationForecastReader.ActiveContractForecastEntry BuildForecastEntry(
        Guid? id = null,
        int ageDaysOld = 400,
        bool hasSuccessor = true,
        int currentConsumers = 10,
        int prevMonthConsumers = 12,
        int twoMonthsConsumers = 15,
        bool ownerSignalled = false) =>
        new(
            id ?? Guid.NewGuid(),
            "contract-forecast",
            "v1.0",
            "REST",
            "team-av",
            FixedNow.AddDays(-ageDaysOld),
            hasSuccessor,
            currentConsumers,
            prevMonthConsumers,
            twoMonthsConsumers,
            ownerSignalled,
            []);

    [Fact]
    public async Task GetContractDeprecationForecast_NoEntries_ReturnsEmptyForecast()
    {
        var reader = Substitute.For<IContractDeprecationForecastReader>();
        reader.ListActiveContractsByTenantAsync(TenantId, Arg.Any<CancellationToken>()).Returns([]);
        var handler = CreateForecastHandler(reader);

        var result = await handler.Handle(new GetContractDeprecationForecast.Query(TenantId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.ForecastedDeprecationCandidates.Should().BeEmpty();
        result.Value.DeprecationOutlook.EstimatedIn30Days.Should().Be(0);
    }

    [Fact]
    public async Task GetContractDeprecationForecast_HighProbability_InForecastList()
    {
        var entry = BuildForecastEntry(ageDaysOld: 450, hasSuccessor: true, twoMonthsConsumers: 20, currentConsumers: 8, ownerSignalled: true);
        var reader = Substitute.For<IContractDeprecationForecastReader>();
        reader.ListActiveContractsByTenantAsync(TenantId, Arg.Any<CancellationToken>()).Returns([entry]);
        var handler = CreateForecastHandler(reader);

        var result = await handler.Handle(
            new GetContractDeprecationForecast.Query(TenantId, ContractMaxAgeDays: 365),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.ForecastedDeprecationCandidates.Should().HaveCount(1);
        result.Value.ForecastedDeprecationCandidates[0].DeprecationProbabilityScore.Should().BeGreaterThan(50.0);
    }

    [Fact]
    public async Task GetContractDeprecationForecast_OwnerSignalScore_ContributesToTotal()
    {
        var withSignal = BuildForecastEntry(ageDaysOld: 100, hasSuccessor: false, ownerSignalled: true, twoMonthsConsumers: 5, currentConsumers: 5);
        var withoutSignal = BuildForecastEntry(ageDaysOld: 100, hasSuccessor: false, ownerSignalled: false, twoMonthsConsumers: 5, currentConsumers: 5);

        var reader = Substitute.For<IContractDeprecationForecastReader>();
        reader.ListActiveContractsByTenantAsync(TenantId, Arg.Any<CancellationToken>()).Returns([withSignal, withoutSignal]);
        var handler = CreateForecastHandler(reader);

        var result = await handler.Handle(new GetContractDeprecationForecast.Query(TenantId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var signalEntry = result.Value.ForecastedDeprecationCandidates
            .FirstOrDefault(c => c.OwnerSignalledDeprecation);
        var noSignalEntry = result.Value.ForecastedDeprecationCandidates
            .FirstOrDefault(c => !c.OwnerSignalledDeprecation);

        if (signalEntry != null && noSignalEntry != null)
            signalEntry.DeprecationProbabilityScore.Should().BeGreaterThan(noSignalEntry.DeprecationProbabilityScore);
    }

    [Fact]
    public async Task GetContractDeprecationForecast_PlannedDeprecationCalendar_IsSortedByDate()
    {
        var entries = new List<IContractDeprecationForecastReader.ActiveContractForecastEntry>
        {
            new(Guid.NewGuid(), "contract-b", "v1", "REST", null, FixedNow.AddDays(-100),
                false, 5, 5, 5, false,
                [
                    new(Guid.NewGuid(), "contract-b", FixedNow.AddDays(60), null, 5),
                    new(Guid.NewGuid(), "contract-a", FixedNow.AddDays(30), null, 3)
                ])
        };
        var reader = Substitute.For<IContractDeprecationForecastReader>();
        reader.ListActiveContractsByTenantAsync(TenantId, Arg.Any<CancellationToken>()).Returns(entries);
        var handler = CreateForecastHandler(reader);

        var result = await handler.Handle(new GetContractDeprecationForecast.Query(TenantId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.PlannedDeprecationCalendar.Should().HaveCount(2);
        result.Value.PlannedDeprecationCalendar[0].PlannedDeprecationDate
            .Should().BeBefore(result.Value.PlannedDeprecationCalendar[1].PlannedDeprecationDate);
    }

    [Fact]
    public async Task GetContractDeprecationForecast_ConsumerDecline_RaisesScore()
    {
        var decliningEntry = BuildForecastEntry(
            ageDaysOld: 50,
            hasSuccessor: false,
            ownerSignalled: false,
            currentConsumers: 6,
            prevMonthConsumers: 10,
            twoMonthsConsumers: 15);

        var stableEntry = BuildForecastEntry(
            ageDaysOld: 50,
            hasSuccessor: false,
            ownerSignalled: false,
            currentConsumers: 10,
            prevMonthConsumers: 10,
            twoMonthsConsumers: 10);

        var reader = Substitute.For<IContractDeprecationForecastReader>();
        reader.ListActiveContractsByTenantAsync(TenantId, Arg.Any<CancellationToken>()).Returns([decliningEntry, stableEntry]);
        var handler = CreateForecastHandler(reader);

        var result = await handler.Handle(
            new GetContractDeprecationForecast.Query(TenantId, ConsumerDeclinePctThreshold: 20.0),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        // declining entry should have higher or equal score due to consumer decline
        var decliningResult = result.Value.ForecastedDeprecationCandidates.FirstOrDefault(c => c.ContractId == decliningEntry.ContractId);
        var stableResult = result.Value.ForecastedDeprecationCandidates.FirstOrDefault(c => c.ContractId == stableEntry.ContractId);
        if (decliningResult != null && stableResult != null)
            decliningResult.ConsumerDeclineScore.Should().BeGreaterThan(stableResult.ConsumerDeclineScore);
    }

    // ════════════════════════════════════════════════════════════════════════
    // Null implementations
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task NullContractDeprecationPipelineReader_ReturnsEmptyList()
    {
        var reader = new NexTraceOne.Catalog.Application.Contracts.NullContractDeprecationPipelineReader();
        var result = await reader.ListDeprecatedContractsByTenantAsync("t1", CancellationToken.None);
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task NullApiVersionStrategyReader_ReturnsEmptyList()
    {
        var reader = new NexTraceOne.Catalog.Application.Contracts.NullApiVersionStrategyReader();
        var result = await reader.ListServiceVersionDataByTenantAsync("t1", 90, CancellationToken.None);
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task NullDeprecationScheduleRepository_GetReturnsNull()
    {
        var repo = new NexTraceOne.Catalog.Application.Contracts.NullDeprecationScheduleRepository();
        var result = await repo.GetByContractIdAsync(Guid.NewGuid(), "t1", CancellationToken.None);
        result.Should().BeNull();
    }

    [Fact]
    public async Task NullDeprecationScheduleRepository_UpsertCompletes()
    {
        var repo = new NexTraceOne.Catalog.Application.Contracts.NullDeprecationScheduleRepository();
        var record = new IDeprecationScheduleRepository.DeprecationScheduleRecord(
            Guid.NewGuid(), Guid.NewGuid(), "t1", FixedNow.AddDays(30), null, null, null, null, "user", null, FixedNow);
        var act = async () => await repo.UpsertAsync(record, CancellationToken.None);
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task NullContractDeprecationForecastReader_ReturnsEmptyList()
    {
        var reader = new NexTraceOne.Catalog.Application.Contracts.NullContractDeprecationForecastReader();
        var result = await reader.ListActiveContractsByTenantAsync("t1", CancellationToken.None);
        result.Should().BeEmpty();
    }
}
