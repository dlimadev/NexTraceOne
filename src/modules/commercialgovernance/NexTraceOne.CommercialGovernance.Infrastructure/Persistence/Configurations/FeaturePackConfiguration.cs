using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NexTraceOne.CommercialCatalog.Domain.Entities;

namespace NexTraceOne.Licensing.Infrastructure.Persistence.Configurations;

internal sealed class FeaturePackConfiguration : IEntityTypeConfiguration<FeaturePack>
{
    public void Configure(EntityTypeBuilder<FeaturePack> builder)
    {
        builder.ToTable("cc_feature_packs");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => FeaturePackId.From(value));
        builder.Property(x => x.Code).HasMaxLength(100).IsRequired();
        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(2000);
        builder.Property(x => x.IsActive).IsRequired();

        builder.HasIndex(x => x.Code).IsUnique();

        builder.HasMany(x => x.Items)
            .WithOne()
            .HasForeignKey(x => x.FeaturePackId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(x => x.Items)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
