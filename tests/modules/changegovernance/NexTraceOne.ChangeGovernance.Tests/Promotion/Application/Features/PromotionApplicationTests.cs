using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.ChangeGovernance.Application.Promotion.Abstractions;
using NexTraceOne.ChangeGovernance.Domain.Promotion.Entities;
using NexTraceOne.ChangeGovernance.Domain.Promotion.Enums;

using CreatePromotionRequestFeature = NexTraceOne.ChangeGovernance.Application.Promotion.Features.CreatePromotionRequest.CreatePromotionRequest;
using EvaluatePromotionGatesFeature = NexTraceOne.ChangeGovernance.Application.Promotion.Features.EvaluatePromotionGates.EvaluatePromotionGates;
using ApprovePromotionFeature = NexTraceOne.ChangeGovernance.Application.Promotion.Features.ApprovePromotion.ApprovePromotion;
using GetPromotionStatusFeature = NexTraceOne.ChangeGovernance.Application.Promotion.Features.GetPromotionStatus.GetPromotionStatus;

namespace NexTraceOne.ChangeGovernance.Tests.Promotion.Application.Features;

/// <summary>Testes de handlers da camada Application do módulo Promotion.</summary>
public sealed class PromotionApplicationTests
{
    private static readonly DateTimeOffset FixedNow = new(2025, 6, 1, 10, 0, 0, TimeSpan.Zero);

    private static DeploymentEnvironment CreateActiveEnvironment(string name = "Production")
        => DeploymentEnvironment.Create(name, "Desc", 1, true, true, FixedNow);

    private static PromotionRequest CreatePendingRequest(
        DeploymentEnvironment? src = null,
        DeploymentEnvironment? tgt = null)
    {
        var srcEnv = src ?? CreateActiveEnvironment("Staging");
        var tgtEnv = tgt ?? CreateActiveEnvironment("Production");
        return PromotionRequest.Create(Guid.NewGuid(), srcEnv.Id, tgtEnv.Id, "dev@company.com", FixedNow);
    }

    // ── CreatePromotionRequest ────────────────────────────────────────────

    [Fact]
    public async Task CreatePromotionRequest_Handle_WithValidCommand_ShouldCreateRequest()
    {
        var sourceEnv = CreateActiveEnvironment("Staging");
        var targetEnv = CreateActiveEnvironment("Production");
        var requestRepo = Substitute.For<IPromotionRequestRepository>();
        var envRepo = Substitute.For<IDeploymentEnvironmentRepository>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var dateTimeProvider = Substitute.For<IDateTimeProvider>();
        dateTimeProvider.UtcNow.Returns(FixedNow);

        envRepo.GetByIdAsync(Arg.Is<DeploymentEnvironmentId>(id => id.Value == sourceEnv.Id.Value), Arg.Any<CancellationToken>())
            .Returns(sourceEnv);
        envRepo.GetByIdAsync(Arg.Is<DeploymentEnvironmentId>(id => id.Value == targetEnv.Id.Value), Arg.Any<CancellationToken>())
            .Returns(targetEnv);

        var sut = new CreatePromotionRequestFeature.Handler(requestRepo, envRepo, unitOfWork, dateTimeProvider);
        var command = new CreatePromotionRequestFeature.Command(
            Guid.NewGuid(), sourceEnv.Id.Value, targetEnv.Id.Value, "dev@company.com", "Hotfix deployment");

        var result = await sut.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Status.Should().Be(PromotionStatus.Pending.ToString());
        requestRepo.Received(1).Add(Arg.Any<PromotionRequest>());
        await unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreatePromotionRequest_Handle_WithMissingSourceEnv_ShouldFail()
    {
        var requestRepo = Substitute.For<IPromotionRequestRepository>();
        var envRepo = Substitute.For<IDeploymentEnvironmentRepository>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var dateTimeProvider = Substitute.For<IDateTimeProvider>();

        envRepo.GetByIdAsync(Arg.Any<DeploymentEnvironmentId>(), Arg.Any<CancellationToken>())
            .Returns((DeploymentEnvironment?)null);

        var sut = new CreatePromotionRequestFeature.Handler(requestRepo, envRepo, unitOfWork, dateTimeProvider);
        var command = new CreatePromotionRequestFeature.Command(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "dev@company.com", null);

        var result = await sut.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("Environment.NotFound");
    }

    [Fact]
    public void CreatePromotionRequest_Validator_WithEmptyReleaseId_ShouldFail()
    {
        var validator = new CreatePromotionRequestFeature.Validator();
        var command = new CreatePromotionRequestFeature.Command(
            Guid.Empty, Guid.NewGuid(), Guid.NewGuid(), "dev@company.com", null);

        var result = validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "ReleaseId");
    }

    // ── EvaluatePromotionGates ────────────────────────────────────────────

    [Fact]
    public async Task EvaluatePromotionGates_Handle_WithAllGatesPassed_ShouldApprove()
    {
        var srcEnv = CreateActiveEnvironment("Staging");
        var tgtEnv = CreateActiveEnvironment("Production");
        var request = CreatePendingRequest(srcEnv, tgtEnv);
        var gate = PromotionGate.Create(tgtEnv.Id, "AllTestsPassed", "Quality", true);

        var requestRepo = Substitute.For<IPromotionRequestRepository>();
        var gateRepo = Substitute.For<IPromotionGateRepository>();
        var evalRepo = Substitute.For<IGateEvaluationRepository>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var dateTimeProvider = Substitute.For<IDateTimeProvider>();
        dateTimeProvider.UtcNow.Returns(FixedNow);

        requestRepo.GetByIdAsync(Arg.Any<PromotionRequestId>(), Arg.Any<CancellationToken>())
            .Returns(request);
        gateRepo.ListRequiredByEnvironmentIdAsync(Arg.Any<DeploymentEnvironmentId>(), Arg.Any<CancellationToken>())
            .Returns(new List<PromotionGate> { gate });
        gateRepo.ListByEnvironmentIdAsync(Arg.Any<DeploymentEnvironmentId>(), Arg.Any<CancellationToken>())
            .Returns(new List<PromotionGate> { gate });

        var sut = new EvaluatePromotionGatesFeature.Handler(requestRepo, gateRepo, evalRepo, unitOfWork, dateTimeProvider);
        var command = new EvaluatePromotionGatesFeature.Command(
            request.Id.Value,
            "ci@system.com",
            new List<EvaluatePromotionGatesFeature.GateEvaluationInput>
            {
                new(gate.Id.Value, true, "All 342 tests passed")
            });

        var result = await sut.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.AllRequiredPassed.Should().BeTrue();
        result.Value.Status.Should().Be(PromotionStatus.Approved.ToString());
        evalRepo.Received(1).Add(Arg.Any<GateEvaluation>());
        await unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task EvaluatePromotionGates_Handle_WithRequiredGateFailed_ShouldReject()
    {
        var srcEnv = CreateActiveEnvironment("Staging");
        var tgtEnv = CreateActiveEnvironment("Production");
        var request = CreatePendingRequest(srcEnv, tgtEnv);
        var gate = PromotionGate.Create(tgtEnv.Id, "ScanPassed", "Security", true);

        var requestRepo = Substitute.For<IPromotionRequestRepository>();
        var gateRepo = Substitute.For<IPromotionGateRepository>();
        var evalRepo = Substitute.For<IGateEvaluationRepository>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var dateTimeProvider = Substitute.For<IDateTimeProvider>();
        dateTimeProvider.UtcNow.Returns(FixedNow);

        requestRepo.GetByIdAsync(Arg.Any<PromotionRequestId>(), Arg.Any<CancellationToken>())
            .Returns(request);
        gateRepo.ListRequiredByEnvironmentIdAsync(Arg.Any<DeploymentEnvironmentId>(), Arg.Any<CancellationToken>())
            .Returns(new List<PromotionGate> { gate });
        gateRepo.ListByEnvironmentIdAsync(Arg.Any<DeploymentEnvironmentId>(), Arg.Any<CancellationToken>())
            .Returns(new List<PromotionGate> { gate });

        var sut = new EvaluatePromotionGatesFeature.Handler(requestRepo, gateRepo, evalRepo, unitOfWork, dateTimeProvider);
        var command = new EvaluatePromotionGatesFeature.Command(
            request.Id.Value,
            "ci@system.com",
            new List<EvaluatePromotionGatesFeature.GateEvaluationInput>
            {
                new(gate.Id.Value, false, "CVE-2025-1234 detected")
            });

        var result = await sut.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.AllRequiredPassed.Should().BeFalse();
        result.Value.Status.Should().Be(PromotionStatus.Rejected.ToString());
    }

    [Fact]
    public async Task EvaluatePromotionGates_Handle_WithRequestNotFound_ShouldFail()
    {
        var requestRepo = Substitute.For<IPromotionRequestRepository>();
        var gateRepo = Substitute.For<IPromotionGateRepository>();
        var evalRepo = Substitute.For<IGateEvaluationRepository>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var dateTimeProvider = Substitute.For<IDateTimeProvider>();

        requestRepo.GetByIdAsync(Arg.Any<PromotionRequestId>(), Arg.Any<CancellationToken>())
            .Returns((PromotionRequest?)null);

        var sut = new EvaluatePromotionGatesFeature.Handler(requestRepo, gateRepo, evalRepo, unitOfWork, dateTimeProvider);
        var command = new EvaluatePromotionGatesFeature.Command(
            Guid.NewGuid(), "ci@system.com",
            new List<EvaluatePromotionGatesFeature.GateEvaluationInput>());

        var result = await sut.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("Request.NotFound");
    }

    // ── ApprovePromotion ──────────────────────────────────────────────────

    [Fact]
    public async Task ApprovePromotion_Handle_WithInEvaluationAndGatesPassed_ShouldApprove()
    {
        var srcEnv = CreateActiveEnvironment("Staging");
        var tgtEnv = CreateActiveEnvironment("Production");
        var request = CreatePendingRequest(srcEnv, tgtEnv);
        request.StartEvaluation();

        var gate = PromotionGate.Create(tgtEnv.Id, "AllTestsPassed", "Quality", true);
        var evaluation = GateEvaluation.Create(request.Id, gate.Id, true, "ci@system.com", null, FixedNow);

        var requestRepo = Substitute.For<IPromotionRequestRepository>();
        var gateRepo = Substitute.For<IPromotionGateRepository>();
        var evalRepo = Substitute.For<IGateEvaluationRepository>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var dateTimeProvider = Substitute.For<IDateTimeProvider>();
        dateTimeProvider.UtcNow.Returns(FixedNow);

        requestRepo.GetByIdAsync(Arg.Any<PromotionRequestId>(), Arg.Any<CancellationToken>())
            .Returns(request);
        gateRepo.ListRequiredByEnvironmentIdAsync(Arg.Any<DeploymentEnvironmentId>(), Arg.Any<CancellationToken>())
            .Returns(new List<PromotionGate> { gate });
        evalRepo.ListByRequestIdAsync(Arg.Any<PromotionRequestId>(), Arg.Any<CancellationToken>())
            .Returns(new List<GateEvaluation> { evaluation });

        var sut = new ApprovePromotionFeature.Handler(requestRepo, gateRepo, evalRepo, unitOfWork, dateTimeProvider);
        var command = new ApprovePromotionFeature.Command(request.Id.Value, "lead@company.com", "LGTM");

        var result = await sut.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Status.Should().Be(PromotionStatus.Approved.ToString());
        result.Value.CompletedAt.Should().Be(FixedNow);
        await unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ApprovePromotion_Handle_WithRequiredGateNotPassed_ShouldFail()
    {
        var srcEnv = CreateActiveEnvironment("Staging");
        var tgtEnv = CreateActiveEnvironment("Production");
        var request = CreatePendingRequest(srcEnv, tgtEnv);
        request.StartEvaluation();

        var gate = PromotionGate.Create(tgtEnv.Id, "ScanPassed", "Security", true);

        var requestRepo = Substitute.For<IPromotionRequestRepository>();
        var gateRepo = Substitute.For<IPromotionGateRepository>();
        var evalRepo = Substitute.For<IGateEvaluationRepository>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var dateTimeProvider = Substitute.For<IDateTimeProvider>();

        requestRepo.GetByIdAsync(Arg.Any<PromotionRequestId>(), Arg.Any<CancellationToken>())
            .Returns(request);
        gateRepo.ListRequiredByEnvironmentIdAsync(Arg.Any<DeploymentEnvironmentId>(), Arg.Any<CancellationToken>())
            .Returns(new List<PromotionGate> { gate });
        evalRepo.ListByRequestIdAsync(Arg.Any<PromotionRequestId>(), Arg.Any<CancellationToken>())
            .Returns(new List<GateEvaluation>());

        var sut = new ApprovePromotionFeature.Handler(requestRepo, gateRepo, evalRepo, unitOfWork, dateTimeProvider);
        var command = new ApprovePromotionFeature.Command(request.Id.Value, "lead@company.com", null);

        var result = await sut.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("Gate.NotPassed");
    }

    // ── GetPromotionStatus ────────────────────────────────────────────────

    [Fact]
    public async Task GetPromotionStatus_Handle_WithValidRequest_ShouldReturnStatus()
    {
        var request = CreatePendingRequest();
        var requestRepo = Substitute.For<IPromotionRequestRepository>();
        var evalRepo = Substitute.For<IGateEvaluationRepository>();

        requestRepo.GetByIdAsync(Arg.Any<PromotionRequestId>(), Arg.Any<CancellationToken>())
            .Returns(request);
        evalRepo.ListByRequestIdAsync(Arg.Any<PromotionRequestId>(), Arg.Any<CancellationToken>())
            .Returns(new List<GateEvaluation>());

        var sut = new GetPromotionStatusFeature.Handler(requestRepo, evalRepo);
        var query = new GetPromotionStatusFeature.Query(request.Id.Value);

        var result = await sut.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Status.Should().Be(PromotionStatus.Pending.ToString());
        result.Value.TotalEvaluations.Should().Be(0);
    }

    [Fact]
    public async Task GetPromotionStatus_Handle_WithNotFoundRequest_ShouldFail()
    {
        var requestRepo = Substitute.For<IPromotionRequestRepository>();
        var evalRepo = Substitute.For<IGateEvaluationRepository>();

        requestRepo.GetByIdAsync(Arg.Any<PromotionRequestId>(), Arg.Any<CancellationToken>())
            .Returns((PromotionRequest?)null);

        var sut = new GetPromotionStatusFeature.Handler(requestRepo, evalRepo);
        var query = new GetPromotionStatusFeature.Query(Guid.NewGuid());

        var result = await sut.Handle(query, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("Request.NotFound");
    }
}
