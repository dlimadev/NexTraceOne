using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using NexTraceOne.BuildingBlocks.Observability.Telemetry.Models;

namespace NexTraceOne.OperationalIntelligence.Infrastructure.TelemetryStore.Persistence.Configurations;

internal sealed class AnomalySnapshotConfiguration : IEntityTypeConfiguration<AnomalySnapshot>
{
    public void Configure(EntityTypeBuilder<AnomalySnapshot> builder)
    {
        builder.ToTable("ops_ts_anomaly_snapshots");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.ServiceName).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Environment).HasMaxLength(100).IsRequired();
        builder.Property(x => x.AnomalyType).HasMaxLength(100).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(2000).IsRequired();
        builder.Property(x => x.MessageKey).HasMaxLength(500).IsRequired();

        builder.Property(x => x.Severity).IsRequired();
        builder.Property(x => x.ObservedValue).IsRequired();
        builder.Property(x => x.ExpectedValue).IsRequired();
        builder.Property(x => x.DeviationPercent).IsRequired();

        builder.Property(x => x.DetectedAt).HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(x => x.ResolvedAt).HasColumnType("timestamp with time zone");
        builder.Property(x => x.CreatedAt).HasColumnType("timestamp with time zone").IsRequired();

        builder.HasIndex(x => new { x.ServiceId, x.Environment, x.DetectedAt });
        builder.HasIndex(x => new { x.Severity, x.ResolvedAt });
    }
}
