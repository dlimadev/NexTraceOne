using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using NexTraceOne.Catalog.Domain.Contracts.Entities;

namespace NexTraceOne.Catalog.Infrastructure.Contracts.Persistence.Configurations;

/// <summary>
/// Configura o mapeamento EF Core da entidade BackgroundServiceDraftMetadata.
/// Armazena metadados específicos de Background Service para drafts em edição no Contract Studio.
/// Tabela: ctr_background_service_draft_metadata, com FK para ctr_contract_drafts.
/// </summary>
internal sealed class BackgroundServiceDraftMetadataConfiguration : IEntityTypeConfiguration<BackgroundServiceDraftMetadata>
{
    /// <summary>Configura o mapeamento da entidade BackgroundServiceDraftMetadata.</summary>
    public void Configure(EntityTypeBuilder<BackgroundServiceDraftMetadata> builder)
    {
        builder.ToTable("ctr_background_service_draft_metadata", t =>
        {
            t.HasCheckConstraint(
                "CK_ctr_bg_service_draft_messaging_role",
                "\"MessagingRole\" IN ('None', 'Producer', 'Consumer', 'Both')");
        });

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => BackgroundServiceDraftMetadataId.From(value));

        builder.Property(x => x.ContractDraftId)
            .HasConversion(id => id.Value, value => ContractDraftId.From(value))
            .IsRequired();

        builder.Property(x => x.ServiceName).HasMaxLength(300).IsRequired();
        builder.Property(x => x.Category).HasMaxLength(100).IsRequired();
        builder.Property(x => x.TriggerType).HasMaxLength(50).IsRequired();
        builder.Property(x => x.ScheduleExpression).HasMaxLength(200);
        builder.Property(x => x.InputsJson).HasColumnType("text").IsRequired();
        builder.Property(x => x.OutputsJson).HasColumnType("text").IsRequired();
        builder.Property(x => x.SideEffectsJson).HasColumnType("text").IsRequired();

        // Messaging role fields
        builder.Property(x => x.MessagingRole).HasMaxLength(50).IsRequired().HasDefaultValue("None");
        builder.Property(x => x.ConsumedTopicsJson).HasColumnType("text").IsRequired().HasDefaultValue("[]");
        builder.Property(x => x.ProducedTopicsJson).HasColumnType("text").IsRequired().HasDefaultValue("[]");
        builder.Property(x => x.ConsumedServicesJson).HasColumnType("text").IsRequired().HasDefaultValue("[]");
        builder.Property(x => x.ProducedEventsJson).HasColumnType("text").IsRequired().HasDefaultValue("[]");

        // Um BackgroundServiceDraftMetadata por ContractDraft (1:0..1)
        builder.HasIndex(x => x.ContractDraftId).IsUnique();
    }
}
