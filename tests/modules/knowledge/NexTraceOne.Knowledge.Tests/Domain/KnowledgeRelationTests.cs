using NexTraceOne.Knowledge.Domain.Entities;
using NexTraceOne.Knowledge.Domain.Enums;

namespace NexTraceOne.Knowledge.Tests.Domain;

/// <summary>
/// Testes de unidade para KnowledgeRelation.
/// </summary>
public sealed class KnowledgeRelationTests
{
    [Fact]
    public void Create_ShouldSetAllProperties()
    {
        // Arrange
        var sourceId = Guid.NewGuid();
        var targetId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        // Act
        var relation = KnowledgeRelation.Create(
            sourceId,
            KnowledgeSourceEntityType.KnowledgeDocument,
            targetId,
            RelationType.Service,
            "Document about this service",
            "Runbook",
            userId,
            now);

        // Assert
        relation.Id.Should().NotBeNull();
        relation.SourceEntityId.Should().Be(sourceId);
        relation.SourceEntityType.Should().Be(KnowledgeSourceEntityType.KnowledgeDocument);
        relation.TargetEntityId.Should().Be(targetId);
        relation.TargetType.Should().Be(RelationType.Service);
        relation.Description.Should().Be("Document about this service");
        relation.Context.Should().Be("Runbook");
        relation.CreatedById.Should().Be(userId);
        relation.CreatedAt.Should().Be(now);
    }

    [Fact]
    public void UpdateDescription_ShouldChangeDescription()
    {
        // Arrange
        var relation = KnowledgeRelation.Create(
            Guid.NewGuid(),
            KnowledgeSourceEntityType.KnowledgeDocument,
            Guid.NewGuid(), RelationType.Incident,
            "Original description",
            null,
            Guid.NewGuid(), DateTimeOffset.UtcNow);

        // Act
        relation.UpdateDescription("Updated description");

        // Assert
        relation.Description.Should().Be("Updated description");
    }

    [Fact]
    public void Create_WithDefaultSourceId_ShouldThrow()
    {
        // Act & Assert
        var act = () => KnowledgeRelation.Create(
            Guid.Empty, KnowledgeSourceEntityType.KnowledgeDocument,
            Guid.NewGuid(), RelationType.Service,
            null, null, Guid.NewGuid(), DateTimeOffset.UtcNow);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_WithDefaultTargetId_ShouldThrow()
    {
        // Act & Assert
        var act = () => KnowledgeRelation.Create(
            Guid.NewGuid(), KnowledgeSourceEntityType.KnowledgeDocument,
            Guid.Empty, RelationType.Service,
            null, null, Guid.NewGuid(), DateTimeOffset.UtcNow);
        act.Should().Throw<ArgumentException>();
    }
}
