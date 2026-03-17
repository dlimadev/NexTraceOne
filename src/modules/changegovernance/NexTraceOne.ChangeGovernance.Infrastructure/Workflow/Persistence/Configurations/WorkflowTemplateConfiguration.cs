using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using NexTraceOne.ChangeGovernance.Domain.Workflow.Entities;

namespace NexTraceOne.ChangeGovernance.Infrastructure.Workflow.Persistence.Configurations;

internal sealed class WorkflowTemplateConfiguration : IEntityTypeConfiguration<WorkflowTemplate>
{
    /// <summary>Configura o mapeamento da entidade WorkflowTemplate para a tabela wf_workflow_templates.</summary>
    public void Configure(EntityTypeBuilder<WorkflowTemplate> builder)
    {
        builder.ToTable("wf_workflow_templates");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => WorkflowTemplateId.From(value));

        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(2000).IsRequired();
        builder.Property(x => x.ChangeType).HasMaxLength(100).IsRequired();
        builder.Property(x => x.ApiCriticality).HasMaxLength(50).IsRequired();
        builder.Property(x => x.TargetEnvironment).HasMaxLength(100).IsRequired();
        builder.Property(x => x.MinimumApprovers).IsRequired();
        builder.Property(x => x.IsActive).IsRequired().HasDefaultValue(true);
        builder.Property(x => x.CreatedAt).HasColumnType("timestamp with time zone").IsRequired();

        builder.HasIndex(x => x.ChangeType);
        builder.HasIndex(x => x.IsActive);
    }
}
