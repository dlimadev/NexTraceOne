using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NexTraceOne.Configuration.Domain.Entities;

namespace NexTraceOne.Configuration.Infrastructure.Persistence.Configurations;

internal sealed class WebhookTemplateConfiguration : IEntityTypeConfiguration<WebhookTemplate>
{
    public void Configure(EntityTypeBuilder<WebhookTemplate> builder)
    {
        builder.ToTable("cfg_webhook_templates");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => new WebhookTemplateId(value));
        builder.Property(x => x.TenantId).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Name).HasMaxLength(100).IsRequired();
        builder.Property(x => x.EventType).HasMaxLength(50).IsRequired();
        builder.Property(x => x.PayloadTemplate).HasMaxLength(8000).IsRequired();
        builder.Property(x => x.HeadersJson).HasMaxLength(4000);
        builder.Property(x => x.IsEnabled).IsRequired();
        builder.Property(x => x.CreatedAt).HasColumnType("timestamp with time zone").IsRequired();

        builder.HasIndex(x => new { x.TenantId, x.EventType });
    }
}
