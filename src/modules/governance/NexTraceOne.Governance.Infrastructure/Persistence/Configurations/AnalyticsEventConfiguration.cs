using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using NexTraceOne.Governance.Domain.Entities;

namespace NexTraceOne.Governance.Infrastructure.Persistence.Configurations;

/// <summary>
/// Configuração EF Core para a entidade AnalyticsEvent.
/// Eventos mínimos de Product Analytics.
/// </summary>
internal sealed class AnalyticsEventConfiguration : IEntityTypeConfiguration<AnalyticsEvent>
{
    public void Configure(EntityTypeBuilder<AnalyticsEvent> builder)
    {
        builder.ToTable("gov_analytics_events");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => new AnalyticsEventId(value));

        builder.Property(x => x.TenantId)
            .HasColumnType("uuid")
            .IsRequired();

        builder.Property(x => x.UserId)
            .HasMaxLength(200);

        builder.Property(x => x.Persona)
            .HasMaxLength(50);

        builder.Property(x => x.Module)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.EventType)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.Feature)
            .HasMaxLength(200);

        builder.Property(x => x.EntityType)
            .HasMaxLength(100);

        builder.Property(x => x.Outcome)
            .HasMaxLength(200);

        builder.Property(x => x.Route)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(x => x.TeamId)
            .HasMaxLength(200);

        builder.Property(x => x.DomainId)
            .HasMaxLength(200);

        builder.Property(x => x.SessionId)
            .HasMaxLength(200);

        builder.Property(x => x.ClientType)
            .HasMaxLength(50);

        builder.Property(x => x.MetadataJson)
            .HasColumnType("text");

        builder.Property(x => x.OccurredAt)
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.HasIndex(x => x.OccurredAt);
        builder.HasIndex(x => x.Module);
        builder.HasIndex(x => x.EventType);
        builder.HasIndex(x => x.Persona);
        builder.HasIndex(x => x.UserId);
    }
}
