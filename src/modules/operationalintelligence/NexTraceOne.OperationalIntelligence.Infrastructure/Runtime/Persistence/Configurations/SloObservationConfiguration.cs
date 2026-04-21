using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NexTraceOne.OperationalIntelligence.Domain.Runtime.Entities;

namespace NexTraceOne.OperationalIntelligence.Infrastructure.Runtime.Persistence.Configurations;

/// <summary>EF Core configuration for <see cref="SloObservation"/>.</summary>
internal sealed class SloObservationConfiguration : IEntityTypeConfiguration<SloObservation>
{
    public void Configure(EntityTypeBuilder<SloObservation> builder)
    {
        builder.ToTable("ops_slo_observations");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => SloObservationId.From(value));

        builder.Property(x => x.TenantId).HasMaxLength(100).IsRequired();
        builder.Property(x => x.ServiceName).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Environment).HasMaxLength(100).IsRequired();
        builder.Property(x => x.MetricName).HasMaxLength(200).IsRequired();
        builder.Property(x => x.ObservedValue)
            .HasColumnType("numeric(18,6)")
            .HasPrecision(18, 6)
            .IsRequired();
        builder.Property(x => x.SloTarget)
            .HasColumnType("numeric(18,6)")
            .HasPrecision(18, 6)
            .IsRequired();
        builder.Property(x => x.Unit).HasMaxLength(50).IsRequired();
        builder.Property(x => x.Status).HasColumnType("integer").IsRequired();
        builder.Property(x => x.PeriodStart).HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(x => x.PeriodEnd).HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(x => x.ObservedAt).HasColumnType("timestamp with time zone").IsRequired();

        // ── Auditoria ────────────────────────────────────────────────────────
        builder.Property(x => x.CreatedAt).HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(x => x.CreatedBy).HasMaxLength(200).IsRequired();
        builder.Property(x => x.UpdatedAt).HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(x => x.UpdatedBy).HasMaxLength(200).IsRequired();
        builder.Property(x => x.IsDeleted).HasDefaultValue(false).IsRequired();

        // ── Índices ──────────────────────────────────────────────────────────
        builder.HasIndex(x => new { x.TenantId, x.ServiceName, x.PeriodStart })
            .HasDatabaseName("ix_ops_slo_obs_tenant_service_period");
        builder.HasIndex(x => new { x.TenantId, x.Status })
            .HasDatabaseName("ix_ops_slo_obs_tenant_status");
        builder.HasIndex(x => x.ObservedAt)
            .HasDatabaseName("ix_ops_slo_obs_observed_at");

        // ── Soft-delete global filter ────────────────────────────────────────
        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}
