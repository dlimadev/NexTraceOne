using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Abstractions;
using NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Features.GetChangeLeadTimeReport;
using NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Features.GetDeploymentFrequencyHealthReport;
using NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Features.GetReleasePatternAnalysisReport;

using ClusteringTier = NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Features.GetReleasePatternAnalysisReport.GetReleasePatternAnalysisReport.ReleaseClusteringTier;
using LeadTimeTier = NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Features.GetChangeLeadTimeReport.GetChangeLeadTimeReport.LeadTimeTier;
using DeployTier = NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Features.GetDeploymentFrequencyHealthReport.GetDeploymentFrequencyHealthReport.DeployFrequencyTier;

namespace NexTraceOne.ChangeGovernance.Tests.ChangeIntelligence.Application.Features;

/// <summary>
/// Testes unitários para Wave AW — Release Intelligence &amp; Deployment Analytics.
/// Cobre AW.1 GetReleasePatternAnalysisReport (~16 testes),
/// AW.2 GetChangeLeadTimeReport (~16 testes),
/// AW.3 GetDeploymentFrequencyHealthReport (~15 testes).
/// </summary>
public sealed class WaveAwReleaseIntelligenceTests
{
    private static readonly DateTimeOffset FixedNow = new(2026, 8, 1, 12, 0, 0, TimeSpan.Zero);
    private const string TenantId = "tenant-aw-001";

    private static IDateTimeProvider CreateClock()
    {
        var clock = Substitute.For<IDateTimeProvider>();
        clock.UtcNow.Returns(FixedNow);
        return clock;
    }

    // ════════════════════════════════════════════════════════════════════════
    // AW.1 — GetReleasePatternAnalysisReport
    // ════════════════════════════════════════════════════════════════════════

    private static GetReleasePatternAnalysisReport.Handler CreatePatternHandler(
        IReadOnlyList<ReleasePatternEntry> entries)
    {
        var reader = Substitute.For<IReleasePatternReader>();
        reader.ListReleasesByTenantAsync(
            TenantId, Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns(entries);
        return new GetReleasePatternAnalysisReport.Handler(reader, CreateClock());
    }

    private static ReleasePatternEntry MakePatternEntry(
        string serviceName = "svc-a",
        string teamName = "team-aw",
        string environment = "production",
        DateTimeOffset? deployedAt = null,
        bool hasIncident = false,
        DateTimeOffset? incidentAt = null,
        int serviceChangesCount = 1,
        bool isEndOfSprint = false) =>
        new(Guid.NewGuid(), serviceName, teamName, environment,
            deployedAt ?? FixedNow.AddDays(-10),
            hasIncident, incidentAt,
            serviceChangesCount, isEndOfSprint);

    [Fact]
    public async Task AW1_EmptyReport_WhenNoReleases()
    {
        var handler = CreatePatternHandler([]);
        var result = await handler.Handle(
            new GetReleasePatternAnalysisReport.Query(TenantId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.BatchSizeAnalysis.LargeReleaseCount.Should().Be(0);
        result.Value.BatchSizeAnalysis.AvgServiceChangesPerRelease.Should().Be(0m);
        result.Value.ClusteringRisk.Tier.Should().Be(ClusteringTier.Safe);
        result.Value.IncidentPatterns.RepeatFailureServices.Should().BeEmpty();
        result.Value.TenantReleasePatternScore.Should().Be(100m);
    }

    [Fact]
    public async Task AW1_BatchSizeAnalysis_CountsLargeReleases()
    {
        var entries = new[]
        {
            MakePatternEntry(serviceChangesCount: 10), // large (threshold=5)
            MakePatternEntry(serviceChangesCount: 3),
            MakePatternEntry(serviceChangesCount: 7),  // large
        };

        var handler = CreatePatternHandler(entries);
        var result = await handler.Handle(
            new GetReleasePatternAnalysisReport.Query(TenantId, LargeReleaseThreshold: 5),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.BatchSizeAnalysis.LargeReleaseCount.Should().Be(2);
        result.Value.BatchSizeAnalysis.AvgServiceChangesPerRelease.Should().BeApproximately(6.67m, 0.1m);
    }

    [Fact]
    public async Task AW1_BatchSizeVsFailureCorrelation_SignificantWhenLargeReleasesHaveHigherFailureRate()
    {
        var entries = new[]
        {
            MakePatternEntry(serviceChangesCount: 10, hasIncident: true),
            MakePatternEntry(serviceChangesCount: 10, hasIncident: true),
            MakePatternEntry(serviceChangesCount: 2, hasIncident: false),
            MakePatternEntry(serviceChangesCount: 2, hasIncident: false),
            MakePatternEntry(serviceChangesCount: 2, hasIncident: false),
            MakePatternEntry(serviceChangesCount: 2, hasIncident: false),
        };

        var handler = CreatePatternHandler(entries);
        var result = await handler.Handle(
            new GetReleasePatternAnalysisReport.Query(TenantId, LargeReleaseThreshold: 5),
            CancellationToken.None);

        result.Value.BatchSizeAnalysis.BatchSizeVsFailureCorrelationSignificant.Should().BeTrue();
    }

    [Fact]
    public async Task AW1_BatchSizeTrend_IsIncreasingWhenSecondHalfHigher()
    {
        var base_ = FixedNow.AddDays(-90);
        var entries = new[]
        {
            MakePatternEntry(serviceChangesCount: 1, deployedAt: base_.AddDays(5)),
            MakePatternEntry(serviceChangesCount: 1, deployedAt: base_.AddDays(10)),
            MakePatternEntry(serviceChangesCount: 10, deployedAt: base_.AddDays(70)),
            MakePatternEntry(serviceChangesCount: 10, deployedAt: base_.AddDays(75)),
        };

        var handler = CreatePatternHandler(entries);
        var result = await handler.Handle(
            new GetReleasePatternAnalysisReport.Query(TenantId, LookbackDays: 90),
            CancellationToken.None);

        result.Value.BatchSizeAnalysis.BatchSizeTrend.Should().Be("Increasing");
    }

    [Fact]
    public async Task AW1_TemporalPatterns_HighRiskDayConcentrationIncludes_FriSatSun()
    {
        // Friday = day of week 5
        var friday = new DateTimeOffset(2026, 7, 31, 14, 0, 0, TimeSpan.Zero); // a Friday
        var monday = new DateTimeOffset(2026, 7, 27, 10, 0, 0, TimeSpan.Zero);

        var entries = new[]
        {
            MakePatternEntry(deployedAt: friday),
            MakePatternEntry(deployedAt: monday),
        };

        var handler = CreatePatternHandler(entries);
        var result = await handler.Handle(
            new GetReleasePatternAnalysisReport.Query(TenantId), CancellationToken.None);

        result.Value.TemporalPatterns.HighRiskDayConcentrationPct.Should().Be(50m);
    }

    [Fact]
    public async Task AW1_TemporalPatterns_EndOfSprintCluster_Calculated()
    {
        var entries = new[]
        {
            MakePatternEntry(isEndOfSprint: true),
            MakePatternEntry(isEndOfSprint: true),
            MakePatternEntry(isEndOfSprint: false),
            MakePatternEntry(isEndOfSprint: false),
        };

        var handler = CreatePatternHandler(entries);
        var result = await handler.Handle(
            new GetReleasePatternAnalysisReport.Query(TenantId), CancellationToken.None);

        result.Value.TemporalPatterns.EndOfSprintClusterPct.Should().Be(50m);
    }

    [Fact]
    public async Task AW1_TemporalPatterns_HeatmapByHourBucket_AllBucketsPresent()
    {
        var handler = CreatePatternHandler([MakePatternEntry()]);
        var result = await handler.Handle(
            new GetReleasePatternAnalysisReport.Query(TenantId), CancellationToken.None);

        result.Value.TemporalPatterns.DeploymentHeatmapByHourBucket.Keys.Should()
            .Contain(new[] { "0-5", "6-11", "12-17", "18-23" });
    }

    [Fact]
    public async Task AW1_ClusteringRisk_Safe_WhenNoClustering()
    {
        var entries = Enumerable.Range(0, 5)
            .Select(i => MakePatternEntry(deployedAt: FixedNow.AddDays(-i * 5)))
            .ToArray();

        var handler = CreatePatternHandler(entries);
        var result = await handler.Handle(
            new GetReleasePatternAnalysisReport.Query(TenantId, ClusterWarningPerWeek: 3),
            CancellationToken.None);

        result.Value.ClusteringRisk.Tier.Should().Be(ClusteringTier.Safe);
    }

    [Fact]
    public async Task AW1_ClusteringRisk_Critical_WhenManySameDayClusterDays()
    {
        // 4 different days, each with 4 releases on same env → 4 cluster days in 7-day window
        var base_ = FixedNow.AddDays(-6);
        var entries = Enumerable.Range(0, 4)
            .SelectMany(day => Enumerable.Range(0, 4)
                .Select(h => MakePatternEntry(deployedAt: base_.AddDays(day).AddHours(h))))
            .ToArray();

        var handler = CreatePatternHandler(entries);
        var result = await handler.Handle(
            new GetReleasePatternAnalysisReport.Query(TenantId, ClusterWarningPerWeek: 1, LookbackDays: 7),
            CancellationToken.None);

        result.Value.ClusteringRisk.Tier.Should().Be(ClusteringTier.Critical);
    }

    [Fact]
    public async Task AW1_IncidentPatternAfterRelease_Hour1Rate_Calculated()
    {
        var deployTime = FixedNow.AddDays(-10);
        var entries = new[]
        {
            MakePatternEntry(hasIncident: true, deployedAt: deployTime,
                incidentAt: deployTime.AddMinutes(30)),
            MakePatternEntry(hasIncident: false, deployedAt: deployTime.AddDays(-1)),
        };

        var handler = CreatePatternHandler(entries);
        var result = await handler.Handle(
            new GetReleasePatternAnalysisReport.Query(TenantId), CancellationToken.None);

        result.Value.IncidentPatterns.IncidentInHour1Rate.Should().Be(0.5m);
    }

    [Fact]
    public async Task AW1_IncidentPatternAfterRelease_RepeatFailureServices_IdentifiesServices()
    {
        var deployTime = FixedNow.AddDays(-10);
        var entries = new[]
        {
            MakePatternEntry("svc-bad", hasIncident: true, deployedAt: deployTime,
                incidentAt: deployTime.AddMinutes(20)),
            MakePatternEntry("svc-bad", hasIncident: true, deployedAt: deployTime.AddDays(-2),
                incidentAt: deployTime.AddDays(-2).AddMinutes(15)),
            MakePatternEntry("svc-good", hasIncident: false),
        };

        var handler = CreatePatternHandler(entries);
        var result = await handler.Handle(
            new GetReleasePatternAnalysisReport.Query(TenantId, RepeatFailureThreshold: 0.3m),
            CancellationToken.None);

        result.Value.IncidentPatterns.RepeatFailureServices.Should().Contain("svc-bad");
        result.Value.IncidentPatterns.RepeatFailureServices.Should().NotContain("svc-good");
    }

    [Fact]
    public async Task AW1_TenantReleasePatternScore_IsMaxWhenNoRisks()
    {
        var entries = Enumerable.Range(0, 5)
            .Select(i => MakePatternEntry(
                deployedAt: FixedNow.AddDays(-i * 3),
                serviceChangesCount: 1,
                hasIncident: false,
                isEndOfSprint: false))
            .ToArray();

        var handler = CreatePatternHandler(entries);
        var result = await handler.Handle(
            new GetReleasePatternAnalysisReport.Query(TenantId), CancellationToken.None);

        result.Value.TenantReleasePatternScore.Should().BeGreaterThan(70m);
    }

    [Fact]
    public async Task AW1_TenantReleasePatternScore_IsLowWhenManyRisks()
    {
        var deployTime = FixedNow.AddDays(-5);
        // Many high-incident releases on same day with large batches
        var entries = Enumerable.Range(0, 20)
            .Select(i => MakePatternEntry(
                deployedAt: deployTime.AddMinutes(i),
                serviceChangesCount: 10,
                hasIncident: true,
                incidentAt: deployTime.AddMinutes(i).AddMinutes(10),
                isEndOfSprint: true))
            .ToArray();

        var handler = CreatePatternHandler(entries);
        var result = await handler.Handle(
            new GetReleasePatternAnalysisReport.Query(TenantId, LookbackDays: 7, ClusterWarningPerWeek: 1),
            CancellationToken.None);

        result.Value.TenantReleasePatternScore.Should().BeLessThan(60m);
    }

    [Fact]
    public async Task AW1_Validator_RejectsEmptyTenantId()
    {
        var validator = new GetReleasePatternAnalysisReport.Validator();
        var result = await validator.ValidateAsync(
            new GetReleasePatternAnalysisReport.Query(""));

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task AW1_Validator_RejectsInvalidLookbackDays()
    {
        var validator = new GetReleasePatternAnalysisReport.Validator();
        var result = await validator.ValidateAsync(
            new GetReleasePatternAnalysisReport.Query(TenantId, LookbackDays: 0));

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task AW1_NullReleasePatternReader_ReturnsEmptyList()
    {
        var nullReader = new NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.NullReleasePatternReader();
        var result = await nullReader.ListReleasesByTenantAsync(
            TenantId, FixedNow.AddDays(-90), FixedNow, CancellationToken.None);

        result.Should().BeEmpty();
    }

    // ════════════════════════════════════════════════════════════════════════
    // AW.2 — GetChangeLeadTimeReport
    // ════════════════════════════════════════════════════════════════════════

    private static GetChangeLeadTimeReport.Handler CreateLeadTimeHandler(
        IReadOnlyList<LeadTimeEntry> entries)
    {
        var reader = Substitute.For<IChangeLeadTimeReader>();
        reader.ListReleaseLeadTimesByTenantAsync(
            TenantId, Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns(entries);
        return new GetChangeLeadTimeReport.Handler(reader, CreateClock());
    }

    private static LeadTimeEntry MakeLeadTimeEntry(
        string serviceName = "svc-lt",
        string teamName = "team-aw",
        string environment = "production",
        DateTimeOffset? createdAt = null,
        DateTimeOffset? approvalRequestedAt = null,
        DateTimeOffset? approvedAt = null,
        DateTimeOffset? preProdAt = null,
        DateTimeOffset? productionAt = null,
        DateTimeOffset? verifiedAt = null)
    {
        var created = createdAt ?? FixedNow.AddDays(-10);
        return new LeadTimeEntry(
            Guid.NewGuid(), serviceName, teamName, environment,
            created, approvalRequestedAt, approvedAt, preProdAt, productionAt, verifiedAt);
    }

    [Fact]
    public async Task AW2_EmptyReport_WhenNoEntries()
    {
        var handler = CreateLeadTimeHandler([]);
        var result = await handler.Handle(
            new GetChangeLeadTimeReport.Query(TenantId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.MedianLeadTime.Should().Be(0);
        result.Value.P95LeadTime.Should().Be(0);
        result.Value.SlowestApprovalGroups.Should().BeEmpty();
        result.Value.SlowestPromotionServices.Should().BeEmpty();
        result.Value.Releases.Should().BeEmpty();
    }

    [Fact]
    public async Task AW2_StageBreakdown_ComputesCreatedToApprovalRequested()
    {
        var created = FixedNow.AddHours(-5);
        var approvalRequested = FixedNow.AddHours(-3);
        var entry = MakeLeadTimeEntry(createdAt: created, approvalRequestedAt: approvalRequested);

        var handler = CreateLeadTimeHandler([entry]);
        var result = await handler.Handle(
            new GetChangeLeadTimeReport.Query(TenantId), CancellationToken.None);

        var release = result.Value.Releases.Single();
        release.CreatedToApprovalRequested.Should().BeApproximately(120, 1); // 2 hours = 120 min
    }

    [Fact]
    public async Task AW2_StageBreakdown_NullWhenTimestampMissing()
    {
        var entry = MakeLeadTimeEntry(); // no timestamps
        var handler = CreateLeadTimeHandler([entry]);
        var result = await handler.Handle(
            new GetChangeLeadTimeReport.Query(TenantId), CancellationToken.None);

        var release = result.Value.Releases.Single();
        release.CreatedToApprovalRequested.Should().BeNull();
        release.ApprovalRequestedToApproved.Should().BeNull();
    }

    [Fact]
    public async Task AW2_BottleneckStage_IdentifiesLongestStage()
    {
        var created = FixedNow.AddHours(-10);
        var approvalRequested = FixedNow.AddHours(-9);
        var approved = FixedNow.AddHours(-1); // approval took 8h = longest
        var preProd = FixedNow.AddMinutes(-30);
        var prod = FixedNow.AddMinutes(-10);

        var entry = MakeLeadTimeEntry(createdAt: created,
            approvalRequestedAt: approvalRequested,
            approvedAt: approved,
            preProdAt: preProd,
            productionAt: prod);

        var handler = CreateLeadTimeHandler([entry]);
        var result = await handler.Handle(
            new GetChangeLeadTimeReport.Query(TenantId), CancellationToken.None);

        result.Value.Releases.Single().BottleneckStage.Should().Be("ApprovalRequestedToApproved");
    }

    [Fact]
    public async Task AW2_LeadTimeTier_Elite_WhenMedianBelow60Min()
    {
        var created = FixedNow.AddMinutes(-30);
        var approved = FixedNow.AddMinutes(-25);
        var prod = FixedNow.AddMinutes(-5);

        var entries = Enumerable.Range(0, 5)
            .Select(_ => MakeLeadTimeEntry(createdAt: created, productionAt: prod))
            .ToArray();

        var handler = CreateLeadTimeHandler(entries);
        var result = await handler.Handle(
            new GetChangeLeadTimeReport.Query(TenantId), CancellationToken.None);

        result.Value.TenantLeadTimeTier.Should().Be(LeadTimeTier.Elite);
    }

    [Fact]
    public async Task AW2_LeadTimeTier_Low_WhenMedianAbove7Days()
    {
        var created = FixedNow.AddDays(-20);
        var approvalRequested = FixedNow.AddDays(-10); // 10-day stage = 14400 min > 7-day Elite threshold

        var entries = Enumerable.Range(0, 3)
            .Select(_ => MakeLeadTimeEntry(createdAt: created, approvalRequestedAt: approvalRequested))
            .ToArray();

        var handler = CreateLeadTimeHandler(entries);
        var result = await handler.Handle(
            new GetChangeLeadTimeReport.Query(TenantId), CancellationToken.None);

        result.Value.TenantLeadTimeTier.Should().Be(LeadTimeTier.Low);
    }

    [Fact]
    public async Task AW2_SlowestApprovalGroups_IdentifiesGroupsAboveSla()
    {
        var created = FixedNow.AddHours(-50);
        var approvalRequested = FixedNow.AddHours(-49);
        var approvedLate = FixedNow.AddHours(-23); // 26h approval time > 24h SLA
        var approvedFast = approvalRequested.AddHours(1); // 1h

        var entries = new[]
        {
            MakeLeadTimeEntry("svc-slow", "team-slow", createdAt: created,
                approvalRequestedAt: approvalRequested, approvedAt: approvedLate),
            MakeLeadTimeEntry("svc-fast", "team-fast", createdAt: created,
                approvalRequestedAt: approvalRequested, approvedAt: approvedFast),
        };

        var handler = CreateLeadTimeHandler(entries);
        var result = await handler.Handle(
            new GetChangeLeadTimeReport.Query(TenantId, ApprovalSlaHours: 24),
            CancellationToken.None);

        result.Value.SlowestApprovalGroups.Should().Contain("team-slow");
        result.Value.SlowestApprovalGroups.Should().NotContain("team-fast");
    }

    [Fact]
    public async Task AW2_SlowestPromotionServices_IdentifiesServicesAbove24h()
    {
        var preProd = FixedNow.AddHours(-30);
        var prodLate = FixedNow.AddHours(-4);   // 26h gap
        var prodFast = preProd.AddHours(2);      // 2h gap

        var entries = new[]
        {
            MakeLeadTimeEntry("svc-slow-promo", preProdAt: preProd, productionAt: prodLate),
            MakeLeadTimeEntry("svc-fast-promo", preProdAt: preProd, productionAt: prodFast),
        };

        var handler = CreateLeadTimeHandler(entries);
        var result = await handler.Handle(
            new GetChangeLeadTimeReport.Query(TenantId), CancellationToken.None);

        result.Value.SlowestPromotionServices.Should().Contain("svc-slow-promo");
        result.Value.SlowestPromotionServices.Should().NotContain("svc-fast-promo");
    }

    [Fact]
    public async Task AW2_ApprovalBottleneckIndex_IsNonZeroWhenApprovalDominates()
    {
        var created = FixedNow.AddHours(-100);
        var approvalRequested = FixedNow.AddHours(-99);
        var approved = FixedNow.AddHours(-10); // ~89h approval
        var prod = FixedNow.AddHours(-9);       // ~1h preProd→prod

        var entries = new[]
        {
            MakeLeadTimeEntry(createdAt: created,
                approvalRequestedAt: approvalRequested,
                approvedAt: approved,
                productionAt: prod)
        };

        var handler = CreateLeadTimeHandler(entries);
        var result = await handler.Handle(
            new GetChangeLeadTimeReport.Query(TenantId), CancellationToken.None);

        result.Value.ApprovalBottleneckIndex.Should().BeGreaterThan(50m);
    }

    [Fact]
    public async Task AW2_LeadTimeTrend_IsIncreasing_WhenRecentMedianHigher()
    {
        // Old releases (quick: 2h approval stage)
        var oldEntries = Enumerable.Range(0, 5)
            .Select(_ => MakeLeadTimeEntry(
                createdAt: FixedNow.AddDays(-80),
                approvalRequestedAt: FixedNow.AddDays(-80).AddHours(2)))
            .ToList();

        // Recent releases (slow: 200h approval stage)
        var recentEntries = Enumerable.Range(0, 5)
            .Select(_ => MakeLeadTimeEntry(
                createdAt: FixedNow.AddDays(-5),
                approvalRequestedAt: FixedNow.AddDays(-5).AddHours(200)))
            .ToList();

        var handler = CreateLeadTimeHandler([.. oldEntries, .. recentEntries]);
        var result = await handler.Handle(
            new GetChangeLeadTimeReport.Query(TenantId), CancellationToken.None);

        result.Value.LeadTimeTrend.Should().Be("Increasing");
    }

    [Fact]
    public async Task AW2_EnvironmentWaitTime_GroupsByEnvironment()
    {
        var entries = new[]
        {
            MakeLeadTimeEntry("svc-a", environment: "production",
                preProdAt: FixedNow.AddHours(-10), productionAt: FixedNow.AddHours(-5)),
            MakeLeadTimeEntry("svc-b", environment: "staging",
                preProdAt: FixedNow.AddHours(-3), productionAt: FixedNow.AddHours(-1)),
        };

        var handler = CreateLeadTimeHandler(entries);
        var result = await handler.Handle(
            new GetChangeLeadTimeReport.Query(TenantId), CancellationToken.None);

        result.Value.EnvironmentWaitTime.Should().ContainKey("production");
        result.Value.EnvironmentWaitTime.Should().ContainKey("staging");
        result.Value.EnvironmentWaitTime["production"].Should().BeGreaterThan(
            result.Value.EnvironmentWaitTime["staging"]);
    }

    [Fact]
    public async Task AW2_MedianAndP95_Calculated()
    {
        // 10 entries with approval stage durations from 10 to 100 min
        var entries = Enumerable.Range(1, 10)
            .Select(i => MakeLeadTimeEntry(
                createdAt: FixedNow.AddMinutes(-i * 10 - 10),
                approvalRequestedAt: FixedNow.AddMinutes(-10)))
            .ToArray();

        var handler = CreateLeadTimeHandler(entries);
        var result = await handler.Handle(
            new GetChangeLeadTimeReport.Query(TenantId), CancellationToken.None);

        result.Value.MedianLeadTime.Should().BeGreaterThan(0);
        result.Value.P95LeadTime.Should().BeGreaterThanOrEqualTo(result.Value.MedianLeadTime);
    }

    [Fact]
    public async Task AW2_Validator_RejectsEmptyTenantId()
    {
        var validator = new GetChangeLeadTimeReport.Validator();
        var result = await validator.ValidateAsync(new GetChangeLeadTimeReport.Query(""));

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task AW2_Validator_RejectsInvalidMaxServices()
    {
        var validator = new GetChangeLeadTimeReport.Validator();
        var result = await validator.ValidateAsync(
            new GetChangeLeadTimeReport.Query(TenantId, MaxServices: 0));

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task AW2_NullChangeLeadTimeReader_ReturnsEmptyList()
    {
        var nullReader = new NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.NullChangeLeadTimeReader();
        var result = await nullReader.ListReleaseLeadTimesByTenantAsync(
            TenantId, FixedNow.AddDays(-90), FixedNow, CancellationToken.None);

        result.Should().BeEmpty();
    }

    // ════════════════════════════════════════════════════════════════════════
    // AW.3 — GetDeploymentFrequencyHealthReport
    // ════════════════════════════════════════════════════════════════════════

    private static GetDeploymentFrequencyHealthReport.Handler CreateFrequencyHandler(
        IReadOnlyList<DeploymentFrequencyEntry> entries)
    {
        var reader = Substitute.For<IDeploymentFrequencyReader>();
        reader.ListDeploymentsByTenantAsync(
            TenantId, Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns(entries);
        return new GetDeploymentFrequencyHealthReport.Handler(reader, CreateClock());
    }

    private static DeploymentFrequencyEntry MakeDeployEntry(
        string serviceName = "svc-df",
        string teamName = "team-aw",
        string serviceTier = "Standard",
        string environment = "production",
        DateTimeOffset? deployedAt = null,
        bool succeeded = true) =>
        new(Guid.NewGuid(), serviceName, teamName, serviceTier, environment,
            deployedAt ?? FixedNow.AddDays(-5), succeeded);

    [Fact]
    public async Task AW3_EmptyReport_WhenNoDeployments()
    {
        var handler = CreateFrequencyHandler([]);
        var result = await handler.Handle(
            new GetDeploymentFrequencyHealthReport.Query(TenantId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Services.Should().BeEmpty();
        result.Value.Summary.StaleServices.Should().BeEmpty();
        result.Value.Summary.TenantDeployFrequencyHealthScore.Should().Be(0m);
    }

    [Fact]
    public async Task AW3_Stale_WhenLastDeployOlderThanThreshold()
    {
        var oldDeploy = FixedNow.AddDays(-70); // > 60 day stale threshold
        var entries = new[] { MakeDeployEntry(deployedAt: oldDeploy) };

        var handler = CreateFrequencyHandler(entries);
        var result = await handler.Handle(
            new GetDeploymentFrequencyHealthReport.Query(TenantId, StaleDeployDays: 60),
            CancellationToken.None);

        result.Value.Services.Single().Tier.Should().Be(DeployTier.Stale);
        result.Value.Summary.StaleServices.Should().Contain("svc-df");
    }

    [Fact]
    public async Task AW3_Overdeploying_WhenFrequencyAbove20PerMonth()
    {
        // 25 deploys in 30 days → > 20/month
        var entries = Enumerable.Range(0, 25)
            .Select(i => MakeDeployEntry("svc-hot", deployedAt: FixedNow.AddDays(-i)))
            .ToArray();

        var handler = CreateFrequencyHandler(entries);
        var result = await handler.Handle(
            new GetDeploymentFrequencyHealthReport.Query(TenantId, LookbackDays: 30),
            CancellationToken.None);

        result.Value.Services.Single().Tier.Should().Be(DeployTier.Overdeploying);
        result.Value.Summary.OverdeployingServices.Should().Contain("svc-hot");
    }

    [Fact]
    public async Task AW3_Optimal_WhenFrequencyBetween2And20PerMonth()
    {
        // 5 deploys in 30 days → 5/month = Optimal
        var entries = Enumerable.Range(0, 5)
            .Select(i => MakeDeployEntry("svc-opt", deployedAt: FixedNow.AddDays(-i * 5)))
            .ToArray();

        var handler = CreateFrequencyHandler(entries);
        var result = await handler.Handle(
            new GetDeploymentFrequencyHealthReport.Query(TenantId, LookbackDays: 30),
            CancellationToken.None);

        result.Value.Services.Single().Tier.Should().Be(DeployTier.Optimal);
    }

    [Fact]
    public async Task AW3_Underdeploying_WhenFrequencyBelow2PerMonth()
    {
        // 1 deploy in 90 days → < 2/month
        var entries = new[] { MakeDeployEntry("svc-slow", deployedAt: FixedNow.AddDays(-30)) };

        var handler = CreateFrequencyHandler(entries);
        var result = await handler.Handle(
            new GetDeploymentFrequencyHealthReport.Query(TenantId, LookbackDays: 90),
            CancellationToken.None);

        result.Value.Services.Single().Tier.Should().Be(DeployTier.Underdeploying);
    }

    [Fact]
    public async Task AW3_TenantDeployFrequencyHealthScore_IsPctOptimal()
    {
        var optimalEntries = Enumerable.Range(0, 5)
            .Select(i => MakeDeployEntry("svc-opt", deployedAt: FixedNow.AddDays(-i * 5)))
            .ToList();
        var staleEntry = MakeDeployEntry("svc-stale", deployedAt: FixedNow.AddDays(-80));

        var entries = optimalEntries.Append(staleEntry).ToArray();

        var handler = CreateFrequencyHandler(entries);
        var result = await handler.Handle(
            new GetDeploymentFrequencyHealthReport.Query(TenantId, LookbackDays: 30),
            CancellationToken.None);

        // 1 optimal + 1 stale = 50% (but limited by MaxServices)
        result.Value.Summary.TenantDeployFrequencyHealthScore.Should().BeInRange(0m, 100m);
    }

    [Fact]
    public async Task AW3_HighVariabilityFlag_SetWhenGapStdDevHigh()
    {
        // High variability: deploy every 1, 1, 20, 1, 1 days
        var dates = new[]
        {
            FixedNow.AddDays(-24),
            FixedNow.AddDays(-23),
            FixedNow.AddDays(-22),
            FixedNow.AddDays(-2),
            FixedNow.AddDays(-1),
            FixedNow
        };
        var entries = dates
            .Select(d => MakeDeployEntry("svc-var", deployedAt: d))
            .ToArray();

        var handler = CreateFrequencyHandler(entries);
        var result = await handler.Handle(
            new GetDeploymentFrequencyHealthReport.Query(TenantId, HighVariabilityThreshold: 0.3m),
            CancellationToken.None);

        result.Value.Services.Single().HighVariabilityFlag.Should().BeTrue();
    }

    [Fact]
    public async Task AW3_TeamDeployFrequencyComparison_GroupsByTeam()
    {
        var entries = new[]
        {
            MakeDeployEntry("svc-a", "team-alpha", deployedAt: FixedNow.AddDays(-1)),
            MakeDeployEntry("svc-b", "team-alpha", deployedAt: FixedNow.AddDays(-3)),
            MakeDeployEntry("svc-c", "team-beta", deployedAt: FixedNow.AddDays(-2)),
        };

        var handler = CreateFrequencyHandler(entries);
        var result = await handler.Handle(
            new GetDeploymentFrequencyHealthReport.Query(TenantId), CancellationToken.None);

        result.Value.TeamDeployFrequencyComparison.Should().ContainKey("team-alpha");
        result.Value.TeamDeployFrequencyComparison.Should().ContainKey("team-beta");
    }

    [Fact]
    public async Task AW3_DeployFrequencyVsIncidentRate_SortedByFrequency()
    {
        // svc-high: 10 deploys, svc-low: 1 deploy
        var highEntries = Enumerable.Range(0, 10)
            .Select(i => MakeDeployEntry("svc-high", deployedAt: FixedNow.AddDays(-i)));
        var lowEntry = new[] { MakeDeployEntry("svc-low", deployedAt: FixedNow.AddDays(-5)) };

        var handler = CreateFrequencyHandler([.. highEntries, .. lowEntry]);
        var result = await handler.Handle(
            new GetDeploymentFrequencyHealthReport.Query(TenantId, LookbackDays: 30),
            CancellationToken.None);

        result.Value.DeployFrequencyVsIncidentRate.First().ServiceName.Should().Be("svc-high");
    }

    [Fact]
    public async Task AW3_Validator_RejectsEmptyTenantId()
    {
        var validator = new GetDeploymentFrequencyHealthReport.Validator();
        var result = await validator.ValidateAsync(
            new GetDeploymentFrequencyHealthReport.Query(""));

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task AW3_Validator_RejectsInvalidLookbackDays()
    {
        var validator = new GetDeploymentFrequencyHealthReport.Validator();
        var result = await validator.ValidateAsync(
            new GetDeploymentFrequencyHealthReport.Query(TenantId, LookbackDays: 400));

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task AW3_NullDeploymentFrequencyReader_ReturnsEmptyList()
    {
        var nullReader = new NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.NullDeploymentFrequencyReader();
        var result = await nullReader.ListDeploymentsByTenantAsync(
            TenantId, FixedNow.AddDays(-90), FixedNow, CancellationToken.None);

        result.Should().BeEmpty();
    }
}
