using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NexTraceOne.Governance.Domain.Entities;

namespace NexTraceOne.Governance.Infrastructure.Persistence.Configurations;

public sealed class PresenceSessionConfiguration : IEntityTypeConfiguration<PresenceSession>
{
    public void Configure(EntityTypeBuilder<PresenceSession> builder)
    {
        builder.ToTable("gov_presence_sessions");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasConversion(id => id.Value, v => new PresenceSessionId(v));
        builder.Property(x => x.TenantId).HasColumnName("tenant_id").HasMaxLength(100).IsRequired();
        builder.Property(x => x.ResourceType).HasMaxLength(50).IsRequired();
        builder.Property(x => x.ResourceId).IsRequired();
        builder.Property(x => x.UserId).HasMaxLength(100).IsRequired();
        builder.Property(x => x.DisplayName).HasMaxLength(100).IsRequired();
        builder.Property(x => x.AvatarColor).HasMaxLength(20).IsRequired();
        builder.Property(x => x.IsActive).IsRequired();
        builder.Property(x => x.JoinedAt).IsRequired();
        builder.Property(x => x.LastSeenAt).IsRequired();
        builder.Property(x => x.LeftAt);

        builder.HasIndex(new[] { "TenantId", "ResourceType", "ResourceId", "IsActive" })
            .HasDatabaseName("ix_gov_presence_resource_active");
        builder.HasIndex(new[] { "TenantId", "UserId", "IsActive" })
            .HasDatabaseName("ix_gov_presence_user_active");
    }
}
