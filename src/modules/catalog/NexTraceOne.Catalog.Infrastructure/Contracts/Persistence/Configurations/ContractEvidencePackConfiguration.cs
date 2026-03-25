using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using NexTraceOne.Catalog.Domain.Contracts.Entities;
using NexTraceOne.Catalog.Domain.Contracts.Enums;

namespace NexTraceOne.Catalog.Infrastructure.Contracts.Persistence.Configurations;

/// <summary>
/// Configura o mapeamento EF Core da entidade ContractEvidencePack.
/// Evidence packs agregam informação técnica para decisões de governança contratual.
/// </summary>
internal sealed class ContractEvidencePackConfiguration : IEntityTypeConfiguration<ContractEvidencePack>
{
    public void Configure(EntityTypeBuilder<ContractEvidencePack> builder)
    {
        builder.ToTable("ctr_contract_evidence_packs");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => ContractEvidencePackId.From(value));

        builder.Property(x => x.ContractVersionId)
            .HasConversion(id => id.Value, value => ContractVersionId.From(value))
            .IsRequired();

        builder.Property(x => x.ApiAssetId).IsRequired();

        builder.Property(x => x.Protocol)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.SemVer).HasMaxLength(50).IsRequired();

        builder.Property(x => x.ChangeLevel)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.BreakingChangeCount).IsRequired();
        builder.Property(x => x.AdditiveChangeCount).IsRequired();
        builder.Property(x => x.NonBreakingChangeCount).IsRequired();

        builder.Property(x => x.RecommendedVersion).HasMaxLength(50).IsRequired();
        builder.Property(x => x.OverallScore).HasPrecision(5, 4).IsRequired();
        builder.Property(x => x.RiskScore).HasPrecision(5, 4).IsRequired();
        builder.Property(x => x.RuleViolationCount).IsRequired();
        builder.Property(x => x.RequiresWorkflowApproval).IsRequired();
        builder.Property(x => x.RequiresChangeNotification).IsRequired();

        builder.Property(x => x.ExecutiveSummary).HasMaxLength(4000).IsRequired();
        builder.Property(x => x.TechnicalSummary).HasMaxLength(8000).IsRequired();

        builder.Property(x => x.ImpactedConsumers)
            .HasColumnType("text[]");

        builder.Property(x => x.GeneratedAt)
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(x => x.GeneratedBy).HasMaxLength(500).IsRequired();

        // FK: ContractEvidencePack → ContractVersion
        builder.HasOne<ContractVersion>()
            .WithMany()
            .HasForeignKey(x => x.ContractVersionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => x.ContractVersionId);
        builder.HasIndex(x => x.ApiAssetId);
        builder.HasIndex(x => x.ChangeLevel);
    }
}
