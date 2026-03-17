using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using NexTraceOne.ChangeGovernance.Domain.Workflow.Entities;

namespace NexTraceOne.ChangeGovernance.Infrastructure.Workflow.Persistence.Configurations;

internal sealed class WorkflowStageConfiguration : IEntityTypeConfiguration<WorkflowStage>
{
    /// <summary>Configura o mapeamento da entidade WorkflowStage para a tabela wf_workflow_stages.</summary>
    public void Configure(EntityTypeBuilder<WorkflowStage> builder)
    {
        builder.ToTable("wf_workflow_stages");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => WorkflowStageId.From(value));

        builder.Property(x => x.WorkflowInstanceId)
            .HasConversion(id => id.Value, value => WorkflowInstanceId.From(value))
            .IsRequired();
        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
        builder.Property(x => x.StageOrder).IsRequired();
        builder.Property(x => x.Status)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();
        builder.Property(x => x.RequiredApprovers).IsRequired();
        builder.Property(x => x.CurrentApprovals).IsRequired();
        builder.Property(x => x.CommentRequired).IsRequired();
        builder.Property(x => x.SlaDurationHours);
        builder.Property(x => x.StartedAt).HasColumnType("timestamp with time zone");
        builder.Property(x => x.CompletedAt).HasColumnType("timestamp with time zone");

        builder.Property(x => x.CreatedAt).HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(x => x.CreatedBy).HasMaxLength(500).IsRequired();
        builder.Property(x => x.UpdatedAt).HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(x => x.UpdatedBy).HasMaxLength(500).IsRequired();
        builder.Property(x => x.IsDeleted).IsRequired().HasDefaultValue(false);

        builder.Ignore(x => x.IsComplete);

        builder.HasIndex(x => x.WorkflowInstanceId);
        builder.HasIndex(x => new { x.WorkflowInstanceId, x.StageOrder }).IsUnique();
    }
}
