using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NexTraceOne.CommercialCatalog.Domain.Entities;

namespace NexTraceOne.Licensing.Infrastructure.Persistence.Configurations;

internal sealed class FeaturePackItemConfiguration : IEntityTypeConfiguration<FeaturePackItem>
{
    public void Configure(EntityTypeBuilder<FeaturePackItem> builder)
    {
        builder.ToTable("cc_feature_pack_items");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => FeaturePackItemId.From(value));
        builder.Property(x => x.FeaturePackId)
            .HasConversion(id => id.Value, value => FeaturePackId.From(value))
            .IsRequired();
        builder.Property(x => x.CapabilityCode).HasMaxLength(200).IsRequired();
        builder.Property(x => x.CapabilityName).HasMaxLength(200).IsRequired();

        builder.HasIndex(x => new { x.FeaturePackId, x.CapabilityCode }).IsUnique();
    }
}
