using NexTraceOne.Integrations.Application.Parsing;
using NexTraceOne.Integrations.Domain.Entities;
using NexTraceOne.Integrations.Domain.Enums;

namespace NexTraceOne.Integrations.Tests.Application.Parsing;

/// <summary>
/// Testes unitários para <see cref="GenericIngestionPayloadParser"/> e
/// métodos de estado da entidade <see cref="IngestionExecution"/>.
/// </summary>
public sealed class IngestionPayloadParserTests
{
    private readonly GenericIngestionPayloadParser _parser = new();

    // ── Parser — success scenarios ──────────────────────────────────────────

    [Fact]
    public void ParseDeployPayload_AllKnownFields_ReturnsSuccessWithAllFieldsParsed()
    {
        // Arrange
        const string payload = """
            {
                "serviceName": "payment-service",
                "environment": "production",
                "version": "2.3.1",
                "commitSha": "abc123def456",
                "changeType": "deploy",
                "timestamp": "2024-06-15T14:30:00Z"
            }
            """;

        // Act
        var result = _parser.ParseDeployPayload(payload);

        // Assert
        result.IsSuccessful.Should().BeTrue();
        result.ErrorMessage.Should().BeNull();
        result.ServiceName.Should().Be("payment-service");
        result.Environment.Should().Be("production");
        result.Version.Should().Be("2.3.1");
        result.CommitSha.Should().Be("abc123def456");
        result.ChangeType.Should().Be("deploy");
        result.Timestamp.Should().Be(DateTimeOffset.Parse("2024-06-15T14:30:00Z"));
    }

    [Fact]
    public void ParseDeployPayload_MinimalFields_ReturnsSuccessWithPartialParse()
    {
        // Arrange — only service and version; other fields absent
        const string payload = """
            {
                "service": "order-service",
                "version": "1.0.0"
            }
            """;

        // Act
        var result = _parser.ParseDeployPayload(payload);

        // Assert
        result.IsSuccessful.Should().BeTrue();
        result.ServiceName.Should().Be("order-service");
        result.Version.Should().Be("1.0.0");
        result.Environment.Should().BeNull();
        result.CommitSha.Should().BeNull();
        result.ChangeType.Should().BeNull();
    }

    [Fact]
    public void ParseDeployPayload_AliasFieldNames_ParsesCorrectly()
    {
        // Arrange — use alternate aliases: env, commit_sha, change_type, service_name
        const string payload = """
            {
                "service_name": "catalog-api",
                "env": "staging",
                "version": "0.9.0",
                "commit_sha": "deadbeef",
                "change_type": "hotfix",
                "deployed_at": "2024-07-01T10:00:00+00:00"
            }
            """;

        // Act
        var result = _parser.ParseDeployPayload(payload);

        // Assert
        result.IsSuccessful.Should().BeTrue();
        result.ServiceName.Should().Be("catalog-api");
        result.Environment.Should().Be("staging");
        result.CommitSha.Should().Be("deadbeef");
        result.ChangeType.Should().Be("hotfix");
        result.Timestamp.Should().Be(DateTimeOffset.Parse("2024-07-01T10:00:00+00:00"));
    }

    [Fact]
    public void ParseDeployPayload_ExtraFields_AreCapturedInAdditionalMetadata()
    {
        // Arrange
        const string payload = """
            {
                "serviceName": "auth-service",
                "version": "3.0.0",
                "pipeline": "main-ci",
                "triggeredBy": "ci-bot"
            }
            """;

        // Act
        var result = _parser.ParseDeployPayload(payload);

        // Assert
        result.IsSuccessful.Should().BeTrue();
        result.AdditionalMetadata.Should().ContainKey("pipeline");
        result.AdditionalMetadata["pipeline"].Should().Be("main-ci");
        result.AdditionalMetadata.Should().ContainKey("triggeredBy");
        result.AdditionalMetadata.Should().NotContainKey("serviceName");
        result.AdditionalMetadata.Should().NotContainKey("version");
    }

    // ── Parser — failure scenarios ──────────────────────────────────────────

    [Fact]
    public void ParseDeployPayload_MalformedJson_ReturnsIsSuccessfulFalse()
    {
        // Arrange
        const string payload = "{ this is not valid json }";

        // Act
        var result = _parser.ParseDeployPayload(payload);

        // Assert
        result.IsSuccessful.Should().BeFalse();
        result.ErrorMessage.Should().NotBeNullOrEmpty();
        result.ServiceName.Should().BeNull();
    }

    [Fact]
    public void ParseDeployPayload_NoRecognizableFields_ReturnsIsSuccessfulFalse()
    {
        // Arrange — valid JSON but no fields that the parser knows about
        const string payload = """
            {
                "foo": "bar",
                "baz": "qux",
                "unknownField": 42
            }
            """;

        // Act
        var result = _parser.ParseDeployPayload(payload);

        // Assert
        result.IsSuccessful.Should().BeFalse();
        result.ErrorMessage.Should().Contain("No recognizable semantic fields");
    }

    [Fact]
    public void ParseDeployPayload_EmptyPayload_ReturnsIsSuccessfulFalse()
    {
        // Act
        var result = _parser.ParseDeployPayload(string.Empty);

        // Assert
        result.IsSuccessful.Should().BeFalse();
        result.ErrorMessage.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void ParseDeployPayload_JsonArray_ReturnsIsSuccessfulFalse()
    {
        // Arrange — root is array, not object
        const string payload = """["item1", "item2"]""";

        // Act
        var result = _parser.ParseDeployPayload(payload);

        // Assert
        result.IsSuccessful.Should().BeFalse();
        result.ErrorMessage.Should().Contain("not a JSON object");
    }

    // ── Entity state transitions ─────────────────────────────────────────────

    [Fact]
    public void IngestionExecution_MarkAsProcessed_SetsAllParsedFieldsAndStatus()
    {
        // Arrange
        var connector = IntegrationConnector.Create(
            name: "test-connector",
            connectorType: "CI/CD",
            description: null,
            provider: "GitHub",
            endpoint: null,
            environment: null,
            authenticationMode: null,
            pollingMode: null,
            allowedTeams: null,
            utcNow: DateTimeOffset.UtcNow);

        var execution = IngestionExecution.Start(connector.Id, null, "corr-001", DateTimeOffset.UtcNow);
        var parsedAt = new DateTimeOffset(2024, 6, 15, 14, 30, 0, TimeSpan.Zero);

        // Act
        execution.MarkAsProcessed(
            serviceName: "inventory-service",
            environment: "production",
            version: "1.5.0",
            commitSha: "abc123",
            changeType: "deploy",
            parsedAt: parsedAt);

        // Assert
        execution.ProcessingStatus.Should().Be(ProcessingStatus.Processed);
        execution.ParsedServiceName.Should().Be("inventory-service");
        execution.ParsedEnvironment.Should().Be("production");
        execution.ParsedVersion.Should().Be("1.5.0");
        execution.ParsedCommitSha.Should().Be("abc123");
        execution.ParsedChangeType.Should().Be("deploy");
        execution.ParsedAt.Should().Be(parsedAt);
    }

    [Fact]
    public void IngestionExecution_MarkAsFailed_SetsFailedStatus()
    {
        // Arrange
        var connector = IntegrationConnector.Create(
            name: "test-connector",
            connectorType: "CI/CD",
            description: null,
            provider: "GitHub",
            endpoint: null,
            environment: null,
            authenticationMode: null,
            pollingMode: null,
            allowedTeams: null,
            utcNow: DateTimeOffset.UtcNow);

        var execution = IngestionExecution.Start(connector.Id, null, "corr-002", DateTimeOffset.UtcNow);

        // Act
        execution.MarkAsFailed("Malformed JSON in payload");

        // Assert — status transitions to Failed, parsed fields remain null
        execution.ProcessingStatus.Should().Be(ProcessingStatus.Failed);
        execution.ParsedServiceName.Should().BeNull();
        execution.ParsedAt.Should().BeNull();
    }

    [Fact]
    public void IngestionExecution_DefaultProcessingStatus_IsMetadataRecorded()
    {
        // Arrange
        var connector = IntegrationConnector.Create(
            name: "test-connector",
            connectorType: "CI/CD",
            description: null,
            provider: "GitHub",
            endpoint: null,
            environment: null,
            authenticationMode: null,
            pollingMode: null,
            allowedTeams: null,
            utcNow: DateTimeOffset.UtcNow);

        // Act
        var execution = IngestionExecution.Start(connector.Id, null, "corr-003", DateTimeOffset.UtcNow);

        // Assert
        execution.ProcessingStatus.Should().Be(ProcessingStatus.MetadataRecorded);
    }
}
