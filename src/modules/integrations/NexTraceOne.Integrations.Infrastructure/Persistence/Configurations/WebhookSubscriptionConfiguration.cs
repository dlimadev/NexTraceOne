using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using NexTraceOne.Integrations.Domain.Entities;

namespace NexTraceOne.Integrations.Infrastructure.Persistence.Configurations;

/// <summary>
/// Configuração EF Core para a entidade WebhookSubscription.
/// Define mapeamento de tabela, typed ID, JSONB para event types, e índices.
/// </summary>
internal sealed class WebhookSubscriptionConfiguration : IEntityTypeConfiguration<WebhookSubscription>
{
    public void Configure(EntityTypeBuilder<WebhookSubscription> builder)
    {
        builder.ToTable("int_webhook_subscriptions");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => new WebhookSubscriptionId(value));

        builder.Property(x => x.TenantId)
            .HasColumnName("tenant_id")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.Name)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.TargetUrl)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(x => x.EventTypes)
            .HasColumnType("jsonb")
            .HasConversion(
                types => System.Text.Json.JsonSerializer.Serialize(types, (System.Text.Json.JsonSerializerOptions?)null),
                json => System.Text.Json.JsonSerializer.Deserialize<List<string>>(json, (System.Text.Json.JsonSerializerOptions?)null) ?? new List<string>())
            .IsRequired();

        builder.Property(x => x.SecretHash)
            .HasMaxLength(500);

        builder.Property(x => x.Description)
            .HasMaxLength(500);

        builder.Property(x => x.IsActive)
            .IsRequired();

        builder.Property(x => x.DeliveryCount)
            .IsRequired();

        builder.Property(x => x.SuccessCount)
            .IsRequired();

        builder.Property(x => x.FailureCount)
            .IsRequired();

        builder.Property(x => x.LastTriggeredAt)
            .HasColumnType("timestamp with time zone");

        builder.Property(x => x.CreatedAt)
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(x => x.UpdatedAt)
            .HasColumnType("timestamp with time zone");

        // Indexes
        builder.HasIndex(x => x.TenantId);
        builder.HasIndex(x => new { x.TenantId, x.IsActive });
        builder.HasIndex(x => new { x.TenantId, x.Name }).IsUnique();

        // Concurrency token (PostgreSQL xmin)
        builder.Property(x => x.RowVersion).IsRowVersion();
    }
}
