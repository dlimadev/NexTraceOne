using System.Text.Json;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using NexTraceOne.BuildingBlocks.Observability.Telemetry.Models;

namespace NexTraceOne.OperationalIntelligence.Infrastructure.TelemetryStore.Persistence.Configurations;

internal sealed class InvestigationContextConfiguration : IEntityTypeConfiguration<InvestigationContext>
{
    public void Configure(EntityTypeBuilder<InvestigationContext> builder)
    {
        builder.ToTable("ops_ts_investigation_contexts");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Title).HasMaxLength(500).IsRequired();
        builder.Property(x => x.TitleMessageKey).HasMaxLength(500);
        builder.Property(x => x.InvestigationType).HasMaxLength(100).IsRequired();
        builder.Property(x => x.PrimaryServiceName).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Environment).HasMaxLength(100).IsRequired();
        builder.Property(x => x.Status).HasMaxLength(50).IsRequired();
        builder.Property(x => x.AiSummaryJson).HasMaxLength(4000);

        builder.Property(x => x.TimeWindowStart).HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(x => x.TimeWindowEnd).HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(x => x.CreatedAt).HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(x => x.UpdatedAt).HasColumnType("timestamp with time zone").IsRequired();

        builder.Property(x => x.AnomalySnapshotIds)
            .HasColumnType("jsonb")
            .HasConversion(
                v => JsonSerializer.Serialize(v, JsonSerializerOptions.Default),
                v => JsonSerializer.Deserialize<List<Guid>>(v, JsonSerializerOptions.Default) ?? new List<Guid>());

        builder.Property(x => x.ReleaseCorrelationIds)
            .HasColumnType("jsonb")
            .HasConversion(
                v => JsonSerializer.Serialize(v, JsonSerializerOptions.Default),
                v => JsonSerializer.Deserialize<List<Guid>>(v, JsonSerializerOptions.Default) ?? new List<Guid>());

        builder.Property(x => x.TelemetryReferenceIds)
            .HasColumnType("jsonb")
            .HasConversion(
                v => JsonSerializer.Serialize(v, JsonSerializerOptions.Default),
                v => JsonSerializer.Deserialize<List<Guid>>(v, JsonSerializerOptions.Default) ?? new List<Guid>());

        builder.Property(x => x.AffectedServiceIds)
            .HasColumnType("jsonb")
            .HasConversion(
                v => JsonSerializer.Serialize(v, JsonSerializerOptions.Default),
                v => JsonSerializer.Deserialize<List<Guid>>(v, JsonSerializerOptions.Default) ?? new List<Guid>());

        builder.HasIndex(x => new { x.PrimaryServiceId, x.Environment, x.Status });
        builder.HasIndex(x => new { x.Status, x.CreatedAt });
    }
}
