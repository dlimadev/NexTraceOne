using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NexTraceOne.Configuration.Domain.Entities;

namespace NexTraceOne.Configuration.Infrastructure.Persistence.Configurations;

internal sealed class ChangeChecklistConfiguration : IEntityTypeConfiguration<ChangeChecklist>
{
    public void Configure(EntityTypeBuilder<ChangeChecklist> builder)
    {
        builder.ToTable("cfg_change_checklists");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => new ChangeChecklistId(value));
        builder.Property(x => x.TenantId).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Name).HasMaxLength(100).IsRequired();
        builder.Property(x => x.ChangeType).HasMaxLength(50).IsRequired();
        builder.Property(x => x.Environment).HasMaxLength(50);
        builder.Property(x => x.IsRequired).IsRequired();
        builder.PrimitiveCollection(x => x.Items).HasColumnType("text[]");

        builder.HasIndex(x => new { x.TenantId, x.ChangeType });
    }
}
