using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NexTraceOne.OperationalIntelligence.Domain.Incidents.Entities;
using NexTraceOne.OperationalIntelligence.Domain.Incidents.Enums;

namespace NexTraceOne.OperationalIntelligence.Infrastructure.Incidents.Persistence.Configurations;

/// <summary>Configuração EF Core da entidade IncidentRecord.</summary>
internal sealed class IncidentRecordConfiguration : IEntityTypeConfiguration<IncidentRecord>
{
    public void Configure(EntityTypeBuilder<IncidentRecord> builder)
    {
        builder.ToTable("oi_incidents");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => IncidentRecordId.From(value));

        builder.Property(x => x.ExternalRef).HasMaxLength(100).IsRequired();
        builder.Property(x => x.Title).HasMaxLength(500).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(4000);
        builder.Property(x => x.Type).HasColumnType("integer").IsRequired();
        builder.Property(x => x.Severity).HasColumnType("integer").IsRequired();
        builder.Property(x => x.Status).HasColumnType("integer").IsRequired();
        builder.Property(x => x.ServiceId).HasMaxLength(200).IsRequired();
        builder.Property(x => x.ServiceName).HasMaxLength(200).IsRequired();
        builder.Property(x => x.OwnerTeam).HasMaxLength(200).IsRequired();
        builder.Property(x => x.ImpactedDomain).HasMaxLength(200);
        builder.Property(x => x.Environment).HasMaxLength(100).IsRequired();
        builder.Property(x => x.TenantId).HasColumnName("tenant_id");
        builder.Property(x => x.EnvironmentId).HasColumnName("environment_id");
        builder.Property(x => x.DetectedAt).HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(x => x.LastUpdatedAt).HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(x => x.HasCorrelation).IsRequired();
        builder.Property(x => x.CorrelationConfidence).HasColumnType("integer").IsRequired();
        builder.Property(x => x.MitigationStatus).HasColumnType("integer").IsRequired();

        builder.Property(x => x.CorrelationAnalysis).HasMaxLength(4000);
        builder.Property(x => x.EvidenceTelemetrySummary).HasMaxLength(4000);
        builder.Property(x => x.EvidenceBusinessImpact).HasMaxLength(4000);
        builder.Property(x => x.EvidenceAnalysis).HasMaxLength(4000);
        builder.Property(x => x.EvidenceTemporalContext).HasMaxLength(4000);
        builder.Property(x => x.MitigationNarrative).HasMaxLength(4000);
        builder.Property(x => x.HasEscalationPath).IsRequired();
        builder.Property(x => x.EscalationPath).HasMaxLength(2000);

        builder.Property(x => x.TimelineJson).HasColumnType("jsonb");
        builder.Property(x => x.LinkedServicesJson).HasColumnType("jsonb");
        builder.Property(x => x.CorrelatedChangesJson).HasColumnType("jsonb");
        builder.Property(x => x.CorrelatedServicesJson).HasColumnType("jsonb");
        builder.Property(x => x.CorrelatedDependenciesJson).HasColumnType("jsonb");
        builder.Property(x => x.ImpactedContractsJson).HasColumnType("jsonb");
        builder.Property(x => x.EvidenceObservationsJson).HasColumnType("jsonb");
        builder.Property(x => x.RelatedContractsJson).HasColumnType("jsonb");
        builder.Property(x => x.RunbookLinksJson).HasColumnType("jsonb");
        builder.Property(x => x.MitigationActionsJson).HasColumnType("jsonb");
        builder.Property(x => x.MitigationRecommendationsJson).HasColumnType("jsonb");
        builder.Property(x => x.MitigationRecommendedRunbooksJson).HasColumnType("jsonb");

        builder.HasIndex(x => x.ExternalRef).IsUnique();
        builder.HasIndex(x => x.ServiceId);
        builder.HasIndex(x => x.Status);
        builder.HasIndex(x => x.Severity);
        builder.HasIndex(x => x.DetectedAt);
        builder.HasIndex(x => x.TenantId).HasDatabaseName("ix_oi_incidents_tenant_id");
        builder.HasIndex(x => new { x.TenantId, x.EnvironmentId }).HasDatabaseName("ix_oi_incidents_tenant_environment");
    }
}
