using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using NexTraceOne.Catalog.Domain.Contracts.Entities;

namespace NexTraceOne.Catalog.Infrastructure.Contracts.Persistence.Configurations;

/// <summary>
/// Configura o mapeamento EF Core da entidade SoapDraftMetadata.
/// Armazena metadados SOAP/WSDL específicos de drafts de contrato (ContractType = Soap).
/// Tabela: ctr_soap_draft_metadata, com FK para ctr_contract_drafts.
/// </summary>
internal sealed class SoapDraftMetadataConfiguration : IEntityTypeConfiguration<SoapDraftMetadata>
{
    /// <summary>Configura o mapeamento da entidade SoapDraftMetadata.</summary>
    public void Configure(EntityTypeBuilder<SoapDraftMetadata> builder)
    {
        builder.ToTable("ctr_soap_draft_metadata", t =>
        {
            t.HasCheckConstraint(
                "CK_ctr_soap_draft_metadata_soap_version",
                "soap_version IN ('1.1', '1.2')");
        });

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => SoapDraftMetadataId.From(value));

        builder.Property(x => x.ContractDraftId)
            .HasConversion(id => id.Value, value => ContractDraftId.From(value))
            .IsRequired();

        builder.Property(x => x.ServiceName).HasMaxLength(200).IsRequired();
        builder.Property(x => x.TargetNamespace).HasMaxLength(1000).IsRequired();
        builder.Property(x => x.SoapVersion).HasMaxLength(5).IsRequired();
        builder.Property(x => x.EndpointUrl).HasMaxLength(2000);
        builder.Property(x => x.PortTypeName).HasMaxLength(200);
        builder.Property(x => x.BindingName).HasMaxLength(200);
        builder.Property(x => x.OperationsJson).HasColumnType("text").IsRequired();

        // Um SoapDraftMetadata por ContractDraft (1:0..1)
        builder.HasIndex(x => x.ContractDraftId).IsUnique();
    }
}
