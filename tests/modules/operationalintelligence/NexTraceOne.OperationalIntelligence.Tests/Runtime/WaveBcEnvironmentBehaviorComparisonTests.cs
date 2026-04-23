using System.Linq;
using NSubstitute;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.OperationalIntelligence.Application.Runtime.Abstractions;
using NexTraceOne.OperationalIntelligence.Application.Runtime.Features.GetEnvironmentBehaviorComparisonReport;

namespace NexTraceOne.OperationalIntelligence.Tests.Runtime;

/// <summary>
/// Testes unitários para Wave BC.1 — GetEnvironmentBehaviorComparisonReport.
/// </summary>
public sealed class WaveBcEnvironmentBehaviorComparisonTests
{
    private static readonly DateTimeOffset FixedNow = new(2026, 6, 1, 10, 0, 0, TimeSpan.Zero);
    private const string TenantId = "tenant-bc1";

    private static IDateTimeProvider CreateClock()
    {
        var clock = Substitute.For<IDateTimeProvider>();
        clock.UtcNow.Returns(FixedNow);
        return clock;
    }

    private static IEnvironmentBehaviorComparisonReader.ServiceBehaviorEntry MakeEntry(
        string serviceId = "svc-1",
        string tier = "Critical",
        decimal sourceP99 = 100m,
        decimal targetP99 = 100m,
        decimal sourceErrorRate = 1m,
        decimal targetErrorRate = 1m,
        decimal sourceAvailability = 99.9m,
        decimal targetAvailability = 99.9m,
        bool configDrift = false,
        int configDriftKeys = 0)
        => new(
            ServiceId: serviceId,
            ServiceName: $"Service {serviceId}",
            ServiceTier: tier,
            SourceP99Ms: sourceP99,
            TargetP99Ms: targetP99,
            SourceErrorRatePct: sourceErrorRate,
            TargetErrorRatePct: targetErrorRate,
            SourceAvailabilityPct: sourceAvailability,
            TargetAvailabilityPct: targetAvailability,
            ConfigDriftDetected: configDrift,
            ConfigDriftKeyCount: configDriftKeys);

    private static GetEnvironmentBehaviorComparisonReport.Handler CreateHandler(
        IReadOnlyList<IEnvironmentBehaviorComparisonReader.ServiceBehaviorEntry> entries,
        IReadOnlyList<IEnvironmentBehaviorComparisonReader.PromotionOutcomeEntry>? outcomes = null)
    {
        var reader = Substitute.For<IEnvironmentBehaviorComparisonReader>();
        reader.ListByTenantAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(entries);
        reader.GetHistoricalPromotionOutcomesAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(outcomes ?? []);
        return new GetEnvironmentBehaviorComparisonReport.Handler(reader, CreateClock());
    }

    [Fact]
    public async Task BC1_InsufficientServices_Returns_InsufficientDataTier()
    {
        // Less than MinServices (3)
        var entries = new[] { MakeEntry("svc-1"), MakeEntry("svc-2") };
        var handler = CreateHandler(entries);
        var result = await handler.Handle(
            new GetEnvironmentBehaviorComparisonReport.Query(TenantId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.OverallTier.Should().Be(GetEnvironmentBehaviorComparisonReport.PromotionReadinessTier.InsufficientData);
    }

    [Fact]
    public async Task BC1_IdenticalBehavior_Returns_ReadyTier_HighScore()
    {
        // All services identical (p99 same, error rate same, no drift)
        var entries = Enumerable.Range(1, 5)
            .Select(i => MakeEntry($"svc-{i}", sourceP99: 100m, targetP99: 100m,
                sourceErrorRate: 1m, targetErrorRate: 1m, configDrift: false))
            .ToList();
        var handler = CreateHandler(entries);
        var result = await handler.Handle(
            new GetEnvironmentBehaviorComparisonReport.Query(TenantId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.OverallTier.Should().Be(GetEnvironmentBehaviorComparisonReport.PromotionReadinessTier.Ready);
        result.Value.TenantBehaviorSimilarityScore.Should().BeGreaterThan(80m);
    }

    [Fact]
    public async Task BC1_CriticalServiceNotReady_BlocksTier()
    {
        // One critical service with highly divergent latency → forces NotReady
        var entries = new[]
        {
            MakeEntry("svc-1", tier: "Critical", sourceP99: 500m, targetP99: 100m), // huge latency divergence
            MakeEntry("svc-2", tier: "Standard"),
            MakeEntry("svc-3", tier: "Standard"),
            MakeEntry("svc-4", tier: "Standard"),
        };
        var handler = CreateHandler(entries);
        var result = await handler.Handle(
            new GetEnvironmentBehaviorComparisonReport.Query(TenantId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.OverallTier.Should().Be(GetEnvironmentBehaviorComparisonReport.PromotionReadinessTier.NotReady);
        result.Value.CriticalServicesNotReadyCount.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task BC1_LatencyDivergence_Generates_Alert()
    {
        // Source p99 much higher than target (25% diff > 20% threshold)
        var entries = Enumerable.Range(1, 4)
            .Select(i => MakeEntry($"svc-{i}", sourceP99: 125m, targetP99: 100m))
            .ToList();
        var handler = CreateHandler(entries);
        var result = await handler.Handle(
            new GetEnvironmentBehaviorComparisonReport.Query(TenantId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.BehaviorDivergenceAlerts.Should().NotBeEmpty();
        result.Value.BehaviorDivergenceAlerts.Any(a => a.DivergenceType == "LatencyDivergence").Should().BeTrue();
    }

    [Fact]
    public async Task BC1_ErrorRateDivergence_Generates_Alert()
    {
        // Error rate divergence > 5%
        var entries = Enumerable.Range(1, 4)
            .Select(i => MakeEntry($"svc-{i}", sourceErrorRate: 10m, targetErrorRate: 1m))
            .ToList();
        var handler = CreateHandler(entries);
        var result = await handler.Handle(
            new GetEnvironmentBehaviorComparisonReport.Query(TenantId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.BehaviorDivergenceAlerts.Any(a => a.DivergenceType == "ErrorRateDivergence").Should().BeTrue();
    }

    [Fact]
    public async Task BC1_ConfigDrift_ReducesSimilarityScore()
    {
        var withDrift = Enumerable.Range(1, 4)
            .Select(i => MakeEntry($"svc-{i}", configDrift: true, configDriftKeys: 3))
            .ToList();
        var withoutDrift = Enumerable.Range(1, 4)
            .Select(i => MakeEntry($"svc-{i}", configDrift: false))
            .ToList();

        var handlerWithDrift = CreateHandler(withDrift);
        var handlerWithoutDrift = CreateHandler(withoutDrift);

        var resultWithDrift = await handlerWithDrift.Handle(
            new GetEnvironmentBehaviorComparisonReport.Query(TenantId), CancellationToken.None);
        var resultWithoutDrift = await handlerWithoutDrift.Handle(
            new GetEnvironmentBehaviorComparisonReport.Query(TenantId), CancellationToken.None);

        resultWithDrift.IsSuccess.Should().BeTrue();
        resultWithoutDrift.IsSuccess.Should().BeTrue();
        resultWithDrift.Value!.TenantBehaviorSimilarityScore.Should()
            .BeLessThan(resultWithoutDrift.Value!.TenantBehaviorSimilarityScore);
    }

    [Fact]
    public async Task BC1_HistoricalOutcomes_BuildsCorrectly()
    {
        var entries = Enumerable.Range(1, 4).Select(i => MakeEntry($"svc-{i}")).ToList();
        var outcomes = new[]
        {
            new IEnvironmentBehaviorComparisonReader.PromotionOutcomeEntry(
                FixedNow.AddDays(-10), "svc-1", true, 90m),
            new IEnvironmentBehaviorComparisonReader.PromotionOutcomeEntry(
                FixedNow.AddDays(-20), "svc-1", false, 45m),
        };
        var handler = CreateHandler(entries, outcomes);
        var result = await handler.Handle(
            new GetEnvironmentBehaviorComparisonReport.Query(TenantId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.HistoricalOutcome.Should().NotBeNull();
        result.Value.HistoricalOutcome!.TotalPromotions.Should().Be(2);
        result.Value.HistoricalOutcome.SuccessRatePct.Should().Be(50m);
    }

    [Fact]
    public async Task BC1_ServiceComparisons_ContainsAllServices()
    {
        var entries = Enumerable.Range(1, 5).Select(i => MakeEntry($"svc-{i}")).ToList();
        var handler = CreateHandler(entries);
        var result = await handler.Handle(
            new GetEnvironmentBehaviorComparisonReport.Query(TenantId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.ServiceComparisons.Should().HaveCount(5);
    }

    [Fact]
    public async Task BC1_Validator_InvalidTenantId_Fails()
    {
        var validator = new GetEnvironmentBehaviorComparisonReport.Validator();
        var validationResult = validator.Validate(new GetEnvironmentBehaviorComparisonReport.Query(""));
        validationResult.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task BC1_Validator_LookbackDays_OutOfRange_Fails()
    {
        var validator = new GetEnvironmentBehaviorComparisonReport.Validator();
        var result = validator.Validate(
            new GetEnvironmentBehaviorComparisonReport.Query(TenantId, LookbackDays: 200));
        result.IsValid.Should().BeFalse();
    }
}
