using NexTraceOne.Knowledge.Domain.Entities;
using NexTraceOne.Knowledge.Domain.Enums;

namespace NexTraceOne.Knowledge.Tests.Domain;

/// <summary>
/// Testes de unidade para os comportamentos do domínio de Knowledge:
/// KnowledgeDocument, OperationalNote e KnowledgeRelation.
/// </summary>
public sealed class KnowledgeDocumentTests
{
    [Fact]
    public void Create_ShouldSetAllProperties()
    {
        // Arrange
        var authorId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        var tags = new List<string> { "production", "critical" };

        // Act
        var document = KnowledgeDocument.Create(
            "Deployment Guide",
            "# Deployment Guide\n\nStep-by-step instructions...",
            "Guide for deploying services",
            DocumentCategory.Procedure,
            tags,
            authorId,
            now);

        // Assert
        document.Id.Should().NotBeNull();
        document.Title.Should().Be("Deployment Guide");
        document.Slug.Should().Be("deployment-guide");
        document.Content.Should().Contain("Step-by-step");
        document.Summary.Should().Be("Guide for deploying services");
        document.Category.Should().Be(DocumentCategory.Procedure);
        document.Status.Should().Be(DocumentStatus.Draft);
        document.Tags.Should().BeEquivalentTo(tags);
        document.AuthorId.Should().Be(authorId);
        document.Version.Should().Be(1);
        document.CreatedAt.Should().Be(now);
    }

    [Fact]
    public void UpdateContent_ShouldIncrementVersion()
    {
        // Arrange
        var authorId = Guid.NewGuid();
        var editorId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        var document = KnowledgeDocument.Create("Title", "Content", null, DocumentCategory.General, null, authorId, now);

        // Act
        document.UpdateContent("New Title", "New Content", "New Summary", editorId, now.AddHours(1));

        // Assert
        document.Title.Should().Be("New Title");
        document.Content.Should().Be("New Content");
        document.Summary.Should().Be("New Summary");
        document.LastEditorId.Should().Be(editorId);
        document.Version.Should().Be(2);
        document.UpdatedAt.Should().Be(now.AddHours(1));
    }

    [Fact]
    public void Publish_ShouldSetStatusToPublished()
    {
        // Arrange
        var now = DateTimeOffset.UtcNow;
        var document = KnowledgeDocument.Create("Title", "Content", null, DocumentCategory.General, null, Guid.NewGuid(), now);

        // Act
        document.Publish(now.AddMinutes(5));

        // Assert
        document.Status.Should().Be(DocumentStatus.Published);
        document.PublishedAt.Should().Be(now.AddMinutes(5));
    }

    [Fact]
    public void Archive_ShouldSetStatusToArchived()
    {
        // Arrange
        var now = DateTimeOffset.UtcNow;
        var document = KnowledgeDocument.Create("Title", "Content", null, DocumentCategory.General, null, Guid.NewGuid(), now);
        document.Publish(now);

        // Act
        document.Archive(now.AddDays(1));

        // Assert
        document.Status.Should().Be(DocumentStatus.Archived);
    }

    [Fact]
    public void Deprecate_ShouldSetStatusToDeprecated()
    {
        // Arrange
        var now = DateTimeOffset.UtcNow;
        var document = KnowledgeDocument.Create("Title", "Content", null, DocumentCategory.General, null, Guid.NewGuid(), now);

        // Act
        document.Deprecate(now.AddDays(30));

        // Assert
        document.Status.Should().Be(DocumentStatus.Deprecated);
    }

    [Fact]
    public void Create_WithEmptyTitle_ShouldThrow()
    {
        // Act & Assert
        var act = () => KnowledgeDocument.Create("", "Content", null, DocumentCategory.General, null, Guid.NewGuid(), DateTimeOffset.UtcNow);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_WithEmptyContent_ShouldThrow()
    {
        // Act & Assert
        var act = () => KnowledgeDocument.Create("Title", "", null, DocumentCategory.General, null, Guid.NewGuid(), DateTimeOffset.UtcNow);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_WithDefaultAuthorId_ShouldThrow()
    {
        // Act & Assert
        var act = () => KnowledgeDocument.Create("Title", "Content", null, DocumentCategory.General, null, Guid.Empty, DateTimeOffset.UtcNow);
        act.Should().Throw<ArgumentException>();
    }
}
