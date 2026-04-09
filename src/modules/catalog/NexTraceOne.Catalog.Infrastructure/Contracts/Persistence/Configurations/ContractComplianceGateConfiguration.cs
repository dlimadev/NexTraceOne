using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NexTraceOne.Catalog.Domain.Contracts.Entities;
using NexTraceOne.Catalog.Domain.Contracts.Enums;

namespace NexTraceOne.Catalog.Infrastructure.Contracts.Persistence.Configurations;

/// <summary>
/// Configuração EF Core para a entidade ContractComplianceGate.
/// Prefixo cat_ — alinhado com a baseline do módulo Catalog (Contracts).
/// </summary>
internal sealed class ContractComplianceGateConfiguration : IEntityTypeConfiguration<ContractComplianceGate>
{
    public void Configure(EntityTypeBuilder<ContractComplianceGate> builder)
    {
        builder.ToTable("cat_contract_compliance_gates");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => ContractComplianceGateId.From(value));

        builder.Property(x => x.Name)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.Description)
            .HasMaxLength(2000);

        builder.Property(x => x.Rules)
            .HasColumnType("jsonb");

        builder.Property(x => x.Scope)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.ScopeId)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.BlockOnViolation)
            .IsRequired();

        builder.Property(x => x.IsActive)
            .IsRequired();

        builder.Property(x => x.CreatedBy)
            .HasMaxLength(200);

        builder.Property(x => x.CreatedAt)
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(x => x.TenantId)
            .HasMaxLength(100);

        builder.Property(x => x.RowVersion).IsRowVersion();

        // Indexes
        builder.HasIndex(x => x.Scope);
        builder.HasIndex(x => x.ScopeId);
        builder.HasIndex(x => x.IsActive);
        builder.HasIndex(x => x.TenantId);
        builder.HasIndex(x => new { x.Scope, x.ScopeId, x.IsActive });
    }
}
