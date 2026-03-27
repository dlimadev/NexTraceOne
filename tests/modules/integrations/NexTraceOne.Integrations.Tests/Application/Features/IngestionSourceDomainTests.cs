using NexTraceOne.Integrations.Domain.Entities;

namespace NexTraceOne.Integrations.Tests.Application.Features;

/// <summary>
/// Testes de unidade para comportamentos do domínio de integrações:
/// IngestionSource data receipt e processing tracking.
/// </summary>
public sealed class IngestionSourceDomainTests
{
    [Fact]
    public void IngestionSource_RecordDataReceived_ShouldUpdateLastProcessedAt()
    {
        // Arrange
        var connectorId = new IntegrationConnectorId(Guid.NewGuid());
        var now = DateTimeOffset.UtcNow;
        var source = IngestionSource.Create(connectorId, "Webhook", "Webhook", "Changes", "Desc", null, 30, now);

        var processTime = now.AddMinutes(5);

        // Act
        source.RecordDataReceived(10, processTime);

        // Assert
        source.LastProcessedAt.Should().Be(processTime);
        source.LastDataReceivedAt.Should().Be(processTime);
        source.DataItemsProcessed.Should().Be(10);
    }

    [Fact]
    public void IngestionSource_RecordProcessingCompleted_ShouldOnlyUpdateLastProcessedAt()
    {
        // Arrange
        var connectorId = new IntegrationConnectorId(Guid.NewGuid());
        var now = DateTimeOffset.UtcNow;
        var source = IngestionSource.Create(connectorId, "Poller", "API Polling", "Runtime", null, null, 60, now);

        var processTime = now.AddMinutes(10);

        // Act
        source.RecordProcessingCompleted(processTime);

        // Assert
        source.LastProcessedAt.Should().Be(processTime);
        source.LastDataReceivedAt.Should().BeNull();
        source.DataItemsProcessed.Should().Be(0);
    }
}
