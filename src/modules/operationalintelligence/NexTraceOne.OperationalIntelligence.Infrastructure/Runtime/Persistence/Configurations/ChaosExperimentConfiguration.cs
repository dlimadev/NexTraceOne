using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NexTraceOne.OperationalIntelligence.Domain.Runtime.Entities;
using NexTraceOne.OperationalIntelligence.Domain.Runtime.Enums;

namespace NexTraceOne.OperationalIntelligence.Infrastructure.Runtime.Persistence.Configurations;

/// <summary>EF Core configuration for <see cref="ChaosExperiment"/>.</summary>
internal sealed class ChaosExperimentConfiguration : IEntityTypeConfiguration<ChaosExperiment>
{
    public void Configure(EntityTypeBuilder<ChaosExperiment> builder)
    {
        builder.ToTable("ops_chaos_experiments", t =>
        {
            t.HasCheckConstraint("CK_ops_chaos_experiments_status",
                "\"Status\" >= 0 AND \"Status\" <= 4");
            t.HasCheckConstraint("CK_ops_chaos_experiments_duration",
                "\"DurationSeconds\" >= 10 AND \"DurationSeconds\" <= 3600");
            t.HasCheckConstraint("CK_ops_chaos_experiments_target_pct",
                "\"TargetPercentage\" >= 1 AND \"TargetPercentage\" <= 100");
        });

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => ChaosExperimentId.From(value));

        builder.Property(x => x.TenantId).HasMaxLength(100).IsRequired();
        builder.Property(x => x.ServiceName).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Environment).HasMaxLength(100).IsRequired();
        builder.Property(x => x.ExperimentType).HasMaxLength(100).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(2000);
        builder.Property(x => x.RiskLevel).HasMaxLength(20).IsRequired();
        builder.Property(x => x.Status).HasColumnType("integer").IsRequired();
        builder.Property(x => x.DurationSeconds).IsRequired();
        builder.Property(x => x.TargetPercentage).HasPrecision(5, 2).IsRequired();
        builder.Property(x => x.ExecutionNotes).HasMaxLength(5000);

        builder.Property(x => x.Steps)
            .HasColumnType("jsonb")
            .HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => JsonSerializer.Deserialize<List<string>>(v, (JsonSerializerOptions?)null) ?? new List<string>())
            .IsRequired();

        builder.Property(x => x.SafetyChecks)
            .HasColumnType("jsonb")
            .HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => JsonSerializer.Deserialize<List<string>>(v, (JsonSerializerOptions?)null) ?? new List<string>())
            .IsRequired();

        builder.Property(x => x.StartedAt).HasColumnType("timestamp with time zone");
        builder.Property(x => x.CompletedAt).HasColumnType("timestamp with time zone");

        // ── Auditoria ────────────────────────────────────────────────────────
        builder.Property(x => x.CreatedAt).HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(x => x.CreatedBy).HasMaxLength(200).IsRequired();
        builder.Property(x => x.UpdatedAt).HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(x => x.UpdatedBy).HasMaxLength(200).IsRequired();
        builder.Property(x => x.IsDeleted).HasDefaultValue(false).IsRequired();

        // ── Índices ──────────────────────────────────────────────────────────
        builder.HasIndex(x => x.TenantId);
        builder.HasIndex(x => new { x.TenantId, x.ServiceName });
        builder.HasIndex(x => new { x.TenantId, x.Status });

        // ── Soft-delete global filter ────────────────────────────────────────
        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}
