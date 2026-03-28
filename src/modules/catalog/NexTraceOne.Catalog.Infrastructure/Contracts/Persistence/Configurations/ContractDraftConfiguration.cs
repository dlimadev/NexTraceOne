using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using NexTraceOne.Catalog.Domain.Contracts.Entities;
using NexTraceOne.Catalog.Domain.Contracts.Enums;

namespace NexTraceOne.Catalog.Infrastructure.Contracts.Persistence.Configurations;

/// <summary>
/// Configura o mapeamento EF Core da entidade ContractDraft.
/// Drafts representam rascunhos de contrato no Contract Studio, com estado de edição,
/// revisão, aprovação e publicação. Inclui suporte a geração por IA.
/// </summary>
internal sealed class ContractDraftConfiguration : IEntityTypeConfiguration<ContractDraft>
{
    /// <summary>Configura o mapeamento da entidade ContractDraft para a tabela ctr_contract_drafts.</summary>
    public void Configure(EntityTypeBuilder<ContractDraft> builder)
    {
        builder.ToTable("ctr_contract_drafts", t =>
        {
            t.HasCheckConstraint(
                "CK_ctr_contract_drafts_status",
                "\"Status\" IN ('Editing', 'InReview', 'Approved', 'Rejected', 'Published')");
        });
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => ContractDraftId.From(value));

        builder.Property(x => x.Title).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(2000).IsRequired();
        builder.Property(x => x.ServiceId);
        builder.Property(x => x.SpecContent).HasColumnType("text").IsRequired();
        builder.Property(x => x.Format).HasMaxLength(10).IsRequired();
        builder.Property(x => x.ProposedVersion).HasMaxLength(50).IsRequired();
        builder.Property(x => x.Author).HasMaxLength(200).IsRequired();

        builder.Property(x => x.ContractType)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.Protocol)
            .HasConversion<string>()
            .HasMaxLength(50)
            .HasDefaultValue(ContractProtocol.OpenApi)
            .IsRequired();

        builder.Property(x => x.Status)
            .HasConversion<string>()
            .HasMaxLength(50)
            .HasDefaultValue(DraftStatus.Editing)
            .IsRequired();

        builder.Property(x => x.BaseContractVersionId);
        builder.Property(x => x.IsAiGenerated).IsRequired().HasDefaultValue(false);
        builder.Property(x => x.AiGenerationPrompt).HasMaxLength(5000);
        builder.Property(x => x.LastEditedAt).HasColumnType("timestamp with time zone");
        builder.Property(x => x.LastEditedBy).HasMaxLength(200);

        // Auditoria
        builder.Property(x => x.CreatedAt).HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(x => x.CreatedBy).HasMaxLength(500).IsRequired();
        builder.Property(x => x.UpdatedAt).HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(x => x.UpdatedBy).HasMaxLength(500).IsRequired();
        builder.Property(x => x.IsDeleted).IsRequired().HasDefaultValue(false);

        builder.HasIndex(x => x.Status);
        builder.HasIndex(x => x.ServiceId);
        builder.HasIndex(x => x.Author);
        builder.HasIndex(x => x.Protocol);
        builder.HasIndex(x => x.IsDeleted).HasFilter("\"IsDeleted\" = false");

        // Concorrência otimista via PostgreSQL xmin
        builder.Property(x => x.RowVersion)
            .IsRowVersion();

        // Relacionamento com exemplos
        builder.HasMany(x => x.Examples)
            .WithOne()
            .HasForeignKey(e => e.DraftId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
