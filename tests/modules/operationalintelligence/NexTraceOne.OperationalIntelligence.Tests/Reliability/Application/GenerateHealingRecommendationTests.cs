using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.OperationalIntelligence.Application.Reliability.Abstractions;
using NexTraceOne.OperationalIntelligence.Application.Reliability.Features.GenerateHealingRecommendation;
using NexTraceOne.OperationalIntelligence.Domain.Reliability.Entities;
using NexTraceOne.OperationalIntelligence.Domain.Reliability.Enums;

namespace NexTraceOne.OperationalIntelligence.Tests.Reliability.Application;

/// <summary>Testes do handler GenerateHealingRecommendation com mocks.</summary>
public sealed class GenerateHealingRecommendationTests
{
    private static readonly DateTimeOffset FixedNow = new(2026, 3, 1, 10, 0, 0, TimeSpan.Zero);
    private static readonly Guid TenantId = Guid.NewGuid();

    private readonly IHealingRecommendationRepository _repository = Substitute.For<IHealingRecommendationRepository>();
    private readonly ICurrentTenant _currentTenant = Substitute.For<ICurrentTenant>();
    private readonly IDateTimeProvider _dateTimeProvider = Substitute.For<IDateTimeProvider>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    public GenerateHealingRecommendationTests()
    {
        _currentTenant.Id.Returns(TenantId);
        _dateTimeProvider.UtcNow.Returns(FixedNow);
        _unitOfWork.CommitAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult(1));
    }

    [Fact]
    public async Task Handle_WithValidCommand_ShouldReturnSuccess()
    {
        var handler = CreateHandler();
        var command = new GenerateHealingRecommendation.Command(
            "order-service",
            "production",
            Guid.NewGuid(),
            "Memory leak after deploy",
            "Restart",
            "{\"target\":\"pod-abc\"}",
            85,
            "{\"downtime\":\"~30s\"}",
            "[\"runbook-001\"]",
            92.5m);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.ServiceName.Should().Be("order-service");
        result.Value.Environment.Should().Be("production");
        result.Value.ActionType.Should().Be("Restart");
        result.Value.ConfidenceScore.Should().Be(85);
        result.Value.Status.Should().Be("Proposed");
        result.Value.GeneratedAt.Should().Be(FixedNow);
        result.Value.RecommendationId.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public async Task Handle_WithValidCommand_ShouldPersistRecommendation()
    {
        var handler = CreateHandler();
        var command = new GenerateHealingRecommendation.Command(
            "order-service",
            "production",
            null,
            "High CPU usage",
            "Scale",
            "{\"replicas\":3}",
            70);

        await handler.Handle(command, CancellationToken.None);

        _repository.Received(1).Add(Arg.Is<HealingRecommendation>(r =>
            r.ServiceName == "order-service" &&
            r.ActionType == HealingActionType.Scale &&
            r.Status == HealingRecommendationStatus.Proposed));

        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithInvalidActionType_ShouldReturnError()
    {
        var handler = CreateHandler();
        var command = new GenerateHealingRecommendation.Command(
            "order-service",
            "production",
            null,
            "Root cause",
            "InvalidAction",
            "{}",
            50);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("INVALID_ACTION_TYPE");
    }

    [Fact]
    public async Task Handle_WithAllActionTypes_ShouldSucceed()
    {
        var handler = CreateHandler();

        foreach (var actionType in Enum.GetNames<HealingActionType>())
        {
            var command = new GenerateHealingRecommendation.Command(
                "svc",
                "prod",
                null,
                "Root cause",
                actionType,
                "{}",
                50);

            var result = await handler.Handle(command, CancellationToken.None);
            result.IsSuccess.Should().BeTrue($"ActionType '{actionType}' should be valid");
        }
    }

    [Fact]
    public async Task Handle_ShouldUseTenantIdFromCurrentTenant()
    {
        var handler = CreateHandler();
        var command = new GenerateHealingRecommendation.Command(
            "svc", "prod", null, "Root cause", "Restart", "{}", 50);

        await handler.Handle(command, CancellationToken.None);

        _repository.Received(1).Add(Arg.Is<HealingRecommendation>(r =>
            r.TenantId == TenantId));
    }

    private GenerateHealingRecommendation.Handler CreateHandler()
        => new(_repository, _currentTenant, _dateTimeProvider, _unitOfWork);
}
