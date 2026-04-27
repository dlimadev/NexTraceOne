using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NexTraceOne.Governance.Domain.Entities;

namespace NexTraceOne.Governance.Infrastructure.Persistence.Configurations;

/// <summary>Configuração EF Core para DashboardUsageEvent (V3.6).</summary>
internal sealed class DashboardUsageEventConfiguration : IEntityTypeConfiguration<DashboardUsageEvent>
{
    public void Configure(EntityTypeBuilder<DashboardUsageEvent> builder)
    {
        builder.ToTable("gov_dashboard_usage_events");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => new DashboardUsageEventId(value));

        builder.Property(x => x.TenantId)
            .HasColumnName("tenant_id")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.UserId).HasMaxLength(100);
        builder.Property(x => x.Persona).HasMaxLength(50);
        builder.Property(x => x.EventType).HasMaxLength(20).HasDefaultValue("view");

        builder.HasIndex(x => new { x.DashboardId, x.TenantId })
            .HasDatabaseName("ix_gov_dash_usage_dashboard_tenant");

        builder.HasIndex(x => x.OccurredAt)
            .HasDatabaseName("ix_gov_dash_usage_occurred_at");

        builder.HasIndex(x => new { x.TenantId, x.EventType })
            .HasDatabaseName("ix_gov_dash_usage_tenant_type");
    }
}
