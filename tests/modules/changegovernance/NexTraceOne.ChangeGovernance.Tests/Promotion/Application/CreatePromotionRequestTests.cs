using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.ChangeGovernance.Application.Promotion.Abstractions;
using NexTraceOne.ChangeGovernance.Application.Promotion.Features.CreatePromotionRequest;
using NexTraceOne.ChangeGovernance.Domain.Promotion.Entities;

namespace NexTraceOne.ChangeGovernance.Tests.Promotion.Application;

/// <summary>
/// Testes de unidade para a feature CreatePromotionRequest.
/// Cobre criação com sucesso, ambiente não encontrado e ambiente inativo.
/// </summary>
public sealed class CreatePromotionRequestTests
{
    private static readonly DateTimeOffset FixedNow = new(2025, 6, 1, 10, 0, 0, TimeSpan.Zero);

    private readonly IPromotionRequestRepository _requestRepository =
        Substitute.For<IPromotionRequestRepository>();
    private readonly IDeploymentEnvironmentRepository _environmentRepository =
        Substitute.For<IDeploymentEnvironmentRepository>();
    private readonly IPromotionUnitOfWork _unitOfWork =
        Substitute.For<IPromotionUnitOfWork>();
    private readonly IDateTimeProvider _dateTimeProvider =
        Substitute.For<IDateTimeProvider>();

    public CreatePromotionRequestTests()
    {
        _dateTimeProvider.UtcNow.Returns(FixedNow);
    }

    private CreatePromotionRequest.Handler CreateHandler()
        => new(_requestRepository, _environmentRepository, _unitOfWork, _dateTimeProvider);

    private static DeploymentEnvironment CreateActiveEnvironment(string name = "Staging")
        => DeploymentEnvironment.Create(name, "Deployment environment", 0, false, false, FixedNow);

    private static DeploymentEnvironment CreateInactiveEnvironment(string name = "Production")
    {
        var env = DeploymentEnvironment.Create(name, "Deployment environment", 1, true, false, FixedNow);
        env.Deactivate();
        return env;
    }

    // ── Test 1: Valid environments → request created ──────────────────────────

    [Fact]
    public async Task CreatePromotionRequest_ValidEnvironments_ShouldSucceed()
    {
        // Arrange
        var srcEnv = CreateActiveEnvironment("Development");
        var tgtEnv = CreateActiveEnvironment("Staging");

        _environmentRepository.GetByIdAsync(
                Arg.Is<DeploymentEnvironmentId>(id => id.Value == srcEnv.Id.Value),
                Arg.Any<CancellationToken>())
            .Returns(srcEnv);
        _environmentRepository.GetByIdAsync(
                Arg.Is<DeploymentEnvironmentId>(id => id.Value == tgtEnv.Id.Value),
                Arg.Any<CancellationToken>())
            .Returns(tgtEnv);
        _unitOfWork.CommitAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(1));

        var handler = CreateHandler();
        var command = new CreatePromotionRequest.Command(
            ReleaseId: Guid.NewGuid(),
            SourceEnvironmentId: srcEnv.Id.Value,
            TargetEnvironmentId: tgtEnv.Id.Value,
            RequestedBy: "dev@company.com",
            Justification: "Deploying hotfix 1.2.3");

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.PromotionRequestId.Should().NotBeEmpty();
        result.Value.Status.Should().Be("Pending");

        _requestRepository.Received(1).Add(Arg.Any<PromotionRequest>());
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    // ── Test 2: Source environment not found → error ──────────────────────────

    [Fact]
    public async Task CreatePromotionRequest_SourceEnvironmentNotFound_ShouldReturnError()
    {
        // Arrange
        var tgtEnv = CreateActiveEnvironment("Staging");

        // Source environment returns null, target exists
        _environmentRepository.GetByIdAsync(Arg.Any<DeploymentEnvironmentId>(), Arg.Any<CancellationToken>())
            .Returns((DeploymentEnvironment?)null);

        var handler = CreateHandler();
        var command = new CreatePromotionRequest.Command(
            ReleaseId: Guid.NewGuid(),
            SourceEnvironmentId: Guid.NewGuid(),
            TargetEnvironmentId: tgtEnv.Id.Value,
            RequestedBy: "dev@company.com",
            Justification: null);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Contain("Environment.NotFound");

        _requestRepository.DidNotReceive().Add(Arg.Any<PromotionRequest>());
        await _unitOfWork.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
    }

    // ── Test 3: Target environment inactive → error ───────────────────────────

    [Fact]
    public async Task CreatePromotionRequest_TargetEnvironmentInactive_ShouldReturnError()
    {
        // Arrange
        var srcEnv = CreateActiveEnvironment("Development");
        var inactiveTgtEnv = CreateInactiveEnvironment("Production");

        _environmentRepository.GetByIdAsync(
                Arg.Is<DeploymentEnvironmentId>(id => id.Value == srcEnv.Id.Value),
                Arg.Any<CancellationToken>())
            .Returns(srcEnv);
        _environmentRepository.GetByIdAsync(
                Arg.Is<DeploymentEnvironmentId>(id => id.Value == inactiveTgtEnv.Id.Value),
                Arg.Any<CancellationToken>())
            .Returns(inactiveTgtEnv);

        var handler = CreateHandler();
        var command = new CreatePromotionRequest.Command(
            ReleaseId: Guid.NewGuid(),
            SourceEnvironmentId: srcEnv.Id.Value,
            TargetEnvironmentId: inactiveTgtEnv.Id.Value,
            RequestedBy: "dev@company.com",
            Justification: null);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Contain("Environment.NotFound");

        _requestRepository.DidNotReceive().Add(Arg.Any<PromotionRequest>());
        await _unitOfWork.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
    }
}
