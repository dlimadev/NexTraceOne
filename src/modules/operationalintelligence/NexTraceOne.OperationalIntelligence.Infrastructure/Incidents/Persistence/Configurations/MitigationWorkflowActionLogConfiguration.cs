using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NexTraceOne.OperationalIntelligence.Domain.Incidents.Entities;

namespace NexTraceOne.OperationalIntelligence.Infrastructure.Incidents.Persistence.Configurations;

/// <summary>Configuração EF Core da entidade MitigationWorkflowActionLog.</summary>
internal sealed class MitigationWorkflowActionLogConfiguration : IEntityTypeConfiguration<MitigationWorkflowActionLog>
{
    public void Configure(EntityTypeBuilder<MitigationWorkflowActionLog> builder)
    {
        builder.ToTable("ops_mitigation_workflow_actions");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => MitigationWorkflowActionLogId.From(value));

        builder.Property(x => x.WorkflowId).IsRequired();
        builder.Property(x => x.IncidentId).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Action).HasMaxLength(200).IsRequired();
        builder.Property(x => x.NewStatus).HasColumnType("integer").IsRequired();
        builder.Property(x => x.PerformedBy).HasMaxLength(500);
        builder.Property(x => x.Reason).HasMaxLength(2000);
        builder.Property(x => x.Notes).HasMaxLength(4000);
        builder.Property(x => x.PerformedAt).HasColumnType("timestamp with time zone").IsRequired();

        builder.HasIndex(x => x.WorkflowId);
        builder.HasIndex(x => x.IncidentId);
    }
}
