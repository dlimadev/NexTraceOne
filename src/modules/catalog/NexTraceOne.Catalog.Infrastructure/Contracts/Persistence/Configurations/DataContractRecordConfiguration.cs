using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using NexTraceOne.Catalog.Domain.Entities;

namespace NexTraceOne.Catalog.Infrastructure.Contracts.Persistence.Configurations;

/// <summary>
/// Configura o mapeamento EF Core da entidade DataContractRecord.
/// Wave AQ.1 — RegisterDataContract / GetDataContractComplianceReport.
/// </summary>
internal sealed class DataContractRecordConfiguration : IEntityTypeConfiguration<DataContractRecord>
{
    public void Configure(EntityTypeBuilder<DataContractRecord> builder)
    {
        builder.ToTable("ctr_data_contract_records");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.TenantId).HasMaxLength(200).IsRequired();
        builder.Property(x => x.ServiceId).HasMaxLength(200).IsRequired();
        builder.Property(x => x.DatasetName).HasMaxLength(300).IsRequired();
        builder.Property(x => x.ContractVersion).HasMaxLength(100).IsRequired();
        builder.Property(x => x.FreshnessRequirementHours);
        builder.Property(x => x.FieldDefinitionsJson).HasColumnType("jsonb");
        builder.Property(x => x.OwnerTeamId).HasMaxLength(200);
        builder.Property(x => x.Status).HasMaxLength(50).IsRequired();
        builder.Property(x => x.CreatedAt)
            .HasColumnType("timestamp with time zone")
            .IsRequired();
        builder.Property(x => x.UpdatedAt)
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.HasIndex(x => x.TenantId);
        builder.HasIndex(x => new { x.TenantId, x.ServiceId });
    }
}
