using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using NexTraceOne.ChangeGovernance.Domain.Workflow.Entities;

namespace NexTraceOne.ChangeGovernance.Infrastructure.Workflow.Persistence.Configurations;

internal sealed class EvidencePackConfiguration : IEntityTypeConfiguration<EvidencePack>
{
    /// <summary>Configura o mapeamento da entidade EvidencePack para a tabela wf_evidence_packs.</summary>
    public void Configure(EntityTypeBuilder<EvidencePack> builder)
    {
        builder.ToTable("chg_evidence_packs");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => EvidencePackId.From(value));

        builder.Property(x => x.WorkflowInstanceId)
            .HasConversion(id => id.Value, value => WorkflowInstanceId.From(value))
            .IsRequired();
        builder.Property(x => x.ReleaseId).IsRequired();
        builder.Property(x => x.ContractDiffSummary).HasMaxLength(5000);
        builder.Property(x => x.BlastRadiusScore).HasPrecision(5, 4);
        builder.Property(x => x.SpectralScore).HasPrecision(5, 4);
        builder.Property(x => x.ChangeIntelligenceScore).HasPrecision(5, 4);
        builder.Property(x => x.ApprovalHistory).HasColumnType("jsonb");
        builder.Property(x => x.ContractHash).HasMaxLength(256);
        builder.Property(x => x.CompletenessPercentage).HasPrecision(5, 2).IsRequired();
        builder.Property(x => x.GeneratedAt).HasColumnType("timestamp with time zone").IsRequired();

        // ── CI/CD fields (P5.4) ──────────────────────────────────────────────
        builder.Property(x => x.PipelineSource).HasMaxLength(500);
        builder.Property(x => x.BuildId).HasMaxLength(500);
        builder.Property(x => x.CommitSha).HasMaxLength(100);
        builder.Property(x => x.CiChecksResult).HasMaxLength(50);

        builder.HasIndex(x => x.WorkflowInstanceId).IsUnique();
        builder.HasIndex(x => x.ReleaseId);
    }
}
