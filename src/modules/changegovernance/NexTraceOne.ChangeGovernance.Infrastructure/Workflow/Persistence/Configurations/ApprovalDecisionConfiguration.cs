using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using NexTraceOne.ChangeGovernance.Domain.Workflow.Entities;

namespace NexTraceOne.ChangeGovernance.Infrastructure.Workflow.Persistence.Configurations;

internal sealed class ApprovalDecisionConfiguration : IEntityTypeConfiguration<ApprovalDecision>
{
    /// <summary>Configura o mapeamento da entidade ApprovalDecision para a tabela wf_approval_decisions.</summary>
    public void Configure(EntityTypeBuilder<ApprovalDecision> builder)
    {
        builder.ToTable("chg_approval_decisions");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => ApprovalDecisionId.From(value));

        builder.Property(x => x.WorkflowStageId)
            .HasConversion(id => id.Value, value => WorkflowStageId.From(value))
            .IsRequired();
        builder.Property(x => x.WorkflowInstanceId)
            .HasConversion(id => id.Value, value => WorkflowInstanceId.From(value))
            .IsRequired();
        builder.Property(x => x.DecidedBy).HasMaxLength(500).IsRequired();
        builder.Property(x => x.Decision)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();
        builder.Property(x => x.Comment).HasMaxLength(4000);
        builder.Property(x => x.DecidedAt).HasColumnType("timestamp with time zone").IsRequired();

        builder.Property(x => x.CreatedAt).HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(x => x.CreatedBy).HasMaxLength(500).IsRequired();
        builder.Property(x => x.UpdatedAt).HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(x => x.UpdatedBy).HasMaxLength(500).IsRequired();
        builder.Property(x => x.IsDeleted).IsRequired().HasDefaultValue(false);

        builder.HasIndex(x => x.WorkflowStageId);
        builder.HasIndex(x => x.WorkflowInstanceId);
    }
}
