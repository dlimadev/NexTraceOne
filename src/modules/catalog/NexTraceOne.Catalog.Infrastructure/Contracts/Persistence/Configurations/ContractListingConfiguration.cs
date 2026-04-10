using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using NexTraceOne.Catalog.Domain.Contracts.Entities;

namespace NexTraceOne.Catalog.Infrastructure.Contracts.Persistence.Configurations;

/// <summary>
/// Configura o mapeamento EF Core da entidade ContractListing.
/// Listagens de contratos publicados no marketplace interno.
/// </summary>
internal sealed class ContractListingConfiguration : IEntityTypeConfiguration<ContractListing>
{
    public void Configure(EntityTypeBuilder<ContractListing> builder)
    {
        builder.ToTable("cat_contract_listings");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => ContractListingId.From(value));

        builder.Property(x => x.ContractId).IsRequired().HasMaxLength(200);
        builder.Property(x => x.Category).IsRequired().HasMaxLength(100);
        builder.Property(x => x.Tags).HasColumnType("jsonb");

        builder.Property(x => x.ConsumerCount).IsRequired();
        builder.Property(x => x.Rating).IsRequired().HasPrecision(3, 2);
        builder.Property(x => x.TotalReviews).IsRequired();
        builder.Property(x => x.IsPromoted).IsRequired();

        builder.Property(x => x.Description).HasMaxLength(4000);

        builder.Property(x => x.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(x => x.PublishedBy).HasMaxLength(200);

        builder.Property(x => x.PublishedAt)
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(x => x.TenantId).HasMaxLength(200);

        builder.Property(x => x.RowVersion)
            .IsRowVersion();

        builder.HasIndex(x => x.ContractId);
        builder.HasIndex(x => x.Category);
        builder.HasIndex(x => x.Status);
        builder.HasIndex(x => x.IsPromoted);
        builder.HasIndex(x => x.PublishedAt);
    }
}
