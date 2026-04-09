using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using NexTraceOne.OperationalIntelligence.Domain.Runtime.Entities;
using NexTraceOne.OperationalIntelligence.Domain.Runtime.Enums;

namespace NexTraceOne.OperationalIntelligence.Infrastructure.Runtime.Persistence.Configurations;

/// <summary>EF Core configuration for <see cref="ResilienceReport"/>.</summary>
internal sealed class ResilienceReportConfiguration : IEntityTypeConfiguration<ResilienceReport>
{
    public void Configure(EntityTypeBuilder<ResilienceReport> builder)
    {
        builder.ToTable("ops_resilience_reports", t =>
        {
            t.HasCheckConstraint("CK_ops_resilience_reports_score",
                "\"ResilienceScore\" >= 0 AND \"ResilienceScore\" <= 100");
        });

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => ResilienceReportId.From(value));

        builder.Property(x => x.ChaosExperimentId).IsRequired();
        builder.Property(x => x.ServiceName).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Environment).HasMaxLength(100).IsRequired();
        builder.Property(x => x.ExperimentType).HasMaxLength(100).IsRequired();
        builder.Property(x => x.ResilienceScore).IsRequired();

        builder.Property(x => x.TheoreticalBlastRadius).HasColumnType("jsonb");
        builder.Property(x => x.ActualBlastRadius).HasColumnType("jsonb");
        builder.Property(x => x.BlastRadiusDeviation).HasPrecision(10, 4);
        builder.Property(x => x.TelemetryObservations).HasColumnType("jsonb");
        builder.Property(x => x.LatencyImpactMs).HasPrecision(12, 4);
        builder.Property(x => x.ErrorRateImpact).HasPrecision(10, 4);

        builder.Property(x => x.Strengths).HasColumnType("jsonb");
        builder.Property(x => x.Weaknesses).HasColumnType("jsonb");
        builder.Property(x => x.Recommendations).HasColumnType("jsonb");

        builder.Property(x => x.Status)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.ReviewedByUserId).HasMaxLength(200);
        builder.Property(x => x.ReviewedAt).HasColumnType("timestamp with time zone");
        builder.Property(x => x.ReviewComment).HasMaxLength(2000);

        builder.Property(x => x.GeneratedAt).HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(x => x.TenantId).HasMaxLength(100).IsRequired();

        builder.Property(x => x.RowVersion)
            .IsRowVersion();

        // ── Auditoria ────────────────────────────────────────────────────────
        builder.Property(x => x.CreatedAt).HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(x => x.CreatedBy).HasMaxLength(200).IsRequired();
        builder.Property(x => x.UpdatedAt).HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(x => x.UpdatedBy).HasMaxLength(200).IsRequired();
        builder.Property(x => x.IsDeleted).HasDefaultValue(false).IsRequired();

        // ── Índices ──────────────────────────────────────────────────────────
        builder.HasIndex(x => x.TenantId).HasDatabaseName("ix_ops_resilience_reports_tenant_id");
        builder.HasIndex(x => new { x.TenantId, x.ServiceName }).HasDatabaseName("ix_ops_resilience_reports_tenant_service");
        builder.HasIndex(x => x.ChaosExperimentId).HasDatabaseName("ix_ops_resilience_reports_experiment_id");
        builder.HasIndex(x => x.Status).HasDatabaseName("ix_ops_resilience_reports_status");

        // ── Soft-delete global filter ────────────────────────────────────────
        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}
