using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NexTraceOne.Catalog.Domain.Graph.Entities;

namespace NexTraceOne.Catalog.Infrastructure.Graph.Persistence.Configurations;

internal sealed class DiscoverySourceConfiguration : IEntityTypeConfiguration<DiscoverySource>
{
    public void Configure(EntityTypeBuilder<DiscoverySource> builder)
    {
        builder.ToTable("cat_discovery_sources");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => DiscoverySourceId.From(value));
        builder.Property(x => x.SourceType).HasMaxLength(100).IsRequired();
        builder.Property(x => x.ExternalReference).HasMaxLength(500).IsRequired();
        builder.Property(x => x.DiscoveredAt).IsRequired();
        builder.Property(x => x.ConfidenceScore).HasPrecision(5, 4).IsRequired();
    }
}
