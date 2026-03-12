using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NexTraceOne.EngineeringGraph.Domain.Entities;

namespace NexTraceOne.EngineeringGraph.Infrastructure.Persistence.Configurations;

internal sealed class ServiceAssetConfiguration : IEntityTypeConfiguration<ServiceAsset>
{
    public void Configure(EntityTypeBuilder<ServiceAsset> builder)
    {
        builder.ToTable("eg_service_assets");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => ServiceAssetId.From(value));
        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Domain).HasMaxLength(200).IsRequired();
        builder.Property(x => x.TeamName).HasMaxLength(200).IsRequired();
        builder.HasIndex(x => x.Name).IsUnique();
    }
}

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

internal sealed class ConsumerAssetConfiguration : IEntityTypeConfiguration<ConsumerAsset>
{
    public void Configure(EntityTypeBuilder<ConsumerAsset> builder)
    {
        builder.ToTable("eg_consumer_assets");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => ConsumerAssetId.From(value));
        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Kind).HasMaxLength(100).IsRequired();
        builder.Property(x => x.Environment).HasMaxLength(100).IsRequired();
    }
}

internal sealed class ConsumerRelationshipConfiguration : IEntityTypeConfiguration<ConsumerRelationship>
{
    public void Configure(EntityTypeBuilder<ConsumerRelationship> builder)
    {
        builder.ToTable("eg_consumer_relationships");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => ConsumerRelationshipId.From(value));
        builder.Property(x => x.ConsumerAssetId)
            .HasConversion(id => id.Value, value => ConsumerAssetId.From(value))
            .IsRequired();
        builder.Property(x => x.ConsumerName).HasMaxLength(200).IsRequired();
        builder.Property(x => x.SourceType).HasMaxLength(100).IsRequired();
        builder.Property(x => x.ConfidenceScore).HasPrecision(5, 4).IsRequired();
        builder.Property(x => x.FirstObservedAt).IsRequired();
        builder.Property(x => x.LastObservedAt).IsRequired();
    }
}

internal sealed class DiscoverySourceConfiguration : IEntityTypeConfiguration<DiscoverySource>
{
    public void Configure(EntityTypeBuilder<DiscoverySource> builder)
    {
        builder.ToTable("eg_discovery_sources");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => DiscoverySourceId.From(value));
        builder.Property(x => x.SourceType).HasMaxLength(100).IsRequired();
        builder.Property(x => x.ExternalReference).HasMaxLength(500).IsRequired();
        builder.Property(x => x.DiscoveredAt).IsRequired();
        builder.Property(x => x.ConfidenceScore).HasPrecision(5, 4).IsRequired();
    }
}
