using NexTraceOne.Catalog.Domain.Contracts.Entities;

namespace NexTraceOne.Catalog.Tests.Contracts.Domain.Entities;

/// <summary>
/// Testes unitários para <see cref="NegotiationComment"/>.
/// Valida criação, propriedades e validações de entrada.
/// </summary>
public sealed class NegotiationCommentTests
{
    private static readonly Guid ValidNegotiationId = Guid.NewGuid();
    private static readonly DateTimeOffset FixedDate = new(2025, 06, 15, 10, 0, 0, TimeSpan.Zero);

    // ── Create com valores válidos ───────────────────────────────────

    [Fact]
    public void Create_Should_SetAllProperties_When_ValidValues()
    {
        var comment = NegotiationComment.Create(
            ValidNegotiationId, "user-456", "Jane Doe",
            "I suggest changing the endpoint path", "L42", FixedDate);

        comment.Id.Value.Should().NotBeEmpty();
        comment.NegotiationId.Should().Be(ValidNegotiationId);
        comment.AuthorId.Should().Be("user-456");
        comment.AuthorDisplayName.Should().Be("Jane Doe");
        comment.Content.Should().Be("I suggest changing the endpoint path");
        comment.LineReference.Should().Be("L42");
        comment.CreatedAt.Should().Be(FixedDate);
    }

    [Fact]
    public void Create_Should_AllowNullLineReference()
    {
        var comment = NegotiationComment.Create(
            ValidNegotiationId, "user-456", "Jane Doe",
            "General feedback", null, FixedDate);

        comment.LineReference.Should().BeNull();
    }

    // ── Validação de criação ─────────────────────────────────────────

    [Fact]
    public void Create_Should_Throw_When_NegotiationIdIsDefault()
    {
        var act = () => NegotiationComment.Create(
            Guid.Empty, "user-456", "Jane Doe", "Content", null, FixedDate);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_Should_Throw_When_AuthorIdIsEmpty()
    {
        var act = () => NegotiationComment.Create(
            ValidNegotiationId, "", "Jane Doe", "Content", null, FixedDate);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_Should_Throw_When_AuthorDisplayNameIsEmpty()
    {
        var act = () => NegotiationComment.Create(
            ValidNegotiationId, "user-456", "", "Content", null, FixedDate);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_Should_Throw_When_ContentIsEmpty()
    {
        var act = () => NegotiationComment.Create(
            ValidNegotiationId, "user-456", "Jane Doe", "", null, FixedDate);

        act.Should().Throw<ArgumentException>();
    }

    // ── IDs únicos ──────────────────────────────────────────────────

    [Fact]
    public void Create_Should_GenerateUniqueIds()
    {
        var c1 = NegotiationComment.Create(
            ValidNegotiationId, "user-1", "User 1", "Comment 1", null, FixedDate);
        var c2 = NegotiationComment.Create(
            ValidNegotiationId, "user-2", "User 2", "Comment 2", null, FixedDate);

        c1.Id.Should().NotBe(c2.Id);
    }

    [Fact]
    public void NegotiationCommentId_New_Should_CreateUniqueId()
    {
        var id1 = NegotiationCommentId.New();
        var id2 = NegotiationCommentId.New();

        id1.Should().NotBe(id2);
        id1.Value.Should().NotBeEmpty();
    }

    [Fact]
    public void NegotiationCommentId_From_Should_PreserveGuid()
    {
        var guid = Guid.NewGuid();
        var id = NegotiationCommentId.From(guid);

        id.Value.Should().Be(guid);
    }
}
