using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using NexTraceOne.Catalog.Domain.Contracts.Entities;
using NexTraceOne.Catalog.Domain.Contracts.Enums;

namespace NexTraceOne.Catalog.Infrastructure.Contracts.Persistence.Configurations;

/// <summary>
/// Configura o mapeamento EF Core da entidade ContractLintRuleset.
/// Rulesets representam regras de linting customizadas para governança de contratos.
/// A tabela mantém o nome original "ctr_spectral_rulesets" para preservar compatibilidade
/// com dados e migrações existentes.
/// </summary>
internal sealed class ContractLintRulesetConfiguration : IEntityTypeConfiguration<ContractLintRuleset>
{
    public void Configure(EntityTypeBuilder<ContractLintRuleset> builder)
    {
        builder.ToTable("ctr_spectral_rulesets", t =>
        {
            t.HasCheckConstraint(
                "CK_ctr_spectral_rulesets_origin",
                "\"Origin\" IN ('Platform', 'Organization', 'Team', 'Imported')");
        });

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => ContractLintRulesetId.From(value));

        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(2000);
        builder.Property(x => x.Version).HasMaxLength(50).IsRequired();
        builder.Property(x => x.Content).HasColumnType("text").IsRequired();

        builder.Property(x => x.Origin)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.DefaultExecutionMode)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.EnforcementBehavior)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.OrganizationId).HasMaxLength(256);
        builder.Property(x => x.Owner).HasMaxLength(200);
        builder.Property(x => x.Domain).HasMaxLength(100);
        builder.Property(x => x.ApplicableServiceType).HasMaxLength(100);
        builder.Property(x => x.ApplicableProtocols).HasMaxLength(200);
        builder.Property(x => x.SourceUrl).HasMaxLength(1000);

        builder.Property(x => x.IsActive).IsRequired().HasDefaultValue(true);
        builder.Property(x => x.IsDefault).IsRequired().HasDefaultValue(false);

        // Auditoria
        builder.Property(x => x.CreatedAt).HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(x => x.CreatedBy).HasMaxLength(500).IsRequired();
        builder.Property(x => x.UpdatedAt).HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(x => x.UpdatedBy).HasMaxLength(500).IsRequired();
        builder.Property(x => x.IsDeleted).IsRequired().HasDefaultValue(false);

        // Concorrência otimista via PostgreSQL xmin
        builder.Property(x => x.RowVersion)
            .IsRowVersion();

        builder.HasIndex(x => x.Name);
        builder.HasIndex(x => x.Origin);
        builder.HasIndex(x => x.IsActive).HasFilter("\"IsActive\" = true");
        builder.HasIndex(x => x.OrganizationId);
        builder.HasIndex(x => x.IsDeleted).HasFilter("\"IsDeleted\" = false");
    }
}
