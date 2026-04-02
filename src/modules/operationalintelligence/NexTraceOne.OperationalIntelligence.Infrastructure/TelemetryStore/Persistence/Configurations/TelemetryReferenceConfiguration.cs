using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using NexTraceOne.BuildingBlocks.Observability.Telemetry.Models;

namespace NexTraceOne.OperationalIntelligence.Infrastructure.TelemetryStore.Persistence.Configurations;

internal sealed class TelemetryReferenceConfiguration : IEntityTypeConfiguration<TelemetryReference>
{
    public void Configure(EntityTypeBuilder<TelemetryReference> builder)
    {
        builder.ToTable("ops_ts_telemetry_references");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.SignalType).HasColumnType("integer").IsRequired();
        builder.Property(x => x.ExternalId).HasMaxLength(500).IsRequired();
        builder.Property(x => x.BackendType).HasMaxLength(50).IsRequired();
        builder.Property(x => x.AccessUri).HasMaxLength(4000);
        builder.Property(x => x.ServiceName).HasMaxLength(200);
        builder.Property(x => x.Environment).HasMaxLength(100);

        builder.Property(x => x.OriginalTimestamp).HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(x => x.CreatedAt).HasColumnType("timestamp with time zone").IsRequired();

        builder.HasIndex(x => x.CorrelationId);
        builder.HasIndex(x => new { x.ServiceId, x.SignalType, x.OriginalTimestamp });
    }
}
