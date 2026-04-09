using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using NexTraceOne.OperationalIntelligence.Domain.Reliability.Entities;
using NexTraceOne.OperationalIntelligence.Domain.Reliability.Enums;

namespace NexTraceOne.OperationalIntelligence.Infrastructure.Reliability.Persistence.Configurations;

/// <summary>EF Core configuration for <see cref="IncidentPredictionPattern"/>.</summary>
internal sealed class IncidentPredictionPatternConfiguration : IEntityTypeConfiguration<IncidentPredictionPattern>
{
    public void Configure(EntityTypeBuilder<IncidentPredictionPattern> builder)
    {
        builder.ToTable("ops_reliability_incident_prediction_patterns");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => IncidentPredictionPatternId.From(value));

        builder.Property(x => x.PatternName).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(2000).IsRequired();

        builder.Property(x => x.PatternType)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.ServiceId).HasMaxLength(200);
        builder.Property(x => x.ServiceName).HasMaxLength(200);
        builder.Property(x => x.Environment).HasMaxLength(100).IsRequired();

        builder.Property(x => x.ConfidencePercent).IsRequired();
        builder.Property(x => x.OccurrenceCount).IsRequired();
        builder.Property(x => x.SampleSize).IsRequired();

        builder.Property(x => x.Evidence).HasColumnType("jsonb").IsRequired();
        builder.Property(x => x.TriggerConditions).HasColumnType("jsonb").IsRequired();
        builder.Property(x => x.PreventionRecommendations).HasColumnType("jsonb");

        builder.Property(x => x.Severity)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.Status)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.DetectedAt).HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(x => x.ValidatedAt).HasColumnType("timestamp with time zone");
        builder.Property(x => x.ValidationComment).HasMaxLength(2000);
        builder.Property(x => x.TenantId);

        builder.HasIndex(x => x.TenantId).HasDatabaseName("ix_ops_reliability_incident_prediction_patterns_tenant_id");
        builder.HasIndex(x => x.Environment);
        builder.HasIndex(x => x.Status);
        builder.HasIndex(x => x.PatternType);
        builder.HasIndex(x => new { x.ServiceId, x.Environment, x.DetectedAt });
    }
}
