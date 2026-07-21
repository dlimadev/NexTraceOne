using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using NexTraceOne.Catalog.Domain.Contracts.Entities;

namespace NexTraceOne.Catalog.Infrastructure.Persistence.Configurations;

/// <summary>Mapeia a(s) FK(s) typed-id não descoberta(s) pela convenção (ver reference-typed-id-fk-mapping-gap).</summary>
internal sealed class MarketplaceReviewConfiguration : IEntityTypeConfiguration<MarketplaceReview>
{
    public void Configure(EntityTypeBuilder<MarketplaceReview> builder)
    {
        builder.Property(x => x.ListingId)
            .HasConversion(id => id.Value, value => new ContractListingId(value));
        builder.HasIndex(x => x.ListingId);
    }
}
