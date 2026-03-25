using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NexTraceOne.Catalog.Domain.SourceOfTruth.Entities;
using NexTraceOne.Catalog.Domain.SourceOfTruth.Enums;

namespace NexTraceOne.Catalog.Infrastructure.Graph.Persistence.Configurations;

internal sealed class LinkedReferenceConfiguration : IEntityTypeConfiguration<LinkedReference>
{
    public void Configure(EntityTypeBuilder<LinkedReference> builder)
    {
        builder.ToTable("cat_linked_references");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => LinkedReferenceId.From(value));
        builder.Property(x => x.AssetId).IsRequired();
        builder.Property(x => x.AssetType).HasMaxLength(50).IsRequired();
        builder.Property(x => x.ReferenceType).HasMaxLength(50).IsRequired();
        builder.Property(x => x.Title).HasMaxLength(500).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(4000);
        builder.Property(x => x.Url).HasMaxLength(2000);
        builder.Property(x => x.Content);
        builder.Property(x => x.Metadata);
        builder.Property(x => x.IsActive).IsRequired();

        builder.HasIndex(x => new { x.AssetId, x.AssetType });
        builder.HasIndex(x => x.ReferenceType);
    }
}
