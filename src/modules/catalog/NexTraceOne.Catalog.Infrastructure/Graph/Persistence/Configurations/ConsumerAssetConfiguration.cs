using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NexTraceOne.Catalog.Domain.Graph.Entities;

namespace NexTraceOne.Catalog.Infrastructure.Graph.Persistence.Configurations;

internal sealed class ConsumerAssetConfiguration : IEntityTypeConfiguration<ConsumerAsset>
{
    public void Configure(EntityTypeBuilder<ConsumerAsset> builder)
    {
        builder.ToTable("cat_consumer_assets");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => ConsumerAssetId.From(value));
        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Kind).HasMaxLength(100).IsRequired();
        builder.Property(x => x.Environment).HasMaxLength(100).IsRequired();
    }
}
