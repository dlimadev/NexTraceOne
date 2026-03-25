using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using NexTraceOne.OperationalIntelligence.Domain.Runtime.Entities;
using NexTraceOne.OperationalIntelligence.Domain.Runtime.Enums;

namespace NexTraceOne.OperationalIntelligence.Infrastructure.Runtime.Persistence.Configurations;

/// <summary>EF Core configuration for <see cref="RuntimeSnapshot"/>.</summary>
internal sealed class RuntimeSnapshotConfiguration : IEntityTypeConfiguration<RuntimeSnapshot>
{
    public void Configure(EntityTypeBuilder<RuntimeSnapshot> builder)
    {
        builder.ToTable("ops_runtime_snapshots", t =>
        {
            t.HasCheckConstraint("CK_ops_runtime_snapshots_health", "\"HealthStatus\" >= 0 AND \"HealthStatus\" <= 3");
        });

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => RuntimeSnapshotId.From(value));

        builder.Property(x => x.ServiceName).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Environment).HasMaxLength(100).IsRequired();
        builder.Property(x => x.Source).HasMaxLength(100).IsRequired();

        builder.Property(x => x.AvgLatencyMs).IsRequired();
        builder.Property(x => x.P99LatencyMs).IsRequired();
        builder.Property(x => x.ErrorRate).IsRequired();
        builder.Property(x => x.RequestsPerSecond).IsRequired();
        builder.Property(x => x.CpuUsagePercent).IsRequired();
        builder.Property(x => x.MemoryUsageMb).IsRequired();
        builder.Property(x => x.ActiveInstances).IsRequired();

        builder.Property(x => x.HealthStatus).HasColumnType("integer").IsRequired();
        builder.Property(x => x.CapturedAt).HasColumnType("timestamp with time zone").IsRequired();

        builder.HasIndex(x => new { x.ServiceName, x.Environment, x.CapturedAt });
        builder.HasIndex(x => x.HealthStatus);

        // ── Concorrência otimista (PostgreSQL xmin) ──────────────────────────
        builder.Property(x => x.RowVersion).IsRowVersion();
    }
}
