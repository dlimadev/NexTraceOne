using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using NexTraceOne.ChangeGovernance.Domain.RulesetGovernance.Entities;

namespace NexTraceOne.ChangeGovernance.Infrastructure.RulesetGovernance.Persistence.Configurations;

internal sealed class RulesetConfiguration : IEntityTypeConfiguration<Ruleset>
{
    /// <summary>Configura o mapeamento da entidade Ruleset para a tabela rg_rulesets.</summary>
    public void Configure(EntityTypeBuilder<Ruleset> builder)
    {
        builder.ToTable("rg_rulesets");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => RulesetId.From(value));

        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(2000).IsRequired();
        builder.Property(x => x.Content).HasColumnType("text").IsRequired();
        builder.Property(x => x.RulesetType).HasColumnType("integer").IsRequired();
        builder.Property(x => x.IsActive).IsRequired().HasDefaultValue(true);
        builder.Property(x => x.RulesetCreatedAt).HasColumnType("timestamp with time zone").IsRequired();

        builder.Property(x => x.CreatedAt).HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(x => x.CreatedBy).HasMaxLength(500).IsRequired();
        builder.Property(x => x.UpdatedAt).HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(x => x.UpdatedBy).HasMaxLength(500).IsRequired();
        builder.Property(x => x.IsDeleted).IsRequired().HasDefaultValue(false);

        // ── Concorrência otimista (PostgreSQL xmin) ──────────────────────────
        builder.Property(x => x.RowVersion).IsRowVersion();
    }
}
