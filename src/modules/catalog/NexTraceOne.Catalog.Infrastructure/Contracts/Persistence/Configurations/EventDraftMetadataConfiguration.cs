using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using NexTraceOne.Catalog.Domain.Contracts.Entities;

namespace NexTraceOne.Catalog.Infrastructure.Contracts.Persistence.Configurations;

/// <summary>
/// Configura o mapeamento EF Core da entidade EventDraftMetadata.
/// Armazena metadados AsyncAPI específicos de drafts de contrato em edição (ContractType = Event).
/// Tabela: ctr_event_draft_metadata, com FK para ctr_contract_drafts.
/// </summary>
internal sealed class EventDraftMetadataConfiguration : IEntityTypeConfiguration<EventDraftMetadata>
{
    /// <summary>Configura o mapeamento da entidade EventDraftMetadata.</summary>
    public void Configure(EntityTypeBuilder<EventDraftMetadata> builder)
    {
        builder.ToTable("ctr_event_draft_metadata");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => EventDraftMetadataId.From(value));

        builder.Property(x => x.ContractDraftId)
            .HasConversion(id => id.Value, value => ContractDraftId.From(value))
            .IsRequired();

        builder.Property(x => x.Title).HasMaxLength(300).IsRequired();
        builder.Property(x => x.AsyncApiVersion).HasMaxLength(20).IsRequired();
        builder.Property(x => x.DefaultContentType).HasMaxLength(200).IsRequired();
        builder.Property(x => x.ChannelsJson).HasColumnType("text").IsRequired();
        builder.Property(x => x.MessagesJson).HasColumnType("text").IsRequired();

        // Um EventDraftMetadata por ContractDraft (1:0..1)
        builder.HasIndex(x => x.ContractDraftId).IsUnique();
    }
}
