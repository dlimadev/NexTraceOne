using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NexTraceOne.Configuration.Domain.Entities;

namespace NexTraceOne.Configuration.Infrastructure.Persistence.Configurations;

internal sealed class TaxonomyValueConfiguration : IEntityTypeConfiguration<TaxonomyValue>
{
    public void Configure(EntityTypeBuilder<TaxonomyValue> builder)
    {
        builder.ToTable("cfg_taxonomy_values");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => new TaxonomyValueId(value));
        builder.Property(x => x.CategoryId)
            .HasConversion(id => id.Value, value => new TaxonomyCategoryId(value))
            .IsRequired();
        builder.Property(x => x.TenantId).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Label).HasMaxLength(100).IsRequired();
        builder.Property(x => x.SortOrder).IsRequired();

        builder.HasOne<TaxonomyCategory>()
            .WithMany()
            .HasForeignKey(x => x.CategoryId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => new { x.CategoryId, x.TenantId });
    }
}
