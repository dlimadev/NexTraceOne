using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using NexTraceOne.Catalog.Domain.Portal.Entities;

namespace NexTraceOne.Catalog.Infrastructure.Portal.Persistence.Configurations;

/// <summary>
/// Configuração EF Core para a entidade Subscription.
/// Mapeia propriedades, conversões de ID fortemente tipado, índices e restrições.
/// </summary>
internal sealed class SubscriptionConfiguration : IEntityTypeConfiguration<Subscription>
{
    public void Configure(EntityTypeBuilder<Subscription> builder)
    {
        builder.ToTable("dp_subscriptions");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => SubscriptionId.From(value));

        builder.Property(x => x.ApiAssetId).IsRequired();
        builder.Property(x => x.ApiName).HasMaxLength(200).IsRequired();
        builder.Property(x => x.SubscriberId).IsRequired();
        builder.Property(x => x.SubscriberEmail).HasMaxLength(320).IsRequired();
        builder.Property(x => x.ConsumerServiceName).HasMaxLength(200).IsRequired();
        builder.Property(x => x.ConsumerServiceVersion).HasMaxLength(50).IsRequired();
        builder.Property(x => x.Level).HasColumnType("integer").IsRequired();
        builder.Property(x => x.Channel).HasColumnType("integer").IsRequired();
        builder.Property(x => x.WebhookUrl).HasMaxLength(2000);
        builder.Property(x => x.IsActive).IsRequired();
        builder.Property(x => x.CreatedAt).HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(x => x.LastNotifiedAt).HasColumnType("timestamp with time zone");

        builder.HasIndex(x => new { x.ApiAssetId, x.SubscriberId }).IsUnique();
        builder.HasIndex(x => x.SubscriberId);
        builder.HasIndex(x => x.ApiAssetId);
    }
}
