using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using NexTraceOne.Governance.Domain.Entities;

namespace NexTraceOne.Governance.Infrastructure.Persistence.Configurations;

/// <summary>
/// Configuração EF Core para a entidade PolicyDefinitionVersion.
/// Tabela append-only com snapshots imutáveis de PolicyAsCodeDefinition.
/// Prefixo gov_ — alinhado com a baseline do módulo Governance.
/// </summary>
internal sealed class PolicyDefinitionVersionConfiguration : IEntityTypeConfiguration<PolicyDefinitionVersion>
{
    public void Configure(EntityTypeBuilder<PolicyDefinitionVersion> builder)
    {
        builder.ToTable("gov_policy_definition_versions");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => new PolicyDefinitionVersionId(value));

        builder.Property(x => x.PolicyId)
            .HasConversion(id => id.Value, value => new PolicyAsCodeDefinitionId(value))
            .IsRequired();

        builder.Property(x => x.TenantId).IsRequired();

        builder.Property(x => x.Version)
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(x => x.DefinitionContent)
            .HasColumnType("text")
            .IsRequired();

        builder.Property(x => x.Format)
            .HasConversion<string>()
            .HasMaxLength(10)
            .IsRequired();

        builder.Property(x => x.CreatedBy)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.CreatedAt)
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(x => x.ChangeNote)
            .HasMaxLength(500);

        builder.HasIndex(x => x.PolicyId);
        builder.HasIndex(x => x.TenantId);
        builder.HasIndex(x => new { x.PolicyId, x.Version });
    }
}
