using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using NexTraceOne.Notifications.Domain.Entities;
using NexTraceOne.Notifications.Domain.Enums;
using NexTraceOne.Notifications.Domain.StronglyTypedIds;

namespace NexTraceOne.Notifications.Infrastructure.Persistence.Configurations;

/// <summary>
/// Configura o mapeamento da entidade NotificationDelivery para a tabela ntf_deliveries.
/// Regista tentativas de entrega externa com rastreabilidade completa.
/// </summary>
internal sealed class NotificationDeliveryConfiguration : IEntityTypeConfiguration<NotificationDelivery>
{
    public void Configure(EntityTypeBuilder<NotificationDelivery> builder)
    {
        builder.ToTable("ntf_deliveries", t =>
        {
            t.HasCheckConstraint(
                "CK_ntf_deliveries_status",
                "\"Status\" IN ('Pending', 'Delivered', 'Failed', 'Skipped')");
            t.HasCheckConstraint(
                "CK_ntf_deliveries_channel",
                "\"Channel\" IN ('InApp', 'Email', 'MicrosoftTeams')");
        });
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => new NotificationDeliveryId(value));

        builder.Property(x => x.NotificationId)
            .HasConversion(id => id.Value, value => new NotificationId(value))
            .IsRequired();

        builder.Property(x => x.Channel)
            .HasConversion<string>()
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.RecipientAddress)
            .HasMaxLength(500);

        builder.Property(x => x.Status)
            .HasConversion<string>()
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.CreatedAt)
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(x => x.DeliveredAt)
            .HasColumnType("timestamp with time zone");

        builder.Property(x => x.FailedAt)
            .HasColumnType("timestamp with time zone");

        builder.Property(x => x.ErrorMessage)
            .HasMaxLength(4000);

        builder.Property(x => x.RetryCount)
            .IsRequired();

        // Índices para consultas frequentes
        builder.HasIndex(x => x.NotificationId);
        builder.HasIndex(x => x.Status);
        builder.HasIndex(x => x.Channel);
        builder.HasIndex(x => new { x.Status, x.RetryCount });

        // ── FK → Notification ────────────────────────────────────────────────
        builder.HasOne<Notification>()
            .WithMany()
            .HasForeignKey(x => x.NotificationId)
            .OnDelete(DeleteBehavior.Cascade);

        // ── Concorrência otimista (PostgreSQL xmin) ──────────────────────────
        builder.Property(x => x.RowVersion).IsRowVersion();
    }
}
