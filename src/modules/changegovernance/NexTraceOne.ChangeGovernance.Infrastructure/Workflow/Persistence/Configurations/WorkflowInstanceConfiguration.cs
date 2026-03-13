using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NexTraceOne.Workflow.Domain.Entities;

namespace NexTraceOne.Workflow.Infrastructure.Persistence.Configurations;

internal sealed class WorkflowInstanceConfiguration : IEntityTypeConfiguration<WorkflowInstance>
{
    /// <summary>Configura o mapeamento da entidade WorkflowInstance para a tabela wf_workflow_instances.</summary>
    public void Configure(EntityTypeBuilder<WorkflowInstance> builder)
    {
        builder.ToTable("wf_workflow_instances");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => WorkflowInstanceId.From(value));

        builder.Property(x => x.WorkflowTemplateId)
            .HasConversion(id => id.Value, value => WorkflowTemplateId.From(value))
            .IsRequired();
        builder.Property(x => x.ReleaseId).IsRequired();
        builder.Property(x => x.SubmittedBy).HasMaxLength(500).IsRequired();
        builder.Property(x => x.Status)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();
        builder.Property(x => x.CurrentStageIndex).IsRequired();
        builder.Property(x => x.SubmittedAt).HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(x => x.CompletedAt).HasColumnType("timestamp with time zone");

        builder.HasIndex(x => x.WorkflowTemplateId);
        builder.HasIndex(x => x.ReleaseId);
        builder.HasIndex(x => x.Status);
    }
}
