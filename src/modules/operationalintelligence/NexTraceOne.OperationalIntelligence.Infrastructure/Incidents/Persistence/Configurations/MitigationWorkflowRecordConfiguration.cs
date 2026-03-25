using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NexTraceOne.OperationalIntelligence.Domain.Incidents.Entities;
using NexTraceOne.OperationalIntelligence.Domain.Incidents.Enums;

namespace NexTraceOne.OperationalIntelligence.Infrastructure.Incidents.Persistence.Configurations;

/// <summary>Configuração EF Core da entidade MitigationWorkflowRecord.</summary>
internal sealed class MitigationWorkflowRecordConfiguration : IEntityTypeConfiguration<MitigationWorkflowRecord>
{
    public void Configure(EntityTypeBuilder<MitigationWorkflowRecord> builder)
    {
        builder.ToTable("ops_mitigation_workflows");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => MitigationWorkflowRecordId.From(value));

        builder.Property(x => x.IncidentId).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Title).HasMaxLength(500).IsRequired();
        builder.Property(x => x.Status).HasColumnType("integer").IsRequired()
            .HasDefaultValue(MitigationWorkflowStatus.Draft);
        builder.Property(x => x.ActionType).HasColumnType("integer").IsRequired();
        builder.Property(x => x.RiskLevel).HasColumnType("integer").IsRequired();
        builder.Property(x => x.RequiresApproval).IsRequired();
        builder.Property(x => x.LinkedRunbookId);
        builder.Property(x => x.ApprovedBy).HasMaxLength(500);
        builder.Property(x => x.ApprovedAt).HasColumnType("timestamp with time zone");
        builder.Property(x => x.CreatedByUser).HasMaxLength(500).IsRequired();
        builder.Property(x => x.StartedAt).HasColumnType("timestamp with time zone");
        builder.Property(x => x.CompletedAt).HasColumnType("timestamp with time zone");
        builder.Property(x => x.CompletedOutcome).HasColumnType("integer");
        builder.Property(x => x.CompletedNotes).HasMaxLength(4000);
        builder.Property(x => x.CompletedBy).HasMaxLength(500);

        builder.Property(x => x.StepsJson).HasColumnType("jsonb");
        builder.Property(x => x.DecisionsJson).HasColumnType("jsonb");

        builder.HasIndex(x => x.IncidentId);
        builder.HasIndex(x => x.Status);
    }
}
