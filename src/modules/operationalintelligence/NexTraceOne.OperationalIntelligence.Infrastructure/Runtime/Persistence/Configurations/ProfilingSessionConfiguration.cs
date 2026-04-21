using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NexTraceOne.OperationalIntelligence.Domain.Runtime.Entities;

namespace NexTraceOne.OperationalIntelligence.Infrastructure.Runtime.Persistence.Configurations;

/// <summary>EF Core configuration for <see cref="ProfilingSession"/>.</summary>
internal sealed class ProfilingSessionConfiguration : IEntityTypeConfiguration<ProfilingSession>
{
    public void Configure(EntityTypeBuilder<ProfilingSession> builder)
    {
        builder.ToTable("ops_profiling_sessions");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => ProfilingSessionId.From(value));

        builder.Property(x => x.TenantId).HasMaxLength(100).IsRequired();
        builder.Property(x => x.ServiceName).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Environment).HasMaxLength(100).IsRequired();
        builder.Property(x => x.FrameType).HasColumnType("integer").IsRequired();
        builder.Property(x => x.WindowStart).HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(x => x.WindowEnd).HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(x => x.DurationSeconds).IsRequired();
        builder.Property(x => x.TotalCpuSamples).IsRequired();
        builder.Property(x => x.PeakMemoryMb)
            .HasColumnType("numeric(10,2)")
            .HasPrecision(10, 2)
            .IsRequired();
        builder.Property(x => x.PeakThreadCount).IsRequired();
        builder.Property(x => x.TopFramesJson).HasMaxLength(50000);
        builder.Property(x => x.RawDataUri).HasMaxLength(2000);
        builder.Property(x => x.RawDataHash).HasMaxLength(128);
        builder.Property(x => x.ReleaseVersion).HasMaxLength(50);
        builder.Property(x => x.CommitSha).HasMaxLength(100);
        builder.Property(x => x.HasAnomalies).IsRequired();

        // ── Auditoria ────────────────────────────────────────────────────────
        builder.Property(x => x.CreatedAt).HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(x => x.CreatedBy).HasMaxLength(200).IsRequired();
        builder.Property(x => x.UpdatedAt).HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(x => x.UpdatedBy).HasMaxLength(200).IsRequired();
        builder.Property(x => x.IsDeleted).HasDefaultValue(false).IsRequired();

        // ── Índices ──────────────────────────────────────────────────────────
        builder.HasIndex(x => new { x.ServiceName, x.Environment, x.WindowStart })
            .HasDatabaseName("ix_ops_profiling_sessions_service_env_window");
        builder.HasIndex(x => x.TenantId)
            .HasDatabaseName("ix_ops_profiling_sessions_tenant_id");
        builder.HasIndex(x => x.HasAnomalies)
            .HasDatabaseName("ix_ops_profiling_sessions_has_anomalies")
            .HasFilter("\"HasAnomalies\" = true");

        // ── Soft-delete global filter ────────────────────────────────────────
        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}
