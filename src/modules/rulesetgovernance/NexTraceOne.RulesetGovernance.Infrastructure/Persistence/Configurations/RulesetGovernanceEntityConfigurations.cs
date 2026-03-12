using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NexTraceOne.RulesetGovernance.Domain.Entities;

namespace NexTraceOne.RulesetGovernance.Infrastructure.Persistence.Configurations;

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
    }
}

internal sealed class RulesetBindingConfiguration : IEntityTypeConfiguration<RulesetBinding>
{
    /// <summary>Configura o mapeamento da entidade RulesetBinding para a tabela rg_ruleset_bindings.</summary>
    public void Configure(EntityTypeBuilder<RulesetBinding> builder)
    {
        builder.ToTable("rg_ruleset_bindings");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => RulesetBindingId.From(value));

        builder.Property(x => x.RulesetId)
            .HasConversion(id => id.Value, value => RulesetId.From(value))
            .IsRequired();
        builder.Property(x => x.AssetType).HasMaxLength(100).IsRequired();
        builder.Property(x => x.BindingCreatedAt).HasColumnType("timestamp with time zone").IsRequired();

        builder.Property(x => x.CreatedAt).HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(x => x.CreatedBy).HasMaxLength(500).IsRequired();
        builder.Property(x => x.UpdatedAt).HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(x => x.UpdatedBy).HasMaxLength(500).IsRequired();
        builder.Property(x => x.IsDeleted).IsRequired().HasDefaultValue(false);

        builder.HasIndex(x => new { x.RulesetId, x.AssetType }).IsUnique();
    }
}

internal sealed class LintResultConfiguration : IEntityTypeConfiguration<LintResult>
{
    /// <summary>Configura o mapeamento da entidade LintResult para a tabela rg_lint_results.</summary>
    public void Configure(EntityTypeBuilder<LintResult> builder)
    {
        builder.ToTable("rg_lint_results");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => LintResultId.From(value));

        builder.Property(x => x.RulesetId)
            .HasConversion(id => id.Value, value => RulesetId.From(value))
            .IsRequired();
        builder.Property(x => x.ReleaseId).IsRequired();
        builder.Property(x => x.ApiAssetId).IsRequired();
        builder.Property(x => x.Score)
            .HasColumnType("numeric(5,2)")
            .HasPrecision(5, 2)
            .IsRequired();
        builder.Property(x => x.TotalFindings).IsRequired();

        builder.Property(x => x.Findings)
            .HasConversion(
                v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                v => (IReadOnlyList<Finding>)System.Text.Json.JsonSerializer.Deserialize<List<Finding>>(v, (System.Text.Json.JsonSerializerOptions?)null)!)
            .HasColumnType("text");

        builder.Property(x => x.ExecutedAt).HasColumnType("timestamp with time zone").IsRequired();

        builder.Property(x => x.CreatedAt).HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(x => x.CreatedBy).HasMaxLength(500).IsRequired();
        builder.Property(x => x.UpdatedAt).HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(x => x.UpdatedBy).HasMaxLength(500).IsRequired();
        builder.Property(x => x.IsDeleted).IsRequired().HasDefaultValue(false);

        builder.HasIndex(x => x.ReleaseId);
    }
}
