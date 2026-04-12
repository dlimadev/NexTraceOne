using NexTraceOne.Catalog.Domain.Contracts.Entities;
using NexTraceOne.Catalog.Domain.Contracts.Enums;

namespace NexTraceOne.Catalog.Tests.Contracts.Domain.Entities;

/// <summary>
/// Testes unitários para <see cref="ContractListing"/>.
/// Valida criação via factory method Publish, atualização de métricas via UpdateMetrics,
/// guarda de parâmetros, limites de negócio, trimming de strings e imutabilidade.
/// </summary>
public sealed class ContractListingTests
{
    private static readonly DateTimeOffset FixedNow = new(2025, 07, 01, 12, 0, 0, TimeSpan.Zero);

    // ── Factory method: Publish — valid scenarios ──

    [Fact]
    public void Publish_ValidInputs_ShouldSetAllFields()
    {
        var listing = CreateValid();

        listing.Id.Value.Should().NotBeEmpty();
        listing.ContractId.Should().Be("contract-abc");
        listing.Category.Should().Be("Payments");
        listing.Tags.Should().Be("[\"rest\",\"payments\"]");
        listing.ConsumerCount.Should().Be(0);
        listing.Rating.Should().Be(0m);
        listing.TotalReviews.Should().Be(0);
        listing.IsPromoted.Should().BeTrue();
        listing.Description.Should().Be("Payment gateway contract");
        listing.Status.Should().Be(MarketplaceListingStatus.Published);
        listing.PublishedBy.Should().Be("user-1");
        listing.PublishedAt.Should().Be(FixedNow);
        listing.TenantId.Should().Be("tenant-abc");
    }

    [Fact]
    public void Publish_NullOptionalFields_ShouldBeValid()
    {
        var listing = ContractListing.Publish(
            "contract-xyz",
            "Logistics",
            null,
            false,
            null,
            MarketplaceListingStatus.Draft,
            null,
            FixedNow);

        listing.Tags.Should().BeNull();
        listing.Description.Should().BeNull();
        listing.PublishedBy.Should().BeNull();
        listing.TenantId.Should().BeNull();
        listing.IsPromoted.Should().BeFalse();
    }

    [Theory]
    [InlineData(MarketplaceListingStatus.Draft)]
    [InlineData(MarketplaceListingStatus.Published)]
    [InlineData(MarketplaceListingStatus.Archived)]
    public void Publish_AllStatuses_ShouldBeAccepted(MarketplaceListingStatus status)
    {
        var listing = ContractListing.Publish(
            "contract-1",
            "General",
            null,
            false,
            null,
            status,
            null,
            FixedNow);

        listing.Status.Should().Be(status);
    }

    [Fact]
    public void Publish_TrimsStrings()
    {
        var listing = ContractListing.Publish(
            "  contract-abc  ",
            "  Payments  ",
            null,
            false,
            "  Description  ",
            MarketplaceListingStatus.Published,
            "  user-1  ",
            FixedNow,
            "  tenant-1  ");

        listing.ContractId.Should().Be("contract-abc");
        listing.Category.Should().Be("Payments");
        listing.Description.Should().Be("Description");
        listing.PublishedBy.Should().Be("user-1");
        listing.TenantId.Should().Be("tenant-1");
    }

    [Fact]
    public void Publish_IsPromotedTrue_ShouldSetFlag()
    {
        var listing = ContractListing.Publish(
            "contract-promo",
            "Featured",
            null,
            true,
            null,
            MarketplaceListingStatus.Published,
            null,
            FixedNow);

        listing.IsPromoted.Should().BeTrue();
    }

    [Fact]
    public void Publish_IsPromotedFalse_ShouldSetFlag()
    {
        var listing = ContractListing.Publish(
            "contract-normal",
            "General",
            null,
            false,
            null,
            MarketplaceListingStatus.Published,
            null,
            FixedNow);

        listing.IsPromoted.Should().BeFalse();
    }

    // ── UpdateMetrics ──

    [Fact]
    public void UpdateMetrics_ValidValues_ShouldUpdateFields()
    {
        var listing = CreateValid();

        listing.UpdateMetrics(42, 4.5m, 100);

        listing.ConsumerCount.Should().Be(42);
        listing.Rating.Should().Be(4.5m);
        listing.TotalReviews.Should().Be(100);
    }

    [Fact]
    public void UpdateMetrics_ZeroValues_ShouldBeValid()
    {
        var listing = CreateValid();

        listing.UpdateMetrics(0, 0m, 0);

        listing.ConsumerCount.Should().Be(0);
        listing.Rating.Should().Be(0m);
        listing.TotalReviews.Should().Be(0);
    }

    [Fact]
    public void UpdateMetrics_BoundaryRating5_ShouldBeValid()
    {
        var listing = CreateValid();

        listing.UpdateMetrics(1, 5m, 1);

        listing.Rating.Should().Be(5m);
    }

    [Fact]
    public void UpdateMetrics_BoundaryRating0_ShouldBeValid()
    {
        var listing = CreateValid();

        listing.UpdateMetrics(0, 0m, 0);

        listing.Rating.Should().Be(0m);
    }

    [Fact]
    public void UpdateMetrics_NegativeConsumerCount_ShouldThrow()
    {
        var listing = CreateValid();

        var act = () => listing.UpdateMetrics(-1, 3m, 5);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void UpdateMetrics_RatingAbove5_ShouldThrow()
    {
        var listing = CreateValid();

        var act = () => listing.UpdateMetrics(1, 5.1m, 1);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void UpdateMetrics_NegativeRating_ShouldThrow()
    {
        var listing = CreateValid();

        var act = () => listing.UpdateMetrics(1, -0.1m, 1);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void UpdateMetrics_NegativeTotalReviews_ShouldThrow()
    {
        var listing = CreateValid();

        var act = () => listing.UpdateMetrics(1, 3m, -1);

        act.Should().Throw<ArgumentException>();
    }

    // ── Guard clauses: Publish ──

    [Fact]
    public void Publish_EmptyContractId_ShouldThrow()
    {
        var act = () => ContractListing.Publish(
            "",
            "Payments",
            null, false, null,
            MarketplaceListingStatus.Published,
            null, FixedNow);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Publish_WhitespaceContractId_ShouldThrow()
    {
        var act = () => ContractListing.Publish(
            "   ",
            "Payments",
            null, false, null,
            MarketplaceListingStatus.Published,
            null, FixedNow);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Publish_ContractIdTooLong_ShouldThrow()
    {
        var act = () => ContractListing.Publish(
            new string('x', 201),
            "Payments",
            null, false, null,
            MarketplaceListingStatus.Published,
            null, FixedNow);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Publish_EmptyCategory_ShouldThrow()
    {
        var act = () => ContractListing.Publish(
            "contract-abc",
            "",
            null, false, null,
            MarketplaceListingStatus.Published,
            null, FixedNow);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Publish_CategoryTooLong_ShouldThrow()
    {
        var act = () => ContractListing.Publish(
            "contract-abc",
            new string('x', 101),
            null, false, null,
            MarketplaceListingStatus.Published,
            null, FixedNow);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Publish_DescriptionTooLong_ShouldThrow()
    {
        var act = () => ContractListing.Publish(
            "contract-abc",
            "Payments",
            null, false,
            new string('x', 4001),
            MarketplaceListingStatus.Published,
            null, FixedNow);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Publish_PublishedByTooLong_ShouldThrow()
    {
        var act = () => ContractListing.Publish(
            "contract-abc",
            "Payments",
            null, false, null,
            MarketplaceListingStatus.Published,
            new string('x', 201),
            FixedNow);

        act.Should().Throw<ArgumentException>();
    }

    // ── Strongly typed Id ──

    [Fact]
    public void ContractListingId_New_ShouldGenerateUniqueIds()
    {
        var id1 = ContractListingId.New();
        var id2 = ContractListingId.New();

        id1.Should().NotBe(id2);
        id1.Value.Should().NotBeEmpty();
    }

    [Fact]
    public void ContractListingId_From_ShouldPreserveGuid()
    {
        var guid = Guid.NewGuid();
        var id = ContractListingId.From(guid);

        id.Value.Should().Be(guid);
    }

    // ── Helper ──

    private static ContractListing CreateValid() => ContractListing.Publish(
        contractId: "contract-abc",
        category: "Payments",
        tags: "[\"rest\",\"payments\"]",
        isPromoted: true,
        description: "Payment gateway contract",
        status: MarketplaceListingStatus.Published,
        publishedBy: "user-1",
        publishedAt: FixedNow,
        tenantId: "tenant-abc");
}
