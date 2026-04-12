using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using NexTraceOne.OperationalIntelligence.Domain.Incidents.Entities;
using NexTraceOne.OperationalIntelligence.Domain.Incidents.Enums;

namespace NexTraceOne.OperationalIntelligence.Infrastructure.Incidents.Persistence.Configurations;

/// <summary>Configuração EF Core da entidade IncidentNarrative.</summary>
internal sealed class IncidentNarrativeConfiguration : IEntityTypeConfiguration<IncidentNarrative>
{
    public void Configure(EntityTypeBuilder<IncidentNarrative> builder)
    {
        builder.ToTable("ops_incident_narratives");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => IncidentNarrativeId.From(value));

        builder.Property(x => x.IncidentId).IsRequired();
        builder.Property(x => x.NarrativeText).IsRequired();
        builder.Property(x => x.SymptomsSection).HasMaxLength(10000);
        builder.Property(x => x.TimelineSection).HasMaxLength(10000);
        builder.Property(x => x.ProbableCauseSection).HasMaxLength(10000);
        builder.Property(x => x.MitigationSection).HasMaxLength(10000);
        builder.Property(x => x.RelatedChangesSection).HasMaxLength(10000);
        builder.Property(x => x.AffectedServicesSection).HasMaxLength(10000);
        builder.Property(x => x.ModelUsed).HasMaxLength(200).IsRequired();
        builder.Property(x => x.TokensUsed).IsRequired();
        builder.Property(x => x.Status)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();
        builder.Property(x => x.TenantId).HasColumnName("tenant_id").IsRequired();
        builder.Property(x => x.GeneratedAt).HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(x => x.LastRefreshedAt).HasColumnType("timestamp with time zone");
        builder.Property(x => x.RefreshCount).IsRequired();

        builder.HasIndex(x => x.IncidentId).IsUnique();
        builder.HasIndex(x => x.TenantId).HasDatabaseName("ix_ops_incident_narratives_tenant_id");
    }
}
