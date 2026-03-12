using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NexTraceOne.Workflow.Domain.Entities;
using NexTraceOne.Workflow.Domain.Enums;

namespace NexTraceOne.Workflow.Infrastructure.Persistence.Configurations;

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

internal sealed class EvidencePackConfiguration : IEntityTypeConfiguration<EvidencePack>
{
    /// <summary>Configura o mapeamento da entidade EvidencePack para a tabela wf_evidence_packs.</summary>
    public void Configure(EntityTypeBuilder<EvidencePack> builder)
    {
        builder.ToTable("wf_evidence_packs");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => EvidencePackId.From(value));

        builder.Property(x => x.WorkflowInstanceId)
            .HasConversion(id => id.Value, value => WorkflowInstanceId.From(value))
            .IsRequired();
        builder.Property(x => x.ReleaseId).IsRequired();
        builder.Property(x => x.ContractDiffSummary).HasMaxLength(4000);
        builder.Property(x => x.BlastRadiusScore).HasPrecision(5, 4);
        builder.Property(x => x.SpectralScore).HasPrecision(5, 4);
        builder.Property(x => x.ChangeIntelligenceScore).HasPrecision(5, 4);
        builder.Property(x => x.ApprovalHistory).HasColumnType("jsonb");
        builder.Property(x => x.ContractHash).HasMaxLength(128);
        builder.Property(x => x.CompletenessPercentage).HasPrecision(5, 2).IsRequired();
        builder.Property(x => x.GeneratedAt).HasColumnType("timestamp with time zone").IsRequired();

        builder.HasIndex(x => x.WorkflowInstanceId).IsUnique();
        builder.HasIndex(x => x.ReleaseId);
    }
}

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

internal sealed class ApprovalDecisionConfiguration : IEntityTypeConfiguration<ApprovalDecision>
{
    /// <summary>Configura o mapeamento da entidade ApprovalDecision para a tabela wf_approval_decisions.</summary>
    public void Configure(EntityTypeBuilder<ApprovalDecision> builder)
    {
        builder.ToTable("wf_approval_decisions");
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
