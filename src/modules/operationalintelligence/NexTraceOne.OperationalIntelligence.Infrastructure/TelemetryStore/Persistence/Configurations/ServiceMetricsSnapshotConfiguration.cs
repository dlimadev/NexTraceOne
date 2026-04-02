using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using NexTraceOne.BuildingBlocks.Observability.Telemetry.Models;

namespace NexTraceOne.OperationalIntelligence.Infrastructure.TelemetryStore.Persistence.Configurations;

internal sealed class ServiceMetricsSnapshotConfiguration : IEntityTypeConfiguration<ServiceMetricsSnapshot>
{
    public void Configure(EntityTypeBuilder<ServiceMetricsSnapshot> builder)
    {
        builder.ToTable("ops_ts_service_metrics");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.ServiceName).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Environment).HasMaxLength(100).IsRequired();
        builder.Property(x => x.AggregationLevel).HasColumnType("integer").IsRequired();

        builder.Property(x => x.IntervalStart).HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(x => x.IntervalEnd).HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(x => x.CreatedAt).HasColumnType("timestamp with time zone").IsRequired();

        builder.Property(x => x.RequestCount).IsRequired();
        builder.Property(x => x.RequestsPerMinute).IsRequired();
        builder.Property(x => x.RequestsPerHour).IsRequired();
        builder.Property(x => x.ErrorCount).IsRequired();
        builder.Property(x => x.ErrorRatePercent).IsRequired();
        builder.Property(x => x.LatencyAvgMs).IsRequired();
        builder.Property(x => x.LatencyP50Ms).IsRequired();
        builder.Property(x => x.LatencyP95Ms).IsRequired();
        builder.Property(x => x.LatencyP99Ms).IsRequired();
        builder.Property(x => x.LatencyMaxMs).IsRequired();

        builder.HasIndex(x => new { x.ServiceId, x.Environment, x.AggregationLevel, x.IntervalStart });
        builder.HasIndex(x => new { x.ServiceName, x.Environment, x.IntervalStart });
    }
}
