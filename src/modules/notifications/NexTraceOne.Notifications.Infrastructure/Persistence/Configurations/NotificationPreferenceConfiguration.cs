using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using NexTraceOne.Notifications.Domain.Entities;
using NexTraceOne.Notifications.Domain.Enums;
using NexTraceOne.Notifications.Domain.StronglyTypedIds;

namespace NexTraceOne.Notifications.Infrastructure.Persistence.Configurations;

/// <summary>Configura o mapeamento da entidade NotificationPreference para a tabela ntf_preferences.</summary>
internal sealed class NotificationPreferenceConfiguration : IEntityTypeConfiguration<NotificationPreference>
{
    public void Configure(EntityTypeBuilder<NotificationPreference> builder)
    {
        builder.ToTable("ntf_preferences");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => new NotificationPreferenceId(value));

        builder.Property(x => x.TenantId).IsRequired();
        builder.Property(x => x.UserId).IsRequired();

        builder.Property(x => x.Category)
            .HasConversion<string>()
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.Channel)
            .HasConversion<string>()
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.Enabled).IsRequired();

        builder.Property(x => x.UpdatedAt)
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        // Índices para consultas frequentes e unicidade
        builder.HasIndex(x => new { x.TenantId, x.UserId, x.Category, x.Channel })
            .IsUnique();

        builder.HasIndex(x => x.TenantId);
        builder.HasIndex(x => x.UserId);
    }
}
