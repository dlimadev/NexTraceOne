using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using NexTraceOne.BuildingBlocks.Observability.Telemetry.Models;

namespace NexTraceOne.OperationalIntelligence.Infrastructure.TelemetryStore.Persistence.Configurations;

internal sealed class DependencyMetricsSnapshotConfiguration : IEntityTypeConfiguration<DependencyMetricsSnapshot>
{
    public void Configure(EntityTypeBuilder<DependencyMetricsSnapshot> builder)
    {
        builder.ToTable("ops_ts_dependency_metrics");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.SourceServiceName).HasMaxLength(200).IsRequired();
        builder.Property(x => x.TargetServiceName).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Environment).HasMaxLength(100).IsRequired();
        builder.Property(x => x.AggregationLevel).HasColumnType("integer").IsRequired();

        builder.Property(x => x.IntervalStart).HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(x => x.IntervalEnd).HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(x => x.CreatedAt).HasColumnType("timestamp with time zone").IsRequired();

        builder.Property(x => x.CallCount).IsRequired();
        builder.Property(x => x.ErrorCount).IsRequired();
        builder.Property(x => x.ErrorRatePercent).IsRequired();
        builder.Property(x => x.LatencyAvgMs).IsRequired();
        builder.Property(x => x.LatencyP95Ms).IsRequired();
        builder.Property(x => x.LatencyP99Ms).IsRequired();

        builder.HasIndex(x => new { x.SourceServiceId, x.TargetServiceId, x.Environment, x.AggregationLevel, x.IntervalStart });
    }
}
