using FluentAssertions;
using NSubstitute;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.OperationalIntelligence.Application.Reliability.Abstractions;
using NexTraceOne.OperationalIntelligence.Application.Reliability.Features.PredictServiceFailure;
using NexTraceOne.OperationalIntelligence.Application.Reliability.Features.GetCapacityForecast;
using NexTraceOne.OperationalIntelligence.Application.Reliability.Features.GetSloBurnRateAlert;
using NexTraceOne.OperationalIntelligence.Application.Reliability.Features.GetChangeRiskPrediction;
using NexTraceOne.OperationalIntelligence.Application.Runtime.Features.CorrelateTraceToChange;
using NexTraceOne.OperationalIntelligence.Application.Runtime.Features.DetectLogAnomaly;
using NexTraceOne.OperationalIntelligence.Domain.Reliability.Entities;
using Xunit;

namespace NexTraceOne.OperationalIntelligence.Tests.Predictive;

/// <summary>
/// Testes unitários para as features de Predictive Intelligence (5.1) e
/// Observability Correlation Engine (5.4).
/// </summary>
public sealed class PredictiveIntelligenceTests
{
    private static readonly DateTimeOffset FixedNow = new(2026, 4, 5, 14, 0, 0, TimeSpan.Zero);

    // ── Domain: ServiceFailurePrediction ─────────────────────────────────

    [Fact]
    public void ServiceFailurePrediction_Create_WithValidInputs_ShouldSucceed()
    {
        var result = ServiceFailurePrediction.Create(
            "svc-1", "OrderService", "production",
            45m, "24h", ["High error rate: 30%"], "Monitor closely.", FixedNow);

        result.IsSuccess.Should().BeTrue();
        result.Value.ServiceId.Should().Be("svc-1");
        result.Value.RiskLevel.Should().Be("Medium");
        result.Value.FailureProbabilityPercent.Should().Be(45m);
    }

    [Fact]
    public void ServiceFailurePrediction_Create_WithHighProbability_ShouldReturnHighRisk()
    {
        var result = ServiceFailurePrediction.Create(
            "svc-2", "PaymentService", "production",
            75m, "7d", [], null, FixedNow);

        result.IsSuccess.Should().BeTrue();
        result.Value.RiskLevel.Should().Be("High");
    }

    [Fact]
    public void ServiceFailurePrediction_Create_WithLowProbability_ShouldReturnLowRisk()
    {
        var result = ServiceFailurePrediction.Create(
            "svc-3", "HealthService", "staging",
            10m, "48h", [], null, FixedNow);

        result.IsSuccess.Should().BeTrue();
        result.Value.RiskLevel.Should().Be("Low");
    }

    [Fact]
    public void ServiceFailurePrediction_Create_WithInvalidProbability_ShouldFail()
    {
        var result = ServiceFailurePrediction.Create(
            "svc-1", "OrderService", "production",
            150m, "24h", [], null, FixedNow);

        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("INVALID_PROBABILITY");
    }

    [Fact]
    public void ServiceFailurePrediction_Create_WithInvalidHorizon_ShouldFail()
    {
        var result = ServiceFailurePrediction.Create(
            "svc-1", "OrderService", "production",
            30m, "12h", [], null, FixedNow);

        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("INVALID_PREDICTION_HORIZON");
    }

    [Fact]
    public void ServiceFailurePrediction_Create_WithTooManyCausalFactors_ShouldFail()
    {
        var tooMany = new List<string> { "F1", "F2", "F3", "F4", "F5", "F6" };
        var result = ServiceFailurePrediction.Create(
            "svc-1", "OrderService", "production",
            30m, "24h", tooMany, null, FixedNow);

        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("TOO_MANY_CAUSAL_FACTORS");
    }

    // ── Domain: CapacityForecast ──────────────────────────────────────────

    [Fact]
    public void CapacityForecast_Create_WithValidInputs_ShouldSucceed()
    {
        var result = CapacityForecast.Create(
            "svc-1", "OrderService", "production",
            "CPU", 70m, 2m, 15, null, FixedNow);

        result.IsSuccess.Should().BeTrue();
        result.Value.SaturationRisk.Should().Be("Near");
    }

    [Fact]
    public void CapacityForecast_ComputeSaturationRisk_WhenDaysIsNull_ShouldReturnLow()
    {
        CapacityForecast.ComputeSaturationRisk(null).Should().Be("Low");
    }

    [Fact]
    public void CapacityForecast_ComputeSaturationRisk_WhenDaysLessThan7_ShouldReturnImmediate()
    {
        CapacityForecast.ComputeSaturationRisk(5).Should().Be("Immediate");
    }

    [Fact]
    public void CapacityForecast_ComputeSaturationRisk_WhenDaysLessThan30_ShouldReturnNear()
    {
        CapacityForecast.ComputeSaturationRisk(20).Should().Be("Near");
    }

    [Fact]
    public void CapacityForecast_Create_WithInvalidResourceType_ShouldFail()
    {
        var result = CapacityForecast.Create(
            "svc-1", "OrderService", "production",
            "GPU", 70m, 2m, null, null, FixedNow);

        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("INVALID_RESOURCE_TYPE");
    }

    // ── Handler: PredictServiceFailure ───────────────────────────────────

    [Fact]
    public async Task PredictServiceFailure_WhenHighRisk_ShouldReturnCriticalRecommendation()
    {
        var repo = Substitute.For<IServiceFailurePredictionRepository>();
        var uow = Substitute.For<IUnitOfWork>();
        var clock = Substitute.For<IDateTimeProvider>();
        clock.UtcNow.Returns(FixedNow);

        var handler = new PredictServiceFailure.Handler(repo, uow, clock);
        var cmd = new PredictServiceFailure.Command(
            "svc-payment", "PaymentService", "production", "24h",
            30m, 8, 8m, null);

        var result = await handler.Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.RiskLevel.Should().Be("High");
        result.Value.RecommendedAction.Should().Contain("Immediate action");
        repo.Received(1).Add(Arg.Any<ServiceFailurePrediction>());
        await uow.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task PredictServiceFailure_WhenLowRisk_ShouldReturnRoutineRecommendation()
    {
        var repo = Substitute.For<IServiceFailurePredictionRepository>();
        var uow = Substitute.For<IUnitOfWork>();
        var clock = Substitute.For<IDateTimeProvider>();
        clock.UtcNow.Returns(FixedNow);

        var handler = new PredictServiceFailure.Handler(repo, uow, clock);
        var cmd = new PredictServiceFailure.Command(
            "svc-stable", "StableService", "production", "7d",
            1m, 0, 0m, null);

        var result = await handler.Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.RiskLevel.Should().Be("Low");
        result.Value.RecommendedAction.Should().Contain("routine monitoring");
    }

    [Fact]
    public async Task PredictServiceFailure_WithAdditionalContext_ShouldIncludeInCausalFactors()
    {
        var repo = Substitute.For<IServiceFailurePredictionRepository>();
        var uow = Substitute.For<IUnitOfWork>();
        var clock = Substitute.For<IDateTimeProvider>();
        clock.UtcNow.Returns(FixedNow);

        var handler = new PredictServiceFailure.Handler(repo, uow, clock);
        var cmd = new PredictServiceFailure.Command(
            "svc-x", "XService", "staging", "48h",
            20m, 3, 4m, "Database timeout detected");

        var result = await handler.Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.CausalFactors.Should().Contain("Database timeout detected");
    }

    // ── Handler: GetCapacityForecast ─────────────────────────────────────

    [Fact]
    public async Task GetCapacityForecast_WhenGrowthRatePositive_ShouldComputeDaysToSaturation()
    {
        var repo = Substitute.For<ICapacityForecastRepository>();
        var uow = Substitute.For<IUnitOfWork>();
        var clock = Substitute.For<IDateTimeProvider>();
        clock.UtcNow.Returns(FixedNow);

        var handler = new GetCapacityForecast.Handler(repo, uow, clock);
        var cmd = new GetCapacityForecast.Command(
            "svc-db", "DatabaseService", "production",
            "Memory", 80m, 2m, null);

        var result = await handler.Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.EstimatedDaysToSaturation.Should().Be(10);
        result.Value.SaturationRisk.Should().Be("Near");
    }

    [Fact]
    public async Task GetCapacityForecast_WhenZeroGrowthRate_ShouldNotComputeDays()
    {
        var repo = Substitute.For<ICapacityForecastRepository>();
        var uow = Substitute.For<IUnitOfWork>();
        var clock = Substitute.For<IDateTimeProvider>();
        clock.UtcNow.Returns(FixedNow);

        var handler = new GetCapacityForecast.Handler(repo, uow, clock);
        var cmd = new GetCapacityForecast.Command(
            "svc-static", "StaticService", "production",
            "CPU", 50m, 0m, null);

        var result = await handler.Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.EstimatedDaysToSaturation.Should().BeNull();
        result.Value.SaturationRisk.Should().Be("Low");
    }

    // ── Handler: GetSloBurnRateAlert ──────────────────────────────────────

    [Fact]
    public async Task GetSloBurnRateAlert_WhenBurnRateAbove14_ShouldBeCritical()
    {
        var clock = Substitute.For<IDateTimeProvider>();
        clock.UtcNow.Returns(FixedNow);

        var handler = new GetSloBurnRateAlert.Handler(clock);
        var query = new GetSloBurnRateAlert.Query(
            "svc-1", "production", 15m, 99m, "1");

        var result = await handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.IsCritical.Should().BeTrue();
        result.Value.IsWarning.Should().BeFalse();
        result.Value.AlertMessage.Should().Contain("CRITICAL");
    }

    [Fact]
    public async Task GetSloBurnRateAlert_WhenBurnRateBetween1And14_ShouldBeWarning()
    {
        var clock = Substitute.For<IDateTimeProvider>();
        clock.UtcNow.Returns(FixedNow);

        var handler = new GetSloBurnRateAlert.Handler(clock);
        var query = new GetSloBurnRateAlert.Query(
            "svc-1", "production", 2m, 99m, "24");

        var result = await handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.IsWarning.Should().BeTrue();
        result.Value.IsCritical.Should().BeFalse();
        result.Value.AlertMessage.Should().Contain("WARNING");
    }

    [Fact]
    public async Task GetSloBurnRateAlert_WhenBurnRateLow_ShouldBeOk()
    {
        var clock = Substitute.For<IDateTimeProvider>();
        clock.UtcNow.Returns(FixedNow);

        var handler = new GetSloBurnRateAlert.Handler(clock);
        var query = new GetSloBurnRateAlert.Query(
            "svc-1", "production", 0.05m, 99.9m, "168");

        var result = await handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.IsCritical.Should().BeFalse();
        result.Value.IsWarning.Should().BeFalse();
        result.Value.AlertMessage.Should().Contain("OK");
    }

    // ── Handler: GetChangeRiskPrediction ─────────────────────────────────

    [Fact]
    public async Task GetChangeRiskPrediction_WhenBreakingChange_ShouldReturnHighRisk()
    {
        var clock = Substitute.For<IDateTimeProvider>();
        clock.UtcNow.Returns(FixedNow);

        var handler = new GetChangeRiskPrediction.Handler(clock);
        var changeId = Guid.NewGuid();
        var query = new GetChangeRiskPrediction.Query(
            changeId, "svc-critical", "production",
            5, 60m, false, true, "Breaking");

        var result = await handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.RiskLevel.Should().BeOneOf("High", "Critical");
        result.Value.Recommendations.Should().Contain(r => r.Contains("approval"));
    }

    [Fact]
    public async Task GetChangeRiskPrediction_WhenLowRiskChange_ShouldReturnStandardChecklist()
    {
        var clock = Substitute.For<IDateTimeProvider>();
        clock.UtcNow.Returns(FixedNow);

        var handler = new GetChangeRiskPrediction.Handler(clock);
        var changeId = Guid.NewGuid();
        var query = new GetChangeRiskPrediction.Query(
            changeId, "svc-simple", "staging",
            0, 5m, true, false, "Patch");

        var result = await handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.RiskLevel.Should().BeOneOf("Low", "Medium");
        result.Value.Recommendations.Should().Contain(r => r.Contains("standard deployment"));
    }

    [Fact]
    public async Task GetChangeRiskPrediction_WhenNoTestEvidence_ShouldSuggestAddingTests()
    {
        var clock = Substitute.For<IDateTimeProvider>();
        clock.UtcNow.Returns(FixedNow);

        var handler = new GetChangeRiskPrediction.Handler(clock);
        var query = new GetChangeRiskPrediction.Query(
            Guid.NewGuid(), "svc-x", "production",
            0, 10m, false, false, "Minor");

        var result = await handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Recommendations.Should().Contain(r => r.Contains("test evidence"));
    }

    // ── Handler: CorrelateTraceToChange ───────────────────────────────────

    [Fact]
    public async Task CorrelateTraceToChange_WhenNoChangeData_ShouldReturnNoCorrelation()
    {
        var clock = Substitute.For<IDateTimeProvider>();
        clock.UtcNow.Returns(FixedNow);

        var handler = new CorrelateTraceToChange.Handler(clock);
        var query = new CorrelateTraceToChange.Query(
            "trace-abc-123", "svc-order", "production", FixedNow);

        var result = await handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.HasCorrelatedChanges.Should().BeFalse();
        result.Value.TraceId.Should().Be("trace-abc-123");
        result.Value.CorrelationReason.Should().NotBeNullOrEmpty();
    }

    // ── Handler: DetectLogAnomaly ─────────────────────────────────────────

    [Fact]
    public async Task DetectLogAnomaly_WhenErrorSpikeAbove50Percent_ShouldDetectAnomaly()
    {
        var clock = Substitute.For<IDateTimeProvider>();
        clock.UtcNow.Returns(FixedNow);

        var handler = new DetectLogAnomaly.Handler(clock);
        var cmd = new DetectLogAnomaly.Command(
            "svc-api", "production",
            ErrorCountLastHour: 200, ErrorCountPreviousHour: 10,
            WarningCountLastHour: 50, BaselineErrorRate: 10m, ChangeId: null);

        var result = await handler.Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.IsAnomaly.Should().BeTrue();
        result.Value.AnomalyType.Should().Be("ErrorSpike");
    }

    [Fact]
    public async Task DetectLogAnomaly_WhenPostChangeAnomaly_ShouldReturnRegressionType()
    {
        var clock = Substitute.For<IDateTimeProvider>();
        clock.UtcNow.Returns(FixedNow);

        var handler = new DetectLogAnomaly.Handler(clock);
        var cmd = new DetectLogAnomaly.Command(
            "svc-payment", "production",
            ErrorCountLastHour: 150, ErrorCountPreviousHour: 5,
            WarningCountLastHour: 20, BaselineErrorRate: 5m, ChangeId: "deploy-789");

        var result = await handler.Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.IsAnomaly.Should().BeTrue();
        result.Value.PostChangeAnomaly.Should().BeTrue();
        result.Value.Recommendation.Should().Contain("rollback");
    }

    [Fact]
    public async Task DetectLogAnomaly_WhenNoAnomaly_ShouldReturnNoneType()
    {
        var clock = Substitute.For<IDateTimeProvider>();
        clock.UtcNow.Returns(FixedNow);

        var handler = new DetectLogAnomaly.Handler(clock);
        var cmd = new DetectLogAnomaly.Command(
            "svc-stable", "staging",
            ErrorCountLastHour: 5, ErrorCountPreviousHour: 4,
            WarningCountLastHour: 2, BaselineErrorRate: 10m, ChangeId: null);

        var result = await handler.Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.IsAnomaly.Should().BeFalse();
        result.Value.AnomalyType.Should().Be("None");
        result.Value.Recommendation.Should().BeNull();
    }

    // ── Validator tests ───────────────────────────────────────────────────

    [Fact]
    public void PredictServiceFailure_Validator_WhenInvalidHorizon_ShouldFailValidation()
    {
        var validator = new PredictServiceFailure.Validator();
        var cmd = new PredictServiceFailure.Command(
            "svc-1", "Service", "production", "12h", 5m, 0, 0m, null);

        var result = validator.Validate(cmd);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage.Contains("Valid horizons"));
    }

    [Fact]
    public void GetCapacityForecast_Validator_WhenInvalidResourceType_ShouldFailValidation()
    {
        var validator = new GetCapacityForecast.Validator();
        var cmd = new GetCapacityForecast.Command(
            "svc-1", "Service", "production", "GPU", 50m, 0m, null);

        var result = validator.Validate(cmd);

        result.IsValid.Should().BeFalse();
    }
}
