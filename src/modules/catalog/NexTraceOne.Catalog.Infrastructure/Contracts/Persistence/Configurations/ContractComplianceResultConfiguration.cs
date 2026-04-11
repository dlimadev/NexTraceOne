using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NexTraceOne.Catalog.Domain.Contracts.Entities;
using NexTraceOne.Catalog.Domain.Contracts.Enums;

namespace NexTraceOne.Catalog.Infrastructure.Contracts.Persistence.Configurations;

/// <summary>
/// Configuração EF Core para a entidade ContractComplianceResult.
/// Prefixo cat_ — alinhado com a baseline do módulo Catalog (Contracts).
/// </summary>
internal sealed class ContractComplianceResultConfiguration : IEntityTypeConfiguration<ContractComplianceResult>
{
    public void Configure(EntityTypeBuilder<ContractComplianceResult> builder)
    {
        builder.ToTable("cat_contract_compliance_results");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => ContractComplianceResultId.From(value));

        builder.Property(x => x.GateId)
            .IsRequired();

        builder.Property(x => x.ContractVersionId)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.ChangeId)
            .HasMaxLength(200);

        builder.Property(x => x.Result)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.Violations)
            .HasColumnType("jsonb");

        builder.Property(x => x.EvidencePackId)
            .HasMaxLength(200);

        builder.Property(x => x.EvaluatedAt)
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(x => x.TenantId)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.RowVersion).IsRowVersion();

        // Indexes
        builder.HasIndex(x => x.GateId);
        builder.HasIndex(x => x.ContractVersionId);
        builder.HasIndex(x => x.ChangeId);
        builder.HasIndex(x => x.Result);
        builder.HasIndex(x => x.TenantId);
        builder.HasIndex(x => x.EvaluatedAt);
    }
}
