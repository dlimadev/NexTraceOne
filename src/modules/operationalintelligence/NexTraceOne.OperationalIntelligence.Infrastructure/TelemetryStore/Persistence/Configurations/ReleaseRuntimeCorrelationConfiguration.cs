using System.Text.Json;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using NexTraceOne.BuildingBlocks.Observability.Telemetry.Models;

namespace NexTraceOne.OperationalIntelligence.Infrastructure.TelemetryStore.Persistence.Configurations;

internal sealed class ReleaseRuntimeCorrelationConfiguration : IEntityTypeConfiguration<ReleaseRuntimeCorrelation>
{
    public void Configure(EntityTypeBuilder<ReleaseRuntimeCorrelation> builder)
    {
        builder.ToTable("ops_ts_release_correlations");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.ServiceName).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Environment).HasMaxLength(100).IsRequired();
        builder.Property(x => x.MarkerType).HasMaxLength(50).IsRequired();
        builder.Property(x => x.ImpactClassification).HasMaxLength(50).IsRequired();

        builder.Property(x => x.DeployedAt).HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(x => x.CreatedAt).HasColumnType("timestamp with time zone").IsRequired();

        builder.Property(x => x.PreDeployErrorRate).IsRequired();
        builder.Property(x => x.PreDeployLatencyP95Ms).IsRequired();
        builder.Property(x => x.PreDeployRequestsPerMinute).IsRequired();
        builder.Property(x => x.PostDeployErrorRate).IsRequired();
        builder.Property(x => x.PostDeployLatencyP95Ms).IsRequired();
        builder.Property(x => x.PostDeployRequestsPerMinute).IsRequired();
        builder.Property(x => x.ImpactScore).IsRequired();

        builder.Property(x => x.TelemetryReferenceIds)
            .HasColumnType("jsonb")
            .HasConversion(
                v => JsonSerializer.Serialize(v, JsonSerializerOptions.Default),
                v => JsonSerializer.Deserialize<List<Guid>>(v, JsonSerializerOptions.Default) ?? new List<Guid>());

        builder.HasIndex(x => x.ReleaseId);
        builder.HasIndex(x => new { x.ServiceId, x.Environment, x.DeployedAt });
    }
}
