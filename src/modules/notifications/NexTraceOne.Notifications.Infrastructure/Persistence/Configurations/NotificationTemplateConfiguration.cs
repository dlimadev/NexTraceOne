using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using NexTraceOne.Notifications.Domain.Entities;
using NexTraceOne.Notifications.Domain.Enums;
using NexTraceOne.Notifications.Domain.StronglyTypedIds;

namespace NexTraceOne.Notifications.Infrastructure.Persistence.Configurations;

/// <summary>
/// Configura o mapeamento da entidade NotificationTemplate para a tabela ntf_templates.
/// Templates persistidos substituem os templates em memória quando presentes.
/// </summary>
internal sealed class NotificationTemplateConfiguration : IEntityTypeConfiguration<NotificationTemplate>
{
    public void Configure(EntityTypeBuilder<NotificationTemplate> builder)
    {
        builder.ToTable("ntf_templates");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => new NotificationTemplateId(value));

        builder.Property(x => x.TenantId).IsRequired();

        builder.Property(x => x.EventType)
            .HasMaxLength(300)
            .IsRequired();

        builder.Property(x => x.Name)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(x => x.SubjectTemplate)
            .HasMaxLength(1000)
            .IsRequired();

        builder.Property(x => x.BodyTemplate)
            .HasColumnType("text")
            .IsRequired();

        builder.Property(x => x.PlainTextTemplate)
            .HasColumnType("text");

        builder.Property(x => x.Channel)
            .HasConversion<string>()
            .HasMaxLength(100);

        builder.Property(x => x.Locale)
            .HasMaxLength(10)
            .IsRequired();

        builder.Property(x => x.IsActive).IsRequired();
        builder.Property(x => x.IsBuiltIn).IsRequired();

        builder.Property(x => x.CreatedAt)
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(x => x.UpdatedAt)
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        // Índices para resolução eficiente de templates
        builder.HasIndex(x => new { x.TenantId, x.EventType, x.Channel, x.Locale });
        builder.HasIndex(x => x.TenantId);
        builder.HasIndex(x => x.EventType);
        builder.HasIndex(x => x.IsActive);
    }
}
