using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using NexTraceOne.Catalog.Domain.Contracts.Entities;

namespace NexTraceOne.Catalog.Infrastructure.Contracts.Persistence.Configurations;

/// <summary>
/// Configura o mapeamento EF Core da entidade MarketplaceReview.
/// Avaliações de contratos publicados no marketplace interno.
/// </summary>
internal sealed class MarketplaceReviewConfiguration : IEntityTypeConfiguration<MarketplaceReview>
{
    public void Configure(EntityTypeBuilder<MarketplaceReview> builder)
    {
        builder.ToTable("cat_contract_reviews");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => MarketplaceReviewId.From(value));

        builder.Property(x => x.ListingId)
            .IsRequired()
            .HasConversion(id => id.Value, value => ContractListingId.From(value));

        builder.Property(x => x.AuthorId).IsRequired().HasMaxLength(200);
        builder.Property(x => x.Rating).IsRequired();
        builder.Property(x => x.Comment).HasMaxLength(2000);

        builder.Property(x => x.ReviewedAt)
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(x => x.TenantId).HasMaxLength(200);

        builder.Property(x => x.RowVersion)
            .IsRowVersion();

        builder.HasIndex(x => x.ListingId);
        builder.HasIndex(x => x.AuthorId);
        builder.HasIndex(x => x.ReviewedAt);
    }
}
