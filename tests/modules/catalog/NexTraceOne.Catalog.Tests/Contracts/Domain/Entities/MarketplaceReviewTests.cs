using NexTraceOne.Catalog.Domain.Contracts.Entities;

namespace NexTraceOne.Catalog.Tests.Contracts.Domain.Entities;

/// <summary>
/// Testes unitários para <see cref="MarketplaceReview"/>.
/// Valida criação via factory method Submit, guarda de parâmetros,
/// limites de rating e comprimento de comentário.
/// </summary>
public sealed class MarketplaceReviewTests
{
    private static readonly DateTimeOffset FixedNow = new(2025, 07, 01, 12, 0, 0, TimeSpan.Zero);
    private static readonly ContractListingId ValidListingId = ContractListingId.New();

    // ── Factory method: Submit — valid scenarios ──

    [Fact]
    public void Submit_ValidInputs_ShouldSetAllFields()
    {
        var review = CreateValid();

        review.Id.Value.Should().NotBeEmpty();
        review.ListingId.Should().Be(ValidListingId);
        review.AuthorId.Should().Be("author-1");
        review.Rating.Should().Be(4);
        review.Comment.Should().Be("Great contract, well documented.");
        review.ReviewedAt.Should().Be(FixedNow);
        review.TenantId.Should().Be("tenant-abc");
    }

    [Fact]
    public void Submit_NullOptionalFields_ShouldBeValid()
    {
        var review = MarketplaceReview.Submit(
            ValidListingId,
            "author-2",
            3,
            null,
            FixedNow);

        review.Comment.Should().BeNull();
        review.TenantId.Should().BeNull();
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    public void Submit_AllValidRatings_ShouldBeAccepted(int rating)
    {
        var review = MarketplaceReview.Submit(
            ValidListingId,
            "author-1",
            rating,
            null,
            FixedNow);

        review.Rating.Should().Be(rating);
    }

    [Fact]
    public void Submit_TrimsStrings()
    {
        var review = MarketplaceReview.Submit(
            ValidListingId,
            "  author-1  ",
            5,
            "  Nice  ",
            FixedNow,
            "  tenant-1  ");

        review.AuthorId.Should().Be("author-1");
        review.Comment.Should().Be("Nice");
        review.TenantId.Should().Be("tenant-1");
    }

    // ── Guard clauses ──

    [Fact]
    public void Submit_EmptyAuthorId_ShouldThrow()
    {
        var act = () => MarketplaceReview.Submit(
            ValidListingId,
            "",
            4,
            null,
            FixedNow);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Submit_WhitespaceAuthorId_ShouldThrow()
    {
        var act = () => MarketplaceReview.Submit(
            ValidListingId,
            "   ",
            4,
            null,
            FixedNow);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Submit_AuthorIdTooLong_ShouldThrow()
    {
        var act = () => MarketplaceReview.Submit(
            ValidListingId,
            new string('x', 201),
            4,
            null,
            FixedNow);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Submit_Rating0_ShouldThrow()
    {
        var act = () => MarketplaceReview.Submit(
            ValidListingId,
            "author-1",
            0,
            null,
            FixedNow);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Submit_Rating6_ShouldThrow()
    {
        var act = () => MarketplaceReview.Submit(
            ValidListingId,
            "author-1",
            6,
            null,
            FixedNow);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Submit_NegativeRating_ShouldThrow()
    {
        var act = () => MarketplaceReview.Submit(
            ValidListingId,
            "author-1",
            -1,
            null,
            FixedNow);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Submit_CommentTooLong_ShouldThrow()
    {
        var act = () => MarketplaceReview.Submit(
            ValidListingId,
            "author-1",
            4,
            new string('x', 2001),
            FixedNow);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Submit_NullListingId_ShouldThrow()
    {
        var act = () => MarketplaceReview.Submit(
            null!,
            "author-1",
            4,
            null,
            FixedNow);

        act.Should().Throw<ArgumentNullException>();
    }

    // ── Strongly typed Id ──

    [Fact]
    public void MarketplaceReviewId_New_ShouldGenerateUniqueIds()
    {
        var id1 = MarketplaceReviewId.New();
        var id2 = MarketplaceReviewId.New();

        id1.Should().NotBe(id2);
        id1.Value.Should().NotBeEmpty();
    }

    [Fact]
    public void MarketplaceReviewId_From_ShouldPreserveGuid()
    {
        var guid = Guid.NewGuid();
        var id = MarketplaceReviewId.From(guid);

        id.Value.Should().Be(guid);
    }

    // ── Helper ──

    private static MarketplaceReview CreateValid() => MarketplaceReview.Submit(
        listingId: ValidListingId,
        authorId: "author-1",
        rating: 4,
        comment: "Great contract, well documented.",
        reviewedAt: FixedNow,
        tenantId: "tenant-abc");
}
