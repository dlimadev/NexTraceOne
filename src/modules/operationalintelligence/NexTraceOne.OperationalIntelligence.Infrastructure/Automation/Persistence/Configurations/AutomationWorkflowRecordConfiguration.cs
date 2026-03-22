using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NexTraceOne.OperationalIntelligence.Domain.Automation.Entities;
using NexTraceOne.OperationalIntelligence.Domain.Automation.Enums;
using NexTraceOne.OperationalIntelligence.Domain.Incidents.Enums;

namespace NexTraceOne.OperationalIntelligence.Infrastructure.Automation.Persistence.Configurations;

/// <summary>Configuração EF Core da entidade AutomationWorkflowRecord.</summary>
internal sealed class AutomationWorkflowRecordConfiguration : IEntityTypeConfiguration<AutomationWorkflowRecord>
{
    public void Configure(EntityTypeBuilder<AutomationWorkflowRecord> builder)
    {
        builder.ToTable("oi_automation_workflows");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => new AutomationWorkflowRecordId(value));

        builder.Property(x => x.ActionId).HasMaxLength(200).IsRequired();
        builder.Property(x => x.ServiceId).HasMaxLength(200);
        builder.Property(x => x.IncidentId).HasMaxLength(200);
        builder.Property(x => x.ChangeId).HasMaxLength(200);
        builder.Property(x => x.Rationale).HasMaxLength(1000).IsRequired();
        builder.Property(x => x.RequestedBy).HasMaxLength(200).IsRequired();
        builder.Property(x => x.TargetScope).HasMaxLength(200);
        builder.Property(x => x.TargetEnvironment).HasMaxLength(200);
        builder.Property(x => x.ApprovedBy).HasMaxLength(200);
        builder.Property(x => x.ApprovedAt).HasColumnType("timestamp with time zone");

        builder.Property(x => x.Status)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired()
            .HasDefaultValue(AutomationWorkflowStatus.Draft);

        builder.Property(x => x.ApprovalStatus)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired()
            .HasDefaultValue(AutomationApprovalStatus.Pending);

        builder.Property(x => x.RiskLevel)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired()
            .HasDefaultValue(RiskLevel.Low);

        builder.Property(x => x.CreatedAt)
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(x => x.UpdatedAt)
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.HasIndex(x => x.ActionId);
        builder.HasIndex(x => x.ServiceId);
        builder.HasIndex(x => x.IncidentId);
        builder.HasIndex(x => x.Status);
        builder.HasIndex(x => x.RequestedBy);
        builder.HasIndex(x => x.CreatedAt);
    }
}
