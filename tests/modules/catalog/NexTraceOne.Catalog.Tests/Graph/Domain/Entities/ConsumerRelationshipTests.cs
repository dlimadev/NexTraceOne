using FluentAssertions;
using NexTraceOne.Catalog.Domain.Graph.Entities;

namespace NexTraceOne.Catalog.Tests.Graph.Domain.Entities;

/// <summary>
/// Testes de domínio para a entidade ConsumerRelationship que representa
/// a relação entre um consumidor e uma API no grafo de engenharia.
/// Cenários cobertos: criação válida e atualização via Refresh com nova confiança e data.
/// </summary>
public sealed class ConsumerRelationshipTests
{
    [Fact]
    public void Create_Should_ReturnRelationship_When_DataIsValid()
    {
        // Arrange
        var consumer = ConsumerAsset.Create("billing-service", "Service", "Production");
        var source = DiscoverySource.Create("CatalogImport", "catalog/billing.csv",
            new DateTimeOffset(2025, 3, 1, 0, 0, 0, TimeSpan.Zero), 0.85m);
        var observedAt = new DateTimeOffset(2025, 3, 1, 10, 0, 0, TimeSpan.Zero);

        // Act
        var relationship = ConsumerRelationship.Create(consumer, source, observedAt);

        // Assert
        relationship.ConsumerAssetId.Should().Be(consumer.Id);
        relationship.ConsumerName.Should().Be("billing-service");
        relationship.SourceType.Should().Be("CatalogImport");
        relationship.ConfidenceScore.Should().Be(0.85m);
        relationship.FirstObservedAt.Should().Be(observedAt);
        relationship.LastObservedAt.Should().Be(observedAt);
    }

    [Fact]
    public void Refresh_Should_UpdateConfidenceAndLastObservedAt_When_Called()
    {
        // Arrange — relação criada com dados iniciais
        var consumer = ConsumerAsset.Create("checkout-service", "Service", "Production");
        var initialSource = DiscoverySource.Create("Manual", "manual/checkout.csv",
            new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero), 0.70m);
        var initialObservedAt = new DateTimeOffset(2025, 1, 1, 10, 0, 0, TimeSpan.Zero);
        var relationship = ConsumerRelationship.Create(consumer, initialSource, initialObservedAt);

        // Act — refresh com nova fonte e timestamp
        var updatedSource = DiscoverySource.Create("OpenTelemetry", "otel:trace:999",
            new DateTimeOffset(2025, 3, 15, 0, 0, 0, TimeSpan.Zero), 0.95m);
        var updatedObservedAt = new DateTimeOffset(2025, 3, 15, 14, 0, 0, TimeSpan.Zero);
        relationship.Refresh(updatedSource, updatedObservedAt);

        // Assert — confiança e lastObservedAt atualizados, firstObservedAt preservado
        relationship.SourceType.Should().Be("OpenTelemetry");
        relationship.ConfidenceScore.Should().Be(0.95m);
        relationship.LastObservedAt.Should().Be(updatedObservedAt);
        relationship.FirstObservedAt.Should().Be(initialObservedAt);
    }
}
