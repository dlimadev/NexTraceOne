using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NexTraceOne.OperationalIntelligence.Domain.Incidents.Entities;

namespace NexTraceOne.OperationalIntelligence.Infrastructure.Incidents.Persistence.Configurations;

/// <summary>Configuração EF Core da entidade IncidentChangeCorrelation.</summary>
internal sealed class IncidentChangeCorrelationConfiguration : IEntityTypeConfiguration<IncidentChangeCorrelation>
{
    public void Configure(EntityTypeBuilder<IncidentChangeCorrelation> builder)
    {
        builder.ToTable("ops_incident_change_correlations");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => IncidentChangeCorrelationId.From(value));

        builder.Property(x => x.IncidentId).IsRequired();
        builder.Property(x => x.ChangeId).IsRequired();
        builder.Property(x => x.ServiceId).IsRequired();
        builder.Property(x => x.ConfidenceLevel).HasColumnType("integer").IsRequired();
        builder.Property(x => x.MatchType).HasColumnType("integer").IsRequired();
        builder.Property(x => x.TimeWindowHours).IsRequired();
        builder.Property(x => x.CorrelatedAt).HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(x => x.TenantId).HasColumnName("tenant_id");
        builder.Property(x => x.ServiceName).HasMaxLength(200).IsRequired();
        builder.Property(x => x.ChangeDescription).HasMaxLength(2000).IsRequired();
        builder.Property(x => x.ChangeEnvironment).HasMaxLength(100).IsRequired();
        builder.Property(x => x.ChangeOccurredAt).HasColumnType("timestamp with time zone").IsRequired();

        builder.HasIndex(x => x.IncidentId).HasDatabaseName("ix_ops_icc_incident_id");
        builder.HasIndex(x => x.TenantId).HasDatabaseName("ix_ops_icc_tenant_id");
        builder.HasIndex(x => new { x.IncidentId, x.ChangeId })
            .IsUnique()
            .HasDatabaseName("ix_ops_icc_incident_change_unique");
    }
}
