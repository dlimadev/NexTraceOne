using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using NexTraceOne.Notifications.Domain.Entities;
using NexTraceOne.Notifications.Domain.Enums;
using NexTraceOne.Notifications.Domain.StronglyTypedIds;

namespace NexTraceOne.Notifications.Infrastructure.Persistence.Configurations;

/// <summary>Configura o mapeamento da entidade Notification para a tabela ntf_notifications.</summary>
internal sealed class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.ToTable("ntf_notifications");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => new NotificationId(value));

        builder.Property(x => x.TenantId).IsRequired();
        builder.Property(x => x.RecipientUserId).IsRequired();
        builder.Property(x => x.EventType).HasMaxLength(300).IsRequired();
        builder.Property(x => x.Category).HasConversion<string>().HasMaxLength(100).IsRequired();
        builder.Property(x => x.Severity).HasConversion<string>().HasMaxLength(100).IsRequired();
        builder.Property(x => x.Title).HasMaxLength(500).IsRequired();
        builder.Property(x => x.Message).HasMaxLength(4000).IsRequired();
        builder.Property(x => x.SourceModule).HasMaxLength(200).IsRequired();
        builder.Property(x => x.SourceEntityType).HasMaxLength(200);
        builder.Property(x => x.SourceEntityId).HasMaxLength(500);
        builder.Property(x => x.EnvironmentId);
        builder.Property(x => x.ActionUrl).HasMaxLength(2000);
        builder.Property(x => x.RequiresAction).IsRequired();
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(100).IsRequired();
        builder.Property(x => x.PayloadJson).HasColumnType("text");
        builder.Property(x => x.CreatedAt).HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(x => x.ReadAt).HasColumnType("timestamp with time zone");
        builder.Property(x => x.AcknowledgedAt).HasColumnType("timestamp with time zone");
        builder.Property(x => x.ArchivedAt).HasColumnType("timestamp with time zone");
        builder.Property(x => x.ExpiresAt).HasColumnType("timestamp with time zone");

        builder.HasIndex(x => x.TenantId);
        builder.HasIndex(x => x.RecipientUserId);
        builder.HasIndex(x => x.Status);
        builder.HasIndex(x => x.CreatedAt);
        builder.HasIndex(x => new { x.RecipientUserId, x.Status });
    }
}
