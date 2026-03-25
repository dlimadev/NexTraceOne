using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using NexTraceOne.Catalog.Domain.Contracts.Entities;

namespace NexTraceOne.Catalog.Infrastructure.Contracts.Persistence.Configurations;

/// <summary>
/// Configura o mapeamento EF Core da entidade ContractReview.
/// Revisões registam as decisões (aprovação ou rejeição) de drafts de contrato
/// para rastreabilidade completa do fluxo de aprovação.
/// </summary>
internal sealed class ContractReviewConfiguration : IEntityTypeConfiguration<ContractReview>
{
    /// <summary>Configura o mapeamento da entidade ContractReview para a tabela ct_contract_reviews.</summary>
    public void Configure(EntityTypeBuilder<ContractReview> builder)
    {
        builder.ToTable("ctr_contract_reviews");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => ContractReviewId.From(value));

        builder.Property(x => x.DraftId)
            .HasConversion(id => id.Value, value => ContractDraftId.From(value))
            .IsRequired();

        builder.Property(x => x.ReviewedBy).HasMaxLength(200).IsRequired();

        builder.Property(x => x.Decision)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.Comment).HasMaxLength(2000).IsRequired();
        builder.Property(x => x.ReviewedAt).HasColumnType("timestamp with time zone").IsRequired();

        builder.HasIndex(x => x.DraftId);
    }
}
