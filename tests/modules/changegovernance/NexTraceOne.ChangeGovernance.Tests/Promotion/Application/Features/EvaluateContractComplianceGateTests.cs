using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.ChangeGovernance.Application.Promotion.Abstractions;
using NexTraceOne.ChangeGovernance.Domain.Promotion.Entities;
using NexTraceOne.ChangeGovernance.Domain.Promotion.Enums;

using EvaluateContractComplianceGateFeature = NexTraceOne.ChangeGovernance.Application.Promotion.Features.EvaluateContractComplianceGate.EvaluateContractComplianceGate;

namespace NexTraceOne.ChangeGovernance.Tests.Promotion.Application.Features;

/// <summary>
/// Testes do handler EvaluateContractComplianceGate — avaliação do gate de conformidade de contratos.
/// </summary>
public sealed class EvaluateContractComplianceGateTests
{
    private static readonly DateTimeOffset FixedNow = new(2026, 4, 6, 10, 0, 0, TimeSpan.Zero);

    private static DeploymentEnvironment CreateEnvironment(string name = "Production")
        => DeploymentEnvironment.Create(name, "Target environment", 1, true, true, FixedNow);

    private static PromotionRequest CreateRequest(DeploymentEnvironment target)
        => PromotionRequest.Create(Guid.NewGuid(), target.Id, target.Id, "dev@company.com", FixedNow);

    [Fact]
    public async Task Handle_ShouldReturnFalse_WhenNoContractComplianceGateConfigured()
    {
        var env = CreateEnvironment();
        var request = CreateRequest(env);
        var requestRepo = Substitute.For<IPromotionRequestRepository>();
        var gateRepo = Substitute.For<IPromotionGateRepository>();
        var dt = Substitute.For<IDateTimeProvider>();
        dt.UtcNow.Returns(FixedNow);

        requestRepo.GetByIdAsync(Arg.Any<PromotionRequestId>(), Arg.Any<CancellationToken>())
            .Returns(request);
        gateRepo.ListByEnvironmentIdAsync(Arg.Any<DeploymentEnvironmentId>(), Arg.Any<CancellationToken>())
            .Returns(new List<PromotionGate>().AsReadOnly());

        var sut = new EvaluateContractComplianceGateFeature.Handler(requestRepo, gateRepo, dt);
        var query = new EvaluateContractComplianceGateFeature.Query(request.Id.Value, "order-service", "Production");

        var result = await sut.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.HasContractComplianceGate.Should().BeFalse();
        result.Value.ContractComplianceGatePassed.Should().BeTrue();
        result.Value.GateId.Should().BeNull();
    }

    [Fact]
    public async Task Handle_ShouldReturnTrue_WhenContractComplianceGateExists()
    {
        var env = CreateEnvironment();
        var request = CreateRequest(env);
        var gate = PromotionGate.Create(env.Id, "ContractComplianceCheck", "ContractCompliance", true);
        var requestRepo = Substitute.For<IPromotionRequestRepository>();
        var gateRepo = Substitute.For<IPromotionGateRepository>();
        var dt = Substitute.For<IDateTimeProvider>();
        dt.UtcNow.Returns(FixedNow);

        requestRepo.GetByIdAsync(Arg.Any<PromotionRequestId>(), Arg.Any<CancellationToken>())
            .Returns(request);
        gateRepo.ListByEnvironmentIdAsync(Arg.Any<DeploymentEnvironmentId>(), Arg.Any<CancellationToken>())
            .Returns(new List<PromotionGate> { gate }.AsReadOnly());

        var sut = new EvaluateContractComplianceGateFeature.Handler(requestRepo, gateRepo, dt);
        var query = new EvaluateContractComplianceGateFeature.Query(request.Id.Value, "order-service", "Production");

        var result = await sut.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.HasContractComplianceGate.Should().BeTrue();
        result.Value.GateId.Should().Be(gate.Id.Value);
    }

    [Fact]
    public async Task Handle_ShouldReturnError_WhenPromotionRequestNotFound()
    {
        var requestRepo = Substitute.For<IPromotionRequestRepository>();
        var gateRepo = Substitute.For<IPromotionGateRepository>();
        var dt = Substitute.For<IDateTimeProvider>();
        dt.UtcNow.Returns(FixedNow);

        requestRepo.GetByIdAsync(Arg.Any<PromotionRequestId>(), Arg.Any<CancellationToken>())
            .Returns((PromotionRequest?)null);

        var sut = new EvaluateContractComplianceGateFeature.Handler(requestRepo, gateRepo, dt);
        var query = new EvaluateContractComplianceGateFeature.Query(Guid.NewGuid(), "svc", "Production");

        var result = await sut.Handle(query, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_ShouldSetEvaluatedAt()
    {
        var env = CreateEnvironment();
        var request = CreateRequest(env);
        var requestRepo = Substitute.For<IPromotionRequestRepository>();
        var gateRepo = Substitute.For<IPromotionGateRepository>();
        var dt = Substitute.For<IDateTimeProvider>();
        dt.UtcNow.Returns(FixedNow);

        requestRepo.GetByIdAsync(Arg.Any<PromotionRequestId>(), Arg.Any<CancellationToken>())
            .Returns(request);
        gateRepo.ListByEnvironmentIdAsync(Arg.Any<DeploymentEnvironmentId>(), Arg.Any<CancellationToken>())
            .Returns(new List<PromotionGate>().AsReadOnly());

        var sut = new EvaluateContractComplianceGateFeature.Handler(requestRepo, gateRepo, dt);
        var query = new EvaluateContractComplianceGateFeature.Query(request.Id.Value, "svc", "Staging");

        var result = await sut.Handle(query, CancellationToken.None);

        result.Value!.EvaluatedAt.Should().Be(FixedNow);
    }

    [Fact]
    public async Task Handle_ShouldReturnServiceName_InResponse()
    {
        var env = CreateEnvironment();
        var request = CreateRequest(env);
        var requestRepo = Substitute.For<IPromotionRequestRepository>();
        var gateRepo = Substitute.For<IPromotionGateRepository>();
        var dt = Substitute.For<IDateTimeProvider>();
        dt.UtcNow.Returns(FixedNow);

        requestRepo.GetByIdAsync(Arg.Any<PromotionRequestId>(), Arg.Any<CancellationToken>())
            .Returns(request);
        gateRepo.ListByEnvironmentIdAsync(Arg.Any<DeploymentEnvironmentId>(), Arg.Any<CancellationToken>())
            .Returns(new List<PromotionGate>().AsReadOnly());

        var sut = new EvaluateContractComplianceGateFeature.Handler(requestRepo, gateRepo, dt);
        var query = new EvaluateContractComplianceGateFeature.Query(request.Id.Value, "payment-api", "Production");

        var result = await sut.Handle(query, CancellationToken.None);

        result.Value!.ServiceName.Should().Be("payment-api");
    }

    [Fact]
    public async Task Validator_ShouldFail_WhenServiceNameIsEmpty()
    {
        var validator = new EvaluateContractComplianceGateFeature.Validator();
        var vr = await validator.ValidateAsync(
            new EvaluateContractComplianceGateFeature.Query(Guid.NewGuid(), string.Empty, "Prod"));
        vr.IsValid.Should().BeFalse();
    }
}
