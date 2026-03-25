using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NexTraceOne.OperationalIntelligence.Domain.Incidents.Entities;

namespace NexTraceOne.OperationalIntelligence.Infrastructure.Incidents.Persistence.Configurations;

/// <summary>Configuração EF Core da entidade MitigationValidationLog.</summary>
internal sealed class MitigationValidationLogConfiguration : IEntityTypeConfiguration<MitigationValidationLog>
{
    public void Configure(EntityTypeBuilder<MitigationValidationLog> builder)
    {
        builder.ToTable("ops_mitigation_validations");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => MitigationValidationLogId.From(value));

        builder.Property(x => x.IncidentId).HasMaxLength(200).IsRequired();
        builder.Property(x => x.WorkflowId).IsRequired();
        builder.Property(x => x.Status).HasColumnType("integer").IsRequired();
        builder.Property(x => x.ObservedOutcome).HasMaxLength(4000);
        builder.Property(x => x.ValidatedBy).HasMaxLength(500);
        builder.Property(x => x.ValidatedAt).HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(x => x.ChecksJson).HasColumnType("jsonb");

        builder.HasIndex(x => x.IncidentId);
        builder.HasIndex(x => x.WorkflowId);
    }
}
