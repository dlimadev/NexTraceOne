using System.Linq;
using NSubstitute;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.ChangeGovernance.Application.Platform.Abstractions;
using NexTraceOne.ChangeGovernance.Application.Platform.Features.GetConfigurationDriftReport;
using NexTraceOne.ChangeGovernance.Application.Platform.Features.GetPlatformHealthIndexReport;
using NexTraceOne.ChangeGovernance.Application.Platform.Features.GetAdaptiveRecommendationReport;
using NexTraceOne.Configuration.Application.Abstractions;
using NexTraceOne.Configuration.Contracts.DTOs;

namespace NexTraceOne.ChangeGovernance.Tests.Platform.Application.Features;

/// <summary>
/// Testes unitários para Wave AU — Platform Self-Optimization &amp; Adaptive Intelligence.
/// AU.1: GetConfigurationDriftReport       (~14 testes)
/// AU.2: GetPlatformHealthIndexReport      (~16 testes)
/// AU.3: GetAdaptiveRecommendationReport   (~16 testes)
/// </summary>
public sealed class WaveAuPlatformIntelligenceTests
{
    private static readonly DateTimeOffset FixedNow = new(2026, 7, 1, 10, 0, 0, TimeSpan.Zero);
    private const string TenantId = "tenant-au-001";

    private static IDateTimeProvider CreateClock()
    {
        var clock = Substitute.For<IDateTimeProvider>();
        clock.UtcNow.Returns(FixedNow);
        return clock;
    }

    private static IConfigurationResolutionService CreateConfigService()
    {
        var svc = Substitute.For<IConfigurationResolutionService>();
        svc.ResolveEffectiveValueAsync(
                Arg.Any<string>(),
                Arg.Any<NexTraceOne.Configuration.Domain.Enums.ConfigurationScope>(),
                Arg.Any<string?>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<EffectiveConfigurationDto?>(null));
        return svc;
    }

    private static void SetupConfig(IConfigurationResolutionService svc, string key, string? value)
    {
        EffectiveConfigurationDto? result = value is null
            ? null
            : new EffectiveConfigurationDto(key, value, "System", null, false, false, key, "string", false, 1);
        svc.ResolveEffectiveValueAsync(key,
                Arg.Any<NexTraceOne.Configuration.Domain.Enums.ConfigurationScope>(),
                Arg.Any<string?>(),
                Arg.Any<CancellationToken>())
            .Returns(result);
    }

    private static IConfigurationDriftReader.ConfigKeyDriftRow MakeDriftRow(
        string key = "some.config.key",
        string module = "governance",
        bool isDivergent = true,
        GetConfigurationDriftReport.DivergenceType divergenceType = GetConfigurationDriftReport.DivergenceType.Unexplained,
        bool isHighImpact = false,
        DateTimeOffset? lastUpdatedAt = null)
        => new(
            Key: key,
            Module: module,
            ValueByEnvironment: new Dictionary<string, string?> { ["prod"] = "A", ["staging"] = "B" },
            IsDivergent: isDivergent,
            DivergenceType: divergenceType,
            IsHighImpact: isHighImpact,
            LastUpdatedAt: lastUpdatedAt);

    private static IPlatformHealthIndexReader.PlatformHealthRawData MakeRawData(
        decimal scc = 100m, decimal cc = 100m, decimal cga = 100m,
        decimal sga = 100m, decimal oc = 100m, decimal agr = 100m,
        decimal df = 100m, decimal? benchmark = null)
        => new(scc, cc, cga, sga, oc, agr, df, benchmark);

    private static IAdaptiveRecommendationReader.RecommendationSignal MakeSignal(
        GetAdaptiveRecommendationReport.RecommendationCategory category = GetAdaptiveRecommendationReport.RecommendationCategory.Reliability,
        int impactScore = 80,
        GetAdaptiveRecommendationReport.EffortEstimate effort = GetAdaptiveRecommendationReport.EffortEstimate.Low,
        string title = "Fix it",
        string description = "Fix this issue")
        => new(
            RecommendationId: Guid.NewGuid(),
            Category: category,
            Title: title,
            Description: description,
            ImpactScore: impactScore,
            EffortEstimate: effort,
            AffectedServices: ["svc-a"],
            AffectedTeams: ["team-1"],
            RecommendationSource: "automated",
            EvidenceLinks: []);

    // ═══════════════════════════════════════════════════════════════════════
    // AU.1 — GetConfigurationDriftReport
    // ═══════════════════════════════════════════════════════════════════════

    private static GetConfigurationDriftReport.Handler CreateDriftHandler(
        IReadOnlyList<IConfigurationDriftReader.ConfigKeyDriftRow> rows,
        IConfigurationResolutionService? configService = null)
    {
        var reader = Substitute.For<IConfigurationDriftReader>();
        reader.GetConfigKeyDriftAsync(Arg.Any<string>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns(rows);
        return new GetConfigurationDriftReport.Handler(reader, configService ?? CreateConfigService(), CreateClock());
    }

    [Fact]
    public async Task GetConfigurationDriftReport_NoKeys_ReturnsAlignedTier()
    {
        var handler = CreateDriftHandler([]);
        var result = await handler.Handle(new GetConfigurationDriftReport.Query(TenantId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Tier.Should().Be(GetConfigurationDriftReport.ConfigDriftTier.Aligned);
    }

    [Fact]
    public async Task GetConfigurationDriftReport_1UnexplainedKey_ReturnsMinorDrift()
    {
        var handler = CreateDriftHandler([MakeDriftRow()]);
        var result = await handler.Handle(new GetConfigurationDriftReport.Query(TenantId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Tier.Should().Be(GetConfigurationDriftReport.ConfigDriftTier.MinorDrift);
    }

    [Fact]
    public async Task GetConfigurationDriftReport_4UnexplainedKeys_ReturnsMajorDrift()
    {
        var rows = Enumerable.Range(0, 4).Select(i => MakeDriftRow(key: $"key.{i}")).ToList();
        var handler = CreateDriftHandler(rows);
        var result = await handler.Handle(new GetConfigurationDriftReport.Query(TenantId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Tier.Should().Be(GetConfigurationDriftReport.ConfigDriftTier.MajorDrift);
    }

    [Fact]
    public async Task GetConfigurationDriftReport_11UnexplainedKeys_ReturnsCritical()
    {
        var rows = Enumerable.Range(0, 11).Select(i => MakeDriftRow(key: $"key.{i}")).ToList();
        var handler = CreateDriftHandler(rows);
        var result = await handler.Handle(new GetConfigurationDriftReport.Query(TenantId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Tier.Should().Be(GetConfigurationDriftReport.ConfigDriftTier.Critical);
    }

    [Fact]
    public async Task GetConfigurationDriftReport_AllKeysWithoutDivergence_HealthScore100()
    {
        var rows = Enumerable.Range(0, 5)
            .Select(i => MakeDriftRow(key: $"key.{i}", isDivergent: false, divergenceType: GetConfigurationDriftReport.DivergenceType.Intentional))
            .ToList();
        var handler = CreateDriftHandler(rows);
        var result = await handler.Handle(new GetConfigurationDriftReport.Query(TenantId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.TenantConfigurationHealthScore.Should().Be(100m);
    }

    [Fact]
    public async Task GetConfigurationDriftReport_HalfUnexplained_HealthScore50()
    {
        var rows = new[]
        {
            MakeDriftRow(key: "key.1", divergenceType: GetConfigurationDriftReport.DivergenceType.Intentional),
            MakeDriftRow(key: "key.2", divergenceType: GetConfigurationDriftReport.DivergenceType.Unexplained),
        };
        var handler = CreateDriftHandler(rows);
        var result = await handler.Handle(new GetConfigurationDriftReport.Query(TenantId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.TenantConfigurationHealthScore.Should().Be(50m);
    }

    [Fact]
    public async Task GetConfigurationDriftReport_HighImpactModule_IncludedInHighImpactDivergences()
    {
        var row = MakeDriftRow(module: "governance", divergenceType: GetConfigurationDriftReport.DivergenceType.Unexplained);
        var handler = CreateDriftHandler([row]);
        var result = await handler.Handle(new GetConfigurationDriftReport.Query(TenantId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.HighImpactDivergences.Should().ContainSingle();
    }

    [Fact]
    public async Task GetConfigurationDriftReport_StaleKeys_IncludedInStaleConfigKeys()
    {
        var staleDate = FixedNow.AddDays(-100); // 100 days ago > default 90
        var row = MakeDriftRow(
            divergenceType: GetConfigurationDriftReport.DivergenceType.Intentional,
            lastUpdatedAt: staleDate);
        var handler = CreateDriftHandler([row]);
        var result = await handler.Handle(new GetConfigurationDriftReport.Query(TenantId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.StaleConfigKeys.Should().ContainSingle();
    }

    [Fact]
    public async Task GetConfigurationDriftReport_RolloutReadinessBlocks_ContainApprovalKeys()
    {
        var row = MakeDriftRow(key: "approval.timeout", divergenceType: GetConfigurationDriftReport.DivergenceType.Unexplained);
        var handler = CreateDriftHandler([row]);
        var result = await handler.Handle(new GetConfigurationDriftReport.Query(TenantId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.RolloutReadinessBlocks.Should().ContainSingle();
    }

    [Fact]
    public async Task GetConfigurationDriftReport_Top5Unexplained_LimitedToFive()
    {
        var rows = Enumerable.Range(0, 8).Select(i => MakeDriftRow(key: $"key.{i}")).ToList();
        var handler = CreateDriftHandler(rows);
        var result = await handler.Handle(new GetConfigurationDriftReport.Query(TenantId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.ConfigAlignmentRecommendations.Should().HaveCount(5);
    }

    [Fact]
    public async Task GetConfigurationDriftReport_EmptyTenantId_ValidationFails()
    {
        var validator = new GetConfigurationDriftReport.Validator();
        var validationResult = await validator.ValidateAsync(new GetConfigurationDriftReport.Query(""));

        validationResult.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task GetConfigurationDriftReport_LookbackDays0_ValidationFails()
    {
        var validator = new GetConfigurationDriftReport.Validator();
        var validationResult = await validator.ValidateAsync(new GetConfigurationDriftReport.Query(TenantId, LookbackDays: 0));

        validationResult.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task GetConfigurationDriftReport_LookbackDays200_ValidationFails()
    {
        var validator = new GetConfigurationDriftReport.Validator();
        var validationResult = await validator.ValidateAsync(new GetConfigurationDriftReport.Query(TenantId, LookbackDays: 200));

        validationResult.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task GetConfigurationDriftReport_ConfigStaleDaysFromConfig_UsedCorrectly()
    {
        // stale_days config = 10, key updated 20 days ago → should be stale
        var configSvc = CreateConfigService();
        SetupConfig(configSvc, GetConfigurationDriftReport.StaleDaysKey, "10");

        var lastUpdated = FixedNow.AddDays(-20);
        var row = MakeDriftRow(
            divergenceType: GetConfigurationDriftReport.DivergenceType.Intentional,
            lastUpdatedAt: lastUpdated);

        var handler = CreateDriftHandler([row], configSvc);
        var result = await handler.Handle(new GetConfigurationDriftReport.Query(TenantId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.StaleConfigKeys.Should().ContainSingle();
    }

    // ═══════════════════════════════════════════════════════════════════════
    // AU.2 — GetPlatformHealthIndexReport
    // ═══════════════════════════════════════════════════════════════════════

    private static GetPlatformHealthIndexReport.Handler CreateHealthHandler(
        IPlatformHealthIndexReader.PlatformHealthRawData rawData,
        IReadOnlyList<IPlatformHealthIndexReader.PlatformHealthTimelinePoint>? timeline = null,
        IConfigurationResolutionService? configService = null)
    {
        var reader = Substitute.For<IPlatformHealthIndexReader>();
        reader.GetPlatformHealthDataAsync(Arg.Any<string>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns(rawData);
        reader.GetTimelineAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(timeline ?? []);
        return new GetPlatformHealthIndexReport.Handler(reader, configService ?? CreateConfigService(), CreateClock());
    }

    [Fact]
    public async Task GetPlatformHealthIndexReport_AllDimensionsPerfect_ReturnsOptimized()
    {
        var handler = CreateHealthHandler(MakeRawData());
        var result = await handler.Handle(new GetPlatformHealthIndexReport.Query(TenantId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Tier.Should().Be(GetPlatformHealthIndexReport.PlatformHealthTier.Optimized);
    }

    [Fact]
    public async Task GetPlatformHealthIndexReport_Score85Exactly_ReturnsOptimized()
    {
        // All dims = 85 → weighted index = 85
        var handler = CreateHealthHandler(MakeRawData(scc: 85, cc: 85, cga: 85, sga: 85, oc: 85, agr: 85, df: 85));
        var result = await handler.Handle(new GetPlatformHealthIndexReport.Query(TenantId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Tier.Should().Be(GetPlatformHealthIndexReport.PlatformHealthTier.Optimized);
    }

    [Fact]
    public async Task GetPlatformHealthIndexReport_Score65To84_ReturnsOperational()
    {
        // All dims = 70 → weighted index = 70 (between 65 and 84)
        var handler = CreateHealthHandler(MakeRawData(scc: 70, cc: 70, cga: 70, sga: 70, oc: 70, agr: 70, df: 70));
        var result = await handler.Handle(new GetPlatformHealthIndexReport.Query(TenantId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Tier.Should().Be(GetPlatformHealthIndexReport.PlatformHealthTier.Operational);
    }

    [Fact]
    public async Task GetPlatformHealthIndexReport_Score40To64_ReturnsPartial()
    {
        // All dims = 50 → weighted index = 50 (between 40 and 64)
        var handler = CreateHealthHandler(MakeRawData(scc: 50, cc: 50, cga: 50, sga: 50, oc: 50, agr: 50, df: 50));
        var result = await handler.Handle(new GetPlatformHealthIndexReport.Query(TenantId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Tier.Should().Be(GetPlatformHealthIndexReport.PlatformHealthTier.Partial);
    }

    [Fact]
    public async Task GetPlatformHealthIndexReport_ScoreBelow40_ReturnsUnderutilized()
    {
        var handler = CreateHealthHandler(MakeRawData(scc: 0, cc: 0, cga: 0, sga: 0, oc: 0, agr: 0, df: 0));
        var result = await handler.Handle(new GetPlatformHealthIndexReport.Query(TenantId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Tier.Should().Be(GetPlatformHealthIndexReport.PlatformHealthTier.Underutilized);
    }

    [Fact]
    public async Task GetPlatformHealthIndexReport_WeightedCalculation_CorrectIndex()
    {
        // SCC=80(15%), CC=60(15%), CGA=90(15%), SGA=70(15%), OC=50(10%), AGR=100(15%), DF=40(15%)
        // = 80*0.15 + 60*0.15 + 90*0.15 + 70*0.15 + 50*0.10 + 100*0.15 + 40*0.15
        // = 12 + 9 + 13.5 + 10.5 + 5 + 15 + 6 = 71
        var handler = CreateHealthHandler(MakeRawData(scc: 80, cc: 60, cga: 90, sga: 70, oc: 50, agr: 100, df: 40));
        var result = await handler.Handle(new GetPlatformHealthIndexReport.Query(TenantId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.PlatformHealthIndex.Should().Be(71m);
    }

    [Fact]
    public async Task GetPlatformHealthIndexReport_WeakestDimensions_Returns3Lowest()
    {
        // SCC=10, CC=20, CGA=90, SGA=80, OC=30, AGR=70, DF=60 → weakest 3: SCC(10), CC(20), OC(30)
        var handler = CreateHealthHandler(MakeRawData(scc: 10, cc: 20, cga: 90, sga: 80, oc: 30, agr: 70, df: 60));
        var result = await handler.Handle(new GetPlatformHealthIndexReport.Query(TenantId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.WeakestDimensions.Should().HaveCount(3);
        result.Value.WeakestDimensions.Select(d => d.Name).Should().Contain("ServiceCatalogCompleteness");
        result.Value.WeakestDimensions.Select(d => d.Name).Should().Contain("ContractCoverage");
        result.Value.WeakestDimensions.Select(d => d.Name).Should().Contain("ObservabilityContextualization");
    }

    [Fact]
    public async Task GetPlatformHealthIndexReport_ValueRealizationScore_GeometricMean()
    {
        // cc=27, cga=27, sga=27 → geometric mean = 27
        var handler = CreateHealthHandler(MakeRawData(cc: 27, cga: 27, sga: 27));
        var result = await handler.Handle(new GetPlatformHealthIndexReport.Query(TenantId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.ValueRealizationScore.Should().BeApproximately(27m, 0.01m);
    }

    [Fact]
    public async Task GetPlatformHealthIndexReport_BenchmarkNull_WhenNoBenchmark()
    {
        var handler = CreateHealthHandler(MakeRawData(benchmark: null));
        var result = await handler.Handle(new GetPlatformHealthIndexReport.Query(TenantId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.TenantBenchmarkPosition.Should().BeNull();
    }

    [Fact]
    public async Task GetPlatformHealthIndexReport_Timeline_HasMonthlyPoints()
    {
        var timeline = Enumerable.Range(0, 6)
            .Select(i => new IPlatformHealthIndexReader.PlatformHealthTimelinePoint(
                FixedNow.AddMonths(-i), 75m))
            .ToList();
        var handler = CreateHealthHandler(MakeRawData(), timeline);
        var result = await handler.Handle(new GetPlatformHealthIndexReport.Query(TenantId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.PlatformHealthTimeline.Should().HaveCount(6);
    }

    [Fact]
    public async Task GetPlatformHealthIndexReport_DimensionBreakdown_ContainsAll7Dims()
    {
        var handler = CreateHealthHandler(MakeRawData());
        var result = await handler.Handle(new GetPlatformHealthIndexReport.Query(TenantId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Dimensions.Should().HaveCount(7);
        result.Value.Dimensions.Select(d => d.Name).Should().Contain("ServiceCatalogCompleteness");
        result.Value.Dimensions.Select(d => d.Name).Should().Contain("ContractCoverage");
        result.Value.Dimensions.Select(d => d.Name).Should().Contain("DataFreshness");
    }

    [Fact]
    public async Task GetPlatformHealthIndexReport_EmptyTenantId_ValidationFails()
    {
        var validator = new GetPlatformHealthIndexReport.Validator();
        var validationResult = await validator.ValidateAsync(new GetPlatformHealthIndexReport.Query(""));

        validationResult.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task GetPlatformHealthIndexReport_ObservabilityWeight10Pct_CorrectWeight()
    {
        var handler = CreateHealthHandler(MakeRawData());
        var result = await handler.Handle(new GetPlatformHealthIndexReport.Query(TenantId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var obsDim = result.Value!.Dimensions.Single(d => d.Name == "ObservabilityContextualization");
        obsDim.WeightPct.Should().Be(10m);
    }

    [Fact]
    public async Task GetPlatformHealthIndexReport_OptimizedThresholdFromConfig_Used()
    {
        // Config sets optimized threshold to 95, score = 90 → Operational (not Optimized)
        var configSvc = CreateConfigService();
        SetupConfig(configSvc, GetPlatformHealthIndexReport.OptimizedThresholdKey, "95");

        var handler = CreateHealthHandler(MakeRawData(scc: 90, cc: 90, cga: 90, sga: 90, oc: 90, agr: 90, df: 90), configService: configSvc);
        var result = await handler.Handle(new GetPlatformHealthIndexReport.Query(TenantId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Tier.Should().Be(GetPlatformHealthIndexReport.PlatformHealthTier.Operational);
    }

    [Fact]
    public async Task GetPlatformHealthIndexReport_OperationalThresholdFromConfig_Used()
    {
        // Config sets operational threshold to 80, all dims=75 → score=75 → Partial (below 80)
        var configSvc = CreateConfigService();
        SetupConfig(configSvc, GetPlatformHealthIndexReport.OperationalThresholdKey, "80");

        var handler = CreateHealthHandler(MakeRawData(scc: 75, cc: 75, cga: 75, sga: 75, oc: 75, agr: 75, df: 75), configService: configSvc);
        var result = await handler.Handle(new GetPlatformHealthIndexReport.Query(TenantId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Tier.Should().Be(GetPlatformHealthIndexReport.PlatformHealthTier.Partial);
    }

    [Fact]
    public async Task GetPlatformHealthIndexReport_ZeroScoreAllDims_ReturnsUnderutilized()
    {
        var handler = CreateHealthHandler(MakeRawData(scc: 0, cc: 0, cga: 0, sga: 0, oc: 0, agr: 0, df: 0));
        var result = await handler.Handle(new GetPlatformHealthIndexReport.Query(TenantId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Tier.Should().Be(GetPlatformHealthIndexReport.PlatformHealthTier.Underutilized);
        result.Value.ValueRealizationScore.Should().Be(0m);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // AU.3 — GetAdaptiveRecommendationReport
    // ═══════════════════════════════════════════════════════════════════════

    private static GetAdaptiveRecommendationReport.Handler CreateRecommendationHandler(
        IReadOnlyList<IAdaptiveRecommendationReader.RecommendationSignal> signals,
        IConfigurationResolutionService? configService = null)
    {
        var reader = Substitute.For<IAdaptiveRecommendationReader>();
        reader.GetSignalsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(signals);
        return new GetAdaptiveRecommendationReport.Handler(reader, configService ?? CreateConfigService(), CreateClock());
    }

    [Fact]
    public async Task GetAdaptiveRecommendationReport_EmptySignals_ReturnsEmptyReport()
    {
        var handler = CreateRecommendationHandler([]);
        var result = await handler.Handle(new GetAdaptiveRecommendationReport.Query(TenantId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Top10Recommendations.Should().BeEmpty();
        result.Value.CategoryDistribution.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAdaptiveRecommendationReport_5Signals_ReturnsAll5()
    {
        var signals = Enumerable.Range(0, 5).Select(_ => MakeSignal()).ToList();
        var handler = CreateRecommendationHandler(signals);
        var result = await handler.Handle(new GetAdaptiveRecommendationReport.Query(TenantId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Top10Recommendations.Should().HaveCount(5);
    }

    [Fact]
    public async Task GetAdaptiveRecommendationReport_15Signals_LimitedToTopN()
    {
        var signals = Enumerable.Range(0, 15).Select(_ => MakeSignal()).ToList();
        var handler = CreateRecommendationHandler(signals);
        var result = await handler.Handle(new GetAdaptiveRecommendationReport.Query(TenantId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Top10Recommendations.Should().HaveCount(10);
    }

    [Fact]
    public async Task GetAdaptiveRecommendationReport_HighImpactLowEffort_RanksFirst()
    {
        var highPriority = MakeSignal(impactScore: 100, effort: GetAdaptiveRecommendationReport.EffortEstimate.Low, title: "High Priority");
        var lowPriority = MakeSignal(impactScore: 10, effort: GetAdaptiveRecommendationReport.EffortEstimate.High, title: "Low Priority");
        var handler = CreateRecommendationHandler([lowPriority, highPriority]);
        var result = await handler.Handle(new GetAdaptiveRecommendationReport.Query(TenantId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Top10Recommendations[0].Title.Should().Be("High Priority");
    }

    [Fact]
    public async Task GetAdaptiveRecommendationReport_PriorityScore_ImpactDividedByEffort()
    {
        // impact=80, effort=Low(1.0) → priority = 80.0
        var signal = MakeSignal(impactScore: 80, effort: GetAdaptiveRecommendationReport.EffortEstimate.Low);
        var handler = CreateRecommendationHandler([signal]);
        var result = await handler.Handle(new GetAdaptiveRecommendationReport.Query(TenantId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Top10Recommendations[0].PriorityScore.Should().Be(80m);
    }

    [Fact]
    public async Task GetAdaptiveRecommendationReport_CategoryDistribution_CorrectCounts()
    {
        var signals = new[]
        {
            MakeSignal(category: GetAdaptiveRecommendationReport.RecommendationCategory.Reliability),
            MakeSignal(category: GetAdaptiveRecommendationReport.RecommendationCategory.Reliability),
            MakeSignal(category: GetAdaptiveRecommendationReport.RecommendationCategory.Security),
        };
        var handler = CreateRecommendationHandler(signals);
        var result = await handler.Handle(new GetAdaptiveRecommendationReport.Query(TenantId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var reliability = result.Value!.CategoryDistribution.Single(c => c.Category == GetAdaptiveRecommendationReport.RecommendationCategory.Reliability);
        reliability.Count.Should().Be(2);
        var security = result.Value.CategoryDistribution.Single(c => c.Category == GetAdaptiveRecommendationReport.RecommendationCategory.Security);
        security.Count.Should().Be(1);
    }

    [Fact]
    public async Task GetAdaptiveRecommendationReport_ActionabilityPct_OnlyLowMedium()
    {
        // 2 Low + 1 High → actionability = 2/3 * 100 = 66.67%
        var signals = new[]
        {
            MakeSignal(effort: GetAdaptiveRecommendationReport.EffortEstimate.Low),
            MakeSignal(effort: GetAdaptiveRecommendationReport.EffortEstimate.Low),
            MakeSignal(effort: GetAdaptiveRecommendationReport.EffortEstimate.High),
        };
        var handler = CreateRecommendationHandler(signals);
        var result = await handler.Handle(new GetAdaptiveRecommendationReport.Query(TenantId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.RecommendationActionability.Should().BeApproximately(66.67m, 0.01m);
    }

    [Fact]
    public async Task GetAdaptiveRecommendationReport_TenantActionPrioritySummary_3Items()
    {
        var signals = Enumerable.Range(0, 5).Select(i => MakeSignal(title: $"Item {i}")).ToList();
        var handler = CreateRecommendationHandler(signals);
        var result = await handler.Handle(new GetAdaptiveRecommendationReport.Query(TenantId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.TenantActionPrioritySummary.Should().HaveCount(3);
    }

    [Fact]
    public async Task GetAdaptiveRecommendationReport_LowEffort_Multiplier1()
    {
        var signal = MakeSignal(effort: GetAdaptiveRecommendationReport.EffortEstimate.Low);
        var handler = CreateRecommendationHandler([signal]);
        var result = await handler.Handle(new GetAdaptiveRecommendationReport.Query(TenantId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Top10Recommendations[0].EffortMultiplier.Should().Be(1.0m);
    }

    [Fact]
    public async Task GetAdaptiveRecommendationReport_MediumEffort_Multiplier2()
    {
        var signal = MakeSignal(effort: GetAdaptiveRecommendationReport.EffortEstimate.Medium);
        var handler = CreateRecommendationHandler([signal]);
        var result = await handler.Handle(new GetAdaptiveRecommendationReport.Query(TenantId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Top10Recommendations[0].EffortMultiplier.Should().Be(2.0m);
    }

    [Fact]
    public async Task GetAdaptiveRecommendationReport_HighEffort_Multiplier3()
    {
        var signal = MakeSignal(effort: GetAdaptiveRecommendationReport.EffortEstimate.High);
        var handler = CreateRecommendationHandler([signal]);
        var result = await handler.Handle(new GetAdaptiveRecommendationReport.Query(TenantId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Top10Recommendations[0].EffortMultiplier.Should().Be(3.0m);
    }

    [Fact]
    public async Task GetAdaptiveRecommendationReport_RefreshedAt_IsCurrentTime()
    {
        var handler = CreateRecommendationHandler([MakeSignal()]);
        var result = await handler.Handle(new GetAdaptiveRecommendationReport.Query(TenantId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.RefreshedAt.Should().Be(FixedNow);
    }

    [Fact]
    public async Task GetAdaptiveRecommendationReport_EmptyTenantId_ValidationFails()
    {
        var validator = new GetAdaptiveRecommendationReport.Validator();
        var validationResult = await validator.ValidateAsync(new GetAdaptiveRecommendationReport.Query(""));

        validationResult.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task GetAdaptiveRecommendationReport_TopN0_ValidationFails()
    {
        var validator = new GetAdaptiveRecommendationReport.Validator();
        var validationResult = await validator.ValidateAsync(new GetAdaptiveRecommendationReport.Query(TenantId, TopN: 0));

        validationResult.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task GetAdaptiveRecommendationReport_TopN55_ValidationFails()
    {
        var validator = new GetAdaptiveRecommendationReport.Validator();
        var validationResult = await validator.ValidateAsync(new GetAdaptiveRecommendationReport.Query(TenantId, TopN: 55));

        validationResult.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task GetAdaptiveRecommendationReport_TopNFromConfig_OverridesDefault()
    {
        // 15 signals, config top_n=5 → 5 items
        var configSvc = CreateConfigService();
        SetupConfig(configSvc, GetAdaptiveRecommendationReport.TopNKey, "5");

        var signals = Enumerable.Range(0, 15).Select(_ => MakeSignal()).ToList();
        var handler = CreateRecommendationHandler(signals, configSvc);
        var result = await handler.Handle(new GetAdaptiveRecommendationReport.Query(TenantId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Top10Recommendations.Should().HaveCount(5);
    }
}
