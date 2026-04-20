using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Abstractions;

using Feature = NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Features.GetPromotionReadinessDelta.GetPromotionReadinessDelta;

namespace NexTraceOne.ChangeGovernance.Tests.ChangeIntelligence.Application.Features;

/// <summary>
/// Testes unitários para <see cref="Feature"/>.
///
/// Cobre:
/// - caminho honest-null (sem dados → Readiness = Unknown + SimulatedNote preservado);
/// - heurística de classificação (Ready/Review/Blocked) a partir dos deltas;
/// - validação dos limites de <c>WindowDays</c> e ambientes distintos.
/// </summary>
public sealed class GetPromotionReadinessDeltaTests
{
    private static readonly Guid TenantId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    private readonly IRuntimeComparisonReader _reader = Substitute.For<IRuntimeComparisonReader>();
    private readonly ICurrentTenant _tenant = Substitute.For<ICurrentTenant>();

    public GetPromotionReadinessDeltaTests()
    {
        _tenant.Id.Returns(TenantId);
    }

    private Feature.Handler CreateHandler() => new(_reader, _tenant);

    [Fact]
    public async Task Handler_WhenReaderReturnsSimulated_ReturnsUnknownAndPreservesNote()
    {
        const string note = "No runtime comparison bridge configured; simulated.";
        _reader.CompareAsync(TenantId, "svc", "staging", "production", 7, Arg.Any<CancellationToken>())
            .Returns(new RuntimeComparisonSnapshot(
                "svc", "staging", "production", 7,
                ErrorRateDelta: null,
                LatencyP95DeltaMs: null,
                ThroughputDelta: null,
                CostDelta: null,
                IncidentsDelta: null,
                DataQuality: 0m,
                SimulatedNote: note));

        var result = await CreateHandler().Handle(
            new Feature.Query("svc", "staging", "production", null),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Readiness.Should().Be(Feature.PromotionReadinessLevel.Unknown);
        result.Value.SimulatedNote.Should().Be(note);
        result.Value.WindowDays.Should().Be(Feature.DefaultWindowDays);
    }

    [Fact]
    public async Task Handler_WhenGoodDeltas_ReturnsReady()
    {
        _reader.CompareAsync(TenantId, "svc", "staging", "production", 14, Arg.Any<CancellationToken>())
            .Returns(new RuntimeComparisonSnapshot(
                "svc", "staging", "production", 14,
                ErrorRateDelta: 0.001m,
                LatencyP95DeltaMs: 10m,
                ThroughputDelta: 0.05m,
                CostDelta: 0.02m,
                IncidentsDelta: 0,
                DataQuality: 0.9m,
                SimulatedNote: null));

        var result = await CreateHandler().Handle(
            new Feature.Query("svc", "staging", "production", 14),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Readiness.Should().Be(Feature.PromotionReadinessLevel.Ready);
        result.Value.SimulatedNote.Should().BeNull();
    }

    [Fact]
    public async Task Handler_WhenLatencyRegressionModerate_ReturnsReview()
    {
        _reader.CompareAsync(TenantId, "svc", "staging", "production", 7, Arg.Any<CancellationToken>())
            .Returns(new RuntimeComparisonSnapshot(
                "svc", "staging", "production", 7,
                ErrorRateDelta: 0m,
                LatencyP95DeltaMs: 120m,
                ThroughputDelta: 0m,
                CostDelta: 0m,
                IncidentsDelta: 0,
                DataQuality: 0.8m,
                SimulatedNote: null));

        var result = await CreateHandler().Handle(
            new Feature.Query("svc", "staging", "production", 7),
            CancellationToken.None);

        result.Value.Readiness.Should().Be(Feature.PromotionReadinessLevel.Review);
    }

    [Fact]
    public async Task Handler_WhenErrorRegressionHigh_ReturnsBlocked()
    {
        _reader.CompareAsync(TenantId, "svc", "staging", "production", 7, Arg.Any<CancellationToken>())
            .Returns(new RuntimeComparisonSnapshot(
                "svc", "staging", "production", 7,
                ErrorRateDelta: 0.10m,
                LatencyP95DeltaMs: 30m,
                ThroughputDelta: 0m,
                CostDelta: 0m,
                IncidentsDelta: 0,
                DataQuality: 0.9m,
                SimulatedNote: null));

        var result = await CreateHandler().Handle(
            new Feature.Query("svc", "staging", "production", 7),
            CancellationToken.None);

        result.Value.Readiness.Should().Be(Feature.PromotionReadinessLevel.Blocked);
    }

    [Fact]
    public async Task Handler_WhenIncidentsRegressed_ReturnsBlockedEvenWithoutOtherSignals()
    {
        _reader.CompareAsync(TenantId, "svc", "staging", "production", 7, Arg.Any<CancellationToken>())
            .Returns(new RuntimeComparisonSnapshot(
                "svc", "staging", "production", 7,
                ErrorRateDelta: null,
                LatencyP95DeltaMs: null,
                ThroughputDelta: null,
                CostDelta: null,
                IncidentsDelta: 2,
                DataQuality: 0.5m,
                SimulatedNote: null));

        var result = await CreateHandler().Handle(
            new Feature.Query("svc", "staging", "production", 7),
            CancellationToken.None);

        result.Value.Readiness.Should().Be(Feature.PromotionReadinessLevel.Blocked);
    }

    [Fact]
    public void Validator_RejectsEnvFromEqualsEnvTo()
    {
        var validator = new Feature.Validator();
        var result = validator.Validate(new Feature.Query("svc", "prod", "prod", null));
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validator_RejectsWindowDaysOverMax()
    {
        var validator = new Feature.Validator();
        var result = validator.Validate(new Feature.Query("svc", "staging", "production", Feature.MaxWindowDays + 1));
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validator_AcceptsNullWindowDays()
    {
        var validator = new Feature.Validator();
        var result = validator.Validate(new Feature.Query("svc", "staging", "production", null));
        result.IsValid.Should().BeTrue();
    }
}
