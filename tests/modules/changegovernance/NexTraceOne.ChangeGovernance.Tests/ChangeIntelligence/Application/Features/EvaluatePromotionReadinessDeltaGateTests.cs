using Microsoft.Extensions.Options;

using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Abstractions;
using NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Features.GetPromotionReadinessDelta;

using Feature = NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Features.EvaluatePromotionReadinessDeltaGate.EvaluatePromotionReadinessDeltaGate;
using FeatureOptions = NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Features.EvaluatePromotionReadinessDeltaGate.PromotionReadinessDeltaOptions;

namespace NexTraceOne.ChangeGovernance.Tests.ChangeIntelligence.Application.Features;

/// <summary>
/// Testes unitários para <see cref="Feature"/>.
///
/// Cobre:
/// - Ready → gate passa sempre;
/// - Review + block_on_review=false → passa com aviso;
/// - Review + block_on_review=true → falha;
/// - Blocked → falha sempre;
/// - Unknown (DataQuality=0) → passa (non-blocking);
/// - Validação de campos obrigatórios.
/// </summary>
public sealed class EvaluatePromotionReadinessDeltaGateTests
{
    private static readonly Guid TenantId = Guid.Parse("22222222-2222-2222-2222-222222222222");

    private readonly IRuntimeComparisonReader _reader = Substitute.For<IRuntimeComparisonReader>();
    private readonly ICurrentTenant _tenant = Substitute.For<ICurrentTenant>();

    public EvaluatePromotionReadinessDeltaGateTests()
    {
        _tenant.Id.Returns(TenantId);
    }

    private Feature.Handler CreateHandler(bool blockOnReview = false)
    {
        var options = Options.Create(new FeatureOptions { BlockOnReview = blockOnReview });
        return new Feature.Handler(_reader, _tenant, options);
    }

    private void SetupReader(
        decimal? errorRateDelta = null,
        decimal? latencyDelta = null,
        int? incidentsDelta = null,
        decimal dataQuality = 0.9m,
        string? simulatedNote = null)
    {
        _reader.CompareAsync(TenantId, "svc", "staging", "production", Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(new RuntimeComparisonSnapshot(
                "svc", "staging", "production", 7,
                ErrorRateDelta: errorRateDelta,
                LatencyP95DeltaMs: latencyDelta,
                ThroughputDelta: null,
                CostDelta: null,
                IncidentsDelta: incidentsDelta,
                DataQuality: dataQuality,
                SimulatedNote: simulatedNote));
    }

    [Fact]
    public async Task Handler_WhenDeltasGood_ReturnsPassedAndReady()
    {
        SetupReader(errorRateDelta: 0.001m, latencyDelta: 5m, incidentsDelta: 0);

        var result = await CreateHandler().Handle(
            new Feature.Query("svc", "staging", "production", null),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Passed.Should().BeTrue();
        result.Value.Readiness.Should().Be(PromotionReadinessLevel.Ready);
    }

    [Fact]
    public async Task Handler_WhenModerateLatencyRegression_AndBlockOnReviewFalse_PassesWithWarning()
    {
        SetupReader(latencyDelta: 120m);

        var result = await CreateHandler(blockOnReview: false).Handle(
            new Feature.Query("svc", "staging", "production", null),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Passed.Should().BeTrue();
        result.Value.Readiness.Should().Be(PromotionReadinessLevel.Review);
        result.Value.Reason.Should().Contain("non-blocking");
    }

    [Fact]
    public async Task Handler_WhenModerateLatencyRegression_AndBlockOnReviewTrue_Fails()
    {
        SetupReader(latencyDelta: 120m);

        var result = await CreateHandler(blockOnReview: true).Handle(
            new Feature.Query("svc", "staging", "production", null),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Passed.Should().BeFalse();
        result.Value.Readiness.Should().Be(PromotionReadinessLevel.Review);
    }

    [Fact]
    public async Task Handler_WhenHighErrorRateDelta_ReturnsFailed_Blocked()
    {
        SetupReader(errorRateDelta: 0.08m);

        var result = await CreateHandler().Handle(
            new Feature.Query("svc", "staging", "production", null),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Passed.Should().BeFalse();
        result.Value.Readiness.Should().Be(PromotionReadinessLevel.Blocked);
        result.Value.Reason.Should().Contain("error rate");
    }

    [Fact]
    public async Task Handler_WhenIncidentsIncreased_ReturnsFailed_Blocked()
    {
        SetupReader(incidentsDelta: 1);

        var result = await CreateHandler().Handle(
            new Feature.Query("svc", "staging", "production", null),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Passed.Should().BeFalse();
        result.Value.Readiness.Should().Be(PromotionReadinessLevel.Blocked);
        result.Value.Reason.Should().Contain("incident");
    }

    [Fact]
    public async Task Handler_WhenDataQualityZero_ReturnsUnknown_AlwaysPasses()
    {
        SetupReader(dataQuality: 0m, simulatedNote: "No runtime data available.");

        var result = await CreateHandler(blockOnReview: true).Handle(
            new Feature.Query("svc", "staging", "production", null),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Passed.Should().BeTrue();
        result.Value.Readiness.Should().Be(PromotionReadinessLevel.Unknown);
        result.Value.SimulatedNote.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Handler_ResponseContainsCorrectMetadata()
    {
        SetupReader(errorRateDelta: 0m, latencyDelta: 0m);

        var result = await CreateHandler().Handle(
            new Feature.Query("svc", "staging", "production", 14),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.ServiceName.Should().Be("svc");
        result.Value.EnvironmentFrom.Should().Be("staging");
        result.Value.EnvironmentTo.Should().Be("production");
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
        var result = validator.Validate(
            new Feature.Query("svc", "staging", "production", Feature.MaxWindowDays + 1));
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validator_AcceptsNullWindowDays()
    {
        var validator = new Feature.Validator();
        var result = validator.Validate(
            new Feature.Query("svc", "staging", "production", null));
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validator_RejectsEmptyServiceName()
    {
        var validator = new Feature.Validator();
        var result = validator.Validate(
            new Feature.Query("", "staging", "production", null));
        result.IsValid.Should().BeFalse();
    }
}
