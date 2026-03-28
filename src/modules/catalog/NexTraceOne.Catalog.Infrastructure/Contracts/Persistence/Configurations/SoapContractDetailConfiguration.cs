using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using NexTraceOne.Catalog.Domain.Contracts.Entities;

namespace NexTraceOne.Catalog.Infrastructure.Contracts.Persistence.Configurations;

/// <summary>
/// Configura o mapeamento EF Core da entidade SoapContractDetail.
/// Armazena metadados SOAP/WSDL específicos de versões de contrato publicadas (Protocol = Wsdl).
/// Tabela: ctr_soap_contract_details, com FK para ctr_contract_versions.
/// </summary>
internal sealed class SoapContractDetailConfiguration : IEntityTypeConfiguration<SoapContractDetail>
{
    /// <summary>Configura o mapeamento da entidade SoapContractDetail.</summary>
    public void Configure(EntityTypeBuilder<SoapContractDetail> builder)
    {
        builder.ToTable("ctr_soap_contract_details", t =>
        {
            t.HasCheckConstraint(
                "CK_ctr_soap_contract_details_soap_version",
                "\"SoapVersion\" IN ('1.1', '1.2')");
        });

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => SoapContractDetailId.From(value));

        builder.Property(x => x.ContractVersionId)
            .HasConversion(id => id.Value, value => ContractVersionId.From(value))
            .IsRequired();

        builder.Property(x => x.ServiceName).HasMaxLength(200).IsRequired();
        builder.Property(x => x.TargetNamespace).HasMaxLength(1000).IsRequired();
        builder.Property(x => x.SoapVersion).HasMaxLength(5).IsRequired();
        builder.Property(x => x.EndpointUrl).HasMaxLength(2000);
        builder.Property(x => x.WsdlSourceUrl).HasMaxLength(2000);
        builder.Property(x => x.PortTypeName).HasMaxLength(200);
        builder.Property(x => x.BindingName).HasMaxLength(200);
        builder.Property(x => x.ExtractedOperationsJson).HasColumnType("text").IsRequired();

        // Auditoria
        builder.Property(x => x.CreatedAt).HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(x => x.CreatedBy).HasMaxLength(500).IsRequired();
        builder.Property(x => x.UpdatedAt).HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(x => x.UpdatedBy).HasMaxLength(500).IsRequired();
        builder.Property(x => x.IsDeleted).IsRequired().HasDefaultValue(false);

        // Um SoapContractDetail por ContractVersion (1:0..1)
        builder.HasIndex(x => x.ContractVersionId).IsUnique();
        builder.HasIndex(x => x.IsDeleted).HasFilter("\"IsDeleted\" = false");
    }
}
