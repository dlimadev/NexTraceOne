using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.Integrations.Application.Features.GetIngestionFreshness;
using NexTraceOne.Integrations.Application.Features.GetIngestionHealth;
using NexTraceOne.Integrations.Application.Features.GetIntegrationConnector;
using NexTraceOne.Integrations.Application.Features.ListIngestionExecutions;
using NexTraceOne.Integrations.Application.Features.ListIngestionSources;
using NexTraceOne.Integrations.Application.Features.ListIntegrationConnectors;
using NexTraceOne.Integrations.Application.Features.ReprocessExecution;
using NexTraceOne.Integrations.Application.Features.RetryConnector;
using NexTraceOne.Integrations.Application.Abstractions;
using NexTraceOne.Integrations.Domain.Entities;
using NexTraceOne.Integrations.Domain.Enums;
using MediatR;
using NSubstitute;
using NexTraceOne.BuildingBlocks.Core.Results;
using ProcessIngestionPayloadFeature = NexTraceOne.Integrations.Application.Features.ProcessIngestionPayload.ProcessIngestionPayload;

namespace NexTraceOne.Integrations.Tests.Application.Features;

/// <summary>
/// Testes de unidade para as features do Integration Hub.
/// Utilizam mocks dos repositórios para verificar comportamentos dos handlers.
/// </summary>
public sealed class IntegrationHubFeatureTests
{
    private readonly IIntegrationConnectorRepository _connectorRepository = Substitute.For<IIntegrationConnectorRepository>();
    private readonly IIngestionSourceRepository _sourceRepository = Substitute.For<IIngestionSourceRepository>();
    private readonly IIngestionExecutionRepository _executionRepository = Substitute.For<IIngestionExecutionRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IDateTimeProvider _clock = Substitute.For<IDateTimeProvider>();
    private readonly ISender _sender = Substitute.For<ISender>();

    public IntegrationHubFeatureTests()
    {
        _clock.UtcNow.Returns(DateTimeOffset.UtcNow);
    }

    // ── ListIntegrationConnectors ──

    [Fact]
    public async Task ListConnectors_WithData_ShouldReturnItems()
    {
        // Arrange
        var connectors = CreateTestConnectors(3);
        _connectorRepository.ListAsync(
            Arg.Any<ConnectorStatus?>(),
            Arg.Any<ConnectorHealth?>(),
            Arg.Any<string?>(),
            Arg.Any<string?>(),
            Arg.Any<CancellationToken>())
            .Returns(connectors);

        var handler = new ListIntegrationConnectors.Handler(_connectorRepository);
        var query = new ListIntegrationConnectors.Query();

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(3);
    }

    [Fact]
    public async Task ListConnectors_EmptyDatabase_ShouldReturnEmptyList()
    {
        // Arrange
        _connectorRepository.ListAsync(
            Arg.Any<ConnectorStatus?>(),
            Arg.Any<ConnectorHealth?>(),
            Arg.Any<string?>(),
            Arg.Any<string?>(),
            Arg.Any<CancellationToken>())
            .Returns(new List<IntegrationConnector>());

        var handler = new ListIntegrationConnectors.Handler(_connectorRepository);
        var query = new ListIntegrationConnectors.Query();

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().BeEmpty();
    }

    // ── GetIntegrationConnector ──

    [Fact]
    public async Task GetConnector_ValidId_ShouldReturnConnector()
    {
        // Arrange
        var connectorId = Guid.NewGuid();
        var connector = IntegrationConnector.Create(
            name: "github-cicd",
            connectorType: "CI/CD",
            description: "GitHub CI/CD",
            provider: "GitHub",
            endpoint: "https://api.github.com",
            environment: null,
            authenticationMode: null,
            pollingMode: null,
            allowedTeams: null,
            utcNow: DateTimeOffset.UtcNow);

        _connectorRepository.GetByIdAsync(Arg.Any<IntegrationConnectorId>(), Arg.Any<CancellationToken>())
            .Returns(connector);
        _executionRepository.ListByConnectorIdAsync(Arg.Any<IntegrationConnectorId>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(new List<IngestionExecution>());
        _sourceRepository.ListByConnectorIdAsync(Arg.Any<IntegrationConnectorId>(), Arg.Any<CancellationToken>())
            .Returns(new List<IngestionSource>());

        var handler = new GetIntegrationConnector.Handler(_connectorRepository, _executionRepository, _sourceRepository);
        var query = new GetIntegrationConnector.Query(connectorId.ToString());

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Name.Should().Be("github-cicd");
        result.Value.Provider.Should().Be("GitHub");
    }

    [Fact]
    public async Task GetConnector_InvalidGuidFormat_ShouldReturnValidationError()
    {
        // Arrange
        var handler = new GetIntegrationConnector.Handler(_connectorRepository, _executionRepository, _sourceRepository);
        var query = new GetIntegrationConnector.Query("not-a-guid");

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("INVALID_CONNECTOR_ID");
    }

    [Fact]
    public async Task GetConnector_NotFound_ShouldReturnNotFoundError()
    {
        // Arrange
        _connectorRepository.GetByIdAsync(Arg.Any<IntegrationConnectorId>(), Arg.Any<CancellationToken>())
            .Returns((IntegrationConnector?)null);

        var handler = new GetIntegrationConnector.Handler(_connectorRepository, _executionRepository, _sourceRepository);
        var query = new GetIntegrationConnector.Query(Guid.NewGuid().ToString());

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("CONNECTOR_NOT_FOUND");
    }

    // ── ListIngestionSources ──

    [Fact]
    public async Task ListSources_WithData_ShouldReturnItems()
    {
        // Arrange
        var connector = IntegrationConnector.Create("test", "CI/CD", null, "Test", null, null, null, null, null, DateTimeOffset.UtcNow);
        var sources = new List<IngestionSource>
        {
            IngestionSource.Create(connector.Id, "Webhook", "Webhook", null, null, null, 30, DateTimeOffset.UtcNow),
            IngestionSource.Create(connector.Id, "Polling", "API Polling", null, null, null, 60, DateTimeOffset.UtcNow)
        };

        _sourceRepository.ListAsync(
            Arg.Any<IntegrationConnectorId?>(),
            Arg.Any<SourceStatus?>(),
            Arg.Any<FreshnessStatus?>(),
            Arg.Any<CancellationToken>())
            .Returns(sources);
        _connectorRepository.GetByIdAsync(Arg.Any<IntegrationConnectorId>(), Arg.Any<CancellationToken>())
            .Returns(connector);

        var handler = new ListIngestionSources.Handler(_sourceRepository, _connectorRepository);
        var query = new ListIngestionSources.Query();

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(2);
    }

    // ── ListIngestionExecutions ──

    [Fact]
    public async Task ListExecutions_WithData_ShouldReturnItems()
    {
        // Arrange
        var connector = IntegrationConnector.Create("test", "CI/CD", null, "Test", null, null, null, null, null, DateTimeOffset.UtcNow);
        var executions = new List<IngestionExecution>
        {
            IngestionExecution.Start(connector.Id, null, "corr-1", DateTimeOffset.UtcNow),
            IngestionExecution.Start(connector.Id, null, "corr-2", DateTimeOffset.UtcNow)
        };

        _executionRepository.ListAsync(
            Arg.Any<IntegrationConnectorId?>(),
            Arg.Any<IngestionSourceId?>(),
            Arg.Any<ExecutionResult?>(),
            Arg.Any<DateTimeOffset?>(),
            Arg.Any<DateTimeOffset?>(),
            Arg.Any<int>(),
            Arg.Any<int>(),
            Arg.Any<CancellationToken>())
            .Returns(executions);
        _executionRepository.CountAsync(
            Arg.Any<IntegrationConnectorId?>(),
            Arg.Any<IngestionSourceId?>(),
            Arg.Any<ExecutionResult?>(),
            Arg.Any<DateTimeOffset?>(),
            Arg.Any<DateTimeOffset?>(),
            Arg.Any<CancellationToken>())
            .Returns(2);
        _connectorRepository.GetByIdAsync(Arg.Any<IntegrationConnectorId>(), Arg.Any<CancellationToken>())
            .Returns(connector);

        var handler = new ListIngestionExecutions.Handler(_executionRepository, _connectorRepository);
        var query = new ListIngestionExecutions.Query();

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(2);
    }

    // ── GetIngestionHealth ──

    [Fact]
    public async Task GetHealth_ShouldReturnValidHealthSummary()
    {
        // Arrange
        var connectors = CreateTestConnectors(3);
        _connectorRepository.ListAsync(
            Arg.Any<ConnectorStatus?>(),
            Arg.Any<ConnectorHealth?>(),
            Arg.Any<string?>(),
            Arg.Any<string?>(),
            Arg.Any<CancellationToken>())
            .Returns(connectors);
        _connectorRepository.CountByHealthAsync(ConnectorHealth.Healthy, Arg.Any<CancellationToken>())
            .Returns(2);
        _connectorRepository.CountByHealthAsync(ConnectorHealth.Degraded, Arg.Any<CancellationToken>())
            .Returns(1);
        _connectorRepository.CountByHealthAsync(ConnectorHealth.Unhealthy, Arg.Any<CancellationToken>())
            .Returns(0);

        var connector = connectors[0];
        var sources = new List<IngestionSource>
        {
            IngestionSource.Create(connector.Id, "test", "Webhook", null, null, null, 30, DateTimeOffset.UtcNow)
        };
        _sourceRepository.ListAsync(
            Arg.Any<IntegrationConnectorId?>(),
            Arg.Any<SourceStatus?>(),
            Arg.Any<FreshnessStatus?>(),
            Arg.Any<CancellationToken>())
            .Returns(sources);
        _sourceRepository.CountByFreshnessStatusAsync(Arg.Any<FreshnessStatus>(), Arg.Any<CancellationToken>())
            .Returns(1);

        var handler = new GetIngestionHealth.Handler(_connectorRepository, _sourceRepository);
        var query = new GetIngestionHealth.Query();

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.OverallStatus.Should().NotBeNullOrWhiteSpace();
    }

    // ── GetIngestionFreshness ──

    [Fact]
    public async Task GetFreshness_ShouldReturnFreshnessSummary()
    {
        // Arrange
        var connector = IntegrationConnector.Create("test", "CI/CD", null, "Test", null, null, null, null, null, DateTimeOffset.UtcNow);
        var sources = new List<IngestionSource>
        {
            IngestionSource.Create(connector.Id, "test", "Webhook", null, null, null, 30, DateTimeOffset.UtcNow)
        };

        _sourceRepository.ListAsync(
            Arg.Any<IntegrationConnectorId?>(),
            Arg.Any<SourceStatus?>(),
            Arg.Any<FreshnessStatus?>(),
            Arg.Any<CancellationToken>())
            .Returns(sources);
        _connectorRepository.GetByIdAsync(Arg.Any<IntegrationConnectorId>(), Arg.Any<CancellationToken>())
            .Returns(connector);

        var handler = new GetIngestionFreshness.Handler(_sourceRepository, _connectorRepository);
        var query = new GetIngestionFreshness.Query();

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    // ── RetryConnector ──

    [Fact]
    public async Task RetryConnector_ValidId_ShouldReturnQueued()
    {
        // Arrange
        var connectorId = Guid.NewGuid();
        var connector = IntegrationConnector.Create("test", "CI/CD", null, "Test", null, null, null, null, null, DateTimeOffset.UtcNow);

        _connectorRepository.GetByIdAsync(Arg.Any<IntegrationConnectorId>(), Arg.Any<CancellationToken>())
            .Returns(connector);
        _unitOfWork.CommitAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(1));

        var handler = new RetryConnector.Handler(_connectorRepository, _executionRepository, _unitOfWork, _clock);
        var command = new RetryConnector.Command(connectorId.ToString());

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Status.Should().Be("Queued");
        await _executionRepository.Received(1).AddAsync(Arg.Any<IngestionExecution>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RetryConnector_InvalidGuidFormat_ShouldReturnValidationError()
    {
        // Arrange
        var handler = new RetryConnector.Handler(_connectorRepository, _executionRepository, _unitOfWork, _clock);
        var command = new RetryConnector.Command("not-a-guid");

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("INVALID_CONNECTOR_ID");
    }

    [Fact]
    public void RetryConnector_Validator_EmptyId_ShouldFail()
    {
        var validator = new RetryConnector.Validator();
        var command = new RetryConnector.Command(string.Empty);
        var validationResult = validator.Validate(command);
        validationResult.IsValid.Should().BeFalse();
    }

    // ── ReprocessExecution ──

    [Fact]
    public async Task ReprocessExecution_ValidId_ShouldReturnQueued()
    {
        // Arrange
        var connector = IntegrationConnector.Create("test", "CI/CD", null, "Test", null, null, null, null, null, DateTimeOffset.UtcNow);
        var originalExecution = IngestionExecution.Start(connector.Id, null, "original-corr", DateTimeOffset.UtcNow);

        _executionRepository.GetByIdAsync(Arg.Any<IngestionExecutionId>(), Arg.Any<CancellationToken>())
            .Returns(originalExecution);
        _unitOfWork.CommitAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(1));
        _sender.Send(Arg.Any<ProcessIngestionPayloadFeature.Command>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result<ProcessIngestionPayloadFeature.Response>.Success(
                new ProcessIngestionPayloadFeature.Response(Guid.NewGuid(), "metadata_recorded"))));

        var handler = new ReprocessExecution.Handler(_executionRepository, _unitOfWork, _clock, _sender);
        var command = new ReprocessExecution.Command(originalExecution.Id.Value.ToString());

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Status.Should().Be("Queued");
        await _executionRepository.Received(1).AddAsync(Arg.Any<IngestionExecution>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ReprocessExecution_InvalidGuidFormat_ShouldReturnValidationError()
    {
        // Arrange
        var handler = new ReprocessExecution.Handler(_executionRepository, _unitOfWork, _clock, _sender);
        var command = new ReprocessExecution.Command("not-a-guid");

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("INVALID_EXECUTION_ID");
    }

    [Fact]
    public void ReprocessExecution_Validator_EmptyId_ShouldFail()
    {
        var validator = new ReprocessExecution.Validator();
        var command = new ReprocessExecution.Command(string.Empty);
        var validationResult = validator.Validate(command);
        validationResult.IsValid.Should().BeFalse();
    }

    // ── Test Helpers ──

    private static List<IntegrationConnector> CreateTestConnectors(int count)
    {
        var connectors = new List<IntegrationConnector>();
        for (var i = 0; i < count; i++)
        {
            connectors.Add(IntegrationConnector.Create(
                name: $"connector-{i}",
                connectorType: "CI/CD",
                description: $"Test connector {i}",
                provider: "TestProvider",
                endpoint: null,
                environment: null,
                authenticationMode: null,
                pollingMode: null,
                allowedTeams: null,
                utcNow: DateTimeOffset.UtcNow));
        }
        return connectors;
    }
}
