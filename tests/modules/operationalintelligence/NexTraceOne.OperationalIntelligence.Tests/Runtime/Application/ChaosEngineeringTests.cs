using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.OperationalIntelligence.Application.Runtime.Abstractions;
using NexTraceOne.OperationalIntelligence.Application.Runtime.Features.CreateChaosExperiment;
using NexTraceOne.OperationalIntelligence.Application.Runtime.Features.ListChaosExperiments;
using NexTraceOne.OperationalIntelligence.Domain.Runtime.Entities;
using NexTraceOne.OperationalIntelligence.Domain.Runtime.Enums;

namespace NexTraceOne.OperationalIntelligence.Tests.Runtime.Application;

/// <summary>
/// Testes unitários para as features de Chaos Engineering.
/// </summary>
public sealed class ChaosEngineeringTests
{
    private static readonly DateTimeOffset FixedNow = new(2026, 4, 6, 12, 0, 0, TimeSpan.Zero);
    private readonly IDateTimeProvider _clock = Substitute.For<IDateTimeProvider>();
    private readonly IChaosExperimentRepository _repository = Substitute.For<IChaosExperimentRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly ICurrentTenant _tenant = Substitute.For<ICurrentTenant>();
    private readonly ICurrentUser _user = Substitute.For<ICurrentUser>();

    public ChaosEngineeringTests()
    {
        _clock.UtcNow.Returns(FixedNow);
        _tenant.Id.Returns(Guid.Parse("00000000-0000-0000-0000-000000000001"));
        _user.Id.Returns("user-1");
    }

    [Fact]
    public async Task CreateChaosExperiment_LatencyInjection_PersistsAndReturnsValidPlan()
    {
        var handler = new CreateChaosExperiment.Handler(_repository, _unitOfWork, _tenant, _user, _clock);
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

        await _repository.Received(1).AddAsync(
            Arg.Is<ChaosExperiment>(e =>
                e.ServiceName == "payment-service" &&
                e.Environment == "Staging" &&
                e.ExperimentType == "latency-injection" &&
                e.TenantId == "00000000-0000-0000-0000-000000000001" &&
                e.Status == ExperimentStatus.Planned &&
                e.RiskLevel == "Low"),
            Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateChaosExperiment_PodKill_ReturnsHighRisk()
    {
        var handler = new CreateChaosExperiment.Handler(_repository, _unitOfWork, _tenant, _user, _clock);
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
        await _repository.Received(1).AddAsync(Arg.Any<ChaosExperiment>(), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateChaosExperiment_NetworkPartition_ReturnsHighRisk()
    {
        var handler = new CreateChaosExperiment.Handler(_repository, _unitOfWork, _tenant, _user, _clock);
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
        var handler = new CreateChaosExperiment.Handler(_repository, _unitOfWork, _tenant, _user, _clock);
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
    public async Task ListChaosExperiments_NoFilter_QueriesRepository()
    {
        var experiments = new List<ChaosExperiment>
        {
            ChaosExperiment.Create("00000000-0000-0000-0000-000000000001", "payment-service", "Production", "latency-injection", null, "Low", 60, 10m,
                new[] { "Step 1" }, new[] { "Check 1" }, FixedNow, "user-1"),
            ChaosExperiment.Create("00000000-0000-0000-0000-000000000001", "order-service", "Staging", "pod-kill", null, "High", 120, 25m,
                new[] { "Step 1" }, new[] { "Check 1" }, FixedNow.AddHours(-1), "user-1"),
        };

        _repository.ListAsync("00000000-0000-0000-0000-000000000001", null, null, null, Arg.Any<CancellationToken>())
            .Returns(experiments);

        var handler = new ListChaosExperiments.Handler(_repository, _tenant);
        var query = new ListChaosExperiments.Query(null, null);

        var result = await handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(2);
        result.Value.TotalCount.Should().Be(2);
        result.Value.Items[0].ServiceName.Should().Be("payment-service");
        result.Value.Items[1].ServiceName.Should().Be("order-service");
    }

    [Fact]
    public async Task ListChaosExperiments_EmptyRepository_ReturnsEmptyList()
    {
        _repository.ListAsync("00000000-0000-0000-0000-000000000001", null, null, null, Arg.Any<CancellationToken>())
            .Returns(new List<ChaosExperiment>());

        var handler = new ListChaosExperiments.Handler(_repository, _tenant);
        var query = new ListChaosExperiments.Query(null, null);

        var result = await handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().BeEmpty();
        result.Value.TotalCount.Should().Be(0);
    }

    [Fact]
    public void ListChaosExperiments_Validator_InvalidPageSize_ShouldFail()
    {
        var validator = new ListChaosExperiments.Validator();
        var query = new ListChaosExperiments.Query(null, null, 1, 0);

        var result = validator.Validate(query);

        result.IsValid.Should().BeFalse();
    }

    // ── Domain entity tests ─────────────────────────────────────────────────

    [Fact]
    public void ChaosExperiment_Create_SetsCorrectProperties()
    {
        var experiment = ChaosExperiment.Create(
            "00000000-0000-0000-0000-000000000001", "svc-a", "Production", "cpu-stress", "Test desc",
            "Medium", 120, 50m, new[] { "s1", "s2" }, new[] { "c1" }, FixedNow, "user-1");

        experiment.TenantId.Should().Be("00000000-0000-0000-0000-000000000001");
        experiment.ServiceName.Should().Be("svc-a");
        experiment.Environment.Should().Be("Production");
        experiment.ExperimentType.Should().Be("cpu-stress");
        experiment.Description.Should().Be("Test desc");
        experiment.RiskLevel.Should().Be("Medium");
        experiment.Status.Should().Be(ExperimentStatus.Planned);
        experiment.DurationSeconds.Should().Be(120);
        experiment.TargetPercentage.Should().Be(50m);
        experiment.Steps.Should().HaveCount(2);
        experiment.SafetyChecks.Should().HaveCount(1);
        experiment.StartedAt.Should().BeNull();
        experiment.CompletedAt.Should().BeNull();
    }

    [Fact]
    public void ChaosExperiment_Start_TransitionsToRunning()
    {
        var experiment = ChaosExperiment.Create(
            "t1", "svc", "Dev", "latency-injection", null,
            "Low", 60, 10m, new[] { "s1" }, new[] { "c1" }, FixedNow, "u1");

        experiment.Start(FixedNow.AddMinutes(5));

        experiment.Status.Should().Be(ExperimentStatus.Running);
        experiment.StartedAt.Should().Be(FixedNow.AddMinutes(5));
    }

    [Fact]
    public void ChaosExperiment_Complete_TransitionsToCompleted()
    {
        var experiment = ChaosExperiment.Create(
            "t1", "svc", "Dev", "latency-injection", null,
            "Low", 60, 10m, new[] { "s1" }, new[] { "c1" }, FixedNow, "u1");

        experiment.Start(FixedNow.AddMinutes(1));
        experiment.Complete(FixedNow.AddMinutes(10), "All good");

        experiment.Status.Should().Be(ExperimentStatus.Completed);
        experiment.CompletedAt.Should().Be(FixedNow.AddMinutes(10));
        experiment.ExecutionNotes.Should().Be("All good");
    }

    [Fact]
    public void ChaosExperiment_Fail_TransitionsToFailed()
    {
        var experiment = ChaosExperiment.Create(
            "t1", "svc", "Dev", "pod-kill", null,
            "High", 60, 10m, new[] { "s1" }, new[] { "c1" }, FixedNow, "u1");

        experiment.Start(FixedNow.AddMinutes(1));
        experiment.Fail(FixedNow.AddMinutes(5), "Service crashed");

        experiment.Status.Should().Be(ExperimentStatus.Failed);
        experiment.ExecutionNotes.Should().Be("Service crashed");
    }

    [Fact]
    public void ChaosExperiment_Cancel_FromPlanned_TransitionsToCancelled()
    {
        var experiment = ChaosExperiment.Create(
            "t1", "svc", "Dev", "pod-kill", null,
            "High", 60, 10m, new[] { "s1" }, new[] { "c1" }, FixedNow, "u1");

        experiment.Cancel(FixedNow.AddMinutes(2), "Changed plans");

        experiment.Status.Should().Be(ExperimentStatus.Cancelled);
    }

    [Fact]
    public void ChaosExperiment_Start_FromRunning_ThrowsInvalidOperation()
    {
        var experiment = ChaosExperiment.Create(
            "t1", "svc", "Dev", "cpu-stress", null,
            "Medium", 60, 10m, new[] { "s1" }, new[] { "c1" }, FixedNow, "u1");

        experiment.Start(FixedNow);

        var act = () => experiment.Start(FixedNow.AddMinutes(1));
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void ChaosExperiment_Cancel_FromCompleted_ThrowsInvalidOperation()
    {
        var experiment = ChaosExperiment.Create(
            "t1", "svc", "Dev", "cpu-stress", null,
            "Medium", 60, 10m, new[] { "s1" }, new[] { "c1" }, FixedNow, "u1");

        experiment.Start(FixedNow);
        experiment.Complete(FixedNow.AddMinutes(5));

        var act = () => experiment.Cancel(FixedNow.AddMinutes(6));
        act.Should().Throw<InvalidOperationException>();
    }
}
