using NexTraceOne.Knowledge.Domain.Entities;
using NexTraceOne.Knowledge.Domain.Enums;

namespace NexTraceOne.Knowledge.Tests.Domain;

/// <summary>
/// Testes de unidade para OperationalNote.
/// </summary>
public sealed class OperationalNoteTests
{
    [Fact]
    public void Create_ShouldSetAllProperties()
    {
        // Arrange
        var authorId = Guid.NewGuid();
        var contextEntityId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        var tags = new List<string> { "incident", "p1" };

        // Act
        var note = OperationalNote.Create(
            "Production issue in order-service",
            "Order processing is delayed by 30s after latest deploy.",
            NoteSeverity.Critical,
            OperationalNoteType.Mitigation,
            "IncidentTimeline",
            authorId,
            contextEntityId,
            "Service",
            tags,
            now);

        // Assert
        note.Id.Should().NotBeNull();
        note.Title.Should().Be("Production issue in order-service");
        note.Content.Should().Contain("Order processing");
        note.Severity.Should().Be(NoteSeverity.Critical);
        note.NoteType.Should().Be(OperationalNoteType.Mitigation);
        note.Origin.Should().Be("IncidentTimeline");
        note.AuthorId.Should().Be(authorId);
        note.ContextEntityId.Should().Be(contextEntityId);
        note.ContextType.Should().Be("Service");
        note.Tags.Should().BeEquivalentTo(tags);
        note.IsResolved.Should().BeFalse();
        note.CreatedAt.Should().Be(now);
    }

    [Fact]
    public void Resolve_ShouldMarkAsResolved()
    {
        // Arrange
        var now = DateTimeOffset.UtcNow;
        var note = OperationalNote.Create("Title", "Content", NoteSeverity.Info, OperationalNoteType.Observation, "Manual", Guid.NewGuid(), null, null, null, now);

        // Act
        note.Resolve(now.AddHours(2));

        // Assert
        note.IsResolved.Should().BeTrue();
        note.ResolvedAt.Should().Be(now.AddHours(2));
    }

    [Fact]
    public void Reopen_ShouldClearResolvedState()
    {
        // Arrange
        var now = DateTimeOffset.UtcNow;
        var note = OperationalNote.Create("Title", "Content", NoteSeverity.Warning, OperationalNoteType.Observation, "Manual", Guid.NewGuid(), null, null, null, now);
        note.Resolve(now.AddHours(1));

        // Act
        note.Reopen(now.AddHours(2));

        // Assert
        note.IsResolved.Should().BeFalse();
        note.ResolvedAt.Should().BeNull();
    }

    [Fact]
    public void UpdateSeverity_ShouldChangeTheSeverity()
    {
        // Arrange
        var now = DateTimeOffset.UtcNow;
        var note = OperationalNote.Create("Title", "Content", NoteSeverity.Info, OperationalNoteType.Observation, "Manual", Guid.NewGuid(), null, null, null, now);

        // Act
        note.UpdateSeverity(NoteSeverity.Critical, now.AddMinutes(10));

        // Assert
        note.Severity.Should().Be(NoteSeverity.Critical);
        note.UpdatedAt.Should().Be(now.AddMinutes(10));
    }

    [Fact]
    public void Create_WithEmptyTitle_ShouldThrow()
    {
        // Act & Assert
        var act = () => OperationalNote.Create("", "Content", NoteSeverity.Info, OperationalNoteType.Observation, "Manual", Guid.NewGuid(), null, null, null, DateTimeOffset.UtcNow);
        act.Should().Throw<ArgumentException>();
    }
}
