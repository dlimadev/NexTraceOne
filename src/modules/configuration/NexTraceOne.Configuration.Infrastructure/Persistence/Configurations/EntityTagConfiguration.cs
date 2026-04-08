using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NexTraceOne.BuildingBlocks.Core.Tags;

namespace NexTraceOne.Configuration.Infrastructure.Persistence.Configurations;

internal sealed class EntityTagConfiguration : IEntityTypeConfiguration<EntityTag>
{
    public void Configure(EntityTypeBuilder<EntityTag> builder)
    {
        builder.ToTable("cfg_entity_tags");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => new EntityTagId(value));
        builder.Property(x => x.TenantId).HasMaxLength(200).IsRequired();
        builder.Property(x => x.EntityType).HasMaxLength(50).IsRequired();
        builder.Property(x => x.EntityId).HasMaxLength(256).IsRequired();
        builder.Property(x => x.Key).HasMaxLength(50).IsRequired();
        builder.Property(x => x.Value).HasMaxLength(100).IsRequired();

        builder.HasIndex(x => new { x.TenantId, x.EntityType, x.EntityId, x.Key }).IsUnique();
        builder.HasIndex(x => new { x.TenantId, x.Key });
    }
}
