using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NexTraceOne.Catalog.Domain.Contracts.Entities;

namespace NexTraceOne.Catalog.Infrastructure.Contracts.Persistence.Configurations;

/// <summary>
/// Configuração EF Core para DataContractSchema.
/// Tabela: ctr_data_contract_schemas
/// </summary>
internal sealed class DataContractSchemaConfiguration : IEntityTypeConfiguration<DataContractSchema>
{
    public void Configure(EntityTypeBuilder<DataContractSchema> builder)
    {
        builder.ToTable("ctr_data_contract_schemas");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => new DataContractSchemaId(value));

        builder.Property(x => x.ApiAssetId).IsRequired();

        builder.Property(x => x.TenantId)
            .HasColumnName("tenant_id")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.Owner)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.SlaFreshnessHours).IsRequired();

        builder.Property(x => x.SchemaJson)
            .HasColumnType("jsonb")
            .IsRequired();

        builder.Property(x => x.PiiClassification)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(x => x.SourceSystem)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.ColumnCount).IsRequired();
        builder.Property(x => x.Version).IsRequired();

        builder.Property(x => x.CapturedAt)
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(x => x.CreatedAt)
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.HasIndex(x => new { x.TenantId, x.ApiAssetId, x.CapturedAt })
            .HasDatabaseName("ix_ctr_data_contract_schemas_api_tenant_captured");

        builder.HasIndex(x => new { x.TenantId, x.CapturedAt })
            .HasDatabaseName("ix_ctr_data_contract_schemas_tenant_captured");
    }
}
