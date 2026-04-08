using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NexTraceOne.Configuration.Domain.Entities;

namespace NexTraceOne.Configuration.Infrastructure.Persistence.Configurations;

internal sealed class UserWatchConfiguration : IEntityTypeConfiguration<UserWatch>
{
    public void Configure(EntityTypeBuilder<UserWatch> builder)
    {
        builder.ToTable("cfg_user_watches");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => new UserWatchId(value));
        builder.Property(x => x.TenantId).HasMaxLength(200).IsRequired();
        builder.Property(x => x.UserId).HasMaxLength(200).IsRequired();
        builder.Property(x => x.EntityType).HasMaxLength(50).IsRequired();
        builder.Property(x => x.EntityId).HasMaxLength(256).IsRequired();
        builder.Property(x => x.NotifyLevel).HasMaxLength(20).IsRequired();
        builder.Property(x => x.CreatedAt).HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(x => x.UpdatedAt).HasColumnType("timestamp with time zone");

        builder.HasIndex(x => new { x.UserId, x.TenantId, x.EntityType, x.EntityId }).IsUnique();
    }
}
