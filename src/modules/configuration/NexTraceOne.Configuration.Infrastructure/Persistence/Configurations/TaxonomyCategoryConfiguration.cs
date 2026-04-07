using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NexTraceOne.Configuration.Domain.Entities;

namespace NexTraceOne.Configuration.Infrastructure.Persistence.Configurations;

internal sealed class TaxonomyCategoryConfiguration : IEntityTypeConfiguration<TaxonomyCategory>
{
    public void Configure(EntityTypeBuilder<TaxonomyCategory> builder)
    {
        builder.ToTable("cfg_taxonomy_categories");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => new TaxonomyCategoryId(value));
        builder.Property(x => x.TenantId).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Name).HasMaxLength(80).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(500).IsRequired();
        builder.Property(x => x.IsRequired).IsRequired();

        builder.HasIndex(x => new { x.TenantId, x.Name }).IsUnique();
    }
}
