using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using NexTraceOne.OperationalIntelligence.Domain.Reliability.Entities;
using NexTraceOne.OperationalIntelligence.Domain.Reliability.Enums;

namespace NexTraceOne.OperationalIntelligence.Infrastructure.Reliability.Persistence.Configurations;

/// <summary>EF Core configuration for <see cref="HealingRecommendation"/>.</summary>
internal sealed class HealingRecommendationConfiguration : IEntityTypeConfiguration<HealingRecommendation>
{
    public void Configure(EntityTypeBuilder<HealingRecommendation> builder)
    {
        builder.ToTable("ops_reliability_healing_recommendations");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => HealingRecommendationId.From(value));

        builder.Property(x => x.ServiceName).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Environment).HasMaxLength(100).IsRequired();
        builder.Property(x => x.IncidentId);
        builder.Property(x => x.RootCauseDescription).HasMaxLength(2000).IsRequired();

        builder.Property(x => x.ActionType)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.ActionDetails).HasColumnType("jsonb").IsRequired();
        builder.Property(x => x.ConfidenceScore).IsRequired();
        builder.Property(x => x.EstimatedImpact).HasColumnType("jsonb");
        builder.Property(x => x.RelatedRunbookIds).HasColumnType("jsonb");
        builder.Property(x => x.HistoricalSuccessRate).HasPrecision(5, 2);

        builder.Property(x => x.Status)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.ApprovedByUserId).HasMaxLength(200);
        builder.Property(x => x.ApprovedAt).HasColumnType("timestamp with time zone");
        builder.Property(x => x.ExecutionStartedAt).HasColumnType("timestamp with time zone");
        builder.Property(x => x.ExecutionCompletedAt).HasColumnType("timestamp with time zone");
        builder.Property(x => x.ExecutionResult).HasColumnType("jsonb");
        builder.Property(x => x.ErrorMessage).HasMaxLength(4000);
        builder.Property(x => x.EvidenceTrail).HasColumnType("jsonb");
        builder.Property(x => x.GeneratedAt).HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(x => x.TenantId);

        builder.Property(x => x.RowVersion)
            .HasColumnName("xmin")
            .HasColumnType("xid")
            .IsRowVersion();

        // ── Índices ──────────────────────────────────────────────────────────
        builder.HasIndex(x => x.TenantId)
            .HasDatabaseName("ix_ops_reliability_healing_recommendations_tenant_id");
        builder.HasIndex(x => x.ServiceName);
        builder.HasIndex(x => x.Status);
        builder.HasIndex(x => new { x.ServiceName, x.Status, x.GeneratedAt });
    }
}
