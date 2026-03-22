using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NexTraceOne.OperationalIntelligence.Domain.Automation.Entities;
using NexTraceOne.OperationalIntelligence.Domain.Automation.Enums;

namespace NexTraceOne.OperationalIntelligence.Infrastructure.Automation.Persistence.Configurations;

/// <summary>Configuração EF Core da entidade AutomationAuditRecord.</summary>
internal sealed class AutomationAuditRecordConfiguration : IEntityTypeConfiguration<AutomationAuditRecord>
{
    public void Configure(EntityTypeBuilder<AutomationAuditRecord> builder)
    {
        builder.ToTable("oi_automation_audit_records");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => new AutomationAuditRecordId(value));

        builder.Property(x => x.WorkflowId)
            .HasConversion(id => id.Value, value => new AutomationWorkflowRecordId(value))
            .IsRequired();

        builder.Property(x => x.Action)
            .HasConversion<string>()
            .HasMaxLength(100)
            .IsRequired()
            .HasDefaultValue(AutomationAuditAction.WorkflowCreated);

        builder.Property(x => x.Actor).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Details).HasMaxLength(2000).IsRequired();
        builder.Property(x => x.ServiceId).HasMaxLength(200);
        builder.Property(x => x.TeamId).HasMaxLength(200);

        builder.Property(x => x.OccurredAt)
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(x => x.CreatedAt)
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.HasOne<AutomationWorkflowRecord>()
            .WithMany()
            .HasForeignKey(x => x.WorkflowId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => x.WorkflowId);
        builder.HasIndex(x => x.ServiceId);
        builder.HasIndex(x => x.TeamId);
        builder.HasIndex(x => x.OccurredAt);
        builder.HasIndex(x => x.Action);
    }
}
