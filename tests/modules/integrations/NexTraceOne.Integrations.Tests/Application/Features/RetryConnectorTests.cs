using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.Integrations.Application.Abstractions;
using NexTraceOne.Integrations.Application.Features.RetryConnector;
using NexTraceOne.Integrations.Domain.Entities;

namespace NexTraceOne.Integrations.Tests.Application.Features;

/// <summary>
/// Testes de unidade para a feature RetryConnector.
/// Verifica comportamento do handler em cenários de sucesso e falha.
/// </summary>
public sealed class RetryConnectorTests
{
    private static readonly DateTimeOffset FixedNow = new(2025, 6, 1, 10, 0, 0, TimeSpan.Zero);

    private readonly IIntegrationConnectorRepository _connectorRepository =
        Substitute.For<IIntegrationConnectorRepository>();
    private readonly IIngestionExecutionRepository _executionRepository =
        Substitute.For<IIngestionExecutionRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IDateTimeProvider _clock = Substitute.For<IDateTimeProvider>();

    public RetryConnectorTests()
    {
        _clock.UtcNow.Returns(FixedNow);
    }

    private RetryConnector.Handler CreateHandler()
        => new(_connectorRepository, _executionRepository, _unitOfWork, _clock);

    private static IntegrationConnector CreateConnector()
        => IntegrationConnector.Create(
            "github-cicd", "CI/CD", null, "GitHub",
            "https://api.github.com", null, null, null, null, FixedNow);

    // ── Test 1: Happy path ────────────────────────────────────────────────────

    [Fact]
    public async Task RetryConnector_ValidConnectorId_ShouldCreateExecutionAndReturnQueued()
    {
        // Arrange
        var connector = CreateConnector();
        _connectorRepository.GetByIdAsync(
                Arg.Is<IntegrationConnectorId>(id => id.Value == connector.Id.Value),
                Arg.Any<CancellationToken>())
            .Returns(connector);
        _unitOfWork.CommitAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(1));

        var handler = CreateHandler();
        var command = new RetryConnector.Command(connector.Id.Value.ToString());

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Status.Should().Be("Queued");
        result.Value.ConnectorId.Should().Be(connector.Id.Value.ToString());
        result.Value.RetryRequestId.Should().NotBeEmpty();
        result.Value.RequestedAt.Should().Be(FixedNow);

        await _executionRepository.Received(1).AddAsync(Arg.Any<IngestionExecution>(), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    // ── Test 2: Connector not found ───────────────────────────────────────────

    [Fact]
    public async Task RetryConnector_ConnectorNotFound_ShouldReturnNotFoundError()
    {
        // Arrange
        var unknownId = Guid.NewGuid();
        _connectorRepository.GetByIdAsync(Arg.Any<IntegrationConnectorId>(), Arg.Any<CancellationToken>())
            .Returns((IntegrationConnector?)null);

        var handler = CreateHandler();
        var command = new RetryConnector.Command(unknownId.ToString());

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("CONNECTOR_NOT_FOUND");
        await _executionRepository.DidNotReceive().AddAsync(Arg.Any<IngestionExecution>(), Arg.Any<CancellationToken>());
        await _unitOfWork.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
    }

    // ── Test 3: Invalid connector ID format ───────────────────────────────────

    [Fact]
    public async Task RetryConnector_InvalidConnectorIdFormat_ShouldReturnValidationError()
    {
        // Arrange
        var handler = CreateHandler();
        var command = new RetryConnector.Command("not-a-valid-guid");

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("INVALID_CONNECTOR_ID");
        await _connectorRepository.DidNotReceive().GetByIdAsync(Arg.Any<IntegrationConnectorId>(), Arg.Any<CancellationToken>());
        await _unitOfWork.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
    }
}
