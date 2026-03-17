using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using NexTraceOne.Catalog.Domain.Portal.Entities;

namespace NexTraceOne.Catalog.Infrastructure.Portal.Persistence.Configurations;

/// <summary>
/// Configuração EF Core para a entidade PortalAnalyticsEvent.
/// Mapeia eventos de analytics do portal com índices otimizados para consultas de agregação.
/// </summary>
internal sealed class PortalAnalyticsEventConfiguration : IEntityTypeConfiguration<PortalAnalyticsEvent>
{
    public void Configure(EntityTypeBuilder<PortalAnalyticsEvent> builder)
    {
        builder.ToTable("dp_portal_analytics_events");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => PortalAnalyticsEventId.From(value));

        builder.Property(x => x.UserId);
        builder.Property(x => x.EventType).HasMaxLength(100).IsRequired();
        builder.Property(x => x.EntityId).HasMaxLength(200);
        builder.Property(x => x.EntityType).HasMaxLength(100);
        builder.Property(x => x.SearchQuery).HasMaxLength(500);
        builder.Property(x => x.ZeroResults);
        builder.Property(x => x.DurationMs);
        builder.Property(x => x.Metadata).HasColumnType("text");
        builder.Property(x => x.OccurredAt).HasColumnType("timestamp with time zone").IsRequired();

        builder.HasIndex(x => x.EventType);
        builder.HasIndex(x => x.OccurredAt);
        builder.HasIndex(x => x.UserId);
    }
}
