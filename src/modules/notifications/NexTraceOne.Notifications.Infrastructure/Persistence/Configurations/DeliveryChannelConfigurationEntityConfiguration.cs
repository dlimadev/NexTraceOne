using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using NexTraceOne.Notifications.Domain.Entities;
using NexTraceOne.Notifications.Domain.Enums;
using NexTraceOne.Notifications.Domain.StronglyTypedIds;

namespace NexTraceOne.Notifications.Infrastructure.Persistence.Configurations;

/// <summary>
/// Configura o mapeamento da entidade DeliveryChannelConfiguration para a tabela ntf_channel_configurations.
/// Cada tenant pode ter uma configuração por tipo de canal de entrega.
/// </summary>
internal sealed class DeliveryChannelConfigurationEntityConfiguration
    : IEntityTypeConfiguration<DeliveryChannelConfiguration>
{
    public void Configure(EntityTypeBuilder<DeliveryChannelConfiguration> builder)
    {
        builder.ToTable("ntf_channel_configurations");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => new DeliveryChannelConfigurationId(value));

        builder.Property(x => x.TenantId).IsRequired();

        builder.Property(x => x.ChannelType)
            .HasConversion<string>()
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.DisplayName)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.IsEnabled).IsRequired();

        builder.Property(x => x.ConfigurationJson)
            .HasColumnType("jsonb");

        builder.Property(x => x.CreatedAt)
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(x => x.UpdatedAt)
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        // Unicidade: cada tenant pode ter apenas uma configuração por tipo de canal
        builder.HasIndex(x => new { x.TenantId, x.ChannelType })
            .IsUnique();

        builder.HasIndex(x => x.TenantId);
    }
}
