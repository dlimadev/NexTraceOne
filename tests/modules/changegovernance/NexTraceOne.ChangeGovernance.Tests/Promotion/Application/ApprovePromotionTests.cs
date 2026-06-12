using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.ChangeGovernance.Application.Promotion.Abstractions;
using NexTraceOne.ChangeGovernance.Application.Promotion.Features.ApprovePromotion;
using NexTraceOne.ChangeGovernance.Contracts.IntegrationEvents;
using NexTraceOne.ChangeGovernance.Domain.Promotion.Entities;

using IReleaseRepository = NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Abstractions.IReleaseRepository;

namespace NexTraceOne.ChangeGovernance.Tests.Promotion.Application;

/// <summary>
/// Testes de unidade para a feature ApprovePromotion.
/// Cobre cenários de aprovação com e sem gates obrigatórios.
/// </summary>
public sealed class ApprovePromotionTests
{
    private static readonly DateTimeOffset FixedNow = new(2025, 6, 1, 10, 0, 0, TimeSpan.Zero);

    private readonly IPromotionRequestRepository _requestRepository =
        Substitute.For<IPromotionRequestRepository>();
    private readonly IPromotionGateRepository _gateRepository =
        Substitute.For<IPromotionGateRepository>();
    private readonly IGateEvaluationRepository _evaluationRepository =
        Substitute.For<IGateEvaluationRepository>();
    private readonly IReleaseRepository _releaseRepository =
        Substitute.For<IReleaseRepository>();
    private readonly IDeploymentEnvironmentRepository _environmentRepository =
        Substitute.For<IDeploymentEnvironmentRepository>();
    private readonly IPromotionUnitOfWork _unitOfWork =
        Substitute.For<IPromotionUnitOfWork>();
    private readonly IDateTimeProvider _dateTimeProvider =
        Substitute.For<IDateTimeProvider>();
    private readonly IEventBus _eventBus =
        Substitute.For<IEventBus>();

    public ApprovePromotionTests()
    {
        _dateTimeProvider.UtcNow.Returns(FixedNow);
    }

    private ApprovePromotion.Handler CreateHandler()
        => new(_requestRepository, _gateRepository, _evaluationRepository, _releaseRepository,
            _environmentRepository, _unitOfWork, _dateTimeProvider, _eventBus);

    private static DeploymentEnvironment CreateActiveEnvironment(string name = "Production")
        => DeploymentEnvironment.Create(name, "Deployment environment", 1, true, true, FixedNow);

    private static PromotionRequest CreateInEvaluationRequest(
        DeploymentEnvironment? src = null,
        DeploymentEnvironment? tgt = null)
    {
        var srcEnv = src ?? CreateActiveEnvironment("Staging");
        var tgtEnv = tgt ?? CreateActiveEnvironment("Production");
        var request = PromotionRequest.Create(Guid.NewGuid(), Guid.NewGuid(), srcEnv.Id, tgtEnv.Id, "dev@company.com", FixedNow);
        request.StartEvaluation();
        return request;
    }

    // ── Test 1: No required gates → approve immediately ───────────────────────

    [Fact]
    public async Task ApprovePromotion_ValidRequest_NoRequiredGates_ShouldSucceed()
    {
        // Arrange
        var request = CreateInEvaluationRequest();

        _requestRepository.GetByIdAsync(Arg.Any<PromotionRequestId>(), Arg.Any<CancellationToken>())
            .Returns(request);
        _gateRepository.ListRequiredByEnvironmentIdAsync(Arg.Any<DeploymentEnvironmentId>(), Arg.Any<CancellationToken>())
            .Returns(new List<PromotionGate>());
        _unitOfWork.CommitAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(1));

        var handler = CreateHandler();
        var command = new ApprovePromotion.Command(request.Id.Value, "lead@company.com", "Approved for release");

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.PromotionRequestId.Should().Be(request.Id.Value);
        result.Value.Status.Should().Be("Approved");
        result.Value.CompletedAt.Should().Be(FixedNow);

        _requestRepository.Received(1).Update(Arg.Any<PromotionRequest>());
        await _eventBus.Received(1).PublishAsync(
            Arg.Any<PromotionCompletedIntegrationEvent>(), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    // ── Test 2: Request not found → NotFound error ────────────────────────────

    [Fact]
    public async Task ApprovePromotion_RequestNotFound_ShouldReturnNotFoundError()
    {
        // Arrange
        _requestRepository.GetByIdAsync(Arg.Any<PromotionRequestId>(), Arg.Any<CancellationToken>())
            .Returns((PromotionRequest?)null);

        var handler = CreateHandler();
        var command = new ApprovePromotion.Command(Guid.NewGuid(), "lead@company.com", null);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Contain("Request.NotFound");

        _requestRepository.DidNotReceive().Update(Arg.Any<PromotionRequest>());
        await _unitOfWork.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
    }

    // ── Test 3: Required gate not passed → gate error ─────────────────────────

    [Fact]
    public async Task ApprovePromotion_RequiredGateNotPassed_ShouldReturnGateError()
    {
        // Arrange
        var tgtEnv = CreateActiveEnvironment("Production");
        var request = CreateInEvaluationRequest(tgt: tgtEnv);
        var requiredGate = PromotionGate.Create(tgtEnv.Id, "SecurityScan", "Security", isRequired: true);

        _requestRepository.GetByIdAsync(Arg.Any<PromotionRequestId>(), Arg.Any<CancellationToken>())
            .Returns(request);
        _gateRepository.ListRequiredByEnvironmentIdAsync(Arg.Any<DeploymentEnvironmentId>(), Arg.Any<CancellationToken>())
            .Returns(new List<PromotionGate> { requiredGate });
        // No evaluations → gate has never been evaluated (not passed)
        _evaluationRepository.ListByRequestIdAsync(Arg.Any<PromotionRequestId>(), Arg.Any<CancellationToken>())
            .Returns(new List<GateEvaluation>());

        var handler = CreateHandler();
        var command = new ApprovePromotion.Command(request.Id.Value, "lead@company.com", null);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Contain("Gate.NotPassed");

        _requestRepository.DidNotReceive().Update(Arg.Any<PromotionRequest>());
        await _unitOfWork.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
    }

    // ── Test 4: All gates passed → approve successfully ───────────────────────

    [Fact]
    public async Task ApprovePromotion_AllGatesPassed_ShouldApproveSuccessfully()
    {
        // Arrange
        var tgtEnv = CreateActiveEnvironment("Production");
        var request = CreateInEvaluationRequest(tgt: tgtEnv);
        var gate1 = PromotionGate.Create(tgtEnv.Id, "AllTestsPassed", "Quality", isRequired: true);
        var gate2 = PromotionGate.Create(tgtEnv.Id, "SecurityScan", "Security", isRequired: true);

        var eval1 = GateEvaluation.Create(request.Id, gate1.Id, passed: true, "ci@system", null, FixedNow);
        var eval2 = GateEvaluation.Create(request.Id, gate2.Id, passed: true, "ci@system", null, FixedNow);

        _requestRepository.GetByIdAsync(Arg.Any<PromotionRequestId>(), Arg.Any<CancellationToken>())
            .Returns(request);
        _gateRepository.ListRequiredByEnvironmentIdAsync(Arg.Any<DeploymentEnvironmentId>(), Arg.Any<CancellationToken>())
            .Returns(new List<PromotionGate> { gate1, gate2 });
        _evaluationRepository.ListByRequestIdAsync(Arg.Any<PromotionRequestId>(), Arg.Any<CancellationToken>())
            .Returns(new List<GateEvaluation> { eval1, eval2 });
        _unitOfWork.CommitAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(1));

        var handler = CreateHandler();
        var command = new ApprovePromotion.Command(request.Id.Value, "lead@company.com", "Both gates verified");

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Status.Should().Be("Approved");
        result.Value.CompletedAt.Should().Be(FixedNow);

        _requestRepository.Received(1).Update(Arg.Any<PromotionRequest>());
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }
}
