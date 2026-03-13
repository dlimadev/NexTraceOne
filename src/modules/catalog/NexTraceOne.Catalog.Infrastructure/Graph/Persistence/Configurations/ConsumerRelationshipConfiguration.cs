using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NexTraceOne.EngineeringGraph.Domain.Entities;

namespace NexTraceOne.EngineeringGraph.Infrastructure.Persistence.Configurations;

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
