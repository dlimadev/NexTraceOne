using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using NexTraceOne.BuildingBlocks.Observability.Telemetry.Models;

namespace NexTraceOne.OperationalIntelligence.Infrastructure.TelemetryStore.Persistence.Configurations;

internal sealed class ObservedTopologyEntryConfiguration : IEntityTypeConfiguration<ObservedTopologyEntry>
{
    public void Configure(EntityTypeBuilder<ObservedTopologyEntry> builder)
    {
        builder.ToTable("ops_ts_observed_topology");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.SourceServiceName).HasMaxLength(200).IsRequired();
        builder.Property(x => x.TargetServiceName).HasMaxLength(200).IsRequired();
        builder.Property(x => x.CommunicationType).HasMaxLength(50).IsRequired();
        builder.Property(x => x.Environment).HasMaxLength(100).IsRequired();

        builder.Property(x => x.ConfidenceScore).IsRequired();
        builder.Property(x => x.FirstSeenAt).HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(x => x.LastSeenAt).HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(x => x.TotalCallCount).IsRequired();
        builder.Property(x => x.IsShadowDependency).IsRequired();
        builder.Property(x => x.UpdatedAt).HasColumnType("timestamp with time zone").IsRequired();

        builder.HasIndex(x => new { x.SourceServiceId, x.TargetServiceId, x.Environment, x.CommunicationType })
            .IsUnique();

        builder.HasIndex(x => new { x.Environment, x.IsShadowDependency });
    }
}
