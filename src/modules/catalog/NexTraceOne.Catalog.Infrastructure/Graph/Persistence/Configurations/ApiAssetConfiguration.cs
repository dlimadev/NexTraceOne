using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NexTraceOne.Catalog.Domain.Graph.Entities;

namespace NexTraceOne.Catalog.Infrastructure.Graph.Persistence.Configurations;

internal sealed class ApiAssetConfiguration : IEntityTypeConfiguration<ApiAsset>
{
    public void Configure(EntityTypeBuilder<ApiAsset> builder)
    {
        builder.ToTable("eg_api_assets");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => ApiAssetId.From(value));
        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
        builder.Property(x => x.RoutePattern).HasMaxLength(500).IsRequired();
        builder.Property(x => x.Version).HasMaxLength(50).IsRequired();
        builder.Property(x => x.Visibility).HasMaxLength(50).IsRequired();

        builder.HasOne(x => x.OwnerService)
            .WithMany()
            .HasForeignKey("OwnerServiceId")
            .IsRequired()
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(x => x.ConsumerRelationships)
            .WithOne()
            .HasForeignKey("ApiAssetId")
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.DiscoverySources)
            .WithOne()
            .HasForeignKey("ApiAssetId")
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);
    }
}
