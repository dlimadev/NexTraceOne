using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NexTraceOne.Workflow.Domain.Entities;

namespace NexTraceOne.Workflow.Infrastructure.Persistence.Configurations;

internal sealed class SlaPolicyConfiguration : IEntityTypeConfiguration<SlaPolicy>
{
    /// <summary>Configura o mapeamento da entidade SlaPolicy para a tabela wf_sla_policies.</summary>
    public void Configure(EntityTypeBuilder<SlaPolicy> builder)
    {
        builder.ToTable("wf_sla_policies");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => SlaPolicyId.From(value));

        builder.Property(x => x.WorkflowTemplateId)
            .HasConversion(id => id.Value, value => WorkflowTemplateId.From(value))
            .IsRequired();
        builder.Property(x => x.StageName).HasMaxLength(200).IsRequired();
        builder.Property(x => x.MaxDurationHours).IsRequired();
        builder.Property(x => x.EscalationEnabled).IsRequired();
        builder.Property(x => x.EscalationTargetRole).HasMaxLength(200);

        builder.Property(x => x.CreatedAt).HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(x => x.CreatedBy).HasMaxLength(500).IsRequired();
        builder.Property(x => x.UpdatedAt).HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(x => x.UpdatedBy).HasMaxLength(500).IsRequired();
        builder.Property(x => x.IsDeleted).IsRequired().HasDefaultValue(false);

        builder.HasIndex(x => x.WorkflowTemplateId);
        builder.HasIndex(x => new { x.WorkflowTemplateId, x.StageName }).IsUnique();
    }
}
