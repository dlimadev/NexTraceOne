using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using NexTraceOne.Catalog.Domain.Contracts.Entities;

namespace NexTraceOne.Catalog.Infrastructure.Contracts.Persistence.Configurations;

/// <summary>
/// Configura o mapeamento EF Core da entidade EventContractDetail.
/// Armazena metadados AsyncAPI específicos de versões de contrato publicadas (Protocol = AsyncApi).
/// Tabela: ctr_event_contract_details, com FK para ctr_contract_versions.
/// </summary>
internal sealed class EventContractDetailConfiguration : IEntityTypeConfiguration<EventContractDetail>
{
    /// <summary>Configura o mapeamento da entidade EventContractDetail.</summary>
    public void Configure(EntityTypeBuilder<EventContractDetail> builder)
    {
        builder.ToTable("ctr_event_contract_details");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => EventContractDetailId.From(value));

        builder.Property(x => x.ContractVersionId)
            .HasConversion(id => id.Value, value => ContractVersionId.From(value))
            .IsRequired();

        builder.Property(x => x.Title).HasMaxLength(300).IsRequired();
        builder.Property(x => x.AsyncApiVersion).HasMaxLength(20).IsRequired();
        builder.Property(x => x.DefaultContentType).HasMaxLength(200).IsRequired();
        builder.Property(x => x.ChannelsJson).HasColumnType("text").IsRequired();
        builder.Property(x => x.MessagesJson).HasColumnType("text").IsRequired();
        builder.Property(x => x.ServersJson).HasColumnType("text").IsRequired();

        // Auditoria
        builder.Property(x => x.CreatedAt).HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(x => x.CreatedBy).HasMaxLength(500).IsRequired();
        builder.Property(x => x.UpdatedAt).HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(x => x.UpdatedBy).HasMaxLength(500).IsRequired();
        builder.Property(x => x.IsDeleted).IsRequired().HasDefaultValue(false);

        // Um EventContractDetail por ContractVersion (1:0..1)
        builder.HasIndex(x => x.ContractVersionId).IsUnique();
        builder.HasIndex(x => x.IsDeleted).HasFilter("\"is_deleted\" = false");
    }
}
