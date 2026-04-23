using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.Catalog.Application.DeveloperExperience.Abstractions;
using NexTraceOne.Catalog.Application.DeveloperExperience.Features.GetIdeContractContext;
using NexTraceOne.Catalog.Application.DeveloperExperience.Features.GetIdeServiceContext;
using NexTraceOne.Catalog.Application.DeveloperExperience.Features.RecordIdeUsage;
using NexTraceOne.Notifications.Application.Abstractions;
using NexTraceOne.Notifications.Application.Features.GetNotificationDeliveryReport;
using NexTraceOne.Notifications.Application.Features.GetNotificationEffectivenessReport;
using static NexTraceOne.Catalog.Application.DeveloperExperience.Abstractions.IIDEUsageRepository;

namespace NexTraceOne.Notifications.Tests.Intelligence;

/// <summary>
/// Testes unitários para Wave AK — Developer Experience &amp; Notification Management.
/// AK.1: GetIdeServiceContext, GetIdeContractContext, RecordIdeUsage
/// AK.2: GetNotificationDeliveryReport
/// AK.3: GetNotificationEffectivenessReport
/// </summary>
public sealed class WaveAkDeveloperExperienceNotificationTests
{
    private static readonly DateTimeOffset FixedNow = new(2026, 5, 1, 10, 0, 0, TimeSpan.Zero);
    private const string TenantId = "tenant-ak-test";

    private static IDateTimeProvider CreateClock()
    {
        var clock = Substitute.For<IDateTimeProvider>();
        clock.UtcNow.Returns(FixedNow);
        return clock;
    }

    // ═══════════════════════════════════════════════════════════════════════
    // AK.1 — GetIdeServiceContext
    // ═══════════════════════════════════════════════════════════════════════

    private static GetIdeServiceContext.Handler CreateServiceContextHandler(
        GetIdeServiceContext.ServiceContextSnapshot? snapshot)
    {
        var reader = Substitute.For<IIdeContextReader>();
        reader.GetServiceContextAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(snapshot);
        return new GetIdeServiceContext.Handler(reader, CreateClock());
    }

    [Fact]
    public async Task AK1_GetIdeServiceContext_Found_ReturnsSnapshot()
    {
        var snap = new GetIdeServiceContext.ServiceContextSnapshot(
            "order-service", "team-payments", "Standard", 3,
            "v1.2.0", FixedNow.AddDays(-5), "Stable", 0, "Met", FixedNow);
        var handler = CreateServiceContextHandler(snap);

        var result = await handler.Handle(new GetIdeServiceContext.Query(TenantId, "order-service"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.ServiceName.Should().Be("order-service");
        result.Value.OwnerTeam.Should().Be("team-payments");
        result.Value.ActiveContractCount.Should().Be(3);
        result.Value.GeneratedAt.Should().Be(FixedNow);
    }

    [Fact]
    public async Task AK1_GetIdeServiceContext_NotFound_ReturnsNotFoundError()
    {
        var handler = CreateServiceContextHandler(null);

        var result = await handler.Handle(new GetIdeServiceContext.Query(TenantId, "unknown-service"), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("IDE.ServiceNotFound");
    }

    [Fact]
    public async Task AK1_GetIdeServiceContext_EmptyTenantId_Throws()
    {
        var handler = CreateServiceContextHandler(null);

        var act = async () => await handler.Handle(new GetIdeServiceContext.Query("", "svc"), CancellationToken.None);

        await act.Should().ThrowAsync<Exception>();
    }

    [Fact]
    public async Task AK1_GetIdeServiceContext_EmptyServiceName_Throws()
    {
        var handler = CreateServiceContextHandler(null);

        var act = async () => await handler.Handle(new GetIdeServiceContext.Query(TenantId, ""), CancellationToken.None);

        await act.Should().ThrowAsync<Exception>();
    }

    [Fact]
    public async Task AK1_GetIdeServiceContext_SnapshotGeneratedAtStamped()
    {
        var snap = new GetIdeServiceContext.ServiceContextSnapshot(
            "api-svc", null, null, 0, null, null, null, 0, null, DateTimeOffset.MinValue);
        var handler = CreateServiceContextHandler(snap);

        var result = await handler.Handle(new GetIdeServiceContext.Query(TenantId, "api-svc"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.GeneratedAt.Should().Be(FixedNow);
    }

    // ── Validator ──────────────────────────────────────────────────────────

    [Fact]
    public void AK1_GetIdeServiceContext_Validator_EmptyTenant_Fails()
    {
        var validator = new GetIdeServiceContext.Validator();
        var result = validator.Validate(new GetIdeServiceContext.Query("", "svc"));
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void AK1_GetIdeServiceContext_Validator_Valid_Passes()
    {
        var validator = new GetIdeServiceContext.Validator();
        var result = validator.Validate(new GetIdeServiceContext.Query(TenantId, "my-service"));
        result.IsValid.Should().BeTrue();
    }

    // ═══════════════════════════════════════════════════════════════════════
    // AK.1 — GetIdeContractContext
    // ═══════════════════════════════════════════════════════════════════════

    private static GetIdeContractContext.Handler CreateContractContextHandler(
        GetIdeContractContext.ContractContextSnapshot? snapshot)
    {
        var reader = Substitute.For<IIdeContextReader>();
        reader.GetContractContextAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(snapshot);
        return new GetIdeContractContext.Handler(reader, CreateClock());
    }

    [Fact]
    public async Task AK1_GetIdeContractContext_Found_ReturnsSnapshot()
    {
        var snap = new GetIdeContractContext.ContractContextSnapshot(
            "orders-api", "REST", "v2.1", "Active", 5,
            """{"id": 1}""", FixedNow);
        var handler = CreateContractContextHandler(snap);

        var result = await handler.Handle(new GetIdeContractContext.Query(TenantId, "orders-api"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.ContractName.Should().Be("orders-api");
        result.Value.ContractType.Should().Be("REST");
        result.Value.ConsumerCount.Should().Be(5);
        result.Value.GeneratedAt.Should().Be(FixedNow);
    }

    [Fact]
    public async Task AK1_GetIdeContractContext_NotFound_ReturnsNotFoundError()
    {
        var handler = CreateContractContextHandler(null);

        var result = await handler.Handle(new GetIdeContractContext.Query(TenantId, "ghost-contract"), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("IDE.ContractNotFound");
    }

    [Fact]
    public async Task AK1_GetIdeContractContext_EmptyTenantId_Throws()
    {
        var handler = CreateContractContextHandler(null);

        var act = async () => await handler.Handle(new GetIdeContractContext.Query("", "c"), CancellationToken.None);

        await act.Should().ThrowAsync<Exception>();
    }

    [Fact]
    public void AK1_GetIdeContractContext_Validator_Valid_Passes()
    {
        var validator = new GetIdeContractContext.Validator();
        var result = validator.Validate(new GetIdeContractContext.Query(TenantId, "orders-api"));
        result.IsValid.Should().BeTrue();
    }

    // ═══════════════════════════════════════════════════════════════════════
    // AK.1 — RecordIdeUsage
    // ═══════════════════════════════════════════════════════════════════════

    private static RecordIdeUsage.Handler CreateRecordHandler(
        IIDEUsageRepository? repo = null,
        IDeveloperExperienceUnitOfWork? uow = null)
    {
        repo ??= Substitute.For<IIDEUsageRepository>();
        uow ??= Substitute.For<IDeveloperExperienceUnitOfWork>();
        return new RecordIdeUsage.Handler(repo, uow, CreateClock());
    }

    [Theory]
    [InlineData("ContractLookup")]
    [InlineData("ServiceLookup")]
    [InlineData("ChangeLookup")]
    [InlineData("AiAssistUsed")]
    [InlineData("HealthCheck")]
    public async Task AK1_RecordIdeUsage_AllEventTypes_Succeeds(string eventType)
    {
        var repo = Substitute.For<IIDEUsageRepository>();
        var uow = Substitute.For<IDeveloperExperienceUnitOfWork>();
        var handler = CreateRecordHandler(repo, uow);

        var result = await handler.Handle(
            new RecordIdeUsage.Command("user-1", TenantId, eventType, "some-resource"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.RecordedAt.Should().Be(FixedNow);
        result.Value.RecordId.Should().NotBeEmpty();
        await repo.Received(1).AddAsync(Arg.Any<IdeUsageRecord>(), Arg.Any<CancellationToken>());
        await uow.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AK1_RecordIdeUsage_InvalidEventType_Throws()
    {
        var handler = CreateRecordHandler();

        var act = async () => await handler.Handle(
            new RecordIdeUsage.Command("user-1", TenantId, "InvalidType", null),
            CancellationToken.None);

        await act.Should().ThrowAsync<Exception>();
    }

    [Fact]
    public async Task AK1_RecordIdeUsage_EmptyUserId_Throws()
    {
        var handler = CreateRecordHandler();

        var act = async () => await handler.Handle(
            new RecordIdeUsage.Command("", TenantId, "HealthCheck", null),
            CancellationToken.None);

        await act.Should().ThrowAsync<Exception>();
    }

    [Fact]
    public void AK1_RecordIdeUsage_Validator_InvalidEventType_Fails()
    {
        var validator = new RecordIdeUsage.Validator();
        var result = validator.Validate(new RecordIdeUsage.Command("u", "t", "NotAnEvent", null));
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void AK1_RecordIdeUsage_Validator_Valid_Passes()
    {
        var validator = new RecordIdeUsage.Validator();
        var result = validator.Validate(new RecordIdeUsage.Command("user-1", TenantId, "ContractLookup", "orders-api"));
        result.IsValid.Should().BeTrue();
    }

    // ═══════════════════════════════════════════════════════════════════════
    // AK.2 — GetNotificationDeliveryReport
    // ═══════════════════════════════════════════════════════════════════════

    private static GetNotificationDeliveryReport.Handler CreateDeliveryHandler(
        INotificationDeliveryReportReader.DeliveryReportData data)
    {
        var reader = Substitute.For<INotificationDeliveryReportReader>();
        reader.GetDeliveryDataAsync(Arg.Any<string>(), Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns(data);
        return new GetNotificationDeliveryReport.Handler(reader, CreateClock());
    }

    private static INotificationDeliveryReportReader.DeliveryReportData EmptyDeliveryData()
        => new([], [], []);

    [Fact]
    public async Task AK2_DeliveryReport_EmptyData_Returns100PercentHealthy()
    {
        var handler = CreateDeliveryHandler(EmptyDeliveryData());

        var result = await handler.Handle(new GetNotificationDeliveryReport.Query(TenantId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.OverallDeliverySuccessRate.Should().Be(100m);
        result.Value.OverallHealthTier.Should().Be(GetNotificationDeliveryReport.ChannelHealthTier.Healthy);
        result.Value.TotalDeadLetterCount.Should().Be(0);
    }

    [Fact]
    public async Task AK2_DeliveryReport_AllSuccess_HealthyTier()
    {
        var data = new INotificationDeliveryReportReader.DeliveryReportData(
            [new("Email", 100, 0, 0)],
            [],
            []);
        var handler = CreateDeliveryHandler(data);

        var result = await handler.Handle(new GetNotificationDeliveryReport.Query(TenantId), CancellationToken.None);

        result.Value!.ByChannel[0].HealthTier.Should().Be(GetNotificationDeliveryReport.ChannelHealthTier.Healthy);
        result.Value.ByChannel[0].DeliverySuccessRate.Should().Be(100m);
    }

    [Fact]
    public async Task AK2_DeliveryReport_97PercentSuccess_DegradedTier()
    {
        // 97 success out of 100 total = 97%
        var data = new INotificationDeliveryReportReader.DeliveryReportData(
            [new("Email", 97, 3, 0)],
            [],
            []);
        var handler = CreateDeliveryHandler(data);

        var result = await handler.Handle(new GetNotificationDeliveryReport.Query(TenantId), CancellationToken.None);

        result.Value!.ByChannel[0].HealthTier.Should().Be(GetNotificationDeliveryReport.ChannelHealthTier.Degraded);
        result.Value.ByChannel[0].DeliverySuccessRate.Should().Be(97m);
    }

    [Fact]
    public async Task AK2_DeliveryReport_90PercentSuccess_FailingTier()
    {
        // 90 success out of 100 total = 90%
        var data = new INotificationDeliveryReportReader.DeliveryReportData(
            [new("SMS", 90, 10, 5)],
            [],
            []);
        var handler = CreateDeliveryHandler(data);

        var result = await handler.Handle(new GetNotificationDeliveryReport.Query(TenantId), CancellationToken.None);

        result.Value!.ByChannel[0].HealthTier.Should().Be(GetNotificationDeliveryReport.ChannelHealthTier.Failing);
        result.Value.TotalDeadLetterCount.Should().Be(5);
    }

    [Fact]
    public async Task AK2_DeliveryReport_OverallRateCalculatedAcrossChannels()
    {
        // Email: 100 success, 0 fail; Teams: 80 success, 20 fail → overall 180/200 = 90%
        var data = new INotificationDeliveryReportReader.DeliveryReportData(
            [new("Email", 100, 0, 0), new("Teams", 80, 20, 2)],
            [],
            []);
        var handler = CreateDeliveryHandler(data);

        var result = await handler.Handle(new GetNotificationDeliveryReport.Query(TenantId), CancellationToken.None);

        result.Value!.OverallDeliverySuccessRate.Should().Be(90m);
        result.Value.OverallHealthTier.Should().Be(GetNotificationDeliveryReport.ChannelHealthTier.Failing);
    }

    [Fact]
    public async Task AK2_DeliveryReport_EventTypeDistribution_ComputedCorrectly()
    {
        var data = new INotificationDeliveryReportReader.DeliveryReportData(
            [new("Email", 100, 0, 0)],
            [new("IncidentCreated", 40), new("ApprovalPending", 60)],
            []);
        var handler = CreateDeliveryHandler(data);

        var result = await handler.Handle(new GetNotificationDeliveryReport.Query(TenantId), CancellationToken.None);

        result.Value!.EventTypeDistribution.Should().HaveCount(2);
        result.Value.EventTypeDistribution[0].EventType.Should().Be("ApprovalPending");
        result.Value.EventTypeDistribution[0].PctOfTotal.Should().Be(60m);
    }

    [Fact]
    public async Task AK2_DeliveryReport_TopRecipients_TruncatedAt10()
    {
        var recipients = Enumerable.Range(1, 15)
            .Select(i => new INotificationDeliveryReportReader.RecipientCountData($"user-{i}", "User", 10 + i))
            .ToList();
        var data = new INotificationDeliveryReportReader.DeliveryReportData(
            [new("Email", 100, 0, 0)],
            [],
            recipients);
        var handler = CreateDeliveryHandler(data);

        var result = await handler.Handle(new GetNotificationDeliveryReport.Query(TenantId), CancellationToken.None);

        result.Value!.TopRecipients.Should().HaveCount(10);
        result.Value.TopRecipients[0].NotificationCount.Should().Be(25);
    }

    [Fact]
    public async Task AK2_DeliveryReport_PeriodStart_CalculatedFromLookback()
    {
        var handler = CreateDeliveryHandler(EmptyDeliveryData());

        var result = await handler.Handle(new GetNotificationDeliveryReport.Query(TenantId, 7), CancellationToken.None);

        result.Value!.PeriodStart.Should().Be(FixedNow.AddDays(-7));
        result.Value.PeriodEnd.Should().Be(FixedNow);
    }

    [Fact]
    public void AK2_DeliveryReport_Validator_LookbackOutOfRange_Fails()
    {
        var validator = new GetNotificationDeliveryReport.Validator();
        var result = validator.Validate(new GetNotificationDeliveryReport.Query(TenantId, 0));
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void AK2_DeliveryReport_Validator_Valid_Passes()
    {
        var validator = new GetNotificationDeliveryReport.Validator();
        var result = validator.Validate(new GetNotificationDeliveryReport.Query(TenantId, 30));
        result.IsValid.Should().BeTrue();
    }

    // ═══════════════════════════════════════════════════════════════════════
    // AK.3 — GetNotificationEffectivenessReport
    // ═══════════════════════════════════════════════════════════════════════

    private static GetNotificationEffectivenessReport.Handler CreateEffectivenessHandler(
        IReadOnlyList<INotificationEffectivenessReader.EventTypeEffectivenessData> data)
    {
        var reader = Substitute.For<INotificationEffectivenessReader>();
        reader.GetEffectivenessDataAsync(
                Arg.Any<string>(), Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(),
                Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(data);
        return new GetNotificationEffectivenessReport.Handler(reader, CreateClock());
    }

    [Fact]
    public async Task AK3_EffectivenessReport_EmptyData_Returns100HealthScore()
    {
        var handler = CreateEffectivenessHandler([]);

        var result = await handler.Handle(new GetNotificationEffectivenessReport.Query(TenantId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.TenantNotificationHealthScore.Should().Be(100m);
        result.Value.AlertFatigueCandidates.Should().BeEmpty();
    }

    [Fact]
    public async Task AK3_EffectivenessReport_ActionRate60Plus_HighImpactTier()
    {
        var data = new[]
        {
            new INotificationEffectivenessReader.EventTypeEffectivenessData("IncidentCreated", "Email", 100, 65, 10, 5m)
        };
        var handler = CreateEffectivenessHandler(data);

        var result = await handler.Handle(new GetNotificationEffectivenessReport.Query(TenantId), CancellationToken.None);

        result.Value!.ByEventType[0].Tier.Should().Be(GetNotificationEffectivenessReport.EffectivenessTier.HighImpact);
        result.Value.ByEventType[0].ActionRatePct.Should().Be(65m);
    }

    [Fact]
    public async Task AK3_EffectivenessReport_ActionRate30Plus_ModerateTier()
    {
        var data = new[]
        {
            new INotificationEffectivenessReader.EventTypeEffectivenessData("ApprovalPending", "Email", 100, 40, 30, 8m)
        };
        var handler = CreateEffectivenessHandler(data);

        var result = await handler.Handle(new GetNotificationEffectivenessReport.Query(TenantId), CancellationToken.None);

        result.Value!.ByEventType[0].Tier.Should().Be(GetNotificationEffectivenessReport.EffectivenessTier.Moderate);
    }

    [Fact]
    public async Task AK3_EffectivenessReport_ActionRate10Plus_LowImpactTier()
    {
        var data = new[]
        {
            new INotificationEffectivenessReader.EventTypeEffectivenessData("SystemAlert", "Teams", 100, 15, 60, 20m)
        };
        var handler = CreateEffectivenessHandler(data);

        var result = await handler.Handle(new GetNotificationEffectivenessReport.Query(TenantId), CancellationToken.None);

        result.Value!.ByEventType[0].Tier.Should().Be(GetNotificationEffectivenessReport.EffectivenessTier.LowImpact);
    }

    [Fact]
    public async Task AK3_EffectivenessReport_ActionRateBelow10_NoiseTier()
    {
        var data = new[]
        {
            new INotificationEffectivenessReader.EventTypeEffectivenessData("DiagnosticPing", "Email", 100, 5, 90, 0m)
        };
        var handler = CreateEffectivenessHandler(data);

        var result = await handler.Handle(new GetNotificationEffectivenessReport.Query(TenantId), CancellationToken.None);

        result.Value!.ByEventType[0].Tier.Should().Be(GetNotificationEffectivenessReport.EffectivenessTier.Noise);
    }

    [Fact]
    public async Task AK3_EffectivenessReport_NoiseAboveVolumeThreshold_IsAlertFatigueCandidate()
    {
        var data = new[]
        {
            new INotificationEffectivenessReader.EventTypeEffectivenessData("DiagnosticPing", "Email", 50, 2, 45, 0m)
        };
        var handler = CreateEffectivenessHandler(data);

        var result = await handler.Handle(
            new GetNotificationEffectivenessReport.Query(TenantId, NoiseVolumeThreshold: 20),
            CancellationToken.None);

        result.Value!.AlertFatigueCandidates.Should().HaveCount(1);
        result.Value.AlertFatigueCandidates[0].EventType.Should().Be("DiagnosticPing");
    }

    [Fact]
    public async Task AK3_EffectivenessReport_NoiseBelowVolumeThreshold_NotAlertFatigueCandidate()
    {
        var data = new[]
        {
            new INotificationEffectivenessReader.EventTypeEffectivenessData("DiagnosticPing", "Email", 10, 0, 9, 0m)
        };
        var handler = CreateEffectivenessHandler(data);

        var result = await handler.Handle(
            new GetNotificationEffectivenessReport.Query(TenantId, NoiseVolumeThreshold: 20),
            CancellationToken.None);

        result.Value!.AlertFatigueCandidates.Should().BeEmpty();
    }

    [Fact]
    public async Task AK3_EffectivenessReport_AllModerateOrBetter_HealthScore100()
    {
        var data = new[]
        {
            new INotificationEffectivenessReader.EventTypeEffectivenessData("E1", "Email", 100, 70, 10, 5m),
            new INotificationEffectivenessReader.EventTypeEffectivenessData("E2", "Teams", 100, 40, 20, 8m)
        };
        var handler = CreateEffectivenessHandler(data);

        var result = await handler.Handle(new GetNotificationEffectivenessReport.Query(TenantId), CancellationToken.None);

        result.Value!.TenantNotificationHealthScore.Should().Be(100m);
    }

    [Fact]
    public async Task AK3_EffectivenessReport_AllNoise_HealthScore0()
    {
        var data = new[]
        {
            new INotificationEffectivenessReader.EventTypeEffectivenessData("E1", "Email", 100, 5, 80, 0m),
            new INotificationEffectivenessReader.EventTypeEffectivenessData("E2", "Teams", 100, 3, 90, 0m)
        };
        var handler = CreateEffectivenessHandler(data);

        var result = await handler.Handle(new GetNotificationEffectivenessReport.Query(TenantId), CancellationToken.None);

        result.Value!.TenantNotificationHealthScore.Should().Be(0m);
    }

    [Fact]
    public async Task AK3_EffectivenessReport_RecommendedAdjustments_ContainsFatigueCandidates()
    {
        var data = new[]
        {
            new INotificationEffectivenessReader.EventTypeEffectivenessData("DiagnosticPing", "Email", 50, 2, 45, 0m)
        };
        var handler = CreateEffectivenessHandler(data);

        var result = await handler.Handle(
            new GetNotificationEffectivenessReport.Query(TenantId, NoiseVolumeThreshold: 20),
            CancellationToken.None);

        result.Value!.RecommendedAdjustments.Should().HaveCount(1);
        result.Value.RecommendedAdjustments[0].Should().Contain("DiagnosticPing");
    }

    [Fact]
    public async Task AK3_EffectivenessReport_TopEffectiveChannels_LimitedTo5()
    {
        var data = Enumerable.Range(1, 7).Select(i =>
            new INotificationEffectivenessReader.EventTypeEffectivenessData(
                $"Event{i}", $"Channel{i}", 100, 70, 10, 5m))
            .ToArray();
        var handler = CreateEffectivenessHandler(data);

        var result = await handler.Handle(new GetNotificationEffectivenessReport.Query(TenantId), CancellationToken.None);

        result.Value!.TopEffectiveChannels.Count.Should().BeLessThanOrEqualTo(5);
    }

    [Fact]
    public void AK3_EffectivenessReport_Validator_LookbackOutOfRange_Fails()
    {
        var validator = new GetNotificationEffectivenessReport.Validator();
        var result = validator.Validate(new GetNotificationEffectivenessReport.Query(TenantId, LookbackDays: 91));
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void AK3_EffectivenessReport_Validator_Valid_Passes()
    {
        var validator = new GetNotificationEffectivenessReport.Validator();
        var result = validator.Validate(new GetNotificationEffectivenessReport.Query(TenantId, 30, 4, 20));
        result.IsValid.Should().BeTrue();
    }
}
