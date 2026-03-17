using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using NexTraceOne.Catalog.Domain.Contracts.Entities;

namespace NexTraceOne.Catalog.Infrastructure.Contracts.Persistence.Configurations;

/// <summary>
/// Configura o mapeamento EF Core da entidade ContractExample.
/// Exemplos podem estar vinculados a um draft ou a uma versão publicada de contrato.
/// Documentam cenários de uso com payloads de request/response ou eventos.
/// </summary>
internal sealed class ContractExampleConfiguration : IEntityTypeConfiguration<ContractExample>
{
    /// <summary>Configura o mapeamento da entidade ContractExample para a tabela ct_contract_examples.</summary>
    public void Configure(EntityTypeBuilder<ContractExample> builder)
    {
        builder.ToTable("ct_contract_examples");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => ContractExampleId.From(value));

        builder.Property(x => x.DraftId)
            .HasConversion(
                id => id == null ? (Guid?)null : id.Value,
                value => value.HasValue ? ContractDraftId.From(value.Value) : null);

        builder.Property(x => x.ContractVersionId)
            .HasConversion(
                id => id == null ? (Guid?)null : id.Value,
                value => value.HasValue ? ContractVersionId.From(value.Value) : null);

        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(2000).IsRequired();
        builder.Property(x => x.Content).HasColumnType("text").IsRequired();
        builder.Property(x => x.ContentFormat).HasMaxLength(50).IsRequired();
        builder.Property(x => x.ExampleType).HasMaxLength(100).IsRequired();
        builder.Property(x => x.CreatedBy).HasMaxLength(200).IsRequired();
        builder.Property(x => x.CreatedAt).HasColumnType("timestamp with time zone").IsRequired();

        builder.HasIndex(x => x.DraftId);
        builder.HasIndex(x => x.ContractVersionId);
    }
}
