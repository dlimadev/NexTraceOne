using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using NexTraceOne.Catalog.Domain.Contracts.Entities;

namespace NexTraceOne.Catalog.Infrastructure.Contracts.Persistence.Configurations;

/// <summary>
/// Configura o mapeamento EF Core da entidade ContractRuleViolation.
/// Violações de ruleset são armazenadas como entidades filhas de ContractVersion.
/// </summary>
internal sealed class ContractRuleViolationConfiguration : IEntityTypeConfiguration<ContractRuleViolation>
{
    /// <summary>Configura o mapeamento para a tabela ct_contract_rule_violations.</summary>
    public void Configure(EntityTypeBuilder<ContractRuleViolation> builder)
    {
        builder.ToTable("ct_contract_rule_violations");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => ContractRuleViolationId.From(value));

        builder.Property(x => x.ContractVersionId)
            .HasConversion(id => id.Value, value => ContractVersionId.From(value))
            .IsRequired();

        builder.Property(x => x.RulesetId).IsRequired(false);
        builder.Property(x => x.RuleName).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Severity).HasMaxLength(50).IsRequired();
        builder.Property(x => x.Message).HasMaxLength(2000).IsRequired();
        builder.Property(x => x.Path).HasMaxLength(1000);
        builder.Property(x => x.SuggestedFix).HasMaxLength(2000);
        builder.Property(x => x.DetectedAt).HasColumnType("timestamp with time zone").IsRequired();

        builder.HasIndex(x => x.ContractVersionId);
        builder.HasIndex(x => x.RulesetId);
    }
}
