using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.OperationalIntelligence.Application.Runtime.Features.CreateChaosExperiment;
using NexTraceOne.OperationalIntelligence.Application.Runtime.Features.ListChaosExperiments;

namespace NexTraceOne.OperationalIntelligence.Tests.Runtime.Application;

/// <summary>
/// Testes unitários para as features de Chaos Engineering.
/// </summary>
public sealed class ChaosEngineeringTests
{
    private static readonly DateTimeOffset FixedNow = new(2026, 4, 6, 12, 0, 0, TimeSpan.Zero);
    private readonly IDateTimeProvider _clock = Substitute.For<IDateTimeProvider>();

    public ChaosEngineeringTests()
    {
        _clock.UtcNow.Returns(FixedNow);
    }

    [Fact]
    public async Task CreateChaosExperiment_LatencyInjection_ReturnsValidPlan()
    {
        var handler = new CreateChaosExperiment.Handler(_clock);
        var command = new CreateChaosExperiment.Command(
            "payment-service",
            "Staging",
            "latency-injection",
            null,
            60,
            10m);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.ServiceName.Should().Be("payment-service");
        result.Value.ExperimentType.Should().Be("latency-injection");
        result.Value.Steps.Should().NotBeEmpty();
        result.Value.Steps.Count.Should().Be(5);
        result.Value.CreatedAt.Should().Be(FixedNow);
        result.Value.ExperimentId.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public async Task CreateChaosExperiment_PodKill_ReturnsHighRisk()
    {
        var handler = new CreateChaosExperiment.Handler(_clock);
        var command = new CreateChaosExperiment.Command(
            "order-service",
            "Production",
            "pod-kill",
            "Kill random pod to test resilience",
            120,
            25m);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.RiskLevel.Should().Be("High");
    }

    [Fact]
    public async Task CreateChaosExperiment_NetworkPartition_ReturnsHighRisk()
    {
        var handler = new CreateChaosExperiment.Handler(_clock);
        var command = new CreateChaosExperiment.Command(
            "gateway-service",
            "Staging",
            "network-partition",
            null,
            60,
            10m);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.RiskLevel.Should().Be("High");
    }

    [Fact]
    public async Task CreateChaosExperiment_ValidCommand_ReturnsSafetyChecks()
    {
        var handler = new CreateChaosExperiment.Handler(_clock);
        var command = new CreateChaosExperiment.Command(
            "notification-service",
            "Development",
            "error-injection",
            null,
            60,
            5m);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.SafetyChecks.Should().NotBeEmpty();
        result.Value.SafetyChecks.Should().Contain("Ensure rollback plan exists");
        result.Value.SafetyChecks.Should().Contain("Confirm monitoring alerts active");
    }

    [Fact]
    public void CreateChaosExperiment_Validator_InvalidDuration_TooShort_ShouldFail()
    {
        var validator = new CreateChaosExperiment.Validator();
        var command = new CreateChaosExperiment.Command(
            "test-service",
            "Development",
            "latency-injection",
            null,
            DurationSeconds: 5,
            TargetPercentage: 10m);

        var result = validator.Validate(command);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void CreateChaosExperiment_Validator_InvalidDuration_TooLong_ShouldFail()
    {
        var validator = new CreateChaosExperiment.Validator();
        var command = new CreateChaosExperiment.Command(
            "test-service",
            "Development",
            "latency-injection",
            null,
            DurationSeconds: 9000,
            TargetPercentage: 10m);

        var result = validator.Validate(command);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void CreateChaosExperiment_Validator_EmptyServiceName_ShouldFail()
    {
        var validator = new CreateChaosExperiment.Validator();
        var command = new CreateChaosExperiment.Command(
            "",
            "Development",
            "latency-injection",
            null,
            60,
            10m);

        var result = validator.Validate(command);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void CreateChaosExperiment_Validator_ValidCommand_ShouldPass()
    {
        var validator = new CreateChaosExperiment.Validator();
        var command = new CreateChaosExperiment.Command(
            "payment-service",
            "Staging",
            "latency-injection",
            null,
            60,
            10m);

        var result = validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task ListChaosExperiments_NoFilter_ReturnsStaticDemoList()
    {
        var handler = new ListChaosExperiments.Handler();
        var query = new ListChaosExperiments.Query(null, null);

        var result = await handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().NotBeEmpty();
        result.Value.TotalCount.Should().BeGreaterThan(0);
    }

    [Fact]
    public void ListChaosExperiments_Validator_InvalidPageSize_ShouldFail()
    {
        var validator = new ListChaosExperiments.Validator();
        var query = new ListChaosExperiments.Query(null, null, 1, 0);

        var result = validator.Validate(query);

        result.IsValid.Should().BeFalse();
    }
}
